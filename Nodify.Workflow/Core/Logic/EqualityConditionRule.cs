using Nodify.Workflow.Core.Logic.Operators;
using System;

namespace Nodify.Workflow.Core.Logic;

/// <summary>
/// Rule for comparing object equality.
/// </summary>
public class EqualityConditionRule : ConditionRuleBase
{
    /// <summary>
    /// The equality operator to use.
    /// </summary>
    public EqualityOperator Operator { get; set; }

    /// <summary>
    /// The value to compare against.
    /// </summary>
    public object? ComparisonValue { get; set; }

    public override bool Evaluate(object? propertyValue)
    {
        bool result = object.Equals(propertyValue, ComparisonValue);
        return Operator == EqualityOperator.Equals ? result : !result;
    }
} 