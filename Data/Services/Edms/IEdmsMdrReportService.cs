namespace Data.Services.Edms;

public interface IEdmsMdrReportService
{
	Task<MdrReportResponse> GetReportAsync(MdrReportFilter filter, CancellationToken cancellationToken = default);
}
