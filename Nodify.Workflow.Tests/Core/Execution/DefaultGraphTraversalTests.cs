using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Execution;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Nodify.Workflow.Tests.Core.Execution;

public interface ISpecialNode : INode { } // Example interface for testing

public class DefaultGraphTraversalTests
{
    private readonly INode _startNode;
    private readonly INode _middleNode;
    private readonly INode _endNode;
    private readonly IConnection _connection1;
    private readonly IConnection _connection2;
    private readonly IGraphTraversal _traversal;

    public DefaultGraphTraversalTests()
    {
        // Setup basic test graph using NSubstitute mocks directly in constructor
        _startNode = Substitute.For<INode>();
        _middleNode = Substitute.For<INode>();
        _endNode = Substitute.For<INode>();

        // Assign unique IDs for identification
        _startNode.Id.Returns(Guid.NewGuid());
        _middleNode.Id.Returns(Guid.NewGuid());
        _endNode.Id.Returns(Guid.NewGuid());

        var startOutput = Substitute.For<IConnector>();
        var middleInput = Substitute.For<IConnector>();
        var middleOutput = Substitute.For<IConnector>();
        var endInput = Substitute.For<IConnector>();

        // Setup startOutput connector
        startOutput.Id.Returns(Guid.NewGuid());
        startOutput.ParentNode.Returns(_startNode);
        startOutput.Direction.Returns(ConnectorDirection.Output);
        startOutput.DataType.Returns(typeof(string));
        startOutput.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        startOutput.AddConnection(Arg.Any<IConnection>()).Returns(true);
        startOutput.Connections.Returns(ci => new List<IConnection>().AsReadOnly()); // Default

        // Setup middleInput connector
        middleInput.Id.Returns(Guid.NewGuid());
        middleInput.ParentNode.Returns(_middleNode);
        middleInput.Direction.Returns(ConnectorDirection.Input);
        middleInput.DataType.Returns(typeof(string));
        middleInput.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        middleInput.AddConnection(Arg.Any<IConnection>()).Returns(true);
        middleInput.Connections.Returns(ci => new List<IConnection>().AsReadOnly()); // Default

        // Setup middleOutput connector
        middleOutput.Id.Returns(Guid.NewGuid());
        middleOutput.ParentNode.Returns(_middleNode);
        middleOutput.Direction.Returns(ConnectorDirection.Output);
        middleOutput.DataType.Returns(typeof(string));
        middleOutput.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        middleOutput.AddConnection(Arg.Any<IConnection>()).Returns(true);
        middleOutput.Connections.Returns(ci => new List<IConnection>().AsReadOnly()); // Default

        // Setup endInput connector
        endInput.Id.Returns(Guid.NewGuid());
        endInput.ParentNode.Returns(_endNode);
        endInput.Direction.Returns(ConnectorDirection.Input);
        endInput.DataType.Returns(typeof(string));
        endInput.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        endInput.AddConnection(Arg.Any<IConnection>()).Returns(true);
        endInput.Connections.Returns(ci => new List<IConnection>().AsReadOnly()); // Default


        // Associate connectors with nodes
        _startNode.OutputConnectors.Returns(new[] { startOutput });
        _startNode.InputConnectors.Returns(new IConnector[0]);
        _middleNode.InputConnectors.Returns(new[] { middleInput });
        _middleNode.OutputConnectors.Returns(new[] { middleOutput });
        _endNode.InputConnectors.Returns(new[] { endInput });
        _endNode.OutputConnectors.Returns(new IConnector[0]);

        // Setup connections (using substitutes)
        _connection1 = Substitute.For<IConnection>();
        _connection1.Id.Returns(Guid.NewGuid());
        _connection1.Source.Returns(startOutput);
        _connection1.Target.Returns(middleInput);

        _connection2 = Substitute.For<IConnection>();
        _connection2.Id.Returns(Guid.NewGuid());
        _connection2.Source.Returns(middleOutput);
        _connection2.Target.Returns(endInput);

        // Link connections back to connectors (override default empty list)
        startOutput.Connections.Returns(new[] { _connection1 });
        middleInput.Connections.Returns(new[] { _connection1 });
        middleOutput.Connections.Returns(new[] { _connection2 });
        endInput.Connections.Returns(new[] { _connection2 });

        _traversal = new DefaultGraphTraversal();
    }

    [Fact]
    public void DepthFirstTraversal_ShouldVisitAllNodes()
    {
        // Arrange
        var visitedNodes = new List<INode>();

        // Act
        _traversal.DepthFirstTraversal(_startNode, node =>
        {
            visitedNodes.Add(node);
            return true;
        });

        // Assert
        visitedNodes.Count.ShouldBe(3);
        visitedNodes[0].ShouldBe(_startNode);
        visitedNodes[1].ShouldBe(_middleNode);
        visitedNodes[2].ShouldBe(_endNode);
    }

    [Fact]
    public void BreadthFirstTraversal_ShouldVisitAllNodes()
    {
        // Arrange
        var visitedNodes = new List<INode>();

        // Act
        _traversal.BreadthFirstTraversal(_startNode, node =>
        {
            visitedNodes.Add(node);
            return true;
        });

        // Assert
        visitedNodes.Count.ShouldBe(3);
        visitedNodes[0].ShouldBe(_startNode);
        visitedNodes[1].ShouldBe(_middleNode);
        visitedNodes[2].ShouldBe(_endNode);
    }

    [Fact]
    public void Traversal_ShouldHandleCycles()
    {
        // Arrange
        // Graph: start -> middle -> end
        // Add cycle: middle -> start
        var middleOutputToStart = Substitute.For<IConnector>();
        // Setup middleOutputToStart connector
        middleOutputToStart.Id.Returns(Guid.NewGuid());
        middleOutputToStart.ParentNode.Returns(_middleNode);
        middleOutputToStart.Direction.Returns(ConnectorDirection.Output);
        middleOutputToStart.DataType.Returns(typeof(string));
        middleOutputToStart.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        middleOutputToStart.AddConnection(Arg.Any<IConnection>()).Returns(true);
        middleOutputToStart.Connections.Returns(ci => new List<IConnection>().AsReadOnly()); // Default
        _middleNode.OutputConnectors.Returns(_middleNode.OutputConnectors.Concat(new[] { middleOutputToStart }).ToArray()); // Add new output

        var startInputForCycle = Substitute.For<IConnector>();
        // Setup startInputForCycle connector
        startInputForCycle.Id.Returns(Guid.NewGuid());
        startInputForCycle.ParentNode.Returns(_startNode);
        startInputForCycle.Direction.Returns(ConnectorDirection.Input);
        startInputForCycle.DataType.Returns(typeof(string));
        startInputForCycle.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        startInputForCycle.AddConnection(Arg.Any<IConnection>()).Returns(true);
        startInputForCycle.Connections.Returns(ci => new List<IConnection>().AsReadOnly()); // Default
        _startNode.InputConnectors.Returns(new[] { startInputForCycle }); // Add new input

        var cycleConnection = Substitute.For<IConnection>();
        cycleConnection.Id.Returns(Guid.NewGuid());
        cycleConnection.Source.Returns(middleOutputToStart);
        cycleConnection.Target.Returns(startInputForCycle);

        // Update relevant connector connection lists
        middleOutputToStart.Connections.Returns(new[] { cycleConnection });
        startInputForCycle.Connections.Returns(new[] { cycleConnection });

        var visitedNodes = new HashSet<INode>();
        int visitCount = 0;

        // Act
        _traversal.DepthFirstTraversal(_startNode, node =>
        {
            visitedNodes.Add(node);
            visitCount++;
            // Prevent infinite loop in test execution if DFS logic fails
            if (visitCount > 10) return false; 
            return true;
        });

        // Assert
        // Should visit each unique reachable node exactly once
        visitedNodes.Count.ShouldBe(3); // start, middle, end
        visitedNodes.ShouldContain(_startNode);
        visitedNodes.ShouldContain(_middleNode);
        visitedNodes.ShouldContain(_endNode);
    }

    [Theory]
    [InlineData(true)]  // Stop traversal early
    [InlineData(false)] // Continue traversal
    public void DepthFirstTraversal_ShouldRespectVisitorReturn(bool stopTraversal)
    {
        // Arrange
        var visitedNodes = new List<INode>();
        int nodesToVisit = stopTraversal ? 1 : 3;

        // Act
        _traversal.DepthFirstTraversal(_startNode, node =>
        {
            visitedNodes.Add(node);
            return visitedNodes.Count < nodesToVisit; // Stop if stopTraversal is true and we visited 1 node
        });

        // Assert
        visitedNodes.Count.ShouldBe(nodesToVisit);
        visitedNodes[0].ShouldBe(_startNode);
        if (!stopTraversal)
        {
            visitedNodes[1].ShouldBe(_middleNode);
            visitedNodes[2].ShouldBe(_endNode);
        }
    }

    [Fact]
    public void Traversal_ShouldHandleEmptyGraph()
    {
        // Arrange
        var emptyNode = Substitute.For<INode>();
        emptyNode.Id.Returns(Guid.NewGuid());
        emptyNode.InputConnectors.Returns(new IConnector[0]);
        emptyNode.OutputConnectors.Returns(new IConnector[0]);
        var visitedNodes = new List<INode>();
        var traversal = new DefaultGraphTraversal(); // Use local instance

        // Act
        traversal.DepthFirstTraversal(emptyNode, node =>
        {
            visitedNodes.Add(node);
            return true;
        });

        // Assert
        visitedNodes.Count.ShouldBe(1);
        visitedNodes[0].ShouldBe(emptyNode);
    }

    [Fact]
    public void Traversal_ShouldRespectNodeTypeFilter() // Note: This test might need adjustment based on actual ISpecialNode usage
    {
        // Arrange
        var specialNode = Substitute.For<ISpecialNode>();
        specialNode.Id.Returns(Guid.NewGuid());

        var specialOutput = Substitute.For<IConnector>();
        specialOutput.Id.Returns(Guid.NewGuid());
        specialOutput.ParentNode.Returns(specialNode);
        specialOutput.Direction.Returns(ConnectorDirection.Output);
        specialOutput.DataType.Returns(typeof(string));
        specialOutput.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        specialOutput.AddConnection(Arg.Any<IConnection>()).Returns(true);
        specialOutput.Connections.Returns(ci => new List<IConnection>().AsReadOnly());
        specialNode.OutputConnectors.Returns(new[] { specialOutput });
        specialNode.InputConnectors.Returns(new IConnector[0]);

        var middleInputConnector = _middleNode.InputConnectors.First();

        var connSpecialMiddle = Substitute.For<IConnection>();
        connSpecialMiddle.Id.Returns(Guid.NewGuid());
        connSpecialMiddle.Source.Returns(specialOutput);
        connSpecialMiddle.Target.Returns(middleInputConnector);

        specialOutput.Connections.Returns(new[] { connSpecialMiddle });
        middleInputConnector.Connections.Returns(new[] { connSpecialMiddle }); // Assuming middle only has this input now

        var visitedSpecialNodes = new List<INode>();

        // Act: Start traversal from the special node
        _traversal.DepthFirstTraversal(specialNode, node =>
        {
            if (node is ISpecialNode) // Filter check
            {
                visitedSpecialNodes.Add(node);
            }
            return true;
        });

        // Assert
        visitedSpecialNodes.Count.ShouldBe(1);
        visitedSpecialNodes[0].ShouldBe(specialNode);
    }

    [Fact]
    public void FindNodeById_ShouldReturnCorrectNode()
    {
        // Arrange
        var targetId = _middleNode.Id;

        // Act
        var foundNode = _traversal.FindNodeById(_startNode, targetId);

        // Assert
        foundNode.ShouldNotBeNull();
        foundNode.Id.ShouldBe(targetId);
    }

    [Fact]
    public void FindNodeById_ShouldReturnNullWhenNodeNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var foundNode = _traversal.FindNodeById(_startNode, nonExistentId);

        // Assert
        foundNode.ShouldBeNull();
    }

    [Fact]
    public void FindShortestPath_ShouldReturnShortestPath()
    {
        // Arrange
        // Base graph is start -> middle -> end
        // Create alternative longer path: start -> alternate -> end using Mocks
        var alternateNode = Substitute.For<INode>();
        alternateNode.Id.Returns(Guid.NewGuid());

        var altInput = Substitute.For<IConnector>();
        altInput.Id.Returns(Guid.NewGuid());
        altInput.ParentNode.Returns(alternateNode);
        altInput.Direction.Returns(ConnectorDirection.Input);
        altInput.Connections.Returns(ci => new List<IConnection>().AsReadOnly());

        var altOutput = Substitute.For<IConnector>();
        altOutput.Id.Returns(Guid.NewGuid());
        altOutput.ParentNode.Returns(alternateNode);
        altOutput.Direction.Returns(ConnectorDirection.Output);
        altOutput.Connections.Returns(ci => new List<IConnection>().AsReadOnly());

        alternateNode.InputConnectors.Returns(new[] { altInput });
        alternateNode.OutputConnectors.Returns(new[] { altOutput });

        // Connection: Start -> Alternate
        var startOutputConnector = _startNode.OutputConnectors.First(); 
        var connStartAlt = Substitute.For<IConnection>();
        connStartAlt.Id.Returns(Guid.NewGuid());
        connStartAlt.Source.Returns(startOutputConnector);
        connStartAlt.Target.Returns(altInput);
        altInput.Connections.Returns(new[] { connStartAlt }); // Link connection to altInput

        // Connection: Alternate -> End
        var endInputConnector = _endNode.InputConnectors.First();
        var connAltEnd = Substitute.For<IConnection>();
        connAltEnd.Id.Returns(Guid.NewGuid());
        connAltEnd.Source.Returns(altOutput);
        connAltEnd.Target.Returns(endInputConnector);
        altOutput.Connections.Returns(new[] { connAltEnd }); // Link connection to altOutput

        // Add the second connection to the start node's output connector
        startOutputConnector.Connections.Returns(new[] { _connection1, connStartAlt }); 

        // Act
        var path = _traversal.FindShortestPath(_startNode, _endNode);

        // Assert
        path.ShouldNotBeNull();
        path.Count.ShouldBe(3); // Should still find start -> middle -> end (length 2 edges, 3 nodes)
        path[0].ShouldBe(_startNode);
        path[1].ShouldBe(_middleNode);
        path[2].ShouldBe(_endNode);
    }

    [Fact]
    public void FindShortestPath_ShouldReturnEmptyListWhenNoPathExists()
    {
        // Arrange
        var isolatedNode = Substitute.For<INode>();
        isolatedNode.Id.Returns(Guid.NewGuid());
        isolatedNode.InputConnectors.Returns(new IConnector[0]);
        isolatedNode.OutputConnectors.Returns(new IConnector[0]);

        // Act
        var path = _traversal.FindShortestPath(_startNode, isolatedNode);

        // Assert
        path.ShouldNotBeNull();
        path.ShouldBeEmpty();
    }

    [Fact]
    public void GetEntryPoints_ShouldReturnNodesWithNoInputs()
    {
        // Act
        // Use a new traversal instance to avoid potential side effects from other tests modifying mock state
        var traversal = new DefaultGraphTraversal();
        var entryPoints = traversal.GetEntryPoints(_startNode);

        // Assert
        entryPoints.Count.ShouldBe(1);
        entryPoints[0].ShouldBe(_startNode);
    }

    [Fact]
    public void GetExitPoints_ShouldReturnNodesWithNoOutputs()
    {
        // Act
         var traversal = new DefaultGraphTraversal();
        var exitPoints = traversal.GetExitPoints(_startNode);

        // Assert
        exitPoints.Count.ShouldBe(1);
        exitPoints[0].ShouldBe(_endNode);
    }

    [Fact]
    public void TopologicalSort_ShouldReturnValidExecutionOrder()
    {
        // Act
         var traversal = new DefaultGraphTraversal();
        var sortedNodes = traversal.TopologicalSort(_startNode);

        // Assert
        sortedNodes.Count.ShouldBe(3);
        sortedNodes[0].ShouldBe(_startNode);
        sortedNodes[1].ShouldBe(_middleNode);
        sortedNodes[2].ShouldBe(_endNode);
    }

    [Fact]
    public void TopologicalSort_ShouldThrowOnCycles()
    {
        // Arrange: Create a simple A -> B -> A cycle using mocks
        var nodeA = Substitute.For<INode>();
        var nodeB = Substitute.For<INode>();
        nodeA.Id.Returns(Guid.NewGuid());
        nodeB.Id.Returns(Guid.NewGuid());

        var connectorAtoB = Substitute.For<IConnector>();
        var connectorBtoA = Substitute.For<IConnector>();
        var connectorBInput = Substitute.For<IConnector>();
        var connectorAInput = Substitute.For<IConnector>();

        // Setup connectorAtoB
        connectorAtoB.Id.Returns(Guid.NewGuid());
        connectorAtoB.ParentNode.Returns(nodeA);
        connectorAtoB.Direction.Returns(ConnectorDirection.Output);
        connectorAtoB.DataType.Returns(typeof(string));
        connectorAtoB.Connections.Returns(ci => new List<IConnection>().AsReadOnly());

        // Setup connectorBInput
        connectorBInput.Id.Returns(Guid.NewGuid());
        connectorBInput.ParentNode.Returns(nodeB);
        connectorBInput.Direction.Returns(ConnectorDirection.Input);
        connectorBInput.Connections.Returns(ci => new List<IConnection>().AsReadOnly());

        // Setup connectorBtoA
        connectorBtoA.Id.Returns(Guid.NewGuid());
        connectorBtoA.ParentNode.Returns(nodeB);
        connectorBtoA.Direction.Returns(ConnectorDirection.Output);
        connectorBtoA.Connections.Returns(ci => new List<IConnection>().AsReadOnly());

        // Setup connectorAInput
        connectorAInput.Id.Returns(Guid.NewGuid());
        connectorAInput.ParentNode.Returns(nodeA);
        connectorAInput.Direction.Returns(ConnectorDirection.Input);
        connectorAInput.Connections.Returns(ci => new List<IConnection>().AsReadOnly());


        // Associate connectors with nodes
        nodeA.OutputConnectors.Returns(new[] { connectorAtoB });
        nodeA.InputConnectors.Returns(new[] { connectorAInput });
        nodeB.OutputConnectors.Returns(new[] { connectorBtoA });
        nodeB.InputConnectors.Returns(new[] { connectorBInput });

        // Setup connections
        var connectionAtoB = Substitute.For<IConnection>();
        connectionAtoB.Id.Returns(Guid.NewGuid());
        connectionAtoB.Source.Returns(connectorAtoB);
        connectionAtoB.Target.Returns(connectorBInput);

        var connectionBtoA = Substitute.For<IConnection>();
        connectionBtoA.Id.Returns(Guid.NewGuid());
        connectionBtoA.Source.Returns(connectorBtoA);
        connectionBtoA.Target.Returns(connectorAInput);

        // Link connections back to connectors
        connectorAtoB.Connections.Returns(new[] { connectionAtoB });
        connectorBInput.Connections.Returns(new[] { connectionAtoB });
        connectorBtoA.Connections.Returns(new[] { connectionBtoA });
        connectorAInput.Connections.Returns(new[] { connectionBtoA });

        var traversal = new DefaultGraphTraversal(); // Use a local traversal instance

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => traversal.TopologicalSort(nodeA)) // Start from nodeA
            .Message.ShouldContain("Cycle detected");
    }

} 