using Nodify.Workflow.Core.Logic.Operators;
using System;
using System.Diagnostics;

namespace Nodify.Workflow.Core.Logic;

/// <summary>
/// Rule for comparing numeric values.
/// </summary>
public class NumericConditionRule : ConditionRuleBase
{
    /// <summary>
    /// The numeric comparison operator to use.
    /// </summary>
    public NumericOperator Operator { get; set; }

    /// <summary>
    /// The numeric value to compare against. Stored as double for flexibility.
    /// </summary>
    public double ComparisonValue { get; set; }

    public override bool Evaluate(object? propertyValue)
    {
        if (propertyValue == null) return false; // Cannot compare null numerically

        // Try to convert the property value to a double
        double propertyValueDouble;
        try
        {
            propertyValueDouble = Convert.ToDouble(propertyValue);
        }
        catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
        {
            Debug.WriteLine($"NumericConditionRule: Could not convert property value '{propertyValue}' to double. {ex.Message}");
            return false; // Cannot perform numeric comparison
        }

        // Perform comparison
        int comparisonResult = propertyValueDouble.CompareTo(ComparisonValue);

        return Operator switch
        {
            NumericOperator.Equals => comparisonResult == 0,
            NumericOperator.NotEquals => comparisonResult != 0,
            NumericOperator.GreaterThan => comparisonResult > 0,
            NumericOperator.LessThan => comparisonResult < 0,
            NumericOperator.GreaterThanOrEqual => comparisonResult >= 0,
            NumericOperator.LessThanOrEqual => comparisonResult <= 0,
            _ => false // Should not happen
        };
    }
} 