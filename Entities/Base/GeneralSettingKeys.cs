namespace Entities.Base
{
	/// <summary>
	/// کلیدهای شناخته‌شده <see cref="GeneralSetting"/> با مقدار پیش‌فرض.
	/// اگر ردیفی در دیتابیس نباشد، سرویس تنظیمات از <see cref="Default"/> استفاده می‌کند.
	/// </summary>
	public static class GeneralSettingKeys
	{
		public sealed record GeneralSettingInfo(string Key, string Title, string? Default, bool IsSecret = false, string? Description = null);

		// ---- پیامک ----
		public const string SmsEnabled = "Sms:Enabled";
		public const string SmsProvider = "Sms:Provider";
		public const string SmsIrBaseUrl = "Sms:SmsIr:BaseUrl";
		public const string SmsIrApiKey = "Sms:SmsIr:ApiKey";
		public const string SmsIrLineNumber = "Sms:SmsIr:LineNumber";
		public const string SmsMaxAttempts = "Sms:MaxAttempts";
		public const string SmsRetryDelayMinutes = "Sms:RetryDelayMinutes";
		public const string SmsBatchSize = "Sms:BatchSize";
		public const string SmsDeliveryBatchSize = "Sms:DeliveryBatchSize";
		public const string SmsDeliveryCheckHours = "Sms:DeliveryCheckHours";

		/// <summary>اگر مقدار داشته باشد، همه پیامک‌ها به همین شماره هدایت می‌شوند (فقط برای تست؛ خالی = خاموش).</summary>
		public const string SmsTestRedirectMobile = "Sms:TestRedirectMobile";

		// ---- پیامک لینک مدارک سانتریفیوژ (جایگزین کار SQL راهکاران، نوع 2924 HTS) ----
		public const string SmsCentrifugeDocsEnabled = "Sms:CentrifugeDocs:Enabled";
		public const string SmsCentrifugeDocsStartFrom = "Sms:CentrifugeDocs:StartFrom";
		public const string SmsCentrifugeDocsMaxAgeDays = "Sms:CentrifugeDocs:MaxAgeDays";

		// ---- پیامک «خروج از کارخانه» (جایگزین کار SQL راهکاران، نوع 2746 HTS) ----
		public const string SmsExitRemittanceEnabled = "Sms:ExitRemittance:Enabled";
		public const string SmsExitRemittanceStartFrom = "Sms:ExitRemittance:StartFrom";
		public const string SmsExitRemittanceDelayDays = "Sms:ExitRemittance:DelayDays";

		// ---- گیرنده‌های ثابت پیامک (جایگزین شماره‌های هاردکد سیستم قبلی؛ با ; جدا شوند) ----
		public const string SmsRecipientsPmCentral = "Sms:Recipients:PmRequestCentral";
		public const string SmsRecipientsPmFactoryExtra = "Sms:Recipients:PmRequestFactoryExtra";
		public const string SmsRecipientsProdFinalInspectionStop = "Sms:Recipients:ProdStatus:FinalInspectionStop";
		public const string SmsRecipientsProdProductionAndFinal = "Sms:Recipients:ProdStatus:ProductionAndFinal";
		public const string SmsRecipientsProdTestEnd = "Sms:Recipients:ProdStatus:TestEndAndInspectionStopEnd";
		public const string SmsRecipientsProdInspectionStartPackaging = "Sms:Recipients:ProdStatus:InspectionStartAndPackaging";
		public const string SmsRecipientsQcEmployees = "Sms:Recipients:ProdStatus:QcEmployees";

		public static readonly IReadOnlyList<GeneralSettingInfo> All =
		[
			new(SmsEnabled, "ارسال پیامک فعال باشد", "true",
				Description: "true/false. اگر false باشد پیامک‌ها در صف می‌مانند و ارسال نمی‌شوند."),
			new(SmsProvider, "پنل پیامکی", "SmsIr", Description: "فعلاً فقط SmsIr پشتیبانی می‌شود."),
			new(SmsIrBaseUrl, "آدرس API پنل sms.ir", "https://api.sms.ir/v1"),
			new(SmsIrApiKey, "کلید API پنل sms.ir", null, true),
			new(SmsIrLineNumber, "شماره خط ارسال‌کننده", "30002128765432"),
			new(SmsMaxAttempts, "حداکثر تعداد تلاش ارسال", "3"),
			new(SmsRetryDelayMinutes, "فاصله تلاش مجدد (دقیقه)", "5",
				Description: "فاصله تلاش n-ام برابر n × این مقدار است."),
			new(SmsBatchSize, "تعداد پیامک در هر اجرای job", "100"),
			new(SmsDeliveryBatchSize, "تعداد استعلام دلیوری در هر اجرا", "200"),
			new(SmsDeliveryCheckHours, "مدت پیگیری وضعیت دلیوری (ساعت)", "48",
				Description: "بعد از این مدت از زمان ارسال، استعلام دلیوری متوقف می‌شود."),
			new(SmsTestRedirectMobile, "شماره آزمایشی (هدایت همه پیامک‌ها)", null,
				Description: "فقط برای تست. اگر پر باشد همه پیامک‌ها به این شماره می‌روند و شماره اصلی در «نام گیرنده» ثبت می‌شود. در حالت عادی خالی بماند."),
			new(SmsCentrifugeDocsEnabled, "ارسال پیامک لینک مدارک سانتریفیوژ فعال باشد", "false",
				Description: "true/false. فقط بعد از غیرفعال کردن کار SQL قدیمی راهکاران (ساخت ردیف نوع 2924) روشن شود، وگرنه مشتری دو پیامک می‌گیرد."),
			new(SmsCentrifugeDocsStartFrom, "شروع پیامک لینک مدارک سانتریفیوژ از (تاریخ و ساعت)", null,
				Description: "قالب yyyy-MM-dd HH:mm (مثلاً 2026-10-17 08:00). فقط سفارش‌های ساخت که بعد از این زمان تایید مالی شده‌اند پیامک می‌گیرند؛ جلوی ارسال برای سفارش‌های قدیمی را می‌گیرد."),
			new(SmsCentrifugeDocsMaxAgeDays, "حداکثر روز بعد از تایید برای ارسال لینک مدارک سانتریفیوژ", "3"),
			new(SmsExitRemittanceEnabled, "ارسال پیامک خروج از کارخانه فعال باشد", "false",
				Description: "true/false. فقط بعد از غیرفعال کردن کار SQL قدیمی راهکاران (ساخت ردیف نوع 2746) روشن شود، وگرنه مشتری دو پیامک می‌گیرد."),
			new(SmsExitRemittanceStartFrom, "پیامک خروج از کارخانه: فقط حواله‌های با تاریخ از این روز به بعد", null,
				Description: "قالب yyyy-MM-dd (مثلاً 2026-10-16). جلوی ارسال برای حواله‌های قدیمی را می‌گیرد. اگر خالی یا نامعتبر باشد چیزی ارسال نمی‌شود."),
			new(SmsExitRemittanceDelayDays, "پیامک خروج از کارخانه: چند روز بعد از تاریخ حواله ارسال شود", "3",
				Description: "در راهکاران قدیم: حواله‌ای که بیش از ۲ روز از تاریخش گذشته (از روز سوم به بعد)."),
			new(SmsRecipientsPmCentral, "گیرنده پیامک درخواست PM (دفتر مرکزی)", null),
			new(SmsRecipientsPmFactoryExtra, "گیرندگان ثابت اضافه پیامک درخواست PM (کارخانه)", null),
			new(SmsRecipientsProdFinalInspectionStop, "گیرندگان پیامک وضعیت سفارش ساخت: شروع توقف بازرسی نهایی", null),
			new(SmsRecipientsProdProductionAndFinal, "گیرندگان پیامک وضعیت سفارش ساخت: پایان تولید / شروع تست / توقف تست", null),
			new(SmsRecipientsProdTestEnd, "گیرندگان پیامک وضعیت سفارش ساخت: پایان تست / پایان توقف بازرسی", null),
			new(SmsRecipientsProdInspectionStartPackaging, "گیرندگان پیامک وضعیت سفارش ساخت: شروع بازرسی / پایان بسته‌بندی", null),
			new(SmsRecipientsQcEmployees, "گیرندگان پیامک وضعیت سفارش ساخت: کارکنان کنترل کیفیت (به همه وضعیت‌ها اضافه می‌شود)", null),
		];

		public static string? Default(string key) =>
			All.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))?.Default;
	}
}
