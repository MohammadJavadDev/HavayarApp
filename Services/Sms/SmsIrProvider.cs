using Entities.Base;
using Services.Settings;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Services.Sms
{
	/// <summary>
	/// پنل sms.ir (API نسخه ۱): POST {BaseUrl}/send/bulk با هدر x-api-key.
	/// برای هر گیرنده یک درخواست جدا ارسال می‌شود تا شناسه پیام هر گیرنده جدا ذخیره شود.
	/// </summary>
	public class SmsIrProvider(IGeneralSettingService settings, IHttpClientFactory httpClientFactory) : ISmsProvider
	{
		public string Name => "SmsIr";

		public Task<string?> GetLineNumberAsync(CancellationToken ct = default)
			=> settings.GetAsync(GeneralSettingKeys.SmsIrLineNumber, ct);

		public async Task<string?> CheckConfigurationAsync(CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(await settings.GetAsync(GeneralSettingKeys.SmsIrApiKey, ct)))
				return $"تنظیم «{GeneralSettingKeys.SmsIrApiKey}» در جدول system.GeneralSetting مقدار ندارد.";

			var line = await settings.GetAsync(GeneralSettingKeys.SmsIrLineNumber, ct);
			if (!long.TryParse(line, out _))
				return $"تنظیم «{GeneralSettingKeys.SmsIrLineNumber}» معتبر نیست.";

			return null;
		}

		public async Task<SmsProviderResult> SendAsync(string mobile, string text, CancellationToken ct = default)
		{
			var stopwatch = Stopwatch.StartNew();
			try
			{
				var baseUrl = (await settings.GetRequiredAsync(GeneralSettingKeys.SmsIrBaseUrl, ct)).TrimEnd('/');
				var apiKey = await settings.GetRequiredAsync(GeneralSettingKeys.SmsIrApiKey, ct);
				var line = long.Parse(await settings.GetRequiredAsync(GeneralSettingKeys.SmsIrLineNumber, ct));

				var payload = JsonSerializer.Serialize(new
				{
					lineNumber = line,
					messageText = text,
					mobiles = new[] { mobile },
					sendDateTime = (long?)null
				});

				using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/send/bulk")
				{
					Content = new StringContent(payload, Encoding.UTF8, "application/json")
				};
				request.Headers.Add("x-api-key", apiKey);
				request.Headers.Add("Accept", "application/json");

				using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
				timeout.CancelAfter(TimeSpan.FromSeconds(30));

				var client = httpClientFactory.CreateClient("SmsIr");
				using var response = await client.SendAsync(request, timeout.Token);
				var body = await response.Content.ReadAsStringAsync(timeout.Token);

				var (apiStatus, apiMessage, messageId) = ParseResponse(body);

				if (response.IsSuccessStatusCode && apiStatus is null or 1)
				{
					return new SmsProviderResult
					{
						Success = true,
						ProviderMessageId = messageId,
						StatusCode = ((int)response.StatusCode).ToString(),
						DurationMs = (int)stopwatch.ElapsedMilliseconds
					};
				}

				// کدهای وضعیت ارسال sms.ir: متن/شماره نامعتبر قطعی است (103 متن خالی، 104 موبایل نادرست،
				// 115 لیست سیاه، 117 متن تأییدنشده)؛ کلید/خط/اعتبار/محدودیت درخواست (10-14، 20، 101، 102، 123) موقتی است
				// چون با اصلاح تنظیمات یا شارژ درست می‌شود. بدون کد API، از HTTP status استفاده می‌کنیم.
				var code = (int)response.StatusCode;
				bool permanent;
				if (apiStatus is not null && apiStatus != 1)
					permanent = apiStatus is 103 or 104 or 115 or 117;
				else
					permanent = !(code is 401 or 403 or 408 or 429 || code >= 500);

				return new SmsProviderResult
				{
					Success = false,
					IsPermanentFailure = permanent,
					StatusCode = apiStatus?.ToString() ?? code.ToString(),
					Error = Truncate($"HTTP {code}: {apiMessage ?? body}"),
					DurationMs = (int)stopwatch.ElapsedMilliseconds
				};
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				return Fail("زمان انتظار پاسخ پنل پیامکی تمام شد.", stopwatch);
			}
			catch (HttpRequestException ex)
			{
				return Fail("خطای شبکه: " + ex.Message, stopwatch);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				return Fail(ex.Message, stopwatch);
			}
		}

		/// <summary>GET {BaseUrl}/send/{messageId}. کدهای deliveryState طبق مستندات sms.ir.</summary>
		public async Task<SmsDeliveryResult> GetDeliveryAsync(string providerMessageId, CancellationToken ct = default)
		{
			try
			{
				var baseUrl = (await settings.GetRequiredAsync(GeneralSettingKeys.SmsIrBaseUrl, ct)).TrimEnd('/');
				var apiKey = await settings.GetRequiredAsync(GeneralSettingKeys.SmsIrApiKey, ct);

				using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/send/{Uri.EscapeDataString(providerMessageId)}");
				request.Headers.Add("x-api-key", apiKey);
				request.Headers.Add("Accept", "application/json");

				using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
				timeout.CancelAfter(TimeSpan.FromSeconds(30));

				var client = httpClientFactory.CreateClient("SmsIr");
				using var response = await client.SendAsync(request, timeout.Token);
				var body = await response.Content.ReadAsStringAsync(timeout.Token);

				if (!response.IsSuccessStatusCode)
					return new SmsDeliveryResult { CallSucceeded = false, Error = Truncate($"HTTP {(int)response.StatusCode}: {body}") };

				using var doc = JsonDocument.Parse(body);
				var root = doc.RootElement;
				if (root.TryGetProperty("status", out var s) && s.TryGetInt32(out var status) && status != 1)
				{
					var msg = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : body;
					return new SmsDeliveryResult { CallSucceeded = false, Error = Truncate($"کد {status}: {msg}") };
				}

				if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
					return new SmsDeliveryResult { CallSucceeded = true, Outcome = SmsDeliveryOutcome.Unknown };

				int? state = data.TryGetProperty("deliveryState", out var ds) && ds.ValueKind == JsonValueKind.Number && ds.TryGetInt32(out var dsi) ? dsi : null;
				DateTime? deliveredOn = data.TryGetProperty("deliveryDateTime", out var dd) && dd.ValueKind == JsonValueKind.Number && dd.TryGetInt64(out var unix)
					? DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime
					: null;

				return new SmsDeliveryResult
				{
					CallSucceeded = true,
					StateCode = state,
					StateText = SmsDeliveryStates.Text(state),
					Outcome = SmsDeliveryStates.ToOutcome(state),
					DeliveredOn = deliveredOn
				};
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				return new SmsDeliveryResult { CallSucceeded = false, Error = "زمان انتظار پاسخ پنل پیامکی تمام شد." };
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				return new SmsDeliveryResult { CallSucceeded = false, Error = Truncate(ex.Message) };
			}
		}

		private static SmsProviderResult Fail(string error, Stopwatch stopwatch) => new()
		{
			Success = false,
			IsPermanentFailure = false,
			Error = Truncate(error),
			DurationMs = (int)stopwatch.ElapsedMilliseconds
		};

		private static (int? Status, string? Message, string? MessageId) ParseResponse(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return (null, null, null);

			try
			{
				using var doc = JsonDocument.Parse(body);
				var root = doc.RootElement;
				if (root.ValueKind != JsonValueKind.Object)
					return (null, null, null);

				int? status = root.TryGetProperty("status", out var s) && s.TryGetInt32(out var si) ? si : null;
				string? message = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;

				string? messageId = null;
				if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
				{
					if (data.TryGetProperty("messageIds", out var ids) && ids.ValueKind == JsonValueKind.Array && ids.GetArrayLength() > 0)
						messageId = ids[0].ToString();
					else if (data.TryGetProperty("packId", out var pack))
						messageId = pack.ToString();
				}

				return (status, message, messageId);
			}
			catch (JsonException)
			{
				return (null, null, null);
			}
		}

		private static string Truncate(string value) => value.Length > 1900 ? value[..1900] : value;
	}
}
