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
using Nodify.Workflow.Core.Interfaces; // Added for ConnectorDirection

namespace Nodify.Workflow.Tests.Nodes.Flow;

public class StartNodeTests
{
    [Fact]
    public void ShouldHaveCorrectMetadata()
    {
        // Arrange
        var attribute = typeof(StartNode).GetCustomAttributes(typeof(WorkflowNodeAttribute), false)
                                      .OfType<WorkflowNodeAttribute>()
                                      .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.DisplayName.ShouldBe("Start");
        attribute.Category.ShouldBe("Flow Control");
        attribute.Description.ShouldBe("The starting point of the workflow.");
    }

    [Fact]
    public void Constructor_ShouldCreateOneOutputConnector()
    {
        // Arrange & Act
        var node = new StartNode();

        // Assert
        node.InputConnectors.ShouldBeEmpty();
        node.OutputConnectors.Count.ShouldBe(1);
        
        var output = node.OutputConnectors.First();
        output.Direction.ShouldBe(ConnectorDirection.Output);
        output.DataType.ShouldBe(typeof(object));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSucceededWithConnectorId()
    {
        // Arrange
        var node = new StartNode();
        var expectedOutputConnectorId = node.OutputConnectors.First().Id;
        var context = Substitute.For<IExecutionContext>(); // Context not used by StartNode, but required
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await node.ExecuteAsync(context, cancellationToken);

        // Assert
        result.Success.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.ActivatedOutputConnectorId.ShouldNotBeNull();
        result.ActivatedOutputConnectorId.ShouldBe(expectedOutputConnectorId);
    }
} 