using System;
using System.Collections.Generic;
using Nodify.Workflow.Core.Graph.Interfaces;

namespace Nodify.Workflow.Core.Graph.Models
{
    /// <summary>
    /// Base implementation of a connector in the workflow graph
    /// </summary>
    public class Connector : IConnector
    {
        private readonly List<IConnection> _connections;

        /// <inheritdoc />
        public Guid Id { get; }

        /// <inheritdoc />
        public ConnectorDirection Direction { get; }

        /// <inheritdoc />
        public INode ParentNode { get; }

        /// <inheritdoc />
        public Type DataType { get; }

        /// <inheritdoc />
        public IReadOnlyCollection<IConnection> Connections => _connections.AsReadOnly();

        public Connector(INode parentNode, ConnectorDirection direction, Type dataType)
        {
            Id = Guid.NewGuid();
            ParentNode = parentNode ?? throw new ArgumentNullException(nameof(parentNode));
            Direction = direction;
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            _connections = new List<IConnection>();
        }

        /// <inheritdoc />
        public bool AddConnection(IConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // Validate the connection before adding
            if (!ValidateConnection(Direction == ConnectorDirection.Input ? connection.Source : connection.Target))
                return false;

            _connections.Add(connection);
            return true;
        }

        /// <inheritdoc />
        public bool RemoveConnection(IConnection connection)
        {
            if (connection == null)
                return false;

            return _connections.Remove(connection);
        }

        /// <inheritdoc />
        public bool ValidateConnection(IConnector other)
        {
            if (other == null)
                return false;

            // Check direction compatibility
            if (Direction == other.Direction)
                return false;

            // Check data type compatibility
            if (!IsTypeCompatible(other.DataType))
                return false;

            // For input connectors, only allow one connection
            if (Direction == ConnectorDirection.Input && Connections.Count > 0)
                return false;

            return true;
        }

        private bool IsTypeCompatible(Type otherType)
        {
            if (otherType == null)
                return false;

            // Check if types match exactly or if there's an inheritance relationship
            return DataType == otherType || 
                   DataType.IsAssignableFrom(otherType) || 
                   otherType.IsAssignableFrom(DataType);
        }
    }
} 