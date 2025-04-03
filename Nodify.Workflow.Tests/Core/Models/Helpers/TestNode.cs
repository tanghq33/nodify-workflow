using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Models; // Use the base Node class

namespace Nodify.Workflow.Tests.Core.Models.Helpers;

/// <summary>
/// A simple concrete implementation of the abstract Node class for testing purposes.
/// </summary>
internal class TestNode : Node
{
    /// <summary>
    /// Provides a minimal successful implementation for the abstract ExecuteAsync method.
    /// </summary>
    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context)
    {
        // Simulate successful execution for testing basic node/graph functionality
        return Task.FromResult(NodeExecutionResult.Succeeded());
    }

    // Optionally override other virtual methods like Validate if needed for specific tests
} 