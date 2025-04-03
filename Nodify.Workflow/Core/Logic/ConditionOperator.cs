namespace Nodify.Workflow.Core.Logic;

/// <summary>
/// Defines the types of operations available for conditions in the IfElseNode.
/// </summary>
public enum ConditionOperator
{
    // General Equality
    Equals,
    NotEquals,

    // Numeric/Comparable Comparison
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,

    // String Operations
    Contains,
    StartsWith,
    EndsWith,

    // Existence/Collection/String Checks
    IsEmpty,      // Check if string or collection is empty
    IsNotEmpty,   // Check if string or collection is not empty
    
    // Null Checks
    IsNull,       // Check if the property value itself is null
    IsNotNull     // Check if the property value itself is not null
} 