using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Execution.Events;
using Nodify.Workflow.Core.Execution.Runner;
using Nodify.Workflow.Core.Execution;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shouldly;
using Xunit;

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
    public Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context)
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
        public Dictionary<Guid, int> NodeStartingCounts { get; } = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> NodeCompletedCounts { get; } = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> NodeFailedCounts { get; } = new Dictionary<Guid, int>();
        public List<WorkflowExecutionStartedEventArgs> WorkflowStartedEvents { get; } = new();
        public List<NodeExecutionEventArgs> NodeStartingEvents { get; } = new();
        public List<NodeExecutionEventArgs> NodeCompletedEvents { get; } = new();
        public List<NodeExecutionFailedEventArgs> NodeFailedEvents { get; } = new();
        public List<WorkflowExecutionCompletedEventArgs> WorkflowCompletedEvents { get; } = new();
        public List<WorkflowExecutionFailedEventArgs> WorkflowFailedEvents { get; } = new();

        public void Subscribe(WorkflowRunner runner)
        {
            runner.WorkflowStarted += (s, e) => { RecordedEvents.Add(e); WorkflowStartedEvents.Add(e); WorkflowStartedCount++; };
            runner.WorkflowCompleted += (s, e) => { RecordedEvents.Add(e); WorkflowCompletedEvents.Add(e); WorkflowCompletedCount++; };
            runner.WorkflowFailed += (s, e) => { RecordedEvents.Add(e); WorkflowFailedEvents.Add(e); WorkflowFailedCount++; };
            runner.NodeStarting += (s, e) => { RecordedEvents.Add(e); NodeStartingEvents.Add(e); NodeStartingCounts[e.Node.Id] = NodeStartingCounts.GetValueOrDefault(e.Node.Id) + 1; };
            runner.NodeCompleted += (s, e) => { RecordedEvents.Add(e); NodeCompletedEvents.Add(e); NodeCompletedCounts[e.Node.Id] = NodeCompletedCounts.GetValueOrDefault(e.Node.Id) + 1; };
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
        nodeA.ExecuteAsync(Arg.Any<IExecutionContext>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded())); // Configure Success

        var nodeB = Substitute.For<ITestNode>();
        nodeB.Id.Returns(Guid.NewGuid());
        nodeB.ExecuteAsync(Arg.Any<IExecutionContext>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded())); // Configure Success

        mockTraversal.TopologicalSort(nodeA).Returns(new List<INode> { nodeA, nodeB });
        return (nodeA, nodeB);
    }

     private (ITestNode NodeA, FailureNode NodeB, ITestNode NodeC) SetupLinearFailureGraph(Nodify.Workflow.Core.Execution.IGraphTraversal mockTraversal)
    {
        var nodeA = Substitute.For<ITestNode>();
        nodeA.Id.Returns(Guid.NewGuid());
        nodeA.ExecuteAsync(Arg.Any<IExecutionContext>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded())); // Configure Success

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
         nodeA.ExecuteAsync(Arg.Any<IExecutionContext>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded())); // Configure Success

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
        var context = new ExecutionContext();
        var runner = new WorkflowRunner(mockTraversal, new DefaultNodeExecutor());
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
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionEventArgs>();
        ((NodeExecutionEventArgs)recorder.RecordedEvents[1]).Node.Id.ShouldBe(nodeA.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionEventArgs>();
         ((NodeExecutionEventArgs)recorder.RecordedEvents[2]).Node.Id.ShouldBe(nodeA.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionEventArgs>();
         ((NodeExecutionEventArgs)recorder.RecordedEvents[3]).Node.Id.ShouldBe(nodeB.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionEventArgs>();
         ((NodeExecutionEventArgs)recorder.RecordedEvents[4]).Node.Id.ShouldBe(nodeB.Id);
        recorder.RecordedEvents[5].ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();
    }

    [Fact]
    public async Task RunAsync_NodeFailureExecution_RaisesCorrectEvents()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var (nodeA, nodeB, nodeC) = SetupLinearFailureGraph(mockTraversal);
        var context = new ExecutionContext();
        var runner = new WorkflowRunner(mockTraversal, new DefaultNodeExecutor());
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
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionEventArgs>();
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionEventArgs>();
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionEventArgs>();
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionFailedEventArgs>();
        recorder.RecordedEvents[5].ShouldBeOfType<WorkflowExecutionFailedEventArgs>();
    }

     [Fact]
    public async Task RunAsync_EmptyGraphExecution_RaisesStartAndComplete()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var nodeA = SetupEmptyGraph(mockTraversal);
        var context = new ExecutionContext();
        var runner = new WorkflowRunner(mockTraversal, new DefaultNodeExecutor());
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
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionEventArgs>();
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionEventArgs>();
        recorder.RecordedEvents[3].ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();
    }

    [Fact]
    public async Task RunAsync_EventsContainCorrectArguments()
    {
        // Arrange
        var mockTraversal = Substitute.For<Nodify.Workflow.Core.Execution.IGraphTraversal>();
        var (nodeA, nodeB) = SetupLinearSuccessGraph(mockTraversal);
        var context = new ExecutionContext();
        var runner = new WorkflowRunner(mockTraversal, new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(nodeA, context);

        // Assert
        var nodeAStartingArgs = recorder.NodeStartingEvents.FirstOrDefault(e => e.Node.Id == nodeA.Id);
        nodeAStartingArgs.ShouldNotBeNull();
        nodeAStartingArgs.Node.ShouldBeSameAs(nodeA);
        nodeAStartingArgs.Context.ShouldBeSameAs(context);
        var nodeACompletedArgs = recorder.NodeCompletedEvents.FirstOrDefault(e => e.Node.Id == nodeA.Id);
        nodeACompletedArgs.ShouldNotBeNull();
        nodeACompletedArgs.Node.ShouldBeSameAs(nodeA);
        nodeACompletedArgs.Context.ShouldBeSameAs(context);
        var workflowStartedArgs = recorder.WorkflowStartedEvents.FirstOrDefault();
        workflowStartedArgs.ShouldNotBeNull();
        workflowStartedArgs.Context.ShouldBeSameAs(context);
        var workflowCompletedArgs = recorder.WorkflowCompletedEvents.FirstOrDefault();
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
        var context = new ExecutionContext();
        var runner = new WorkflowRunner(mockTraversal, new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(nodeA, context);

        // Assert
        var nodeBFailedArgs = recorder.NodeFailedEvents.FirstOrDefault(e => e.Node.Id == nodeB.Id);
        nodeBFailedArgs.ShouldNotBeNull();
        nodeBFailedArgs.Node.ShouldBeSameAs(nodeB);
        nodeBFailedArgs.Error.ShouldBeOfType<InvalidOperationException>();
        nodeBFailedArgs.Context.ShouldBeSameAs(context);
        var workflowFailedArgs = recorder.WorkflowFailedEvents.FirstOrDefault();
        workflowFailedArgs.ShouldNotBeNull();
        workflowFailedArgs.FailedNode?.ShouldBeSameAs(nodeB);
        workflowFailedArgs.Error.ShouldBeOfType<InvalidOperationException>();
        workflowFailedArgs.Context.ShouldBeSameAs(context);
    }
} 