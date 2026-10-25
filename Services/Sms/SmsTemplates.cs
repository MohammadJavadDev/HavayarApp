namespace Services.Sms
{
	/// <summary>
	/// متن پیامک‌ها؛ عیناً از سیستم قبلی (HTS) منتقل شده. تغییر متن هر پیامک فقط در همین کلاس انجام شود.
	/// جداکننده خط «\n» است و انتهای متن خط خالی ندارد.
	/// </summary>
	public static class SmsTemplates
	{
		private static string S(string? v) => v ?? string.Empty;

		/// <summary>HTS: PmRequestService.SendNotification / PmRequestController.Add</summary>
		public static string PmRequest(string? type, string? requestText, string? createdBy)
		{
			var text = S(requestText);
			var shortText = text.Length > 30 ? text[..30] : text;
			return "درخواست PM"
				+ "\nنوع: " + S(type)
				+ "\nمتن: " + shortText + (text.Length > 25 ? " ..." : "")
				+ "\nاز: " + S(createdBy);
		}

		/// <summary>HTS: CarRequestController (تخصیص راننده)</summary>
		public static string CarRequest(
			long id, string? type, string? passenger, int? countPeople, string? destination,
			string? needDate, string? needTime, string? description, string? supportComment)
			=> "درخواست خودرو"
				+ "\nدرخواست: " + id
				+ "\nنوع: " + S(type)
				+ "\nمسافر: " + S(passenger)
				+ "\nتعداد: " + countPeople
				+ "\nمقصد: " + S(destination)
				+ "\nتاریخ: " + S(needDate)
				+ "\nساعت: " + S(needTime)
				+ "\nتوضیحات: " + S(description)
				+ "\nکامنت پشتیبانی: " + S(supportComment);

		/// <summary>HTS: AmbassadorRequestController (تخصیص پیک)</summary>
		public static string AmbassadorRequest(
			string? type, string? jobType, string? passenger, string? destination,
			string? needDate, string? needTime, string? packageDescription, string? supportComment)
			=> "درخواست پیک"
				+ "\nنوع: " + S(type)
				+ "\nکار: " + S(jobType)
				+ "\nمسافر: " + S(passenger)
				+ "\nمقصد: " + S(destination)
				+ "\nتاریخ: " + S(needDate)
				+ "\nساعت: " + S(needTime)
				+ "\nتوضیحات: " + S(packageDescription)
				+ "\nکامنت پشتیبانی: " + S(supportComment);

		/// <summary>HTS: CustomerSatisfactionServiceSurveyController.GetMessageText (isForSms)</summary>
		public static string SurveyLink(string surveyUrl)
			=> "\"گروه صنعتی هوایار\""
				+ "\nرضایت سنجی از خدمات ارائه شده"
				+ "\nمشتری گرامی"
				+ "\nبا سلام و احترام"
				+ "\nبا توجه به دریافت خدمات از \"گروه صنعتی هوایار\"، خواهشمند است با ثبت نظر خود ما را در بهبود ارائه خدمات یاری فرمایید."
				+ "\nبرای ثبت نظر، خواهشمند است بر روی لینک ذیل کلیک نمایید:"
				+ "\n" + surveyUrl
				+ "\nواحد ارتباط با مشتریان"
				+ "\nتلفن: 41976-021"
				+ "\n09129590195"
				+ "\n09128077599"
				+ "\nCRM@havayar.com";

		/// <summary>HTS: SaleTQManagementController (تایید پرسش فنی). در سیستم جدید شماره نسخه ندارد.</summary>
		public static string TechnicalQueryNew(long id)
			=> "اعلام پرسش فنی جدید"
				+ "\nشماره درخواست:" + id;

		/// <summary>HTS: ProductionOrderItemCommentService.SendSmsNotification</summary>
		public static string ProductionStatus(string? productionOrderNumber, string? status, string? serial, string? customerTitle)
			=> $"سفارش '{S(productionOrderNumber)}' به وضعیت '{S(status)}' تغییر یافت"
				+ "\nسریال : " + S(serial)
				+ "\nمشتری : " + S(customerTitle);

		/// <summary>
		/// کار SQL راهکاران (Gnr_SmsBucketList نوع 2924). در نسخه قدیمی «\n» داخل رشته T-SQL خط جدید نبود و خودِ نویسه‌های
		/// «\» و «n» برای مشتری می‌رفت؛ اینجا خط جدید واقعی است.
		/// </summary>
		public static string CentrifugeDocs(string? company, string link)
			=> "گروه صنعتی هوایار"
				+ "\n\nمشتری محترم، " + S(company)
				+ "\nفایلهای مربوط به دستورالعمل بهره برداری و نگهداری کمپرسورهای سانتریفیوژ و نظامنامه گارانتی را در لینک ذیل مشاهده فرمایید."
				+ "\n\n" + link
				+ "\n\nاز همراهی شما سپاسگزاریم."
				+ "\n\nwww.havayar.com";

		/// <summary>HTS: GeneralHelper.SendFromGnrSmsBucketListForCompressorAndCustomerData</summary>
		public static string OrderShipping(string? customerTitle)
			=> "گروه صنعتی هوایار"
				+ "\n\nمشتری محترم، " + S(customerTitle) + " سفارش شما بزودی ارسال می‌شود."
				+ "\n\nلطفا از باز کردن بسته بندی دستگاه فیلم بگیرید و برای شماره تلفن همراه 09128337433 از طریق ایتا یا ایمیل aftersale@havayar.com ارسال فرمایید."
				+ "\n\nاز همراهی شما سپاسگزاریم."
				+ "\n\nwww.havayar.com";

		/// <summary>HTS: CrmCustomerComplaintService.SendSmsNotification</summary>
		public static string ComplaintNotifyStaff(string? serialNumber)
			=> "باسلام به استحضار می رساند، شکایت جدیدی به شماره سریال " + S(serialNumber) + " در سامانه HTS ثبت گردید";

		/// <summary>HTS: CrmCustomerComplaintController.SendAfterComplaintSms</summary>
		public static string ComplaintReceipt(string? responderName, string? trackingCode)
			=> S(responderName)
				+ "\nبا سلام و احترام"
				+ "\nنارضایتی شما با کد رهگیری " + S(trackingCode) + " دریافت شد. در اسرع وقت پیگیری و نتیجه اعلام می گردد "
				+ "\nواحد ارتباط با مشتریان گروه صنعتی هوایار"
				+ "\n021-41976"
				+ "\n09129590195"
				+ "\n09128077599";

		/// <summary>HTS: CrmCustomerComplaintController.SendCloseComplaintSms</summary>
		public static string ComplaintClosed(string? responderName, string? trackingCode)
			=> S(responderName)
				+ "\nبا سلام و احترام"
				+ "\nنارضایتی شما با کد رهگیری " + S(trackingCode) + ". با موفقیت بسته شد. رضایت شما مشوق ما برای تلاش بیشتر خواهد بود."
				+ "\nواحد ارتباط با مشتریان گروه صنعتی هوایار"
				+ "\n021-41976"
				+ "\n09129590195"
				+ "\n09128077599";

		/// <summary>HTS: GeneralHelper.SendExitRemittanceSmsBucketList</summary>
		public static string ExitRemittance(string? customerTitle)
			=> "گروه صنعتی هوایار"
				+ "\n\nمشتری محترم، " + S(customerTitle) + " خواهشمند است جهت راه اندازی دستگاه، از طریق راههای ارتباطی زیراقدام فرمایید."
				+ "\n\n021-41976"
				+ "\n\nداخلی 1"
				+ "\n\n09128337433"
				+ "\n\naftersale@havayar.com"
				+ "\n\nاز همراهی شما سپاسگزاریم."
				+ "\n\nwww.havayar.com";

		/// <summary>HTS: JudicialFileManagementController.DoOperation (تاریخ مهم پرونده حقوقی)</summary>
		public static string LawImportantDate(string? fileCode, string? subject, string? title, string? dateInText)
			=> "ارسال پیام از هوایار"
				+ "\nکد پرونده:" + S(fileCode)
				+ "\nعنوان پرونده:" + S(subject)
				+ "\nیادآوری:" + S(title) + " درتاریخ " + S(dateInText);
	}
}
