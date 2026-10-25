namespace Data.Services.Dashboard;

public interface IBoardPulseDashboardService
{
	Task<BoardPulseDashboardResponse> GetReportAsync(CancellationToken cancellationToken = default);
}

/// <summary>پاسخ داشبورد نبض هیئت‌مدیره — همهٔ مانده‌ها وضعیت همین لحظه‌اند.</summary>
public sealed class BoardPulseDashboardResponse
{
	/// <summary>زمان محاسبه به شمسی</summary>
	public string AsOfShamsiDateTime { get; set; } = string.Empty;

	/// <summary>برچسب نوع عدد: وضعیت همین لحظه</summary>
	public string SnapshotLabel { get; set; } = "وضعیت همین لحظه";

	public BoardPulseMetricDto OpenProductionItems { get; set; } = new();
	public BoardPulsePurchaseMetricDto OpenPurchaseRequests { get; set; } = new();
	public BoardPulseMetricDto EdmsProjectsInProcess { get; set; } = new();
	public BoardPulseMetricDto OpenRepairs { get; set; } = new();
	public BoardPulseMetricDto ServiceRequestsWaitingToDispatch { get; set; } = new();
	public BoardPulseOnTimeDeliveryDto OnTimeDelivery { get; set; } = new();

	/// <summary>برش فعال/متوقف از جمعیت درخواست خرید باز (!IsDeleted)؛ جمع برش‌ها = Count کارت</summary>
	public List<BoardPulseSliceDto> OpenPurchaseSlices { get; set; } = [];
}

public class BoardPulseMetricDto
{
	public string Title { get; set; } = string.Empty;
	public string Definition { get; set; } = string.Empty;
	public int Count { get; set; }
}

public sealed class BoardPulsePurchaseMetricDto : BoardPulseMetricDto
{
	/// <summary>از جمعیت باز (!IsDeleted) با IsStop = true</summary>
	public int StoppedCount { get; set; }
}

public sealed class BoardPulseOnTimeDeliveryDto
{
	public string Title { get; set; } = string.Empty;
	public string Definition { get; set; } = string.Empty;
	public decimal OnTimeDeliveryPercentage { get; set; }

	/// <summary>جملهٔ فیلتر همان پیش‌فرض سرویس تحویل به‌موقع</summary>
	public string FilterSentence { get; set; } = string.Empty;
}

public sealed class BoardPulseSliceDto
{
	public string Label { get; set; } = string.Empty;
	public int Count { get; set; }
}
