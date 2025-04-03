using Xunit;
using Shouldly;
using Nodify.Workflow.Core.Logic;
using Nodify.Workflow.Core.Logic.Operators;
using System;

namespace Nodify.Workflow.Tests.Core.Logic;

public class StringConditionRuleTests
{
    [Theory]
    // Equals (Case Sensitive)
    [InlineData("hello", "hello", StringOperator.Equals, true)]
    [InlineData("hello", "HELLO", StringOperator.Equals, false)]
    [InlineData("hello", "world", StringOperator.Equals, false)]
    [InlineData("", "", StringOperator.Equals, true)]
    // EqualsIgnoreCase
    [InlineData("hello", "hello", StringOperator.EqualsIgnoreCase, true)]
    [InlineData("hello", "HELLO", StringOperator.EqualsIgnoreCase, true)]
    [InlineData("hello", "world", StringOperator.EqualsIgnoreCase, false)]
    // NotEquals (Case Sensitive)
    [InlineData("hello", "hello", StringOperator.NotEquals, false)]
    [InlineData("hello", "HELLO", StringOperator.NotEquals, true)]
    [InlineData("hello", "world", StringOperator.NotEquals, true)]
    // NotEqualsIgnoreCase
    [InlineData("hello", "hello", StringOperator.NotEqualsIgnoreCase, false)]
    [InlineData("hello", "HELLO", StringOperator.NotEqualsIgnoreCase, false)]
    [InlineData("hello", "world", StringOperator.NotEqualsIgnoreCase, true)]
    // Contains (Case Sensitive)
    [InlineData("hello world", "llo", StringOperator.Contains, true)]
    [InlineData("hello world", "LLO", StringOperator.Contains, false)]
    [InlineData("hello world", "xyz", StringOperator.Contains, false)]
    // ContainsIgnoreCase
    [InlineData("hello world", "llo", StringOperator.ContainsIgnoreCase, true)]
    [InlineData("hello world", "LLO", StringOperator.ContainsIgnoreCase, true)]
    [InlineData("hello world", "xyz", StringOperator.ContainsIgnoreCase, false)]
    // StartsWith (Case Sensitive)
    [InlineData("hello world", "hell", StringOperator.StartsWith, true)]
    [InlineData("hello world", "HELL", StringOperator.StartsWith, false)]
    [InlineData("hello world", "world", StringOperator.StartsWith, false)]
    // StartsWithIgnoreCase
    [InlineData("hello world", "hell", StringOperator.StartsWithIgnoreCase, true)]
    [InlineData("hello world", "HELL", StringOperator.StartsWithIgnoreCase, true)]
    [InlineData("hello world", "world", StringOperator.StartsWithIgnoreCase, false)]
    // EndsWith (Case Sensitive)
    [InlineData("hello world", "orld", StringOperator.EndsWith, true)]
    [InlineData("hello world", "ORLD", StringOperator.EndsWith, false)]
    [InlineData("hello world", "hello", StringOperator.EndsWith, false)]
    // EndsWithIgnoreCase
    [InlineData("hello world", "orld", StringOperator.EndsWithIgnoreCase, true)]
    [InlineData("hello world", "ORLD", StringOperator.EndsWithIgnoreCase, true)]
    [InlineData("hello world", "hello", StringOperator.EndsWithIgnoreCase, false)]
    public void Evaluate_StringComparisons_ShouldReturnCorrectResult(string? propertyValue, string? comparisonValue, StringOperator op, bool expectedResult)
    {
        // Arrange
        var rule = new StringConditionRule
        {
            Operator = op,
            ComparisonValue = comparisonValue
        };

        // Act
        bool actualResult = rule.Evaluate(propertyValue);

        // Assert
        actualResult.ShouldBe(expectedResult);
    }

    [Theory]
    // IsEmpty
    [InlineData(null, StringOperator.IsEmpty, true)]
    [InlineData("", StringOperator.IsEmpty, true)]
    [InlineData(" ", StringOperator.IsEmpty, false)] // Whitespace is not empty
    [InlineData("hello", StringOperator.IsEmpty, false)]
    // IsNotEmpty
    [InlineData(null, StringOperator.IsNotEmpty, false)]
    [InlineData("", StringOperator.IsNotEmpty, false)]
    [InlineData(" ", StringOperator.IsNotEmpty, true)] // Whitespace is not empty
    [InlineData("hello", StringOperator.IsNotEmpty, true)]
    public void Evaluate_StringExistenceChecks_ShouldReturnCorrectResult(string? propertyValue, StringOperator op, bool expectedResult)
    {
         // Arrange
        var rule = new StringConditionRule
        {
            Operator = op,
            ComparisonValue = null // Not used for these operators
        };

        // Act
        bool actualResult = rule.Evaluate(propertyValue);

        // Assert
        actualResult.ShouldBe(expectedResult);
    }
    
    [Theory]
    [InlineData(123)] // Not a string
    [InlineData(true)]
    public void Evaluate_InvalidPropertyValueTypeForComparison_ShouldReturnFalse(object? invalidPropertyValue)
    {
        // Arrange
        var rule = new StringConditionRule
        {
            Operator = StringOperator.Equals, // Operator requires a string
            ComparisonValue = "hello"
        };

        // Act
        bool actualResult = rule.Evaluate(invalidPropertyValue);

        // Assert
        actualResult.ShouldBeFalse();
    }

     [Fact]
    public void Evaluate_ComparisonValueNullWhenRequired_ShouldReturnFalse()
    {
        // Arrange
        var rule = new StringConditionRule
        {
            Operator = StringOperator.Contains, // Requires ComparisonValue
            ComparisonValue = null 
        };

        // Act
        bool actualResult = rule.Evaluate("hello world");

        // Assert
        actualResult.ShouldBeFalse();
    }
} 