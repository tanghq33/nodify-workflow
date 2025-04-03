using System;

namespace Nodify.Workflow.Core.Registry;

/// <summary>
/// Attribute used to mark an INode implementation as discoverable by the NodeRegistry
/// and provide basic metadata for UI or other purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class WorkflowNodeAttribute : Attribute
{
    /// <summary>
    /// Gets the user-friendly name for display.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the category for grouping nodes.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets or sets an optional description of the node's purpose.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Marks a class as a discoverable workflow node.
    /// </summary>
    /// <param name="displayName">The user-friendly name for display.</param>
    /// <param name="category">The category for grouping (e.g., "Logic", "Data").</param>
    /// <param name="description">An optional description.</param>
    public WorkflowNodeAttribute(string displayName, string category, string description = "")
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be null or whitespace.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or whitespace.", nameof(category));

        DisplayName = displayName;
        Category = category;
        Description = description ?? string.Empty;
    }
} 