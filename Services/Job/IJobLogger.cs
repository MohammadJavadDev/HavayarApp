using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Job
{
    /// <summary>
    /// Interface for job logging service
    /// </summary>
    public interface IJobLogger
    {
        /// <summary>
        /// Sets the current job history ID for logging context
        /// </summary>
        void SetCurrentHistoryId(long historyId);

        /// <summary>
        /// Sets the current job history and schedule IDs for logging / realtime context
        /// </summary>
        void SetCurrentHistoryId(long historyId, int scheduleId);

        /// <summary>
        /// Gets the current job history ID
        /// </summary>
        long GetCurrentHistoryId();

        /// <summary>
        /// Gets the current job schedule ID
        /// </summary>
        int GetCurrentScheduleId();
        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="jobHistoryId">The job history ID to associate with this log</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task LogInfoAsync(string message, long jobHistoryId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Logs an informational message
		/// </summary>
		/// <param name="message">The message to log</param>
		/// <param name="cancellationToken">Cancellation token</param>
		Task LogInfoAsync(string message, CancellationToken cancellationToken = default);

		/// <summary>
		/// Logs a warning message
		/// </summary>
		/// <param name="message">The message to log</param>
		/// <param name="jobHistoryId">The job history ID to associate with this log</param>
		/// <param name="cancellationToken">Cancellation token</param>
		Task LogWarningAsync(string message, long jobHistoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="jobHistoryId">The job history ID to associate with this log</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task LogErrorAsync(string message, long jobHistoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="jobHistoryId">The job history ID to associate with this log</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task LogExceptionAsync(Exception exception, long jobHistoryId, CancellationToken cancellationToken = default);


		/// <summary>
		/// Logs an exception
		/// </summary>
		/// <param name="exception">The exception to log</param>
		/// <param name="cancellationToken">Cancellation token</param>
		Task LogExceptionAsync(Exception exception, CancellationToken cancellationToken = default);

		/// <summary>
		/// Logs an object as JSON
		/// </summary>
		/// <param name="obj">The object to log</param>
		/// <param name="jobHistoryId">The job history ID to associate with this log</param>
		/// <param name="cancellationToken">Cancellation token</param>
		Task LogObjectAsync(object obj, long jobHistoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="jobHistoryId">The job history ID to associate with this log</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task LogDebugAsync(string message, long jobHistoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all logs for a specific job history
        /// </summary>
        /// <param name="jobHistoryId">The job history ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of job logs</returns>
        Task<IEnumerable<JobLog>> GetLogsByJobHistoryIdAsync(long jobHistoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all logs for a specific job schedule
        /// </summary>
        /// <param name="jobScheduleId">The job schedule ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of job logs</returns>
        Task<IEnumerable<JobLog>> GetLogsByJobScheduleIdAsync(int jobScheduleId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a job log entry
    /// </summary>
    public class JobLog
    {
        public long Id { get; set; }
        public long JobHistoryId { get; set; }
        public DateTime Timestamp { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
    }
}