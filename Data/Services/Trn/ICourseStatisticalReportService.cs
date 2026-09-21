namespace Data.Services.Trn;

public interface ICourseStatisticalReportService
{
	Task<CourseStatisticalReportResponse> GetReportAsync(
		CourseStatisticalReportFilter filter,
		CancellationToken cancellationToken = default);
}
