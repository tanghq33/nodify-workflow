using Xunit;
using Shouldly;
using Nodify.Workflow.Core.Logic;
using Nodify.Workflow.Core.Logic.Operators;

namespace Nodify.Workflow.Tests.Core.Logic;

public class NullConditionRuleTests
{
    [Theory]
    [InlineData(null, NullOperator.IsNull, true)]
    [InlineData("Hello", NullOperator.IsNull, false)]
    [InlineData(123, NullOperator.IsNull, false)]
    [InlineData(null, NullOperator.IsNotNull, false)]
    [InlineData("Hello", NullOperator.IsNotNull, true)]
    [InlineData(123, NullOperator.IsNotNull, true)]
    public void Evaluate_ShouldReturnCorrectResult(object? propertyValue, NullOperator op, bool expectedResult)
    {
        // Arrange
        var rule = new NullConditionRule
        {
            Operator = op
            // PropertyPath is not used by Evaluate directly
        };

        // Act
        bool actualResult = rule.Evaluate(propertyValue);

        // Assert
        actualResult.ShouldBe(expectedResult);
    }
} 