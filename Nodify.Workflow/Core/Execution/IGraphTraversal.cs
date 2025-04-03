using System;
using System.Collections.Generic;
using Nodify.Workflow.Core.Interfaces;

namespace Nodify.Workflow.Core.Execution;

/// <summary>
/// Defines operations for traversing a node graph.
/// </summary>
public interface IGraphTraversal
{
    // Existing methods...

    /// <summary>
    /// Performs a depth-first traversal starting from the specified node.
    /// </summary>
    /// <param name="startNode">The node to start traversal from.</param>
    /// <param name="visitor">A function called for each visited node. Return false to stop traversal.</param>
    void DepthFirstTraversal(INode startNode, Func<INode, bool> visitor);

    /// <summary>
    /// Performs a breadth-first traversal starting from the specified node.
    /// </summary>
    /// <param name="startNode">The node to start traversal from.</param>
    /// <param name="visitor">A function called for each visited node. Return false to stop traversal.</param>
    void BreadthFirstTraversal(INode startNode, Func<INode, bool> visitor);

    /// <summary>
    /// Finds a node within the graph reachable from the start node by its unique identifier.
    /// </summary>
    /// <param name="startNode">The node to start searching from.</param>
    /// <param name="id">The unique identifier of the node to find.</param>
    /// <returns>The found node, or null if not found.</returns>
    INode FindNodeById(INode startNode, Guid id);

    /// <summary>
    /// Finds the shortest path (in terms of number of connections) between two nodes.
    /// </summary>
    /// <param name="startNode">The starting node.</param>
    /// <param name="endNode">The target node.</param>
    /// <returns>A read-only list of nodes representing the shortest path, or an empty list if no path exists.</returns>
    IReadOnlyList<INode> FindShortestPath(INode startNode, INode endNode);

    /// <summary>
    /// Gets all entry point nodes (nodes with no incoming connections) reachable from the start node.
    /// </summary>
    /// <param name="startNode">The node to start searching from.</param>
    /// <returns>A read-only list of entry point nodes.</returns>
    IReadOnlyList<INode> GetEntryPoints(INode startNode);

    /// <summary>
    /// Gets all exit point nodes (nodes with no outgoing connections) reachable from the start node.
    /// </summary>
    /// <param name="startNode">The node to start searching from.</param>
    /// <returns>A read-only list of exit point nodes.</returns>
    IReadOnlyList<INode> GetExitPoints(INode startNode);

    /// <summary>
    /// Performs a topological sort of the graph reachable from the start node.
    /// </summary>
    /// <param name="startNode">The node to start sorting from.</param>
    /// <returns>A read-only list of nodes in topological order.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a cycle is detected.</exception>
    IReadOnlyList<INode> TopologicalSort(INode startNode);

    /// <summary>
    /// Finds all simple paths (paths without repeated nodes) between two nodes.
    /// </summary>
    /// <param name="startNode">The starting node.</param>
    /// <param name="endNode">The target node.</param>
    /// <returns>An enumerable collection of lists, where each list represents a simple path.</returns>
    IEnumerable<IReadOnlyList<INode>> FindAllSimplePaths(INode startNode, INode endNode);
} 