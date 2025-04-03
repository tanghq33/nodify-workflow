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

    /// <summary>
    /// Creates a new connection between a source and target connector.
    /// Assumes basic validation (direction, type compatibility, circular reference) 
    /// has already been performed by the Graph class.
    /// </summary>
    public Connection(IConnector source, IConnector target)
    {
        Id = Guid.NewGuid();
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));

        // Constructor now assumes prior validation
        // if (source.Direction != ConnectorDirection.Output) ... removed
        // if (target.Direction != ConnectorDirection.Input) ... removed
        // if (!source.ValidateConnection(target)) ... removed

        // Add this connection to both connectors - this remains crucial
        // Consider potential race conditions if connectors are not thread-safe
        // For now, assuming Graph class lock handles external consistency.
        bool sourceAdded = Source.AddConnection(this);
        bool targetAdded = Target.AddConnection(this);

        // If adding to connectors fails (e.g., input already full due to race condition),
        // we need to roll back.
        if (!sourceAdded || !targetAdded)
        {
             // Rollback: Remove from whichever connector it was added to
             if (sourceAdded) Source.RemoveConnection(this);
             if (targetAdded) Target.RemoveConnection(this);
             // Throw an exception to signal failure to the Graph class
             throw new InvalidOperationException("Failed to add connection to one or both connectors. Possible concurrency issue or violation of connector rules.");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Basic validation. Assumes deeper validation (type compatibility, graph structure)
    /// is handled by the Graph or Connector classes.
    /// </remarks>
    public bool Validate()
    {
        // A connection is valid if its source and target are still valid (e.g., not null)
        // More complex validation (like ensuring connectors still belong to nodes in the graph)
        // would typically be done at the Graph level during its Validate() process.
        return Source != null && Target != null;
        // Optional: Check if source/target still belong to their parent nodes if needed here
        // return Source?.ParentNode != null && Target?.ParentNode != null;
    }

    /// <inheritdoc />
    public void Remove()
    {
        // Ensure removal from both connectors
        Source?.RemoveConnection(this);
        Target?.RemoveConnection(this);
    }
}