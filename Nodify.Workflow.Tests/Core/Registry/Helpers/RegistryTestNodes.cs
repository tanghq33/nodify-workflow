using System;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Registry;

namespace Nodify.Workflow.Tests.Core.Registry.Helpers;

// === Test Nodes for Registry Discovery ===

[WorkflowNode("Simple Node", "Test Category", "A basic runnable node.")]
internal class SimpleRegisteredNode : Node
{
    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken) 
        => Task.FromResult(NodeExecutionResult.Succeeded());
}

[WorkflowNode("Another Node", "Test Category")] // No description
internal class AnotherRegisteredNode : Node
{
     public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken) 
        => Task.FromResult(NodeExecutionResult.Succeeded());
}

[WorkflowNode("Different Cat", "Other Category")]
internal class DifferentCategoryNode : Node
{
     public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken) 
        => Task.FromResult(NodeExecutionResult.Succeeded());
}

// This node should NOT be discovered as it lacks the attribute
internal class UnregisteredNode : Node
{
     public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken) 
        => Task.FromResult(NodeExecutionResult.Succeeded());
}

// This node should NOT be discovered as it's abstract
[WorkflowNode("Abstract Node", "Should Not Appear")]
internal abstract class AbstractRegisteredNode : Node
{
    // No implementation needed for testing discovery
}

// This node should be discovered but might fail instantiation if tested
[WorkflowNode("Needs Param", "Test Category")]
internal class NodeWithParamConstructor : Node
{
    private readonly int _value;
    public NodeWithParamConstructor(int value) { _value = value; }
    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken) 
        => Task.FromResult(NodeExecutionResult.Succeeded());
}

// This class implements INode but does NOT inherit from Node base
[WorkflowNode("Direct Interface", "Test Category")]
internal class DirectlyImplementingNode : INode
{
    public Guid Id { get; } = Guid.NewGuid();
    public System.Collections.Generic.IReadOnlyCollection<IConnector> InputConnectors => throw new NotImplementedException();
    public System.Collections.Generic.IReadOnlyCollection<IConnector> OutputConnectors => throw new NotImplementedException();
    public double X { get; set; }
    public double Y { get; set; }
    public void AddInputConnector(IConnector connector) => throw new NotImplementedException();
    public void AddOutputConnector(IConnector connector) => throw new NotImplementedException();
    public IConnector? GetInputConnector(Guid id) => throw new NotImplementedException();
    public IConnector? GetOutputConnector(Guid id) => throw new NotImplementedException();
    public bool RemoveConnector(IConnector connector) => throw new NotImplementedException();
    public void RemoveInputConnector(IConnector connector) => throw new NotImplementedException();
    public void RemoveOutputConnector(IConnector connector) => throw new NotImplementedException();
    public bool Validate() => true;
    public Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken) 
        => Task.FromResult(NodeExecutionResult.Succeeded());
} 