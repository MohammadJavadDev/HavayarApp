using Aspose.Cells;
using System.Globalization;

namespace Data.Services.Eng.FilterWaterTrap;

public sealed class FilterWaterTrapService : IFilterWaterTrapService
{
	private readonly string[] _dataDirectories;

	public FilterWaterTrapService(IEnumerable<string> dataDirectories)
	{
		_dataDirectories = dataDirectories
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Select(Path.GetFullPath)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public WaterTrapCalculationResult CalculateWaterTrap(WaterTrapCalculationRequest request, string? licensePath)
	{
		return WithWorkbook("WaterTrap.xlsx", licensePath, worksheet =>
		{
			SetCell(worksheet, "G3", request.Flowrate);
			SetCell(worksheet, "G4", request.Pressure);
			worksheet.Workbook.CalculateFormula();

			return new WaterTrapCalculationResult
			{
				Model = ReadRangeDisplay(worksheet, "B43", "C43"),
				A = ReadCellDisplay(worksheet, "D43"),
				B = ReadCellDisplay(worksheet, "E43"),
				C = ReadCellDisplay(worksheet, "F43"),
				D = ReadCellDisplay(worksheet, "G43"),
				Kg = ReadCellDisplay(worksheet, "H43")
			};
		});
	}

	public FilterCalculationResult CalculateFilter(FilterCalculationRequest request, string? licensePath)
	{
		return WithWorkbook("Filter.xlsx", licensePath, worksheet =>
		{
			SetCell(worksheet, "C3", request.Flowrate);
			SetCell(worksheet, "C4", request.Pressure);
			SetCell(worksheet, "C5", request.ParticleClass);
			SetCell(worksheet, "C6", request.OilClass);
			worksheet.Workbook.CalculateFormula();

			return new FilterCalculationResult
			{
				PreFilterElement = ReadRangeDisplay(worksheet, "D83", "F83"),
				AfterFilterElement = ReadRangeDisplay(worksheet, "D84", "F84"),
				PreFilterType = ReadCellDisplay(worksheet, "J83"),
				PreFilterTypeDes = ReadCellDisplay(worksheet, "K83"),
				AfterFilterType = ReadCellDisplay(worksheet, "J84"),
				AfterFilterTypeDes = ReadCellDisplay(worksheet, "K84")
			};
		});
	}

	public HdtCalculationResult CalculateHdt(HdtCalculationRequest request, string? licensePath)
	{
		return WithWorkbook("HDT.xlsx", licensePath, worksheet =>
		{
			SetCell(worksheet, "D6", request.MinPressure);
			SetCell(worksheet, "D7", request.AirTemp);
			SetCell(worksheet, "D8", request.NominalFlow);
			SetCell(worksheet, "D9", request.Allowance);
			SetCell(worksheet, "D10", request.DewPoint);
			worksheet.Workbook.CalculateFormula();

			return new HdtCalculationResult
			{
				HdtModel = ReadCellDisplay(worksheet, "G53")
			};
		});
	}

	public byte[] GetHdtTemplateBytes()
	{
		var path = ResolveTemplatePath("HDT.xlsx");
		return File.ReadAllBytes(path);
	}

	public string ResolveTemplatePath(string fileName)
	{
		foreach (var directory in _dataDirectories)
		{
			if (!Directory.Exists(directory))
				continue;

			var path = Path.Combine(directory, fileName);
			if (File.Exists(path))
				return path;
		}

		throw new FileNotFoundException(
			$"Sizing Excel template '{fileName}' was not found. Checked: "
			+ string.Join("; ", _dataDirectories));
	}

	private T WithWorkbook<T>(string fileName, string? licensePath, Func<Worksheet, T> action)
	{
		TrySetLicense(licensePath);

		var path = ResolveTemplatePath(fileName);
		byte[] templateBytes;
		using (var templateFile = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			templateBytes = new byte[templateFile.Length];
			templateFile.ReadExactly(templateBytes);
		}

		using var stream = new MemoryStream(templateBytes);
		var workbook = new Workbook(stream);
		var worksheet = workbook.Worksheets[0];
		return action(worksheet);
	}

	private static void TrySetLicense(string? licensePath)
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

	private static void SetCell(Worksheet worksheet, string cell, string? value)
	{
		var text = value ?? string.Empty;
		var target = worksheet.Cells[cell];
		var merged = target.GetMergedRange();
		var writeCell = merged != null
			? worksheet.Cells[merged.FirstRow, merged.FirstColumn]
			: target;

		if (TryParseDouble(text, out var number))
			writeCell.PutValue(number);
		else
			writeCell.PutValue(text);
	}

	private static string ReadCellDisplay(Worksheet worksheet, string cell)
	{
		return FormatCellValue(worksheet.Cells[cell].Value);
	}

	private static string ReadRangeDisplay(Worksheet worksheet, string startCell, string endCell)
	{
		var start = worksheet.Cells[startCell];
		var end = worksheet.Cells[endCell];
		var parts = new List<string>();

		for (var row = start.Row; row <= end.Row; row++)
		{
			for (var col = start.Column; col <= end.Column; col++)
			{
				var text = FormatCellValue(worksheet.Cells[row, col].Value);
				if (!string.IsNullOrWhiteSpace(text))
					parts.Add(text);
			}
		}

		return string.Join(" ", parts);
	}

	private static string FormatCellValue(object? value)
	{
		if (value == null)
			return string.Empty;

		if (value is Array array)
		{
			var parts = new List<string>();
			foreach (var item in array)
			{
				var text = FormatCellValue(item);
				if (!string.IsNullOrWhiteSpace(text))
					parts.Add(text);
			}
			return string.Join(" ", parts);
		}

		if (value is double d)
			return d.ToString(CultureInfo.InvariantCulture);

		if (value is float f)
			return f.ToString(CultureInfo.InvariantCulture);

		if (value is decimal m)
			return m.ToString(CultureInfo.InvariantCulture);

		if (value is DateTime dt)
			return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
	}

	private static bool TryParseDouble(string value, out double number)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			number = 0;
			return false;
		}

		if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
			return true;
		return double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out number);
	}
}
