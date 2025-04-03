using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Execution;
using NSubstitute;
using Shouldly;

namespace Nodify.Workflow.Tests.Core.Execution;

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
        // Setup basic test graph
        _startNode = new Node();
        _middleNode = new Node();
        _endNode = new Node();

        var startOutput = new Connector(_startNode, ConnectorDirection.Output, typeof(string));
        var middleInput = new Connector(_middleNode, ConnectorDirection.Input, typeof(string));
        var middleOutput = new Connector(_middleNode, ConnectorDirection.Output, typeof(string));
        var endInput = new Connector(_endNode, ConnectorDirection.Input, typeof(string));

        _startNode.AddOutputConnector(startOutput);
        _middleNode.AddInputConnector(middleInput);
        _middleNode.AddOutputConnector(middleOutput);
        _endNode.AddInputConnector(endInput);

        _connection1 = new Connection(startOutput, middleInput);
        _connection2 = new Connection(middleOutput, endInput);

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
        var cycleNode = new Node();
        var cycleOutput = new Connector(cycleNode, ConnectorDirection.Output, typeof(string));
        var cycleInput = new Connector(cycleNode, ConnectorDirection.Input, typeof(string));
        cycleNode.AddOutputConnector(cycleOutput);
        cycleNode.AddInputConnector(cycleInput);

        // Create cycle: startNode -> cycleNode -> startNode
        var startInput = new Connector(_startNode, ConnectorDirection.Input, typeof(string));
        _startNode.AddInputConnector(startInput);
        new Connection(cycleOutput, startInput);

        var visitedNodes = new HashSet<INode>();

        // Act
        _traversal.DepthFirstTraversal(_startNode, node =>
        {
            visitedNodes.Add(node);
            return true;
        });

        // Assert
        visitedNodes.Count.ShouldBe(4); // Should visit all nodes exactly once
        visitedNodes.ShouldContain(_startNode);
        visitedNodes.ShouldContain(cycleNode);
        visitedNodes.ShouldContain(_middleNode);
        visitedNodes.ShouldContain(_endNode);
    }

    [Fact]
    public void Traversal_ShouldHandleEmptyGraph()
    {
        // Arrange
        var emptyNode = new Node();
        var visitedNodes = new List<INode>();

        // Act
        _traversal.DepthFirstTraversal(emptyNode, node =>
        {
            visitedNodes.Add(node);
            return true;
        });

        // Assert
        visitedNodes.Count.ShouldBe(1);
        visitedNodes[0].ShouldBe(emptyNode);
    }

    [Fact]
    public void Traversal_ShouldRespectNodeTypeFilter()
    {
        // Arrange
        var specialNode = Substitute.For<ISpecialNode>();
        var specialOutput = new Connector(specialNode, ConnectorDirection.Output, typeof(string));
        specialNode.AddOutputConnector(specialOutput);

        var normalInput = new Connector(_middleNode, ConnectorDirection.Input, typeof(string));
        _middleNode.AddInputConnector(normalInput);
        new Connection(specialOutput, normalInput);

        var visitedSpecialNodes = new List<INode>();

        // Act
        _traversal.DepthFirstTraversal(specialNode, node =>
        {
            if (node is ISpecialNode)
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
    public void FindShortestPath_ShouldReturnShortestPath()
    {
        // Arrange
        // Create alternative longer path
        var alternateNode = new Node();
        var altInput = new Connector(alternateNode, ConnectorDirection.Input, typeof(string));
        var altOutput = new Connector(alternateNode, ConnectorDirection.Output, typeof(string));
        alternateNode.AddInputConnector(altInput);
        alternateNode.AddOutputConnector(altOutput);

        var startAltOutput = new Connector(_startNode, ConnectorDirection.Output, typeof(string));
        _startNode.AddOutputConnector(startAltOutput);
        new Connection(startAltOutput, altInput);
        new Connection(altOutput, _endNode.InputConnectors.First());

        // Act
        var path = _traversal.FindShortestPath(_startNode, _endNode);

        // Assert
        path.Count.ShouldBe(3); // start -> middle -> end
        path[0].ShouldBe(_startNode);
        path[1].ShouldBe(_middleNode);
        path[2].ShouldBe(_endNode);
    }

    [Fact]
    public void GetEntryPoints_ShouldReturnNodesWithNoInputs()
    {
        // Act
        var entryPoints = _traversal.GetEntryPoints(_startNode);

        // Assert
        entryPoints.Count.ShouldBe(1);
        entryPoints[0].ShouldBe(_startNode);
    }

    [Fact]
    public void GetExitPoints_ShouldReturnNodesWithNoOutputs()
    {
        // Act
        var exitPoints = _traversal.GetExitPoints(_startNode);

        // Assert
        exitPoints.Count.ShouldBe(1);
        exitPoints[0].ShouldBe(_endNode);
    }

    [Fact]
    public void TopologicalSort_ShouldReturnValidExecutionOrder()
    {
        // Act
        var sortedNodes = _traversal.TopologicalSort(_startNode);

        // Assert
        sortedNodes.Count.ShouldBe(3);
        sortedNodes[0].ShouldBe(_startNode);
        sortedNodes[1].ShouldBe(_middleNode);
        sortedNodes[2].ShouldBe(_endNode);
    }

    [Fact]
    public void TopologicalSort_ShouldThrowOnCycles()
    {
        // Arrange
        var cycleNode = new Node();
        var cycleOutput = new Connector(cycleNode, ConnectorDirection.Output, typeof(string));
        var cycleInput = new Connector(cycleNode, ConnectorDirection.Input, typeof(string));
        cycleNode.AddOutputConnector(cycleOutput);
        cycleNode.AddInputConnector(cycleInput);

        // Create cycle: startNode -> cycleNode -> startNode
        var startInput = new Connector(_startNode, ConnectorDirection.Input, typeof(string));
        var startOutput = new Connector(_startNode, ConnectorDirection.Output, typeof(string));
        _startNode.AddInputConnector(startInput);
        _startNode.AddOutputConnector(startOutput);
        
        // Complete the cycle
        new Connection(startOutput, cycleInput);      // startNode -> cycleNode
        new Connection(cycleOutput, startInput);      // cycleNode -> startNode

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _traversal.TopologicalSort(_startNode))
            .Message.ShouldBe("Cycle detected in graph during topological sort");
    }

    [Fact]
    public void FindShortestPath_ShouldReturnEmptyListWhenNoPathExists()
    {
        // Arrange
        var unreachableNode = new Node();

        // Act
        var path = _traversal.FindShortestPath(_startNode, unreachableNode);

        // Assert
        path.Count.ShouldBe(0);
    }

    [Fact]
    public void FindNodeById_ShouldReturnNullWhenNodeNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var node = _traversal.FindNodeById(_startNode, nonExistentId);

        // Assert
        node.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]  // Stop traversal
    [InlineData(false)] // Continue traversal
    public void DepthFirstTraversal_ShouldRespectVisitorReturn(bool stopTraversal)
    {
        // Arrange
        var visitedNodes = new List<INode>();

        // Act
        _traversal.DepthFirstTraversal(_startNode, node =>
        {
            visitedNodes.Add(node);
            return !stopTraversal; // Return false to stop, true to continue
        });

        // Assert
        if (stopTraversal)
        {
            visitedNodes.Count.ShouldBe(1); // Only visited start node
            visitedNodes[0].ShouldBe(_startNode);
        }
        else
        {
            visitedNodes.Count.ShouldBe(3); // Visited all nodes
        }
    }
}

// Helper interface for type filtering test
public interface ISpecialNode : INode { } 