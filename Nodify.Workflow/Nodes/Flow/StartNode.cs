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

[WorkflowNode("Start", "Flow Control", Description = "The starting point of the workflow.")]
public class StartNode : Node
{
    // Removed constant as Name is not on Connector
    // public const string OutputConnectorName = "Start"; 

    public StartNode()
    {
        AddOutputConnector(new Connector(this, ConnectorDirection.Output, typeof(object)));
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        var outputConnector = OutputConnectors.FirstOrDefault(); 
        if (outputConnector != null)
        {
            return Task.FromResult(NodeExecutionResult.Succeeded(outputConnector.Id));
        }
        else
        {
            // Wrap the error message in an exception
            return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException("Start node has no output connector.")));
        }
    }
} 