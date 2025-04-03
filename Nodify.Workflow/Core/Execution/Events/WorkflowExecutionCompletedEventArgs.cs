using System;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution.Events;

public class WorkflowExecutionCompletedEventArgs : EventArgs
{
     public IExecutionContext Context { get; }
     public ExecutionStatus FinalStatus { get; } // Example
     public WorkflowExecutionCompletedEventArgs(IExecutionContext context, ExecutionStatus status)
     { Context = context; FinalStatus = status; }
} 