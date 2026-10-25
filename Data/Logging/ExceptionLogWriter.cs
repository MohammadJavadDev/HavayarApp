using System.Text;
using Entities.Base;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Data.Logging;

public interface IExceptionLogWriter
{
	Task WriteAsync(ExceptionLog log, CancellationToken cancellationToken = default);
}

/// <summary>
/// اول در system.ExceptionLog می‌نویسد. اگر اتصال دیتابیس برقرار نشود، همان رکورد را در فایل روزانه می‌گذارد.
/// اتصال جدا و با timeout کوتاه است تا retry پیش‌فرض EF پاسخ خطا را معطل نکند.
/// </summary>
public sealed class ExceptionLogWriter(
	IConfiguration configuration,
	ILogger<ExceptionLogWriter> logger) : IExceptionLogWriter
{
	private static readonly SemaphoreSlim FileGate = new(1, 1);
	private const int ConnectTimeoutSeconds = 3;
	private const int CommandTimeoutSeconds = 5;

	public async Task WriteAsync(ExceptionLog log, CancellationToken cancellationToken = default)
	{
		try
		{
			await WriteDatabaseAsync(log, cancellationToken);
		}
		catch (Exception dbException)
		{
			try
			{
				await WriteFileAsync(log, dbException, cancellationToken);
			}
			catch (Exception fileException)
			{
				logger.LogError(fileException, "نوشتن فایل لاگ خطا هم ناموفق بود. خطای دیتابیس: {DbError}", dbException.Message);
			}
		}
	}

	private async Task WriteDatabaseAsync(ExceptionLog log, CancellationToken cancellationToken)
	{
		var connectionString = configuration.GetConnectionString("Db");
		if (string.IsNullOrWhiteSpace(connectionString))
			throw new InvalidOperationException("Connection string 'Db' is not configured.");

		var builder = new SqlConnectionStringBuilder(connectionString)
		{
			ConnectTimeout = ConnectTimeoutSeconds
		};

		await using var connection = new SqlConnection(builder.ConnectionString);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandTimeout = CommandTimeoutSeconds;
		command.CommandText = """
			INSERT INTO [system].[ExceptionLog]
			(
				[CreatedOn], [CreatedOnShamsi], [Message], [ExceptionType], [StackTrace],
				[InnerMessage], [InnerStackTrace], [HttpMethod], [RequestPath], [QueryString],
				[TraceId], [RequestId], [StatusCode], [UserId], [UserName], [UserIp]
			)
			VALUES
			(
				@CreatedOn, @CreatedOnShamsi, @Message, @ExceptionType, @StackTrace,
				@InnerMessage, @InnerStackTrace, @HttpMethod, @RequestPath, @QueryString,
				@TraceId, @RequestId, @StatusCode, @UserId, @UserName, @UserIp
			);
			""";

		Add(command, "@CreatedOn", log.CreatedOn);
		Add(command, "@CreatedOnShamsi", Cut(log.CreatedOnShamsi, 30));
		Add(command, "@Message", Cut(log.Message, 8000));
		Add(command, "@ExceptionType", Cut(log.ExceptionType, 500));
		Add(command, "@StackTrace", Cut(log.StackTrace, 16000));
		Add(command, "@InnerMessage", Cut(log.InnerMessage, 8000));
		Add(command, "@InnerStackTrace", Cut(log.InnerStackTrace, 16000));
		Add(command, "@HttpMethod", Cut(log.HttpMethod, 16));
		Add(command, "@RequestPath", Cut(log.RequestPath, 1000));
		Add(command, "@QueryString", Cut(log.QueryString, 2000));
		Add(command, "@TraceId", Cut(log.TraceId, 64));
		Add(command, "@RequestId", Cut(log.RequestId, 64));
		Add(command, "@StatusCode", log.StatusCode);
		Add(command, "@UserId", log.UserId);
		Add(command, "@UserName", Cut(log.UserName, 256));
		Add(command, "@UserIp", Cut(log.UserIp, 64));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	private async Task WriteFileAsync(ExceptionLog log, Exception dbException, CancellationToken cancellationToken)
	{
		var directory = ResolveLogDirectory();
		Directory.CreateDirectory(directory);

		var path = Path.Combine(directory, $"exceptions-{log.CreatedOn:yyyyMMdd}.log");
		var text = BuildFileEntry(log, dbException);

		await FileGate.WaitAsync(cancellationToken);
		try
		{
			await File.AppendAllTextAsync(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), cancellationToken);
		}
		finally
		{
			FileGate.Release();
		}
	}

	private string ResolveLogDirectory()
	{
		var configured = configuration["ExceptionLog:Directory"];
		if (!string.IsNullOrWhiteSpace(configured))
			return Path.IsPathRooted(configured)
				? configured
				: Path.Combine(AppContext.BaseDirectory, configured);

		var uploads = configuration["Storage:UploadsPath"];
		if (!string.IsNullOrWhiteSpace(uploads))
		{
			var uploadsFull = Path.IsPathRooted(uploads)
				? uploads
				: Path.Combine(AppContext.BaseDirectory, uploads);
			var parent = Directory.GetParent(uploadsFull)?.FullName;
			if (!string.IsNullOrWhiteSpace(parent))
				return Path.Combine(parent, "logs");
		}

		return Path.Combine(AppContext.BaseDirectory, "Logs");
	}

	private static string BuildFileEntry(ExceptionLog log, Exception dbException)
	{
		var builder = new StringBuilder();
		builder.AppendLine($"===== {log.CreatedOnShamsi} | {log.CreatedOn:yyyy-MM-dd HH:mm:ss} =====");
		builder.Append("Database: unavailable — ").AppendLine(dbException.Message);
		builder.Append("TraceId: ").AppendLine(log.TraceId);
		builder.Append("RequestId: ").AppendLine(log.RequestId);
		builder.Append("Method: ").AppendLine(log.HttpMethod);
		builder.Append("Path: ").AppendLine(log.RequestPath);
		builder.Append("Query: ").AppendLine(log.QueryString);
		builder.Append("Status: ").AppendLine(log.StatusCode?.ToString());
		builder.Append("UserId: ").AppendLine(log.UserId?.ToString() ?? "نامشخص");
		builder.Append("UserName: ").AppendLine(string.IsNullOrWhiteSpace(log.UserName) ? "نامشخص" : log.UserName);
		builder.Append("UserIp: ").AppendLine(log.UserIp ?? "نامشخص");
		builder.Append("Exception: ").AppendLine(log.ExceptionType);
		builder.Append("Message: ").AppendLine(log.Message);
		builder.AppendLine("StackTrace:");
		builder.AppendLine(log.StackTrace);
		if (!string.IsNullOrWhiteSpace(log.InnerMessage) || !string.IsNullOrWhiteSpace(log.InnerStackTrace))
		{
			builder.AppendLine("Inner:");
			builder.AppendLine(log.InnerMessage);
			builder.AppendLine(log.InnerStackTrace);
		}
		builder.AppendLine();
		return builder.ToString();
	}

	private static void Add(SqlCommand command, string name, object? value)
	{
		command.Parameters.AddWithValue(name, value ?? DBNull.Value);
	}

	private static string? Cut(string? value, int max)
	{
		if (string.IsNullOrEmpty(value) || value.Length <= max)
			return value;
		return value[..max];
	}
}
