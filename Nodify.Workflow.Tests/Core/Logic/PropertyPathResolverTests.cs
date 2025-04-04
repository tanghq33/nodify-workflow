using Xunit;
using Shouldly;
using Nodify.Workflow.Core.Logic;
using System;
using System.Collections.Generic;

namespace Nodify.Workflow.Tests.Core.Logic;

public class PropertyPathResolverTests
{
    private class NestedTestData
    {
        public Level1Data? Level1 { get; set; }
        public string RootValue { get; set; } = "Root";
    }

    private class Level1Data
    {
        public Level2Data? Level2 { get; set; }
        public int IntValue { get; set; } = 123;
    }

    private class Level2Data
    {
        public string? City { get; set; } = "Test City";
        public bool IsActive { get; set; } = true;
    }

    [Theory]
    [InlineData("RootValue", "Root")]
    [InlineData("level1.intvalue", 123)] // Test case insensitivity
    [InlineData("Level1.Level2.City", "Test City")]
    [InlineData("Level1.Level2.IsActive", true)]
    public void TryResolvePath_ValidPath_ShouldResolveValue(string path, object expectedValue)
    {
        // Arrange
        var target = new NestedTestData
        {
            Level1 = new Level1Data
            {
                Level2 = new Level2Data()
            }
        };

        // Act
        bool result = PropertyPathResolver.TryResolvePath(target, path, out object? actualValue, out string? error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeNull();
        actualValue.ShouldBe(expectedValue);
    }

    [Fact]
    public void TryResolvePath_IntermediateNull_ShouldFail()
    {
        // Arrange
        var target = new NestedTestData { Level1 = null }; // Level1 is null
        string path = "Level1.IntValue";

        // Act
        bool result = PropertyPathResolver.TryResolvePath(target, path, out object? actualValue, out string? error);

        // Assert
        result.ShouldBeFalse();
        actualValue.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("IntValue"); // Check for the part that failed
        error.ShouldContain("intermediate object is null"); 
    }
    
     [Fact]
    public void TryResolvePath_DeepIntermediateNull_ShouldFail()
    {
        // Arrange
        var target = new NestedTestData { Level1 = new Level1Data { Level2 = null } }; // Level2 is null
        string path = "Level1.Level2.City";

        // Act
        bool result = PropertyPathResolver.TryResolvePath(target, path, out object? actualValue, out string? error);

        // Assert
        result.ShouldBeFalse();
        actualValue.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("City"); // Check for the part that failed 
        error.ShouldContain("intermediate object is null"); 
    }

    [Theory]
    [InlineData("NonExistentProperty", "NonExistentProperty")]
    [InlineData("Level1.NonExistent", "NonExistent")]
    [InlineData("Level1.Level2.NonExistent", "NonExistent")]
    public void TryResolvePath_InvalidPathPart_ShouldFail(string path, string invalidPart)
    {
        // Arrange
        var target = new NestedTestData
        {
            Level1 = new Level1Data
            {
                Level2 = new Level2Data()
            }
        };

        // Act
        bool result = PropertyPathResolver.TryResolvePath(target, path, out object? actualValue, out string? error);

        // Assert
        result.ShouldBeFalse();
        actualValue.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain(invalidPart);
        error.ShouldContain("not found on type");
    }

    [Fact]
    public void TryResolvePath_TargetNull_ShouldFail()
    {
        // Arrange
        NestedTestData? target = null;
        string path = "RootValue";

        // Act
        bool result = PropertyPathResolver.TryResolvePath(target, path, out object? actualValue, out string? error);

        // Assert
        result.ShouldBeFalse();
        actualValue.ShouldBeNull();
        error.ShouldBe("Target object is null.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolvePath_EmptyOrNullPath_ShouldReturnTargetObject(string? path)
    {
        // Arrange
        var target = new NestedTestData();

        // Act
        bool result = PropertyPathResolver.TryResolvePath(target, path!, out object? actualValue, out string? error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeNull();
        actualValue.ShouldBe(target);
    }

    // Consider adding tests for edge cases like indexers if needed later
} 