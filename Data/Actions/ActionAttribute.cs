using System;

namespace HavayarApp.Data.Actions
{
    /// <summary>
    /// Attribute used to mark classes or methods as actions within the application.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ActionAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the action.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the description of the action.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets a value indicating whether the action is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class.
        /// </summary>
        public ActionAttribute()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class with a specified name.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        public ActionAttribute(string name)
        {
            Name = name;
            IsEnabled = true;
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
        }
    }
}
