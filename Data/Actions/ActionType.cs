namespace HavayarApp.Data.Actions
{
    /// <summary>
    /// Enum for different action types in the application.
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// Default or unknown action type.
        /// </summary>
        None = 0,

        /// <summary>
        /// Create action type.
        /// </summary>
        Create = 1,

        /// <summary>
        /// Read or view action type.
        /// </summary>
        Read = 2,

        /// <summary>
        /// Update or modify action type.
        /// </summary>
        Update = 3,

        /// <summary>
        /// Delete action type.
        /// </summary>
        Delete = 4,

        /// <summary>
        /// Execute or run action type.
        /// </summary>
        Execute = 5,

        /// <summary>
        /// Upload action type.
        /// </summary>
        Upload = 6,

        /// <summary>
        /// Download action type.
        /// </summary>
        Download = 7,

        /// <summary>
        /// Export action type.
        /// </summary>
        Export = 8,

        /// <summary>
        /// Import action type.
        /// </summary>
        Import = 9
    }
}
