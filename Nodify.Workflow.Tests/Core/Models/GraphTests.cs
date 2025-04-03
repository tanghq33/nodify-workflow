using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using NSubstitute;
using Shouldly;
using Xunit;

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
    public void TryAddNode_ShouldProvideDetailedResult()
    {
        // Act
        var result = _graph.TryAddNode(_node1);

        // Assert
        result.Success.ShouldBeTrue();
        result.Result.ShouldBe(_node1);
        result.ErrorMessage.ShouldBeEmpty();
    }

    [Fact]
    public void AddNode_ShouldHandleNullNode()
    {
        // Act
        var result = _graph.AddNode(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryAddNode_ShouldProvideErrorForNullNode()
    {
        // Act
        var result = _graph.TryAddNode(null);

        // Assert
        result.Success.ShouldBeFalse();
        result.Result.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Node cannot be null.");
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
    public void TryRemoveNode_ShouldProvideDetailedResult()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        _graph.AddConnection(_sourceConnector, _targetConnector);

        // Act
        var result = _graph.TryRemoveNode(_node1);

        // Assert
        result.Success.ShouldBeTrue();
        result.Result.ShouldBeTrue();
        result.ErrorMessage.ShouldBeEmpty();
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
    public void TryRemoveNode_ShouldProvideErrorForNonExistentNode()
    {
        // Arrange
        var nonExistentNode = new Node();

        // Act
        var result = _graph.TryRemoveNode(nonExistentNode);

        // Assert
        result.Success.ShouldBeFalse();
        result.Result.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Node not found in graph.");
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
    public void TryAddConnection_ShouldProvideDetailedResult()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);

        // Act
        var result = _graph.TryAddConnection(_sourceConnector, _targetConnector);

        // Assert
        result.Success.ShouldBeTrue();
        result.Result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBeEmpty();
        result.Result.Source.ShouldBe(_sourceConnector);
        result.Result.Target.ShouldBe(_targetConnector);
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
    public void TryAddConnection_ShouldProvideErrorForInvalidConnection()
    {
        // Arrange
        var invalidConnector = new Connector(_node2, ConnectorDirection.Input, typeof(int));
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);

        // Act
        var result = _graph.TryAddConnection(_sourceConnector, invalidConnector);

        // Assert
        result.Success.ShouldBeFalse();
        result.Result.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeEmpty();
    }

    [Fact]
    public void TryAddConnection_ShouldFailForSourceInputConnector()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        var invalidSource = new Connector(_node1, ConnectorDirection.Input, typeof(string)); // Invalid direction
        _node1.AddInputConnector(invalidSource);

        // Act
        var result = _graph.TryAddConnection(invalidSource, _targetConnector);

        // Assert
        result.Success.ShouldBeFalse();
        result.Result.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Source connector must be an output.");
    }

    [Fact]
    public void TryAddConnection_ShouldFailForTargetOutputConnector()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        var invalidTarget = new Connector(_node2, ConnectorDirection.Output, typeof(string)); // Invalid direction
        _node2.AddOutputConnector(invalidTarget);

        // Act
        var result = _graph.TryAddConnection(_sourceConnector, invalidTarget);

        // Assert
        result.Success.ShouldBeFalse();
        result.Result.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Target connector must be an input.");
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
    public void TryRemoveConnection_ShouldProvideDetailedResult()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        var connection = _graph.AddConnection(_sourceConnector, _targetConnector);

        // Act
        var result = _graph.TryRemoveConnection(connection);

        // Assert
        result.Success.ShouldBeTrue();
        result.Result.ShouldBeTrue();
        result.ErrorMessage.ShouldBeEmpty();
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
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        _graph.AddConnection(_sourceConnector, _targetConnector); // Connection 1: Node1 -> Node2

        // Create connectors for connection 2: Node2 -> Node1 (circular)
        var sourceConnector2 = new Connector(_node2, ConnectorDirection.Output, typeof(string));
        var targetConnector2 = new Connector(_node1, ConnectorDirection.Input, typeof(string));
        _node2.AddOutputConnector(sourceConnector2);
        _node1.AddInputConnector(targetConnector2);

        // Act: Attempt to add the connection that would create the cycle
        var cycleConnectionResult = _graph.TryAddConnection(sourceConnector2, targetConnector2);

        // Assert: The attempt to add the cycle connection should fail
        cycleConnectionResult.Success.ShouldBeFalse();
        cycleConnectionResult.ErrorMessage.ShouldContain("circular reference");

        // Act: Validate the graph state *after* the failed connection attempt
        // The graph should still be valid as the cyclic connection wasn't added.
        var isValid = _graph.Validate();
        var validationResult = _graph.TryValidate();

        // Assert: Graph validation should pass
        isValid.ShouldBeTrue(); 
        validationResult.Success.ShouldBeTrue();
        validationResult.ErrorMessage.ShouldBe("Graph validation successful.");
    }

    [Fact]
    public void TryValidate_ShouldProvideDetailedValidationResult()
    {
        // Arrange: Create an invalid graph state
        // Add node1 but not node2
        _graph.AddNode(_node1);
        // Create a connection referencing node2 which is not in the graph
        // We need to bypass Graph.AddConnection as it checks node existence.
        // Manually create connection and add to connectors (simulate inconsistent state)
        var conn = new Connection(_sourceConnector, _targetConnector); 
        // Add to graph's internal collection (This is tricky, ideally we test Graph methods)
        // Let's test by adding an invalid node instead
        _graph.AddNode(_node1);
        var invalidNode = Substitute.For<INode>();
        invalidNode.Validate().Returns(false);
        invalidNode.Id.Returns(Guid.NewGuid());
        _graph.AddNode(invalidNode); // Add the invalid node

        // Act
        var result = _graph.TryValidate();

        // Assert
        result.Success.ShouldBeFalse(); // Should fail validation
        result.Result.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Invalid nodes found"); // Check for the specific error
        result.ErrorMessage.ShouldContain(invalidNode.Id.ToString());
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

    [Fact]
    public void TryValidate_ShouldProvideDetailedResultForValidGraph()
    {
        // Arrange
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
        _graph.AddConnection(_sourceConnector, _targetConnector);

        // Act
        var result = _graph.TryValidate();

        // Assert
        result.Success.ShouldBeTrue();
        result.Result.ShouldBeTrue();
        result.ErrorMessage.ShouldBe("Graph validation successful.");
    }
}