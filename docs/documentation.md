# Technical Documentation

## Graph Model Architecture

### Core Components

#### 1. Node System
- **Node (`INode`, `Node`)**
  - Basic building block of the workflow
  - Contains input and output connectors
  - Maintains position information (X, Y) for UI representation
  - Ensures proper cleanup of connections when removed

#### 2. Connection System
- **Connector (`IConnector`, `Connector`)**
  - Represents connection points on nodes
  - Types: Input and Output (defined by `ConnectorDirection`)
  - Maintains a collection of active connections
  - Validates connection compatibility based on data types
  - Enforces connection constraints (e.g., single input)

- **Connection (`IConnection`, `Connection`)**
  - Links output connector to input connector
  - Maintains bidirectional relationships
  - Validates connection validity

### Design Decisions

#### 1. Bidirectional Relationship Pattern
The graph model maintains bidirectional relationships between components:

```plaintext
Node ←→ Connector ←→ Connection
```

**Why this approach?**
- **Performance**: O(1) access to related components
- **Consistency**: Easier to maintain graph integrity
- **Traversal**: Efficient navigation in any direction
- **Validation**: Quick access for constraint checking

**Trade-offs considered:**
- Memory usage vs. lookup performance
- Complexity of maintaining bidirectional relationships
- Alternative approaches (global registry, unidirectional relationships)

#### 2. Connection Collection in Connectors

Each connector maintains its own collection of connections, even though connections reference their endpoints. This design decision provides:

**Benefits:**
- O(1) access to all connections from a connector
- Simplified graph traversal
- Efficient validation of connection constraints
- Clean resource management

**Implementation considerations:**
```csharp
public class Connector : IConnector
{
    private readonly List<IConnection> _connections;
    
    public IReadOnlyCollection<IConnection> Connections => _connections.AsReadOnly();
    
    public bool AddConnection(IConnection connection)
    {
        // Validate before adding
        if (!ValidateConnection(connection)) return false;
        
        _connections.Add(connection);
        return true;
    }
}
```

#### 3. Graph Traversal Implementation

The `DefaultGraphTraversal` service implements various traversal algorithms:

**Depth-First Search (DFS)**
- Used for: Deep exploration, cycle detection
- Implementation: Recursive with visited set
- Time complexity: O(V + E)

**Breadth-First Search (BFS)**
- Used for: Level-wise traversal, shortest paths (edge count)
- Implementation: Queue-based with visited set
- Time complexity: O(V + E)

**Topological Sort**
- Used for: Execution order determination
- Implementation: DFS with reverse post-order, throws on cycle detection
- Time complexity: O(V + E)

**Find All Simple Paths**
- Used for: Finding all paths between two nodes without repeating nodes.
- Implementation: Recursive DFS with path tracking and visited set per path.
- Time complexity: Can be exponential in the worst case (O(V! * E) or similar for dense graphs with many paths), but often faster for typical DAGs.

### Resource Management

#### 1. Connection Cleanup
- Connections are removed when:
  - A node is deleted
  - A connector is removed
  - The connection itself is explicitly removed
- Cleanup ensures both endpoints are properly updated

#### 2. Circular Reference Detection
- Implemented in the `Graph` class using depth-first search.
- Prevents creation of connections that would form a cycle (`Graph.TryAddConnection`).
- Supports full graph cycle detection for validation (`Graph.TryValidate`).

### Thread Safety Considerations

The current implementation is not thread-safe by default. When using in a multi-threaded environment:

- Use synchronization when modifying the graph
- Consider implementing a read-write lock pattern
- Be aware of potential race conditions in traversal

### Validation System

#### 1. Connection Validation
- Type compatibility checking
- Cardinality rules (e.g., single input)
- Custom validation rules support

#### 2. Node Validation
- Connector relationship validation
- Custom node-specific validation
- Graph-wide constraint checking

### Extension Points

#### 1. Custom Node Types
- Inherit from `Node` or implement `INode`
- Override validation as needed
- Add custom behavior

#### 2. Custom Validation Rules
- Implement custom validation logic
- Add type-specific constraints
- Extend existing validation system

## Execution Engine Architecture

### Core Components

#### 1. Execution Context (`IExecutionContext`, `ExecutionContext`)
- **Purpose**: Manages the state during a single workflow execution instance.
- **Responsibilities**:
  - Stores and retrieves variables (`SetVariable`, `GetVariable`, `TryGetVariable`).
  - Tracks the current execution status (`CurrentStatus`, `SetStatus`, `ExecutionStatus` enum: `NotStarted`, `Running`, `Completed`, `Failed`, `Cancelled`).
  - Records execution logs (`AddLog`, `GetLogs`).
  - Tracks the currently executing node (`CurrentNodeId`, `SetCurrentNode`, `ClearCurrentNode`).
  - Holds a unique execution ID (`ExecutionId`).
- **Implementation**: `ExecutionContext` provides a dictionary-based implementation for variables (case-insensitive keys) and a list for logs.

#### 2. Node Executor (`INodeExecutor`, `DefaultNodeExecutor`)
- **Purpose**: Defines the contract for executing the logic of a single node.
- **Responsibilities**: Encapsulates the mechanism for invoking `INode.ExecuteAsync`.
- **Implementation**: `DefaultNodeExecutor` directly calls `node.ExecuteAsync`, passing the context and cancellation token. It handles basic null checks but primarily acts as a bridge between the runner and the node's logic.

#### 3. Graph Traversal (`IGraphTraversal`, `DefaultGraphTraversal`)
- **Purpose**: Determines the order of node execution.
- **Responsibilities**: Provides algorithms like `TopologicalSort` to get a linear execution sequence for a DAG. Detects cycles.
- **Usage**: The `WorkflowRunner` uses this service to get the list of nodes to execute.

#### 4. Workflow Runner (`WorkflowRunner`)
- **Purpose**: Orchestrates the execution of a workflow graph.
- **Responsibilities**:
  - Accepts a starting node, execution context, and an optional `CancellationToken`.
  - Uses `IGraphTraversal` to determine the execution order.
  - Iterates through nodes in the execution order.
  - Uses `INodeExecutor` to execute each node.
  - Manages the overall workflow status (`Running`, `Completed`, `Failed`, `Cancelled`).
  - Raises execution events at appropriate stages.
  - Handles exceptions, including `OperationCanceledException` for graceful cancellation.

### Execution Lifecycle & Events

The `WorkflowRunner` raises events to allow external monitoring and interaction:

- **`WorkflowStarted` (`WorkflowExecutionStartedEventArgs`)**: Fired once at the beginning of `RunAsync`.
- **`NodeStarting` (`NodeExecutionStartingEventArgs`)**: Fired just before a node's execution begins via the `INodeExecutor`.
- **`NodeCompleted` (`NodeExecutionCompletedEventArgs`)**: Fired after a node executes successfully.
- **`NodeFailed` (`NodeExecutionFailedEventArgs`)**: Fired if a node's execution results in failure (either by returning `NodeExecutionResult.Failed` or throwing an exception *other* than `OperationCanceledException`). Contains the node and the error.
- **`WorkflowCancelled` (`WorkflowCancelledEventArgs`)**: Fired if execution is stopped due to a `CancellationToken` request (caught via `OperationCanceledException`).
- **`WorkflowCompleted` (`WorkflowExecutionCompletedEventArgs`)**: Fired if all nodes in the execution path complete successfully. Contains the final status.
- **`WorkflowFailed` (`WorkflowExecutionFailedEventArgs`)**: Fired if any node fails or an unhandled exception occurs during the workflow run. Contains the error and potentially the failed node.

### Asynchronous Execution & Cancellation

- **Async Operations**: The core execution path is asynchronous (`async Task`).
  - `WorkflowRunner.RunAsync`
  - `INodeExecutor.ExecuteAsync`
  - `INode.ExecuteAsync`
- **Cancellation**: A `CancellationToken` is passed down from `RunAsync` through the `INodeExecutor` to `INode.ExecuteAsync`.
  - **Runner Checks**: The runner checks the token before starting the workflow, before traversing, and before executing each node.
  - **Node Responsibility**: Individual nodes performing long-running operations (like `Task.Delay`, I/O) should honor the received `CancellationToken`.
  - **Handling**: `OperationCanceledException` is caught by the runner, which sets the status to `Cancelled` and raises the `WorkflowCancelled` event.

## Performance Considerations

### 1. Time Complexity
- Node operations: O(1)
- Connection operations: O(1)
- Graph traversal: O(V + E)
- Path finding: O(V + E)
- Validation: O(1) to O(V + E)

### 2. Memory Usage
- Node: O(1) + O(C) where C = number of connectors
- Connector: O(1) + O(N) where N = number of connections
- Graph: O(V + E) where V = vertices, E = edges

### 3. Optimization Opportunities
- Connection collection implementation
- Traversal algorithm improvements
- Validation caching
- Lazy loading of components

## Known Limitations

1. No built-in thread safety
2. Memory overhead from bidirectional relationships
3. Limited support for dynamic type validation

## Future Considerations

1. Thread safety implementation
2. Performance optimizations
3. Additional traversal algorithms
4. Enhanced validation system
5. Serialization improvements
