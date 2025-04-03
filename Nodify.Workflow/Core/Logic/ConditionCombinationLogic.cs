namespace Nodify.Workflow.Core.Logic;

/// <summary>
/// Specifies how multiple conditions in an IfElseNode should be combined.
/// </summary>
public enum ConditionCombinationLogic
{
    /// <summary>
    /// All conditions must evaluate to true.
    /// </summary>
    And,
    /// <summary>
    /// At least one condition must evaluate to true.
    /// </summary>
    Or
} 