using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using NSubstitute;
using Shouldly;
using Xunit;
using Nodify.Workflow.Tests.Core.Models.Helpers;

namespace Nodify.Workflow.Tests.Core.Models;

public class NodeTests
{
    [Fact]
    public void Node_ShouldHaveUniqueId()
    {
        // Arrange & Act
        var node1 = new TestNode();
        var node2 = new TestNode();

        // Assert
        node1.Id.ShouldNotBe(Guid.Empty);
        node2.Id.ShouldNotBe(Guid.Empty);
        node1.Id.ShouldNotBe(node2.Id);
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyConnectorCollections()
    {
        // Arrange & Act
        var node = new TestNode();

        // Assert
        node.InputConnectors.ShouldNotBeNull();
        node.OutputConnectors.ShouldNotBeNull();
        node.InputConnectors.Count.ShouldBe(0);
        node.OutputConnectors.Count.ShouldBe(0);
    }

    [Fact]
    public void AddInputConnector_ShouldAddConnector_WhenDirectionIsInput()
    {
        // Arrange
        var node = new TestNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);

        // Act
        node.AddInputConnector(connector);

        // Assert
        node.InputConnectors.ShouldContain(connector);
    }

    [Fact]
    public void AddInputConnector_ShouldThrow_WhenDirectionIsOutput()
    {
        // Arrange
        var node = new TestNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);

        // Act & Assert
        Should.Throw<ArgumentException>(() => node.AddInputConnector(connector));
    }

    [Fact]
    public void AddOutputConnector_ShouldAddConnector_WhenDirectionIsOutput()
    {
        // Arrange
        var node = new TestNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);

        // Act
        node.AddOutputConnector(connector);

        // Assert
        node.OutputConnectors.ShouldContain(connector);
    }

    [Fact]
    public void AddOutputConnector_ShouldThrow_WhenDirectionIsInput()
    {
        // Arrange
        var node = new TestNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);

        // Act & Assert
        Should.Throw<ArgumentException>(() => node.AddOutputConnector(connector));
    }

    [Fact]
    public void RemoveConnector_ShouldRemoveInputConnector()
    {
        // Arrange
        var node = new TestNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);
        node.AddInputConnector(connector);

        // Act
        var result = node.RemoveConnector(connector);

        // Assert
        result.ShouldBeTrue();
        node.InputConnectors.ShouldNotContain(connector);
    }

    [Fact]
    public void RemoveConnector_ShouldRemoveOutputConnector()
    {
        // Arrange
        var node = new TestNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);
        node.AddOutputConnector(connector);

        // Act
        var result = node.RemoveConnector(connector);

        // Assert
        result.ShouldBeTrue();
        node.OutputConnectors.ShouldNotContain(connector);
    }

    [Fact]
    public void RemoveConnector_ShouldReturnFalse_WhenConnectorNotFound()
    {
        // Arrange
        var node = new TestNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);

        // Act
        var result = node.RemoveConnector(connector);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetInputConnector_ShouldReturnConnector_WhenIdMatches()
    {
        // Arrange
        var node = new TestNode();
        var connectorId = Guid.NewGuid();
        var connector = Substitute.For<IConnector>();
        connector.Id.Returns(connectorId);
        connector.Direction.Returns(ConnectorDirection.Input);
        node.AddInputConnector(connector);

        // Act
        var foundConnector = node.GetInputConnector(connectorId);

        // Assert
        foundConnector.ShouldBe(connector);
    }

    [Fact]
    public void GetInputConnector_ShouldReturnNull_WhenIdNotFound()
    {
        // Arrange
        var node = new TestNode();

        // Act
        var foundConnector = node.GetInputConnector(Guid.NewGuid());

        // Assert
        foundConnector.ShouldBeNull();
    }

    [Fact]
    public void GetOutputConnector_ShouldReturnConnector_WhenIdMatches()
    {
        // Arrange
        var node = new TestNode();
        var connectorId = Guid.NewGuid();
        var connector = Substitute.For<IConnector>();
        connector.Id.Returns(connectorId);
        connector.Direction.Returns(ConnectorDirection.Output);
        node.AddOutputConnector(connector);

        // Act
        var foundConnector = node.GetOutputConnector(connectorId);

        // Assert
        foundConnector.ShouldBe(connector);
    }

    [Fact]
    public void GetOutputConnector_ShouldReturnNull_WhenIdNotFound()
    {
        // Arrange
        var node = new TestNode();

        // Act
        var foundConnector = node.GetOutputConnector(Guid.NewGuid());

        // Assert
        foundConnector.ShouldBeNull();
    }

    [Fact]
    public void Validate_ShouldReturnTrue_WhenConnectorsAreValid()
    {
        // Arrange
        var node = new TestNode();
        var inputConnector = Substitute.For<IConnector>();
        inputConnector.ParentNode.Returns(node);
        inputConnector.Direction.Returns(ConnectorDirection.Input);

        var outputConnector = Substitute.For<IConnector>();
        outputConnector.ParentNode.Returns(node);
        outputConnector.Direction.Returns(ConnectorDirection.Output);

        node.AddInputConnector(inputConnector);
        node.AddOutputConnector(outputConnector);

        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenInputConnectorIsInvalid()
    {
        // Arrange
        var node = new TestNode();
        var otherNode = new TestNode();
        var inputConnector = Substitute.For<IConnector>();
        inputConnector.ParentNode.Returns(otherNode);
        inputConnector.Direction.Returns(ConnectorDirection.Input);
        node.AddInputConnector(inputConnector);

        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenOutputConnectorIsInvalid()
    {
        // Arrange
        var node = new TestNode();
        var otherNode = new TestNode();
        var outputConnector = Substitute.For<IConnector>();
        outputConnector.ParentNode.Returns(otherNode);
        outputConnector.Direction.Returns(ConnectorDirection.Output);
        node.AddOutputConnector(outputConnector);

        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void Position_ShouldBeSettable()
    {
        // Arrange
        var node = new TestNode();
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