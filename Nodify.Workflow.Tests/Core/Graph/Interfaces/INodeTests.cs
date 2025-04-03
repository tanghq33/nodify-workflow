using System;
using System.Linq;
using Xunit;
using NSubstitute;
using Shouldly;
using Nodify.Workflow.Core.Graph.Interfaces;

namespace Nodify.Workflow.Tests.Core.Graph.Interfaces
{
    public class INodeTests
    {
        [Fact]
        public void Node_ShouldHaveUniqueIdentifier()
        {
            // Arrange
            var node1 = Substitute.For<INode>();
            var node2 = Substitute.For<INode>();

            node1.Id.Returns(Guid.NewGuid());
            node2.Id.Returns(Guid.NewGuid());

            // Assert
            node1.Id.ShouldNotBe(node2.Id);
            node1.Id.ShouldNotBe(Guid.Empty);
            node2.Id.ShouldNotBe(Guid.Empty);
        }

        [Fact]
        public void Node_ShouldMaintainInputConnectors()
        {
            // Arrange
            var node = Substitute.For<INode>();
            var connector = Substitute.For<IConnector>();
            
            connector.Direction.Returns(ConnectorDirection.Input);
            node.InputConnectors.Returns(new[] { connector });

            // Assert
            node.InputConnectors.Count.ShouldBe(1);
            node.InputConnectors.First().Direction.ShouldBe(ConnectorDirection.Input);
        }

        [Fact]
        public void Node_ShouldMaintainOutputConnectors()
        {
            // Arrange
            var node = Substitute.For<INode>();
            var connector = Substitute.For<IConnector>();
            
            connector.Direction.Returns(ConnectorDirection.Output);
            node.OutputConnectors.Returns(new[] { connector });

            // Assert
            node.OutputConnectors.Count.ShouldBe(1);
            node.OutputConnectors.First().Direction.ShouldBe(ConnectorDirection.Output);
        }

        [Fact]
        public void Node_ShouldAllowAddingInputConnector()
        {
            // Arrange
            var node = Substitute.For<INode>();
            var connector = Substitute.For<IConnector>();
            
            connector.Direction.Returns(ConnectorDirection.Input);

            // Act
            node.AddInputConnector(connector);

            // Assert
            node.Received(1).AddInputConnector(connector);
        }

        [Fact]
        public void Node_ShouldAllowAddingOutputConnector()
        {
            // Arrange
            var node = Substitute.For<INode>();
            var connector = Substitute.For<IConnector>();
            
            connector.Direction.Returns(ConnectorDirection.Output);

            // Act
            node.AddOutputConnector(connector);

            // Assert
            node.Received(1).AddOutputConnector(connector);
        }

        [Fact]
        public void Node_ShouldAllowRemovingConnector()
        {
            // Arrange
            var node = Substitute.For<INode>();
            var connector = Substitute.For<IConnector>();

            node.RemoveConnector(connector).Returns(true);

            // Act
            var result = node.RemoveConnector(connector);

            // Assert
            result.ShouldBeTrue();
            node.Received(1).RemoveConnector(connector);
        }

        [Fact]
        public void Node_ShouldTrackXYCoordinates()
        {
            // Arrange
            var node = Substitute.For<INode>();
            const double expectedX = 100.0;
            const double expectedY = 200.0;

            // Act
            node.X = expectedX;
            node.Y = expectedY;

            // Assert
            node.X.ShouldBe(expectedX);
            node.Y.ShouldBe(expectedY);
        }

        [Fact]
        public void Node_ShouldValidateConnectorRelationships()
        {
            // Arrange
            var node = Substitute.For<INode>();
            node.Validate().Returns(true);

            // Act
            var result = node.Validate();

            // Assert
            result.ShouldBeTrue();
            node.Received(1).Validate();
        }
    }
} 