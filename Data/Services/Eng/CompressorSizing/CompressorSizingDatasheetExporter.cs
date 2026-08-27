using Aspose.Cells;
using System.Globalization;

namespace Data.Services.Eng.CompressorSizing;

internal static class CompressorSizingDatasheetExporter
{
	public static string BuildFileName(string modelName, string systemUnit)
	{
		var stamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		var model = string.IsNullOrWhiteSpace(modelName) ? "Compressor" : modelName;
		return $"{model}_{systemUnit}_{stamp}.xlsx";
	}

	public static void Export(
		InputDataType input,
		CompressorReportType report,
		string templatePath,
		string systemUnit,
		string licensePath,
		Stream outputStream)
	{
		TrySetLicense(licensePath);

		byte[] templateBytes;
		using (var templateFile = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			templateBytes = new byte[templateFile.Length];
			templateFile.ReadExactly(templateBytes);
		}

		using var templateStream = new MemoryStream(templateBytes);
		var workbook = new Workbook(templateStream);
		var worksheet = workbook.Worksheets[0];
		var isKpa = systemUnit.Contains("kpa", StringComparison.OrdinalIgnoreCase);

		Set(worksheet, "E9", input.Custormer);
		Set(worksheet, "W15", report.modelName);
		Set(worksheet, "AF16", Math.Round(report.CPF, 0).ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "P24", report.DeliveredFlow);
		Set(worksheet, "P25", report.InletCoolingWaterTemp);
		Set(worksheet, "P26", input.Tcw_in.ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "P29", FormatPressure(report.Pressure, isKpa));
		Set(worksheet, "P30", report.Temprature);
		Set(worksheet, "P31", input.Hin.ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "P36", FormatPressure(report.Pressure2, isKpa));
		Set(worksheet, "P40", Math.Round(report.MaxP, 0).ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "X113", report.OilHeater.ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "AA119", Math.Round(report.moc, 1).ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "AC119", Math.Round(report.mic, 1).ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "AF119", Math.Round(report.mac, 1).ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "AF120", Math.Round(input.Tcw_in + report.wTrise, 1).ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "AA122", Math.Round(report.mtot, 1).ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "F157", report.Sst1.ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "F158", report.Sst2.ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "F159", report.Sst3.ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "AA183", report.Dinlet.ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "AA184", report.Ddischarge.ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "AA186", report.Dblowoff.ToString(CultureInfo.InvariantCulture));
		Set(worksheet, "AA187", report.Doutlet.ToString(CultureInfo.InvariantCulture));

		workbook.Save(outputStream, SaveFormat.Xlsx);
	}

	private static void TrySetLicense(string licensePath)
	{
		if (string.IsNullOrWhiteSpace(licensePath) || !File.Exists(licensePath))
			return;

		try
		{
			new License().SetLicense(licensePath);
		}
		catch (Exception)
		{
			// Already licensed in this process, or the file cannot be applied again.
		}
	}

	private static void Set(Worksheet worksheet, string cell, string? value)
	{
		var target = worksheet.Cells[cell];
		var merged = target.GetMergedRange();
		if (merged != null)
			worksheet.Cells[merged.FirstRow, merged.FirstColumn].PutValue(value ?? string.Empty);
		else
			target.PutValue(value ?? string.Empty);
	}

	private static string FormatPressure(string? pressure, bool isKpa)
	{
		if (string.IsNullOrWhiteSpace(pressure))
			return string.Empty;

		if (!isKpa)
			return pressure;

		if (!TryParseDouble(pressure, out var bara))
			return pressure;

		return Math.Round(bara * 100, 1).ToString(CultureInfo.InvariantCulture);
	}

	private static bool TryParseDouble(string value, out double number)
	{
		if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
			return true;
		return double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out number);
	}
}
