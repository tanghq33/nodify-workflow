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
using Nodify.Workflow.Core.Models; // Added for Connector and Connection
using System.Reflection; // Added for reflection hack

namespace Nodify.Workflow.Tests.Core.Execution;

// === Mock Nodes ===
public interface ITestNode : INode { }

internal class FailureNode : ITestNode
{
    public Guid Id { get; } = Guid.NewGuid();
    private List<IConnector> _inputConnectors = new();
    private List<IConnector> _outputConnectors = new();
    public IReadOnlyCollection<IConnector> InputConnectors { get => _inputConnectors.AsReadOnly(); set => _inputConnectors = value.ToList(); }
    public IReadOnlyCollection<IConnector> OutputConnectors { get => _outputConnectors.AsReadOnly(); set => _outputConnectors = value.ToList(); }
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public void AddInputConnector(IConnector connector) => _inputConnectors.Add(connector);
    public void AddOutputConnector(IConnector connector) => _outputConnectors.Add(connector);
    public void RemoveInputConnector(IConnector connector) => _inputConnectors.Remove(connector);
    public void RemoveOutputConnector(IConnector connector) => _outputConnectors.Remove(connector);
    public IConnector? GetInputConnector(Guid id) => _inputConnectors.FirstOrDefault(c => c.Id == id);
    public IConnector? GetOutputConnector(Guid id) => _outputConnectors.FirstOrDefault(c => c.Id == id);
    public bool RemoveConnector(IConnector connector) => _inputConnectors.Remove(connector) || _outputConnectors.Remove(connector);
    public bool Validate() => true;

    public Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        var error = new InvalidOperationException($"Simulated failure in node {Id} ({GetType().Name}).");
        return Task.FromResult(NodeExecutionResult.Failed(error));
    }
}

// === Async Test Nodes ===
internal class AsyncTestNode : ITestNode
{
    public Guid Id { get; } = Guid.NewGuid();
    private List<IConnector> _inputConnectors = new();
    private List<IConnector> _outputConnectors = new();
    public IReadOnlyCollection<IConnector> InputConnectors { get => _inputConnectors.AsReadOnly(); set => _inputConnectors = value.ToList(); }
    public IReadOnlyCollection<IConnector> OutputConnectors { get => _outputConnectors.AsReadOnly(); set => _outputConnectors = value.ToList(); }
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    private readonly TimeSpan nodeDelay;
    private readonly bool _shouldFail;
    private readonly bool _failWithException;
    public Guid? OutputConnectorIdToActivate { get; set; } = null;

    public AsyncTestNode(TimeSpan? delay = null, bool shouldFail = false, bool failWithException = false)
    {
        nodeDelay = delay ?? TimeSpan.FromMilliseconds(10);
        _shouldFail = shouldFail;
        _failWithException = failWithException;
        
        // Add a default output connector automatically
        AddOutputConnector(new Connector(this, ConnectorDirection.Output, typeof(object)));
    }
    
    public void AddInputConnector(IConnector connector) => _inputConnectors.Add(connector);
    public void AddOutputConnector(IConnector connector) => _outputConnectors.Add(connector);
    public void RemoveInputConnector(IConnector connector) => _inputConnectors.Remove(connector);
    public void RemoveOutputConnector(IConnector connector) => _outputConnectors.Remove(connector);
    public IConnector? GetInputConnector(Guid id) => _inputConnectors.FirstOrDefault(c => c.Id == id);
    public IConnector? GetOutputConnector(Guid id) => _outputConnectors.FirstOrDefault(c => c.Id == id);
    public bool RemoveConnector(IConnector connector) => _inputConnectors.Remove(connector) || _outputConnectors.Remove(connector);
    public bool Validate() => true;

    public async Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(nodeDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        if (_shouldFail)
        {
            var error = new InvalidOperationException($"Simulated async failure in node {Id}.");
            if (_failWithException) throw error;
            return NodeExecutionResult.Failed(error);
        }

        // Use the public property to determine which connector ID to activate
        return OutputConnectorIdToActivate.HasValue 
            ? NodeExecutionResult.Succeeded(OutputConnectorIdToActivate.Value) 
            : NodeExecutionResult.Succeeded(); // Succeeds without activation if property is null
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
    private (Graph graph, IExecutionContext context, ITestNode NodeA, ITestNode NodeB) SetupLinearSuccessGraph()
    {
        var graph = new Graph();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var nodeA = Substitute.For<ITestNode>();
        nodeA.Id.Returns(Guid.NewGuid());
        nodeA.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded()));
        graph.AddNode(nodeA);
        // Add default connectors for mock node A
        var outputA = new Connector(nodeA, ConnectorDirection.Output, typeof(object));
        nodeA.OutputConnectors.Returns(new List<IConnector> { outputA }.AsReadOnly());

        var nodeB = Substitute.For<ITestNode>();
        nodeB.Id.Returns(Guid.NewGuid());
        nodeB.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded()));
        graph.AddNode(nodeB);
        // Add default connectors for mock node B
        var inputB = new Connector(nodeB, ConnectorDirection.Input, typeof(object));
        nodeB.InputConnectors.Returns(new List<IConnector> { inputB }.AsReadOnly());

        graph.AddConnection(outputA, inputB); // Connect using graph

        return (graph, context, nodeA, nodeB);
    }

     private (Graph graph, IExecutionContext context, ITestNode NodeA, FailureNode NodeB, ITestNode NodeC) SetupLinearFailureGraph()
    {
        var graph = new Graph();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var nodeA = Substitute.For<ITestNode>();
        nodeA.Id.Returns(Guid.NewGuid());
        nodeA.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded()));
        graph.AddNode(nodeA);
        var outputA = new Connector(nodeA, ConnectorDirection.Output, typeof(object));
        nodeA.OutputConnectors.Returns(new List<IConnector> { outputA }.AsReadOnly());

        var nodeB = new FailureNode(); // Real node, add connectors directly
        graph.AddNode(nodeB);
        var inputB = new Connector(nodeB, ConnectorDirection.Input, typeof(object));
        nodeB.AddInputConnector(inputB); // Use method
        var outputB = new Connector(nodeB, ConnectorDirection.Output, typeof(object)); // Add output in case needed
        nodeB.AddOutputConnector(outputB);

        var nodeC = Substitute.For<ITestNode>();
        nodeC.Id.Returns(Guid.NewGuid());
        graph.AddNode(nodeC);
        var inputC = new Connector(nodeC, ConnectorDirection.Input, typeof(object));
        nodeC.InputConnectors.Returns(new List<IConnector> { inputC }.AsReadOnly());
        
        graph.AddConnection(outputA, inputB); // Connect A -> B
        // Optionally connect B -> C if the test logic requires it
        // graph.AddConnection(outputB, inputC);

        return (graph, context, nodeA, nodeB, nodeC);
    }

     private (Graph graph, IExecutionContext context, INode NodeA) SetupEmptyGraph()
     {
         var graph = new Graph();
         var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
         var nodeA = Substitute.For<ITestNode>();
         nodeA.Id.Returns(Guid.NewGuid());
         nodeA.ExecuteAsync(Arg.Any<IExecutionContext>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(NodeExecutionResult.Succeeded()));
         graph.AddNode(nodeA);
         // Add default output
         var outputA = new Connector(nodeA, ConnectorDirection.Output, typeof(object));
         nodeA.OutputConnectors.Returns(new List<IConnector> { outputA }.AsReadOnly());

         return (graph, context, nodeA);
     }
     
     private (Graph graph, IExecutionContext context, AsyncTestNode Node1, AsyncTestNode Node2) SetupAsyncSuccessGraph(TimeSpan? delay1 = null, TimeSpan? delay2 = null)
     {
         var graph = new Graph();
         var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
         var asyncNode1 = new AsyncTestNode(delay1);
         graph.AddNode(asyncNode1);
         var output1 = asyncNode1.OutputConnectors.First(); // Get the default connector
         
         var asyncNode2 = new AsyncTestNode(delay2);
         graph.AddNode(asyncNode2);
         var input2 = new Connector(asyncNode2, ConnectorDirection.Input, typeof(object));
         asyncNode2.AddInputConnector(input2);

         graph.AddConnection(output1, input2); // Connect using graph

         return (graph, context, asyncNode1, asyncNode2);
     }

    // === Test Scenarios ===
    [Fact]
    public async Task RunAsync_SuccessfulLinearExecution_RaisesCorrectEventsInOrder()
    {
        // Arrange
        var (graph, context, nodeA, nodeB) = SetupLinearSuccessGraph();
        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(nodeA, context); // Start from nodeA

        // Assert
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(nodeA.Id).ShouldBe(0);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeB.Id).ShouldBe(1); // This should now be 1
        recorder.NodeCompletedCounts.GetValueOrDefault(nodeB.Id).ShouldBe(1); // This should now be 1
        recorder.NodeFailedCounts.GetValueOrDefault(nodeB.Id).ShouldBe(0);
        recorder.WorkflowCompletedCount.ShouldBe(1);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.RecordedEvents.Count.ShouldBe(6);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(nodeA.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(nodeA.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(nodeB.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(nodeB.Id);
        recorder.RecordedEvents[5].ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();
    }

    [Fact]
    public async Task RunAsync_NodeFailureExecution_RaisesCorrectEvents()
    {
        // Arrange
        var (graph, context, nodeA, nodeB, nodeC) = SetupLinearFailureGraph();
        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(nodeA, context);

        // Assert
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(nodeA.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(nodeB.Id).ShouldBe(1); // This should now be 1
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
        var (graph, context, nodeA) = SetupEmptyGraph();
        var runner = new WorkflowRunner(new DefaultNodeExecutor());
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
        var (graph, context, nodeA, nodeB) = SetupLinearSuccessGraph();
        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);
        
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
        var (graph, context, nodeA, nodeB, _) = SetupLinearFailureGraph();
        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);
        
        await runner.RunAsync(nodeA, context);

        // Assert
        var workflowFailedArgs = recorder.GetEventsOfType<WorkflowExecutionFailedEventArgs>().FirstOrDefault();
        workflowFailedArgs.ShouldNotBeNull();
        workflowFailedArgs.Context.ShouldBeSameAs(context);
        workflowFailedArgs.Error.ShouldBeOfType<InvalidOperationException>();
        workflowFailedArgs.FailedNode.ShouldBeSameAs(nodeB);

        var nodeBFailedArgs = recorder.GetEventsOfType<NodeExecutionFailedEventArgs>().FirstOrDefault(e => e.Node.Id == nodeB.Id);
        nodeBFailedArgs.ShouldNotBeNull();
        nodeBFailedArgs.Node.ShouldBeSameAs(nodeB);
        nodeBFailedArgs.Error.ShouldBeSameAs(workflowFailedArgs.Error);
        nodeBFailedArgs.Context.ShouldBeSameAs(context);
    }

    // === Async Event Test Scenarios ===

    [Fact]
    public async Task RunAsync_SingleAsyncNodeSuccess_RaisesCorrectEventsInOrder()
    {
        // Arrange
        // Create nodes directly for isolation, don't use SetupAsyncSuccessGraph here
        var asyncNode1 = new AsyncTestNode(); 
        var asyncNode2Id = Guid.NewGuid(); // We just need an ID to check against
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = new WorkflowRunner(new DefaultNodeExecutor()); 
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // No connections are made

        // Act
        await runner.RunAsync(asyncNode1, context);

        // Assert - Only Node1 should execute
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(0);

        // Ensure Node 2 did NOT run - Check against the unused ID
        recorder.NodeStartingCounts.ContainsKey(asyncNode2Id).ShouldBeFalse();
        recorder.NodeCompletedCounts.ContainsKey(asyncNode2Id).ShouldBeFalse();

        recorder.WorkflowCompletedCount.ShouldBe(1);
        recorder.WorkflowFailedCount.ShouldBe(0);

        // Expecting 4 events: WorkflowStart, Node1Start, Node1Complete, WorkflowComplete
        recorder.RecordedEvents.Count.ShouldBe(4);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();
    }

    [Fact]
    public async Task RunAsync_SingleAsyncNodeFailureException_RaisesCorrectEvents()
    {
        // Arrange
        var asyncNode1 = new AsyncTestNode(shouldFail: true, failWithException: true);
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(asyncNode1, context);

        // Assert
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.WorkflowCancelledCount.ShouldBe(0);

        // Expect 4 events: WorkflowStart, Node1Start, Node1Fail, WorkflowFail
        recorder.RecordedEvents.Count.ShouldBe(4); 
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionFailedEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<WorkflowExecutionFailedEventArgs>().FailedNode?.Id.ShouldBe(asyncNode1.Id);
    }

    [Fact]
    public async Task RunAsync_SingleAsyncNodeFailureResult_RaisesCorrectEvents()
    {
        // Arrange
        var asyncNode1 = new AsyncTestNode(shouldFail: true, failWithException: false);
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Act
        await runner.RunAsync(asyncNode1, context);

        // Assert
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeFailedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.WorkflowCancelledCount.ShouldBe(0);

        // Expect 4 events: WorkflowStart, Node1Start, Node1Fail, WorkflowFail
        recorder.RecordedEvents.Count.ShouldBe(4);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionFailedEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<WorkflowExecutionFailedEventArgs>().FailedNode?.Id.ShouldBe(asyncNode1.Id);
    }

    [Fact]
    public async Task RunAsync_MultipleSequentialAsyncNodes_RaisesCorrectEventsInOrder()
    {
        // Arrange
        var (graph, context, asyncNode1, asyncNode2) = SetupAsyncSuccessGraph();
        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        // Connect Node1 -> Node2 (assuming single output activation)
        await runner.RunAsync(asyncNode1, context);

        // Assert
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(1);
        recorder.WorkflowFailedCount.ShouldBe(0);

        // Expect 6 events: WorkflowStart, N1Start, N1Complete, N2Start, N2Complete, WorkflowComplete
        recorder.RecordedEvents.Count.ShouldBe(6);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode2.Id);
        recorder.RecordedEvents[4].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(asyncNode2.Id);
        recorder.RecordedEvents[5].ShouldBeOfType<WorkflowExecutionCompletedEventArgs>();
    }

    [Fact]
    public async Task RunAsync_AsyncNodeActivatesOutput_NextNodeRuns()
    {
         // Arrange
        var graph = new Graph();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        
        // Create nodes
        var asyncNode1 = new AsyncTestNode(); // Use constructor without ID
        graph.AddNode(asyncNode1);
        var nextNode = new TestNode();
        graph.AddNode(nextNode);

        // Get the connector added by AsyncTestNode constructor
        var output1 = asyncNode1.OutputConnectors.FirstOrDefault();
        output1.ShouldNotBeNull("AsyncTestNode should have a default output connector.");
        
        // Add input for nextNode
        var inputNext = new Connector(nextNode, ConnectorDirection.Input, typeof(object));
        if (nextNode is TestNode realTestNode) realTestNode.AddInputConnector(inputNext);

        // Connect using graph
        graph.AddConnection(output1, inputNext); 
        
        // Set the ID to activate on the node instance
        asyncNode1.OutputConnectorIdToActivate = output1.Id;

        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);
       
        // Act
        await runner.RunAsync(asyncNode1, context);

        // Assert (Should now pass)
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(nextNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(nextNode.Id).ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(1);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.RecordedEvents.Count.ShouldBe(6);
    }

    [Fact]
    public async Task RunAsync_CancellationDuringAsyncNodeDelay_ShouldStopAndSetCancelledStatus()
    {
        // Arrange
        var longDelay = TimeSpan.FromSeconds(2); 
        var longAsyncNode = new AsyncTestNode(delay: longDelay);
        var nextNode = new TestNode();
        
        var graph = new Graph();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        graph.AddNode(longAsyncNode);
        graph.AddNode(nextNode);

        var output1 = longAsyncNode.OutputConnectors.FirstOrDefault();
        if (output1 == null) 
        {
            output1 = new Connector(longAsyncNode, ConnectorDirection.Output, typeof(object));
            longAsyncNode.AddOutputConnector(output1);
        }
        var inputNext = new Connector(nextNode, ConnectorDirection.Input, typeof(object));
        if (nextNode is TestNode realTestNode) realTestNode.AddInputConnector(inputNext);
        
        graph.AddConnection(output1, inputNext);

        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        var cts = new CancellationTokenSource();

        // Act
        var runTask = runner.RunAsync(longAsyncNode, context, cts.Token);

        await Task.Delay(50); 
        cts.Cancel();

        await runTask;

        // Assert
        context.CurrentStatus.ShouldBe(ExecutionStatus.Cancelled);
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(longAsyncNode.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(longAsyncNode.Id).ShouldBe(0);
        recorder.NodeFailedCounts.GetValueOrDefault(longAsyncNode.Id).ShouldBe(0);
        recorder.NodeStartingCounts.GetValueOrDefault(nextNode.Id).ShouldBe(0);
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.WorkflowCancelledCount.ShouldBe(1);
        recorder.RecordedEvents.Count.ShouldBe(3);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(longAsyncNode.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<WorkflowCancelledEventArgs>();
    }

    [Fact]
    public async Task RunAsync_CancellationBetweenNodes_ShouldStopAndSetCancelledStatus()
    {
        // Arrange
        var asyncNode1 = new AsyncTestNode(TimeSpan.FromMilliseconds(10));
        var asyncNode2 = new AsyncTestNode(TimeSpan.FromMilliseconds(100));

        var graph = new Graph();
        var context = new Nodify.Workflow.Core.Execution.Context.ExecutionContext();
        graph.AddNode(asyncNode1);
        graph.AddNode(asyncNode2);
        
        var output1 = asyncNode1.OutputConnectors.FirstOrDefault();
        if (output1 == null) 
        {
            output1 = new Connector(asyncNode1, ConnectorDirection.Output, typeof(object));
            asyncNode1.AddOutputConnector(output1);
        }
        var input2 = new Connector(asyncNode2, ConnectorDirection.Input, typeof(object));
        asyncNode2.AddInputConnector(input2);

        graph.AddConnection(output1, input2);

        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);

        var cts = new CancellationTokenSource();

        runner.NodeCompleted += (s, e) =>
        {
            if (e.Node.Id == asyncNode1.Id)
            {
                cts.Cancel();
            }
        };

        // Act
        await runner.RunAsync(asyncNode1, context, cts.Token);

        // Assert
        context.CurrentStatus.ShouldBe(ExecutionStatus.Cancelled);
        recorder.WorkflowStartedCount.ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(0);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(0);
        recorder.WorkflowCompletedCount.ShouldBe(0);
        recorder.WorkflowFailedCount.ShouldBe(0);
        recorder.WorkflowCancelledCount.ShouldBe(1);
        recorder.RecordedEvents.Count.ShouldBe(4);
        recorder.RecordedEvents[0].ShouldBeOfType<WorkflowExecutionStartedEventArgs>();
        recorder.RecordedEvents[1].ShouldBeOfType<NodeExecutionStartingEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[2].ShouldBeOfType<NodeExecutionCompletedEventArgs>().Node.Id.ShouldBe(asyncNode1.Id);
        recorder.RecordedEvents[3].ShouldBeOfType<WorkflowCancelledEventArgs>();
    }
    
    [Fact]
    public async Task RunAsync_NoCancellation_ShouldCompleteNormally()
    {
        // Arrange
        var (graph, context, asyncNode1, asyncNode2) = SetupAsyncSuccessGraph();
        var runner = new WorkflowRunner(new DefaultNodeExecutor());
        var recorder = new EventRecorder();
        recorder.Subscribe(runner);
        var cts = new CancellationTokenSource(); // No cancellation triggered

        await runner.RunAsync(asyncNode1, context, cts.Token);

        // Assert
        context.CurrentStatus.ShouldBe(ExecutionStatus.Completed);
        recorder.WorkflowCancelledCount.ShouldBe(0);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode1.Id).ShouldBe(1);
        recorder.NodeStartingCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(1);
        recorder.NodeCompletedCounts.GetValueOrDefault(asyncNode2.Id).ShouldBe(1);
        recorder.WorkflowCompletedCount.ShouldBe(1);
    }
} 