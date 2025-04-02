using System;
using System.Collections.Generic;

namespace Nodify.Workflow.Core.Graph.Interfaces
{
    /// <summary>
    /// Represents a node in the workflow graph
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Unique identifier for the node
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Collection of input connectors
        /// </summary>
        IReadOnlyCollection<IConnector> InputConnectors { get; }

        /// <summary>
        /// Collection of output connectors
        /// </summary>
        IReadOnlyCollection<IConnector> OutputConnectors { get; }

        /// <summary>
        /// X coordinate of the node in the graph
        /// </summary>
        double X { get; set; }

        /// <summary>
        /// Y coordinate of the node in the graph
        /// </summary>
        double Y { get; set; }

        /// <summary>
        /// Adds an input connector to the node
        /// </summary>
        /// <param name="connector">The connector to add</param>
        void AddInputConnector(IConnector connector);

        /// <summary>
        /// Adds an output connector to the node
        /// </summary>
        /// <param name="connector">The connector to add</param>
        void AddOutputConnector(IConnector connector);

        /// <summary>
        /// Removes a connector from the node
        /// </summary>
        /// <param name="connector">The connector to remove</param>
        /// <returns>True if the connector was removed successfully</returns>
        bool RemoveConnector(IConnector connector);

        /// <summary>
        /// Validates the node's state
        /// </summary>
        /// <returns>True if the node is in a valid state</returns>
        bool Validate();
    }
} 