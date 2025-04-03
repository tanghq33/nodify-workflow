using Nodify.Workflow.Core.Logic.Operators;
using System;

namespace Nodify.Workflow.Core.Logic;

/// <summary>
/// Rule for checking if a property value is null or not null.
/// </summary>
public class NullConditionRule : ConditionRuleBase
{
    /// <summary>
    /// The null check operator to use.
    /// </summary>
    public NullOperator Operator { get; set; }

    // No ComparisonValue needed for null checks

    public override bool Evaluate(object? propertyValue)
    {
        bool isNull = propertyValue == null;
        return Operator == NullOperator.IsNull ? isNull : !isNull;
    }
} 