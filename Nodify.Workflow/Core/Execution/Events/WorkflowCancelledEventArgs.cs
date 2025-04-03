using System;
using Nodify.Workflow.Core.Execution.Context;

namespace Nodify.Workflow.Core.Execution.Events;

/// <summary>
/// Event arguments raised when a workflow execution is cancelled.
/// </summary>
public class WorkflowCancelledEventArgs : EventArgs
{
    /// <summary>
    /// Gets the execution context at the time of cancellation.
    /// </summary>
    public IExecutionContext Context { get; }

    public WorkflowCancelledEventArgs(IExecutionContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
} 