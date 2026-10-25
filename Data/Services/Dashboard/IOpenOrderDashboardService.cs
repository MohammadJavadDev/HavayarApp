namespace Data.Services.Dashboard;

public interface IOpenOrderDashboardService
{
	Task<OpenOrderDashboardResponse> GetDashboardAsync(
		OpenOrderDashboardFilter? filter = null,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// فیلتر تاریخ فقط برای هم‌شکلی با سایر داشبوردها پذیرفته می‌شود؛
/// همهٔ شاخص‌های این صفحه «وضعیت همین لحظه» هستند و از بازهٔ تاریخ اثر نمی‌پذیرند.
/// </summary>
public class OpenOrderDashboardFilter
{
	public string? FromDateShamsi { get; set; }
	public string? ToDateShamsi { get; set; }
}

public class OpenOrderDashboardMetricDto
{
	public string Key { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string Definition { get; set; } = string.Empty;
	public long Count { get; set; }
}

public class OpenOrderDashboardDetailRowDto
{
	public long Id { get; set; }
	public string? PartTitle { get; set; }
	public string? PartCode { get; set; }
	public string? RequestedPersonel { get; set; }
	public string? PurchaseRequestShamsiDate { get; set; }
	public string? NeedDateShamsiDate { get; set; }
	public bool IsStop { get; set; }
	public bool EngineeringAccept { get; set; }
}

public class OpenOrderDashboardResponse
{
	/// <summary>برچسب ثابت مانده — وضعیت همین لحظه</summary>
	public string StockLabel { get; set; } = "وضعیت همین لحظه";

	/// <summary>زمان محاسبه به شمسی</summary>
	public string AsOfShamsiDateTime { get; set; } = string.Empty;

	/// <summary>جملهٔ فیلتر جمعیت برای نمایش زیر صفحه</summary>
	public string PopulationFilterText { get; set; } =
		"جمعیت: درخواست‌های خرید حذف‌نشده";

	public OpenOrderDashboardMetricDto OpenTotal { get; set; } = new();

	public List<OpenOrderDashboardMetricDto> StopSplit { get; set; } = [];
	public List<OpenOrderDashboardMetricDto> EngineeringSplit { get; set; } = [];
	public List<OpenOrderDashboardMetricDto> AgeBuckets { get; set; } = [];
	public List<OpenOrderDashboardMetricDto> NeedDateBuckets { get; set; } = [];

	public List<OpenOrderDashboardDetailRowDto> Details { get; set; } = [];
}
