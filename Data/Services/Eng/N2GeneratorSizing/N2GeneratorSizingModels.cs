namespace Data.Services.Eng.N2GeneratorSizing;

public interface IN2GeneratorSizingService
{
	N2GeneratorSizingResult Calculate(N2GeneratorSizingRequest request, string? licensePath);
}

public sealed class N2GeneratorSizingRequest
{
	public string? InletPressure { get; set; }
	public string? InletTemperature { get; set; }
	public string? ProductRange { get; set; }
	public string? RequiredN2Flow { get; set; }
	public string? Purity { get; set; }
	public string? GeneratorsOnDuty { get; set; }
	public string? CmsManufacturer { get; set; }
	public string? AirConsumptionMethod { get; set; }
	public string? N2GeneratorModel { get; set; }
}

public sealed class N2GeneratorSizingResult
{
	public string DeliveryPressure { get; set; } = string.Empty;
	public string RequiredCmsPerGeneratorLit { get; set; } = string.Empty;
	public string RequiredCmsPerGeneratorKg { get; set; } = string.Empty;
	public string N2GeneratorModel { get; set; } = string.Empty;
	public string CapacityRatioOneSizeLess { get; set; } = string.Empty;
	public string CapacityRatioSelected { get; set; } = string.Empty;
	public string RequiredCmsFillingPerGenerator { get; set; } = string.Empty;
	public string TowerNetVolume { get; set; } = string.Empty;
	public string TotalAvailableN2Flow { get; set; } = string.Empty;
	public string DryAirConsumption { get; set; } = string.Empty;
	public string RequiredDryAirReceiverSize { get; set; } = string.Empty;
	public string DryAirReceiverRange { get; set; } = string.Empty;
	public string DryAirReceiverModel { get; set; } = string.Empty;
	public string RequiredN2ReceiverSize { get; set; } = string.Empty;
	public string N2ReceiverRange { get; set; } = string.Empty;
	public string N2ReceiverModel { get; set; } = string.Empty;
}
