using System;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution.Events;

public class NodeExecutionFailedEventArgs : NodeExecutionEventArgs
{
    public Exception Error { get; }
    public NodeExecutionFailedEventArgs(INode node, IExecutionContext context, Exception error)
        : base(node, context) { Error = error; }
} 