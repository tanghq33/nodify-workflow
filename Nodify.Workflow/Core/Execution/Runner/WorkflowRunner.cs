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

    // Updated constructor
    public WorkflowRunner(IGraphTraversal traversal, INodeExecutor nodeExecutor)
    {
        _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
        _nodeExecutor = nodeExecutor ?? throw new ArgumentNullException(nameof(nodeExecutor));
    }

    // Actual execution logic will be implemented here
    public virtual async Task RunAsync(INode startNode, IExecutionContext context, CancellationToken cancellationToken = default)
    {
        Guid workflowId = context.ExecutionId;
        // Check for immediate cancellation before even starting
        if (cancellationToken.IsCancellationRequested)
        {
            context.SetStatus(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Cancelled);
            // Do not throw here, just return. The workflow didn't technically start.
            return;
        }

        context.SetStatus(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Running);
        OnWorkflowStarted(new WorkflowExecutionStartedEventArgs(context, workflowId));

        try
        {
            // 2. Get execution order
            IReadOnlyList<INode> executionOrder;
            try
            {
                // Pass token if traversal supports it (assuming not for now based on IGraphTraversal)
                cancellationToken.ThrowIfCancellationRequested();
                executionOrder = _traversal.TopologicalSort(startNode);
            }
            catch (OperationCanceledException) { throw; } // Re-throw cancellation
            catch (Exception ex)
            {
                 context.SetStatus(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Failed);
                 OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, new InvalidOperationException("Workflow failed during graph traversal.", ex)));
                 return;
            }

            // 3. Loop through nodes
            foreach (var node in executionOrder)
            {
                // Check for cancellation before starting the node
                cancellationToken.ThrowIfCancellationRequested();

                // Raise specific event type
                OnNodeStarting(new NodeExecutionStartingEventArgs(node, context));

                try
                {
                    // Pass token to executor
                    NodeExecutionResult result = await _nodeExecutor.ExecuteAsync(node, context, cancellationToken);

                    // Check cancellation *after* await, in case node execution finished but cancellation was requested during await
                    cancellationToken.ThrowIfCancellationRequested();

                    // Raise specific event type
                    if (result.Success)
                    {
                        OnNodeCompleted(new NodeExecutionCompletedEventArgs(node, context));
                    }
                    else
                    {
                        // Node failed - result.Error should not be null
                        OnNodeFailed(new NodeExecutionFailedEventArgs(node, context, result.Error!));
                        context.SetStatus(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Failed);
                        OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, result.Error!, node));
                        return; // Stop execution
                    }
                }
                // Catch OCE separately from other node execution errors
                catch (OperationCanceledException) { throw; } // Re-throw to be caught by the outer handler
                catch (Exception ex) // Catch errors during node execution or event raising
                {
                     OnNodeFailed(new NodeExecutionFailedEventArgs(node, context, ex));
                     context.SetStatus(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Failed);
                     OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, ex, node));
                     return; // Stop execution
                }
            }

            // 4. If loop completes without error
            context.SetStatus(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Completed);
            OnWorkflowCompleted(new WorkflowExecutionCompletedEventArgs(context, context.CurrentStatus));
        }
        catch (OperationCanceledException) // Catch cancellation from anywhere within the try block
        {
            context.SetStatus(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Cancelled);
            OnWorkflowCancelled(new WorkflowCancelledEventArgs(context));
            // Do not treat cancellation as a workflow failure
        }
        catch (Exception ex) // Catch unexpected errors during overall workflow setup/execution
        {
             // 5. Handle unexpected exceptions
             context.SetStatus(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Failed);
             OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, ex)); // Failed node is null here
        }
    }

    // Helper methods with updated signatures
    protected virtual void OnWorkflowStarted(WorkflowExecutionStartedEventArgs e) => WorkflowStarted?.Invoke(this, e);
    protected virtual void OnNodeStarting(NodeExecutionStartingEventArgs e) => NodeStarting?.Invoke(this, e);
    protected virtual void OnNodeCompleted(NodeExecutionCompletedEventArgs e) => NodeCompleted?.Invoke(this, e);
    protected virtual void OnNodeFailed(NodeExecutionFailedEventArgs e) => NodeFailed?.Invoke(this, e);
    protected virtual void OnWorkflowCompleted(WorkflowExecutionCompletedEventArgs e) => WorkflowCompleted?.Invoke(this, e);
    protected virtual void OnWorkflowFailed(WorkflowExecutionFailedEventArgs e) => WorkflowFailed?.Invoke(this, e);
    protected virtual void OnWorkflowCancelled(WorkflowCancelledEventArgs e) => WorkflowCancelled?.Invoke(this, e);
} 