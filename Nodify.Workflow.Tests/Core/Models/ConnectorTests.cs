using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Nodify.Workflow.Tests.Core.Models;

public class ConnectorTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithUniqueId()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var connector1 = new Connector(node, ConnectorDirection.Input, typeof(string));
        var connector2 = new Connector(node, ConnectorDirection.Input, typeof(string));

        // Assert
        connector1.Id.ShouldNotBe(Guid.Empty);
        connector2.Id.ShouldNotBe(Guid.Empty);
        connector1.Id.ShouldNotBe(connector2.Id);
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var direction = ConnectorDirection.Input;
        var dataType = typeof(string);

        // Act
        var connector = new Connector(node, direction, dataType);

        // Assert
        connector.ParentNode.ShouldBe(node);
        connector.Direction.ShouldBe(direction);
        connector.DataType.ShouldBe(dataType);
        connector.Connections.Count.ShouldBe(0);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullParentNode()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new Connector(null, ConnectorDirection.Input, typeof(string)));
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullDataType()
    {
        // Arrange
        var node = Substitute.For<INode>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new Connector(node, ConnectorDirection.Input, null));
    }

    [Fact]
    public void AddConnection_ShouldAddValidConnection()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var connector = new Connector(node, ConnectorDirection.Input, typeof(string));
        var otherConnector = new Connector(node, ConnectorDirection.Output, typeof(string));
        var connection = Substitute.For<IConnection>();

        connection.Source.Returns(otherConnector);
        connection.Target.Returns(connector);

        // Act
        var result = connector.AddConnection(connection);

        // Assert
        result.ShouldBeTrue();
        connector.Connections.Count.ShouldBe(1);
        connector.Connections.ShouldContain(connection);
    }

    [Fact]
    public void AddConnection_ShouldThrowOnNullConnection()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var connector = new Connector(node, ConnectorDirection.Input, typeof(string));

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => connector.AddConnection(null));
    }

    [Fact]
    public void AddConnection_ShouldRejectInvalidConnection()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var connector = new Connector(node, ConnectorDirection.Input, typeof(string));
        var connection = Substitute.For<IConnection>();
        var invalidConnector = new Connector(node, ConnectorDirection.Input, typeof(int));

        connection.Source.Returns(invalidConnector);

        // Act
        var result = connector.AddConnection(connection);

        // Assert
        result.ShouldBeFalse();
        connector.Connections.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveConnection_ShouldRemoveConnection()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var connector = new Connector(node, ConnectorDirection.Input, typeof(string));
        var otherConnector = new Connector(node, ConnectorDirection.Output, typeof(string));
        var connection = Substitute.For<IConnection>();

        connection.Source.Returns(otherConnector);
        connection.Target.Returns(connector);
        connector.AddConnection(connection);

        // Act
        var result = connector.RemoveConnection(connection);

        // Assert
        result.ShouldBeTrue();
        connector.Connections.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveConnection_ShouldReturnFalseForNull()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var connector = new Connector(node, ConnectorDirection.Input, typeof(string));

        // Act
        var result = connector.RemoveConnection(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ValidateConnection_ShouldAllowCompatibleTypes()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var input = new Connector(node, ConnectorDirection.Input, typeof(string));
        var output = new Connector(node, ConnectorDirection.Output, typeof(string));

        // Act
        var result = input.ValidateConnection(output);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ValidateConnection_ShouldAllowInheritanceCompatibleTypes()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var input = new Connector(node, ConnectorDirection.Input, typeof(object));
        var output = new Connector(node, ConnectorDirection.Output, typeof(string));

        // Act
        var result = input.ValidateConnection(output);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ValidateConnection_ShouldRejectIncompatibleTypes()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var input = new Connector(node, ConnectorDirection.Input, typeof(int));
        var output = new Connector(node, ConnectorDirection.Output, typeof(string));

        // Act
        var result = input.ValidateConnection(output);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ValidateConnection_ShouldRejectSameDirection()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var connector1 = new Connector(node, ConnectorDirection.Input, typeof(string));
        var connector2 = new Connector(node, ConnectorDirection.Input, typeof(string));

        // Act
        var result = connector1.ValidateConnection(connector2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ValidateConnection_ShouldRejectMultipleInputConnections()
    {
        // Arrange
        var node = Substitute.For<INode>();
        var input = new Connector(node, ConnectorDirection.Input, typeof(string));
        var output1 = new Connector(node, ConnectorDirection.Output, typeof(string));
        var output2 = new Connector(node, ConnectorDirection.Output, typeof(string));
        var connection = Substitute.For<IConnection>();

        connection.Source.Returns(output1);
        connection.Target.Returns(input);
        input.AddConnection(connection);

        // Act
        var result = input.ValidateConnection(output2);

        // Assert
        result.ShouldBeFalse();
    }
}