using Xunit;
using Shouldly;
using Nodify.Workflow.Core.Logic;
using Nodify.Workflow.Core.Logic.Operators;

namespace Nodify.Workflow.Tests.Core.Logic;

public class EqualityConditionRuleTests
{
    [Theory]
    [InlineData("Hello", "Hello", EqualityOperator.Equals, true)]
    [InlineData("Hello", "World", EqualityOperator.Equals, false)]
    [InlineData(123, 123, EqualityOperator.Equals, true)]
    [InlineData(123, 456, EqualityOperator.Equals, false)]
    [InlineData(true, true, EqualityOperator.Equals, true)]
    [InlineData(true, false, EqualityOperator.Equals, false)]
    [InlineData(null, null, EqualityOperator.Equals, true)] 
    [InlineData("Hello", null, EqualityOperator.Equals, false)]
    [InlineData(null, "Hello", EqualityOperator.Equals, false)]
    [InlineData("Hello", "Hello", EqualityOperator.NotEquals, false)]
    [InlineData("Hello", "World", EqualityOperator.NotEquals, true)]
    [InlineData(123, 123, EqualityOperator.NotEquals, false)]
    [InlineData(123, 456, EqualityOperator.NotEquals, true)]
    [InlineData(null, null, EqualityOperator.NotEquals, false)]
    [InlineData("Hello", null, EqualityOperator.NotEquals, true)]
    [InlineData(null, "Hello", EqualityOperator.NotEquals, true)]
    public void Evaluate_ShouldReturnCorrectResult(object? propertyValue, object? comparisonValue, EqualityOperator op, bool expectedResult)
    {
        // Arrange
        var rule = new EqualityConditionRule
        {
            Operator = op,
            ComparisonValue = comparisonValue
            // PropertyPath is not used by Evaluate directly
        };

        // Act
        bool actualResult = rule.Evaluate(propertyValue);

        // Assert
        actualResult.ShouldBe(expectedResult);
    }
} 