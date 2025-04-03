using System;
using System.Collections.Generic;
using System.Linq;

namespace Nodify.Workflow.Core.Execution.Context
{
    /// <summary>
    /// Manages the state during workflow execution, including variables, status, and logs.
    /// </summary>
    public class ExecutionContext : IExecutionContext
    {
        private readonly Dictionary<string, object?> _variables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _logs = new List<string>();
        private Guid? _currentNodeId;

        /// <summary>
        /// Gets the current status of the workflow execution.
        /// </summary>
        public ExecutionStatus CurrentStatus { get; private set; } = ExecutionStatus.NotStarted;

        /// <summary>
        /// Gets the ID of the node currently being executed, if any.
        /// </summary>
        public Guid? CurrentNodeId => _currentNodeId;

        /// <summary>
        /// Sets or updates a variable in the execution context.
        /// </summary>
        /// <param name="key">The case-insensitive key of the variable.</param>
        /// <param name="value">The value to store.</param>
        public void SetVariable(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Variable key cannot be null or whitespace.", nameof(key));
            }
            _variables[key] = value;
        }

        /// <summary>
        /// Gets the value of a variable from the execution context.
        /// </summary>
        /// <param name="key">The case-insensitive key of the variable.</param>
        /// <returns>The value of the variable, or null if the key is not found.</returns>
        public object? GetVariable(string key)
        {
             if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Variable key cannot be null or whitespace.", nameof(key));
            }
            if (_variables.TryGetValue(key, out object? value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Tries to get the value of a variable and cast it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the variable.</typeparam>
        /// <param name="key">The case-insensitive key of the variable.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found and the value can be cast to type T; otherwise, the default value for type T.</param>
        /// <returns>true if the key was found and the value could be cast to type T; otherwise, false.</returns>
        public bool TryGetVariable<T>(string key, out T? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                 throw new ArgumentException("Variable key cannot be null or whitespace.", nameof(key));
            }

            if (_variables.TryGetValue(key, out object? objValue))
            {
                if (objValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
                if (objValue == null && default(T) == null)
                {
                     value = default;
                     return true;
                }
                value = default;
                return false;
            }
            else
            {
                value = default;
                return false;
            }
        }


        /// <summary>
        /// Sets the current execution status.
        /// </summary>
        /// <param name="status">The new status.</param>
        public void SetStatus(ExecutionStatus status)
        {
            CurrentStatus = status;
        }

        /// <summary>
        /// Adds a log message to the execution context.
        /// </summary>
        /// <param name="message">The log message.</param>
        public void AddLog(string message)
        {
            _logs.Add(message ?? string.Empty);
        }

        /// <summary>
        /// Gets an enumeration of all log messages recorded in this context.
        /// </summary>
        /// <returns>An enumerable collection of log messages.</returns>
        public IEnumerable<string> GetLogs()
        {
            return _logs.AsReadOnly();
        }

        /// <summary>
        /// Sets the ID of the node currently being processed.
        /// </summary>
        /// <param name="nodeId">The Guid of the current node.</param>
        public void SetCurrentNode(Guid nodeId)
        {
            _currentNodeId = nodeId;
        }

         /// <summary>
        /// Resets the current node ID (e.g., when execution finishes or between steps).
        /// </summary>
        public void ClearCurrentNode()
        {
            _currentNodeId = null;
        }

        /// <summary>
        /// Placeholder for evaluating conditions based on context variables.
        /// </summary>
        /// <param name="condition">The condition string to evaluate.</param>
        /// <returns>The result of the condition evaluation.</returns>
        public bool EvaluateCondition(string condition)
        {
             if (TryGetVariable<bool>(condition, out var boolValue))
             {
                 return boolValue;
             }
            System.Diagnostics.Debug.WriteLine($"Warning: Condition evaluation for '{condition}' not fully implemented.");
            return false;
        }
    }
} 