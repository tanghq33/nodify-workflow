using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;

namespace Nodify.Workflow.Core.Models;

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

        // Connection must be between an Input and an Output connector
        if (Direction == other.Direction)
            return false;

        // Determine source (output) and target (input) based on direction
        IConnector source = Direction == ConnectorDirection.Output ? this : other;
        IConnector target = Direction == ConnectorDirection.Input ? this : other;

        // Check data type compatibility: target must be assignable from source
        if (!IsTypeCompatible(source.DataType, target.DataType))
            return false;

        // Input connectors constraints (e.g., allow only one connection)
        if (target.Direction == ConnectorDirection.Input && target.Connections.Count > 0)
        {
            // Check if the existing connection involves the same source
            // Allows reconnecting the same source, but prevents multiple different sources.
            if (target.Connections.Any(c => c.Source != source))
            {
                return false;
            }
        }

        // Output connectors constraints (if any in the future, e.g., max connections)
        // if (source.Direction == ConnectorDirection.Output && source.Connections.Count >= MaxOutputConnections)
        //     return false;

        return true;
    }

    /// <summary>
    /// Checks if the target data type is assignable from the source data type.
    /// </summary>
    /// <param name="sourceType">The data type of the source (output) connector.</param>
    /// <param name="targetType">The data type of the target (input) connector.</param>
    /// <returns>True if the types are compatible for connection.</returns>
    private bool IsTypeCompatible(Type sourceType, Type targetType)
    {
        if (sourceType == null || targetType == null)
            return false;

        // Target type must be assignable from source type (e.g., target is base class or interface)
        return targetType.IsAssignableFrom(sourceType);
    }
}