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
    public string OutputName { get; set; } = string.Empty;

    // Input Connector ID
    private readonly Guid _inputConnectorId;

    public OutputNode()
    {
        // Input Data Connector
        var inputConnector = new Connector(this, ConnectorDirection.Input, typeof(object));
        _inputConnectorId = inputConnector.Id;
        AddInputConnector(inputConnector);
        // No output connectors for this node type
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        // The inputData received is what we want to store.
        if (string.IsNullOrWhiteSpace(OutputName))
        {
            return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException("OutputName property cannot be null or empty.")));
        }

        try
        {
            // Store the received inputData in the context using the specified OutputName
            context.SetVariable(OutputName, inputData);
            
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
        if (string.IsNullOrWhiteSpace(OutputName))
        {
            return false; // Consider it invalid if OutputName is not set
        }

        return true;
    }
} 