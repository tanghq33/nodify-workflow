using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading; // Add for CancellationToken
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Tests.Core.Models.Helpers;

namespace Nodify.Workflow.Tests.Core.Execution
{
    /// <summary>
    /// Custom exception for testing asynchronous failures.
    /// </summary>
    public class SimulatedAsyncException : Exception
    {
        public SimulatedAsyncException(string message) : base(message) { }
    }

    /// <summary>
    /// A test node that simulates asynchronous work with configurable delay and outcome.
    /// </summary>
    internal class AsyncTestNode : TestNode
    {
        private readonly TimeSpan _delay;
        private readonly bool _shouldSucceed;
        private readonly bool _throwException; // If true, throws; otherwise, returns Failed result
        private readonly Exception _exceptionToUse; // Ensure it's not null
        private readonly string _nodeIdForContext; // Store the provided ID for context variables

        public AsyncTestNode(
            string id = "async-test-node",
            TimeSpan? delay = null,
            bool shouldSucceed = true,
            bool throwException = false, // Only relevant if shouldSucceed is false
            Exception? exceptionToUse = null) // Optional custom exception
            : base() // Call base parameterless constructor
        {
            _nodeIdForContext = id; // Store for internal use
            _delay = delay ?? TimeSpan.Zero;
            _shouldSucceed = shouldSucceed;
            _throwException = throwException;
            _exceptionToUse = exceptionToUse ?? new SimulatedAsyncException($"Async operation failed in node {_nodeIdForContext}");

            // Add standard input/output connectors directly
            // Using typeof(object) as a generic placeholder data type.
            var inputConnector = new Connector(this, ConnectorDirection.Input, typeof(object));
            var outputConnector = new Connector(this, ConnectorDirection.Output, typeof(object));

            AddInputConnector(inputConnector);
            AddOutputConnector(outputConnector);
        }

        public override async Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
        {
            // Use the stored _nodeIdForContext for context variables
            context.SetVariable($"{_nodeIdForContext}_Started", true); // Mark start for verification

            try
            {
                if (_delay > TimeSpan.Zero)
                {
                    // Pass cancellationToken to Task.Delay
                    await Task.Delay(_delay, cancellationToken);
                }

                // Check for cancellation immediately after delay (or other async work)
                cancellationToken.ThrowIfCancellationRequested();

                context.SetVariable($"{_nodeIdForContext}_FinishedDelay", true); // Mark delay finish

                if (_shouldSucceed)
                {
                    context.SetVariable($"{_nodeIdForContext}_Result", "Success");
                    return NodeExecutionResult.Succeeded();
                }
                else
                {
                    context.SetVariable($"{_nodeIdForContext}_Result", "Failure");
                    if (_throwException)
                    {
                        // Use the non-null _exceptionToUse
                        throw _exceptionToUse;
                    }
                    else
                    {
                        // Use the non-null _exceptionToUse for the Failed result
                        return NodeExecutionResult.Failed(_exceptionToUse);
                    }
                }
            }
            catch (OperationCanceledException) // Catch OCE specifically if Task.Delay throws it
            {
                context.SetVariable($"{_nodeIdForContext}_Result", "Cancelled");
                // Re-throw so the WorkflowRunner can catch it and handle cancellation flow
                throw;
            }
        }
    }
} 