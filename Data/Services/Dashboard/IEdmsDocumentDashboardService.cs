namespace Data.Services.Dashboard;

public interface IEdmsDocumentDashboardService
{
	Task<EdmsDocumentDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
}

/// <summary>پاسخ داشبورد مدارک مهندسی — فقط مانده (وضعیت همین لحظه).</summary>
public class EdmsDocumentDashboardResponse
{
	/// <summary>زمان محاسبه به شمسی</summary>
	public string AsOfShamsiDateTime { get; set; } = string.Empty;

	/// <summary>برچسب نوع عدد — همیشه «وضعیت همین لحظه»</summary>
	public string MetricKindLabel { get; set; } = "وضعیت همین لحظه";

	/// <summary>جملهٔ فیلتر ثابت برای نمایش زیر صفحه</summary>
	public string FilterDescription { get; set; } = string.Empty;

	public int TotalDocuments { get; set; }
	public int TotalProjects { get; set; }
	public int HoldDocuments { get; set; }
	public int InProcessProjects { get; set; }

	public List<EdmsDashboardCardDto> Cards { get; set; } = [];

	/// <summary>برش‌های نمودار وضعیت مدرک (بالاترین‌ها + سایر؛ جمع = TotalDocuments)</summary>
	public List<EdmsStatusSliceDto> DocumentStatusSlices { get; set; } = [];

	/// <summary>برش‌های نمودار وضعیت پروژه (جمع = TotalProjects)</summary>
	public List<EdmsStatusSliceDto> ProjectStatusSlices { get; set; } = [];
}

public class EdmsDashboardCardDto
{
	public string Key { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public int Value { get; set; }
	/// <summary>جملهٔ تعریف فارسی که زیر کارت نشان داده می‌شود</summary>
	public string Definition { get; set; } = string.Empty;
	public string MetricKindLabel { get; set; } = "وضعیت همین لحظه";
}

public class EdmsStatusSliceDto
{
	public string Key { get; set; } = string.Empty;
	public string Label { get; set; } = string.Empty;
	public int Count { get; set; }
}
