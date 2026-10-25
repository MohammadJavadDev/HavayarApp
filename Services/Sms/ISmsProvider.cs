namespace Services.Sms
{
	public class SmsProviderResult
	{
		public bool Success { get; init; }

		/// <summary>true یعنی تلاش مجدد فایده ندارد (مثلاً شماره یا متن نامعتبر).</summary>
		public bool IsPermanentFailure { get; init; }

		public string? ProviderMessageId { get; init; }
		public string? StatusCode { get; init; }
		public string? Error { get; init; }
		public int DurationMs { get; init; }
	}

	public enum SmsDeliveryOutcome
	{
		/// <summary>استعلام انجام نشد یا پنل هنوز وضعیتی ندارد؛ بعداً دوباره استعلام می‌شود.</summary>
		Unknown = 0,

		/// <summary>در مسیر (مثلاً پردازش در مخابرات)؛ هنوز نتیجه نهایی نیست.</summary>
		Pending = 1,

		Delivered = 2,

		/// <summary>نتیجه نهایی منفی (نرسیده به گوشی/مخابرات، خطا، لیست سیاه).</summary>
		NotDelivered = 3
	}

	public class SmsDeliveryResult
	{
		/// <summary>true یعنی خود فراخوانی API موفق بود (حتی اگر دلیوری هنوز نامشخص باشد).</summary>
		public bool CallSucceeded { get; init; }

		public SmsDeliveryOutcome Outcome { get; init; }
		public int? StateCode { get; init; }
		public string? StateText { get; init; }
		public DateTime? DeliveredOn { get; init; }
		public string? Error { get; init; }
	}

	/// <summary>
	/// آداپتر پنل پیامکی. تنظیمات هر بار از <c>GeneralSetting</c> خوانده می‌شود، پس تغییر تنظیمات ری‌استارت نمی‌خواهد.
	/// </summary>
	public interface ISmsProvider
	{
		string Name { get; }

		/// <summary>شماره خطی که فعلاً استفاده می‌شود (برای ثبت در لاگ).</summary>
		Task<string?> GetLineNumberAsync(CancellationToken ct = default);

		/// <summary>اگر تنظیمات ناقص باشد متن خطا، وگرنه null.</summary>
		Task<string?> CheckConfigurationAsync(CancellationToken ct = default);

		Task<SmsProviderResult> SendAsync(string mobile, string text, CancellationToken ct = default);

		/// <summary>استعلام وضعیت دلیوری یک پیامک با شناسه‌ی پیام در پنل.</summary>
		Task<SmsDeliveryResult> GetDeliveryAsync(string providerMessageId, CancellationToken ct = default);
	}
}
