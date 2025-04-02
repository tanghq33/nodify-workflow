using System;
using System.Collections.Generic;

namespace Nodify.Workflow.Core.Graph.Interfaces
{
    /// <summary>
    /// Represents a connector on a node that can be used to create connections
    /// </summary>
    public interface IConnector
    {
        /// <summary>
        /// Unique identifier for the connector
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// The direction of the connector (Input/Output)
        /// </summary>
        ConnectorDirection Direction { get; }

        /// <summary>
        /// The node that owns this connector
        /// </summary>
        INode ParentNode { get; }

        /// <summary>
        /// The type of data this connector can handle
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// Collection of connections associated with this connector
        /// </summary>
        IReadOnlyCollection<IConnection> Connections { get; }

        /// <summary>
        /// Adds a connection to this connector
        /// </summary>
        /// <param name="connection">The connection to add</param>
        /// <returns>True if the connection was added successfully</returns>
        bool AddConnection(IConnection connection);

        /// <summary>
        /// Removes a connection from this connector
        /// </summary>
        /// <param name="connection">The connection to remove</param>
        /// <returns>True if the connection was removed successfully</returns>
        bool RemoveConnection(IConnection connection);

        /// <summary>
        /// Validates if a connection can be made with another connector
        /// </summary>
        /// <param name="other">The other connector to validate against</param>
        /// <returns>True if a connection is valid</returns>
        bool ValidateConnection(IConnector other);
    }

    /// <summary>
    /// Defines the direction of a connector
    /// </summary>
    public enum ConnectorDirection
    {
        Input,
        Output
    }
} 