using System;
using System.Collections.Generic;

namespace Nodify.Workflow.Core.Execution.Context
{
    /// <summary>
    /// Defines the interface for an execution context.
    /// </summary>
    public interface IExecutionContext
    {
        /// <summary>
        /// Gets the current status of the workflow execution.
        /// </summary>
        ExecutionStatus CurrentStatus { get; } // Changed type to ExecutionStatus enum

        /// <summary>
        /// Gets the ID of the node currently being executed, if any.
        /// </summary>
        Guid? CurrentNodeId { get; }

        /// <summary>
        /// Sets or updates a variable in the execution context.
        /// </summary>
        /// <param name="key">The case-insensitive key of the variable.</param>
        /// <param name="value">The value to store.</param>
        void SetVariable(string key, object? value);

        /// <summary>
        /// Gets the value of a variable from the execution context.
        /// </summary>
        /// <param name="key">The case-insensitive key of the variable.</param>
        /// <returns>The value of the variable, or null if the key is not found.</returns>
        object? GetVariable(string key);

        /// <summary>
        /// Tries to get the value of a variable and cast it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the variable.</typeparam>
        /// <param name="key">The case-insensitive key of the variable.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found and the value can be cast to type T; otherwise, the default value for type T.</param>
        /// <returns>true if the key was found and the value could be cast to type T; otherwise, false.</returns>
         bool TryGetVariable<T>(string key, out T? value);

        /// <summary>
        /// Sets the current execution status.
        /// </summary>
        /// <param name="status">The new status.</param>
        void SetStatus(ExecutionStatus status); // Changed type to ExecutionStatus enum

        /// <summary>
        /// Adds a log message to the execution context.
        /// </summary>
        /// <param name="message">The log message.</param>
        void AddLog(string message);

        /// <summary>
        /// Gets an enumeration of all log messages recorded in this context.
        /// </summary>
        /// <returns>An enumerable collection of log messages.</returns>
        IEnumerable<string> GetLogs();

        /// <summary>
        /// Sets the ID of the node currently being processed.
        /// </summary>
        /// <param name="nodeId">The Guid of the current node.</param>
        void SetCurrentNode(Guid nodeId);

         /// <summary>
        /// Resets the current node ID (e.g., when execution finishes or between steps).
        /// </summary>
         void ClearCurrentNode();

        /// <summary>
        /// Placeholder for evaluating conditions based on context variables.
        /// </summary>
        /// <param name="condition">The condition string to evaluate.</param>
        /// <returns>The result of the condition evaluation.</returns>
        bool EvaluateCondition(string condition); // Or a more complex input type

        /// <summary>
        /// Gets the execution ID.
        /// </summary>
        Guid ExecutionId { get; }

        /// <summary>
        /// Gets all variables in the execution context.
        /// </summary>
        /// <returns>A read-only dictionary containing all variables.</returns>
        IReadOnlyDictionary<string, object> GetAllVariables();
    }
} 