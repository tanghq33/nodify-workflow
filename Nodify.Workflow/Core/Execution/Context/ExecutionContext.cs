using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;

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
        private ExecutionStatus _currentStatus = ExecutionStatus.NotStarted;
        private readonly ConcurrentDictionary<Guid, object?> _outputConnectorValues = new();

        /// <summary>
        /// Gets the unique identifier for this execution context.
        /// </summary>
        public Guid ExecutionId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionContext"/> class.
        /// </summary>
        public ExecutionContext()
        {
            ExecutionId = Guid.NewGuid();
        }

        /// <summary>
        /// Gets the current status of the workflow execution.
        /// </summary>
        public ExecutionStatus CurrentStatus => _currentStatus;

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
            return _variables.TryGetValue(key, out var value) ? value : null;
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
            value = default;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (_variables.TryGetValue(key, out var objValue))
            {
                if (objValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
                // Handle case where value exists but is wrong type or null
                // If T is a value type and objValue is null, should return false.
                if (objValue == null && default(T) != null)
                {
                    return false;
                }
                // Try conversion if types don't match directly but might be convertible
                try
                {
                    // Allow null propagation if T is nullable
                    value = (T?)Convert.ChangeType(objValue, typeof(T)); 
                    // Check if conversion resulted in null when T is a non-nullable value type
                    if (value == null && default(T) != null) return false;
                    return true;
                }
                catch (InvalidCastException)
                {
                    return false;
                }
                catch (FormatException)
                {
                    return false;
                }
                catch(ArgumentNullException) // ChangeType throws this if value is null and T is a value type
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the current execution status.
        /// </summary>
        /// <param name="status">The new status.</param>
        public void SetStatus(ExecutionStatus status)
        {
            _currentStatus = status;
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
            // Attempt to get the variable directly as an object
            if (_variables.TryGetValue(condition, out var objValue))
            {
                // Check if the retrieved value is actually a boolean
                if (objValue is bool boolValue)
                {
                    return boolValue;
                }
                // Variable exists but is not a boolean type
                 System.Diagnostics.Debug.WriteLine($"Warning: Condition variable '{condition}' is not a boolean type ({objValue?.GetType().Name ?? "null"}). Evaluating as false.");
                return false;
            }
            
            // Variable does not exist
            System.Diagnostics.Debug.WriteLine($"Warning: Condition variable '{condition}' not found. Evaluating as false.");
            return false;
        }

        /// <summary>
        /// Adds a variable to the execution context.
        /// </summary>
        /// <param name="key">The case-insensitive key of the variable.</param>
        /// <param name="value">The value to store.</param>
        public void AddVariable(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Variable key cannot be null or whitespace.", nameof(key));
            }

            if (_variables.ContainsKey(key))
            {
                throw new ArgumentException($"Variable with key '{key}' already exists.", nameof(key));
            }
            _variables.Add(key, value);
        }

        /// <summary>
        /// Gets all variables from the execution context.
        /// </summary>
        /// <returns>A read-only dictionary of all variables.</returns>
        public IReadOnlyDictionary<string, object?> GetAllVariables()
        {
            return _variables;
        }

        // Implementation for output connector values
        public void SetOutputConnectorValue(Guid outputConnectorId, object? value)
        {
            if (outputConnectorId == Guid.Empty)
            {
                throw new ArgumentException("Output connector ID cannot be empty.", nameof(outputConnectorId));
            }
            _outputConnectorValues[outputConnectorId] = value;
        }

        public object? GetOutputConnectorValue(Guid outputConnectorId)
        {
            if (outputConnectorId == Guid.Empty)
            {
                return null; // Or throw?
            }
            _outputConnectorValues.TryGetValue(outputConnectorId, out var value);
            return value;
        }

        public void ClearOutputConnectorValues()
        {
            _outputConnectorValues.Clear();
        }
    }
} 