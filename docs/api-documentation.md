# Nodify Workflow API Documentation

## Getting Started

### Installation

```bash
dotnet add package Nodify.Workflow
```

### Basic Usage

```csharp
using Nodify.Workflow.Core.Graph.Models;
using Nodify.Workflow.Core.Graph.Services;

// Create nodes
var startNode = new Node();
var endNode = new Node();

// Create and connect connectors
var output = new Connector(startNode, ConnectorDirection.Output, typeof(string));
var input = new Connector(endNode, ConnectorDirection.Input, typeof(string));

startNode.AddOutputConnector(output);
endNode.AddInputConnector(input);

// Create connection
var connection = new Connection(output, input);
```

## Core Components

### Node

The basic building block of the workflow graph.

```csharp
public interface INode
{
    Guid Id { get; }
    IReadOnlyCollection<IConnector> InputConnectors { get; }
    IReadOnlyCollection<IConnector> OutputConnectors { get; }
    double X { get; set; }
    double Y { get; set; }
    
    void AddInputConnector(IConnector connector);
    void AddOutputConnector(IConnector connector);
    bool RemoveConnector(IConnector connector);
    bool Validate();
}
```

#### Example: Creating a Custom Node

```csharp
public class StringProcessorNode : Node
{
    public StringProcessorNode()
    {
        // Create input connector
        var input = new Connector(this, ConnectorDirection.Input, typeof(string));
        AddInputConnector(input);

        // Create output connector
        var output = new Connector(this, ConnectorDirection.Output, typeof(string));
        AddOutputConnector(output);
    }

    public override bool Validate()
    {
        // Custom validation logic
        return base.Validate() && 
               InputConnectors.Count == 1 && 
               OutputConnectors.Count == 1;
    }
}
```

### Connector

Represents a connection point on a node.

```csharp
public interface IConnector
{
    Guid Id { get; }
    ConnectorDirection Direction { get; }
    INode ParentNode { get; }
    Type DataType { get; }
    IReadOnlyCollection<IConnection> Connections { get; }
    
    bool AddConnection(IConnection connection);
    bool RemoveConnection(IConnection connection);
    bool ValidateConnection(IConnector other);
}
```

#### Example: Type-Safe Connections

```csharp
// Create strongly-typed connectors
var numberOutput = new Connector(node1, ConnectorDirection.Output, typeof(int));
var stringInput = new Connector(node2, ConnectorDirection.Input, typeof(string));

// This will fail validation due to type mismatch
var connection = new Connection(numberOutput, stringInput); // Throws ArgumentException
```

### Connection

Links two connectors together.

```csharp
public interface IConnection
{
    Guid Id { get; }
    IConnector Source { get; }
    IConnector Target { get; }
    
    bool Validate();
    void Remove();
    bool WouldCreateCircularReference();
}
```

#### Example: Managing Connections

```csharp
// Create connection
var connection = new Connection(output, input);

// Check for validity
if (!connection.Validate())
{
    // Handle invalid connection
}

// Check for cycles
if (connection.WouldCreateCircularReference())
{
    // Handle circular reference
}

// Remove connection
connection.Remove();
```

## Graph Traversal

The `GraphTraversal` service provides methods to explore and analyze the workflow graph.

### Depth-First Traversal

```csharp
var traversal = new GraphTraversal();

// Visit all nodes
traversal.DepthFirstTraversal(startNode, node => {
    Console.WriteLine($"Visiting node: {node.Id}");
    return true; // continue traversal
});

// Stop at specific condition
traversal.DepthFirstTraversal(startNode, node => {
    if (node.OutputConnectors.Count == 0) {
        Console.WriteLine("Found end node!");
        return false; // stop traversal
    }
    return true;
});
```

### Finding Paths

```csharp
// Find shortest path between nodes
var path = traversal.FindShortestPath(startNode, endNode);
if (path.Count > 0)
{
    Console.WriteLine("Path found:");
    foreach (var node in path)
    {
        Console.WriteLine($"-> {node.Id}");
    }
}

// Find node by ID
var targetId = Guid.Parse("...");
var foundNode = traversal.FindNodeById(startNode, targetId);
```

### Analyzing Graph Structure

```csharp
// Get entry points (nodes with no inputs)
var entryPoints = traversal.GetEntryPoints(startNode);

// Get exit points (nodes with no outputs)
var exitPoints = traversal.GetExitPoints(startNode);

// Get execution order
var sortedNodes = traversal.TopologicalSort(startNode);
```

## Best Practices

### 1. Connection Management

```csharp
// GOOD: Let the Connection constructor handle bidirectional relationships
var connection = new Connection(output, input);

// BAD: Don't manually manage connections
output.AddConnection(connection);
input.AddConnection(connection);
```

### 2. Resource Cleanup

```csharp
// GOOD: Use the Remove method to clean up connections
connection.Remove();

// BAD: Don't manually remove from just one side
connector.RemoveConnection(connection);
```

### 3. Validation

```csharp
// GOOD: Validate before creating connections
if (output.ValidateConnection(input))
{
    var connection = new Connection(output, input);
}

// GOOD: Check for cycles
if (!connection.WouldCreateCircularReference())
{
    // Proceed with connection
}
```

### 4. Type Safety

```csharp
// GOOD: Use strongly-typed connectors
var stringOutput = new Connector(node, ConnectorDirection.Output, typeof(string));

// BAD: Don't use object type unless absolutely necessary
var genericOutput = new Connector(node, ConnectorDirection.Output, typeof(object));
```

## Error Handling

Common exceptions and how to handle them:

```csharp
try
{
    var connection = new Connection(output, input);
}
catch (ArgumentException ex)
{
    // Handle invalid connection parameters
    // - Incompatible types
    // - Wrong connector directions
    // - Invalid connection state
}
catch (InvalidOperationException ex)
{
    // Handle operation failures
    // - Circular reference detection
    // - Connection limit exceeded
}
```

## Advanced Usage

### Custom Validation Rules

```csharp
public class CustomConnector : Connector
{
    public override bool ValidateConnection(IConnector other)
    {
        // Custom validation logic
        if (!base.ValidateConnection(other))
            return false;

        // Additional rules
        if (Connections.Count >= 3)
            return false; // Maximum 3 connections

        return true;
    }
}
```

### Graph Analysis

```csharp
public class GraphAnalyzer
{
    private readonly GraphTraversal _traversal;

    public bool HasCycles(INode startNode)
    {
        var visited = new HashSet<INode>();
        var stack = new HashSet<INode>();
        
        bool DFS(INode node)
        {
            if (stack.Contains(node))
                return true; // Cycle detected
                
            if (visited.Contains(node))
                return false;
                
            visited.Add(node);
            stack.Add(node);
            
            foreach (var connector in node.OutputConnectors)
            foreach (var connection in connector.Connections)
            {
                if (DFS(connection.Target.ParentNode))
                    return true;
            }
            
            stack.Remove(node);
            return false;
        }
        
        return DFS(startNode);
    }
}
```

## Performance Tips

1. **Avoid Unnecessary Traversals**
   ```csharp
   // GOOD: Store references when needed frequently
   var endNodes = traversal.GetExitPoints(startNode).ToList();
   
   // BAD: Don't traverse repeatedly
   foreach (var item in items)
   {
       var ends = traversal.GetExitPoints(startNode);
       // Process ends...
   }
   ```

2. **Use Appropriate Collections**
   ```csharp
   // GOOD: Use HashSet for visited nodes
   var visited = new HashSet<INode>();
   
   // BAD: Don't use List for lookups
   var visited = new List<INode>();
   ```

3. **Optimize Validation**
   ```csharp
   // GOOD: Cache validation results when possible
   private bool? _isValid;
   public override bool Validate()
   {
       if (_isValid.HasValue)
           return _isValid.Value;
           
       _isValid = PerformValidation();
       return _isValid.Value;
   }
   ```
