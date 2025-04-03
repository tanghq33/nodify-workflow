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

[WorkflowNode("Set Variable", "Data", Description = "Sets or updates a variable in the workflow context.")]
public class SetVariableNode : Node
{
    // Removed connector keys as Key property doesn't exist
    // private const string FlowInputConnectorKey = "FlowInput";
    // private const string FlowOutputConnectorKey = "FlowOutput";

    // Configurable Properties
    public string VariableName { get; set; } = string.Empty;
    public object? ValueToSet { get; set; }

    public SetVariableNode()
    {
        // Input Flow Connector (identified by Id)
        AddInputConnector(new Connector(this, ConnectorDirection.Input, typeof(object)));
        
        // Output Flow Connector (identified by Id)
        AddOutputConnector(new Connector(this, ConnectorDirection.Output, typeof(object)));
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(VariableName))
        {
            return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException("VariableName property cannot be empty.")));
        }

        try
        {
            context.SetVariable(VariableName, ValueToSet);
            
            // Find the first (assumed only) output connector and activate it by Id
            var outputConnector = OutputConnectors.FirstOrDefault(); // Using Linq
            if (outputConnector == null)
            {
                 // This should realistically not happen if the constructor adds one
                 return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException("SetVariableNode has no output connector defined.")));
            }
            return Task.FromResult(NodeExecutionResult.Succeeded(outputConnector.Id));
        }
        catch (Exception ex)
        {
            return Task.FromResult(NodeExecutionResult.Failed(ex));
        }
    }
} 