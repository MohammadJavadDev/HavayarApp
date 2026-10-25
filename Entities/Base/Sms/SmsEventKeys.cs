using Entities.Base.Sms.Enums;

namespace Entities.Base.Sms
{
	/// <summary>
	/// فهرست رویدادهای پیامکی سیستم (مهاجرت‌شده از پیامک‌های HTS).
	/// هر پیامک در <see cref="SmsMessage.EventKey"/> یکی از این کلیدها را دارد.
	/// کلید جدید فقط با افزودن یک ردیف به <see cref="All"/> اضافه می‌شود (بدون migration).
	/// </summary>
	public static class SmsEventKeys
	{
		public sealed record SmsEventInfo(string Key, string Title, SmsCategoryEnum Category, bool IsSensitive = false);

		// ---- کد تأیید ----
		public const string HmaSignUpOtp = "Hma.SignUpOtp";
		public const string HmaLoginOtp = "Hma.LoginOtp";
		public const string BaleBotOtp = "BaleBot.Otp";
		public const string ResumeLoginOtp = "Hrm.ResumeLoginOtp";

		// ---- اپلیکیشن موبایل ----
		public const string HmaRegisterDone = "Hma.RegisterDone";
		public const string HmaRegisterRequestAdmin = "Hma.RegisterRequestAdmin";
		public const string HmaPasswordReset = "Hma.PasswordReset";
		public const string HmaPasswordSend = "Hma.PasswordSend";
		public const string HmaServiceRequestReceipt = "Hma.ServiceRequestReceipt";
		public const string HmaPartRequestReceipt = "Hma.PartRequestReceipt";

		// ---- فروش / پس از فروش ----
		public const string SaleOrderShipping = "Sale.OrderShipping";
		public const string SaleProductDocs = "Sale.ProductDocs";
		public const string SaleCentrifugeDocs = "Sale.CentrifugeDocs";
		public const string SaleExitRemittance = "Sale.ExitRemittance";
		public const string SaleInstallContractorReply = "Sale.InstallContractorReply";
		public const string SaleTqNew = "Sale.TqNew";
		public const string SaleTqEffectiveAction = "Sale.TqEffectiveAction";
		public const string SaleTqEquipmentReceived = "Sale.TqEquipmentReceived";

		// ---- CRM ----
		public const string CrmFeedbackReceipt = "Crm.FeedbackReceipt";
		public const string CrmFeedbackNotifyStaff = "Crm.FeedbackNotifyStaff";
		public const string CrmComplaintReceipt = "Crm.ComplaintReceipt";
		public const string CrmComplaintClosed = "Crm.ComplaintClosed";
		public const string CrmComplaintNotifyStaff = "Crm.ComplaintNotifyStaff";
		public const string CrmWebsiteRequestReceipt = "Crm.WebsiteRequestReceipt";
		public const string CrmSurveyThanks = "Crm.SurveyThanks";
		public const string CrmSurveyLink = "Crm.SurveyLink";
		public const string CrmReceptionThanks = "Crm.ReceptionThanks";

		// ---- خدمات داخلی ----
		public const string SrvPmRequest = "Srv.PmRequest";
		public const string SrvPmPeriodic = "Srv.PmPeriodicTechnician";
		public const string SrvCarRequestDriver = "Srv.CarRequestDriver";
		public const string SrvAmbassadorRequest = "Srv.AmbassadorRequest";

		// ---- کیفیت / تولید ----
		public const string QcNcrNew = "Qc.NcrNew";
		public const string PlnProductionOrderStatus = "Pln.ProductionOrderStatus";

		// ---- حقوقی / منابع انسانی ----
		public const string LawImportantDate = "Law.ImportantDateReminder";
		public const string HrmInterviewInvite = "Hrm.InterviewInvitation";

		// ---- جلسات ----
		public const string MtgAnnouncing = "Mtg.Announcing";
		public const string MtgItemReminder = "Mtg.ItemReminder";
		public const string MtgCustom = "Mtg.CustomSms";

		// ---- ارسال دستی ----
		public const string ManualPersonnel = "Manual.Personnel";
		public const string ManualCustomer = "Manual.Customer";
		public const string ManualExternal = "Manual.External";

		public static readonly IReadOnlyList<SmsEventInfo> All =
		[
			new(HmaSignUpOtp, "کد تأیید ثبت‌نام اپ موبایل", SmsCategoryEnum.Otp, true),
			new(HmaLoginOtp, "کد تأیید ورود اپ موبایل", SmsCategoryEnum.Otp, true),
			new(BaleBotOtp, "کد احراز هویت بات بله", SmsCategoryEnum.Otp, true),
			new(ResumeLoginOtp, "کد ورود کارتابل جذب و استخدام", SmsCategoryEnum.Otp, true),

			new(HmaRegisterDone, "اعلام ثبت‌نام موفق اپ موبایل", SmsCategoryEnum.Customer),
			new(HmaRegisterRequestAdmin, "اعلام درخواست ثبت‌نام جدید اپ به مدیر", SmsCategoryEnum.Internal),
			new(HmaPasswordReset, "رمز عبور جدید اپ (فراموشی رمز)", SmsCategoryEnum.Customer, true),
			new(HmaPasswordSend, "ارسال رمز عبور اپ از پنل مدیریت", SmsCategoryEnum.Customer, true),
			new(HmaServiceRequestReceipt, "رسید درخواست پشتیبانی اپ", SmsCategoryEnum.Customer),
			new(HmaPartRequestReceipt, "رسید درخواست مشاوره خرید اپ", SmsCategoryEnum.Customer),

			new(SaleOrderShipping, "اطلاع ارسال سفارش و راهنمای فیلم بسته‌بندی", SmsCategoryEnum.Customer),
			new(SaleProductDocs, "لینک الزامات نصب و نظامنامه گارانتی", SmsCategoryEnum.Customer),
			new(SaleCentrifugeDocs, "لینک دستورالعمل کمپرسور سانتریفیوژ و نظامنامه گارانتی", SmsCategoryEnum.Customer),
			new(SaleExitRemittance, "راه‌اندازی دستگاه پس از حواله خروج", SmsCategoryEnum.Customer),
			new(SaleInstallContractorReply, "پاسخ خودکار معرفی پیمانکار نصب", SmsCategoryEnum.Customer),
			new(SaleTqNew, "اعلام پرسش فنی جدید", SmsCategoryEnum.Internal),
			new(SaleTqEffectiveAction, "اعلام اثربخشی اقدام پرسش فنی", SmsCategoryEnum.Internal),
			new(SaleTqEquipmentReceived, "رسید تجهیزات به کارخانه", SmsCategoryEnum.Internal),

			new(CrmFeedbackReceipt, "رسید انتقاد/پیشنهاد به مشتری", SmsCategoryEnum.Customer),
			new(CrmFeedbackNotifyStaff, "اطلاع انتقاد/پیشنهاد جدید به کارشناس", SmsCategoryEnum.Internal),
			new(CrmComplaintReceipt, "رسید شکایت به مشتری", SmsCategoryEnum.Customer),
			new(CrmComplaintClosed, "اعلام بسته شدن شکایت به مشتری", SmsCategoryEnum.Customer),
			new(CrmComplaintNotifyStaff, "اطلاع شکایت جدید به کارشناس", SmsCategoryEnum.Internal),
			new(CrmWebsiteRequestReceipt, "رسید درخواست/پیام وب‌سایت", SmsCategoryEnum.Customer),
			new(CrmSurveyThanks, "تشکر از مشارکت در رضایت‌سنجی", SmsCategoryEnum.Customer),
			new(CrmSurveyLink, "لینک نظرسنجی", SmsCategoryEnum.Customer),
			new(CrmReceptionThanks, "تشکر از حضور در غرفه/سمینار", SmsCategoryEnum.Customer),

			new(SrvPmRequest, "درخواست PM", SmsCategoryEnum.Internal),
			new(SrvPmPeriodic, "سرویس دوره‌ای به تکنسین", SmsCategoryEnum.Internal),
			new(SrvCarRequestDriver, "درخواست خودرو به راننده", SmsCategoryEnum.Internal),
			new(SrvAmbassadorRequest, "درخواست پیک", SmsCategoryEnum.Internal),

			new(QcNcrNew, "عدم انطباق جدید", SmsCategoryEnum.Internal),
			new(PlnProductionOrderStatus, "تغییر وضعیت سفارش ساخت", SmsCategoryEnum.Internal),

			new(LawImportantDate, "یادآوری تاریخ مهم پرونده حقوقی", SmsCategoryEnum.Reminder),
			new(HrmInterviewInvite, "دعوت به مصاحبه", SmsCategoryEnum.Customer),

			new(MtgAnnouncing, "اعلام/لغو جلسه", SmsCategoryEnum.Internal),
			new(MtgItemReminder, "یادآوری مهلت اجرای بند صورتجلسه", SmsCategoryEnum.Reminder),
			new(MtgCustom, "پیامک دلخواه جلسات", SmsCategoryEnum.Manual),

			new(ManualPersonnel, "ارسال دستی به پرسنل", SmsCategoryEnum.Manual),
			new(ManualCustomer, "ارسال دستی به مشتری", SmsCategoryEnum.Manual),
			new(ManualExternal, "ارسال دستی به شماره خارجی", SmsCategoryEnum.Manual),
		];

		public static SmsEventInfo? Find(string? key) =>
			All.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
	}
}
