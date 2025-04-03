using Nodify.Workflow.Core.Logic.Operators;
using System;
using System.Diagnostics;

namespace Nodify.Workflow.Core.Logic;

/// <summary>
/// Rule for comparing and evaluating string values.
/// </summary>
public class StringConditionRule : ConditionRuleBase
{
    /// <summary>
    /// The string comparison or evaluation operator to use.
    /// </summary>
    public StringOperator Operator { get; set; }

    /// <summary>
    /// The string value to compare against. Ignored for IsEmpty/IsNotEmpty.
    /// </summary>
    public string? ComparisonValue { get; set; }

    public override bool Evaluate(object? propertyValue)
    {
        // Handle operators that work directly on the object/string presence
        if (Operator == StringOperator.IsEmpty)
        {
            return string.IsNullOrEmpty(propertyValue as string);
        }
        if (Operator == StringOperator.IsNotEmpty)
        {
            return !string.IsNullOrEmpty(propertyValue as string);
        }

        // For other operators, we need propertyValue to be a non-null string
        if (propertyValue is not string propertyStringValue)
        {
            // Log or handle cases where the property is null or not a string?
            // For now, return false as string operations aren't applicable.
            Debug.WriteLine($"StringConditionRule: Property value is null or not a string ('{propertyValue?.GetType().Name ?? "null"}') for operator {Operator}.");
            return false;
        }

        // Handle operators that require a comparison value
        // Check if ComparisonValue is required but null for certain operators
        bool comparisonValueRequired = Operator is StringOperator.Equals or StringOperator.EqualsIgnoreCase or
                                       StringOperator.NotEquals or StringOperator.NotEqualsIgnoreCase or
                                       StringOperator.Contains or StringOperator.ContainsIgnoreCase or
                                       StringOperator.StartsWith or StringOperator.StartsWithIgnoreCase or
                                       StringOperator.EndsWith or StringOperator.EndsWithIgnoreCase;

        if (comparisonValueRequired && ComparisonValue == null)
        {
            Debug.WriteLine($"StringConditionRule: ComparisonValue is null but required for operator {Operator}.");
            return false; 
        }
        
        // We know ComparisonValue is not null if required for the switch cases below
        string comparisonNonNull = ComparisonValue ?? string.Empty; 

        return Operator switch
        {
            StringOperator.Equals => propertyStringValue.Equals(comparisonNonNull, StringComparison.Ordinal),
            StringOperator.EqualsIgnoreCase => propertyStringValue.Equals(comparisonNonNull, StringComparison.OrdinalIgnoreCase),
            StringOperator.NotEquals => !propertyStringValue.Equals(comparisonNonNull, StringComparison.Ordinal),
            StringOperator.NotEqualsIgnoreCase => !propertyStringValue.Equals(comparisonNonNull, StringComparison.OrdinalIgnoreCase),
            
            StringOperator.Contains => propertyStringValue.Contains(comparisonNonNull),
            StringOperator.ContainsIgnoreCase => propertyStringValue.IndexOf(comparisonNonNull, StringComparison.OrdinalIgnoreCase) >= 0,
            
            StringOperator.StartsWith => propertyStringValue.StartsWith(comparisonNonNull, StringComparison.Ordinal),
            StringOperator.StartsWithIgnoreCase => propertyStringValue.StartsWith(comparisonNonNull, StringComparison.OrdinalIgnoreCase),
            
            StringOperator.EndsWith => propertyStringValue.EndsWith(comparisonNonNull, StringComparison.Ordinal),
            StringOperator.EndsWithIgnoreCase => propertyStringValue.EndsWith(comparisonNonNull, StringComparison.OrdinalIgnoreCase),
            
            // IsEmpty/IsNotEmpty handled above
            _ => false // Should not happen
        };
    }
} 