using System;
using System.Collections.Generic;

namespace Nodify.Workflow.Core.Interfaces;

/// <summary>
/// Represents the central structure of a workflow, containing nodes and the connections between them.
/// Provides methods for manipulating and validating the graph topology.
/// </summary>
public interface IGraph
{
    /// <summary>
    /// Gets a read-only collection of all nodes currently present in the graph.
    /// Note: This is typically a snapshot and may not reflect changes made after retrieval.
    /// </summary>
    IReadOnlyCollection<INode> Nodes { get; }

    /// <summary>
    /// Gets a read-only collection of all connections currently present in the graph.
    /// Note: This is typically a snapshot and may not reflect changes made after retrieval.
    /// </summary>
    IReadOnlyCollection<IConnection> Connections { get; }

    /// <summary>
    /// Adds a node instance to the graph.
    /// </summary>
    /// <param name="node">The node instance to add. Must not be null.</param>
    /// <returns>True if the node was successfully added (e.g., no ID conflict), false otherwise.</returns>
    bool AddNode(INode node);

    /// <summary>
    /// Removes a node and all connections associated with its connectors from the graph.
    /// </summary>
    /// <param name="node">The node instance to remove. Must not be null.</param>
    /// <returns>True if the node was found and successfully removed, false otherwise.</returns>
    bool RemoveNode(INode node);

    /// <summary>
    /// Retrieves a specific node from the graph based on its unique identifier.
    /// </summary>
    /// <param name="id">The unique <see cref="Guid"/> identifier of the node to find.</param>
    /// <returns>The <see cref="INode"/> instance if found; otherwise, null.</returns>
    INode GetNodeById(Guid id);

    /// <summary>
    /// Creates and adds a connection between a specified source (output) connector and a target (input) connector.
    /// Implementations should perform validation checks (e.g., direction, type compatibility, node existence, circular references).
    /// </summary>
    /// <param name="sourceConnector">The source connector (must be an <see cref="ConnectorDirection.Output"/>).</param>
    /// <param name="targetConnector">The target connector (must be an <see cref="ConnectorDirection.Input"/>).</param>
    /// <returns>The newly created <see cref="IConnection"/> instance if the connection is valid and successfully added; otherwise, null.</returns>
    IConnection AddConnection(IConnector sourceConnector, IConnector targetConnector);

    /// <summary>
    /// Removes a specific connection instance from the graph.
    /// This also involves removing the connection reference from the source and target connectors involved.
    /// </summary>
    /// <param name="connection">The connection instance to remove. Must not be null.</param>
    /// <returns>True if the connection was found and successfully removed, false otherwise.</returns>
    bool RemoveConnection(IConnection connection);

    /// <summary>
    /// Validates the overall integrity and correctness of the graph structure.
    /// This may include checks for orphaned connections, node-level validation, and potentially more complex rules.
    /// </summary>
    /// <returns>True if the entire graph structure is considered valid, false otherwise.</returns>
    bool Validate();
}