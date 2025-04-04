using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Execution.Events;
using Nodify.Workflow.Core.Execution.Runner;
using Nodify.Workflow.Core.Execution;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shouldly;
using Xunit;
using Nodify.Workflow.Tests.Core.Models.Helpers; // Add for TestNode

namespace Nodify.Workflow.Tests.Core.Execution;

// === Mock Nodes ===
public interface ITestNode : INode { }

internal class FailureNode : ITestNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public IReadOnlyCollection<IConnector> InputConnectors { get; set; } = new List<IConnector>().AsReadOnly();
    public IReadOnlyCollection<IConnector> OutputConnectors { get; set; } = new List<IConnector>().AsReadOnly();
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public void AddInputConnector(IConnector connector) { }
    public void AddOutputConnector(IConnector connector) { }
    public void RemoveInputConnector(IConnector connector) { }
    public void RemoveOutputConnector(IConnector connector) { }
    public IConnector? GetInputConnector(Guid id) => null;
    public IConnector? GetOutputConnector(Guid id) => null;
    public bool RemoveConnector(IConnector connector) => false;
    public bool Validate() => true;

    // Implemented ExecuteAsync for FailureNode
    public Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        var error = new InvalidOperationException($"Simulated failure in node {Id} ({GetType().Name}).");
        return Task.FromResult(NodeExecutionResult.Failed(error));
    }
}

public class WorkflowExecutionEventsTests
{
    // === Event Recorder Helper ===
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

    // === Test Graph Setup Helpers ===
    private (ITestNode NodeA, ITestNode NodeB) SetupLinearSuccessGraph(Nodify.Workflow.Core.Execution.IGraphTraversal mockTraversal)
    {
        var nodeA = Substitute.For<ITestNode>();
        nodeA.Id.Returns(Guid.NewGuid());
        nodeA.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded()));

        var nodeB = Substitute.For<ITestNode>();
        nodeB.Id.Returns(Guid.NewGuid());
        nodeB.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded()));

        mockTraversal.TopologicalSort(nodeA).Returns(new List<INode> { nodeA, nodeB });
        return (nodeA, nodeB);
    }

     private (ITestNode NodeA, FailureNode NodeB, ITestNode NodeC) SetupLinearFailureGraph(Nodify.Workflow.Core.Execution.IGraphTraversal mockTraversal)
    {
        var nodeA = Substitute.For<ITestNode>();
        nodeA.Id.Returns(Guid.NewGuid());
        nodeA.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded()));

        var nodeB = new FailureNode(); // FailureNode implements its own ExecuteAsync

        var nodeC = Substitute.For<ITestNode>();
        nodeC.Id.Returns(Guid.NewGuid());
        // nodeC's ExecuteAsync doesn't need configuration as it shouldn't be called

        mockTraversal.TopologicalSort(nodeA).Returns(new List<INode> { nodeA, nodeB, nodeC });
        return (nodeA, nodeB, nodeC);
    }

     private INode SetupEmptyGraph(Nodify.Workflow.Core.Execution.IGraphTraversal mockTraversal)
     {
         var nodeA = Substitute.For<ITestNode>();
         nodeA.Id.Returns(Guid.NewGuid());
         nodeA.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded()));

         mockTraversal.TopologicalSort(nodeA).Returns(new List<INode> { nodeA });
         return nodeA;
     }

    // === Test Scenarios ===
    [Fact]
    public async Task RunAsync_SuccessfulLinearExecution_RaisesCorrectEventsInOrder()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var (nodeA, nodeB) = SetupLinearSuccessGraph(mockTraversal);
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(nodeA, context);

        // Assert
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(nodeA.Id).ShouldBe(0);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeB.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(nodeB.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(nodeB.Id).ShouldBe(0);
        recorder.WorkflowCompletedCount.ShouldBe(1);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.RecordedEvents.Count.ShouldBe(6);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>();
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>();
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>();
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionCompletedEventArgs>();
        recorder.RecordedEvents[5].ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();
    }

    [Fact]
    public async Task RunAsync_NodeFailureExecution_RaisesCorrectEvents()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var (nodeA, nodeB, nodeC) = SetupLinearFailureGraph(mockTraversal);
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(nodeA, context);

        // Assert
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeB.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(nodeB.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(nodeB.Id).ShouldBe(0);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeC.Id).ShouldBe(0);
        recorder.NodeCompletedCounts.GetValueOrDefault(nodeC.Id).ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.RecordedEvents.Count.ShouldBe(6);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(nodeA.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(nodeA.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(nodeB.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionFailedEventArgs>().Node.Id.ShouldBe(nodeB.Id);
        recorder.RecordedEvents[5].ShouldBeOfType<WorkflowExecutionFailedEventArgs>();
    }

     [Fact]
    public async Task RunAsync_EmptyGraphExecution_RaisesStartAndComplete()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var nodeA = SetupEmptyGraph(mockTraversal);
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(nodeA, context);

        // Assert
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(1);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.NodeStartingCounts.Count.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.Count.ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeFailedCounts.Count.ShouldBe(0);
        recorder.RecordedEvents.Count.ShouldBe(4);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>();
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>();
        recorder.RecordedEvents[3].ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();
    }

    [Fact]
    public async Task RunAsync_EventsContainCorrectArguments()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var (nodeA, nodeB) = SetupLinearSuccessGraph(mockTraversal);
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(nodeA, context);

        // Assert
        var nodeAStartingArgs = recorder.GetEventsOfType<NodeExecutionStartingEventArgs>().FirstOrDefault(e => e.Node.Id == nodeA.Id);
        nodeAStartingArgs.ShouldNotBeNull();
        nodeAStartingArgs.Node.ShouldBeSameAs(nodeA);
        nodeAStartingArgs.Context.ShouldBeSameAs(context);
        var nodeACompletedArgs = recorder.GetEventsOfType<NodeExecutionCompletedEventArgs>().FirstOrDefault(e => e.Node.Id == nodeA.Id);
        nodeACompletedArgs.ShouldNotBeNull();
        nodeACompletedArgs.Node.ShouldBeSameAs(nodeA);
        nodeACompletedArgs.Context.ShouldBeSameAs(context);
        var workflowStartedArgs = recorder.GetEventsOfType<WorkflowExecutionStartedEventArgs>().FirstOrDefault();
        workflowStartedArgs.ShouldNotBeNull();
        workflowStartedArgs.Context.ShouldBeSameAs(context);
        var workflowCompletedArgs = recorder.GetEventsOfType<WorkflowExecutionCompletedEventArgs>().FirstOrDefault();
        workflowCompletedArgs.ShouldNotBeNull();
        workflowCompletedArgs.Context.ShouldBeSameAs(context);
        workflowCompletedArgs.FinalStatus.ShouldBe(ExecutionStatus.Completed);
    }

     [Fact]
    public async Task RunAsync_FailureEventContainsExceptionAndNode()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var (nodeA, nodeB, nodeC) = SetupLinearFailureGraph(mockTraversal);
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(nodeA, context);

        // Assert
        var nodeBFailedArgs = recorder.GetEventsOfType<NodeExecutionFailedEventArgs>().FirstOrDefault(e => e.Node.Id == nodeB.Id);
        nodeBFailedArgs.ShouldNotBeNull();
        nodeBFailedArgs.Node.ShouldBeSameAs(nodeB);
        nodeBFailedArgs.Error.ShouldBeOfType<InvalidOperationException>();
        nodeBFailedArgs.Context.ShouldBeSameAs(context);
        var workflowFailedArgs = recorder.GetEventsOfType<WorkflowExecutionFailedEventArgs>().FirstOrDefault();
        workflowFailedArgs.ShouldNotBeNull();
        workflowFailedArgs.FailedNode?.ShouldBeSameAs(nodeB);
        workflowFailedArgs.Error.ShouldBeOfType<InvalidOperationException>();
        workflowFailedArgs.Context.ShouldBeSameAs(context);
    }

    [Fact]
    public async Task RunAsync_SingleAsyncNodeSuccess_RaisesCorrectEventsInOrder()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var recorder = new EventRecorder();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var startNode = new TestNode(); // Create nodes specific to test
        var endNode = new TestNode();
        recorder.Subscribe(runner);

        var asyncNode = new AsyncTestNode("async-1", TimeSpan.FromMilliseconds(50));
        var nodes = new List<INode> { startNode, asyncNode, endNode };
        mockTraversal.TopologicalSort(startNode).Returns(nodes);

        // Act
        await runner.RunAsync(startNode, context);

        // Assert
        recorder.RecordedEvents.Count.ShouldBe(8);
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(1);
        recorder.WorkflowFailedCount.ShouldBe(0);

        recorder.NodeStartingCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(startNode.Id, 0).ShouldBe(0);

        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(asyncNode.Id, 0).ShouldBe(0);

        recorder.NodeStartingCounts.GetValueOrDefault(endNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(endNode.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(endNode.Id, 0).ShouldBe(0);

        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(asyncNode.Id);
        recorder.RecordedEvents[5].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(endNode.Id);
        recorder.RecordedEvents[6].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(endNode.Id);
        recorder.RecordedEvents[7].ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();

        context.TryGetVariable<bool>("async-1_FinishedDelay", out var finished).ShouldBeTrue();
        finished.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_SingleAsyncNodeFailureException_RaisesCorrectEvents()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var recorder = new EventRecorder();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var startNode = new TestNode();
        recorder.Subscribe(runner);

        var exceptionToThrow = new SimulatedAsyncException("Async failure via exception");
        var failingAsyncNode = new AsyncTestNode(
            id: "async-fail-ex", delay: TimeSpan.FromMilliseconds(50),
            shouldSucceed: false, throwException: true, exceptionToUse: exceptionToThrow);
        var shouldNotRunNode = new TestNode();
        var nodes = new List<INode> { startNode, failingAsyncNode, shouldNotRunNode };
        mockTraversal.TopologicalSort(startNode).Returns(nodes);

        // Act
        await runner.RunAsync(startNode, context);

        // Assert
        recorder.RecordedEvents.Count.ShouldBe(6);
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(startNode.Id, 0).ShouldBe(0);
        recorder.NodeStartingCounts.GetValueOrDefault(failingAsyncNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(failingAsyncNode.Id, 0).ShouldBe(0);
        recorder.NodeFailedCounts.GetValueOrDefault(failingAsyncNode.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(shouldNotRunNode.Id, 0).ShouldBe(0);
        recorder.NodeCompletedCounts.GetValueOrDefault(shouldNotRunNode.Id, 0).ShouldBe(0);
        recorder.NodeFailedCounts.GetValueOrDefault(shouldNotRunNode.Id, 0).ShouldBe(0);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(failingAsyncNode.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionFailedEventArgs>().Node.Id.ShouldBe(failingAsyncNode.Id);
        recorder.RecordedEvents[5].ShouldBeOfType<WorkflowExecutionFailedEventArgs>();
        var nodeFailedArgs = recorder.GetEventsOfType<NodeExecutionFailedEventArgs>().First(e => e.Node.Id == failingAsyncNode.Id);
        nodeFailedArgs.Node.ShouldBeSameAs(failingAsyncNode);
        nodeFailedArgs.Error.ShouldBeSameAs(exceptionToThrow);
        nodeFailedArgs.Context.ShouldBeSameAs(context);
        var workflowFailedArgs = recorder.GetEventsOfType<WorkflowExecutionFailedEventArgs>().First();
        workflowFailedArgs.FailedNode.ShouldBeSameAs(failingAsyncNode);
        workflowFailedArgs.Error.ShouldBeSameAs(exceptionToThrow);
        workflowFailedArgs.Context.ShouldBeSameAs(context);
    }

    [Fact]
    public async Task RunAsync_SingleAsyncNodeFailureResult_RaisesCorrectEvents()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var recorder = new EventRecorder();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var startNode = new TestNode();
        recorder.Subscribe(runner);

        var exceptionToReturn = new SimulatedAsyncException("Async failure via result");
        var failingAsyncNode = new AsyncTestNode(
            id: "async-fail-res", delay: TimeSpan.FromMilliseconds(50),
            shouldSucceed: false, throwException: false, exceptionToUse: exceptionToReturn);
        var shouldNotRunNode = new TestNode();
        var nodes = new List<INode> { startNode, failingAsyncNode, shouldNotRunNode };
        mockTraversal.TopologicalSort(startNode).Returns(nodes);

        // Act
        await runner.RunAsync(startNode, context);

        // Assert
        recorder.RecordedEvents.Count.ShouldBe(6);
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(startNode.Id, 0).ShouldBe(0);
        recorder.NodeStartingCounts.GetValueOrDefault(failingAsyncNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(failingAsyncNode.Id, 0).ShouldBe(0);
        recorder.NodeFailedCounts.GetValueOrDefault(failingAsyncNode.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(shouldNotRunNode.Id, 0).ShouldBe(0);
        recorder.NodeCompletedCounts.GetValueOrDefault(shouldNotRunNode.Id, 0).ShouldBe(0);
        recorder.NodeFailedCounts.GetValueOrDefault(shouldNotRunNode.Id, 0).ShouldBe(0);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(failingAsyncNode.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionFailedEventArgs>().Node.Id.ShouldBe(failingAsyncNode.Id);
        recorder.RecordedEvents[5].ShouldBeOfType<WorkflowExecutionFailedEventArgs>();
        var nodeFailedArgs = recorder.GetEventsOfType<NodeExecutionFailedEventArgs>().First(e => e.Node.Id == failingAsyncNode.Id);
        nodeFailedArgs.Node.ShouldBeSameAs(failingAsyncNode);
        nodeFailedArgs.Error.ShouldBeSameAs(exceptionToReturn);
        nodeFailedArgs.Context.ShouldBeSameAs(context);
        var workflowFailedArgs = recorder.GetEventsOfType<WorkflowExecutionFailedEventArgs>().First();
        workflowFailedArgs.FailedNode.ShouldBeSameAs(failingAsyncNode);
        workflowFailedArgs.Error.ShouldBeSameAs(exceptionToReturn);
        workflowFailedArgs.Context.ShouldBeSameAs(context);
    }

    [Fact]
    public async Task RunAsync_MultipleSequentialAsyncNodes_RaisesCorrectEventsInOrder()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var recorder = new EventRecorder();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var startNode = new TestNode();
        var endNode = new TestNode();
        recorder.Subscribe(runner);

        var asyncNode1 = new AsyncTestNode("async-seq-1", TimeSpan.FromMilliseconds(50));
        var asyncNode2 = new AsyncTestNode("async-seq-2", TimeSpan.FromMilliseconds(75));
        var nodes = new List<INode> { startNode, asyncNode1, asyncNode2, endNode };
        mockTraversal.TopologicalSort(startNode).Returns(nodes);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await runner.RunAsync(startNode, context);
        stopwatch.Stop();

        // Assert
        recorder.RecordedEvents.Count.ShouldBe(10);
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(1);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.NodeStartingCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(endNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(endNode.Id).ShouldBe(1);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[5].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode2.Id);
        recorder.RecordedEvents[6].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(asyncNode2.Id);
        recorder.RecordedEvents[7].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(endNode.Id);
        recorder.RecordedEvents[8].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(endNode.Id);
        recorder.RecordedEvents[9].ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();
        stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(125);
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000);
    }

    [Fact]
    public async Task RunAsync_CancellationBeforeStart_ShouldNotRunAndSetCancelledStatus()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var recorder = new EventRecorder();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var startNode = new TestNode();
        var middleNode = new TestNode(); // Keep nodes local to test
        var endNode = new TestNode();
        recorder.Subscribe(runner);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        var cancelledToken = cts.Token;

        mockTraversal.TopologicalSort(startNode).Returns(new List<INode> { startNode, middleNode, endNode });

        // Act - No exception should escape RunAsync
        await runner.RunAsync(startNode, context, cancelledToken);

        // Assert - Updated assertions
        recorder.RecordedEvents.Count.ShouldBe(0);
        recorder.WorkflowStartedCount.ShouldBe(0);
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.WorkflowCancelledCount.ShouldBe(0); // WorkflowCancelled is not raised if RunAsync returns early

        recorder.NodeStartingCounts.Count.ShouldBe(0);
        recorder.NodeCompletedCounts.Count.ShouldBe(0);
        recorder.NodeFailedCounts.Count.ShouldBe(0);

        // Verify final status is Cancelled
        context.CurrentStatus.ShouldBe(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Cancelled);
    }

    [Fact]
    public async Task RunAsync_CancellationDuringAsyncNodeDelay_ShouldStopAndSetCancelledStatus()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var recorder = new EventRecorder();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var startNode = new TestNode();
        var endNode = new TestNode(); // Should not run
        recorder.Subscribe(runner);

        // Use a node that delays long enough to be cancelled during the delay
        var longAsyncNode = new AsyncTestNode("long-async", TimeSpan.FromMilliseconds(500));
        var nodes = new List<INode> { startNode, longAsyncNode, endNode };
        mockTraversal.TopologicalSort(startNode).Returns(nodes);

        var cts = new CancellationTokenSource();

        // Act
        // Start the task but don't await it yet
        var runTask = runner.RunAsync(startNode, context, cts.Token);

        // Wait long enough for the start node to complete and the async node to start delaying
        await Task.Delay(100); // Adjust if needed, must be < longAsyncNode delay

        // Cancel while the async node is (likely) in its Task.Delay
        cts.Cancel();

        // Now await the task - it should complete quickly due to cancellation
        // Use try/catch as the OCE might propagate depending on exact timing and runner implementation
        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            // Expected behaviour if OCE propagates
        }

        // Assert
        // Workflow starts, start node runs, async node starts but doesn't complete
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(longAsyncNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(longAsyncNode.Id, 0).ShouldBe(0); // Did not complete
        recorder.NodeFailedCounts.GetValueOrDefault(longAsyncNode.Id, 0).ShouldBe(0); // Was cancelled, not failed
        recorder.NodeStartingCounts.GetValueOrDefault(endNode.Id, 0).ShouldBe(0); // Did not start
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.WorkflowCancelledCount.ShouldBe(1);

        recorder.RecordedEvents.Count.ShouldBe(5); // StartWF, StartS, StartC, AsyncS, CancelWF
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(longAsyncNode.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<WorkflowCancelledEventArgs>();

        // Verify final status is Cancelled
        context.CurrentStatus.ShouldBe(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Cancelled);
    }

    [Fact]
    public async Task RunAsync_CancellationBetweenNodes_ShouldStopAndSetCancelledStatus()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var recorder = new EventRecorder();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var startNode = new TestNode();
        var endNode = new TestNode(); // Should not run
        recorder.Subscribe(runner);

        // Use two async nodes
        var asyncNode1 = new AsyncTestNode("async-between-1", TimeSpan.FromMilliseconds(50));
        var asyncNode2 = new AsyncTestNode("async-between-2", TimeSpan.FromMilliseconds(200)); // Longer delay
        var nodes = new List<INode> { startNode, asyncNode1, asyncNode2, endNode };
        mockTraversal.TopologicalSort(startNode).Returns(nodes);

        var cts = new CancellationTokenSource();

        // Act
        // Start the task but don't await it yet
        var runTask = runner.RunAsync(startNode, context, cts.Token);

        // Wait long enough for asyncNode1 to complete, and asyncNode2 to start delaying
        await Task.Delay(100); // > asyncNode1 delay, < asyncNode2 delay

        // Cancel while asyncNode2 is (likely) in its Task.Delay
        cts.Cancel();

        // Await the task
        try { await runTask; } catch (OperationCanceledException) { /* Expected */ }

        // Assert
        // Workflow starts, start node runs, asyncNode1 runs, asyncNode2 starts but is cancelled
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(1); // Started
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode2.Id, 0).ShouldBe(0); // Did not complete
        recorder.NodeFailedCounts.GetValueOrDefault(asyncNode2.Id, 0).ShouldBe(0); // Cancelled, not failed
        recorder.NodeStartingCounts.GetValueOrDefault(endNode.Id, 0).ShouldBe(0); // Did not start
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.WorkflowCancelledCount.ShouldBe(1);

        // StartWF, StartS, StartC, Async1S, Async1C, Async2S, CancelWF
        recorder.RecordedEvents.Count.ShouldBe(7);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(startNode.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[5].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode2.Id);
        recorder.RecordedEvents[6].ShouldBeOfType<WorkflowCancelledEventArgs>();

        // Verify final status is Cancelled
        context.CurrentStatus.ShouldBe(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Cancelled);
    }

    [Fact]
    public async Task RunAsync_NoCancellation_ShouldCompleteNormally()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var recorder = new EventRecorder();
        var runner = new WorkflowRunner(new DefaultNodeExecutor(), mockTraversal);
        var startNode = new TestNode();
        var endNode = new TestNode();
        recorder.Subscribe(runner);

        var asyncNode1 = new AsyncTestNode("normal-async-1", TimeSpan.FromMilliseconds(20));
        var asyncNode2 = new AsyncTestNode("normal-async-2", TimeSpan.FromMilliseconds(30));
        var nodes = new List<INode> { startNode, asyncNode1, asyncNode2, endNode };
        mockTraversal.TopologicalSort(startNode).Returns(nodes);

        // Act - Use default token (CancellationToken.None)
        await runner.RunAsync(startNode, context); // Or pass CancellationToken.None explicitly

        // Assert - Normal completion
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(1);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.WorkflowCancelledCount.ShouldBe(0);

        recorder.NodeStartingCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(startNode.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(endNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(endNode.Id).ShouldBe(1);

        recorder.RecordedEvents.Count.ShouldBe(10); // Same as multiple sequential async test
        recorder.RecordedEvents.Last().ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();

        context.CurrentStatus.ShouldBe(Nodify.Workflow.Core.Execution.Context.ExecutionStatus.Completed);
    }
} 