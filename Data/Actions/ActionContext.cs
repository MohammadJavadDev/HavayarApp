using System;

namespace HavayarApp.Data.Actions
{
    /// <summary>
    /// Represents the context for an action execution.
    /// Contains information about the action, its parameters, and execution state.
    /// </summary>
    public class ActionContext
    {
        /// <summary>
        /// Gets or sets the unique identifier for the action.
        /// </summary>
        public string ActionId { get; set; }

        /// <summary>
        /// Gets or sets the name of the action.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the action was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the action was last executed (UTC).
        /// </summary>
        public DateTime? ExecutedAt { get; set; }

        /// <summary>
        /// Gets or sets the current status of the action.
        /// </summary>
        public ActionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets any error message if the action failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the action data/parameters.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets the result of the action execution.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Initializes a new instance of the ActionContext class.
        /// </summary>
        public ActionContext()
        {
            ActionId = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            Status = ActionStatus.Pending;
        }

        /// <summary>
        /// Initializes a new instance of the ActionContext class with a specific action name.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        public ActionContext(string actionName) : this()
        {
            ActionName = actionName;
        }
    }

    /// <summary>
    /// Enumeration representing the status of an action.
    /// </summary>
    public enum ActionStatus
    {
        /// <summary>
        /// Action is waiting to be executed.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Action is currently being executed.
        /// </summary>
        Executing = 1,

        /// <summary>
        /// Action has been completed successfully.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Action execution failed.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Action was cancelled.
        /// </summary>
        Cancelled = 4
    }
}
