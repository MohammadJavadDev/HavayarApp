namespace Data.Services.Pln;

public interface ITimelyDeliveryReportService
{
	Task<TimelyDeliveryReportResponse> GetReportAsync(
		TimelyDeliveryReportFilter filter,
		CancellationToken cancellationToken = default);

	Task<List<ProductionOrderLookupDto>> GetProductionOrderNumbersAsync(
		CancellationToken cancellationToken = default);
}
