using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;
// Note: System.Threading.Tasks was added for potential future async operations but isn't strictly needed for the current synchronous implementation.

namespace Nodify.Workflow.Core.Models;

/// <summary>
/// Provides a default, thread-safe implementation of the <see cref="IGraph"/> interface.
/// Manages the workflow graph structure, including nodes and connections, using concurrent collections and locking for atomic modifications.
/// </summary>
/// <remarks>
/// This implementation uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for efficient read operations and basic thread safety.
/// A dedicated lock (<c>_modificationLock</c>) is used to ensure atomicity for complex operations like adding/removing nodes and connections,
/// preventing race conditions and maintaining graph consistency.
/// Methods like <c>TryAddNode</c>, <c>TryRemoveNode</c>, <c>TryAddConnection</c>, and <c>TryRemoveConnection</c>
/// provide detailed results via <see cref="OperationResult{T}"/>, which is recommended for robust error handling.
/// The simpler wrapper methods (<c>AddNode</c>, <c>RemoveNode</c>, etc.) return booleans or null on failure.
/// </remarks>
public class Graph : IGraph
{
    // Concurrent dictionaries for thread-safe read operations and add/remove attempts.
    private readonly ConcurrentDictionary<Guid, INode> _nodesById;
    private readonly ConcurrentDictionary<Guid, IConnection> _connectionsById;
    // A single lock object to ensure atomicity for operations modifying graph structure
    // (e.g., removing a node and its connections, adding a connection after validation).
    private readonly object _modificationLock = new object();

    public Graph()
    {
        _nodesById = new ConcurrentDictionary<Guid, INode>();
        _connectionsById = new ConcurrentDictionary<Guid, IConnection>();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Provides a snapshot of the nodes at the time of access. The returned collection is read-only
    /// and will not reflect subsequent modifications to the graph.
    /// </remarks>
    public IReadOnlyCollection<INode> Nodes => _nodesById.Values.ToList().AsReadOnly();

    /// <inheritdoc />
    /// <remarks>
    /// Provides a snapshot of the connections at the time of access. The returned collection is read-only
    /// and will not reflect subsequent modifications to the graph.
    /// </remarks>
    public IReadOnlyCollection<IConnection> Connections => _connectionsById.Values.ToList().AsReadOnly();

    /// <summary>
    /// Attempts to add a node to the graph in a thread-safe manner.
    /// </summary>
    /// <param name="node">The node to add. Must not be null and should have a unique ID.</param>
    /// <returns>An <see cref="OperationResult{INode}"/> indicating success (with the added node) or failure (with an error message).</returns>
    /// <example>
    /// <code>
    /// var graph = new Graph();
    /// var node = new MyCustomNode(); // Assuming MyCustomNode implements INode
    /// var result = graph.TryAddNode(node);
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"Node {result.Result.Id} added successfully.");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Failed to add node: {result.ErrorMessage}");
    /// }
    /// </code>
    /// </example>
    public OperationResult<INode> TryAddNode(INode node)
    {
        if (node == null)
            return new OperationResult<INode>(false, default, "Node cannot be null.");

        // Attempt to add using ConcurrentDictionary's atomic TryAdd.
        if (_nodesById.TryAdd(node.Id, node))
            return new OperationResult<INode>(true, node, string.Empty);
        else
            // If TryAdd fails, it's likely because the key (ID) already exists.
            return new OperationResult<INode>(false, default, $"Node with ID {node.Id} already exists or another error occurred.");
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is a convenience wrapper for <see cref="TryAddNode"/>. It returns true on success and false on failure,
    /// discarding detailed error information. Use <see cref="TryAddNode"/> for more control over error handling.
    /// </remarks>
    public bool AddNode(INode node)
    {
        // Simple wrapper for convenience, discards detailed error message.
        return TryAddNode(node).Success;
    }

    /// <summary>
    /// Attempts to remove a node and all its associated connections from the graph atomically.
    /// This operation acquires a lock to ensure consistency.
    /// </summary>
    /// <param name="node">The node to remove. Must not be null.</param>
    /// <returns>An <see cref="OperationResult{Boolean}"/> indicating success or failure, with an error message on failure.</returns>
    /// <example>
    /// <code>
    /// // Assumes graph and node exist
    /// var result = graph.TryRemoveNode(nodeToRemove);
    /// if (result.Success)
    /// {
    ///     Console.WriteLine("Node and its connections removed.");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Failed to remove node: {result.ErrorMessage}");
    /// }
    /// </code>
    /// </example>
    public OperationResult<bool> TryRemoveNode(INode node)
    {
        if (node == null)
            return new OperationResult<bool>(false, false, "Node cannot be null.");

        // Check if node exists before locking (optimization).
        if (!_nodesById.ContainsKey(node.Id))
            return new OperationResult<bool>(false, false, "Node not found in graph.");

        // Lock to ensure atomicity of removing the node and its connections.
        lock (_modificationLock)
        {
            // Re-verify node existence within the lock in case it was removed between the initial check and acquiring the lock.
            if (!_nodesById.ContainsKey(node.Id))
                return new OperationResult<bool>(false, false, "Node not found in graph (removed concurrently).");

            // Identify connections to remove.
            var connectionsToRemove = _connectionsById.Values
                .Where(c => c.Source.ParentNode.Id == node.Id || c.Target.ParentNode.Id == node.Id)
                .ToList(); // ToList() is important to avoid modifying the collection while iterating.

            // Remove connections first.
            foreach (var connection in connectionsToRemove)
            {
                // Use TryRemoveConnection internally to ensure proper cleanup.
                var removeConnResult = TryRemoveConnection(connection); 
                if (!removeConnResult.Success)
                {
                    // Handle potential failure during connection removal if necessary (e.g., log)
                    // For now, we proceed to remove the node anyway.
                }
            }

            // Attempt to remove the node.
            if (_nodesById.TryRemove(node.Id, out _))
                return new OperationResult<bool>(true, true, string.Empty);
            else
                // Should ideally not happen if the node existed at the start of the lock.
                return new OperationResult<bool>(false, false, "Failed to remove node from graph despite initial checks.");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is a convenience wrapper for <see cref="TryRemoveNode"/>. It returns true on success and false on failure,
    /// discarding detailed error information.
    /// </remarks>
    public bool RemoveNode(INode node)
    {
        return TryRemoveNode(node).Success;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Retrieves a node by its ID using a thread-safe dictionary lookup.
    /// Returns null if no node with the specified ID is found.
    /// </remarks>
    public INode GetNodeById(Guid id)
    {
        _nodesById.TryGetValue(id, out var node);
        return node; // Returns null if not found, which is acceptable for TryGetValue pattern.
    }

    /// <summary>
    /// Attempts to add a connection between two connectors, performing validation and ensuring atomicity.
    /// Performs checks for null arguments, connector directions, type compatibility, node existence within the graph,
    /// and potential circular references before attempting to create and add the connection.
    /// Uses a lock during the final validation and modification steps to prevent race conditions.
    /// </summary>
    /// <param name="sourceConnector">The source (output) connector. Must belong to a node within this graph.</param>
    /// <param name="targetConnector">The target (input) connector. Must belong to a node within this graph.</param>
    /// <returns>An <see cref="OperationResult{IConnection}"/> containing the new connection on success, or an error message on failure.</returns>
    /// <example>
    /// <code>
    /// // Assumes graph, sourceNode, targetNode, sourceConnector, targetConnector exist
    /// graph.AddNode(sourceNode);
    /// graph.AddNode(targetNode);
    /// var result = graph.TryAddConnection(sourceConnector, targetConnector);
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"Connection {result.Result.Id} added successfully.");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Failed to add connection: {result.ErrorMessage}");
    /// }
    /// </code>
    /// </example>
    public OperationResult<IConnection> TryAddConnection(IConnector sourceConnector, IConnector targetConnector)
    {
        // --- Initial argument checks ---
        if (sourceConnector == null)
            return new OperationResult<IConnection>(false, default, "Source connector cannot be null.");
        if (targetConnector == null)
            return new OperationResult<IConnection>(false, default, "Target connector cannot be null.");

        // --- Validation outside the lock (read operations) ---
        if (sourceConnector.Direction != ConnectorDirection.Output)
            return new OperationResult<IConnection>(false, default, "Source connector must be an output.");
        if (targetConnector.Direction != ConnectorDirection.Input)
            return new OperationResult<IConnection>(false, default, "Target connector must be an input.");

        // Validate type compatibility and connector rules (e.g., input capacity)
        if (!sourceConnector.ValidateConnection(targetConnector))
            return new OperationResult<IConnection>(false, default, "Invalid connection: Type mismatch or target input connector rules violated.");

        // Verify nodes exist in the graph.
        if (!_nodesById.ContainsKey(sourceConnector.ParentNode.Id) || !_nodesById.ContainsKey(targetConnector.ParentNode.Id))
            return new OperationResult<IConnection>(false, default, "One or both parent nodes for the connectors do not exist in the graph.");

        // Check for potential circular references.
        if (WouldAddingEdgeCreateCycle(sourceConnector.ParentNode, targetConnector.ParentNode))
        {
            return new OperationResult<IConnection>(false, default, "Connection would create a circular reference.");
        }

        // --- Lock for final checks and modification ---
        lock (_modificationLock)
        {
            try
            {
                // Re-check critical conditions inside the lock to prevent race conditions.
                // Ensure nodes still exist.
                 if (!_nodesById.ContainsKey(sourceConnector.ParentNode.Id) || !_nodesById.ContainsKey(targetConnector.ParentNode.Id))
                     return new OperationResult<IConnection>(false, default, "One or both parent nodes were removed concurrently.");

                // Re-validate connector state (e.g., target input capacity).
                // Use the specific target connector's state, not the general ValidateConnection.
                if (targetConnector.Direction == ConnectorDirection.Input && targetConnector.Connections.Any(c => c.Source != sourceConnector))
                {
                    return new OperationResult<IConnection>(false, default, "Target input connector already has a different connection (concurrently added).");
                }

                // If all checks pass, create and add the connection.
                var connection = new Connection(sourceConnector, targetConnector);

                if (_connectionsById.TryAdd(connection.Id, connection))
                {
                    // Connection constructor handles adding itself to connectors.
                    return new OperationResult<IConnection>(true, connection, string.Empty);
                }
                else
                {
                    // Should not happen if ID is unique Guid, but handle defensively.
                    // Rollback changes made by Connection constructor.
                    connection.Remove(); 
                    return new OperationResult<IConnection>(false, default, "Failed to add connection to the graph's internal collection (ID conflict?).");
                }
            }
            catch (InvalidOperationException ex) // Catch potential rollback exceptions from Connection constructor
            {
                 return new OperationResult<IConnection>(false, null, $"Failed to finalize connection on connectors: {ex.Message}");
            }
            catch (Exception ex) // Catch unexpected errors during locked operation
            {
                // Log the exception ex
                return new OperationResult<IConnection>(false, default, $"An unexpected error occurred while adding the connection: {ex.Message}");
            }
        } // End lock
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is a convenience wrapper for <see cref="TryAddConnection"/>. It returns the created <see cref="IConnection"/>
    /// on success and null on failure, discarding detailed error information.
    /// </remarks>
    public IConnection AddConnection(IConnector sourceConnector, IConnector targetConnector)
    {
        var result = TryAddConnection(sourceConnector, targetConnector);
        // Returns null on failure, consistent with TryAdd pattern.
        return result.Success ? result.Result : default;
    }

    /// <summary>
    /// Attempts to remove a connection from the graph atomically.
    /// This involves removing the connection from the graph's collection and invoking the connection's <see cref="IConnection.Remove"/> method
    /// to detach it from its source and target connectors. Uses a lock for consistency.
    /// </summary>
    /// <param name="connection">The connection to remove. Must not be null.</param>
    /// <returns>An <see cref="OperationResult{Boolean}"/> indicating success or failure, with an error message on failure.</returns>
    public OperationResult<bool> TryRemoveConnection(IConnection connection)
    {
        if (connection == null)
            return new OperationResult<bool>(false, false, "Connection cannot be null.");

        if (!_connectionsById.ContainsKey(connection.Id))
            return new OperationResult<bool>(false, false, "Connection not found in graph.");

        lock (_modificationLock)
        {
            // Re-verify existence inside lock.
             if (!_connectionsById.ContainsKey(connection.Id))
                 return new OperationResult<bool>(false, false, "Connection not found in graph (removed concurrently).");

            // Remove from connectors first.
            connection.Remove(); 
            
            // Then remove from the graph's collection.
            if (_connectionsById.TryRemove(connection.Id, out _))
                return new OperationResult<bool>(true, true, string.Empty);
            else
                 // Should not happen if it existed at start of lock.
                return new OperationResult<bool>(false, false, "Failed to remove connection from graph collection.");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is a convenience wrapper for <see cref="TryRemoveConnection"/>. It returns true on success and false on failure,
    /// discarding detailed error information.
    /// </remarks>
    public bool RemoveConnection(IConnection connection)
    {
        return TryRemoveConnection(connection).Success;
    }

    /// <summary>
    /// Validates the overall structural integrity of the graph.
    /// Checks node validity, connection validity, and for circular references.
    /// </summary>
    /// <returns>An <see cref="OperationResult{Boolean}"/> indicating validity and providing details on failure.</returns>
    public OperationResult<bool> TryValidate()
    {
        // Validate all nodes.
        var invalidNodes = _nodesById.Values.Where(n => !n.Validate()).Select(n => n.Id).ToList();
        if (invalidNodes.Any())
            return new OperationResult<bool>(false, false, $"Invalid nodes found: [{string.Join(", ", invalidNodes)}]");

        // Validate all connections.
        var invalidConnections = _connectionsById.Values.Where(c => !c.Validate()).Select(c => c.Id).ToList();
        if (invalidConnections.Any())
            return new OperationResult<bool>(false, false, $"Invalid connections found: [{string.Join(", ", invalidConnections)}]");

        // Check for cycles in the graph topology.
        if (HasCycle())
             return new OperationResult<bool>(false, false, "Graph contains one or more circular references.");

        return new OperationResult<bool>(true, true, "Graph validation successful.");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Performs a comprehensive validation of the graph's current state.
    /// Checks include:
    /// <list type="bullet">
    /// <item><description>Ensuring all connections link valid nodes currently present in the graph.</description></item>
    /// <item><description>Ensuring connections link connectors that still belong to their parent nodes.</description></item>
    /// <item><description>Invoking the <see cref="IConnection.Validate"/> method for each connection.</description></item>
    /// <item><description>Invoking the <see cref="INode.Validate"/> method for each node.</description></item>
    /// </list>
    /// Returns true only if all checks pass for all nodes and connections.
    /// </remarks>
    public bool Validate()
    {
        return TryValidate().Success;
    }

    // --- Private Helper Methods for Cycle Detection ---

    /// <summary>
    /// Checks if adding a hypothetical edge from a source node to a target node would introduce a cycle.
    /// Performs a traversal starting from the target node, looking for a path back to the source node.
    /// This is called *before* the edge (connection) is actually added.
    /// </summary>
    private bool WouldAddingEdgeCreateCycle(INode sourceNode, INode targetNode)
    {
        if (sourceNode == null || targetNode == null) return false; 
        if (sourceNode == targetNode) return true; // Self-loop

        // Traversal: Check if we can reach sourceNode by traversing backwards from targetNode's inputs
        // Or, more conventionally for DFS: Traverse forwards from targetNode and see if sourceNode is reachable.
        // Let's stick to the forward traversal from target node.

        var stack = new Stack<INode>();
        var visitedInThisPath = new HashSet<INode>(); // Nodes visited in the current DFS path starting from targetNode

        stack.Push(targetNode);

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();

            // If we reach the source node, adding an edge from source to target would complete a cycle.
            if (currentNode == sourceNode) 
            {
                return true;
            }

            // Avoid re-visiting nodes within the same path traversal to prevent infinite loops in existing cycles.
            if (!visitedInThisPath.Add(currentNode))
            {
                continue; 
            }

            // Explore neighbors (nodes reachable from currentNode's output connectors)
            foreach (var outputConnector in currentNode.OutputConnectors)
            {
                foreach (var connection in outputConnector.Connections)
                {
                    var neighbor = connection.Target.ParentNode;
                    // Check if neighbor exists (could be null if graph is in inconsistent state, though unlikely)
                    if (neighbor != null && !visitedInThisPath.Contains(neighbor)) // Push only if not already visited in *this* specific path check
                    {
                         stack.Push(neighbor);
                    }
                    // If neighbor has been visited in this path, we don't need to push it again.
                }
            }
        }

        return false; // No path found from targetNode back to sourceNode
    }


     /// <summary>
     /// Checks if the *entire current graph* contains any cycles using Depth-First Search.
     /// This is used for general graph validation.
     /// </summary>
     /// <returns>True if a cycle exists anywhere in the graph, false otherwise.</returns>
     private bool HasCycle()
     {         
         var visited = new HashSet<INode>();      // Nodes visited in any DFS path so far.
         var recursionStack = new HashSet<INode>(); // Nodes currently in the active recursion stack for *one* specific DFS traversal.

         foreach (var node in _nodesById.Values)
         {
             // Start DFS only if the node hasn't been visited as part of a previous DFS traversal.
             if (!visited.Contains(node))
             {
                 if (HasCycleRecursive(node, visited, recursionStack))
                 {
                     return true; // Cycle detected
                 }
             }
         }
         return false; // No cycles found in any component of the graph.
     }

     /// <summary>
     /// Recursive helper for the DFS-based cycle detection.
     /// </summary>
     /// <param name="node">The current node being visited.</param>
     /// <param name="visited">Set of all nodes visited across all DFS paths started so far.</param>
     /// <param name="recursionStack">Set of nodes currently in the stack for the *current* DFS path.</param>
     /// <returns>True if a cycle is detected reachable from this node, false otherwise.</returns>
     private bool HasCycleRecursive(INode node, HashSet<INode> visited, HashSet<INode> recursionStack)
     {
         visited.Add(node);
         recursionStack.Add(node);

         foreach (var connector in node.OutputConnectors)
         {
             foreach (var connection in connector.Connections)
             {
                 var neighbor = connection.Target.ParentNode;
                 if (neighbor == null) continue; // Skip if connection target is somehow invalid

                 if (!visited.Contains(neighbor)) // If neighbor hasn't been visited at all yet
                 {
                     if (HasCycleRecursive(neighbor, visited, recursionStack))
                         return true;
                 }
                 // If neighbor *has* been visited AND is currently in our recursion stack, we found a back-edge -> cycle.
                 else if (recursionStack.Contains(neighbor)) 
                 {
                     return true;
                 }
                 // If neighbor was visited but not in recursionStack, it's part of a different branch we already explored from another starting point - safe.
             }
         }

         recursionStack.Remove(node); // Remove node from recursion stack as we backtrack up the call chain.
         // Note: We don't need a separate 'explored' set here. If HasCycleRecursive returns false for a node,
         // it means all reachable nodes from it have been explored without finding cycles originating *from that path*.
         // The global 'visited' set prevents re-processing nodes unnecessarily.
         return false;
     }
}