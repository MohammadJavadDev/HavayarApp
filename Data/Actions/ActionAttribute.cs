using System;

namespace HavayarApp.Data.Actions
{
    /// <summary>
    /// Attribute used to mark classes, methods, or properties as actions within the application.
    /// Actions are automatically discovered and registered by the ActionRegistry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ActionAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the action.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the action.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the type of the entity this action is associated with.
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// Gets or sets the type of action (Create, Update, Delete, etc.).
        /// </summary>
        public ActionType ActionType { get; set; }

        /// <summary>
        /// Gets or sets the execution order of this action (lower values execute first).
        /// Useful when multiple actions need to run in a specific sequence.
        /// </summary>
        public int ExecutionOrder { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class.
        /// </summary>
        public ActionAttribute()
        {
            IsEnabled = true;
            ActionType = ActionType.Execute;
            ExecutionOrder = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class with a specified name.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        public ActionAttribute(string name)
        {
            Name = name;
            IsEnabled = true;
            ActionType = ActionType.Execute;
            ExecutionOrder = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class with a specified name and description.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="description">The description of the action.</param>
        public ActionAttribute(string name, string description)
        {
            Name = name;
            Description = description;
            IsEnabled = true;
            ActionType = ActionType.Execute;
            ExecutionOrder = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class with a specified name, description, and enabled state.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="description">The description of the action.</param>
        /// <param name="isEnabled">A value indicating whether the action is enabled.</param>
        public ActionAttribute(string name, string description, bool isEnabled)
        {
            Name = name;
            Description = description;
            IsEnabled = isEnabled;
            ActionType = ActionType.Execute;
            ExecutionOrder = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class with entity type and action type.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="entityType">The type of entity this action is associated with.</param>
        /// <param name="actionType">The type of action.</param>
        public ActionAttribute(string name, Type entityType, ActionType actionType)
        {
            Name = name;
            EntityType = entityType;
            ActionType = actionType;
            IsEnabled = true;
            ExecutionOrder = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class with full parameters.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="description">The description of the action.</param>
        /// <param name="entityType">The type of entity this action is associated with.</param>
        /// <param name="actionType">The type of action.</param>
        /// <param name="isEnabled">A value indicating whether the action is enabled.</param>
        /// <param name="executionOrder">The execution order of this action.</param>
        public ActionAttribute(string name, string description, Type entityType, ActionType actionType, bool isEnabled = true, int executionOrder = 0)
        {
            Name = name;
            Description = description;
            EntityType = entityType;
            ActionType = actionType;
            IsEnabled = isEnabled;
            ExecutionOrder = executionOrder;
        }
    }
}
