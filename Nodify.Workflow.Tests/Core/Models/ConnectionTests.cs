using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Nodify.Workflow.Tests.Core.Models;

public class ConnectionTests
{
    [Fact]
    public void Connection_ShouldHaveUniqueId()
    {
        // Arrange
        var source = CreateOutputConnector();
        var target = CreateInputConnector();

        // Configure validation to pass
        source.ValidateConnection(target).Returns(true);
        target.ValidateConnection(source).Returns(true);

        var connection1 = new Connection(source, target);
        var connection2 = new Connection(source, target);

        // Assert
        connection1.Id.ShouldNotBe(Guid.Empty);
        connection2.Id.ShouldNotBe(Guid.Empty);
        connection1.Id.ShouldNotBe(connection2.Id);
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var source = CreateOutputConnector();
        var target = CreateInputConnector();

        // Configure validation to pass
        source.ValidateConnection(target).Returns(true);
        target.ValidateConnection(source).Returns(true);

        // Act
        var connection = new Connection(source, target);

        // Assert
        connection.Source.ShouldBe(source);
        connection.Target.ShouldBe(target);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullSource()
    {
        // Arrange
        var target = CreateInputConnector();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new Connection(null, target));
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullTarget()
    {
        // Arrange
        var source = CreateOutputConnector();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new Connection(source, null));
    }

    [Fact]
    public void Validate_ShouldReturnTrueForValidConnection()
    {
        // Arrange
        var source = CreateOutputConnector();
        var target = CreateInputConnector();

        // Configure validation to pass
        source.ValidateConnection(target).Returns(true);
        target.ValidateConnection(source).Returns(true);

        var connection = new Connection(source, target);

        // Act
        var result = connection.Validate();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Remove_ShouldRemoveFromBothConnectors()
    {
        // Arrange
        var source = CreateOutputConnector();
        var target = CreateInputConnector();

        // Configure validation to pass
        source.ValidateConnection(target).Returns(true);
        target.ValidateConnection(source).Returns(true);

        var connection = new Connection(source, target);

        // Act
        connection.Remove();

        // Assert
        source.Received(1).RemoveConnection(connection);
        target.Received(1).RemoveConnection(connection);
    }

    private IConnector CreateInputConnector(INode? node = null)
    {
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);
        connector.DataType.Returns(typeof(string));
        connector.ParentNode.Returns(node ?? Substitute.For<INode>());
        connector.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        connector.AddConnection(Arg.Any<IConnection>()).Returns(true);
        return connector;
    }

    private IConnector CreateOutputConnector(INode? node = null)
    {
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);
        connector.DataType.Returns(typeof(string));
        connector.ParentNode.Returns(node ?? Substitute.For<INode>());
        connector.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        connector.AddConnection(Arg.Any<IConnection>()).Returns(true);
        return connector;
    }
}