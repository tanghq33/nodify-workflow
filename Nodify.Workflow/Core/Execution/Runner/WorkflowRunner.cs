using System;
using System.Threading.Tasks;
using System.Threading;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Execution.Events;
using System.Collections.Generic;
using System.Linq;

namespace Nodify.Workflow.Core.Execution.Runner;

public class WorkflowRunner
{
    // Define events using specific derived types
    public event EventHandler<WorkflowExecutionStartedEventArgs>? WorkflowStarted;
    public event EventHandler<NodeExecutionStartingEventArgs>? NodeStarting;
    public event EventHandler<NodeExecutionCompletedEventArgs>? NodeCompleted;
    public event EventHandler<NodeExecutionFailedEventArgs>? NodeFailed;
    public event EventHandler<WorkflowExecutionCompletedEventArgs>? WorkflowCompleted;
    public event EventHandler<WorkflowExecutionFailedEventArgs>? WorkflowFailed;
    public event EventHandler<WorkflowCancelledEventArgs>? WorkflowCancelled;

    private readonly IGraphTraversal _traversal;
    private readonly INodeExecutor _nodeExecutor;

    public WorkflowRunner(INodeExecutor nodeExecutor, IGraphTraversal? traversal = null)
    {
        _traversal = traversal ?? new DefaultGraphTraversal();
        _nodeExecutor = nodeExecutor ?? throw new ArgumentNullException(nameof(nodeExecutor));
    }

    public virtual async Task RunAsync(INode startNode, IExecutionContext context, CancellationToken cancellationToken = default)
    {
        Guid workflowId = context.ExecutionId;
        if (cancellationToken.IsCancellationRequested)
        {
            context.SetStatus(Context.ExecutionStatus.Cancelled);
            return;
        }

        context.SetStatus(Context.ExecutionStatus.Running);
        OnWorkflowStarted(new WorkflowExecutionStartedEventArgs(context, workflowId));

        try
        {
            // Get all nodes in topological order
            var orderedNodes = _traversal.TopologicalSort(startNode);
            object? lastOutputData = null;

            foreach (var currentNode in orderedNodes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    context.SetStatus(Context.ExecutionStatus.Cancelled);
                    OnWorkflowCancelled(new WorkflowCancelledEventArgs(context));
                    return;
                }

                OnNodeStarting(new NodeExecutionStartingEventArgs(currentNode, context));
                NodeExecutionResult result;
                try
                {
                    result = await _nodeExecutor.ExecuteAsync(currentNode, context, lastOutputData, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation by stopping execution and raising the cancelled event
                    context.SetStatus(Context.ExecutionStatus.Cancelled);
                    OnWorkflowCancelled(new WorkflowCancelledEventArgs(context));
                    return;
                }
                catch (Exception nodeExecutionEx)
                {
                    // Only treat non-cancellation exceptions as failures
                    OnNodeFailed(new NodeExecutionFailedEventArgs(currentNode, context, nodeExecutionEx));
                    context.SetStatus(Context.ExecutionStatus.Failed);
                    OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, nodeExecutionEx, currentNode));
                    return;
                }

                if (result.Success)
                {
                    OnNodeCompleted(new NodeExecutionCompletedEventArgs(currentNode, context));
                    lastOutputData = result.OutputData;
                }
                else
                {
                    // Check if the failure is due to cancellation
                    if (result.Error is OperationCanceledException)
                    {
                        context.SetStatus(Context.ExecutionStatus.Cancelled);
                        OnWorkflowCancelled(new WorkflowCancelledEventArgs(context));
                        return;
                    }

                    OnNodeFailed(new NodeExecutionFailedEventArgs(currentNode, context, result.Error!));
                    context.SetStatus(Context.ExecutionStatus.Failed);
                    OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, result.Error!, currentNode));
                    return;
                }
            }

            context.SetStatus(Context.ExecutionStatus.Completed);
            OnWorkflowCompleted(new WorkflowExecutionCompletedEventArgs(context, context.CurrentStatus));
        }
        catch (Exception ex)
        {
            // Only treat non-cancellation exceptions as failures
            if (ex is OperationCanceledException)
            {
                context.SetStatus(Context.ExecutionStatus.Cancelled);
                OnWorkflowCancelled(new WorkflowCancelledEventArgs(context));
            }
            else
            {
                context.SetStatus(Context.ExecutionStatus.Failed);
                OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, ex, null));
            }
        }
    }

    protected virtual void OnWorkflowStarted(WorkflowExecutionStartedEventArgs e) => WorkflowStarted?.Invoke(this, e);
    protected virtual void OnNodeStarting(NodeExecutionStartingEventArgs e) => NodeStarting?.Invoke(this, e);
    protected virtual void OnNodeCompleted(NodeExecutionCompletedEventArgs e) => NodeCompleted?.Invoke(this, e);
    protected virtual void OnNodeFailed(NodeExecutionFailedEventArgs e) => NodeFailed?.Invoke(this, e);
    protected virtual void OnWorkflowCompleted(WorkflowExecutionCompletedEventArgs e) => WorkflowCompleted?.Invoke(this, e);
    protected virtual void OnWorkflowFailed(WorkflowExecutionFailedEventArgs e) => WorkflowFailed?.Invoke(this, e);
    protected virtual void OnWorkflowCancelled(WorkflowCancelledEventArgs e) => WorkflowCancelled?.Invoke(this, e);
} 