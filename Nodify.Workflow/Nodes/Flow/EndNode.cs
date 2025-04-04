using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Registry;
using System;

namespace Nodify.Workflow.Nodes.Flow;

[WorkflowNode("End", "Flow", Description = "The ending point of the workflow.")]
public class EndNode : Node
{
    public const string InputConnectorName = "End";

    public EndNode()
    {
        AddInputConnector(new Connector(this, ConnectorDirection.Input, typeof(object)));
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        // InputData is ignored by EndNode.
        // Workflow successfully completed, return success without activating any output connector.
        return Task.FromResult(NodeExecutionResult.Succeeded());
    }
} 