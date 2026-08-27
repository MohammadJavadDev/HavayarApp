namespace Data.Services.Eng.CompressorSizing;

public class CompressorSizingRequest
{
	public string? ProjectName { get; set; }
	public string? Custormer { get; set; }
	public string? Contact { get; set; }
	public double StdPressure { get; set; }
	public double StdTemperature { get; set; }
	public double StdRelativeHumidity { get; set; }
	public string? StdPressure_u { get; set; }
	public string? StdTemperature_u { get; set; }
	public double Pin { get; set; }
	public double dPin { get; set; }
	public double Tin { get; set; }
	public double Hin { get; set; }
	public double Q { get; set; }
	public double Pd { get; set; }
	public string? Pin_u { get; set; }
	public string? dPin_u { get; set; }
	public string? Tin_u { get; set; }
	public string? Q_u { get; set; }
	public string? Pd_u { get; set; }
	public double Tcw_in { get; set; }
	public double Tcw_rise { get; set; }
	public string? modelName { get; set; }
}

public sealed class CompressorSizingDatasheetRequest : CompressorSizingRequest
{
	public string? SystemUnit { get; set; }
}

public sealed class CompressorSelectionResult
{
	public bool Success { get; set; }
	public string? ModelName { get; set; }
	public string? Message { get; set; }
}

public sealed class CompressorSizingAnalysisResult
{
	public string? ModelName { get; set; }
	public string? Project { get; set; }
	public string? Customer { get; set; }
	public string? Contact { get; set; }
	public string? ReportDate { get; set; }
	public double DesignedTurnDownRatio { get; set; }
	public double RequiredTurnDownRatio { get; set; }
	public double InletFlow { get; set; }
	public string? InletFlowUnit { get; set; }
	public double DischargePressure { get; set; }
	public string? DischargePressureUnit { get; set; }
	public int CouplingPowerAtInletFlow { get; set; }
	public double OperatingPressure { get; set; }
	public string? OperatingPressureUnit { get; set; }
	public int MaximumInletFlow { get; set; }
	public int CouplingPowerAtMaximumInletFlow { get; set; }
	public int CompressorDischargeTemperature { get; set; }
	public double IntercoolersConsumption { get; set; }
	public double IntercoolerPressureDrop { get; set; }
	public double AftercoolerConsumption { get; set; }
	public double AftercoolerPressureDrop { get; set; }
	public double OilCoolerConsumption { get; set; }
	public double OilCoolerPressureDrop { get; set; }
	public double TotalCoolingWaterConsumption { get; set; }
	public double CoolingWaterMaximumTemperatureRise { get; set; }
	public double BarometricPressure { get; set; }
	public string? BarometricPressureUnit { get; set; }
	public double InletPressureDrop { get; set; }
	public string? InletPressureDropUnit { get; set; }
	public double InletTemperature { get; set; }
	public string? InletTemperatureUnit { get; set; }
	public double RelativeHumidity { get; set; }
	public double CoolingWaterInletTemperature { get; set; }
	public double CoolingWaterTemperatureRise { get; set; }
	public int AftercoolerOutletTemperature { get; set; }
	public string? Gas { get; set; }
}

public sealed class CompressorSizingDatasheetFile
{
	public required byte[] Content { get; init; }
	public required string FileName { get; init; }
}
