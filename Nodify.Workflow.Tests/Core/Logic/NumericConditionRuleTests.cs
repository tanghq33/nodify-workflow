using Xunit;
using Shouldly;
using Nodify.Workflow.Core.Logic;
using Nodify.Workflow.Core.Logic.Operators;
using System;

namespace Nodify.Workflow.Tests.Core.Logic;

public class NumericConditionRuleTests
{
    [Theory]
    // Equals / NotEquals
    [InlineData(10, 10.0, NumericOperator.Equals, true)]
    [InlineData(10, 10.1, NumericOperator.Equals, false)]
    [InlineData(10, 10.0, NumericOperator.NotEquals, false)]
    [InlineData(10, 10.1, NumericOperator.NotEquals, true)]
    [InlineData(10.5, 10.5, NumericOperator.Equals, true)]
    [InlineData("10", 10.0, NumericOperator.Equals, true)] // Test string conversion
    [InlineData("-5.5", -5.5, NumericOperator.Equals, true)]
    // GreaterThan
    [InlineData(11, 10.0, NumericOperator.GreaterThan, true)]
    [InlineData(10, 10.0, NumericOperator.GreaterThan, false)]
    [InlineData(9, 10.0, NumericOperator.GreaterThan, false)]
    [InlineData("11", 10.0, NumericOperator.GreaterThan, true)]
    // LessThan
    [InlineData(9, 10.0, NumericOperator.LessThan, true)]
    [InlineData(10, 10.0, NumericOperator.LessThan, false)]
    [InlineData(11, 10.0, NumericOperator.LessThan, false)]
    [InlineData("9", 10.0, NumericOperator.LessThan, true)]
    // GreaterThanOrEqual
    [InlineData(11, 10.0, NumericOperator.GreaterThanOrEqual, true)]
    [InlineData(10, 10.0, NumericOperator.GreaterThanOrEqual, true)]
    [InlineData(9, 10.0, NumericOperator.GreaterThanOrEqual, false)]
    // LessThanOrEqual
    [InlineData(9, 10.0, NumericOperator.LessThanOrEqual, true)]
    [InlineData(10, 10.0, NumericOperator.LessThanOrEqual, true)]
    [InlineData(11, 10.0, NumericOperator.LessThanOrEqual, false)]
    public void Evaluate_ValidNumericInputs_ShouldReturnCorrectResult(object propertyValue, double comparisonValue, NumericOperator op, bool expectedResult)
    {
        // Arrange
        var rule = new NumericConditionRule
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
    [InlineData(null)]
    [InlineData("not a number")]
    [InlineData(true)] // Boolean is not convertible to double
    public void Evaluate_InvalidPropertyValueType_ShouldReturnFalse(object? invalidPropertyValue)
    {
        // Arrange
        var rule = new NumericConditionRule
        {
            Operator = NumericOperator.Equals,
            ComparisonValue = 10.0
        };

        // Act
        bool actualResult = rule.Evaluate(invalidPropertyValue);

        // Assert
        actualResult.ShouldBeFalse();
    }
} 