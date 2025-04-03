using System;
using System.Linq;
using Shouldly;
using Xunit;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Interfaces;
using NSubstitute;
using System.Collections.Generic;
using Nodify.Workflow.Tests.Core.Models.Helpers; // Use TestNode
using Nodify.Workflow.Core; // For OperationResult

namespace Nodify.Workflow.Tests.Core.Models;

public class GraphTests
{
    private Graph _graph;
    private INode _node1;
    private INode _node2;
    private IConnector _sourceConnector;
    private IConnector _targetConnector;

    public GraphTests()
    {
        InitializeGraph(); // Call initialization here
    }

    private void InitializeGraph()
    {
        _graph = new Graph();
        _node1 = new TestNode { X = 10, Y = 10 };
        _node2 = new TestNode { X = 200, Y = 10 };
        _sourceConnector = new Connector(_node1, ConnectorDirection.Output, typeof(string));
        _targetConnector = new Connector(_node2, ConnectorDirection.Input, typeof(string));
        _node1.AddOutputConnector(_sourceConnector);
        _node2.AddInputConnector(_targetConnector);
        _graph.AddNode(_node1);
        _graph.AddNode(_node2);
    }

    // --- Node Tests --- 

    [Fact]
    public void AddNode_ShouldAddNodeToGraph()
    {
        // Assert
        _graph.Nodes.Count.ShouldBe(2);
        _graph.Nodes.ShouldContain(_node1);
        _graph.Nodes.ShouldContain(_node2);
    }

    [Fact]
    public void AddNode_ShouldReturnFalse_WhenNodeIsNull()
    {
        // Act & Assert
        _graph.AddNode(null!).ShouldBeFalse();
    }

    [Fact]
    public void TryAddNode_ShouldReturnFailure_WhenNodeIsNull()
    {
        // Act
        var result = _graph.TryAddNode(null!);
        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void TryAddNode_ShouldReturnFailure_WhenNodeIdExists()
    {
        // Act
        var result = _graph.TryAddNode(_node1); // Try adding existing node
        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void RemoveNode_ShouldRemoveNodeAndConnections()
    {
        // Arrange: Add connection
        var addResult = _graph.TryAddConnection(_sourceConnector, _targetConnector);
        addResult.Success.ShouldBeTrue();
        _graph.Connections.Count.ShouldBe(1);

        // Act
        var removeResult = _graph.RemoveNode(_node1);

        // Assert
        removeResult.ShouldBeTrue();
        _graph.Nodes.Count.ShouldBe(1);
        _graph.Nodes.ShouldNotContain(_node1);
        _graph.Connections.ShouldBeEmpty();
        _sourceConnector.Connections.ShouldBeEmpty(); // Verify connector cleanup
        _targetConnector.Connections.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveNode_ShouldReturnFalse_WhenNodeNotFound()
    {
        // Arrange
        var nonExistentNode = new TestNode();
        // Act
        var result = _graph.RemoveNode(nonExistentNode);
        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryRemoveNode_ShouldRemoveNodeAndConnections()
    {
        // Arrange: Add connection
        var addResult = _graph.TryAddConnection(_sourceConnector, _targetConnector);
        addResult.Success.ShouldBeTrue();
        _graph.Connections.Count.ShouldBe(1);

        // Act
        var removeResult = _graph.TryRemoveNode(_node1);

        // Assert
        removeResult.Success.ShouldBeTrue();
        removeResult.Result.ShouldBeTrue();
        _graph.Nodes.Count.ShouldBe(1);
        _graph.Nodes.ShouldNotContain(_node1);
        _graph.Connections.ShouldBeEmpty();
        _sourceConnector.Connections.ShouldBeEmpty();
        _targetConnector.Connections.ShouldBeEmpty();
    }

    [Fact]
    public void TryRemoveNode_ShouldReturnFailure_WhenNodeNotFound()
    {
        // Arrange
        var nonExistentNode = new TestNode();
        // Act
        var result = _graph.TryRemoveNode(nonExistentNode);
        // Assert
        result.Success.ShouldBeFalse();
        result.Result.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void GetNodeById_ShouldReturnNode_WhenFound()
    {
        // Act
        var foundNode = _graph.GetNodeById(_node1.Id);
        // Assert
        foundNode.ShouldBe(_node1);
    }

    [Fact]
    public void GetNodeById_ShouldReturnNull_WhenNotFound()
    {
        // Act
        var foundNode = _graph.GetNodeById(Guid.NewGuid());
        // Assert
        foundNode.ShouldBeNull();
    }

    // --- Connection Tests --- 

    [Fact]
    public void AddConnection_ShouldAddConnection_WhenValid()
    {
        // Act
        var connection = _graph.AddConnection(_sourceConnector, _targetConnector);
        // Assert
        connection.ShouldNotBeNull();
        _graph.Connections.Count.ShouldBe(1);
        _graph.Connections.ShouldContain(connection);
        _sourceConnector.Connections.ShouldContain(connection);
        _targetConnector.Connections.ShouldContain(connection);
    }

    [Fact]
    public void AddConnection_ShouldReturnNull_WhenInvalid()
    {
        // Arrange
        var invalidTarget = new Connector(_node2, ConnectorDirection.Input, typeof(int));
        _node2.AddInputConnector(invalidTarget);
        // Act
        var connection = _graph.AddConnection(_sourceConnector, invalidTarget);
        // Assert
        connection.ShouldBeNull();
        _graph.Connections.ShouldBeEmpty();
    }

    [Fact]
    public void TryAddConnection_ShouldAddConnection_WhenValid()
    {
        // Act
        var result = _graph.TryAddConnection(_sourceConnector, _targetConnector);
        // Assert
        result.Success.ShouldBeTrue();
        result.Result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBeEmpty();
        _graph.Connections.Count.ShouldBe(1);
        _graph.Connections.ShouldContain(result.Result);
    }

    [Fact]
    public void TryAddConnection_ShouldReturnFailure_WhenInvalid()
    {
        // Arrange
        var invalidTarget = new Connector(_node2, ConnectorDirection.Input, typeof(int));
        _node2.AddInputConnector(invalidTarget);
        // Act
        var result = _graph.TryAddConnection(_sourceConnector, invalidTarget);
        // Assert
        result.Success.ShouldBeFalse();
        result.Result.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
        _graph.Connections.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveConnection_ShouldRemoveConnection()
    {
        // Arrange
        var connection = _graph.AddConnection(_sourceConnector, _targetConnector);
        _graph.Connections.Count.ShouldBe(1);
        // Act
        var result = _graph.RemoveConnection(connection!);
        // Assert
        result.ShouldBeTrue();
        _graph.Connections.ShouldBeEmpty();
        _sourceConnector.Connections.ShouldBeEmpty();
        _targetConnector.Connections.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveConnection_ShouldReturnFalse_WhenConnectionNotFound()
    {
        // Arrange
        var nonExistentConnection = new Connection(_sourceConnector, _targetConnector);
        // Act
        var result = _graph.RemoveConnection(nonExistentConnection);
        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryRemoveConnection_ShouldRemoveConnection()
    {
        // Arrange
        var addResult = _graph.TryAddConnection(_sourceConnector, _targetConnector);
        addResult.Success.ShouldBeTrue();
        _graph.Connections.Count.ShouldBe(1);
        // Act
        var removeResult = _graph.TryRemoveConnection(addResult.Result!);
        // Assert
        removeResult.Success.ShouldBeTrue();
        removeResult.Result.ShouldBeTrue();
        _graph.Connections.ShouldBeEmpty();
        _sourceConnector.Connections.ShouldBeEmpty();
        _targetConnector.Connections.ShouldBeEmpty();
    }

    [Fact]
    public void TryRemoveConnection_ShouldReturnFailure_WhenConnectionNotFound()
    {
        // Arrange
        var nonExistentConnection = new Connection(_sourceConnector, _targetConnector);
        // Act
        var result = _graph.TryRemoveConnection(nonExistentConnection);
        // Assert
        result.Success.ShouldBeFalse();
        result.Result.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // --- Validation Tests --- 
    // Assuming Validate and TryValidate exist
    [Fact]
    public void Validate_ShouldReturnTrue_WhenGraphIsValid()
    {
        // Arrange: Add a valid connection
        _graph.AddConnection(_sourceConnector, _targetConnector);
        // Act
        var isValid = _graph.Validate();
        // Assert
        isValid.ShouldBeTrue();
    }

    // Add more validation tests if needed (e.g., invalid nodes/connections)
}