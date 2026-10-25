using Common;
using Common.Exceptions;
using Common.Utilities;
using Data.Logging;
using Entities.Base;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text;
using WebFramework.Api;

namespace WebFramework.Middlewares;

public static class CustomExceptionHandlerMiddlewareExtensions
{
	public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
	{
		return builder.UseMiddleware<CustomExceptionHandlerMiddleware>();
	}
}

public class CustomExceptionHandlerMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<CustomExceptionHandlerMiddleware> _logger;
	private readonly IExceptionLogWriter _exceptionLogWriter;

	public CustomExceptionHandlerMiddleware(
		RequestDelegate next,
		ILogger<CustomExceptionHandlerMiddleware> logger,
		IExceptionLogWriter exceptionLogWriter)
	{
		_next = next;
		_logger = logger;
		_exceptionLogWriter = exceptionLogWriter;
	}

	public async Task Invoke(HttpContext context)
	{
		string message = null;
		object errors = null;
		HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;
		ApiResultStatusCode apiStatusCode = ApiResultStatusCode.ServerError;

		try
		{
			await _next(context);
		}
		catch (AppException exception)
		{
			var userInfo = FormatUser(context);
			_logger.LogError(exception, exception.Message + userInfo);
			httpStatusCode = exception.HttpStatusCode;
			apiStatusCode = exception.ApiStatusCode;

			var dic = new Dictionary<string, string>
			{
				["Exception"] = exception.Message,
				["StackTrace"] = exception.StackTrace,
			};
			if (exception.InnerException != null)
			{
				dic.Add("InnerException.Exception", exception.InnerException.Message);
				dic.Add("InnerException.StackTrace", exception.InnerException.StackTrace);
			}
			if (exception.AdditionalData != null)
				dic.Add("AdditionalData", exception.AdditionalData.JsonSerialize());
			message = exception.Message;
			errors = dic;
			await PersistAsync(exception, (int)httpStatusCode);
			await WriteToResponseAsync();
		}
		catch (SecurityTokenExpiredException exception)
		{
			var userInfo = FormatUser(context);
			_logger.LogError(exception, exception.Message + userInfo);
			SetUnAuthorizeResponse(exception);
			await PersistAsync(exception, (int)httpStatusCode);
			await WriteToResponseAsync();
		}
		catch (UnauthorizedAccessException exception)
		{
			var userInfo = FormatUser(context);
			_logger.LogError(exception, exception.Message + userInfo);
			SetUnAuthorizeResponse(exception);
			await PersistAsync(exception, (int)httpStatusCode);
			await WriteToResponseAsync();
		}
		catch (Exception exception)
		{
			var userInfo = FormatUser(context);
			_logger.LogError(exception, DescribeMessage(exception) + userInfo);

			var dic = new Dictionary<string, string>
			{
				["Exception"] = DescribeMessage(exception),
				["StackTrace"] = exception.StackTrace,
			};
			message = DescribeMessage(exception);
			errors = dic;
			await PersistAsync(exception, (int)httpStatusCode);
			await WriteToResponseAsync();
		}

		async Task PersistAsync(Exception exception, int statusCode)
		{
			try
			{
				var (userId, userName, userIp) = ReadUser(context);
				var now = DateTime.Now;
				var log = new ExceptionLog
				{
					CreatedOn = now,
					CreatedOnShamsi = now.ToShamsiDateTime(),
					Message = DescribeMessage(exception),
					ExceptionType = exception.GetType().FullName,
					StackTrace = exception.StackTrace,
					InnerMessage = exception.InnerException?.Message,
					InnerStackTrace = FlattenInner(exception),
					HttpMethod = context.Request.Method,
					RequestPath = context.Request.Path.Value,
					QueryString = context.Request.QueryString.Value,
					TraceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
					RequestId = context.TraceIdentifier,
					StatusCode = statusCode,
					UserId = userId,
					UserName = userName,
					UserIp = userIp
				};
				await _exceptionLogWriter.WriteAsync(log);
			}
			catch (Exception persistException)
			{
				_logger.LogError(persistException, "ذخیره لاگ خطا ناموفق بود.");
			}
		}

		async Task WriteToResponseAsync()
		{
			if (context.Response.HasStarted)
				throw new InvalidOperationException("The response has already started, the http status code middleware will not be executed.");

			var result = new ApiResult(false, apiStatusCode, message, errors);
			var json = result.JsonSerialize();

			context.Response.StatusCode = (int)httpStatusCode;
			context.Response.ContentType = "application/json;charset=utf-8";
			await context.Response.WriteAsync(json);
		}

		void SetUnAuthorizeResponse(Exception exception)
		{
			httpStatusCode = HttpStatusCode.Unauthorized;
			apiStatusCode = ApiResultStatusCode.UnAuthorized;

			var dic = new Dictionary<string, string>
			{
				["Exception"] = exception.Message,
				["StackTrace"] = exception.StackTrace
			};
			if (exception is SecurityTokenExpiredException tokenException)
				dic.Add("Expires", tokenException.Expires.ToString());

			errors = dic;
			message = exception.Message;
		}
	}

	/// <summary>
	/// UseAuthentication بعد از این middleware اجرا می‌شود، پس context.User فقط بعد از _next پر است.
	/// </summary>
	private static string FormatUser(HttpContext context)
	{
		var (userId, userName, userIp) = ReadUser(context);
		return " UserName:" + (string.IsNullOrWhiteSpace(userName) ? "نامشخص" : userName)
			+ " UserId: " + (userId?.ToString() ?? "نامشخص")
			+ " UserIp: " + (userIp ?? "نامشخص");
	}

	private static (long? UserId, string UserName, string UserIp) ReadUser(HttpContext context)
	{
		var principal = context.User;
		var userName = principal?.Identity?.Name;
		if (string.IsNullOrWhiteSpace(userName))
			userName = principal?.FindFirst(ClaimTypes.Name)?.Value;

		var rawId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? principal?.FindFirst("id")?.Value;
		long? userId = long.TryParse(rawId, out var parsed) && parsed > 0 ? parsed : null;

		var userIp = context.Connection.RemoteIpAddress?.ToString();
		return (userId, userName, userIp);
	}

	/// <summary>
	/// پیام قابل‌خواندن؛ برای خطاهای لود فایل/اسمبلی که Message خالی دارند نام فایل را هم می‌آورد.
	/// </summary>
	private static string DescribeMessage(Exception exception)
	{
		var message = exception.Message;
		var fileName = exception switch
		{
			System.IO.FileNotFoundException f => f.FileName,
			System.IO.FileLoadException f => f.FileName,
			TypeLoadException t => t.TypeName,
			_ => null
		};
		if (!string.IsNullOrWhiteSpace(fileName) && (message == null || !message.Contains(fileName)))
			message = $"{message} [{exception.GetType().Name}: {fileName}]";
		return message;
	}

	private static string FlattenInner(Exception exception)
	{
		if (exception.InnerException == null)
			return null;

		var builder = new StringBuilder();
		var inner = exception.InnerException;
		var depth = 0;
		while (inner != null && depth < 8)
		{
			if (builder.Length > 0)
				builder.AppendLine("---");
			builder.Append(inner.GetType().FullName).Append(": ").AppendLine(inner.Message);
			if (!string.IsNullOrWhiteSpace(inner.StackTrace))
				builder.AppendLine(inner.StackTrace);
			inner = inner.InnerException;
			depth++;
		}
		return builder.ToString();
	}
}
