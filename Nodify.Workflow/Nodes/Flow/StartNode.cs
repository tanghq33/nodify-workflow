using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Models; // Added for Connector
using Nodify.Workflow.Core.Interfaces; // Added for Enums
using Nodify.Workflow.Core.Registry;
using System;
using System.Linq;

namespace Nodify.Workflow.Nodes.Flow;

[WorkflowNode("Start", "Flow", Description = "The starting point of the workflow.")]
public class StartNode : Node
{
    private readonly Guid _outputConnectorId;

    public StartNode()
    {
        var output = new Connector(this, ConnectorDirection.Output, typeof(object));
        _outputConnectorId = output.Id;
        AddOutputConnector(output);
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        // InputData is ignored by StartNode.
        // Simply activate the output connector
        return Task.FromResult(NodeExecutionResult.Succeeded(_outputConnectorId));
    }
} 