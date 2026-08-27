using System.Data;
using System.Globalization;

namespace Data.Services.Eng.CompressorSizing;

public sealed class CompressorSizingService : ICompressorSizingService
{
	private readonly string[] _dataDirectories;
	private readonly object _gate = new();
	private CompressorProducts? _engine;
	private Exception? _initError;

	public CompressorSizingService(IEnumerable<string> dataDirectories)
	{
		_dataDirectories = dataDirectories
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Select(Path.GetFullPath)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public CompressorSelectionResult SelectByEfficiency(CompressorSizingRequest request)
	{
		return ToSelection(GetEngine().E_base_selection(ToInput(request)));
	}

	public CompressorSelectionResult SelectByReliability(CompressorSizingRequest request)
	{
		return ToSelection(GetEngine().R_base_selection(ToInput(request)));
	}

	public CompressorSizingAnalysisResult Analyze(CompressorSizingRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.modelName))
			throw new InvalidOperationException("Compressor cannot be empty.");

		var input = ToInput(request);
		var report = GetEngine().Analysis(input);
		return MapAnalysis(input, report);
	}

	public DataSet BuildReportDataSet(CompressorSizingRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.modelName))
			throw new InvalidOperationException("Compressor cannot be empty.");

		var input = ToInput(request);
		var report = GetEngine().Analysis(input);
		return CompressorSizingReportDataSet.Create(MapAnalysis(input, report), report);
	}

	public string ResolveReportTemplatePath()
	{
		foreach (var directory in _dataDirectories)
		{
			if (!Directory.Exists(directory))
				continue;

			var path = Path.Combine(directory, "CompressorSizing.mrt");
			if (File.Exists(path))
				return path;
		}

		throw new FileNotFoundException(
			"Compressor sizing Stimulsoft template CompressorSizing.mrt was not found. Checked: "
			+ string.Join("; ", _dataDirectories));
	}

	public string? ResolveWatermarkImagePath()
	{
		foreach (var directory in _dataDirectories)
		{
			if (!Directory.Exists(directory))
				continue;

			var path = Path.Combine(directory, "CompressorSizing-watermark.png");
			if (File.Exists(path))
				return path;
		}

		return null;
	}

	public CompressorSizingDatasheetFile ExportDatasheet(CompressorSizingDatasheetRequest request, string licensePath)
	{
		if (string.IsNullOrWhiteSpace(request.modelName))
			throw new InvalidOperationException("Compressor cannot be empty.");

		var systemUnit = NormalizeSystemUnit(request.SystemUnit);
		var input = ToInput(request);
		var report = GetEngine().Analysis(input);
		var templatePath = ResolveTemplatePath(systemUnit);

		using var output = new MemoryStream();
		CompressorSizingDatasheetExporter.Export(input, report, templatePath, systemUnit, licensePath, output);

		return new CompressorSizingDatasheetFile
		{
			Content = output.ToArray(),
			FileName = CompressorSizingDatasheetExporter.BuildFileName(report.modelName, systemUnit)
		};
	}

	private CompressorProducts GetEngine()
	{
		if (_engine != null)
			return _engine;

		lock (_gate)
		{
			if (_engine != null)
				return _engine;
			if (_initError != null)
				throw _initError;

			try
			{
				var directory = _dataDirectories.FirstOrDefault(ContainsCurveFiles);
				if (directory == null)
				{
					throw new FileNotFoundException(
						"Compressor curve files (compCurves, compPowers, CWdata) were not found. Checked: "
						+ string.Join("; ", _dataDirectories));
				}

				_engine = new CompressorProducts(directory);
				return _engine;
			}
			catch (Exception ex)
			{
				_initError = ex;
				throw;
			}
		}
	}

	private static bool ContainsCurveFiles(string directory)
	{
		return Directory.Exists(directory)
			&& File.Exists(Path.Combine(directory, "compCurves"))
			&& File.Exists(Path.Combine(directory, "compPowers"))
			&& File.Exists(Path.Combine(directory, "CWdata"));
	}

	private static InputDataType ToInput(CompressorSizingRequest request)
	{
		return new InputDataType
		{
			ProjectName = request.ProjectName ?? string.Empty,
			Custormer = request.Custormer ?? string.Empty,
			Contact = request.Contact ?? string.Empty,
			StdPressure = request.StdPressure,
			StdTemperature = request.StdTemperature,
			StdRelativeHumidity = request.StdRelativeHumidity,
			StdPressure_u = request.StdPressure_u ?? "bara",
			StdTemperature_u = request.StdTemperature_u ?? "C",
			Pin = request.Pin,
			dPin = request.dPin,
			Tin = request.Tin,
			Hin = request.Hin,
			Q = request.Q,
			Pd = request.Pd,
			Pin_u = request.Pin_u ?? "bara",
			dPin_u = request.dPin_u ?? "bara",
			Tin_u = request.Tin_u ?? "C",
			Q_u = NormalizeFlowUnit(request.Q_u),
			Pd_u = request.Pd_u ?? "bara",
			Tcw_in = request.Tcw_in,
			Tcw_rise = request.Tcw_rise,
			modelName = request.modelName ?? string.Empty
		};
	}

	private static string NormalizeFlowUnit(string? unit)
	{
		if (string.Equals(unit, "Kg/h", StringComparison.OrdinalIgnoreCase))
			return "kg/h";
		return string.IsNullOrWhiteSpace(unit) ? "Nm^3/h" : unit;
	}

	private static string NormalizeSystemUnit(string? systemUnit)
	{
		if (string.Equals(systemUnit, "SI-bar", StringComparison.OrdinalIgnoreCase))
			return "SI-bar";
		if (string.Equals(systemUnit, "SI-kpa", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(systemUnit, "SI(kpa)", StringComparison.OrdinalIgnoreCase))
			return "SI-kpa";

		throw new InvalidOperationException("Datasheet cannot be empty.");
	}

	private string ResolveTemplatePath(string systemUnit)
	{
		var names = systemUnit == "SI-kpa"
			? new[] { "SI-kpa.xlsx", "SI(kpa).xlsx" }
			: new[] { "SI-bar.xlsx" };

		foreach (var directory in _dataDirectories)
		{
			if (!Directory.Exists(directory))
				continue;

			foreach (var name in names)
			{
				var path = Path.Combine(directory, name);
				if (File.Exists(path))
					return path;
			}
		}

		throw new FileNotFoundException(
			"Datasheet template " + names[0] + " was not found. Checked: "
			+ string.Join("; ", _dataDirectories));
	}

	private static CompressorSelectionResult ToSelection(string selected)
	{
		var isModel = CompressorSizingService.IsKnownModel(selected);
		return new CompressorSelectionResult
		{
			Success = isModel,
			ModelName = isModel ? selected : null,
			Message = isModel ? selected : selected
		};
	}

	private static bool IsKnownModel(string? name)
	{
		return !string.IsNullOrWhiteSpace(name)
			&& name.StartsWith("HYT", StringComparison.OrdinalIgnoreCase)
			&& !name.Contains(' ');
	}

	private static CompressorSizingAnalysisResult MapAnalysis(InputDataType input, CompressorReportType report)
	{
		return new CompressorSizingAnalysisResult
		{
			ModelName = report.modelName,
			Project = input.ProjectName,
			Customer = input.Custormer,
			Contact = input.Contact,
			ReportDate = FormatShamsiDayMonthYear(DateTime.Now),
			DesignedTurnDownRatio = report.DTR,
			RequiredTurnDownRatio = report.RTR,
			InletFlow = input.Q,
			InletFlowUnit = input.Q_u,
			DischargePressure = Math.Round(input.Pd, 2),
			DischargePressureUnit = input.Pd_u,
			CouplingPowerAtInletFlow = Convert.ToInt32(report.CPF),
			OperatingPressure = Math.Round(input.Pd, 2),
			OperatingPressureUnit = input.Pd_u,
			MaximumInletFlow = Convert.ToInt32(report.MaxF),
			CouplingPowerAtMaximumInletFlow = Convert.ToInt32(report.MaxP),
			CompressorDischargeTemperature = Convert.ToInt32(report.Te3),
			IntercoolersConsumption = Math.Round(report.mic, 2),
			IntercoolerPressureDrop = Math.Round(report.dp_mic, 2),
			AftercoolerConsumption = Math.Round(report.mac, 2),
			AftercoolerPressureDrop = Math.Round(report.dp_mac, 2),
			OilCoolerConsumption = Math.Round(report.moc, 2),
			OilCoolerPressureDrop = Math.Round(report.dp_moc, 2),
			TotalCoolingWaterConsumption = Math.Round(report.mtot, 2),
			CoolingWaterMaximumTemperatureRise = Math.Round(report.wTrise, 2),
			BarometricPressure = Math.Round(input.Pin, 3),
			BarometricPressureUnit = input.Pin_u,
			InletPressureDrop = Math.Round(input.dPin, 2),
			InletPressureDropUnit = input.dPin_u,
			InletTemperature = Math.Round(input.Tin, 1),
			InletTemperatureUnit = input.Tin_u,
			RelativeHumidity = input.Hin,
			CoolingWaterInletTemperature = input.Tcw_in,
			CoolingWaterTemperatureRise = input.Tcw_rise,
			AftercoolerOutletTemperature = 40,
			Gas = "Air"
		};
	}

	private static string FormatShamsiDayMonthYear(DateTime date)
	{
		var calendar = new PersianCalendar();
		return $"{calendar.GetDayOfMonth(date):D2}/{calendar.GetMonth(date):D2}/{calendar.GetYear(date)}";
	}
}
