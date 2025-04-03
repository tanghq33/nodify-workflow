using Xunit;
using Shouldly;
using Nodify.Workflow.Nodes.Flow;
using Nodify.Workflow.Core.Registry;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution.Context;
using NSubstitute;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Models; // For ConnectorDirection
using System;
using Nodify.Workflow.Core.Interfaces; // For ConnectorDirection

namespace Nodify.Workflow.Tests.Nodes.Flow;

public class EndNodeTests
{
     [Fact]
    public void ShouldHaveCorrectMetadata()
    {
        // Arrange
        var attribute = typeof(EndNode).GetCustomAttributes(typeof(WorkflowNodeAttribute), false)
                                      .OfType<WorkflowNodeAttribute>()
                                      .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.DisplayName.ShouldBe("End");
        attribute.Category.ShouldBe("Flow Control");
        attribute.Description.ShouldBe("Marks the termination point of a workflow path.");
    }

    [Fact]
    public void Constructor_ShouldCreateOneInputConnector()
    {
        // Arrange & Act
        var node = new EndNode();

        // Assert
        node.OutputConnectors.ShouldBeEmpty();
        node.InputConnectors.Count.ShouldBe(1);

        var input = node.InputConnectors.First();
        // input.Name.ShouldBe(EndNode.InputConnectorName); // Name property does not exist on IConnector
        input.Direction.ShouldBe(ConnectorDirection.Input);
        input.DataType.ShouldBe(typeof(object)); // Flow connector uses typeof(object)
    }

    // TODO: Test ExecuteAsync: Should return Succeeded() (no specific output)

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSucceededWithoutConnectorId()
    {
        // Arrange
        var node = new EndNode();
        var context = Substitute.For<IExecutionContext>(); // Context not used by EndNode
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await node.ExecuteAsync(context, cancellationToken);

        // Assert
        result.Success.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.ActivatedOutputConnectorId.ShouldBeNull(); // End node doesn't activate an output
    }
} 