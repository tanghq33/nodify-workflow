using System;
using System.Threading.Tasks;
using System.Threading;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Execution.Events;

namespace Nodify.Workflow.Core.Execution;

/// <summary>
/// Default, basic implementation of INodeExecutor.
/// Currently uses temporary simulation logic for node execution.
/// TODO: Replace simulation with actual node execution logic invocation (e.g., calling node.Execute()).
/// </summary>
public class DefaultNodeExecutor : INodeExecutor
{
    /// <summary>
    /// Executes the node's logic by calling its ExecuteAsync method.
    /// </summary>
    /// <param name="node">The node to execute.</param>
    /// <param name="context">The current execution context.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The result of the node's execution.</returns>
    public async Task<NodeExecutionResult> ExecuteAsync(INode node, IExecutionContext context, CancellationToken cancellationToken)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        try
        {
            // Removed Temporary TDD Simulation Logic
            // Directly call the node's execution logic
            return await node.ExecuteAsync(context, cancellationToken);
        }
        catch(Exception ex)
        {
             // Catch unexpected errors during node execution
             return NodeExecutionResult.Failed(ex);
        }
    }
} 