using System;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution.Events;

public class WorkflowExecutionStartedEventArgs : EventArgs
{
    public IExecutionContext Context { get; }
    public Guid WorkflowId { get; } // Example property
    public WorkflowExecutionStartedEventArgs(IExecutionContext context, Guid workflowId)
    { Context = context; WorkflowId = workflowId; }
} 