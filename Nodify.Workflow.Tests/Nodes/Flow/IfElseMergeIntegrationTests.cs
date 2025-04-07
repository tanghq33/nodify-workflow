using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Execution.Events;
using Nodify.Workflow.Core.Execution.Runner;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Logic;
using Nodify.Workflow.Core.Logic.Operators;
using Nodify.Workflow.Nodes.Flow;
using Nodify.Workflow.Nodes.Logic;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Nodify.Workflow.Tests.Nodes.Flow;

public class IfElseMergeIntegrationTests
{
    private readonly INodeExecutor _nodeExecutor;
    private readonly Nodify.Workflow.Core.Execution.IGraphTraversal _graphTraversal;
    private readonly EventRecorder _eventRecorder;
    private readonly WorkflowRunner _runner;
    private readonly Graph _graph;

    // Event recorder helper class (copied from WorkflowExecutionEventsTests)
    private class EventRecorder
    {
        public List<EventArgs> RecordedEvents { get; } = new List<EventArgs>();
        public int WorkflowStartedCount { get; private set; }
        public int WorkflowCompletedCount { get; private set; }
        public int WorkflowFailedCount { get; private set; }
        public int WorkflowCancelledCount { get; private set; }
        public Dictionary<Guid, int> NodeStartingCounts { get; } = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> NodeCompletedCounts { get; } = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> NodeFailedCounts { get; } = new Dictionary<Guid, int>();
        public List<WorkflowExecutionStartedEventArgs> WorkflowStartedEvents { get; } = new();
        public List<NodeExecutionStartingEventArgs> NodeStartingEvents { get; } = new();
        public List<NodeExecutionCompletedEventArgs> NodeCompletedEvents { get; } = new();
        public List<NodeExecutionFailedEventArgs> NodeFailedEvents { get; } = new();
        public List<WorkflowExecutionCompletedEventArgs> WorkflowCompletedEvents { get; } = new();
        public List<WorkflowExecutionFailedEventArgs> WorkflowFailedEvents { get; } = new();
        public List<WorkflowCancelledEventArgs> WorkflowCancelledEvents { get; } = new();

        public void Subscribe(WorkflowRunner runner)
        {
            runner.WorkflowStarted += (s, e) => { RecordedEvents.Add(e); WorkflowStartedEvents.Add(e); WorkflowStartedCount++; };
            runner.WorkflowCompleted += (s, e) => { RecordedEvents.Add(e); WorkflowCompletedEvents.Add(e); WorkflowCompletedCount++; };
            runner.WorkflowFailed += (s, e) => { RecordedEvents.Add(e); WorkflowFailedEvents.Add(e); WorkflowFailedCount++; };
            runner.WorkflowCancelled += (s, e) => { RecordedEvents.Add(e); WorkflowCancelledEvents.Add(e); WorkflowCancelledCount++; };
            runner.NodeStarting += (s, e) => { RecordedEvents.Add(e); if(e is NodeExecutionStartingEventArgs startArgs) NodeStartingEvents.Add(startArgs); NodeStartingCounts[e.Node.Id] = NodeStartingCounts.GetValueOrDefault(e.Node.Id) + 1; };
            runner.NodeCompleted += (s, e) => { RecordedEvents.Add(e); if(e is NodeExecutionCompletedEventArgs completeArgs) NodeCompletedEvents.Add(completeArgs); NodeCompletedCounts[e.Node.Id] = NodeCompletedCounts.GetValueOrDefault(e.Node.Id) + 1; };
            runner.NodeFailed += (s, e) => { RecordedEvents.Add(e); NodeFailedEvents.Add(e); NodeFailedCounts[e.Node.Id] = NodeFailedCounts.GetValueOrDefault(e.Node.Id) + 1; };
        }

        public T GetEventArgs<T>(int index) where T : EventArgs => (T)RecordedEvents[index];
        public IEnumerable<T> GetEventsOfType<T>() where T: EventArgs => RecordedEvents.OfType<T>();
    }

    public IfElseMergeIntegrationTests()
    {
        _nodeExecutor = new DefaultNodeExecutor();
        _graphTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        _eventRecorder = new EventRecorder();
        _graph = new Graph();
        _runner = new WorkflowRunner(_nodeExecutor, _graphTraversal);
        _eventRecorder.Subscribe(_runner);
    }

    [Fact]
    public async Task RunAsync_IfElseTruePathToMerge_ShouldExecuteCorrectNodesInOrder()
    {
        // Arrange
        var startNode = new StartNode();
        var ifElseNode = new IfElseNode
        {
            VariableName = "TestVar",
            Conditions = new List<ConditionRuleBase>
            {
                new EqualityConditionRule 
                { 
                    PropertyPath = string.Empty,
                    Operator = EqualityOperator.Equals,
                    ComparisonValue = true
                }
            }
        };
        var nodeTruePath = Substitute.For<INode>();
        var nodeFalsePath = Substitute.For<INode>();
        var mergeNode = new MergeNode();
        var endNode = new EndNode();

        // Set up mock node IDs
        nodeTruePath.Id.Returns(Guid.NewGuid());
        nodeFalsePath.Id.Returns(Guid.NewGuid());

        // Set up mock node validation
        nodeTruePath.Validate().Returns(true);
        nodeFalsePath.Validate().Returns(true);

        // Set up mock node connectors
        var trueMockInConnector = new Connector(nodeTruePath, ConnectorDirection.Input, typeof(object));
        var trueMockOutConnector = new Connector(nodeTruePath, ConnectorDirection.Output, typeof(object));
        var falseMockInConnector = new Connector(nodeFalsePath, ConnectorDirection.Input, typeof(object));
        var falseMockOutConnector = new Connector(nodeFalsePath, ConnectorDirection.Output, typeof(object));

        nodeTruePath.InputConnectors.Returns(new[] { trueMockInConnector });
        nodeTruePath.OutputConnectors.Returns(new[] { trueMockOutConnector });
        nodeFalsePath.InputConnectors.Returns(new[] { falseMockInConnector });
        nodeFalsePath.OutputConnectors.Returns(new[] { falseMockOutConnector });

        // Add nodes to graph
        _graph.AddNode(startNode);
        _graph.AddNode(ifElseNode);
        _graph.AddNode(nodeTruePath);
        _graph.AddNode(nodeFalsePath);
        _graph.AddNode(mergeNode);
        _graph.AddNode(endNode);

        // Get connectors
        var startOutConnector = startNode.OutputConnectors.First();
        var ifElseInConnector = ifElseNode.InputConnectors.First();
        var ifElseTrueConnector = ifElseNode.OutputConnectors.First(); // First is true path
        var ifElseFalseConnector = ifElseNode.OutputConnectors.Skip(1).First(); // Second is false path
        var mergeInAConnector = mergeNode.InputConnectors.First();
        var mergeInBConnector = mergeNode.InputConnectors.Skip(1).First();
        var mergeOutConnector = mergeNode.OutputConnectors.First();
        var endInConnector = endNode.InputConnectors.First();

        // Connect nodes
        _graph.AddConnection(startOutConnector, ifElseInConnector);
        _graph.AddConnection(ifElseTrueConnector, trueMockInConnector);
        _graph.AddConnection(ifElseFalseConnector, falseMockInConnector);
        _graph.AddConnection(trueMockOutConnector, mergeInAConnector);
        _graph.AddConnection(falseMockOutConnector, mergeInBConnector);
        _graph.AddConnection(mergeOutConnector, endInConnector);

        // Setup mock traversal to return nodes in correct order for true path
        _graphTraversal.TopologicalSort(startNode).Returns(new[]
        {
            startNode,
            ifElseNode,
            nodeTruePath,
            mergeNode,
            endNode
        });

        // Setup mock node behavior
        nodeTruePath.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(async info => 
            {
                await Task.CompletedTask; // Simulate async work
                return NodeExecutionResult.SucceededWithData(trueMockOutConnector.Id, "TruePathData");
            });

        nodeFalsePath.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(async info => 
            {
                await Task.CompletedTask; // Simulate async work
                return NodeExecutionResult.SucceededWithData(falseMockOutConnector.Id, "FalsePathData");
            });

        // Setup execution context with condition value
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        context.SetVariable("TestVar", true);

        // Act
        await _runner.RunAsync(startNode, context, CancellationToken.None);

        // Assert
        context.CurrentStatus.ShouldBe(ExecutionStatus.Completed);
        
        // Verify workflow events
        _eventRecorder.WorkflowStartedCount.ShouldBe(1);
        _eventRecorder.WorkflowCompletedCount.ShouldBe(1);
        _eventRecorder.WorkflowFailedCount.ShouldBe(0);
        _eventRecorder.WorkflowCancelledCount.ShouldBe(0);

        // Verify node events
        _eventRecorder.NodeStartingEvents.Count.ShouldBe(5); // start, if/else, true path, merge, end
        _eventRecorder.NodeCompletedEvents.Count.ShouldBe(5);
        _eventRecorder.NodeFailedEvents.Count.ShouldBe(0);

        // Verify execution order through node starting events
        var nodeStartingEvents = _eventRecorder.NodeStartingEvents;
        nodeStartingEvents[0].Node.ShouldBe(startNode);
        nodeStartingEvents[1].Node.ShouldBe(ifElseNode);
        nodeStartingEvents[2].Node.ShouldBe(nodeTruePath);
        nodeStartingEvents[3].Node.ShouldBe(mergeNode);
        nodeStartingEvents[4].Node.ShouldBe(endNode);

        // Verify false path was not executed
        nodeStartingEvents.ShouldNotContain(e => e.Node == nodeFalsePath);
    }
} 