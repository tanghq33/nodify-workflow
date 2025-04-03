using System;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution;

/// <summary>
/// Default, basic implementation of INodeExecutor.
/// Currently uses temporary simulation logic for node execution.
/// TODO: Replace simulation with actual node execution logic invocation (e.g., calling node.Execute()).
/// </summary>
public class DefaultNodeExecutor : INodeExecutor
{
    public async Task<NodeExecutionResult> ExecuteAsync(INode node, IExecutionContext context)
    {
        try
        {
            // Removed Temporary TDD Simulation Logic
            // Directly call the node's execution logic
            return await node.ExecuteAsync(context);
        }
        catch(Exception ex)
        {
             // Catch unexpected errors during node execution
             return NodeExecutionResult.Failed(ex);
        }
    }
} 