using System;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution.Events;

/// <summary>
/// Event arguments raised when a node has successfully completed execution.
/// </summary>
public class NodeExecutionCompletedEventArgs : NodeExecutionEventArgs
{
    public NodeExecutionCompletedEventArgs(INode node, IExecutionContext context)
        : base(node, context)
    { }
} 