namespace Nodify.Workflow.Core.Execution.Context
{
    /// <summary>
    /// Defines the possible states of workflow execution.
    /// </summary>
    public enum ExecutionStatus
    {
        /// <summary>
        /// The workflow execution has not yet started.
        /// </summary>
        NotStarted,

        /// <summary>
        /// The workflow is actively executing.
        /// </summary>
        Running,

        /// <summary>
        /// The workflow execution is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// The workflow execution completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The workflow execution failed due to an error.
        /// </summary>
        Failed,

        /// <summary>
        /// The workflow execution was cancelled.
        /// </summary>
        Cancelled
    }
} 