using System;

namespace Nodify.Workflow.Core.Registry;

/// <summary>
/// Holds metadata about a discoverable workflow node type.
/// </summary>
public class NodeTypeMetadata
{
    /// <summary>
    /// Gets the actual <see cref="System.Type"/> of the node.
    /// </summary>
    public Type NodeType { get; }

    /// <summary>
    /// Gets the user-friendly name for display in UI or logs.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets a category for grouping nodes (e.g., "Logic", "Data", "Input/Output").
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets a brief description of the node's purpose.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeTypeMetadata"/> class.
    /// </summary>
    /// <param name="nodeType">The <see cref="System.Type"/> of the node.</param>
    /// <param name="displayName">The user-friendly display name.</param>
    /// <param name="category">The category for grouping.</param>
    /// <param name="description">An optional description.</param>
    public NodeTypeMetadata(Type nodeType, string displayName, string category, string description = "")
    {
        NodeType = nodeType ?? throw new ArgumentNullException(nameof(nodeType));
        DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : throw new ArgumentException("Display name cannot be null or whitespace.", nameof(displayName));
        Category = !string.IsNullOrWhiteSpace(category) ? category : throw new ArgumentException("Category cannot be null or whitespace.", nameof(category));
        Description = description ?? string.Empty;
    }
} 