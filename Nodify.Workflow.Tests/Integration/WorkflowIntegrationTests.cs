using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Execution.Runner;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Logic;
using Nodify.Workflow.Core.Logic.Operators;
using Nodify.Workflow.Nodes.Data;
using Nodify.Workflow.Nodes.Flow;
using Nodify.Workflow.Nodes.Logic;
using Shouldly;
using Xunit;

namespace Nodify.Workflow.Tests.Integration;

public class WorkflowIntegrationTests
{
    [Fact]
    public async Task ExecuteWorkflow_WhenInputValueIsGreaterThanThreshold_ShouldReturnGreater()
    {
        // Arrange
        var graph = new Graph();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();

        // Create nodes
        var startNode = new StartNode();
        var setInputNode = new SetVariableNode { VariableName = "myValue", Value = 10 };
        var ifElseNode = new IfElseNode
        {
            VariableName = "myValue"
        };
        var setTrueNode = new SetVariableNode { VariableName = "result", Value = "Greater" };
        var setFalseNode = new SetVariableNode { VariableName = "result", Value = "Not Greater" };
        var mergeNode = new MergeNode();
        var endNode = new EndNode();

        // Add nodes to graph
        graph.AddNode(startNode);
        graph.AddNode(setInputNode);
        graph.AddNode(ifElseNode);
        graph.AddNode(setTrueNode);
        graph.AddNode(setFalseNode);
        graph.AddNode(mergeNode);
        graph.AddNode(endNode);

        // Create connections
        // Start -> SetInput
        graph.AddConnection(startNode.OutputConnectors.First(), setInputNode.InputConnectors.First());
        
        // SetInput -> IfElse
        graph.AddConnection(setInputNode.OutputConnectors.First(), ifElseNode.InputConnectors.First());
        
        // IfElse True -> SetTrue
        graph.AddConnection(ifElseNode.OutputConnectors.First(), setTrueNode.InputConnectors.First());
        
        // IfElse False -> SetFalse
        graph.AddConnection(ifElseNode.OutputConnectors.Skip(1).First(), setFalseNode.InputConnectors.First());
        
        // SetTrue -> Merge
        graph.AddConnection(setTrueNode.OutputConnectors.First(), mergeNode.InputConnectors.First());
        
        // SetFalse -> Merge
        graph.AddConnection(setFalseNode.OutputConnectors.First(), mergeNode.InputConnectors.Skip(1).First());
        
        // Merge -> End
        graph.AddConnection(mergeNode.OutputConnectors.First(), endNode.InputConnectors.First());

        // Configure condition
        ifElseNode.Conditions.Add(new NumericConditionRule { 
            PropertyPath = string.Empty, 
            Operator = NumericOperator.GreaterThan, 
            ComparisonValue = 5 
        });

        // Create workflow runner
        var nodeExecutor = new DefaultNodeExecutor();
        var runner = new WorkflowRunner(nodeExecutor);

        // Act
        await runner.RunAsync(startNode, context);

        // Assert
        context.CurrentStatus.ShouldBe(ExecutionStatus.Completed);
        context.TryGetVariable<string>("result", out var result).ShouldBeTrue();
        result.ShouldBe("Greater");
    }

    [Fact]
    public async Task ExecuteWorkflow_WhenInputValueIsNotGreaterThanThreshold_ShouldReturnNotGreater()
    {
        // Arrange
        var graph = new Graph();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();

        // Create nodes
        var startNode = new StartNode();
        var setInputNode = new SetVariableNode { VariableName = "myValue", Value = 3 };
        var ifElseNode = new IfElseNode
        {
            VariableName = "myValue"
        };
        var setTrueNode = new SetVariableNode { VariableName = "result", Value = "Greater" };
        var setFalseNode = new SetVariableNode { VariableName = "result", Value = "Not Greater" };
        var mergeNode = new MergeNode();
        var endNode = new EndNode();

        // Add nodes to graph
        graph.AddNode(startNode);
        graph.AddNode(setInputNode);
        graph.AddNode(ifElseNode);
        graph.AddNode(setTrueNode);
        graph.AddNode(setFalseNode);
        graph.AddNode(mergeNode);
        graph.AddNode(endNode);

        // Create connections
        // Start -> SetInput
        graph.AddConnection(startNode.OutputConnectors.First(), setInputNode.InputConnectors.First());
        
        // SetInput -> IfElse
        graph.AddConnection(setInputNode.OutputConnectors.First(), ifElseNode.InputConnectors.First());
        
        // IfElse True -> SetTrue
        graph.AddConnection(ifElseNode.OutputConnectors.First(), setTrueNode.InputConnectors.First());
        
        // IfElse False -> SetFalse
        graph.AddConnection(ifElseNode.OutputConnectors.Skip(1).First(), setFalseNode.InputConnectors.First());
        
        // SetTrue -> Merge
        graph.AddConnection(setTrueNode.OutputConnectors.First(), mergeNode.InputConnectors.First());
        
        // SetFalse -> Merge
        graph.AddConnection(setFalseNode.OutputConnectors.First(), mergeNode.InputConnectors.Skip(1).First());
        
        // Merge -> End
        graph.AddConnection(mergeNode.OutputConnectors.First(), endNode.InputConnectors.First());

        // Configure condition
        ifElseNode.Conditions.Add(new NumericConditionRule { 
            PropertyPath = string.Empty, 
            Operator = NumericOperator.GreaterThan, 
            ComparisonValue = 5 
        });

        // Create workflow runner
        var nodeExecutor = new DefaultNodeExecutor();
        var runner = new WorkflowRunner(nodeExecutor);

        // Act
        await runner.RunAsync(startNode, context);

        // Assert
        context.CurrentStatus.ShouldBe(ExecutionStatus.Completed);
        context.TryGetVariable<string>("result", out var result).ShouldBeTrue();
        result.ShouldBe("Not Greater");
    }
} 