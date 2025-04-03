using System;

namespace Nodify.Workflow.Core.Execution;

public class NodeExecutionResult
{
    public bool Success { get; }
    public Exception? Error { get; }
    public Guid? ActivatedOutputConnectorId { get; }

    public NodeExecutionResult(bool success, Exception? error = null, Guid? activatedOutputConnectorId = null)
    {
        if (!success && error == null)
        {
            throw new ArgumentNullException(nameof(error), "An error must be provided if execution was not successful.");
        }
        if (success && activatedOutputConnectorId == null && error == null)
        {
           // Allow successful completion without output activation (e.g., EndNode)
        }
        else if (success && activatedOutputConnectorId != null && error == null)
        {
            // Standard success with output activation
        }
        else if (!success && error != null && activatedOutputConnectorId == null)
        { 
            // Standard failure
        }
        else
        {   // Disallow invalid combinations (e.g. success with error, failure with activation)
            throw new ArgumentException("Invalid combination of success, error, and activatedOutputConnectorId.");
        }

        Success = success;
        Error = error;
        ActivatedOutputConnectorId = activatedOutputConnectorId;
    }

    public static NodeExecutionResult Succeeded() => new(true, null, null);
    public static NodeExecutionResult Succeeded(Guid activatedOutputConnectorId) => new(true, null, activatedOutputConnectorId);
    public static NodeExecutionResult Failed(Exception error) => new(false, error, null);
} 