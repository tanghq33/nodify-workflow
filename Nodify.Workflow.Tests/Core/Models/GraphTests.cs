using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Shouldly;

namespace Nodify.Workflow.Tests.Core.Models;

public class GraphTests
{
    private readonly INode _node1;
    private readonly INode _node2;
    private readonly IConnector _sourceConnector;
    private readonly IConnector _targetConnector;
    private readonly Graph _graph;

    public GraphTests()
    {
        // Setup test nodes and connectors
        _node1 = new Node();
        _node2 = new Node();

        _sourceConnector = new Connector(_node1, ConnectorDirection.Output, typeof(string));
        _targetConnector = new Connector(_node2, ConnectorDirection.Input, typeof(string));

        _node1.AddOutputConnector(_sourceConnector);
        _node2.AddInputConnector(_targetConnector);

        _graph = new Graph();
    }

    [Fact]
    public void AddNode_ShouldAddNodeToGraph()
    {
        // Act
        var result = _graph.AddNode(_node1);

        // Assert
        result.ShouldBeTrue();
        _graph.Nodes.Count.ShouldBe(1);
        _graph.Nodes.First().ShouldBe(_node1);
    }

    [Fact]
    public void AddNode_ShouldThrowOnNullNode()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _graph.AddNode(null));
    }

    [Fact]
    public void RemoveNode_ShouldRemoveNodeAndItsConnections()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        _graph.AddConnection(_sourceConnector, _targetConnector);

        // Act
        var result = _graph.RemoveNode(_node1);

        // Assert
        result.ShouldBeTrue();
        _graph.Nodes.Count.ShouldBe(1);
        _graph.Nodes.ShouldNotContain(_node1);
        _graph.Connections.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveNode_ShouldReturnFalseForNonExistentNode()
    {
        // Arrange
        var nonExistentNode = new Node();

        // Act
        var result = _graph.RemoveNode(nonExistentNode);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void AddConnection_ShouldConnectTwoNodes()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);

        // Act
        var connection = _graph.AddConnection(_sourceConnector, _targetConnector);

        // Assert
        connection.ShouldNotBeNull();
        _graph.Connections.Count.ShouldBe(1);
        _graph.Connections.First().Source.ShouldBe(_sourceConnector);
        _graph.Connections.First().Target.ShouldBe(_targetConnector);
    }

    [Fact]
    public void AddConnection_ShouldReturnNullForInvalidConnection()
    {
        // Arrange
        var invalidConnector = new Connector(_node2, ConnectorDirection.Input, typeof(int));
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);

        // Act
        var connection = _graph.AddConnection(_sourceConnector, invalidConnector);

        // Assert
        connection.ShouldBeNull();
        _graph.Connections.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveConnection_ShouldDisconnectNodes()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        var connection = _graph.AddConnection(_sourceConnector, _targetConnector);

        // Act
        var result = _graph.RemoveConnection(connection);

        // Assert
        result.ShouldBeTrue();
        _graph.Connections.Count.ShouldBe(0);
        _sourceConnector.Connections.Count.ShouldBe(0);
        _targetConnector.Connections.Count.ShouldBe(0);
    }

    [Fact]
    public void GetNodeById_ShouldReturnCorrectNode()
    {
        // Arrange
        _graph.AddNode(_node1);

        // Act
        var foundNode = _graph.GetNodeById(_node1.Id);

        // Assert
        foundNode.ShouldNotBeNull();
        foundNode.ShouldBe(_node1);
    }

    [Fact]
    public void ValidateGraph_ShouldDetectCircularReferences()
    {
        // Arrange
        var node3 = new Node();
        var connector1 = new Connector(node3, ConnectorDirection.Output, typeof(string));
        var connector2 = new Connector(_node1, ConnectorDirection.Input, typeof(string));
        node3.AddOutputConnector(connector1);
        _node1.AddInputConnector(connector2);

        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        _graph.AddNode(node3);

        // Create a cycle: node1 -> node2 -> node3 -> node1
        _graph.AddConnection(_sourceConnector, _targetConnector);
        _graph.AddConnection(new Connector(_node2, ConnectorDirection.Output, typeof(string)),
                           new Connector(node3, ConnectorDirection.Input, typeof(string)));
        _graph.AddConnection(connector1, connector2);

        // Act
        var isValid = _graph.Validate();

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void ValidateGraph_ShouldPassForValidGraph()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        _graph.AddConnection(_sourceConnector, _targetConnector);

        // Act
        var isValid = _graph.Validate();

        // Assert
        isValid.ShouldBeTrue();
    }
}