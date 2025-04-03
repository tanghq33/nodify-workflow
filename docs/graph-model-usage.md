# Graph Model Usage Examples

This document provides practical examples of how to use the core graph model API (`IGraph`, `INode`, `IConnector`, `IConnection`) and its default implementation (`Graph`, `Node`, `Connector`, `Connection`).

## 1. Setting up the Graph

First, instantiate the `Graph` class:

```csharp
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using System;
using System.Collections.Generic; // Required for List<IConnector>

// Assuming you have Node and Connector implementations available
// (or use the provided base classes if suitable)

var graph = new Graph();
Console.WriteLine("Graph created.");
```

## 2. Creating Nodes

Nodes represent operations in the workflow. Create instances of your node types (which should implement `INode`). Here we use the base `Node` class for simplicity.

```csharp
// Create two basic nodes
var nodeA = new Node { Title = "Start Node" };
var nodeB = new Node { Title = "End Node" };

Console.WriteLine($"Created Node A (ID: {nodeA.Id}, Title: {nodeA.Title})");
Console.WriteLine($"Created Node B (ID: {nodeB.Id}, Title: {nodeB.Title})");
```

## 3. Creating Connectors

Connectors define the input and output points for nodes. They must belong to a node and have a direction and data type.

```csharp
// Create an output connector for Node A (e.g., produces a string)
var outputConnectorA = new Connector(nodeA, ConnectorDirection.Output, typeof(string)) { Title = "Output" };

// Create an input connector for Node B (e.g., accepts a string)
var inputConnectorB = new Connector(nodeB, ConnectorDirection.Input, typeof(string)) { Title = "Input" };

Console.WriteLine($"Created Output Connector for Node A (ID: {outputConnectorA.Id}, Type: {outputConnectorA.DataType.Name})");
Console.WriteLine($"Created Input Connector for Node B (ID: {inputConnectorB.Id}, Type: {inputConnectorB.DataType.Name})");

// Associate connectors with their parent nodes
// (The base Node class might require explicit addition, or your custom Node constructor could handle this)
// Assuming Node class has methods like AddInputConnector/AddOutputConnector or takes them in constructor
// For the base Node, we might need to manage this manually or modify the base Node class.
// Let's assume the Node implementation handles association correctly when connectors are created.
// If not, you might need:
// nodeA.OutputConnectors = new List<IConnector> { outputConnectorA };
// nodeB.InputConnectors = new List<IConnector> { inputConnectorB };
// Or a method like: nodeA.AddOutputConnector(outputConnectorA);
```
*Note: The exact way connectors are associated depends on your `INode` implementation. Ensure your nodes correctly manage their `InputConnectors` and `OutputConnectors` collections.*


## 4. Adding Nodes to the Graph

Use the `TryAddNode` (recommended) or `AddNode` method:

```csharp
var addResultA = graph.TryAddNode(nodeA);
if (!addResultA.Success)
{
    Console.WriteLine($"Error adding Node A: {addResultA.ErrorMessage}");
    return; // Or handle error appropriately
}

var addResultB = graph.TryAddNode(nodeB);
if (!addResultB.Success)
{
    Console.WriteLine($"Error adding Node B: {addResultB.ErrorMessage}");
    return;
}

Console.WriteLine($"Nodes added to graph. Node count: {graph.Nodes.Count}");
```

## 5. Adding Connections

Connect an output connector of one node to an input connector of another node using `TryAddConnection` (recommended) or `AddConnection`. The graph performs validation checks.

```csharp
var connectionResult = graph.TryAddConnection(outputConnectorA, inputConnectorB);

if (connectionResult.Success)
{
    var connection = connectionResult.Result;
    Console.WriteLine($"Connection added successfully (ID: {connection.Id}).");
    Console.WriteLine($"Graph connection count: {graph.Connections.Count}");
}
else
{
    Console.WriteLine($"Failed to add connection: {connectionResult.ErrorMessage}");
}

// Example: Trying to add an invalid connection (e.g., incompatible types or circular reference)
var anotherOutput = new Connector(nodeB, ConnectorDirection.Output, typeof(int)); // Output on Node B
var anotherInput = new Connector(nodeA, ConnectorDirection.Input, typeof(int)); // Input on Node A
// nodeB.AddOutputConnector(anotherOutput); // Assuming method exists
// nodeA.AddInputConnector(anotherInput); // Assuming method exists

var circularResult = graph.TryAddConnection(anotherOutput, anotherInput);
if (!circularResult.Success)
{
    Console.WriteLine($"Expected failure for circular connection: {circularResult.ErrorMessage}");
}
```

## 6. Validating the Graph

Check the overall validity of the graph structure:

```csharp
bool isValid = graph.Validate();
Console.WriteLine($"Is graph valid? {isValid}"); // Should be true if steps above succeeded

// Example: Make the graph invalid (e.g., remove a node leaving an orphaned connection)
graph.RemoveNode(nodeA); // This should also remove the connection

isValid = graph.Validate();
Console.WriteLine($"Is graph valid after removing Node A? {isValid}"); // Should still be true as RemoveNode cleans up connections

// Re-add Node A and connection for further examples if needed
// graph.AddNode(nodeA);
// graph.AddConnection(outputConnectorA, inputConnectorB);

```

## 7. Removing Connections and Nodes

```csharp
// Remove the specific connection
var connectionToRemove = graph.Connections.FirstOrDefault(); // Get the connection added earlier
if (connectionToRemove != null)
{
    var removeConnResult = graph.TryRemoveConnection(connectionToRemove);
    if (removeConnResult.Success)
    {
        Console.WriteLine("Connection removed successfully.");
        Console.WriteLine($"Graph connection count: {graph.Connections.Count}"); // Should be 0
    }
    else
    {
         Console.WriteLine($"Failed to remove connection: {removeConnResult.ErrorMessage}");
    }
}


// Remove a node (which should also remove any remaining connections associated with it)
var removeNodeResult = graph.TryRemoveNode(nodeB);
if (removeNodeResult.Success)
{
    Console.WriteLine("Node B removed successfully.");
    Console.WriteLine($"Graph node count: {graph.Nodes.Count}"); // Should be 1 (only Node A remains)
}
else
{
     Console.WriteLine($"Failed to remove Node B: {removeNodeResult.ErrorMessage}");
}
```

These examples cover the basic CRUD (Create, Read, Update, Delete) operations for the graph model. Refer to the XML documentation within the `Nodify.Workflow.Core` library for more detailed API information. 