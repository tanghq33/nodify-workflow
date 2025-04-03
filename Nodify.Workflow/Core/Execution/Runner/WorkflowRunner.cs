using System;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Execution.Events;
using System.Collections.Generic;
using System.Linq;

namespace Nodify.Workflow.Core.Execution.Runner;

public class WorkflowRunner
{
    // Define events using standard EventHandler pattern
    public event EventHandler<WorkflowExecutionStartedEventArgs>? WorkflowStarted;
    public event EventHandler<NodeExecutionEventArgs>? NodeStarting;
    public event EventHandler<NodeExecutionEventArgs>? NodeCompleted;
    public event EventHandler<NodeExecutionFailedEventArgs>? NodeFailed;
    public event EventHandler<WorkflowExecutionCompletedEventArgs>? WorkflowCompleted;
    public event EventHandler<WorkflowExecutionFailedEventArgs>? WorkflowFailed;

    private readonly IGraphTraversal _traversal;
    private readonly INodeExecutor _nodeExecutor;

    // Updated constructor
    public WorkflowRunner(IGraphTraversal traversal, INodeExecutor nodeExecutor)
    {
        _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
        _nodeExecutor = nodeExecutor ?? throw new ArgumentNullException(nameof(nodeExecutor));
    }

    // Actual execution logic will be implemented here
    public virtual async Task RunAsync(INode startNode, IExecutionContext context)
    {
        Guid workflowId = context.ExecutionId;
        context.SetStatus(ExecutionStatus.Running);
        OnWorkflowStarted(new WorkflowExecutionStartedEventArgs(context, workflowId));

        try
        {
            // 2. Get execution order
            IReadOnlyList<INode> executionOrder;
            try
            {
                executionOrder = _traversal.TopologicalSort(startNode);
            }
            catch (Exception ex)
            {
                 context.SetStatus(ExecutionStatus.Failed);
                 OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, new InvalidOperationException("Workflow failed during graph traversal.", ex)));
                 return;
            }

            // 3. Loop through nodes
            foreach (var node in executionOrder)
            {
                OnNodeStarting(new NodeExecutionEventArgs(node, context));

                try
                {
                    // *** Replaced simulation with executor call ***
                    NodeExecutionResult result = await _nodeExecutor.ExecuteAsync(node, context);

                    // Raise events based on execution result
                    if (result.Success)
                    {
                        OnNodeCompleted(new NodeExecutionEventArgs(node, context));
                    }
                    else
                    {
                        // Node failed - result.Error should not be null
                        OnNodeFailed(new NodeExecutionFailedEventArgs(node, context, result.Error!));
                        context.SetStatus(ExecutionStatus.Failed);
                        OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, result.Error!, node));
                        return; // Stop execution
                    }
                }
                catch (Exception ex) // Catch errors during node execution or event raising
                {
                     OnNodeFailed(new NodeExecutionFailedEventArgs(node, context, ex));
                     context.SetStatus(ExecutionStatus.Failed);
                     OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, ex, node));
                     return; // Stop execution
                }
            }

            // 4. If loop completes without error
            context.SetStatus(ExecutionStatus.Completed);
            OnWorkflowCompleted(new WorkflowExecutionCompletedEventArgs(context, context.CurrentStatus));
        }
        catch (Exception ex) // Catch unexpected errors during overall workflow setup/execution
        {
             // 5. Handle unexpected exceptions
             context.SetStatus(ExecutionStatus.Failed);
             OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, ex)); // Failed node is null here
        }
    }

    // Helper methods to raise events safely
    protected virtual void OnWorkflowStarted(WorkflowExecutionStartedEventArgs e) => WorkflowStarted?.Invoke(this, e);
    protected virtual void OnNodeStarting(NodeExecutionEventArgs e) => NodeStarting?.Invoke(this, e);
    protected virtual void OnNodeCompleted(NodeExecutionEventArgs e) => NodeCompleted?.Invoke(this, e);
    protected virtual void OnNodeFailed(NodeExecutionFailedEventArgs e) => NodeFailed?.Invoke(this, e);
    protected virtual void OnWorkflowCompleted(WorkflowExecutionCompletedEventArgs e) => WorkflowCompleted?.Invoke(this, e);
    protected virtual void OnWorkflowFailed(WorkflowExecutionFailedEventArgs e) => WorkflowFailed?.Invoke(this, e);
} 