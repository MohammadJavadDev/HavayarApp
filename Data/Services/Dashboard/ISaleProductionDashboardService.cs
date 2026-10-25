namespace Data.Services.Dashboard;

public interface ISaleProductionDashboardService
{
	Task<SaleProductionDashboardResponse> GetDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>پاسخ داشبورد فروش / سفارش ساخت (وضعیت همین لحظه).</summary>
public class SaleProductionDashboardResponse
{
	public List<SaleProductionMetricCardDto> Cards { get; set; } = [];

	/// <summary>برش وضعیت اقلام آخرین‌نسخهٔ حذف‌نشده — جمع باید با کارت کل یکی باشد.</summary>
	public List<SaleProductionSliceDto> StatusSlices { get; set; } = [];

	/// <summary>برش تاریخ توافقی فقط روی زیرمجموعهٔ تکمیل‌نشده — جمع باید با کارت تکمیل‌نشده یکی باشد.</summary>
	public List<SaleProductionSliceDto> AgreedDateSlices { get; set; } = [];

	public decimal OnTimeDeliveryPercentage { get; set; }

	public DateTime ComputedAtMiladi { get; set; }

	/// <summary>جملهٔ ثابت فیلتر برای نمایش زیر صفحه.</summary>
	public string FilterDescription { get; set; } = string.Empty;
}

public class SaleProductionMetricCardDto
{
	public string Key { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public decimal Value { get; set; }
	public string Unit { get; set; } = "عدد";
	/// <summary>جملهٔ تعریف فارسی شاخص — زیر کارت نمایش داده می‌شود.</summary>
	public string Definition { get; set; } = string.Empty;
	/// <summary>برچسب نوع عدد؛ مانده‌ها «وضعیت همین لحظه» هستند.</summary>
	public string Badge { get; set; } = "وضعیت همین لحظه";
	public string? AccentClass { get; set; }
}

public class SaleProductionSliceDto
{
	public string Key { get; set; } = string.Empty;
	public string Label { get; set; } = string.Empty;
	public int Count { get; set; }
}
