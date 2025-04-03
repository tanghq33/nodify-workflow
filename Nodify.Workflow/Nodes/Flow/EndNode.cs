using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Registry;
using System;

namespace Nodify.Workflow.Nodes.Flow;

[WorkflowNode("End", "Flow Control", Description = "Marks the termination point of a workflow path.")]
public class EndNode : Node
{
    public const string InputConnectorName = "End";

    public EndNode()
    {
        AddInputConnector(new Connector(this, ConnectorDirection.Input, typeof(object)));
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        // End node simply terminates the flow
        return Task.FromResult(NodeExecutionResult.Succeeded());
    }
} 