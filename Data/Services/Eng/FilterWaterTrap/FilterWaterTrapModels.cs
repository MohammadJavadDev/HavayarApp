namespace Data.Services.Eng.FilterWaterTrap;

public interface IFilterWaterTrapService
{
	WaterTrapCalculationResult CalculateWaterTrap(WaterTrapCalculationRequest request, string? licensePath);
	FilterCalculationResult CalculateFilter(FilterCalculationRequest request, string? licensePath);
	HdtCalculationResult CalculateHdt(HdtCalculationRequest request, string? licensePath);
	byte[] GetHdtTemplateBytes();
	string ResolveTemplatePath(string fileName);
}

public sealed class WaterTrapCalculationRequest
{
	public string? Flowrate { get; set; }
	public string? Pressure { get; set; }
}

public sealed class WaterTrapCalculationResult
{
	public string? Model { get; set; }
	public string? A { get; set; }
	public string? B { get; set; }
	public string? C { get; set; }
	public string? D { get; set; }
	public string? Kg { get; set; }
}

public sealed class FilterCalculationRequest
{
	public string? Flowrate { get; set; }
	public string? Pressure { get; set; }
	public string? ParticleClass { get; set; }
	public string? OilClass { get; set; }
}

public sealed class FilterCalculationResult
{
	public string? PreFilterElement { get; set; }
	public string? AfterFilterElement { get; set; }
	public string? PreFilterType { get; set; }
	public string? PreFilterTypeDes { get; set; }
	public string? AfterFilterType { get; set; }
	public string? AfterFilterTypeDes { get; set; }
}

public sealed class HdtCalculationRequest
{
	public string? MinPressure { get; set; }
	public string? AirTemp { get; set; }
	public string? NominalFlow { get; set; }
	public string? Allowance { get; set; }
	public string? DewPoint { get; set; }
}

public sealed class HdtCalculationResult
{
	public string? HdtModel { get; set; }
}
