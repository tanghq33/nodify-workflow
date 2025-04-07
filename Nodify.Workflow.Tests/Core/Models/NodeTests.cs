using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using NSubstitute;
using Shouldly;
using Xunit;
using Nodify.Workflow.Tests.Core.Models.Helpers;

namespace Nodify.Workflow.Tests.Core.Models;

// Helper class for testing Node base functionality
internal class TestableNode : Node
{
    // Implement the abstract method minimally
    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        // Not relevant for base Node tests, return simple success
        return Task.FromResult(NodeExecutionResult.Succeeded());
    }
}

public class NodeTests
{
    [Fact]
    public void Constructor_ShouldAssignUniqueId()
    {
        // Arrange & Act
        var node1 = new TestableNode();
        var node2 = new TestableNode();

        // Assert
        node1.Id.ShouldNotBe(Guid.Empty);
        node2.Id.ShouldNotBe(Guid.Empty);
        node1.Id.ShouldNotBe(node2.Id);
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyConnectorCollections()
    {
        // Arrange & Act
        var node = new TestableNode();

        // Assert
        node.InputConnectors.ShouldNotBeNull();
        node.OutputConnectors.ShouldNotBeNull();
        node.InputConnectors.ShouldBeEmpty();
        node.OutputConnectors.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldInitializeCoordinatesToDefault()
    {
        // Arrange & Act
        var node = new TestableNode();

        // Assert
        // Default double is 0.0
        node.X.ShouldBe(0.0);
        node.Y.ShouldBe(0.0);
    }

    [Fact]
    public void SetX_ShouldUpdateXValue()
    {
        // Arrange
        var node = new TestableNode();
        double expectedX = 123.45;

        // Act
        node.X = expectedX;

        // Assert
        node.X.ShouldBe(expectedX);
    }

    [Fact]
    public void SetY_ShouldUpdateYValue()
    {
        // Arrange
        var node = new TestableNode();
        double expectedY = -50.2;

        // Act
        node.Y = expectedY;

        // Assert
        node.Y.ShouldBe(expectedY);
    }

    [Fact]
    public void AddInputConnector_WhenValid_ShouldAddToInputConnectors()
    {
        // Arrange
        var node = new TestableNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);

        // Act
        node.AddInputConnector(connector);

        // Assert
        node.InputConnectors.Count.ShouldBe(1);
        node.InputConnectors.ShouldContain(connector);
        node.OutputConnectors.ShouldBeEmpty();
    }

    [Fact]
    public void AddInputConnector_WhenOutputConnector_ShouldThrowArgumentException()
    {
        // Arrange
        var node = new TestableNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => node.AddInputConnector(connector));
        ex.Message.ShouldContain("input connector");
        node.InputConnectors.ShouldBeEmpty();
    }

    [Fact]
    public void AddOutputConnector_WhenValid_ShouldAddToOutputConnectors()
    {
        // Arrange
        var node = new TestableNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);

        // Act
        node.AddOutputConnector(connector);

        // Assert
        node.OutputConnectors.Count.ShouldBe(1);
        node.OutputConnectors.ShouldContain(connector);
        node.InputConnectors.ShouldBeEmpty();
    }

    [Fact]
    public void AddOutputConnector_WhenInputConnector_ShouldThrowArgumentException()
    {
        // Arrange
        var node = new TestableNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => node.AddOutputConnector(connector));
        ex.Message.ShouldContain("output connector");
        node.OutputConnectors.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveConnector_WhenInputConnectorExists_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var node = new TestableNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Input);
        node.AddInputConnector(connector);
        node.InputConnectors.Count.ShouldBe(1); // Pre-check

        // Act
        var result = node.RemoveConnector(connector);

        // Assert
        result.ShouldBeTrue();
        node.InputConnectors.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveConnector_WhenOutputConnectorExists_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var node = new TestableNode();
        var connector = Substitute.For<IConnector>();
        connector.Direction.Returns(ConnectorDirection.Output);
        node.AddOutputConnector(connector);
        node.OutputConnectors.Count.ShouldBe(1); // Pre-check

        // Act
        var result = node.RemoveConnector(connector);

        // Assert
        result.ShouldBeTrue();
        node.OutputConnectors.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveConnector_WhenConnectorDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var node = new TestableNode();
        var inputConnector = Substitute.For<IConnector>();
        inputConnector.Direction.Returns(ConnectorDirection.Input);
        var outputConnector = Substitute.For<IConnector>();
        outputConnector.Direction.Returns(ConnectorDirection.Output);

        // Act
        var resultInput = node.RemoveConnector(inputConnector);
        var resultOutput = node.RemoveConnector(outputConnector);

        // Assert
        resultInput.ShouldBeFalse();
        resultOutput.ShouldBeFalse();
        node.InputConnectors.ShouldBeEmpty();
        node.OutputConnectors.ShouldBeEmpty();
    }

    [Fact]
    public void GetInputConnector_WhenIdExists_ShouldReturnConnector()
    {
        // Arrange
        var node = new TestableNode();
        var targetId = Guid.NewGuid();
        var connector1 = Substitute.For<IConnector>();
        connector1.Id.Returns(Guid.NewGuid());
        connector1.Direction.Returns(ConnectorDirection.Input);
        var connector2 = Substitute.For<IConnector>();
        connector2.Id.Returns(targetId);
        connector2.Direction.Returns(ConnectorDirection.Input);
        node.AddInputConnector(connector1);
        node.AddInputConnector(connector2);

        // Act
        var foundConnector = node.GetInputConnector(targetId);

        // Assert
        foundConnector.ShouldNotBeNull();
        foundConnector.ShouldBe(connector2);
    }

    [Fact]
    public void GetInputConnector_WhenIdDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var node = new TestableNode();
        var connector1 = Substitute.For<IConnector>();
        connector1.Id.Returns(Guid.NewGuid());
        connector1.Direction.Returns(ConnectorDirection.Input);
        node.AddInputConnector(connector1);
        var missingId = Guid.NewGuid();

        // Act
        var foundConnector = node.GetInputConnector(missingId);

        // Assert
        foundConnector.ShouldBeNull();
    }

    [Fact]
    public void GetOutputConnector_WhenIdExists_ShouldReturnConnector()
    {
        // Arrange
        var node = new TestableNode();
        var targetId = Guid.NewGuid();
        var connector1 = Substitute.For<IConnector>();
        connector1.Id.Returns(Guid.NewGuid());
        connector1.Direction.Returns(ConnectorDirection.Output);
        var connector2 = Substitute.For<IConnector>();
        connector2.Id.Returns(targetId);
        connector2.Direction.Returns(ConnectorDirection.Output);
        node.AddOutputConnector(connector1);
        node.AddOutputConnector(connector2);

        // Act
        var foundConnector = node.GetOutputConnector(targetId);

        // Assert
        foundConnector.ShouldNotBeNull();
        foundConnector.ShouldBe(connector2);
    }

    [Fact]
    public void GetOutputConnector_WhenIdDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var node = new TestableNode();
        var connector1 = Substitute.For<IConnector>();
        connector1.Id.Returns(Guid.NewGuid());
        connector1.Direction.Returns(ConnectorDirection.Output);
        node.AddOutputConnector(connector1);
        var missingId = Guid.NewGuid();

        // Act
        var foundConnector = node.GetOutputConnector(missingId);

        // Assert
        foundConnector.ShouldBeNull();
    }

    [Fact]
    public void Validate_WhenConnectorsAreValid_ShouldReturnTrue()
    {
        // Arrange
        var node = new TestableNode();
        var inputConnector = Substitute.For<IConnector>();
        inputConnector.ParentNode.Returns(node); // Connector belongs to this node
        inputConnector.Direction.Returns(ConnectorDirection.Input);

        var outputConnector = Substitute.For<IConnector>();
        outputConnector.ParentNode.Returns(node); // Connector belongs to this node
        outputConnector.Direction.Returns(ConnectorDirection.Output);

        node.AddInputConnector(inputConnector);
        node.AddOutputConnector(outputConnector);

        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenInputConnectorHasWrongParent_ShouldReturnFalse()
    {
        // Arrange
        var node = new TestableNode();
        var otherNode = new TestableNode(); // A different node instance
        var inputConnector = Substitute.For<IConnector>();
        inputConnector.ParentNode.Returns(otherNode); // Connector belongs to otherNode
        inputConnector.Direction.Returns(ConnectorDirection.Input);

        node.AddInputConnector(inputConnector);

        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WhenOutputConnectorHasWrongParent_ShouldReturnFalse()
    {
        // Arrange
        var node = new TestableNode();
        var otherNode = new TestableNode(); // A different node instance
        var outputConnector = Substitute.For<IConnector>();
        outputConnector.ParentNode.Returns(otherNode); // Connector belongs to otherNode
        outputConnector.Direction.Returns(ConnectorDirection.Output);

        node.AddOutputConnector(outputConnector);

        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WhenConnectorsAreEmpty_ShouldReturnTrue()
    {
        // Arrange
        var node = new TestableNode();

        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeTrue();
    }
}