using System;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Nodes.Flow; // Assuming MergeNode is here
using NSubstitute;
using Shouldly;
using Xunit;

namespace Nodify.Workflow.Tests.Nodes.Flow;

public class MergeNodeTests
{
    [Fact]
    public async Task ExecuteAsync_WhenInputDataIsProvided_ShouldReturnSucceededWithSameData()
    {
        // Arrange
        var node = new MergeNode();
        var mockContext = Substitute.For<IExecutionContext>();
        var testData = new { Message = "Hello Merge" };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await node.ExecuteAsync(mockContext, testData, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(node._outputConnectorId);
        result.OutputData.ShouldBeSameAs(testData); // Verify the exact object instance is passed
        result.Error.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenInputDataIsNull_ShouldReturnSucceededWithNullData()
    {
        // Arrange
        var node = new MergeNode();
        var mockContext = Substitute.For<IExecutionContext>();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await node.ExecuteAsync(mockContext, null, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(node._outputConnectorId);
        result.OutputData.ShouldBeNull();
        result.Error.ShouldBeNull();
    }
} 