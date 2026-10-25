using Common.Attributes;
using Data.Contracts;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using Entities.Base.Sms;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using Services.Settings;
using Services.Sms;
using System.Globalization;

namespace App.BackgroundJob.Jobs.Sale
{
	/// <summary>
	/// جایگزین کار SQL راهکاران که ردیف نوع 2924 را در Gnr_SmsBucketList HTS می‌ساخت:
	/// سفارش ساخت تایید مالی‌شده (State=3) که قلمی با کد کالای کمپرسور سانتریفیوژ دارد، یک پیامک «لینک دستورالعمل
	/// بهره‌برداری و نظامنامه گارانتی» به شماره‌ی نماینده مشتری (اعلام نیازمندی) می‌گیرد.
	///
	/// تفاوت‌ها با کار قدیمی (عمدی):
	///  - خط جدید واقعی در متن (قبلاً «\n» نوشتاری برای مشتری می‌رفت)؛
	///  - برای هر سفارش ساخت و هر لینک فقط یک پیامک (قبلاً برای هر قلم یک پیامک تکراری)؛
	///  - فقط موبایل معتبر؛ اگر شماره هنوز ثبت نشده بود تا <c>Sms:CentrifugeDocs:MaxAgeDays</c> روز دوباره تلاش می‌شود؛
	///  - سفارش‌های قدیمی پیامک نمی‌گیرند: فقط تاییدهایی بعد از <c>Sms:CentrifugeDocs:StartFrom</c>.
	/// </summary>
	public class CentrifugeDocsSmsJob(
		IUnitOfWork unitOfWork,
		IGeneralSettingService settings,
		ISmsService sms)
	{
		/// <summary>کد کالا → شماره لینک (app.havayar.com/productsFiles/{n})؛ عیناً از کار SQL قدیمی.</summary>
		private static readonly Dictionary<string, int> LinkByPartCode = BuildLinkMap();

		private static Dictionary<string, int> BuildLinkMap()
		{
			var map = new Dictionary<string, int> { ["1801430001"] = 9, ["1801170047"] = 12 };
			foreach (var code in new[] { "1801170002", "1801170038", "1801170005", "1801170009", "1801170001", "1801170026" }) map[code] = 13;
			foreach (var code in new[] { "1801170015", "1801170039", "1801170061", "1801170051", "1801170057", "1801170043", "1801170050", "1801170048", "1801170016" }) map[code] = 10;
			foreach (var code in new[] { "1801170058", "1801170041", "1801170049", "1801170059" }) map[code] = 11;
			return map;
		}

		[JobHandler(
			"پیامک لینک مدارک سانتریفیوژ",
			"برای سفارش ساخت تایید مالی‌شده با کمپرسور سانتریفیوژ، لینک دستورالعمل و نظامنامه گارانتی را به نماینده مشتری پیامک می‌کند (جایگزین کار SQL راهکاران).")]
		public async Task SendCentrifugeDocsSmsAsync(IJobLogger? jobLogger = null, CancellationToken cancellationToken = default)
		{
			if (!await settings.GetBoolAsync(GeneralSettingKeys.SmsCentrifugeDocsEnabled, false, cancellationToken))
				return;

			var startRaw = await settings.GetAsync(GeneralSettingKeys.SmsCentrifugeDocsStartFrom, cancellationToken);
			if (!DateTime.TryParseExact(startRaw, ["yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"],
				CultureInfo.InvariantCulture, DateTimeStyles.None, out var startFrom))
			{
				await (jobLogger?.LogErrorAsync(
					$"تنظیم «{GeneralSettingKeys.SmsCentrifugeDocsStartFrom}» مقدار معتبر ندارد (قالب yyyy-MM-dd HH:mm)؛ برای جلوگیری از ارسال به سفارش‌های قدیمی هیچ پیامکی ساخته نشد.",
					0, cancellationToken) ?? Task.CompletedTask);
				return;
			}

			var maxAgeDays = Math.Max(1, await settings.GetIntAsync(GeneralSettingKeys.SmsCentrifugeDocsMaxAgeDays, 3, cancellationToken));
			var windowStart = DateTime.Now.AddDays(-maxAgeDays);
			if (startFrom > windowStart)
				windowStart = startFrom;

			// سفارش‌های ساختی که در بازه‌ی مجاز از وضعیت دیگر به «تایید مالی» (3) رفته‌اند (کامنت واقعی تغییر وضعیت).
			var approvedOrderIds = await unitOfWork.Repository<ProductionOrderComment>().TableNoTracking
				.Where(c => c.NewState == ProductionOrderStateEnum.FinancialApproval
					&& c.PreviousState != ProductionOrderStateEnum.FinancialApproval
					&& c.CreatedOnMiladiDateTime >= windowStart)
				.Select(c => c.ProductionOrderId)
				.Distinct()
				.ToListAsync(cancellationToken);
			if (approvedOrderIds.Count == 0)
				return;

			var codes = LinkByPartCode.Keys.ToList();
			var items = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.Where(i => i.ProductionOrderId != null && approvedOrderIds.Contains(i.ProductionOrderId.Value)
					&& i.ProductionOrder!.State == ProductionOrderStateEnum.FinancialApproval
					&& i.Part != null && i.Part.Code != null && codes.Contains(i.Part.Code))
				.Select(i => new
				{
					OrderId = i.ProductionOrderId!.Value,
					PartCode = i.Part!.Code!,
					Phone = i.ProductionOrder!.RequirmentAdvertise != null ? i.ProductionOrder.RequirmentAdvertise.RepresentativeContactNumber : null,
					Company = i.ProductionOrder.RequirmentAdvertise != null && i.ProductionOrder.RequirmentAdvertise.PersonOrCompany != null
						? (i.ProductionOrder.RequirmentAdvertise.PersonOrCompany.CompanyName ?? i.ProductionOrder.RequirmentAdvertise.PersonOrCompany.FullName)
						: null
				})
				.ToListAsync(cancellationToken);

			var queued = 0;
			var skippedNoPhone = 0;
			foreach (var group in items.GroupBy(x => new { x.OrderId, Link = LinkByPartCode[x.PartCode] }))
			{
				cancellationToken.ThrowIfCancellationRequested();

				// یکتایی: سفارش ساخت + شماره لینک (دو قلم با لینک یکسان فقط یک پیامک)
				var sourceType = $"ProductionOrder:{group.Key.Link}";
				if (await sms.ExistsAsync(SmsEventKeys.SaleCentrifugeDocs, sourceType, group.Key.OrderId, cancellationToken))
					continue;

				var first = group.First();
				var mobiles = SmsHelpers.SplitReceivers(first.Phone).Where(x => x.IsValid).Select(x => x.Mobile).ToList();
				if (mobiles.Count == 0)
				{
					skippedNoPhone++;
					continue;
				}

				var body = SmsTemplates.CentrifugeDocs(first.Company, $"https://app.havayar.com/productsFiles/{group.Key.Link}");
				if (await sms.TryEnqueueAsync(SmsEventKeys.SaleCentrifugeDocs, string.Join(";", mobiles), body,
					sourceType, group.Key.OrderId, ct: cancellationToken))
					queued++;
			}

			if (queued > 0 || skippedNoPhone > 0)
				await (jobLogger?.LogInfoAsync(
					$"پیامک لینک مدارک سانتریفیوژ — در صف: {queued}، بدون شماره‌ی نماینده (تلاش مجدد تا پایان بازه): {skippedNoPhone}",
					cancellationToken) ?? Task.CompletedTask);
		}
	}
}
