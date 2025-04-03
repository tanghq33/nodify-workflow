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
using NSubstitute.ExceptionExtensions; // Added for Throws

namespace Nodify.Workflow.Tests.Nodes.Data;

public class SetVariableNodeTests
{
    [Fact]
    public void ShouldHaveCorrectMetadata()
    {
        // Arrange
        var attribute = typeof(SetVariableNode).GetCustomAttributes(typeof(WorkflowNodeAttribute), false)
                                            .OfType<WorkflowNodeAttribute>()
                                            .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.DisplayName.ShouldBe("Set Variable");
        attribute.Category.ShouldBe("Data");
        attribute.Description.ShouldBe("Sets or updates a variable in the workflow context.");
    }

    [Fact]
    public void Constructor_ShouldCreateCorrectConnectors()
    {
        // Arrange & Act
        var node = new SetVariableNode();

        // Assert
        node.InputConnectors.Count.ShouldBe(1);
        var input = node.InputConnectors.First();
        input.Direction.ShouldBe(ConnectorDirection.Input);
        input.DataType.ShouldBe(typeof(object));

        node.OutputConnectors.Count.ShouldBe(1);
        var output = node.OutputConnectors.First();
        output.Direction.ShouldBe(ConnectorDirection.Output);
        output.DataType.ShouldBe(typeof(object));
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ShouldSetVariableAndSucceed()
    {
        // Arrange
        var node = new SetVariableNode { VariableName = "TestVar", ValueToSet = 123 };
        var expectedOutputConnectorId = node.OutputConnectors.First().Id;
        var context = Substitute.For<IExecutionContext>();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await node.ExecuteAsync(context, cancellationToken);

        // Assert
        context.Received(1).SetVariable("TestVar", 123);
        result.Success.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.ActivatedOutputConnectorId.ShouldBe(expectedOutputConnectorId);
    }
    
    [Fact]
    public async Task ExecuteAsync_NullValue_ShouldSetVariableAndSucceed()
    {
        // Arrange
        var node = new SetVariableNode { VariableName = "TestVar", ValueToSet = null };
        var expectedOutputConnectorId = node.OutputConnectors.First().Id;
        var context = Substitute.For<IExecutionContext>();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await node.ExecuteAsync(context, cancellationToken);

        // Assert
        context.Received(1).SetVariable("TestVar", null);
        result.Success.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.ActivatedOutputConnectorId.ShouldBe(expectedOutputConnectorId);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyVariableName_ShouldFail()
    {
        // Arrange
        var node = new SetVariableNode { VariableName = string.Empty, ValueToSet = 123 };
        var context = Substitute.For<IExecutionContext>();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await node.ExecuteAsync(context, cancellationToken);

        // Assert
        result.Success.ShouldBeFalse();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldBeOfType<InvalidOperationException>();
        result.Error.Message.ShouldBe("VariableName property cannot be empty.");
        context.DidNotReceive().SetVariable(Arg.Any<string>(), Arg.Any<object?>());
    }

    [Fact]
    public async Task ExecuteAsync_ContextSetVariableThrows_ShouldFail()
    {
        // Arrange
        var node = new SetVariableNode { VariableName = "TestVar", ValueToSet = 123 };
        var context = Substitute.For<IExecutionContext>();
        var expectedException = new ArgumentException("Test context error");
        context.When(x => x.SetVariable(Arg.Any<string>(), Arg.Any<object?>()))
               .Do(_ => throw expectedException);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await node.ExecuteAsync(context, cancellationToken);

        // Assert
        result.Success.ShouldBeFalse();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        result.Error.ShouldBe(expectedException);
    }
} 