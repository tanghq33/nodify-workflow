using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Registry;
using Nodify.Workflow.Nodes.Data;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Nodify.Workflow.Tests.Nodes.Data;

public class OutputNodeTests
{
    [Fact]
    public void ShouldHaveCorrectMetadata()
    {
        // Arrange
        var attribute = typeof(OutputNode).GetCustomAttributes(typeof(WorkflowNodeAttribute), false)
                                            .OfType<WorkflowNodeAttribute>()
                                            .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.DisplayName.ShouldBe("Output Data");
        attribute.Category.ShouldBe("Data");
        attribute.Description.ShouldBe("Captures input data and stores it in a specified context variable.");
    }

    [Fact]
    public void Constructor_ShouldCreateInputConnector()
    {
        // Arrange & Act
        var node = new OutputNode();

        // Assert
        node.InputConnectors.Count.ShouldBe(1);
        var input = node.InputConnectors.First();
        input.Direction.ShouldBe(ConnectorDirection.Input);
        input.DataType.ShouldBe(typeof(object));
    }

    [Fact]
    public void Constructor_ShouldNotHaveOutputConnectors()
    {
        // Arrange & Act
        var node = new OutputNode();

        // Assert
        node.OutputConnectors.ShouldBeEmpty();
    }

    // === Execution Tests ===

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ShouldStoreInputDataInContextAndSucceed()
    {
        // Arrange
        var node = new OutputNode { VariableName = "FinalResult" };
        var context = Substitute.For<IExecutionContext>();
        var inputData = "ResultData";

        // Act
        var result = await node.ExecuteAsync(context, inputData, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.ActivatedOutputConnectorId.ShouldBeNull(); // Output node doesn't activate further connectors
        result.OutputData.ShouldBeNull(); // Output node doesn't pass data through NodeExecutionResult
        context.Received(1).SetVariable("FinalResult", inputData);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullInput_ShouldStoreNullInContextAndSucceed()
    {
        // Arrange
        var node = new OutputNode { VariableName = "NullResult" };
        var context = Substitute.For<IExecutionContext>();
        object? inputData = null;

        // Act
        var result = await node.ExecuteAsync(context, inputData, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        context.Received(1).SetVariable("NullResult", null);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexObjectInput_ShouldStoreObjectInContextAndSucceed()
    {
        // Arrange
        var node = new OutputNode { VariableName = "ComplexOutput" };
        var context = Substitute.For<IExecutionContext>();
        var inputData = new { Name = "Test", Value = 123 };

        // Act
        var result = await node.ExecuteAsync(context, inputData, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        context.Received(1).SetVariable("ComplexOutput", inputData);
    }

    // === Execution - Error Scenarios ===

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_EmptyOrWhitespaceOutputName_ShouldFail(string invalidName)
    {
        // Arrange
        var node = new OutputNode { VariableName = invalidName };
        var context = Substitute.For<IExecutionContext>();
        var inputData = "SomeData";

        // Act
        var result = await node.ExecuteAsync(context, inputData, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Message.ShouldContain("VariableName property cannot be null or empty");
    }

    [Fact]
    public async Task ExecuteAsync_NullOutputName_ShouldFail()
    {
        // Arrange
        var node = new OutputNode { VariableName = null! }; // Explicitly null
        var context = Substitute.For<IExecutionContext>();
        var inputData = "SomeData";

        // Act
        var result = await node.ExecuteAsync(context, inputData, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Message.ShouldContain("VariableName property cannot be null or empty");
    }

    [Fact]
    public async Task ExecuteAsync_ContextSetVariableThrows_ShouldFail()
    {
        // Arrange
        var node = new OutputNode { VariableName = "ResultVar" };
        var context = Substitute.For<IExecutionContext>();
        var inputData = "SomeData";
        var expectedException = new ArgumentException("Context error");
        context.When(x => x.SetVariable("ResultVar", inputData))
               .Do(_ => throw expectedException);

        // Act
        var result = await node.ExecuteAsync(context, inputData, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        result.Error.ShouldBe(expectedException);
    }

    // === Validation Tests ===

    [Fact]
    public void Validate_WhenOutputNameIsValid_ShouldReturnTrue()
    {
        // Arrange
        var node = new OutputNode { VariableName = "ValidName" };
        // Ensure base validation passes (mocking parent on connector)
        var inputConnector = node.InputConnectors.First();
        var connectorMock = Substitute.For<IConnector>();
        connectorMock.Id.Returns(inputConnector.Id); // Match ID if needed by base
        connectorMock.ParentNode.Returns(node);
        connectorMock.Direction.Returns(ConnectorDirection.Input);
        // Replace original connector with mock if needed for base.Validate()
        // This is complex, easier if base.Validate just checks ParentNode != null
        // For simplicity, assume base.Validate() is covered elsewhere or simple.

        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenOutputNameIsInvalid_ShouldReturnFalse(string? invalidName)
    {
        // Arrange
        var node = new OutputNode { VariableName = invalidName! };

        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeFalse();
    }
} 