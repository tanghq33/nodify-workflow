using Xunit;
using Shouldly;
using Nodify.Workflow.Nodes.Data;
using Nodify.Workflow.Core.Registry;
using System.Linq;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Interfaces;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution.Context;
using NSubstitute;
using System.Threading;
using System;
using Nodify.Workflow.Core.Execution; // Added for NodeExecutionResult
using System.Collections.Generic;

namespace Nodify.Workflow.Tests.Nodes.Data;

public class GetVariableNodeTests
{
    [Fact]
    public void ShouldHaveCorrectMetadata()
    {
        // Arrange
        var attribute = typeof(GetVariableNode).GetCustomAttributes(typeof(WorkflowNodeAttribute), false)
                                            .OfType<WorkflowNodeAttribute>()
                                            .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.DisplayName.ShouldBe("Get Variable");
        attribute.Category.ShouldBe("Data");
        attribute.Description.ShouldBe("Retrieves a variable from the execution context and outputs it.");
    }

    [Fact]
    public void Constructor_ShouldCreateCorrectConnectors()
    {
        // Arrange & Act
        var node = new GetVariableNode();

        // Assert
        node.InputConnectors.Count.ShouldBe(1);
        var input = node.InputConnectors.First();
        input.Direction.ShouldBe(ConnectorDirection.Input);
        input.DataType.ShouldBe(typeof(object)); // Flow Input

        node.OutputConnectors.Count.ShouldBe(2);
        var flowOutput = node.OutputConnectors.FirstOrDefault(c => c.DataType == typeof(object)); // Assuming flow is object
        var valueOutput = node.OutputConnectors.FirstOrDefault(c => c != flowOutput); // Assuming the other is value
        
        flowOutput.ShouldNotBeNull();
        flowOutput.Direction.ShouldBe(ConnectorDirection.Output);
        flowOutput.DataType.ShouldBe(typeof(object));

        valueOutput.ShouldNotBeNull();
        valueOutput.Direction.ShouldBe(ConnectorDirection.Output);
        valueOutput.DataType.ShouldBe(typeof(object)); // Value output is also object type
    }

    [Fact]
    public async Task ExecuteAsync_VariableExists_ShouldSetOutputValueAndSucceed()
    {
        // Arrange
        var node = new GetVariableNode { VariableName = "TestVar" };
        var context = Substitute.For<IExecutionContext>();
        object? outVar = "Hello World";
        context.TryGetVariable<object>("TestVar", out Arg.Any<object?>())
            .Returns(callInfo => { callInfo[1] = outVar; return true; });

        // Act
        var result = await node.ExecuteAsync(context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(node.OutputConnectors.First(c => c.DataType == typeof(object)).Id);
        result.OutputData.ShouldBe("Hello World");
    }

    [Fact]
    public async Task ExecuteAsync_VariableDoesNotExist_ShouldFail()
    {
        // Arrange
        var node = new GetVariableNode { VariableName = "MissingVar" };
        var context = Substitute.For<IExecutionContext>();
        var cancellationToken = CancellationToken.None;
        object? outValue = null;

        context.TryGetVariable<object>("MissingVar", out outValue).Returns(false);

        // Act
        var result = await node.ExecuteAsync(context, null, cancellationToken);

        // Assert
        context.Received(1).TryGetVariable<object>("MissingVar", out outValue);
        context.DidNotReceive().SetOutputConnectorValue(Arg.Any<Guid>(), Arg.Any<object?>());
        result.Success.ShouldBeFalse();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldBeOfType<KeyNotFoundException>();
        result.Error.Message.ShouldContain("MissingVar");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyVariableName_ShouldFail()
    {
        // Arrange
        var node = new GetVariableNode { VariableName = " " }; // Whitespace
        var context = Substitute.For<IExecutionContext>();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await node.ExecuteAsync(context, null, cancellationToken);

        // Assert
        context.DidNotReceive().TryGetVariable<object>(Arg.Any<string>(), out Arg.Any<object?>());
        context.DidNotReceive().SetOutputConnectorValue(Arg.Any<Guid>(), Arg.Any<object?>());
        result.Success.ShouldBeFalse();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldBeOfType<InvalidOperationException>();
        result.Error.Message.ShouldBe("VariableName property cannot be empty.");
    }
} 