using System;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution.Events;

public class WorkflowExecutionFailedEventArgs : EventArgs
{
     public IExecutionContext Context { get; }
     public INode? FailedNode { get; } // Example
     public Exception Error { get; } // Example
     public WorkflowExecutionFailedEventArgs(IExecutionContext context, Exception error, INode? failedNode = null)
     { Context = context; Error = error; FailedNode = failedNode; }
} 