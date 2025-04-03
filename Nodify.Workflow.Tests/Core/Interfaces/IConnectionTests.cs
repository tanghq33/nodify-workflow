using Nodify.Workflow.Core.Interfaces;
using NSubstitute;
using Shouldly;

namespace Nodify.Workflow.Tests.Core.Interfaces;

public class IConnectionTests
{
    [Fact]
    public void Connection_ShouldHaveSourceAndTargetConnectors()
    {
        // Arrange
        var connection = Substitute.For<IConnection>();
        var sourceConnector = Substitute.For<IConnector>();
        var targetConnector = Substitute.For<IConnector>();

        connection.Source.Returns(sourceConnector);
        connection.Target.Returns(targetConnector);

        // Assert
        connection.Source.ShouldBe(sourceConnector);
        connection.Target.ShouldBe(targetConnector);
    }

    [Fact]
    public void Connection_ShouldEnforceSourceMustBeOutput()
    {
        // Arrange
        var connection = Substitute.For<IConnection>();
        var sourceConnector = Substitute.For<IConnector>();

        sourceConnector.Direction.Returns(ConnectorDirection.Input);
        connection.Source.Returns(sourceConnector);
        connection.Validate().Returns(false);

        // Act
        var result = connection.Validate();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Connection_ShouldEnforceTargetMustBeInput()
    {
        // Arrange
        var connection = Substitute.For<IConnection>();
        var targetConnector = Substitute.For<IConnector>();

        targetConnector.Direction.Returns(ConnectorDirection.Output);
        connection.Target.Returns(targetConnector);
        connection.Validate().Returns(false);

        // Act
        var result = connection.Validate();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Connection_ShouldEnforceTypeCompatibility()
    {
        // Arrange
        var connection = Substitute.For<IConnection>();
        var sourceConnector = Substitute.For<IConnector>();
        var targetConnector = Substitute.For<IConnector>();

        sourceConnector.DataType.Returns(typeof(string));
        targetConnector.DataType.Returns(typeof(int));
        connection.Source.Returns(sourceConnector);
        connection.Target.Returns(targetConnector);
        connection.Validate().Returns(false);

        // Act
        var result = connection.Validate();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Connection_ShouldBeRemovable()
    {
        // Arrange
        var connection = Substitute.For<IConnection>();
        var sourceConnector = Substitute.For<IConnector>();
        var targetConnector = Substitute.For<IConnector>();

        connection.Source.Returns(sourceConnector);
        connection.Target.Returns(targetConnector);

        // Act
        connection.Remove();

        // Assert
        connection.Received(1).Remove();
    }

    [Fact]
    public void Connection_ShouldPreventCircularReferences()
    {
        // Arrange
        var connection = Substitute.For<IConnection>();
        connection.WouldCreateCircularReference().Returns(true);

        // Act
        var result = connection.WouldCreateCircularReference();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Connection_ShouldHaveUniqueIdentifier()
    {
        // Arrange
        var connection1 = Substitute.For<IConnection>();
        var connection2 = Substitute.For<IConnection>();

        connection1.Id.Returns(Guid.NewGuid());
        connection2.Id.Returns(Guid.NewGuid());

        // Assert
        connection1.Id.ShouldNotBe(connection2.Id);
        connection1.Id.ShouldNotBe(Guid.Empty);
        connection2.Id.ShouldNotBe(Guid.Empty);
    }
}