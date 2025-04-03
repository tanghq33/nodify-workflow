namespace Nodify.Workflow.Core.Logic.Operators;

/// <summary>
/// Operators for string manipulation and comparison.
/// </summary>
public enum StringOperator
{
    Equals,         // Case-sensitive equality
    EqualsIgnoreCase, // Case-insensitive equality
    NotEquals,      // Case-sensitive inequality
    NotEqualsIgnoreCase, // Case-insensitive inequality
    Contains,
    ContainsIgnoreCase,
    StartsWith,
    StartsWithIgnoreCase,
    EndsWith,
    EndsWithIgnoreCase,
    IsEmpty,        // Check if string is null or empty
    IsNotEmpty      // Check if string is not null or empty
} 