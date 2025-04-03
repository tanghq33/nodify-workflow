using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;

namespace Nodify.Workflow.Core.Models;

/// <summary>
/// Base implementation of a connection in the workflow graph
/// </summary>
public class Connection : IConnection
{
    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public IConnector Source { get; }

    /// <inheritdoc />
    public IConnector Target { get; }

    public Connection(IConnector source, IConnector target)
    {
        Id = Guid.NewGuid();
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));

        if (source.Direction != ConnectorDirection.Output)
            throw new ArgumentException("Source connector must be an output", nameof(source));

        if (target.Direction != ConnectorDirection.Input)
            throw new ArgumentException("Target connector must be an input", nameof(target));

        if (!source.ValidateConnection(target))
            throw new ArgumentException("Invalid connection between connectors");

        // Add this connection to both connectors
        source.AddConnection(this);
        target.AddConnection(this);
    }

    /// <inheritdoc />
    public bool Validate()
    {
        if (Source.Direction != ConnectorDirection.Output)
            return false;

        if (Target.Direction != ConnectorDirection.Input)
            return false;

        // Validate type compatibility
        if (!Source.ValidateConnection(Target))
            return false;

        return true;
    }

    /// <inheritdoc />
    public void Remove()
    {
        Source.RemoveConnection(this);
        Target.RemoveConnection(this);
    }

    /// <inheritdoc />
    public bool WouldCreateCircularReference()
    {
        var visited = new HashSet<INode>();
        return HasCircularReference(Target.ParentNode, visited);
    }

    private bool HasCircularReference(INode currentNode, HashSet<INode> visited)
    {
        if (currentNode == Source.ParentNode)
            return true;

        if (!visited.Add(currentNode))
            return false;

        // Check all output connections from this node
        foreach (var connector in currentNode.OutputConnectors)
        {
            foreach (var connection in connector.Connections)
            {
                if (HasCircularReference(connection.Target.ParentNode, visited))
                    return true;
            }
        }

        visited.Remove(currentNode);
        return false;
    }
}