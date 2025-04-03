using Nodify.Workflow.Core.Interfaces;
using NSubstitute;
using Shouldly;

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
    public void Constructor_ShouldThrowOnInvalidSourceDirection()
    {
        // Arrange
        var source = CreateInputConnector();  // Wrong direction
        var target = CreateInputConnector();

        // Act & Assert
        Should.Throw<ArgumentException>(() => new Connection(source, target));
    }

    [Fact]
    public void Constructor_ShouldThrowOnInvalidTargetDirection()
    {
        // Arrange
        var source = CreateOutputConnector();
        var target = CreateOutputConnector();  // Wrong direction

        // Act & Assert
        Should.Throw<ArgumentException>(() => new Connection(source, target));
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

    [Fact]
    public void DetectDirectCircularReference()
    {
        // Arrange
        var node1 = Substitute.For<INode>();
        var node2 = Substitute.For<INode>();
        var source = CreateOutputConnector(node1);
        var target = CreateInputConnector(node2);

        // Configure validation to pass
        source.ValidateConnection(target).Returns(true);
        target.ValidateConnection(source).Returns(true);

        node1.OutputConnectors.Returns(new[] { source });
        node2.OutputConnectors.Returns(new IConnector[0]);

        var connection = new Connection(source, target);

        // Act
        var result = connection.WouldCreateCircularReference();

        // Assert
        result.ShouldBeFalse();  // No circular reference yet
    }

    [Fact]
    public void DetectIndirectCircularReference()
    {
        // Arrange
        var node1 = Substitute.For<INode>();
        var node2 = Substitute.For<INode>();
        var node3 = Substitute.For<INode>();

        var source1 = CreateOutputConnector(node1);
        var target1 = CreateInputConnector(node2);
        var source2 = CreateOutputConnector(node2);
        var target2 = CreateInputConnector(node3);
        var source3 = CreateOutputConnector(node3);
        var target3 = CreateInputConnector(node1);

        // Configure validation to pass for all connections
        source1.ValidateConnection(target1).Returns(true);
        target1.ValidateConnection(source1).Returns(true);
        source2.ValidateConnection(target2).Returns(true);
        target2.ValidateConnection(source2).Returns(true);
        source3.ValidateConnection(target3).Returns(true);
        target3.ValidateConnection(source3).Returns(true);

        // Create connections in sequence
        var connection1 = new Connection(source1, target1);
        var connection2 = new Connection(source2, target2);

        // Set up the node connections
        node1.OutputConnectors.Returns(new[] { source1 });
        node1.InputConnectors.Returns(new[] { target3 });
        node2.OutputConnectors.Returns(new[] { source2 });
        node2.InputConnectors.Returns(new[] { target1 });
        node3.OutputConnectors.Returns(new[] { source3 });
        node3.InputConnectors.Returns(new[] { target2 });

        // Set up the existing connections
        source1.Connections.Returns(new[] { connection1 });
        source2.Connections.Returns(new[] { connection2 });
        target1.Connections.Returns(new[] { connection1 });
        target2.Connections.Returns(new[] { connection2 });
        source3.Connections.Returns(new IConnection[] { });
        target3.Connections.Returns(new IConnection[] { });

        // Act - attempt to create a connection that would complete the circle
        var connection3 = new Connection(source3, target3);
        var result = connection3.WouldCreateCircularReference();

        // Assert
        result.ShouldBeTrue();
    }

    private IConnector CreateInputConnector(INode node = null)
    {
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);
        connector.DataType.Returns(typeof(string));
        connector.ParentNode.Returns(node ?? Substitute.For<INode>());
        connector.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        return connector;
    }

    private IConnector CreateOutputConnector(INode node = null)
    {
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);
        connector.DataType.Returns(typeof(string));
        connector.ParentNode.Returns(node ?? Substitute.For<INode>());
        connector.ValidateConnection(Arg.Any<IConnector>()).Returns(true);
        return connector;
    }
}