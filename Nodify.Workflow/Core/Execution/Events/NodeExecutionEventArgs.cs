using System;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution.Events;

public class NodeExecutionEventArgs : EventArgs
{
    public INode Node { get; }
    public IExecutionContext Context { get; }
    public NodeExecutionEventArgs(INode node, IExecutionContext context)
    { Node = node; Context = context; }
} 