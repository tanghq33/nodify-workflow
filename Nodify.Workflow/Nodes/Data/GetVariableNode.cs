using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Registry;

namespace Nodify.Workflow.Nodes.Data;

[WorkflowNode("Get Variable", "Data", Description = "Retrieves a variable from the workflow context.")]
public class GetVariableNode : Node
{
    // Configurable Property
    public string VariableName { get; set; } = string.Empty;

    // Store connector IDs after creation for easy lookup
    private Guid _flowInputId;
    private Guid _flowOutputId;
    private Guid _valueOutputId;

    public GetVariableNode()
    {
        // Input Flow Connector
        var flowIn = new Connector(this, ConnectorDirection.Input, typeof(object));
        _flowInputId = flowIn.Id;
        AddInputConnector(flowIn);

        // Output Flow Connector
        var flowOut = new Connector(this, ConnectorDirection.Output, typeof(object));
        _flowOutputId = flowOut.Id;
        AddOutputConnector(flowOut);

        // Output Data Connector
        var valueOut = new Connector(this, ConnectorDirection.Output, typeof(object)); // Default to object type
        _valueOutputId = valueOut.Id;
        AddOutputConnector(valueOut);
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(VariableName))
        {
            return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException("VariableName property cannot be empty.")));
        }

        if (context.TryGetVariable<object>(VariableName, out var variableValue))
        {
            // Variable found, set the output connector value
            context.SetOutputConnectorValue(_valueOutputId, variableValue);
            
            // Activate the standard flow output
            return Task.FromResult(NodeExecutionResult.Succeeded(_flowOutputId));
        }
        else
        {
            // Variable not found
            return Task.FromResult(NodeExecutionResult.Failed(new KeyNotFoundException($"Variable '{VariableName}' not found in the execution context.")));
        }
    }
} 