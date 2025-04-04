using System;
using System.Threading.Tasks;
using System.Threading;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Execution.Events;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

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

    private readonly INodeExecutor _nodeExecutor;

    public WorkflowRunner(INodeExecutor nodeExecutor, IGraphTraversal? traversal = null)
    {
        _nodeExecutor = nodeExecutor ?? throw new ArgumentNullException(nameof(nodeExecutor));
    }

    public virtual async Task RunAsync(INode startNode, IExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (startNode == null) throw new ArgumentNullException(nameof(startNode));
        if (context == null) throw new ArgumentNullException(nameof(context));

        Guid workflowId = context.ExecutionId;
        if (cancellationToken.IsCancellationRequested)
        {
            context.SetStatus(ExecutionStatus.Cancelled);
            Debug.WriteLine($"[WorkflowRunner RunAsync] Cancelled before start. Workflow ID: {workflowId}");
            return;
        }

        context.SetStatus(ExecutionStatus.Running);
        OnWorkflowStarted(new WorkflowExecutionStartedEventArgs(context, workflowId));
        Debug.WriteLine($"[WorkflowRunner RunAsync] Started. Workflow ID: {workflowId}");

        try
        {
            var executedNodes = new HashSet<INode>();
            var executionQueue = new Queue<(INode Node, object? InputData)>();

            // Start with the initial node
            executionQueue.Enqueue((startNode, null));
            Debug.WriteLine($"[WorkflowRunner RunAsync] Enqueued start node: {startNode.GetType().Name}");

            while (executionQueue.Count > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    context.SetStatus(ExecutionStatus.Cancelled);
                    OnWorkflowCancelled(new WorkflowCancelledEventArgs(context));
                    Debug.WriteLine("[WorkflowRunner RunAsync] Cancelled during execution.");
                    return;
                }

                INode currentNode;
                object? inputData;

                // Get next node from queue
                (currentNode, inputData) = executionQueue.Dequeue();
                Debug.WriteLine($"[WorkflowRunner RunAsync] Dequeued node: {currentNode.GetType().Name}");
                
                // Skip if node was already executed (prevents cycles/re-execution)
                if (executedNodes.Contains(currentNode))
                {
                    Debug.WriteLine($"[WorkflowRunner RunAsync] Skipping node already executed: {currentNode.GetType().Name}");
                    continue;
                }

                OnNodeStarting(new NodeExecutionStartingEventArgs(currentNode, context));
                Debug.WriteLine($"[WorkflowRunner RunAsync] Executing node: {currentNode.GetType().Name}");
                NodeExecutionResult result;

                try
                {
                    result = await _nodeExecutor.ExecuteAsync(currentNode, context, inputData, cancellationToken);
                    Debug.WriteLine($"[WorkflowRunner RunAsync] Node {currentNode.GetType().Name} executed. Success: {result.Success}, Activated Output: {result.ActivatedOutputConnectorId?.ToString() ?? "None"}");
                }
                catch (OperationCanceledException)
                {
                    context.SetStatus(ExecutionStatus.Cancelled);
                    OnWorkflowCancelled(new WorkflowCancelledEventArgs(context));
                    Debug.WriteLine($"[WorkflowRunner RunAsync] Operation cancelled during node execution: {currentNode.GetType().Name}");
                    return;
                }
                catch (Exception nodeExecutionEx)
                {
                    OnNodeFailed(new NodeExecutionFailedEventArgs(currentNode, context, nodeExecutionEx));
                    context.SetStatus(ExecutionStatus.Failed);
                    OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, nodeExecutionEx, currentNode));
                    Debug.WriteLine($"[WorkflowRunner RunAsync] Exception during node execution: {currentNode.GetType().Name}, Error: {nodeExecutionEx.Message}");
                    return;
                }

                if (result.Success)
                {
                    OnNodeCompleted(new NodeExecutionCompletedEventArgs(currentNode, context));
                    executedNodes.Add(currentNode);
                    Debug.WriteLine($"[WorkflowRunner RunAsync] Added {currentNode.GetType().Name} to executed nodes.");

                    // If an output connector was activated, enqueue its connected nodes
                    if (result.ActivatedOutputConnectorId.HasValue)
                    {
                        Debug.WriteLine($"[WorkflowRunner RunAsync] Node {currentNode.GetType().Name} activated output: {result.ActivatedOutputConnectorId.Value}");
                        var activatedConnector = currentNode.OutputConnectors
                            .FirstOrDefault(c => c.Id == result.ActivatedOutputConnectorId.Value);

                        if (activatedConnector != null)
                        {
                            Debug.WriteLine($"[WorkflowRunner RunAsync] Found activated connector with ID: {activatedConnector.Id}");
                            foreach (var connection in activatedConnector.Connections)
                            {
                                var nextNode = connection.Target?.ParentNode;
                                Debug.WriteLine($"[WorkflowRunner RunAsync] Checking connection from activated connector {activatedConnector.Id} -> Target: {(connection.Target != null ? connection.Target.Id : "null")} -> Next Node: {(nextNode != null ? nextNode.GetType().Name : "null")}");
                                if (nextNode != null && !executedNodes.Contains(nextNode))
                                {
                                    executionQueue.Enqueue((nextNode, result.OutputData));
                                    Debug.WriteLine($"[WorkflowRunner RunAsync] Enqueued next node from activated connector: {nextNode.GetType().Name}");
                                }
                                else if (nextNode != null)
                                {
                                    Debug.WriteLine($"[WorkflowRunner RunAsync] Skipping already executed next node from activated connector: {nextNode.GetType().Name}");
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[WorkflowRunner RunAsync] Could not find activated connector with ID: {result.ActivatedOutputConnectorId.Value} on node {currentNode.GetType().Name}");
                        }
                    }
                    // If no output connector was activated and the node has only one output connector,
                    // follow that connector (for simple flow-through nodes)
                    else if (currentNode.OutputConnectors.Count == 1)
                    {
                        Debug.WriteLine($"[WorkflowRunner RunAsync] Node {currentNode.GetType().Name} has 1 output, following it.");
                        var connector = currentNode.OutputConnectors.First();
                        foreach (var connection in connector.Connections)
                        {
                            var nextNode = connection.Target?.ParentNode;
                            Debug.WriteLine($"[WorkflowRunner RunAsync] Checking connection from single output connector {connector.Id} -> Target: {(connection.Target != null ? connection.Target.Id : "null")} -> Next Node: {(nextNode != null ? nextNode.GetType().Name : "null")}");
                            if (nextNode != null && !executedNodes.Contains(nextNode))
                            {
                                executionQueue.Enqueue((nextNode, result.OutputData));
                                Debug.WriteLine($"[WorkflowRunner RunAsync] Enqueued next node from single output: {nextNode.GetType().Name}");
                            }
                            else if (nextNode != null)
                            {
                                Debug.WriteLine($"[WorkflowRunner RunAsync] Skipping already executed next node from single output: {nextNode.GetType().Name}");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[WorkflowRunner RunAsync] Node {currentNode.GetType().Name} finished. No specific output activated and more than one output connector.");
                    }
                }
                else
                {
                    // Check if the failure is due to cancellation
                    if (result.Error is OperationCanceledException)
                    {
                        context.SetStatus(ExecutionStatus.Cancelled);
                        OnWorkflowCancelled(new WorkflowCancelledEventArgs(context));
                        Debug.WriteLine($"[WorkflowRunner RunAsync] Node {currentNode.GetType().Name} execution returned failed due to cancellation.");
                        return;
                    }

                    OnNodeFailed(new NodeExecutionFailedEventArgs(currentNode, context, result.Error!));
                    context.SetStatus(ExecutionStatus.Failed);
                    OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, result.Error!, currentNode));
                    Debug.WriteLine($"[WorkflowRunner RunAsync] Node {currentNode.GetType().Name} execution failed: {result.Error?.Message}");
                    return;
                }
            }

            // All nodes in the execution path have completed successfully
            context.SetStatus(ExecutionStatus.Completed);
            OnWorkflowCompleted(new WorkflowExecutionCompletedEventArgs(context, context.CurrentStatus));
            Debug.WriteLine($"[WorkflowRunner RunAsync] Completed successfully. Workflow ID: {workflowId}");
        }
        catch (Exception ex)
        {
            // Only treat non-cancellation exceptions as failures
            if (ex is OperationCanceledException)
            {
                context.SetStatus(ExecutionStatus.Cancelled);
                OnWorkflowCancelled(new WorkflowCancelledEventArgs(context));
                Debug.WriteLine($"[WorkflowRunner RunAsync] Caught OperationCanceledException. Workflow ID: {workflowId}");
            }
            else
            {
                context.SetStatus(ExecutionStatus.Failed);
                OnWorkflowFailed(new WorkflowExecutionFailedEventArgs(context, ex, null));
                Debug.WriteLine($"[WorkflowRunner RunAsync] Caught unhandled exception: {ex.Message}. Workflow ID: {workflowId}");
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