using System;

namespace Nodify.Workflow.Core.Models;

/// <summary>
/// Represents the outcome of a graph operation, indicating success or failure
/// and providing the result or an error message.
/// </summary>
/// <typeparam name="T">The type of the result on success.</typeparam>
public readonly struct OperationResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the result of the operation if successful, otherwise the default value for type T.
    /// </summary>
    public T Result { get; }

    /// <summary>
    /// Gets the error message if the operation failed, otherwise null or empty.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResult{T}"/> struct.
    /// </summary>
    /// <param name="success">True if the operation was successful, false otherwise.</param>
    /// <param name="result">The result of the operation if successful.</param>
    /// <param name="errorMessage">A message describing the error if the operation failed.</param>
    public OperationResult(bool success, T result, string errorMessage)
    {
        Success = success;
        Result = success ? result : default!; // Ensure result is default if not successful
        ErrorMessage = errorMessage ?? string.Empty; // Ensure error message is not null
    }

    // Optional: Add convenience methods if desired
    // public bool IsFailure => !Success;

    // Optional: Deconstruct method for pattern matching
    // public void Deconstruct(out bool success, out T result, out string errorMessage)
    // {
    //     success = Success;
    //     result = Result;
    //     errorMessage = ErrorMessage;
    // }
} 