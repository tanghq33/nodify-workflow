using System;
using System.Collections.Generic;

namespace Nodify.Workflow.Core.Interfaces;

/// <summary>
/// Defines operations for traversing and analyzing workflow graphs
/// </summary>
public interface IGraphTraversal
{
    /// <summary>
    /// Performs a depth-first traversal of the graph starting from the given node
    /// </summary>
    /// <param name="startNode">The node to start traversal from</param>
    /// <param name="visitor">Callback function for each visited node, return false to stop traversal</param>
    void DepthFirstTraversal(INode startNode, Func<INode, bool> visitor);

    /// <summary>
    /// Performs a breadth-first traversal of the graph starting from the given node
    /// </summary>
    /// <param name="startNode">The node to start traversal from</param>
    /// <param name="visitor">Callback function for each visited node, return false to stop traversal</param>
    void BreadthFirstTraversal(INode startNode, Func<INode, bool> visitor);

    /// <summary>
    /// Finds a node by its ID in the graph
    /// </summary>
    /// <param name="startNode">The node to start searching from</param>
    /// <param name="id">The ID to search for</param>
    /// <returns>The node with the matching ID, or null if not found</returns>
    INode FindNodeById(INode startNode, Guid id);

    /// <summary>
    /// Finds the shortest path between two nodes in the graph
    /// </summary>
    /// <param name="startNode">The starting node</param>
    /// <param name="endNode">The target node</param>
    /// <returns>List of nodes representing the shortest path, or empty list if no path exists</returns>
    IReadOnlyList<INode> FindShortestPath(INode startNode, INode endNode);

    /// <summary>
    /// Gets all entry points (nodes with no input connections) in the graph
    /// </summary>
    /// <param name="startNode">The node to start searching from</param>
    /// <returns>List of entry point nodes</returns>
    IReadOnlyList<INode> GetEntryPoints(INode startNode);

    /// <summary>
    /// Gets all exit points (nodes with no output connections) in the graph
    /// </summary>
    /// <param name="startNode">The node to start searching from</param>
    /// <returns>List of exit point nodes</returns>
    IReadOnlyList<INode> GetExitPoints(INode startNode);

    /// <summary>
    /// Performs a topological sort of the graph to determine execution order.
    /// This is essential for determining the correct order of node execution in the workflow.
    /// </summary>
    /// <param name="startNode">The node to start from</param>
    /// <returns>List of nodes in topological order</returns>
    /// <exception cref="InvalidOperationException">Thrown when the graph contains cycles</exception>
    IReadOnlyList<INode> TopologicalSort(INode startNode);
} 