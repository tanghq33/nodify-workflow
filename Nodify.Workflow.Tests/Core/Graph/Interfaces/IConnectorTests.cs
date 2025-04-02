using System;
using System.Linq;
using Xunit;
using NSubstitute;
using Shouldly;
using Nodify.Workflow.Core.Graph.Interfaces;

namespace Nodify.Workflow.Tests.Core.Graph.Interfaces
{
    public class IConnectorTests
    {
        [Fact]
        public void Connector_Should_Have_Unique_Identifier()
        {
            // Arrange
            var connector1 = Substitute.For<IConnector>();
            var connector2 = Substitute.For<IConnector>();

            connector1.Id.Returns(Guid.NewGuid());
            connector2.Id.Returns(Guid.NewGuid());

            // Assert
            connector1.Id.ShouldNotBe(connector2.Id);
            connector1.Id.ShouldNotBe(Guid.Empty);
            connector2.Id.ShouldNotBe(Guid.Empty);
        }

        [Fact]
        public void Connector_Should_Have_Direction()
        {
            // Arrange
            var inputConnector = Substitute.For<IConnector>();
            var outputConnector = Substitute.For<IConnector>();

            inputConnector.Direction.Returns(ConnectorDirection.Input);
            outputConnector.Direction.Returns(ConnectorDirection.Output);

            // Assert
            inputConnector.Direction.ShouldBe(ConnectorDirection.Input);
            outputConnector.Direction.ShouldBe(ConnectorDirection.Output);
        }

        [Fact]
        public void Connector_Should_Reference_Parent_Node()
        {
            // Arrange
            var node = Substitute.For<INode>();
            var connector = Substitute.For<IConnector>();

            connector.ParentNode.Returns(node);

            // Assert
            connector.ParentNode.ShouldBe(node);
        }

        [Fact]
        public void Connector_Should_Maintain_Connections()
        {
            // Arrange
            var connector = Substitute.For<IConnector>();
            var connection = Substitute.For<IConnection>();

            connector.Connections.Returns(new[] { connection });

            // Assert
            connector.Connections.Count.ShouldBe(1);
            connector.Connections.First().ShouldBe(connection);
        }

        [Fact]
        public void Connector_Should_Enforce_Type_Compatibility()
        {
            // Arrange
            var connector1 = Substitute.For<IConnector>();
            var connector2 = Substitute.For<IConnector>();

            connector1.DataType.Returns(typeof(string));
            connector2.DataType.Returns(typeof(int));

            // Act
            var result = connector1.ValidateConnection(connector2);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void Connector_Should_Allow_Adding_Connection()
        {
            // Arrange
            var connector = Substitute.For<IConnector>();
            var connection = Substitute.For<IConnection>();

            connector.AddConnection(connection).Returns(true);

            // Act
            var result = connector.AddConnection(connection);

            // Assert
            result.ShouldBeTrue();
            connector.Received(1).AddConnection(connection);
        }

        [Fact]
        public void Connector_Should_Allow_Removing_Connection()
        {
            // Arrange
            var connector = Substitute.For<IConnector>();
            var connection = Substitute.For<IConnection>();

            connector.RemoveConnection(connection).Returns(true);

            // Act
            var result = connector.RemoveConnection(connection);

            // Assert
            result.ShouldBeTrue();
            connector.Received(1).RemoveConnection(connection);
        }

        [Fact]
        public void Connector_Should_Validate_Connection_Rules()
        {
            // Arrange
            var input = Substitute.For<IConnector>();
            var output = Substitute.For<IConnector>();

            input.Direction.Returns(ConnectorDirection.Input);
            output.Direction.Returns(ConnectorDirection.Output);
            input.DataType.Returns(typeof(string));
            output.DataType.Returns(typeof(string));
            input.Connections.Returns(new IConnection[0]);
            output.Connections.Returns(new IConnection[0]);

            // Set up mutual validation
            input.ValidateConnection(output).Returns(true);
            output.ValidateConnection(input).Returns(true);

            // Act
            var result = input.ValidateConnection(output);

            // Assert
            result.ShouldBeTrue();
        }
    }
} 