using System.Text.Json;
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

[WorkflowNode("Input JSON", "Data", Description = "Parses a JSON string and outputs the resulting object/value.")]
public class InputJsonNode : Node
{
    // Configurable Property
    public string JsonContent { get; set; } = string.Empty;

    // Output Connector ID
    private readonly Guid _outputConnectorId;

    public InputJsonNode()
    {
        // Output Data Connector
        var outputConnector = new Connector(this, ConnectorDirection.Output, typeof(object));
        _outputConnectorId = outputConnector.Id;
        AddOutputConnector(outputConnector);
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        // InputData is ignored by InputJsonNode.
        if (string.IsNullOrWhiteSpace(JsonContent))
        {
            return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException("JsonContent property cannot be null or empty.")));
        }

        try
        {
            // Use System.Text.Json to parse
            // We parse into JsonDocument first to handle any valid JSON (object, array, primitive)
            using JsonDocument document = JsonDocument.Parse(JsonContent);
            
            // The output is the root element itself. We clone it as the JsonDocument will be disposed.
            object outputValue = document.RootElement.Clone(); 

            return Task.FromResult(NodeExecutionResult.SucceededWithData(_outputConnectorId, outputValue));
        }
        catch (JsonException ex)
        {
            // Handle JSON parsing errors
            return Task.FromResult(NodeExecutionResult.Failed(new JsonException($"Failed to parse JsonContent: {ex.Message}", ex)));
        }
        catch (Exception ex)
        {
            // Catch any other unexpected errors during execution
            return Task.FromResult(NodeExecutionResult.Failed(ex));
        }
    }

    public override bool Validate()
    {
        // Base validation + check if JsonContent is set
        if (!base.Validate())
        {
            return false;
        }
        
        // For this node, empty/null JsonContent might be considered invalid for execution
        // We check this during ExecuteAsync, but basic validation could check for null/whitespace.
        // Let's consider it valid if non-whitespace, even if potentially invalid JSON syntax.
        // ExecuteAsync will handle the actual parsing errors.
        // if (string.IsNullOrWhiteSpace(JsonContent))
        // {
        //      return false; 
        // }
        
        // Could potentially try a quick parse here, but might be too expensive for validation.
        return true; 
    }
} 