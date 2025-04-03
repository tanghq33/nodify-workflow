using Nodify.Workflow.Core.Interfaces;
using System.Collections.Concurrent;

namespace Nodify.Workflow.Core.Models;

/// <summary>
/// Implementation of a workflow graph that manages nodes and their connections
/// </summary>
public class Graph : IGraph
{
    private readonly ConcurrentDictionary<Guid, INode> _nodesById;
    private readonly ConcurrentDictionary<Guid, IConnection> _connectionsById;
    private readonly object _modificationLock = new object();

    /// <summary>
    /// Represents the result of an operation that could fail with a specific error
    /// </summary>
    public record struct OperationResult<T>(bool Success, T Result, string ErrorMessage);

    public Graph()
    {
        _nodesById = new ConcurrentDictionary<Guid, INode>();
        _connectionsById = new ConcurrentDictionary<Guid, IConnection>();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<INode> Nodes => _nodesById.Values.ToList().AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyCollection<IConnection> Connections => _connectionsById.Values.ToList().AsReadOnly();

    /// <summary>
    /// Adds a node to the graph with detailed operation result
    /// </summary>
    /// <param name="node">The node to add</param>
    /// <returns>Operation result indicating success or failure with error message</returns>
    public OperationResult<INode> TryAddNode(INode node)
    {
        if (node == null)
            return new OperationResult<INode>(false, null, "Node cannot be null");

        if (_nodesById.ContainsKey(node.Id))
            return new OperationResult<INode>(false, null, "Node with same ID already exists");

        if (_nodesById.TryAdd(node.Id, node))
            return new OperationResult<INode>(true, node, string.Empty);

        return new OperationResult<INode>(false, null, "Failed to add node to graph");
    }

    /// <inheritdoc />
    public bool AddNode(INode node)
    {
        var result = TryAddNode(node);
        return result.Success;
    }

    /// <summary>
    /// Removes a node and all its connections from the graph with detailed operation result
    /// </summary>
    /// <param name="node">The node to remove</param>
    /// <returns>Operation result indicating success or failure with error message</returns>
    public OperationResult<bool> TryRemoveNode(INode node)
    {
        if (node == null)
            return new OperationResult<bool>(false, false, "Node cannot be null");

        if (!_nodesById.ContainsKey(node.Id))
            return new OperationResult<bool>(false, false, "Node not found in graph");

        lock (_modificationLock)
        {
            // Remove all connections associated with this node
            var connectionsToRemove = _connectionsById.Values
                .Where(c => c.Source.ParentNode.Id == node.Id || c.Target.ParentNode.Id == node.Id)
                .ToList();

            foreach (var connection in connectionsToRemove)
            {
                RemoveConnection(connection);
            }

            if (_nodesById.TryRemove(node.Id, out _))
                return new OperationResult<bool>(true, true, string.Empty);
        }

        return new OperationResult<bool>(false, false, "Failed to remove node from graph");
    }

    /// <inheritdoc />
    public bool RemoveNode(INode node)
    {
        var result = TryRemoveNode(node);
        return result.Success;
    }

    /// <inheritdoc />
    public INode GetNodeById(Guid id)
    {
        _nodesById.TryGetValue(id, out var node);
        return node;
    }

    /// <summary>
    /// Adds a connection between two connectors with detailed operation result
    /// </summary>
    /// <returns>Operation result containing the created connection or error details</returns>
    public OperationResult<IConnection> TryAddConnection(IConnector sourceConnector, IConnector targetConnector)
    {
        if (sourceConnector == null)
            return new OperationResult<IConnection>(false, null, "Source connector cannot be null");
        if (targetConnector == null)
            return new OperationResult<IConnection>(false, null, "Target connector cannot be null");

        // Verify both nodes are in the graph
        if (!_nodesById.ContainsKey(sourceConnector.ParentNode.Id) || 
            !_nodesById.ContainsKey(targetConnector.ParentNode.Id))
            return new OperationResult<IConnection>(false, null, "One or both nodes not found in graph");

        try
        {
            lock (_modificationLock)
            {
                var connection = new Connection(sourceConnector, targetConnector);

                if (connection.WouldCreateCircularReference())
                {
                    connection.Remove();
                    return new OperationResult<IConnection>(false, null, "Connection would create circular reference");
                }

                if (_connectionsById.TryAdd(connection.Id, connection))
                    return new OperationResult<IConnection>(true, connection, string.Empty);
            }
        }
        catch (ArgumentException ex)
        {
            return new OperationResult<IConnection>(false, null, ex.Message);
        }

        return new OperationResult<IConnection>(false, null, "Failed to add connection to graph");
    }

    /// <inheritdoc />
    public IConnection AddConnection(IConnector sourceConnector, IConnector targetConnector)
    {
        var result = TryAddConnection(sourceConnector, targetConnector);
        return result.Result;
    }

    /// <summary>
    /// Removes a connection from the graph with detailed operation result
    /// </summary>
    /// <returns>Operation result indicating success or failure with error message</returns>
    public OperationResult<bool> TryRemoveConnection(IConnection connection)
    {
        if (connection == null)
            return new OperationResult<bool>(false, false, "Connection cannot be null");

        if (!_connectionsById.ContainsKey(connection.Id))
            return new OperationResult<bool>(false, false, "Connection not found in graph");

        lock (_modificationLock)
        {
            connection.Remove();
            if (_connectionsById.TryRemove(connection.Id, out _))
                return new OperationResult<bool>(true, true, string.Empty);
        }

        return new OperationResult<bool>(false, false, "Failed to remove connection from graph");
    }

    /// <inheritdoc />
    public bool RemoveConnection(IConnection connection)
    {
        var result = TryRemoveConnection(connection);
        return result.Success;
    }

    /// <summary>
    /// Validates the entire graph structure with detailed results
    /// </summary>
    /// <returns>Operation result with validation details</returns>
    public OperationResult<bool> TryValidate()
    {
        var invalidNodes = _nodesById.Values.Where(n => !n.Validate()).ToList();
        if (invalidNodes.Any())
            return new OperationResult<bool>(false, false, $"Invalid nodes found: {string.Join(", ", invalidNodes.Select(n => n.Id))}");

        var invalidConnections = _connectionsById.Values.Where(c => !c.Validate()).ToList();
        if (invalidConnections.Any())
            return new OperationResult<bool>(false, false, $"Invalid connections found: {string.Join(", ", invalidConnections.Select(c => c.Id))}");

        var circularConnections = _connectionsById.Values.Where(c => c.WouldCreateCircularReference()).ToList();
        if (circularConnections.Any())
            return new OperationResult<bool>(false, false, $"Circular references found: {string.Join(", ", circularConnections.Select(c => c.Id))}");

        return new OperationResult<bool>(true, true, string.Empty);
    }

    /// <inheritdoc />
    public bool Validate()
    {
        var result = TryValidate();
        return result.Success;
    }
}