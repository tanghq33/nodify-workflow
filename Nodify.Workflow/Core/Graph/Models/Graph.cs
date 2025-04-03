using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Graph.Interfaces;

namespace Nodify.Workflow.Core.Graph.Models
{
    /// <summary>
    /// Implementation of a workflow graph that manages nodes and their connections
    /// </summary>
    public class Graph : IGraph
    {
        private readonly List<INode> _nodes;
        private readonly List<IConnection> _connections;

        public Graph()
        {
            _nodes = new List<INode>();
            _connections = new List<IConnection>();
        }

        /// <inheritdoc />
        public IReadOnlyCollection<INode> Nodes => _nodes.AsReadOnly();

        /// <inheritdoc />
        public IReadOnlyCollection<IConnection> Connections => _connections.AsReadOnly();

        /// <inheritdoc />
        public bool AddNode(INode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (_nodes.Contains(node))
                return false;

            _nodes.Add(node);
            return true;
        }

        /// <inheritdoc />
        public bool RemoveNode(INode node)
        {
            if (node == null || !_nodes.Contains(node))
                return false;

            // Remove all connections associated with this node
            var connectionsToRemove = _connections
                .Where(c => c.Source.ParentNode == node || c.Target.ParentNode == node)
                .ToList();

            foreach (var connection in connectionsToRemove)
            {
                RemoveConnection(connection);
            }

            return _nodes.Remove(node);
        }

        /// <inheritdoc />
        public INode GetNodeById(Guid id)
        {
            return _nodes.FirstOrDefault(n => n.Id == id);
        }

        /// <inheritdoc />
        public IConnection AddConnection(IConnector sourceConnector, IConnector targetConnector)
        {
            if (sourceConnector == null)
                throw new ArgumentNullException(nameof(sourceConnector));
            if (targetConnector == null)
                throw new ArgumentNullException(nameof(targetConnector));

            // Verify both nodes are in the graph
            if (!_nodes.Contains(sourceConnector.ParentNode) || !_nodes.Contains(targetConnector.ParentNode))
                return null;

            try
            {
                var connection = new Connection(sourceConnector, targetConnector);
                
                // Check for circular references
                if (connection.WouldCreateCircularReference())
                {
                    connection.Remove();
                    return null;
                }

                _connections.Add(connection);
                return connection;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public bool RemoveConnection(IConnection connection)
        {
            if (connection == null || !_connections.Contains(connection))
                return false;

            connection.Remove();
            return _connections.Remove(connection);
        }

        /// <inheritdoc />
        public bool Validate()
        {
            // Check that all nodes are valid
            if (!_nodes.All(n => n.Validate()))
                return false;

            // Check that all connections are valid
            if (!_connections.All(c => c.Validate()))
                return false;

            // Check for circular references
            foreach (var connection in _connections)
            {
                if (connection.WouldCreateCircularReference())
                    return false;
            }

            return true;
        }
    }
} 