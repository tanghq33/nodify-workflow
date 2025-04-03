using System.Threading.Tasks;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution;

/// <summary>
/// Defines the contract for executing the logic of a workflow node.
/// </summary>
public interface INodeExecutor
{
    /// <summary>
    /// Executes the specified node within the given execution context.
    /// </summary>
    /// <param name="node">The node to execute.</param>
    /// <param name="context">The current execution context.</param>
    /// <returns>A task representing the asynchronous execution, yielding a NodeExecutionResult.</returns>
    Task<NodeExecutionResult> ExecuteAsync(INode node, IExecutionContext context);
} 