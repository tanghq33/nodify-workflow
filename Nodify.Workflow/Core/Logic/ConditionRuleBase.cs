using System;

namespace Nodify.Workflow.Core.Logic;

/// <summary>
/// Base class for defining conditions to be evaluated by nodes like IfElseNode.
/// </summary>
public abstract class ConditionRuleBase
{
    /// <summary>
    /// Dot-notation path to the property to evaluate within the input object.
    /// Example: "User.Address.City"
    /// </summary>
    public string PropertyPath { get; set; } = string.Empty;

    /// <summary>
    /// Evaluates the condition based on the resolved property value.
    /// </summary>
    /// <param name="propertyValue">The actual value retrieved using the PropertyPath.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    public abstract bool Evaluate(object? propertyValue);
} 