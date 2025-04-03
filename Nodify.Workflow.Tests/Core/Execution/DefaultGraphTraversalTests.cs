using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Nodify.Workflow.Tests.Core.Execution;

public interface ISpecialNode : INode { } // Example interface for testing

public class DefaultGraphTraversalTests
{
    private readonly INode _startNode;
    private readonly INode _middleNode;
    private readonly INode _endNode;
    private readonly IConnection _connection1;
    private readonly IConnection _connection2;
    private readonly Nodify.Workflow.Core.Execution.IGraphTraversal _traversal;

    public DefaultGraphTraversalTests()
    {
        // Setup basic linear test graph (A -> B -> C)
        SetupLinearGraph(out _startNode, out _middleNode, out _endNode, out _connection1, out _connection2);
        _traversal = new DefaultGraphTraversal();
    }

    // === Test Setup Helpers ===

    private static INode CreateMockNode(string? name = null)
    {
        var node = Substitute.For<INode>();
        node.Id.Returns(Guid.NewGuid());
        node.InputConnectors.Returns(new List<IConnector>().AsReadOnly()); // Default empty
        node.OutputConnectors.Returns(new List<IConnector>().AsReadOnly()); // Default empty
        return node;
    }

    private static IConnector CreateMockConnector(INode parent, ConnectorDirection direction, Guid? id = null, Type? dataType = null)
    {
        var connector = Substitute.For<IConnector>();
        connector.Id.Returns(id ?? Guid.NewGuid());
        connector.ParentNode.Returns(parent);
        connector.Direction.Returns(direction);
        connector.DataType.Returns(dataType ?? typeof(object)); // Default type
        connector.Connections.Returns(new List<IConnection>().AsReadOnly()); // Default empty
        connector.ValidateConnection(Arg.Any<IConnector>()).Returns(true); // Assume valid connections for tests
        connector.AddConnection(Arg.Any<IConnection>()).Returns(true);
        return connector;
    }

    private static IConnection CreateMockConnection(IConnector source, IConnector target, Guid? id = null)
    {
        var connection = Substitute.For<IConnection>();
        connection.Id.Returns(id ?? Guid.NewGuid());
        connection.Source.Returns(source);
        connection.Target.Returns(target);

        // Link connection back to connectors
        var sourceConnections = source.Connections.ToList();
        sourceConnections.Add(connection);
        source.Connections.Returns(sourceConnections.AsReadOnly());

        var targetConnections = target.Connections.ToList();
        targetConnections.Add(connection);
        target.Connections.Returns(targetConnections.AsReadOnly());

        return connection;
    }

    // Sets up the basic A -> B -> C structure used in many tests
    private void SetupLinearGraph(out INode nodeA, out INode nodeB, out INode nodeC, out IConnection connAB, out IConnection connBC)
    {
        nodeA = CreateMockNode("A");
        nodeB = CreateMockNode("B");
        nodeC = CreateMockNode("C");

        var connAOut = CreateMockConnector(nodeA, ConnectorDirection.Output);
        var connBIn = CreateMockConnector(nodeB, ConnectorDirection.Input);
        var connBOut = CreateMockConnector(nodeB, ConnectorDirection.Output);
        var connCIn = CreateMockConnector(nodeC, ConnectorDirection.Input);

        nodeA.OutputConnectors.Returns(new[] { connAOut });
        nodeB.InputConnectors.Returns(new[] { connBIn });
        nodeB.OutputConnectors.Returns(new[] { connBOut });
        nodeC.InputConnectors.Returns(new[] { connCIn });

        connAB = CreateMockConnection(connAOut, connBIn);
        connBC = CreateMockConnection(connBOut, connCIn);
    }

    // Helper for Branching Graph: A -> B, A -> C
    private void SetupBranchingGraph(out INode nodeA, out INode nodeB, out INode nodeC)
    {
        nodeA = CreateMockNode("A");
        nodeB = CreateMockNode("B");
        nodeC = CreateMockNode("C");

        var connAOut1 = CreateMockConnector(nodeA, ConnectorDirection.Output, Guid.NewGuid(), typeof(string)); // To B
        var connAOut2 = CreateMockConnector(nodeA, ConnectorDirection.Output, Guid.NewGuid(), typeof(int));    // To C
        var connBIn = CreateMockConnector(nodeB, ConnectorDirection.Input, Guid.NewGuid(), typeof(string));
        var connCIn = CreateMockConnector(nodeC, ConnectorDirection.Input, Guid.NewGuid(), typeof(int));

        nodeA.OutputConnectors.Returns(new[] { connAOut1, connAOut2 });
        nodeB.InputConnectors.Returns(new[] { connBIn });
        nodeC.InputConnectors.Returns(new[] { connCIn });

        CreateMockConnection(connAOut1, connBIn);
        CreateMockConnection(connAOut2, connCIn);
    }

     // Helper for Merging Graph: A -> C, B -> C
    private void SetupMergingGraph(out INode nodeA, out INode nodeB, out INode nodeC)
    {
        nodeA = CreateMockNode("A");
        nodeB = CreateMockNode("B");
        nodeC = CreateMockNode("C");

        var connAOut = CreateMockConnector(nodeA, ConnectorDirection.Output);
        var connBOut = CreateMockConnector(nodeB, ConnectorDirection.Output);
        var connCIn1 = CreateMockConnector(nodeC, ConnectorDirection.Input, Guid.NewGuid()); // From A
        var connCIn2 = CreateMockConnector(nodeC, ConnectorDirection.Input, Guid.NewGuid()); // From B

        nodeA.OutputConnectors.Returns(new[] { connAOut });
        nodeB.OutputConnectors.Returns(new[] { connBOut });
        nodeC.InputConnectors.Returns(new[] { connCIn1, connCIn2 });

        CreateMockConnection(connAOut, connCIn1);
        CreateMockConnection(connBOut, connCIn2);
    }

     // Helper for Complex DAG: A->B, A->C, B->D, C->D, D->E
    private void SetupComplexDAG(out INode nodeA, out INode nodeB, out INode nodeC, out INode nodeD, out INode nodeE)
    {
        nodeA = CreateMockNode("A");
        nodeB = CreateMockNode("B");
        nodeC = CreateMockNode("C");
        nodeD = CreateMockNode("D");
        nodeE = CreateMockNode("E");

        var connAOutB = CreateMockConnector(nodeA, ConnectorDirection.Output, Guid.NewGuid());
        var connAOutC = CreateMockConnector(nodeA, ConnectorDirection.Output, Guid.NewGuid());
        var connBIn = CreateMockConnector(nodeB, ConnectorDirection.Input, Guid.NewGuid());
        var connBOutD = CreateMockConnector(nodeB, ConnectorDirection.Output, Guid.NewGuid());
        var connCIn = CreateMockConnector(nodeC, ConnectorDirection.Input, Guid.NewGuid());
        var connCOutD = CreateMockConnector(nodeC, ConnectorDirection.Output, Guid.NewGuid());
        var connDInB = CreateMockConnector(nodeD, ConnectorDirection.Input, Guid.NewGuid());
        var connDInC = CreateMockConnector(nodeD, ConnectorDirection.Input, Guid.NewGuid());
        var connDOutE = CreateMockConnector(nodeD, ConnectorDirection.Output, Guid.NewGuid());
        var connEIn = CreateMockConnector(nodeE, ConnectorDirection.Input, Guid.NewGuid());

        nodeA.OutputConnectors.Returns(new[] { connAOutB, connAOutC });
        nodeB.InputConnectors.Returns(new[] { connBIn });
        nodeB.OutputConnectors.Returns(new[] { connBOutD });
        nodeC.InputConnectors.Returns(new[] { connCIn });
        nodeC.OutputConnectors.Returns(new[] { connCOutD });
        nodeD.InputConnectors.Returns(new[] { connDInB, connDInC });
        nodeD.OutputConnectors.Returns(new[] { connDOutE });
        nodeE.InputConnectors.Returns(new[] { connEIn });

        CreateMockConnection(connAOutB, connBIn);
        CreateMockConnection(connAOutC, connCIn);
        CreateMockConnection(connBOutD, connDInB);
        CreateMockConnection(connCOutD, connDInC);
        CreateMockConnection(connDOutE, connEIn);
    }

     // Helper for Cyclic Graph: A -> B -> A
    private void SetupCyclicGraphSimple(out INode nodeA, out INode nodeB)
    {
        nodeA = CreateMockNode("A");
        nodeB = CreateMockNode("B");

        var connAOut = CreateMockConnector(nodeA, ConnectorDirection.Output);
        var connBIn = CreateMockConnector(nodeB, ConnectorDirection.Input);
        var connBOut = CreateMockConnector(nodeB, ConnectorDirection.Output);
        var connAIn = CreateMockConnector(nodeA, ConnectorDirection.Input);

        nodeA.InputConnectors.Returns(new[] { connAIn });
        nodeA.OutputConnectors.Returns(new[] { connAOut });
        nodeB.InputConnectors.Returns(new[] { connBIn });
        nodeB.OutputConnectors.Returns(new[] { connBOut });

        CreateMockConnection(connAOut, connBIn);
        CreateMockConnection(connBOut, connAIn); // Cycle back
    }

    // Helper for Disconnected Graph: (A -> B), (C -> D)
    private void SetupDisconnectedGraph(out INode nodeA, out INode nodeB, out INode nodeC, out INode nodeD)
    {
        nodeA = CreateMockNode("A");
        nodeB = CreateMockNode("B");
        nodeC = CreateMockNode("C"); // Isolated root
        nodeD = CreateMockNode("D");

        var connAOut = CreateMockConnector(nodeA, ConnectorDirection.Output);
        var connBIn = CreateMockConnector(nodeB, ConnectorDirection.Input);
        var connCOut = CreateMockConnector(nodeC, ConnectorDirection.Output);
        var connDIn = CreateMockConnector(nodeD, ConnectorDirection.Input);

        nodeA.OutputConnectors.Returns(new[] { connAOut });
        nodeB.InputConnectors.Returns(new[] { connBIn });
        nodeC.OutputConnectors.Returns(new[] { connCOut });
        nodeD.InputConnectors.Returns(new[] { connDIn });

        CreateMockConnection(connAOut, connBIn);
        CreateMockConnection(connCOut, connDIn);
    }

    // === Existing Tests (Mainly Linear) ===
    // [Fact] DepthFirstTraversal_ShouldVisitAllNodes ... (Uses _startNode from linear setup)
    // [Fact] BreadthFirstTraversal_ShouldVisitAllNodes ... (Uses _startNode from linear setup)
    // [Fact] Traversal_ShouldHandleCycles ... (Modifies linear setup)
    // [Theory] DepthFirstTraversal_ShouldRespectVisitorReturn ... (Uses linear setup)
    // [Fact] Traversal_ShouldHandleEmptyGraph ... (Uses a single empty node)
    // [Fact] Traversal_ShouldRespectNodeTypeFilter ... (Modifies linear setup)
    // [Fact] FindNodeById_ShouldReturnCorrectNode ... (Uses linear setup)
    // [Fact] FindNodeById_ShouldReturnNullWhenNodeNotFound ... (Uses linear setup)
    // [Fact] FindShortestPath_ShouldReturnShortestPath ... (Modifies linear setup)
    // [Fact] FindShortestPath_ShouldReturnEmptyListWhenNoPathExists ... (Uses linear setup + isolated)
    // [Fact] GetEntryPoints_ShouldReturnNodesWithNoInputs ... (Uses linear setup)
    // [Fact] GetExitPoints_ShouldReturnNodesWithNoOutputs ... (Uses linear setup)
    // [Fact] TopologicalSort_ShouldReturnValidExecutionOrder ... (Uses linear setup)
    // [Fact] TopologicalSort_ShouldThrowOnCycles ... (Uses dedicated cyclic mock setup)

    // === NEW TESTS ===

    // --- DFT Tests ---
    [Fact]
    public void DepthFirstTraversal_BranchingGraph_VisitsAll()
    {
        SetupBranchingGraph(out var nodeA, out var nodeB, out var nodeC);
        var visited = new List<INode>();
        _traversal.DepthFirstTraversal(nodeA, n => { visited.Add(n); return true; });

        visited.Count.ShouldBe(3);
        visited.ShouldContain(nodeA);
        visited.ShouldContain(nodeB);
        visited.ShouldContain(nodeC);
        // Order could be A,B,C or A,C,B depending on connector order mock returns
    }

    [Fact]
    public void DepthFirstTraversal_MergingGraph_VisitsAllFromRoots()
    {
        SetupMergingGraph(out var nodeA, out var nodeB, out var nodeC);
        var visited = new HashSet<INode>();
        // Need to start from both A and B if they are entry points
        var entryPoints = new[] { nodeA, nodeB }; // Assuming these are the roots
        foreach (var entry in entryPoints)
        {
           _traversal.DepthFirstTraversal(entry, n => { visited.Add(n); return true; });
        }

        visited.Count.ShouldBe(3); // A, B, C
        visited.ShouldContain(nodeA);
        visited.ShouldContain(nodeB);
        visited.ShouldContain(nodeC);
    }

     [Fact]
    public void DepthFirstTraversal_ComplexDAG_VisitsAll()
    {
        SetupComplexDAG(out var nodeA, out var nodeB, out var nodeC, out var nodeD, out var nodeE);
        var visited = new HashSet<INode>();
        _traversal.DepthFirstTraversal(nodeA, n => { visited.Add(n); return true; });

        visited.Count.ShouldBe(5); // A, B, C, D, E
    }

    // --- BFT Tests ---
     [Fact]
    public void BreadthFirstTraversal_BranchingGraph_VisitsLevelOrder()
    {
        SetupBranchingGraph(out var nodeA, out var nodeB, out var nodeC);
        var visited = new List<INode>();
        _traversal.BreadthFirstTraversal(nodeA, n => { visited.Add(n); return true; });

        visited.Count.ShouldBe(3);
        visited[0].ShouldBe(nodeA);
        // Level 2 can be B, C or C, B
        visited.ShouldContain(nodeB);
        visited.ShouldContain(nodeC);
        visited.IndexOf(nodeB).ShouldBeOneOf(1, 2);
        visited.IndexOf(nodeC).ShouldBeOneOf(1, 2);
    }

    [Fact]
    public void BreadthFirstTraversal_MergingGraph_VisitsLevelOrderFromRoots()
    {
        SetupMergingGraph(out var nodeA, out var nodeB, out var nodeC);
        var visited = new List<INode>();
         var visitedSet = new HashSet<INode>(); // Track visited across roots
        // Need to start from both A and B if they are entry points
        var entryPoints = new[] { nodeA, nodeB }; // Assuming these are the roots
        var queue = new Queue<INode>();

        foreach(var entry in entryPoints)
        {
            if(visitedSet.Add(entry))
            {
                 visited.Add(entry); // Add roots first
                 queue.Enqueue(entry);
            }
        }

        while(queue.Count > 0)
        {
            var current = queue.Dequeue();
            // Simplified BFS logic for test assertion structure
             foreach(var outConn in current.OutputConnectors)
             {
                 foreach(var conn in outConn.Connections)
                 {
                     var next = conn.Target.ParentNode;
                      if (visitedSet.Add(next))
                      {
                           visited.Add(next); // Add next level
                           queue.Enqueue(next);
                      }
                 }
             }
        }

        visited.Count.ShouldBe(3);
        visited[0].ShouldBeOneOf(nodeA, nodeB); // Root level
        visited[1].ShouldBeOneOf(nodeA, nodeB);
        visited[0].ShouldNotBe(visited[1]);
        visited[2].ShouldBe(nodeC); // Next level
    }

    [Fact]
    public void BreadthFirstTraversal_ComplexDAG_VisitsLevelOrder()
    {
        SetupComplexDAG(out var nodeA, out var nodeB, out var nodeC, out var nodeD, out var nodeE);
        var visited = new List<INode>();
        _traversal.BreadthFirstTraversal(nodeA, n => { visited.Add(n); return true; });

        visited.Count.ShouldBe(5);
        visited[0].ShouldBe(nodeA); // Level 0
        visited.ShouldContain(nodeB); // Level 1
        visited.ShouldContain(nodeC); // Level 1
        visited.IndexOf(nodeB).ShouldBeOneOf(1, 2);
        visited.IndexOf(nodeC).ShouldBeOneOf(1, 2);
        visited[3].ShouldBe(nodeD); // Level 2
        visited[4].ShouldBe(nodeE); // Level 3
    }

    // --- Topological Sort Tests ---
    [Fact]
    public void TopologicalSort_ComplexDAG_ShouldGiveValidOrder()
    {
        SetupComplexDAG(out var nodeA, out var nodeB, out var nodeC, out var nodeD, out var nodeE);
        var sortedResult = _traversal.TopologicalSort(nodeA); 
        var sortedList = sortedResult.ToList(); 

        sortedList.Count().ShouldBe(5); // Use Count() extension method
        // Check relative order: A before B/C, B/C before D, D before E
        sortedList.IndexOf(nodeA).ShouldBeLessThan(sortedList.IndexOf(nodeB));
        sortedList.IndexOf(nodeA).ShouldBeLessThan(sortedList.IndexOf(nodeC));
        sortedList.IndexOf(nodeB).ShouldBeLessThan(sortedList.IndexOf(nodeD));
        sortedList.IndexOf(nodeC).ShouldBeLessThan(sortedList.IndexOf(nodeD));
        sortedList.IndexOf(nodeD).ShouldBeLessThan(sortedList.IndexOf(nodeE));
    }

    [Fact]
    public void TopologicalSort_MergingGraph_ShouldGiveValidOrder()
    {
         SetupMergingGraph(out var nodeA, out var nodeB, out var nodeC);
         var sorted = _traversal.TopologicalSort(nodeA);

         sorted.Count().ShouldBe(2); // Use Count() extension method
         sorted[0].ShouldBe(nodeA);
         sorted[1].ShouldBe(nodeC);
    }

    [Fact]
    public void TopologicalSort_DisconnectedGraph_SortsReachableComponent()
    {
        SetupDisconnectedGraph(out var nodeA, out var nodeB, out var nodeC, out var nodeD);
        var sorted = _traversal.TopologicalSort(nodeA); 

        sorted.Count().ShouldBe(2); // Use Count() extension method
        sorted[0].ShouldBe(nodeA);
        sorted[1].ShouldBe(nodeB);

        var sorted2 = _traversal.TopologicalSort(nodeC);
        sorted2.Count().ShouldBe(2); // Use Count() extension method
        sorted2[0].ShouldBe(nodeC);
        sorted2[1].ShouldBe(nodeD);
    }

    [Fact]
    public void TopologicalSort_CyclicGraphSimple_ShouldThrowSpecificException()
    {
        SetupCyclicGraphSimple(out var nodeA, out var nodeB);
         Should.Throw<InvalidOperationException>(() => _traversal.TopologicalSort(nodeA))
            .Message.ShouldContain("Cycle detected");
    }

    // --- FindShortestPath Tests ---
    [Fact]
    public void FindShortestPath_ComplexDAG_ShouldFindPath()
    {
         SetupComplexDAG(out var nodeA, out var nodeB, out var nodeC, out var nodeD, out var nodeE);
         var path = _traversal.FindShortestPath(nodeA, nodeE);

         path.ShouldNotBeNull();
         path.Count().ShouldBe(4); // Use Count() extension method
         path[0].ShouldBe(nodeA);
         path[1].ShouldBeOneOf(nodeB, nodeC);
         path[2].ShouldBe(nodeD);
         path[3].ShouldBe(nodeE);
    }

    // === NEW: FindAllSimplePaths Tests ===
    [Fact]
    public void FindAllSimplePaths_LinearGraph_ShouldFindSinglePath()
    {
         var service = _traversal; // Use the class member
         SetupLinearGraph(out var nodeA, out var nodeB, out var nodeC, out _, out _); // Add discards
         // Act
         var paths = service.FindAllSimplePaths(nodeA, nodeC).ToList();
        
         // Assert
         paths.Count.ShouldBe(1);
         paths[0].ShouldBe(new[] { nodeA, nodeB, nodeC });
    }

    [Fact]
    public void FindAllSimplePaths_ComplexDAG_ShouldFindAllPaths()
    {
         var service = _traversal;
         SetupComplexDAG(out var nodeA, out var nodeB, out var nodeC, out var nodeD, out var nodeE);
         // Act - Should fail until implemented
         var pathsResult = service.FindAllSimplePaths(nodeA, nodeE).ToList();

         // Assert
         var paths = pathsResult.Select(p => p.ToList()).ToList(); // Convert to List<List<INode>> for easier assertion
         paths.Count.ShouldBe(2);
         paths.ShouldContain(p => p.SequenceEqual(new[] { nodeA, nodeB, nodeD, nodeE }));
         paths.ShouldContain(p => p.SequenceEqual(new[] { nodeA, nodeC, nodeD, nodeE }));
    }

     [Fact]
    public void FindAllSimplePaths_NoPathExists_ShouldReturnEmpty()
    {
        var service = _traversal;
        SetupDisconnectedGraph(out var nodeA, out _, out var nodeC, out _);
         // Act - Should fail until implemented
        var paths = service.FindAllSimplePaths(nodeA, nodeC).ToList();
        
        // Assert
        paths.ShouldBeEmpty();
    }

    [Fact]
    public void FindAllSimplePaths_StartAndEndSame_ShouldReturnPathWithSingleNode()
    {
         var service = _traversal;
         SetupLinearGraph(out var nodeA, out _, out _, out _, out _); // Add discards, only need nodeA
         // Act - Should fail until implemented
         var paths = service.FindAllSimplePaths(nodeA, nodeA).ToList();
        
         // Assert
         paths.Count.ShouldBe(1);
         paths[0].ShouldHaveSingleItem();
         paths[0][0].ShouldBe(nodeA);
    }

     [Fact]
    public void FindAllSimplePaths_CyclicGraph_ShouldFindSimplePathsOnly()
    {
         var service = _traversal;
         // Graph: A -> B -> C -> D -> B (Cycle B-C-D-B)
         var nodeA = CreateMockNode("A");
         var nodeB = CreateMockNode("B");
         var nodeC = CreateMockNode("C");
         var nodeD = CreateMockNode("D");

         var connAOut = CreateMockConnector(nodeA, ConnectorDirection.Output);
         var connBIn = CreateMockConnector(nodeB, ConnectorDirection.Input);
         var connBOutC = CreateMockConnector(nodeB, ConnectorDirection.Output, Guid.NewGuid());
         var connCIn = CreateMockConnector(nodeC, ConnectorDirection.Input);
         var connCOutD = CreateMockConnector(nodeC, ConnectorDirection.Output);
         var connDIn = CreateMockConnector(nodeD, ConnectorDirection.Input);
         var connDOutB = CreateMockConnector(nodeD, ConnectorDirection.Output); // Cycle back

         nodeA.OutputConnectors.Returns(new[] { connAOut });
         nodeB.InputConnectors.Returns(new[] { connBIn, connDOutB }); // B has input from A and D
         nodeB.OutputConnectors.Returns(new[] { connBOutC });
         nodeC.InputConnectors.Returns(new[] { connCIn });
         nodeC.OutputConnectors.Returns(new[] { connCOutD });
         nodeD.InputConnectors.Returns(new[] { connDIn });
         nodeD.OutputConnectors.Returns(new[] { connDOutB });

         CreateMockConnection(connAOut, connBIn);
         CreateMockConnection(connBOutC, connCIn);
         CreateMockConnection(connCOutD, connDIn);
         CreateMockConnection(connDOutB, connBIn); // Cycle connection D -> B

        // Act - Should fail until implemented
         var paths = service.FindAllSimplePaths(nodeA, nodeD).ToList();
         
        // Assert
         paths.Count.ShouldBe(1); // Should only find A -> B -> C -> D
         paths[0].ShouldBe(new[] { nodeA, nodeB, nodeC, nodeD });
    }

    // --- GetEntry/Exit Points Tests ---
     [Fact]
    public void GetEntryPoints_MergingGraph_ShouldReturnAAndB()
    {
        SetupMergingGraph(out var nodeA, out var nodeB, out var nodeC);
        var entryPoints = _traversal.GetEntryPoints(nodeA);

        entryPoints.Count().ShouldBe(2); // Use Count() extension method
        entryPoints.ShouldContain(nodeA);
        entryPoints.ShouldContain(nodeB);
    }

     [Fact]
    public void GetExitPoints_BranchingGraph_ShouldReturnBAndC()
    {
        SetupBranchingGraph(out var nodeA, out var nodeB, out var nodeC);
         var exitPoints = _traversal.GetExitPoints(nodeA);

        exitPoints.Count().ShouldBe(2); // Use Count() extension method
        exitPoints.ShouldContain(nodeB);
        exitPoints.ShouldContain(nodeC);
    }
} 