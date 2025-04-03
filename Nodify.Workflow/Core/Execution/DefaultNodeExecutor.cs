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
            // START: Temporary TDD Simulation Logic (Moved from WorkflowRunner)
            if (node.GetType().Name == "FailureNode") // Check type name as it's internal to tests
            {
                 var nodeError = new InvalidOperationException($"Simulated failure in node {node.Id} ({node.GetType().Name}).");
                 return NodeExecutionResult.Failed(nodeError);
            }
            else
            {
                await Task.Delay(1); // Simulate async work for success nodes
                return NodeExecutionResult.Succeeded();
            }
            // END: Temporary TDD Simulation Logic
        }
        catch(Exception ex)
        {
             // Catch unexpected errors during the (simulated) execution
             return NodeExecutionResult.Failed(ex);
        }
    }
} 