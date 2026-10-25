namespace Data.Services.Dashboard;

public interface IAfterSalesExecutiveDashboardService
{
	Task<AfterSalesExecutiveDashboardResponse> GetDataAsync(
		AfterSalesExecutiveDashboardFilter filter,
		CancellationToken cancellationToken = default);
}

public class AfterSalesExecutiveDashboardFilter
{
	/// <summary>بازهٔ روند «ثبت‌شده» — روی کارت‌های مانده اثر ندارد.</summary>
	public string? FromDateShamsi { get; set; }

	public string? ToDateShamsi { get; set; }
}

public class AfterSalesExecutiveDashboardResponse
{
	public string ComputedAtShamsiDateTime { get; set; } = "";

	public string StockLabel { get; set; } = "وضعیت همین لحظه";

	public int WaitingToDispatchCount { get; set; }

	public string WaitingToDispatchDefinition { get; set; } = "";

	public int OpenRepairsCount { get; set; }

	public string OpenRepairsDefinition { get; set; } = "";

	public List<AfterSalesNamedCount> OpenRepairsByStatus { get; set; } = [];

	public bool IncludeCngStatusChart { get; set; }

	public string? CngOmittedReason { get; set; }

	public int OpenCngRepairsCount { get; set; }

	public string OpenCngRepairsDefinition { get; set; } = "";

	public List<AfterSalesNamedCount> OpenCngRepairsByStatus { get; set; } = [];

	public bool IncludeRegisteredTrend { get; set; }

	public string? TrendFromDateShamsi { get; set; }

	public string? TrendToDateShamsi { get; set; }

	public string RegisteredTrendDefinition { get; set; } = "";

	public List<AfterSalesNamedCount> RegisteredByShamsiMonth { get; set; } = [];
}

public class AfterSalesNamedCount
{
	public string Name { get; set; } = "";

	public int Count { get; set; }
}
