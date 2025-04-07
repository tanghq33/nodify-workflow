using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Registry;

namespace Nodify.Workflow.Nodes.Data;

[WorkflowNode("Output Data", "Data", Description = "Captures input data and stores it in a specified context variable.")]
public class OutputNode : Node
{
    // Configurable Property: Name of the variable to store the result in.
    public string VariableName { get; set; } = string.Empty;

    // Input Connector ID
    private readonly Guid _inputConnectorId;

    public OutputNode() : base()
    {
        // Input Data Connector
        var inputConnector = new Connector(this, ConnectorDirection.Input, typeof(object));
        _inputConnectorId = inputConnector.Id;
        AddInputConnector(inputConnector);
        // No output connectors for this node type
    }

    public OutputNode(Guid id) : base(id)
    {
        // Input Data Connector with a specific ID for deserialization
        var inputConnector = new Connector(this, ConnectorDirection.Input, typeof(object), Guid.Parse("87654321-4321-4321-4321-987654321098"));
        _inputConnectorId = inputConnector.Id;
        AddInputConnector(inputConnector);
        // No output connectors for this node type
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        // The inputData received is what we want to store.
        if (string.IsNullOrWhiteSpace(VariableName))
        {
            return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException("VariableName property cannot be null or empty.")));
        }

        try
        {
            // Store the received inputData in the context using the specified VariableName
            context.SetVariable(VariableName, inputData);
            
            // OutputNode represents an end point for data flow, so it doesn't activate any further output connectors.
            // We return success without an ActivatedOutputConnectorId.
            return Task.FromResult(NodeExecutionResult.Succeeded()); 
        }
        catch (Exception ex)
        {
            // Catch potential errors during context.SetVariable or other issues
            return Task.FromResult(NodeExecutionResult.Failed(ex));
        }
    }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }
        
        // Essential property for execution must be set
        if (string.IsNullOrWhiteSpace(VariableName))
        {
            return false; // Consider it invalid if VariableName is not set
        }

        return true;
    }
} 