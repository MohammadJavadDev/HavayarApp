using Common.Attributes;
using Common.Utilities;
using Data.SystemAuth;
using Entities.Base;
using Entities.Base.ImportDefinitions;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Services.ImportDefinitionServices
{
	public class ApiImportExecutor
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNamingPolicy = null,
			WriteIndented = false,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
		};

		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ISdk _sdk;
		private readonly Dictionary<string, object> _systemVariables;

		public ApiImportExecutor(
			IHttpClientFactory httpClientFactory,
			IHttpContextAccessor httpContextAccessor,
			ISdk sdk)
		{
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
			_sdk = sdk;

			_systemVariables = new()
			{
				{ "CurrentUserId", sdk.CurrentUser.Id },
				{ "CurrentDateMiladi", DateTime.Today },
				{ "CurrentDateTimeMiladi", DateTime.Now },
				{ "CurrentDateTimeShamsi", DateTime.Now.ToShamsiDateTime() },
				{ "CurrentDateShamsi", DateTime.Now.ToShamsiDate() },
				{ "CurrentUserName", sdk.CurrentUser.FullName },
				{ SystemVariables.CurrentUserId, sdk.CurrentUser.Id },
				{ SystemVariables.CurrentDate, DateTime.Today },
				{ SystemVariables.CurrentDateTime, DateTime.Now },
				{ SystemVariables.CurrentUserName, sdk.CurrentUser.FullName },
			};
		}

		public async Task<ImportLog> ExecuteImportAsync(
			ImportDefinition definition,
			List<Dictionary<string, string>> excelRows,
			Dictionary<string, string> columnMapping,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(definition.ApiUrl))
				throw new Exception("آدرس API تعریف نشده است.");

			var callMode = definition.ApiCallMode ?? ApiCallMode.PerRow;
			var httpMethod = new HttpMethod((definition.ApiHttpMethod ?? "POST").Trim().ToUpperInvariant());
			var requestUrl = ResolveRequestUrl(definition.ApiUrl, definition.ApiIsInternal);

			var log = new ImportLog
			{
				ImportDefinitionId = definition.Id.Value,
				TotalRows = excelRows.Count,
				CreatedById = _sdk.CurrentUser.Id
			};

			var columns = definition.Columns.JsonDeserialize<ImportDefinitionColumn[]>()
				?? Array.Empty<ImportDefinitionColumn>();

			foreach (var column in columns.Where(c => c.DataType == SystemType.Select
				&& c.OptionSettings?.TypeOption == TypeOptionEnum.System))
			{
				column.OptionSettings.ListOptions = EnumExtensions
					.GetEnumValuesWithDisplayNamesByTypeName(column.OptionSettings.SystemTypeName)
					.Select(c => new SelectOptions { Name = c.Text, Value = c.Value })
					.ToList();
			}

			var client = _httpClientFactory.CreateClient();

			if (callMode == ApiCallMode.Batch)
				await ExecuteBatchAsync(client, definition, excelRows, columnMapping, columns, httpMethod, requestUrl, log, cancellationToken);
			else
				await ExecutePerRowAsync(client, definition, excelRows, columnMapping, columns, httpMethod, requestUrl, log, cancellationToken);

			return log;
		}

		private async Task ExecutePerRowAsync(
			HttpClient client,
			ImportDefinition definition,
			List<Dictionary<string, string>> excelRows,
			Dictionary<string, string> columnMapping,
			ImportDefinitionColumn[] columns,
			HttpMethod httpMethod,
			string requestUrl,
			ImportLog log,
			CancellationToken cancellationToken)
		{
			var details = new List<ImportLogDetail>();

			for (int rowIdx = 0; rowIdx < excelRows.Count; rowIdx++)
			{
				var detail = new ImportLogDetail { RowNumber = rowIdx + 2 };

				try
				{
					var bodyObj = BuildRowObject(excelRows[rowIdx], columnMapping, columns);
					var json = JsonSerializer.Serialize(bodyObj, JsonOptions);

					using var response = await SendAsync(client, httpMethod, requestUrl, json, definition.ApiIsInternal, cancellationToken);
					var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
					detail.ResponseJson = responseBody;

					if (response.IsSuccessStatusCode)
					{
						detail.Success = true;
						log.SuccessRows++;
					}
					else
					{
						detail.Success = false;
						detail.ErrorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
						log.FailedRows++;
					}
				}
				catch (Exception ex)
				{
					detail.Success = false;
					detail.ErrorMessage = ex.Message;
					log.FailedRows++;
				}

				details.Add(detail);
			}

			log.Details = details;
		}

		private async Task ExecuteBatchAsync(
			HttpClient client,
			ImportDefinition definition,
			List<Dictionary<string, string>> excelRows,
			Dictionary<string, string> columnMapping,
			ImportDefinitionColumn[] columns,
			HttpMethod httpMethod,
			string requestUrl,
			ImportLog log,
			CancellationToken cancellationToken)
		{
			var details = new List<ImportLogDetail>();
			var batchBody = new List<Dictionary<string, object?>>();
			var hasBuildError = false;

			for (int rowIdx = 0; rowIdx < excelRows.Count; rowIdx++)
			{
				var detail = new ImportLogDetail { RowNumber = rowIdx + 2 };
				try
				{
					batchBody.Add(BuildRowObject(excelRows[rowIdx], columnMapping, columns));
				}
				catch (Exception ex)
				{
					hasBuildError = true;
					detail.Success = false;
					detail.ErrorMessage = ex.Message;
				}
				details.Add(detail);
			}

			if (hasBuildError)
			{
				foreach (var d in details.Where(x => string.IsNullOrEmpty(x.ErrorMessage)))
				{
					d.Success = false;
					d.ErrorMessage = "به دلیل خطای آماده‌سازی ردیف‌های دیگر، درخواست دسته‌ای ارسال نشد.";
				}

				log.SuccessRows = 0;
				log.FailedRows = details.Count;
				log.Details = details;
				return;
			}

			try
			{
				var json = JsonSerializer.Serialize(batchBody, JsonOptions);
				using var response = await SendAsync(client, httpMethod, requestUrl, json, definition.ApiIsInternal, cancellationToken);
				var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
				log.BatchResponseJson = responseBody;

				if (response.IsSuccessStatusCode)
				{
					foreach (var d in details)
					{
						d.Success = true;
						d.ErrorMessage = null;
					}
					log.SuccessRows = details.Count;
					log.FailedRows = 0;
				}
				else
				{
					var err = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
					foreach (var d in details)
					{
						d.Success = false;
						d.ErrorMessage = err;
					}
					log.SuccessRows = 0;
					log.FailedRows = details.Count;
				}
			}
			catch (Exception ex)
			{
				foreach (var d in details)
				{
					d.Success = false;
					d.ErrorMessage = ex.Message;
				}
				log.SuccessRows = 0;
				log.FailedRows = details.Count;
			}

			log.Details = details;
		}

		private Dictionary<string, object?> BuildRowObject(
			Dictionary<string, string> excelRow,
			Dictionary<string, string> columnMapping,
			ImportDefinitionColumn[] columns)
		{
			var obj = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

			foreach (var col in columns.OrderBy(c => c.SortOrder))
			{
				if (col.IsSystemVariable)
				{
					var key = !string.IsNullOrWhiteSpace(col.SystemVariable)
						? col.SystemVariable
						: col.ColumnName;

					if (_systemVariables.TryGetValue(key, out var sysVal))
						obj[col.ColumnName] = sysVal;
					else if (_systemVariables.TryGetValue(col.ColumnName, out var sysVal2))
						obj[col.ColumnName] = sysVal2;
					else
						obj[col.ColumnName] = null;

					continue;
				}

				var excelHeader = columnMapping.ContainsKey(col.ColumnName)
					? columnMapping[col.ColumnName]
					: col.DisplayName;

				if (excelRow.ContainsKey(excelHeader) && !string.IsNullOrWhiteSpace(excelRow[excelHeader]))
				{
					obj[col.ColumnName] = ImportExecutor.ConvertValueForJson(excelRow[excelHeader], col.DataType, col);
				}
				else if (col.IsRequired)
				{
					throw new Exception($"ستون «{col.DisplayName}» اجباری است و مقدار ندارد.");
				}
				else
				{
					obj[col.ColumnName] = null;
				}
			}

			return obj;
		}

		private async Task<HttpResponseMessage> SendAsync(
			HttpClient client,
			HttpMethod method,
			string url,
			string jsonBody,
			bool isInternal,
			CancellationToken cancellationToken)
		{
			using var request = new HttpRequestMessage(method, url);
			request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			if (isInternal)
				ForwardAuthHeaders(request);

			return await client.SendAsync(request, cancellationToken);
		}

		private void ForwardAuthHeaders(HttpRequestMessage request)
		{
			var httpContext = _httpContextAccessor.HttpContext;
			if (httpContext == null)
				return;

			var incoming = httpContext.Request;

			if (incoming.Headers.TryGetValue("Cookie", out var cookieValues))
			{
				var cookie = cookieValues.ToString();
				if (!string.IsNullOrWhiteSpace(cookie))
					request.Headers.TryAddWithoutValidation("Cookie", cookie);
			}

			if (incoming.Headers.TryGetValue("Authorization", out var authValues))
			{
				var auth = authValues.ToString();
				if (!string.IsNullOrWhiteSpace(auth))
					request.Headers.TryAddWithoutValidation("Authorization", auth);
			}
		}

		private string ResolveRequestUrl(string apiUrl, bool isInternal)
		{
			apiUrl = apiUrl.Trim();

			if (Uri.TryCreate(apiUrl, UriKind.Absolute, out var absolute))
				return absolute.ToString();

			if (!isInternal)
			{
				// خارجی نسبی مجاز نیست مگر absolute باشد
				if (!apiUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
					throw new Exception("برای API خارجی باید آدرس کامل (شامل http/https) وارد شود.");
				return apiUrl;
			}

			var httpContext = _httpContextAccessor.HttpContext
				?? throw new Exception("دسترسی به درخواست جاری برای فراخوانی API داخلی ممکن نیست.");

			var req = httpContext.Request;
			var baseUrl = $"{req.Scheme}://{req.Host}";
			if (!apiUrl.StartsWith('/'))
				apiUrl = "/" + apiUrl;

			return baseUrl + apiUrl;
		}
	}
}
