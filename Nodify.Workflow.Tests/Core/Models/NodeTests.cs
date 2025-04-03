using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using NSubstitute;
using Shouldly;

namespace Nodify.Workflow.Tests.Core.Models;

public class NodeTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithUniqueId()
    {
        // Arrange & Act
        var node1 = new Node();
        var node2 = new Node();

        // Assert
        node1.Id.ShouldNotBe(Guid.Empty);
        node2.Id.ShouldNotBe(Guid.Empty);
        node1.Id.ShouldNotBe(node2.Id);
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyConnectorCollections()
    {
        // Arrange & Act
        var node = new Node();

        // Assert
        node.InputConnectors.ShouldNotBeNull();
        node.OutputConnectors.ShouldNotBeNull();
        node.InputConnectors.Count.ShouldBe(0);
        node.OutputConnectors.Count.ShouldBe(0);
    }

    [Fact]
    public void AddInputConnector_ShouldAddValidInputConnector()
    {
        // Arrange
        var node = new Node();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);

        // Act
        node.AddInputConnector(connector);

        // Assert
        node.InputConnectors.Count.ShouldBe(1);
        node.InputConnectors.ShouldContain(connector);
    }

    [Fact]
    public void AddInputConnector_ShouldThrowOnNullConnector()
    {
        // Arrange
        var node = new Node();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => node.AddInputConnector(null));
    }

    [Fact]
    public void AddInputConnector_ShouldThrowOnOutputConnector()
    {
        // Arrange
        var node = new Node();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);

        // Act & Assert
        Should.Throw<ArgumentException>(() => node.AddInputConnector(connector));
    }

    [Fact]
    public void AddOutputConnector_ShouldAddValidOutputConnector()
    {
        // Arrange
        var node = new Node();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);

        // Act
        node.AddOutputConnector(connector);

        // Assert
        node.OutputConnectors.Count.ShouldBe(1);
        node.OutputConnectors.ShouldContain(connector);
    }

    [Fact]
    public void AddOutputConnector_ShouldThrowOnNullConnector()
    {
        // Arrange
        var node = new Node();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => node.AddOutputConnector(null));
    }

    [Fact]
    public void AddOutputConnector_ShouldThrowOnInputConnector()
    {
        // Arrange
        var node = new Node();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);

        // Act & Assert
        Should.Throw<ArgumentException>(() => node.AddOutputConnector(connector));
    }

    [Fact]
    public void RemoveConnector_ShouldRemoveAndCleanupConnections()
    {
        // Arrange
        var node = new Node();
        var connector = Substitute.For<IConnector>();
        var connection = Substitute.For<IConnection>();

        connector.Direction.Returns(ConnectorDirection.Input);
        connector.Connections.Returns(new[] { connection });

        node.AddInputConnector(connector);

        // Act
        var result = node.RemoveConnector(connector);

        // Assert
        result.ShouldBeTrue();
        node.InputConnectors.Count.ShouldBe(0);
        connection.Received(1).Remove();
    }

    [Fact]
    public void RemoveConnector_ShouldReturnFalseForNull()
    {
        // Arrange
        var node = new Node();

        // Act
        var result = node.RemoveConnector(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Validate_ShouldReturnTrueForValidNode()
    {
        // Arrange
        var node = new Node();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);
        connector.ParentNode.Returns(node);

        node.AddInputConnector(connector);

        // Act
        var result = node.Validate();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldReturnFalseForInvalidParentNode()
    {
        // Arrange
        var node = new Node();
        var otherNode = new Node();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);
        connector.ParentNode.Returns(otherNode);

        node.AddInputConnector(connector);

        // Act
        var result = node.Validate();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Position_ShouldBeSettable()
    {
        // Arrange
        var node = new Node();
        const double expectedX = 100.0;
        const double expectedY = 200.0;

        // Act
        node.X = expectedX;
        node.Y = expectedY;

        // Assert
        node.X.ShouldBe(expectedX);
        node.Y.ShouldBe(expectedY);
    }
}