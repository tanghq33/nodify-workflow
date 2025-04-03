using System;
using System.Collections.Generic;
using Nodify.Workflow.Core.Interfaces;

namespace Nodify.Workflow.Core.Registry;

/// <summary>
/// Defines the contract for a registry that discovers and manages available workflow node types.
/// </summary>
public interface INodeRegistry
{
    /// <summary>
    /// Gets metadata for all discovered and registerable node types.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="NodeTypeMetadata"/>.</returns>
    IEnumerable<NodeTypeMetadata> GetAvailableNodeTypes();

    /// <summary>
    /// Creates a new instance of the specified node type.
    /// </summary>
    /// <param name="nodeType">The <see cref="System.Type"/> of the node to instantiate. This type must be discoverable by the registry.</param>
    /// <returns>A new instance of <see cref="INode"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="nodeType"/> is not registered or cannot be instantiated (e.g., abstract, no parameterless constructor).</exception>
    INode CreateNodeInstance(Type nodeType);

    /// <summary>
    /// Creates a new instance of the specified node type using its display name.
    /// </summary>
    /// <param name="displayName">The display name of the node type as defined in its metadata.</param>
    /// <returns>A new instance of <see cref="INode"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if no node type with the given display name is registered or if it cannot be instantiated.</exception>
    INode CreateNodeInstance(string displayName);
} 