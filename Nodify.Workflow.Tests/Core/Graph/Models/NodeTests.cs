using System;
using Xunit;
using NSubstitute;
using Shouldly;
using Nodify.Workflow.Core.Graph.Interfaces;
using Nodify.Workflow.Core.Graph.Models;

namespace Nodify.Workflow.Tests.Core.Graph.Models
{
    public class NodeTests
    {
        [Fact]
        public void Constructor_Should_Initialize_With_Unique_Id()
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
        public void Constructor_Should_Initialize_Empty_Connector_Collections()
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
        public void AddInputConnector_Should_Add_Valid_Input_Connector()
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
        public void AddInputConnector_Should_Throw_On_Null_Connector()
        {
            // Arrange
            var node = new Node();

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => node.AddInputConnector(null));
        }

        [Fact]
        public void AddInputConnector_Should_Throw_On_Output_Connector()
        {
            // Arrange
            var node = new Node();
            var connector = Substitute.For<IConnector>();
            connector.Direction.Returns(ConnectorDirection.Output);

            // Act & Assert
            Should.Throw<ArgumentException>(() => node.AddInputConnector(connector));
        }

        [Fact]
        public void AddOutputConnector_Should_Add_Valid_Output_Connector()
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
        public void AddOutputConnector_Should_Throw_On_Null_Connector()
        {
            // Arrange
            var node = new Node();

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => node.AddOutputConnector(null));
        }

        [Fact]
        public void AddOutputConnector_Should_Throw_On_Input_Connector()
        {
            // Arrange
            var node = new Node();
            var connector = Substitute.For<IConnector>();
            connector.Direction.Returns(ConnectorDirection.Input);

            // Act & Assert
            Should.Throw<ArgumentException>(() => node.AddOutputConnector(connector));
        }

        [Fact]
        public void RemoveConnector_Should_Remove_And_Cleanup_Connections()
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
        public void RemoveConnector_Should_Return_False_For_Null()
        {
            // Arrange
            var node = new Node();

            // Act
            var result = node.RemoveConnector(null);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void Validate_Should_Return_True_For_Valid_Node()
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
        public void Validate_Should_Return_False_For_Invalid_ParentNode()
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
        public void Position_Should_Be_Settable()
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
} 