using System;
using System.Collections.Generic;

namespace Nodify.Workflow.Core.Graph.Interfaces
{
    /// <summary>
    /// Represents a workflow graph containing nodes and their connections
    /// </summary>
    public interface IGraph
    {
        /// <summary>
        /// Collection of all nodes in the graph
        /// </summary>
        IReadOnlyCollection<INode> Nodes { get; }

        /// <summary>
        /// Collection of all connections in the graph
        /// </summary>
        IReadOnlyCollection<IConnection> Connections { get; }

        /// <summary>
        /// Adds a node to the graph
        /// </summary>
        /// <param name="node">The node to add</param>
        /// <returns>True if the node was added successfully</returns>
        bool AddNode(INode node);

        /// <summary>
        /// Removes a node and all its connections from the graph
        /// </summary>
        /// <param name="node">The node to remove</param>
        /// <returns>True if the node was removed successfully</returns>
        bool RemoveNode(INode node);

        /// <summary>
        /// Gets a node by its unique identifier
        /// </summary>
        /// <param name="id">The ID of the node to find</param>
        /// <returns>The node if found, null otherwise</returns>
        INode GetNodeById(Guid id);

        /// <summary>
        /// Adds a connection between two nodes in the graph
        /// </summary>
        /// <param name="sourceConnector">The source connector</param>
        /// <param name="targetConnector">The target connector</param>
        /// <returns>The created connection if successful, null otherwise</returns>
        IConnection AddConnection(IConnector sourceConnector, IConnector targetConnector);

        /// <summary>
        /// Removes a connection from the graph
        /// </summary>
        /// <param name="connection">The connection to remove</param>
        /// <returns>True if the connection was removed successfully</returns>
        bool RemoveConnection(IConnection connection);

        /// <summary>
        /// Validates the entire graph structure
        /// </summary>
        /// <returns>True if the graph is valid</returns>
        bool Validate();
    }
} 