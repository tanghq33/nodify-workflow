using System;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution.Events;

/// <summary>
/// Event arguments raised just before a node begins execution.
/// </summary>
public class NodeExecutionStartingEventArgs : NodeExecutionEventArgs
{
    public NodeExecutionStartingEventArgs(INode node, IExecutionContext context)
        : base(node, context)
    { }
} 