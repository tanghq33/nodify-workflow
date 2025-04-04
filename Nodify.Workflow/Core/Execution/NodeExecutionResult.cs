using System;

namespace Nodify.Workflow.Core.Execution;

public class NodeExecutionResult
{
    public bool Success { get; }
    public Exception? Error { get; }
    public Guid? ActivatedOutputConnectorId { get; }
    public object? OutputData { get; }

    public NodeExecutionResult(bool success, Exception? error = null, Guid? activatedOutputConnectorId = null, object? outputData = null)
    {
        if (!success)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error), "An error must be provided if execution failed.");
            if (activatedOutputConnectorId != null)
                throw new ArgumentException("Cannot activate an output connector on failure.", nameof(activatedOutputConnectorId));
            if (outputData != null)
                throw new ArgumentException("Cannot have output data on failure.", nameof(outputData));
        }
        else
        {
            if (error != null)
                throw new ArgumentException("Cannot provide an error on success.", nameof(error));

            if (activatedOutputConnectorId == null && outputData != null)
                throw new ArgumentException("Cannot have output data without activating an output connector.", nameof(outputData));
        }

        Success = success;
        Error = error;
        ActivatedOutputConnectorId = activatedOutputConnectorId;
        OutputData = outputData;
    }

    public static NodeExecutionResult Succeeded() => new(true, null, null, null);
    public static NodeExecutionResult Succeeded(Guid activatedOutputConnectorId) => new(true, null, activatedOutputConnectorId, null);
    public static NodeExecutionResult SucceededWithData(Guid activatedOutputConnectorId, object? outputData) => new(true, null, activatedOutputConnectorId, outputData);
    public static NodeExecutionResult Failed(Exception error) => new(false, error, null, null);
} 