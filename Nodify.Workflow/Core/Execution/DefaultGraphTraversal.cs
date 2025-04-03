using Nodify.Workflow.Core.Interfaces;
using System.Collections.Generic;

namespace Nodify.Workflow.Core.Execution;

/// <summary>
/// Default implementation of graph traversal operations for workflow execution
/// </summary>
public class DefaultGraphTraversal : IGraphTraversal
{
    /// <inheritdoc />
    public void DepthFirstTraversal(INode startNode, Func<INode, bool> visitor)
    {
        ArgumentNullException.ThrowIfNull(startNode);
        ArgumentNullException.ThrowIfNull(visitor);

        var visited = new HashSet<INode>();
        DepthFirstTraversalInternal(startNode, visitor, visited);
    }

    /// <inheritdoc />
    public void BreadthFirstTraversal(INode startNode, Func<INode, bool> visitor)
    {
        ArgumentNullException.ThrowIfNull(startNode);
        ArgumentNullException.ThrowIfNull(visitor);

        var visited = new HashSet<INode>();
        var queue = new Queue<INode>();

        visited.Add(startNode);
        if (!visitor(startNode)) return;
        queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var connectedNodes = GetConnectedNodes(node);

            foreach (var nextNode in connectedNodes)
            {
                if (visited.Add(nextNode))
                {
                    if (!visitor(nextNode)) return;
                    queue.Enqueue(nextNode);
                }
            }
        }
    }

    /// <inheritdoc />
    public INode FindNodeById(INode startNode, Guid id)
    {
        ArgumentNullException.ThrowIfNull(startNode);

        if (startNode.Id == id) return startNode;

        var visited = new HashSet<INode>();
        var queue = new Queue<INode>();

        visited.Add(startNode);
        queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var connectedNodes = GetConnectedNodes(node);

            foreach (var nextNode in connectedNodes)
            {
                if (nextNode.Id == id) return nextNode;
                if (visited.Add(nextNode))
                {
                    queue.Enqueue(nextNode);
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<INode> FindShortestPath(INode startNode, INode endNode)
    {
        ArgumentNullException.ThrowIfNull(startNode);
        ArgumentNullException.ThrowIfNull(endNode);

        if (startNode == endNode)
            return new List<INode> { startNode };

        var visited = new HashSet<INode>();
        var queue = new Queue<List<INode>>();
        var initialPath = new List<INode> { startNode };

        visited.Add(startNode);
        queue.Enqueue(initialPath);

        while (queue.Count > 0)
        {
            var path = queue.Dequeue();
            var currentNode = path[^1]; // Use index from end operator

            foreach (var connector in currentNode.OutputConnectors)
            {
                foreach (var connection in connector.Connections)
                {
                    var nextNode = connection.Target.ParentNode;
                    if (nextNode == endNode)
                    {
                        path.Add(nextNode);
                        return path;
                    }

                    if (visited.Add(nextNode))
                    {
                        var newPath = new List<INode>(path) { nextNode };
                        queue.Enqueue(newPath);
                    }
                }
            }
        }

        return Array.Empty<INode>();
    }

    /// <inheritdoc />
    public IReadOnlyList<INode> GetEntryPoints(INode startNode)
    {
        ArgumentNullException.ThrowIfNull(startNode);

        var entryPoints = new List<INode>();
        var visited = new HashSet<INode>();

        void Visit(INode node)
        {
            if (!visited.Add(node)) return;

            if (!node.InputConnectors.Any(c => c.Connections.Any()))
            {
                entryPoints.Add(node);
            }

            foreach (var nextNode in GetConnectedNodes(node))
            {
                Visit(nextNode);
            }
        }

        Visit(startNode);
        return entryPoints;
    }

    /// <inheritdoc />
    public IReadOnlyList<INode> GetExitPoints(INode startNode)
    {
        ArgumentNullException.ThrowIfNull(startNode);

        var exitPoints = new List<INode>();
        var visited = new HashSet<INode>();

        void Visit(INode node)
        {
            if (!visited.Add(node)) return;

            if (!node.OutputConnectors.Any(c => c.Connections.Any()))
            {
                exitPoints.Add(node);
            }

            foreach (var nextNode in GetConnectedNodes(node))
            {
                Visit(nextNode);
            }
        }

        Visit(startNode);
        return exitPoints;
    }

    /// <inheritdoc />
    public IReadOnlyList<INode> TopologicalSort(INode startNode)
    {
        ArgumentNullException.ThrowIfNull(startNode);

        var visited = new HashSet<INode>();
        var visiting = new HashSet<INode>(); // For cycle detection
        var sorted = new List<INode>();

        void Visit(INode node)
        {
            if (visited.Contains(node)) return;
            if (!visiting.Add(node))
            {
                throw new InvalidOperationException("Cycle detected in graph during topological sort");
            }

            foreach (var connector in node.OutputConnectors)
            {
                foreach (var connection in connector.Connections)
                {
                    Visit(connection.Target.ParentNode);
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            sorted.Add(node);
        }

        Visit(startNode);
        sorted.Reverse(); // Reverse to get correct execution order
        return sorted;
    }

    private void DepthFirstTraversalInternal(INode node, Func<INode, bool> visitor, HashSet<INode> visited)
    {
        if (!visited.Add(node)) return;
        if (!visitor(node)) return;

        foreach (var nextNode in GetConnectedNodes(node))
        {
            DepthFirstTraversalInternal(nextNode, visitor, visited);
        }
    }

    private static HashSet<INode> GetConnectedNodes(INode node)
    {
        var connectedNodes = new HashSet<INode>();

        // Add nodes from output connections
        foreach (var connector in node.OutputConnectors)
        {
            foreach (var connection in connector.Connections)
            {
                connectedNodes.Add(connection.Target.ParentNode);
            }
        }

        // Add nodes from input connections
        foreach (var connector in node.InputConnectors)
        {
            foreach (var connection in connector.Connections)
            {
                connectedNodes.Add(connection.Source.ParentNode);
            }
        }

        return connectedNodes;
    }
} 