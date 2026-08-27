using System.Data;
using System.Globalization;

namespace Data.Services.Eng.CompressorSizing;

internal static class CompressorSizingReportDataSet
{
	public static DataSet Create(CompressorSizingAnalysisResult analysis, CompressorReportType report)
	{
		var dataSet = new DataSet("CompressorSizing");
		dataSet.Tables.Add(BuildHeader(analysis, report));
		dataSet.Tables.Add(BuildRows("Performance", BuildPerformance(analysis)));
		dataSet.Tables.Add(BuildRows("Cooling", BuildCooling(analysis)));
		dataSet.Tables.Add(BuildRows("Inlet", BuildInlet(analysis)));
		dataSet.Tables.Add(BuildChart("ChartPressure", BuildPressurePoints(report)));
		dataSet.Tables.Add(BuildChart("ChartPower", BuildPowerPoints(report)));
		return dataSet;
	}

	public static DataSet CreateSchema()
	{
		return Create(new CompressorSizingAnalysisResult
		{
			ModelName = "HYTB800-3",
			Project = "Sample Project",
			Customer = "Sample Customer",
			Contact = "Contact",
			ReportDate = "2026-01-01",
			InletFlowUnit = "Nm^3/h",
			DischargePressureUnit = "bara",
			OperatingPressureUnit = "bara",
			BarometricPressureUnit = "bara",
			InletPressureDropUnit = "bara",
			InletTemperatureUnit = "C",
			AftercoolerOutletTemperature = 40,
			Gas = "Air"
		}, new CompressorReportType
		{
			modelName = "HYTB800-3",
			Q_u = "Nm^3/h",
			Pd_u = "bara",
			p_IGV30 = SampleCurve(),
			p_IGV70 = SampleCurve(),
			p_IGV100 = SampleCurve(),
			k_IGV30 = SampleCurve(),
			k_IGV70 = SampleCurve(),
			k_IGV100 = SampleCurve(),
			p_SurgeLine = [1000, 7000, 6, 9],
			p_TurnDown = [2000, 5000, 8, 8],
			p_DP = [5600, 8],
			p_Border = [1000, 8000, 5, 10],
			k_Border = [1000, 8000, 200, 800]
		});
	}

	private static double[] SampleCurve()
	{
		var values = new double[20];
		for (var i = 0; i < 10; i++)
		{
			values[i] = 2000 + i * 400;
			values[i + 10] = 9 - i * 0.2;
		}
		return values;
	}

	private static DataTable BuildHeader(CompressorSizingAnalysisResult analysis, CompressorReportType report)
	{
		var table = new DataTable("Header");
		AddColumns(table,
			"Project", "Customer", "Contact", "ReportDate", "ModelName", "Title",
			"PressureChartTitle", "PowerChartTitle", "FlowUnit", "PressureUnit",
			"PressureMinX", "PressureMaxX", "PressureMinY", "PressureMaxY",
			"PowerMinX", "PowerMaxX", "PowerMinY", "PowerMaxY",
			"DesignedTurnDownRatio", "RequiredTurnDownRatio",
			"InletFlow", "InletFlowUnit", "DischargePressure", "DischargePressureUnit",
			"CouplingPowerAtInletFlow", "OperatingPressure", "OperatingPressureUnit",
			"MaximumInletFlow", "CouplingPowerAtMaximumInletFlow", "CompressorDischargeTemperature",
			"IntercoolersConsumption", "IntercoolerPressureDrop", "AftercoolerConsumption",
			"AftercoolerPressureDrop", "OilCoolerConsumption", "OilCoolerPressureDrop",
			"TotalCoolingWaterConsumption", "CoolingWaterMaximumTemperatureRise",
			"BarometricPressure", "BarometricPressureUnit", "InletPressureDrop", "InletPressureDropUnit",
			"InletTemperature", "InletTemperatureUnit", "RelativeHumidity",
			"CoolingWaterInletTemperature", "CoolingWaterTemperatureRise",
			"AftercoolerOutletTemperature", "Gas");

		var row = table.NewRow();
		row["Project"] = analysis.Project ?? string.Empty;
		row["Customer"] = analysis.Customer ?? string.Empty;
		row["Contact"] = analysis.Contact ?? string.Empty;
		row["ReportDate"] = analysis.ReportDate ?? string.Empty;
		row["ModelName"] = analysis.ModelName ?? string.Empty;
		row["Title"] = "DESIGN CONDITION AND PERFORMANCE DATA FOR  "
			+ (analysis.ModelName ?? string.Empty)
			+ "   (3 STAGES)";
		row["PressureChartTitle"] = "Performance Curve for " + (analysis.ModelName ?? string.Empty);
		row["PowerChartTitle"] = "Power Curve for " + (analysis.ModelName ?? string.Empty);
		row["FlowUnit"] = analysis.InletFlowUnit ?? report.Q_u ?? string.Empty;
		row["PressureUnit"] = analysis.DischargePressureUnit ?? report.Pd_u ?? string.Empty;
		row["PressureMinX"] = Format(report.p_Border[0], 4);
		row["PressureMaxX"] = Format(report.p_Border[1], 4);
		row["PressureMinY"] = Format(report.p_Border[2], 4);
		row["PressureMaxY"] = Format(report.p_Border[3], 4);
		row["PowerMinX"] = Format(report.k_Border[0], 4);
		row["PowerMaxX"] = Format(report.k_Border[1], 4);
		row["PowerMinY"] = Format(report.k_Border[2], 4);
		row["PowerMaxY"] = Format(report.k_Border[3], 4);
		row["DesignedTurnDownRatio"] = Format(analysis.DesignedTurnDownRatio, 2);
		row["RequiredTurnDownRatio"] = Format(analysis.RequiredTurnDownRatio, 2);
		row["InletFlow"] = Format(analysis.InletFlow, 2);
		row["InletFlowUnit"] = analysis.InletFlowUnit ?? string.Empty;
		row["DischargePressure"] = Format(analysis.DischargePressure, 2);
		row["DischargePressureUnit"] = analysis.DischargePressureUnit ?? string.Empty;
		row["CouplingPowerAtInletFlow"] = analysis.CouplingPowerAtInletFlow.ToString(CultureInfo.InvariantCulture);
		row["OperatingPressure"] = Format(analysis.OperatingPressure, 2);
		row["OperatingPressureUnit"] = analysis.OperatingPressureUnit ?? string.Empty;
		row["MaximumInletFlow"] = analysis.MaximumInletFlow.ToString(CultureInfo.InvariantCulture);
		row["CouplingPowerAtMaximumInletFlow"] = analysis.CouplingPowerAtMaximumInletFlow.ToString(CultureInfo.InvariantCulture);
		row["CompressorDischargeTemperature"] = analysis.CompressorDischargeTemperature.ToString(CultureInfo.InvariantCulture);
		row["IntercoolersConsumption"] = Format(analysis.IntercoolersConsumption, 2);
		row["IntercoolerPressureDrop"] = Format(analysis.IntercoolerPressureDrop, 2);
		row["AftercoolerConsumption"] = Format(analysis.AftercoolerConsumption, 2);
		row["AftercoolerPressureDrop"] = Format(analysis.AftercoolerPressureDrop, 2);
		row["OilCoolerConsumption"] = Format(analysis.OilCoolerConsumption, 2);
		row["OilCoolerPressureDrop"] = Format(analysis.OilCoolerPressureDrop, 2);
		row["TotalCoolingWaterConsumption"] = Format(analysis.TotalCoolingWaterConsumption, 2);
		row["CoolingWaterMaximumTemperatureRise"] = Format(analysis.CoolingWaterMaximumTemperatureRise, 2);
		row["BarometricPressure"] = Format(analysis.BarometricPressure, 3);
		row["BarometricPressureUnit"] = analysis.BarometricPressureUnit ?? string.Empty;
		row["InletPressureDrop"] = Format(analysis.InletPressureDrop, 2);
		row["InletPressureDropUnit"] = analysis.InletPressureDropUnit ?? string.Empty;
		row["InletTemperature"] = Format(analysis.InletTemperature, 1);
		row["InletTemperatureUnit"] = analysis.InletTemperatureUnit ?? string.Empty;
		row["RelativeHumidity"] = Format(analysis.RelativeHumidity, 0);
		row["CoolingWaterInletTemperature"] = Format(analysis.CoolingWaterInletTemperature, 1);
		row["CoolingWaterTemperatureRise"] = Format(analysis.CoolingWaterTemperatureRise, 1);
		row["AftercoolerOutletTemperature"] = analysis.AftercoolerOutletTemperature.ToString(CultureInfo.InvariantCulture);
		row["Gas"] = string.IsNullOrWhiteSpace(analysis.Gas) ? "Air" : analysis.Gas;
		table.Rows.Add(row);
		return table;
	}

	private static IEnumerable<(string Label, string Unit, string Value)> BuildPerformance(CompressorSizingAnalysisResult a)
	{
		yield return ("Operating Pressure", a.OperatingPressureUnit ?? string.Empty, Format(a.OperatingPressure, 2));
		yield return ("Compressor Discharge Temperature", "C", a.CompressorDischargeTemperature.ToString(CultureInfo.InvariantCulture));
		yield return ("Temperature at Aftercooler Outlet", "C", a.AftercoolerOutletTemperature.ToString(CultureInfo.InvariantCulture));
		yield return ("Maximum Inlet Flow", a.InletFlowUnit ?? string.Empty, a.MaximumInletFlow.ToString(CultureInfo.InvariantCulture));
		yield return ("Coupling Power at Maximum Inlet Flow", "Kw", a.CouplingPowerAtMaximumInletFlow.ToString(CultureInfo.InvariantCulture));
	}

	private static IEnumerable<(string Label, string Unit, string Value)> BuildCooling(CompressorSizingAnalysisResult a)
	{
		yield return ("Intercoolers Consumption", "m^3/h", Format(a.IntercoolersConsumption, 2));
		yield return ("Intercoolers Pressure Drop", "bar", Format(a.IntercoolerPressureDrop, 2));
		yield return ("Aftercooler Consumption", "m^3/h", Format(a.AftercoolerConsumption, 2));
		yield return ("Aftercooler Pressure Drop", "bar", Format(a.AftercoolerPressureDrop, 2));
		yield return ("Oil Cooler Consumption", "m^3/h", Format(a.OilCoolerConsumption, 2));
		yield return ("Oil Cooler Pressure Drop", "bar", Format(a.OilCoolerPressureDrop, 2));
		yield return ("Total Cooling Water Consumption", "m^3/h", Format(a.TotalCoolingWaterConsumption, 2));
		yield return ("Cooling Water Maximum Temperature Rise", "C", Format(a.CoolingWaterMaximumTemperatureRise, 2));
	}

	private static IEnumerable<(string Label, string Unit, string Value)> BuildInlet(CompressorSizingAnalysisResult a)
	{
		yield return ("Gas", "----", string.IsNullOrWhiteSpace(a.Gas) ? "Air" : a.Gas);
		yield return ("Barometric Pressure", a.BarometricPressureUnit ?? string.Empty, Format(a.BarometricPressure, 3));
		yield return ("Inlet Pressure", a.InletPressureDropUnit ?? string.Empty, Format(a.InletPressureDrop, 2));
		yield return ("Inlet Temperature", a.InletTemperatureUnit ?? string.Empty, Format(a.InletTemperature, 1));
		yield return ("Relative Humidity", "%", Format(a.RelativeHumidity, 0));
		yield return ("Cooling Water Inlet Temperature", "C", Format(a.CoolingWaterInletTemperature, 1));
		yield return ("Cooling Water Temperature Rise", "C", Format(a.CoolingWaterTemperatureRise, 1));
	}

	private static IEnumerable<(string Series, double X, double Y)> BuildPressurePoints(CompressorReportType report)
	{
		foreach (var point in Curve(report.p_IGV30, "P_IGV30")) yield return point;
		foreach (var point in Curve(report.p_IGV70, "P_IGV70")) yield return point;
		foreach (var point in Curve(report.p_IGV100, "P_IGV100")) yield return point;
		yield return ("SurgeLine", report.p_SurgeLine[0], report.p_SurgeLine[2]);
		yield return ("SurgeLine", report.p_SurgeLine[1], report.p_SurgeLine[3]);
		yield return ("Turndown", report.p_TurnDown[0], report.p_TurnDown[2]);
		yield return ("Turndown", report.p_TurnDown[1], report.p_TurnDown[3]);
		if (report.p_DP is { Length: >= 2 })
		{
			yield return ("DesignPoint", report.p_DP[0], report.p_DP[1]);
			yield return ("DesignPoint", report.p_DP[0], report.p_DP[1]);
		}
	}

	private static IEnumerable<(string Series, double X, double Y)> BuildPowerPoints(CompressorReportType report)
	{
		foreach (var point in Curve(report.k_IGV30, "K_IGV30")) yield return point;
		foreach (var point in Curve(report.k_IGV70, "K_IGV70")) yield return point;
		foreach (var point in Curve(report.k_IGV100, "K_IGV100")) yield return point;
	}

	private static IEnumerable<(string Series, double X, double Y)> Curve(double[] values, string series)
	{
		if (values == null || values.Length < 20)
			yield break;

		for (var i = 0; i < 10; i++)
			yield return (series, values[i], values[i + 10]);
	}

	private static DataTable BuildRows(string name, IEnumerable<(string Label, string Unit, string Value)> rows)
	{
		var table = new DataTable(name);
		AddColumns(table, "Label", "Unit", "Value");
		foreach (var item in rows)
		{
			var row = table.NewRow();
			row["Label"] = item.Label;
			row["Unit"] = item.Unit;
			row["Value"] = item.Value;
			table.Rows.Add(row);
		}
		return table;
	}

	private static DataTable BuildChart(string name, IEnumerable<(string Series, double X, double Y)> points)
	{
		var table = new DataTable(name);
		table.Columns.Add("Series", typeof(string));
		table.Columns.Add("X", typeof(double));
		table.Columns.Add("Y", typeof(double));
		foreach (var point in points)
		{
			var row = table.NewRow();
			row["Series"] = point.Series;
			row["X"] = point.X;
			row["Y"] = point.Y;
			table.Rows.Add(row);
		}
		return table;
	}

	private static void AddColumns(DataTable table, params string[] names)
	{
		foreach (var name in names)
			table.Columns.Add(name, typeof(string));
	}

	private static string Format(double value, int digits)
	{
		return Math.Round(value, digits).ToString("0." + new string('#', digits), CultureInfo.InvariantCulture);
	}
}
