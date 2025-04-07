using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
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

public class InputJsonNodeTests
{
    private IExecutionContext _context = null!;

    public InputJsonNodeTests()
    {
        _context = Substitute.For<IExecutionContext>(); // Context is not really used by InputJsonNode execution
    }

    [Fact]
    public void ShouldHaveCorrectMetadata()
    {
        // Arrange
        var attribute = typeof(InputJsonNode).GetCustomAttributes(typeof(WorkflowNodeAttribute), false)
                                            .OfType<WorkflowNodeAttribute>()
                                            .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.DisplayName.ShouldBe("Input JSON");
        attribute.Category.ShouldBe("Data");
        attribute.Description.ShouldBe("Parses a JSON string and outputs the resulting object/value.");
    }

    [Fact]
    public void Constructor_ShouldCreateCorrectConnectors()
    {
        // Arrange & Act
        var node = new InputJsonNode();

        // Assert
        node.InputConnectors.ShouldBeEmpty();
        node.OutputConnectors.Count.ShouldBe(1);
        var output = node.OutputConnectors.First();
        output.Direction.ShouldBe(ConnectorDirection.Output);
        output.DataType.ShouldBe(typeof(object)); // Output is parsed JSON element/value
    }

    // === Execution - Valid JSON ===

    [Fact]
    public async Task ExecuteAsync_WithValidJsonObject_ShouldSucceedAndOutputObject()
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = "{\"name\": \"test\", \"value\": 10}" };
        var outputConnectorId = node.OutputConnectors.First().Id;

        // Act
        var result = await node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(outputConnectorId);
        result.OutputData.ShouldNotBeNull();
        result.OutputData.ShouldBeOfType<JsonElement>();
        var jsonElement = (JsonElement)result.OutputData;
        jsonElement.ValueKind.ShouldBe(JsonValueKind.Object);
        jsonElement.GetProperty("name").GetString().ShouldBe("test");
        jsonElement.GetProperty("value").GetInt32().ShouldBe(10);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidJsonArray_ShouldSucceedAndOutputArray()
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = "[1, \"a\", true, null]" };
        var outputConnectorId = node.OutputConnectors.First().Id;

        // Act
        var result = await node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(outputConnectorId);
        result.OutputData.ShouldNotBeNull();
        result.OutputData.ShouldBeOfType<JsonElement>();
        var jsonElement = (JsonElement)result.OutputData;
        jsonElement.ValueKind.ShouldBe(JsonValueKind.Array);
        jsonElement.GetArrayLength().ShouldBe(4);
        jsonElement[0].GetInt32().ShouldBe(1);
        jsonElement[1].GetString().ShouldBe("a");
        jsonElement[2].GetBoolean().ShouldBeTrue();
        jsonElement[3].ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Theory]
    [InlineData("\"hello world\"", "hello world")]
    [InlineData("\"\"", "")] // Empty string
    public async Task ExecuteAsync_WithValidJsonPrimitiveString_ShouldSucceedAndOutputString(string jsonInput, string expectedOutput)
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = jsonInput };
        var outputConnectorId = node.OutputConnectors.First().Id;

        // Act
        var result = await node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(outputConnectorId);
        result.OutputData.ShouldNotBeNull();
        result.OutputData.ShouldBeOfType<JsonElement>();
        var jsonElement = (JsonElement)result.OutputData;
        jsonElement.ValueKind.ShouldBe(JsonValueKind.String);
        jsonElement.GetString().ShouldBe(expectedOutput);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("123.45", 123.45)]
    [InlineData("-50", -50)]
    [InlineData("0", 0)]
    public async Task ExecuteAsync_WithValidJsonPrimitiveNumber_ShouldSucceedAndOutputNumber(string jsonInput, double expectedOutput)
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = jsonInput };
        var outputConnectorId = node.OutputConnectors.First().Id;

        // Act
        var result = await node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(outputConnectorId);
        result.OutputData.ShouldNotBeNull();
        result.OutputData.ShouldBeOfType<JsonElement>();
        var jsonElement = (JsonElement)result.OutputData;
        jsonElement.ValueKind.ShouldBe(JsonValueKind.Number);
        jsonElement.GetDouble().ShouldBe(expectedOutput);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public async Task ExecuteAsync_WithValidJsonPrimitiveBoolean_ShouldSucceedAndOutputBoolean(string jsonInput, bool expectedOutput)
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = jsonInput };
        var outputConnectorId = node.OutputConnectors.First().Id;

        // Act
        var result = await node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(outputConnectorId);
        result.OutputData.ShouldNotBeNull();
        result.OutputData.ShouldBeOfType<JsonElement>();
        var jsonElement = (JsonElement)result.OutputData;
        jsonElement.ValueKind.ShouldBe(expectedOutput ? JsonValueKind.True : JsonValueKind.False);
        jsonElement.GetBoolean().ShouldBe(expectedOutput);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidJsonPrimitiveNull_ShouldSucceedAndOutputNull()
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = "null" };
        var outputConnectorId = node.OutputConnectors.First().Id;

        // Act
        var result = await node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(outputConnectorId);
        // Note: The output JsonElement's ValueKind is Null, but OutputData itself is the JsonElement, not null.
        result.OutputData.ShouldNotBeNull(); 
        result.OutputData.ShouldBeOfType<JsonElement>();
        var jsonElement = (JsonElement)result.OutputData;
        jsonElement.ValueKind.ShouldBe(JsonValueKind.Null); 
    }

    // === Execution - Invalid JSON ===

    [Theory]
    [InlineData("{name: \"test\"}")] // Missing quotes around key
    [InlineData("[1, 2,")] // Trailing comma
    [InlineData("{\"name\": \"test\"")] // Missing closing brace
    [InlineData("{\"a\":1}{\"b\":2}")] // Multiple roots
    [InlineData("abc")] // Not a valid JSON primitive (needs quotes for string)
    public async Task ExecuteAsync_WithInvalidJsonSyntax_ShouldFailWithJsonException(string invalidJson)
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = invalidJson };

        // Act
        var result = await node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        result.OutputData.ShouldBeNull();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldBeOfType<JsonException>();
        result.Error.Message.ShouldContain("Failed to parse JsonContent");
    }

    // === Execution - Empty/Null/Whitespace Input ===

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithEmptyOrWhitespaceJsonContent_ShouldFail(string emptyJson)
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = emptyJson };

        // Act
        var result = await node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        result.OutputData.ShouldBeNull();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldBeOfType<InvalidOperationException>(); // Fails the initial check
        result.Error.Message.ShouldContain("JsonContent property cannot be null or empty");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullJsonContent_ShouldFail()
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = null! }; // Explicitly null

        // Act
        var result = await node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.ActivatedOutputConnectorId.ShouldBeNull();
        result.OutputData.ShouldBeNull();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldBeOfType<InvalidOperationException>(); // Fails the initial check
        result.Error.Message.ShouldContain("JsonContent property cannot be null or empty");
    }

    // === Validation Tests ===
    [Fact]
    public void Validate_ShouldReturnTrueByDefault()
    {
        // Arrange
        var node = new InputJsonNode { JsonContent = "{\"valid\": true}" };
        
        // Act
        var isValid = node.Validate();

        // Assert
        isValid.ShouldBeTrue(); // Basic validation (connectors ok, content non-empty) passes
    }

    // Note: The current Validate implementation doesn't fail on empty/null JsonContent,
    // it relies on ExecuteAsync. If validation logic changes, add tests here.
    // e.g., Validate_WhenJsonContentIsEmpty_ShouldReturnFalse
} 