using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;

namespace Nodify.Workflow.Core.Models;

/// <summary>
/// Base implementation of a workflow node
/// </summary>
public class Node : INode
{
    private readonly List<IConnector> _inputConnectors;
    private readonly List<IConnector> _outputConnectors;

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<IConnector> InputConnectors => _inputConnectors.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyCollection<IConnector> OutputConnectors => _outputConnectors.AsReadOnly();

    /// <inheritdoc />
    public double X { get; set; }

    /// <inheritdoc />
    public double Y { get; set; }

    public Node()
    {
        Id = Guid.NewGuid();
        _inputConnectors = new List<IConnector>();
        _outputConnectors = new List<IConnector>();
    }

    /// <inheritdoc />
    public void AddInputConnector(IConnector connector)
    {
        if (connector == null)
            throw new ArgumentNullException(nameof(connector));

        if (connector.Direction != ConnectorDirection.Input)
            throw new ArgumentException("Connector must be an input connector", nameof(connector));

        _inputConnectors.Add(connector);
    }

    /// <inheritdoc />
    public void AddOutputConnector(IConnector connector)
    {
        if (connector == null)
            throw new ArgumentNullException(nameof(connector));

        if (connector.Direction != ConnectorDirection.Output)
            throw new ArgumentException("Connector must be an output connector", nameof(connector));

        _outputConnectors.Add(connector);
    }

    /// <inheritdoc />
    public bool RemoveConnector(IConnector connector)
    {
        if (connector == null)
            return false;

        // Remove all connections from the connector before removing it
        foreach (var connection in connector.Connections.ToList())
        {
            connection.Remove();
        }

        return connector.Direction == ConnectorDirection.Input
            ? _inputConnectors.Remove(connector)
            : _outputConnectors.Remove(connector);
    }

    /// <inheritdoc />
    public bool Validate()
    {
        // Basic validation - ensure all connectors are valid
        return InputConnectors.All(c => c != null && c.ParentNode == this)
            && OutputConnectors.All(c => c != null && c.ParentNode == this);
    }
}