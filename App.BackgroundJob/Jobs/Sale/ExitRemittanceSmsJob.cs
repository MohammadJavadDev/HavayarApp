using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Sale;
using Entities.Base;
using Entities.Base.Sms;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using Services.Settings;
using Services.Sms;
using System.Globalization;

namespace App.BackgroundJob.Jobs.Sale
{
	/// <summary>
	/// جایگزین کار SQL راهکاران که ردیف نوع 2746 را در Gnr_SmsBucketList HTS می‌ساخت (و کار HTS که گیرنده و متن را پر و ارسال می‌کرد):
	/// برای هر «حواله خروج انبار» (LGS3.IssuePermit) که قلم کمپرسور دارد و چند روز از تاریخش گذشته،
	/// سفارش فروش مرتبط (ReferenceRef = Order.HamkaranId) پیدا می‌شود و اگر کالای 1801* داشت،
	/// پیامک «جهت راه‌اندازی دستگاه تماس بگیرید» به نماینده مشتری (یا موبایل شرکت) ارسال می‌شود.
	///
	/// تفاوت‌ها با نسخه قدیمی (عمدی):
	///  - برای هر سفارش فقط یک پیامک (قبلاً برای هر قلم حواله یک پیامک)؛
	///  - یکتایی روی (رویداد، سفارش) در system.SmsMessage (قبلاً فقط روی شناسه قلم و بدون در نظر گرفتن نوع)؛
	///  - فقط حواله‌های با تاریخ از <c>Sms:ExitRemittance:StartFrom</c> به بعد.
	/// </summary>
	public class ExitRemittanceSmsJob(
		RahkaranDbContext rahkaranDb,
		IUnitOfWork unitOfWork,
		IGeneralSettingService settings,
		ISmsService sms,
		ISmsRecipientResolver smsRecipients)
	{
		private const string CompressorNamePrefix = "کمپرسور%";

		[JobHandler(
			"پیامک خروج از کارخانه",
			"برای حواله خروج انبار کمپرسور (راهکاران)، چند روز بعد از تاریخ حواله به نماینده مشتری پیامک «جهت راه‌اندازی دستگاه تماس بگیرید» می‌فرستد (جایگزین کار SQL راهکاران).")]
		public async Task SendExitRemittanceSmsAsync(IJobLogger? jobLogger = null, CancellationToken cancellationToken = default)
		{
			if (!await settings.GetBoolAsync(GeneralSettingKeys.SmsExitRemittanceEnabled, false, cancellationToken))
				return;

			var startRaw = await settings.GetAsync(GeneralSettingKeys.SmsExitRemittanceStartFrom, cancellationToken);
			if (!DateTime.TryParseExact(startRaw, ["yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss"],
				CultureInfo.InvariantCulture, DateTimeStyles.None, out var startFrom))
			{
				await (jobLogger?.LogErrorAsync(
					$"تنظیم «{GeneralSettingKeys.SmsExitRemittanceStartFrom}» مقدار معتبر ندارد (قالب yyyy-MM-dd)؛ برای جلوگیری از ارسال به حواله‌های قدیمی هیچ پیامکی ساخته نشد.",
					0, cancellationToken) ?? Task.CompletedTask);
				return;
			}

			var delayDays = Math.Max(1, await settings.GetIntAsync(GeneralSettingKeys.SmsExitRemittanceDelayDays, 3, cancellationToken));

			// حواله‌های خروج قلم کمپرسور: «بیش از ۲ روز گذشته» در راهکاران یعنی DATEDIFF(DAY, Date, GETDATE()) >= 3
			var permits = await rahkaranDb.Database.SqlQueryRaw<ExitPermitRow>(
				@"SELECT i.IssuePermitItemID AS ItemId, p.ReferenceRef AS ReferenceRef, p.[Date] AS PermitDate
				  FROM LGS3.IssuePermitItem i
				  JOIN LGS3.IssuePermit p ON p.IssuePermitID = i.IssuePermitRef
				  JOIN LGS3.Part pt ON pt.PartID = i.PartRef
				  WHERE pt.Name LIKE @namePrefix
				    AND p.ReferenceRef IS NOT NULL
				    AND p.[Date] >= @startFrom
				    AND DATEDIFF(DAY, p.[Date], GETDATE()) >= @delayDays",
				new SqlParameter("@namePrefix", CompressorNamePrefix),
				new SqlParameter("@startFrom", startFrom.Date),
				new SqlParameter("@delayDays", delayDays))
				.ToListAsync(cancellationToken);

			if (permits.Count == 0)
				return;

			var references = permits.Select(x => x.ReferenceRef!.Value).Distinct().ToList();

			// سفارش فروش مرتبط که کالای 1801* (کمپرسور) دارد.
			var orders = await unitOfWork.Repository<OrderDetail>().TableNoTracking
				.Where(d => d.Sale_Order.HamkaranId != null && references.Contains(d.Sale_Order.HamkaranId.Value)
					&& d.Part != null && d.Part.Code != null && d.Part.Code.StartsWith("1801"))
				.Select(d => new
				{
					OrderId = d.Sale_Order.Id,
					HamkaranId = d.Sale_Order.HamkaranId,
					CustomerTitle = d.Sale_Order.Customer.Party != null ? d.Sale_Order.Customer.Party.FullName : null,
					Mobile = d.Sale_Order.Customer.Party != null ? d.Sale_Order.Customer.Party.Mobile : null
				})
				.Distinct()
				.ToListAsync(cancellationToken);

			var queued = 0;
			var noPhone = 0;
			foreach (var order in orders)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (order.OrderId is null or 0)
					continue;

				if (await sms.ExistsAsync(SmsEventKeys.SaleExitRemittance, nameof(Order), order.OrderId.Value, cancellationToken))
					continue;

				// معادل HTS: ISNULL(CustomerAgentPhone, موبایل شرکت)
				var agentMobile = await smsRecipients.GetCustomerAgentMobileAsync(order.OrderId, cancellationToken);
				var mobile = !string.IsNullOrWhiteSpace(agentMobile) ? agentMobile : order.Mobile;
				if (string.IsNullOrWhiteSpace(mobile))
				{
					noPhone++;
					continue;
				}

				if (await sms.TryEnqueueAsync(SmsEventKeys.SaleExitRemittance, mobile,
					SmsTemplates.ExitRemittance(order.CustomerTitle), nameof(Order), order.OrderId, ct: cancellationToken))
					queued++;
			}

			if (queued > 0 || noPhone > 0)
				await (jobLogger?.LogInfoAsync(
					$"پیامک خروج از کارخانه — در صف: {queued}، بدون شماره (فردا دوباره بررسی می‌شود): {noPhone}",
					cancellationToken) ?? Task.CompletedTask);
		}

		public class ExitPermitRow
		{
			public long ItemId { get; set; }
			public long? ReferenceRef { get; set; }
			public DateTime PermitDate { get; set; }
		}
	}
}
