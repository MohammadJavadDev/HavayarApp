using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Job;

using System.Data;

using System.Text.Json;
using Microsoft.Data.SqlClient; 

public class JobLogger : IJobLogger
{
	private readonly string _connectionString;
	private readonly ILogger<JobLogger> _logger;

	// Thread-safe storage for current history ID
	private readonly AsyncLocal<long?> _currentHistoryId = new AsyncLocal<long?>();

	public JobLogger(IConfiguration configuration, ILogger<JobLogger> logger)
	{
		_connectionString = configuration.GetConnectionString("db");
		_logger = logger;
	}

	/// <summary>
	/// Sets the current job history ID for logging context
	/// </summary>
	public void SetCurrentHistoryId(long historyId)
	{
		_currentHistoryId.Value = historyId;
	}

	/// <summary>
	/// Gets the current job history ID
	/// </summary>
	public long GetCurrentHistoryId()
	{
		return _currentHistoryId.Value ?? 0;
	}

	public async Task LogInfoAsync(string message, long jobHistoryId, CancellationToken cancellationToken = default)
	{
		var actualHistoryId = jobHistoryId > 0 ? jobHistoryId : GetCurrentHistoryId();
		await LogAsync("Info", message, null, null, null, actualHistoryId, cancellationToken);
	}

	public async Task LogInfoAsync(string message, CancellationToken cancellationToken = default)
	{
		var actualHistoryId = GetCurrentHistoryId();
		await LogAsync("Info", message, null, null, null, actualHistoryId, cancellationToken);
	}

	public async Task LogWarningAsync(string message, long jobHistoryId, CancellationToken cancellationToken = default)
	{
		var actualHistoryId = jobHistoryId > 0 ? jobHistoryId : GetCurrentHistoryId();
		await LogAsync("Warning", message, null, null, null, actualHistoryId, cancellationToken);
	}

	public async Task LogErrorAsync(string message, long jobHistoryId, CancellationToken cancellationToken = default)
	{
		var actualHistoryId = jobHistoryId > 0 ? jobHistoryId : GetCurrentHistoryId();
		await LogAsync("Error", message, null, null, null, actualHistoryId, cancellationToken);
	}

	public async Task LogExceptionAsync(Exception exception, long jobHistoryId, CancellationToken cancellationToken = default)
	{
		var actualHistoryId = jobHistoryId > 0 ? jobHistoryId : GetCurrentHistoryId();
		await LogAsync("Error", exception.Message, null, exception.GetType().Name, exception.ToString(), actualHistoryId, cancellationToken);
	}

	public async Task LogExceptionAsync(Exception exception, CancellationToken cancellationToken = default)
	{
		var actualHistoryId = GetCurrentHistoryId();
		await LogAsync("Error", exception.Message, null, exception.GetType().Name, exception.ToString(), actualHistoryId, cancellationToken);
	}

	public async Task LogObjectAsync(object obj, long jobHistoryId, CancellationToken cancellationToken = default)
	{
		try
		{
			var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
			{
				WriteIndented = true,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			});

			var actualHistoryId = jobHistoryId > 0 ? jobHistoryId : GetCurrentHistoryId();
			await LogAsync("Info", "Object logged", json, null, null, actualHistoryId, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to serialize object for logging");
			var actualHistoryId = jobHistoryId > 0 ? jobHistoryId : GetCurrentHistoryId();
			await LogAsync("Error", "Failed to serialize object for logging", null, ex.GetType().Name, ex.ToString(), actualHistoryId, cancellationToken);
		}
	}

	public async Task LogDebugAsync(string message, long jobHistoryId, CancellationToken cancellationToken = default)
	{
		var actualHistoryId = jobHistoryId > 0 ? jobHistoryId : GetCurrentHistoryId();
		await LogAsync("Debug", message, null, null, null, actualHistoryId, cancellationToken);
	}

	public async Task<IEnumerable<JobLog>> GetLogsByJobHistoryIdAsync(long jobHistoryId, CancellationToken cancellationToken = default)
	{
		var logs = new List<JobLog>();

		using (var connection = new SqlConnection(_connectionString))
		{
			await connection.OpenAsync(cancellationToken);

			var sql = @"
                SELECT Id, JobHistoryId, [Timestamp], LogLevel, [Message], Details, ExceptionType, StackTrace
                FROM [system].[JobLog]
                WHERE JobHistoryId = @JobHistoryId
                ORDER BY [Timestamp]";

			using (var command = new SqlCommand(sql, connection))
			{
				command.Parameters.Add(new SqlParameter("@JobHistoryId", SqlDbType.BigInt) { Value = jobHistoryId });

				using (var reader = await command.ExecuteReaderAsync(cancellationToken))
				{
					while (await reader.ReadAsync(cancellationToken))
					{
						logs.Add(new JobLog
						{
							Id = reader.GetInt64(0),
							JobHistoryId = reader.GetInt64(1),
							Timestamp = reader.GetDateTime(2),
							LogLevel = reader.IsDBNull(3) ? null : reader.GetString(3),
							Message = reader.IsDBNull(4) ? null : reader.GetString(4),
							Details = reader.IsDBNull(5) ? null : reader.GetString(5),
							ExceptionType = reader.IsDBNull(6) ? null : reader.GetString(6),
							StackTrace = reader.IsDBNull(7) ? null : reader.GetString(7)
						});
					}
				}
			}
		}

		return logs;
	}

	public async Task<IEnumerable<JobLog>> GetLogsByJobScheduleIdAsync(int jobScheduleId, CancellationToken cancellationToken = default)
	{
		var logs = new List<JobLog>();

		using (var connection = new SqlConnection(_connectionString))
		{
			await connection.OpenAsync(cancellationToken);

			var sql = @"
                SELECT jl.Id, jl.JobHistoryId, jl.[Timestamp], jl.LogLevel, jl.[Message], jl.Details, jl.ExceptionType, jl.StackTrace
                FROM [system].[JobLog] jl
                INNER JOIN [system].[JobHistory] jh ON jl.JobHistoryId = jh.Id
                WHERE jh.ScheduleId = @ScheduleId
                ORDER BY jl.[Timestamp]";

			using (var command = new SqlCommand(sql, connection))
			{
				command.Parameters.Add(new SqlParameter("@ScheduleId", SqlDbType.Int) { Value = jobScheduleId });

				using (var reader = await command.ExecuteReaderAsync(cancellationToken))
				{
					while (await reader.ReadAsync(cancellationToken))
					{
						logs.Add(new JobLog
						{
							Id = reader.GetInt64(0),
							JobHistoryId = reader.GetInt64(1),
							Timestamp = reader.GetDateTime(2),
							LogLevel = reader.IsDBNull(3) ? null : reader.GetString(3),
							Message = reader.IsDBNull(4) ? null : reader.GetString(4),
							Details = reader.IsDBNull(5) ? null : reader.GetString(5),
							ExceptionType = reader.IsDBNull(6) ? null : reader.GetString(6),
							StackTrace = reader.IsDBNull(7) ? null : reader.GetString(7)
						});
					}
				}
			}
		}

		return logs;
	}

	private async Task LogAsync(
	    string logLevel,
	    string message,
	    string details,
	    string exceptionType,
	    string stackTrace,
	    long jobHistoryId,
	    CancellationToken cancellationToken)
	{
		try
		{
			var now = DateTime.Now;

			using (var connection = new SqlConnection(_connectionString))
			{
				await connection.OpenAsync(cancellationToken);

				using (var transaction = connection.BeginTransaction())
				{
					try
					{
						// ---------- INSERT JobLog ----------
						var insertSql = @"
                            INSERT INTO [system].[JobLog]
                            (JobHistoryId, [Timestamp], LogLevel, [Message], Details, ExceptionType, StackTrace)
                            VALUES (@JobHistoryId, @Timestamp, @LogLevel, @Message, @Details, @ExceptionType, @StackTrace)";

						using (var insertCommand = new SqlCommand(insertSql, connection, transaction))
						{
							insertCommand.Parameters.Add(new SqlParameter("@JobHistoryId", SqlDbType.BigInt) { Value = jobHistoryId });
							insertCommand.Parameters.Add(new SqlParameter("@Timestamp", SqlDbType.DateTime) { Value = now });
							insertCommand.Parameters.Add(new SqlParameter("@LogLevel", SqlDbType.NVarChar, 50) { Value = (object)logLevel ?? DBNull.Value });
							insertCommand.Parameters.Add(new SqlParameter("@Message", SqlDbType.NVarChar, -1) { Value = (object)message ?? DBNull.Value });
							insertCommand.Parameters.Add(new SqlParameter("@Details", SqlDbType.NVarChar, -1) { Value = (object)details ?? DBNull.Value });
							insertCommand.Parameters.Add(new SqlParameter("@ExceptionType", SqlDbType.NVarChar, 500) { Value = (object)exceptionType ?? DBNull.Value });
							insertCommand.Parameters.Add(new SqlParameter("@StackTrace", SqlDbType.NVarChar, -1) { Value = (object)stackTrace ?? DBNull.Value });

							await insertCommand.ExecuteNonQueryAsync(cancellationToken);
						}

						// ---------- UPDATE JobHistory.LogOutput ----------
						if (jobHistoryId > 0)
						{
							var updateSql = @"
                                UPDATE [system].[JobHistory]
                                SET LogOutput = ISNULL(LogOutput, '') + @LogEntry
                                WHERE Id = @JobHistoryId";

							var logEntry = $"[{now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {message}{Environment.NewLine}";

							using (var updateCommand = new SqlCommand(updateSql, connection, transaction))
							{
								updateCommand.Parameters.Add(new SqlParameter("@JobHistoryId", SqlDbType.BigInt) { Value = jobHistoryId });
								updateCommand.Parameters.Add(new SqlParameter("@LogEntry", SqlDbType.NVarChar, -1) { Value = logEntry });

								await updateCommand.ExecuteNonQueryAsync(cancellationToken);
							}
						}

						transaction.Commit();
					}
					catch
					{
						transaction.Rollback();
						throw;
					}
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to log job message");

			// Fallback logging
			Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [LOG_ERROR] Failed to log: {ex.Message}");
		}
	}
}