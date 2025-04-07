using System; // Added for Console
using System.Linq;
using System.Text.Json; // Added for JsonElement
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Execution.Runner;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Logic;
using Nodify.Workflow.Core.Logic.Operators;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Nodes.Data;
using Nodify.Workflow.Nodes.Flow; // For StartNode, EndNode
using Nodify.Workflow.Nodes.Logic;
using Shouldly;
using Xunit;

namespace Nodify.Workflow.Tests.Integration;

public class NodeIntegrationTests
{
    private WorkflowRunner CreateRunner()
    {
        var nodeExecutor = new DefaultNodeExecutor();
        return new WorkflowRunner(nodeExecutor);
    }

    private IConnector GetOutputConnector(INode node, int index = 0)
    {
        // Helper assumes connector order is predictable
        return node.OutputConnectors.ElementAt(index);
    }

    private IConnector GetInputConnector(INode node, int index = 0)
    {
        // Helper assumes connector order is predictable
        return node.InputConnectors.ElementAt(index);
    }

    // Test Scenario 1: InputJson -> Output (Data Flow)
    [Fact]
    public async Task RunAsync_InputJsonToOutput_ShouldStoreParsedJsonInContext()
    {
        // Arrange
        var graph = new Graph();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = CreateRunner();
        
        var inputJsonNode = new InputJsonNode { JsonContent = "{\"id\": 1, \"msg\": \"Success\"}" };
        var outputNode = new OutputNode { OutputName = "ParsedJson" };
        
        graph.AddNode(inputJsonNode);
        graph.AddNode(outputNode);

        // Connect: InputJson (Data Out) -> Output (Data In)
        graph.AddConnection(GetOutputConnector(inputJsonNode), GetInputConnector(outputNode));
        
        // Act: Run the workflow starting from the InputJsonNode
        await runner.RunAsync(inputJsonNode, context);
        
        // Assert
        context.CurrentStatus.ShouldBe(ExecutionStatus.Completed);
        context.TryGetVariable<JsonElement>("ParsedJson", out var parsedJson).ShouldBeTrue();
        parsedJson.ValueKind.ShouldBe(JsonValueKind.Object);
        parsedJson.GetProperty("id").GetInt32().ShouldBe(1);
        parsedJson.GetProperty("msg").GetString().ShouldBe("Success");
    }

    // Test Scenario 2: InputJson -> Output(PrePopulate) || Start -> IfElse -> SetVar -> End (Conditional Flow)
    [Fact]
    public async Task RunAsync_ConditionalFlowWithJsonInput_ShouldExecuteCorrectPath()
    {
        // --- Part 1: Pre-populate context using InputJson -> Output --- 
        var prepGraph = new Graph();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = CreateRunner();
        var initialInputNode = new InputJsonNode { JsonContent = "{\"status\": \"active\", \"count\": 5}" };
        var prepOutputNode = new OutputNode { OutputName = "MyJsonData" };
        
        prepGraph.AddNode(initialInputNode);
        prepGraph.AddNode(prepOutputNode);
        prepGraph.AddConnection(GetOutputConnector(initialInputNode), GetInputConnector(prepOutputNode));
        await runner.RunAsync(initialInputNode, context); 

        // Verify context pre-population for Part 1
        context.TryGetVariable<JsonElement>("MyJsonData", out _).ShouldBeTrue("Context should contain MyJsonData after prep run (Part 1)");

        // --- Part 2: Run the conditional flow using the pre-populated context --- 
        var mainGraph = new Graph(); 
        var startNode = new StartNode();
        var ifElseNode = new IfElseNode 
        {
             InputVariableName = "MyJsonData",
             Conditions = new System.Collections.Generic.List<ConditionRuleBase>
             {
                 new StringConditionRule { PropertyPath = "status", Operator = StringOperator.Equals, ComparisonValue = "active" }
             }
        };
        var setTrueNode = new SetVariableNode { VariableName = "PathResult", Value = "TRUE" };
        var setFalseNode = new SetVariableNode { VariableName = "PathResult", Value = "FALSE" };
        var endTrueNode = new EndNode();
        var endFalseNode = new EndNode();

        mainGraph.AddNode(startNode);
        mainGraph.AddNode(ifElseNode);
        mainGraph.AddNode(setTrueNode);
        mainGraph.AddNode(setFalseNode);
        mainGraph.AddNode(endTrueNode);
        mainGraph.AddNode(endFalseNode);

        // Connections: Start -> IfElse -> (True) -> SetTrue -> EndTrue / (False) -> SetFalse -> EndFalse
        mainGraph.AddConnection(GetOutputConnector(startNode), GetInputConnector(ifElseNode));
        mainGraph.AddConnection(GetOutputConnector(ifElseNode, 0), GetInputConnector(setTrueNode)); // True path
        mainGraph.AddConnection(GetOutputConnector(ifElseNode, 1), GetInputConnector(setFalseNode)); // False path
        mainGraph.AddConnection(GetOutputConnector(setTrueNode), GetInputConnector(endTrueNode));
        mainGraph.AddConnection(GetOutputConnector(setFalseNode), GetInputConnector(endFalseNode));

        // Act: Run the main conditional flow
        await runner.RunAsync(startNode, context);

        // Assert: True path taken
        context.CurrentStatus.ShouldBe(ExecutionStatus.Completed, "Workflow should complete for true path");
        context.TryGetVariable<string>("PathResult", out var pathResultTrue).ShouldBeTrue();
        pathResultTrue.ShouldBe("TRUE");

        // --- Part 3: Test False Path --- 
        context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext(); // Reset context
        initialInputNode = new InputJsonNode { JsonContent = "{\"status\": \"inactive\", \"count\": 5}" }; // Change status
        prepOutputNode = new OutputNode { OutputName = "MyJsonData" }; // Re-use config, new instance not strictly needed but clear
        
        // Pre-populate context again with 'inactive' status
        prepGraph = new Graph(); // Recreate graph for clarity
        prepGraph.AddNode(initialInputNode);
        prepGraph.AddNode(prepOutputNode);
        prepGraph.AddConnection(GetOutputConnector(initialInputNode), GetInputConnector(prepOutputNode));
        await runner.RunAsync(initialInputNode, context);
        
        // Add Debugging:
        var storedValue = context.GetVariable("MyJsonData");
        Console.WriteLine($"[DEBUG] Part 3 - Stored value type for MyJsonData: {storedValue?.GetType().FullName ?? "null"}");
        if (storedValue is JsonElement storedElement)
        {
            Console.WriteLine($"[DEBUG] Part 3 - Stored JsonElement Kind: {storedElement.ValueKind}");
            Console.WriteLine($"[DEBUG] Part 3 - Stored JsonElement RawText: {storedElement.GetRawText()}");
        }
        else if (storedValue != null)
        {
             Console.WriteLine($"[DEBUG] Part 3 - Stored value ToString: {storedValue}");
        }
        
        context.TryGetVariable<JsonElement>("MyJsonData", out var retrievedElement).ShouldBeTrue("Context should contain MyJsonData as JsonElement after inactive prep run"); 

        // Act: Run the main conditional flow again with the new context
        // Re-use mainGraph structure, runner, startNode
        await runner.RunAsync(startNode, context); 

        // Assert: False path taken
        context.CurrentStatus.ShouldBe(ExecutionStatus.Completed, "Workflow should complete for false path");
        context.TryGetVariable<string>("PathResult", out var pathResultFalse).ShouldBeTrue();
        pathResultFalse.ShouldBe("FALSE");
    }
}
