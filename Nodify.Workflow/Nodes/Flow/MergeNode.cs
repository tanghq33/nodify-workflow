using System;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Registry;

namespace Nodify.Workflow.Nodes.Flow;

[WorkflowNode("Merge", "Flow", Description = "Merges multiple input flows into one, activating the output when any input is activated.")]
public class MergeNode : Node
{
    private readonly Guid _inputConnectorAId;
    private readonly Guid _inputConnectorBId;
    internal readonly Guid _outputConnectorId;

    public MergeNode()
    {
        // Input Connectors
        var inputA = new Connector(this, ConnectorDirection.Input, typeof(object));
        _inputConnectorAId = inputA.Id;
        AddInputConnector(inputA);

        var inputB = new Connector(this, ConnectorDirection.Input, typeof(object));
        _inputConnectorBId = inputB.Id;
        AddInputConnector(inputB);

        // Output Connector
        var output = new Connector(this, ConnectorDirection.Output, typeof(object));
        _outputConnectorId = output.Id;
        AddOutputConnector(output);
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        // This node is activated because one of its inputs received a signal.
        // It simply passes the received signal and any accompanying data forward through its single output.
        return Task.FromResult(NodeExecutionResult.SucceededWithData(_outputConnectorId, inputData));
    }
} 