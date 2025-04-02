using System;
using Xunit;
using NSubstitute;
using Shouldly;
using Nodify.Workflow.Core.Graph.Interfaces;
using Nodify.Workflow.Core.Graph.Models;

namespace Nodify.Workflow.Tests.Core.Graph.Models
{
    public class ConnectorTests
    {
        [Fact]
        public void Constructor_Should_Initialize_With_Unique_Id()
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
        public void Constructor_Should_Initialize_Properties_Correctly()
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
        public void Constructor_Should_Throw_On_Null_ParentNode()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                new Connector(null, ConnectorDirection.Input, typeof(string)));
        }

        [Fact]
        public void Constructor_Should_Throw_On_Null_DataType()
        {
            // Arrange
            var node = Substitute.For<INode>();

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                new Connector(node, ConnectorDirection.Input, null));
        }

        [Fact]
        public void AddConnection_Should_Add_Valid_Connection()
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
        public void AddConnection_Should_Throw_On_Null_Connection()
        {
            // Arrange
            var node = Substitute.For<INode>();
            var connector = new Connector(node, ConnectorDirection.Input, typeof(string));

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => connector.AddConnection(null));
        }

        [Fact]
        public void AddConnection_Should_Reject_Invalid_Connection()
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
        public void RemoveConnection_Should_Remove_Connection()
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
        public void RemoveConnection_Should_Return_False_For_Null()
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
        public void ValidateConnection_Should_Allow_Compatible_Types()
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
        public void ValidateConnection_Should_Allow_Inheritance_Compatible_Types()
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
        public void ValidateConnection_Should_Reject_Incompatible_Types()
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
        public void ValidateConnection_Should_Reject_Same_Direction()
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
        public void ValidateConnection_Should_Reject_Multiple_Input_Connections()
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
} 