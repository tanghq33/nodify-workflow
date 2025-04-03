using System;

namespace Nodify.Workflow.Core.Execution;

public class NodeExecutionResult
{
    public bool Success { get; }
    public Exception? Error { get; }
    // Potentially add output data later

    public NodeExecutionResult(bool success, Exception? error = null)
    {
        if (!success && error == null)
        {
            throw new ArgumentNullException(nameof(error), "An error must be provided if execution was not successful.");
        }
        Success = success;
        Error = error;
    }

    public static NodeExecutionResult Succeeded() => new(true);
    public static NodeExecutionResult Failed(Exception error) => new(false, error);
} 