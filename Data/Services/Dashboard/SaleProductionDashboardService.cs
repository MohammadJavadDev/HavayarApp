using Data;
using Data.Services.Pln;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.Dashboard;

/// <summary>
/// داشبورد اجرایی فروش / سفارش ساخت.
/// جمعیت: اقلام آخرین نسخه و حذف‌نشده. مانده‌ها فیلتر تاریخ نمی‌خورند.
/// درصد تحویل به‌موقع از ITimelyDeliveryReportService با فیلتر پیش‌فرض خوانده می‌شود.
/// </summary>
public class SaleProductionDashboardService(
	ApplicationDbContext db,
	ITimelyDeliveryReportService timelyDeliveryReportService) : ISaleProductionDashboardService
{
	private const string StockBadge = "وضعیت همین لحظه";

	private const string FilterDescriptionText =
		"جمعیت: آخرین نسخه اقلام سفارش ساخت حذف‌نشده. " +
		"برش وضعیت: تکمیل‌نشده / تکمیل‌شده / باطل / سایر یا خالی → نامشخص. " +
		"تاریخ توافقی فقط روی تکمیل‌نشده‌ها؛ بدون تاریخ توافقی سطل جدا است و به‌موقع حساب نمی‌شود. " +
		"مانده‌ها فیلتر تاریخ نمی‌خورند. درصد تحویل به‌موقع از گزارش برنامه‌ریزی با فیلتر پیش‌فرض همان گزارش.";

	public async Task<SaleProductionDashboardResponse> GetDataAsync(
		CancellationToken cancellationToken = default)
	{
		var today = DateTime.Today;

		var rows = await db.Set<ProductionOrderItem>().AsNoTracking()
			.Where(poi => poi.IsLatestVersion && !poi.IsDeleted)
			.Select(poi => new ItemRow
			{
				Status = poi.Status,
				AgreedDeliverDate = poi.AgreedDeliverDate
			})
			.ToListAsync(cancellationToken);

		var total = rows.Count;
		var notCompleted = rows.Count(r => r.Status == ProductionOrderItemStatusEnum.NotCompleted);
		var completed = rows.Count(r => r.Status == ProductionOrderItemStatusEnum.Completed);
		var invalid = rows.Count(r => r.Status == ProductionOrderItemStatusEnum.Invalid);
		var unknown = total - notCompleted - completed - invalid;

		var openRows = rows.Where(r => r.Status == ProductionOrderItemStatusEnum.NotCompleted).ToList();
		var pastDue = openRows.Count(r => r.AgreedDeliverDate != null && r.AgreedDeliverDate.Value.Date < today);
		var notYetDue = openRows.Count(r => r.AgreedDeliverDate != null && r.AgreedDeliverDate.Value.Date >= today);
		var noAgreedDate = openRows.Count(r => r.AgreedDeliverDate == null);

		var timely = await timelyDeliveryReportService.GetReportAsync(
			new TimelyDeliveryReportFilter(),
			cancellationToken);

		var statusSlices = new List<SaleProductionSliceDto>
		{
			new() { Key = "notCompleted", Label = "تکمیل‌نشده", Count = notCompleted },
			new() { Key = "completed", Label = "تکمیل‌شده", Count = completed },
			new() { Key = "invalid", Label = "باطل", Count = invalid },
			new() { Key = "unknown", Label = "نامشخص", Count = unknown }
		};

		var agreedDateSlices = new List<SaleProductionSliceDto>
		{
			new() { Key = "pastDue", Label = "سررسید گذشته", Count = pastDue },
			new() { Key = "notYetDue", Label = "هنوز سررسید نشده", Count = notYetDue },
			new() { Key = "noAgreedDate", Label = "بدون تاریخ توافقی", Count = noAgreedDate }
		};

		return new SaleProductionDashboardResponse
		{
			ComputedAtMiladi = DateTime.Now,
			FilterDescription = FilterDescriptionText,
			OnTimeDeliveryPercentage = timely.OnTimeDeliveryPercentage,
			StatusSlices = statusSlices,
			AgreedDateSlices = agreedDateSlices,
			Cards =
			[
				Card("totalLatestAlive", "کل اقلام آخرین نسخه", total, "عدد",
					"تعداد آخرین نسخه اقلام سفارش ساخت حذف‌نشده (وضعیت همین لحظه).",
					null),
				Card("notCompleted", "تکمیل‌نشده", notCompleted, "عدد",
					"آخرین نسخه اقلام سفارش ساخت حذف‌نشده با وضعیت تکمیل‌نشده.",
					"text-warning"),
				Card("completed", "تکمیل‌شده", completed, "عدد",
					"آخرین نسخه اقلام سفارش ساخت حذف‌نشده با وضعیت تکمیل‌شده.",
					"text-success"),
				Card("invalid", "باطل", invalid, "عدد",
					"آخرین نسخه اقلام سفارش ساخت حذف‌نشده با وضعیت باطل.",
					"text-danger"),
				Card("unknown", "نامشخص", unknown, "عدد",
					"آخرین نسخه اقلام سفارش ساخت حذف‌نشده با وضعیت خالی یا خارج از تکمیل‌نشده / تکمیل‌شده / باطل؛ از جمع حذف نمی‌شوند.",
					"text-muted"),
				Card("pastDue", "سررسید گذشته", pastDue, "عدد",
					"از میان تکمیل‌نشده‌ها، تاریخ توافقی قبل از امروز. تاریخ خالی اینجا شمرده نمی‌شود.",
					"text-danger"),
				Card("notYetDue", "هنوز سررسید نشده", notYetDue, "عدد",
					"از میان تکمیل‌نشده‌ها، تاریخ توافقی امروز یا بعد از آن.",
					"text-success"),
				Card("noAgreedDate", "بدون تاریخ توافقی", noAgreedDate, "عدد",
					"از میان تکمیل‌نشده‌ها، تاریخ توافقی خالی است و به‌موقع حساب نمی‌شود.",
					"text-muted"),
				Card("onTimeDeliveryPercentage", "درصد تحویل به‌موقع", timely.OnTimeDeliveryPercentage, "درصد",
					"درصد تحویل به‌موقع از گزارش برنامه‌ریزی با فیلتر پیش‌فرض همان گزارش؛ در این صفحه دوباره محاسبه نمی‌شود.",
					"text-primary",
					badge: "از گزارش برنامه‌ریزی")
			]
		};
	}

	private static SaleProductionMetricCardDto Card(
		string key,
		string title,
		decimal value,
		string unit,
		string definition,
		string? accentClass,
		string badge = StockBadge) => new()
	{
		Key = key,
		Title = title,
		Value = value,
		Unit = unit,
		Definition = definition,
		Badge = badge,
		AccentClass = accentClass
	};

	private sealed class ItemRow
	{
		public ProductionOrderItemStatusEnum Status { get; init; }
		public DateTime? AgreedDeliverDate { get; init; }
	}
}
