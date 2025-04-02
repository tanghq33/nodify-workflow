using System;

namespace Nodify.Workflow.Core.Graph.Interfaces
{
    /// <summary>
    /// Represents a connection between two connectors in the workflow graph
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Unique identifier for the connection
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// The source (output) connector of the connection
        /// </summary>
        IConnector Source { get; }

        /// <summary>
        /// The target (input) connector of the connection
        /// </summary>
        IConnector Target { get; }

        /// <summary>
        /// Validates if the connection is valid
        /// </summary>
        /// <returns>True if the connection is valid</returns>
        bool Validate();

        /// <summary>
        /// Removes this connection from its source and target connectors
        /// </summary>
        void Remove();

        /// <summary>
        /// Checks if this connection would create a circular reference in the graph
        /// </summary>
        /// <returns>True if the connection would create a circular reference</returns>
        bool WouldCreateCircularReference();
    }
} 