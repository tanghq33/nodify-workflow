using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Models;

/// <summary>
/// Represents a base implementation for a node in the workflow graph.
/// Provides basic functionality for managing connectors and node properties.
/// </summary>
public abstract class Node : INode
{
    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public double X { get; set; }

    /// <inheritdoc />
    public double Y { get; set; }

    private readonly List<IConnector> _inputConnectors;
    private readonly List<IConnector> _outputConnectors;

    /// <inheritdoc />
    public IReadOnlyCollection<IConnector> InputConnectors => _inputConnectors.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyCollection<IConnector> OutputConnectors => _outputConnectors.AsReadOnly();

    /// <inheritdoc />
    public virtual void AddInputConnector(IConnector connector)
    {
        if (connector.Direction != ConnectorDirection.Input)
            throw new ArgumentException("Connector must be an input connector.", nameof(connector));
        _inputConnectors.Add(connector);
    }

    /// <inheritdoc />
    public virtual void AddOutputConnector(IConnector connector)
    {
        if (connector.Direction != ConnectorDirection.Output)
            throw new ArgumentException("Connector must be an output connector.", nameof(connector));
        _outputConnectors.Add(connector);
    }

    /// <inheritdoc />
    public bool RemoveConnector(IConnector connector)
    {
        if (connector.Direction == ConnectorDirection.Input)
        {
            return _inputConnectors.Remove(connector);
        }
        else
        {
            return _outputConnectors.Remove(connector);
        }
    }

    /// <inheritdoc />
    public IConnector? GetInputConnector(Guid id)
    {
        return _inputConnectors.FirstOrDefault(c => c.Id == id);
    }

    /// <inheritdoc />
    public IConnector? GetOutputConnector(Guid id)
    {
        return _outputConnectors.FirstOrDefault(c => c.Id == id);
    }

    /// <inheritdoc />
    public virtual bool Validate()
    {
        // Base validation: Ensure all connectors belong to this node.
        // Derived classes should override and potentially add more checks.
        bool inputsValid = InputConnectors.All(c => c != null && c.ParentNode == this);
        bool outputsValid = OutputConnectors.All(c => c != null && c.ParentNode == this);
        return inputsValid && outputsValid;
    }

    /// <inheritdoc />
    public abstract Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken);

    // Obsolete methods kept for reference or potential future adjustments
    [Obsolete("Use RemoveConnector instead.")]
    public virtual void RemoveInputConnector(IConnector connector)
    {
         _inputConnectors.Remove(connector);
    }

    [Obsolete("Use RemoveConnector instead.")]
    public virtual void RemoveOutputConnector(IConnector connector)
    {
        _outputConnectors.Remove(connector);
    }

    protected Node()
    {
        Id = Guid.NewGuid();
        _inputConnectors = new List<IConnector>();
        _outputConnectors = new List<IConnector>();
    }
}