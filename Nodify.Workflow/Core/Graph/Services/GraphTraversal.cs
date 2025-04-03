using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Graph.Interfaces;

namespace Nodify.Workflow.Core.Graph.Services
{
    /// <summary>
    /// Provides graph traversal and search operations for workflow graphs
    /// </summary>
    public class GraphTraversal
    {
        /// <summary>
        /// Performs a depth-first traversal of the graph starting from the given node
        /// </summary>
        /// <param name="startNode">The node to start traversal from</param>
        /// <param name="visitor">Callback function for each visited node, return false to stop traversal</param>
        public void DepthFirstTraversal(INode startNode, Func<INode, bool> visitor)
        {
            if (startNode == null) throw new ArgumentNullException(nameof(startNode));
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            var visited = new HashSet<INode>();
            DepthFirstTraversalInternal(startNode, visitor, visited);
        }

        /// <summary>
        /// Performs a breadth-first traversal of the graph starting from the given node
        /// </summary>
        /// <param name="startNode">The node to start traversal from</param>
        /// <param name="visitor">Callback function for each visited node, return false to stop traversal</param>
        public void BreadthFirstTraversal(INode startNode, Func<INode, bool> visitor)
        {
            if (startNode == null) throw new ArgumentNullException(nameof(startNode));
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            var visited = new HashSet<INode>();
            var queue = new Queue<INode>();
            
            visited.Add(startNode);
            if (!visitor(startNode)) return;
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                // Get all connected nodes (both input and output)
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

        /// <summary>
        /// Finds a node by its ID in the graph
        /// </summary>
        /// <param name="startNode">The node to start searching from</param>
        /// <param name="id">The ID to search for</param>
        /// <returns>The node with the matching ID, or null if not found</returns>
        public INode FindNodeById(INode startNode, Guid id)
        {
            if (startNode == null) throw new ArgumentNullException(nameof(startNode));

            if (startNode.Id == id) return startNode;

            var visited = new HashSet<INode>();
            var queue = new Queue<INode>();
            
            visited.Add(startNode);
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                // Check both input and output connections
                var connectedNodes = new HashSet<INode>();
                
                foreach (var connector in node.OutputConnectors)
                {
                    foreach (var connection in connector.Connections)
                    {
                        var nextNode = connection.Target.ParentNode;
                        if (nextNode.Id == id) return nextNode;
                        connectedNodes.Add(nextNode);
                    }
                }

                foreach (var connector in node.InputConnectors)
                {
                    foreach (var connection in connector.Connections)
                    {
                        var nextNode = connection.Source.ParentNode;
                        if (nextNode.Id == id) return nextNode;
                        connectedNodes.Add(nextNode);
                    }
                }

                foreach (var nextNode in connectedNodes)
                {
                    if (visited.Add(nextNode))
                    {
                        queue.Enqueue(nextNode);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the shortest path between two nodes in the graph
        /// </summary>
        /// <param name="startNode">The starting node</param>
        /// <param name="endNode">The target node</param>
        /// <returns>List of nodes representing the shortest path, or empty list if no path exists</returns>
        public List<INode> FindShortestPath(INode startNode, INode endNode)
        {
            if (startNode == null) throw new ArgumentNullException(nameof(startNode));
            if (endNode == null) throw new ArgumentNullException(nameof(endNode));

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
                var currentNode = path[path.Count - 1];

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

            return new List<INode>();
        }

        /// <summary>
        /// Gets all entry points (nodes with no input connections) in the graph
        /// </summary>
        /// <param name="startNode">The node to start searching from</param>
        /// <returns>List of entry point nodes</returns>
        public List<INode> GetEntryPoints(INode startNode)
        {
            if (startNode == null) throw new ArgumentNullException(nameof(startNode));

            var entryPoints = new List<INode>();
            var visited = new HashSet<INode>();

            void Visit(INode node)
            {
                if (!visited.Add(node)) return;

                if (!node.InputConnectors.Any(c => c.Connections.Any()))
                {
                    entryPoints.Add(node);
                }

                // Check both input and output connections
                foreach (var connector in node.OutputConnectors)
                {
                    foreach (var connection in connector.Connections)
                    {
                        Visit(connection.Target.ParentNode);
                    }
                }

                foreach (var connector in node.InputConnectors)
                {
                    foreach (var connection in connector.Connections)
                    {
                        Visit(connection.Source.ParentNode);
                    }
                }
            }

            Visit(startNode);
            return entryPoints;
        }

        /// <summary>
        /// Gets all exit points (nodes with no output connections) in the graph
        /// </summary>
        /// <param name="startNode">The node to start searching from</param>
        /// <returns>List of exit point nodes</returns>
        public List<INode> GetExitPoints(INode startNode)
        {
            if (startNode == null) throw new ArgumentNullException(nameof(startNode));

            var exitPoints = new List<INode>();
            var visited = new HashSet<INode>();

            void Visit(INode node)
            {
                if (!visited.Add(node)) return;

                if (!node.OutputConnectors.Any(c => c.Connections.Any()))
                {
                    exitPoints.Add(node);
                }

                // Check both input and output connections
                foreach (var connector in node.OutputConnectors)
                {
                    foreach (var connection in connector.Connections)
                    {
                        Visit(connection.Target.ParentNode);
                    }
                }

                foreach (var connector in node.InputConnectors)
                {
                    foreach (var connection in connector.Connections)
                    {
                        Visit(connection.Source.ParentNode);
                    }
                }
            }

            Visit(startNode);
            return exitPoints;
        }

        /// <summary>
        /// Performs a topological sort of the graph to determine execution order
        /// </summary>
        /// <param name="startNode">The node to start from</param>
        /// <returns>List of nodes in topological order</returns>
        public List<INode> TopologicalSort(INode startNode)
        {
            if (startNode == null) throw new ArgumentNullException(nameof(startNode));

            var visited = new HashSet<INode>();
            var sorted = new List<INode>();

            void Visit(INode node)
            {
                if (visited.Contains(node)) return;
                visited.Add(node);

                // Check both input and output connections
                foreach (var connector in node.OutputConnectors)
                {
                    foreach (var connection in connector.Connections)
                    {
                        Visit(connection.Target.ParentNode);
                    }
                }

                foreach (var connector in node.InputConnectors)
                {
                    foreach (var connection in connector.Connections)
                    {
                        Visit(connection.Source.ParentNode);
                    }
                }

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

            // Check both input and output connections
            foreach (var connector in node.OutputConnectors)
            {
                foreach (var connection in connector.Connections)
                {
                    DepthFirstTraversalInternal(connection.Target.ParentNode, visitor, visited);
                }
            }

            foreach (var connector in node.InputConnectors)
            {
                foreach (var connection in connector.Connections)
                {
                    DepthFirstTraversalInternal(connection.Source.ParentNode, visitor, visited);
                }
            }
        }
    }
} 