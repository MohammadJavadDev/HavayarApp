using Aspose.Cells;
using System.Globalization;

namespace Data.Services.Eng.N2GeneratorSizing;

public sealed class N2GeneratorSizingService : IN2GeneratorSizingService
{
	private const string TemplateFileName = "N2GeneratorSizing.xlsx";
	private const string SheetName = "Equipment Selection";

	private readonly string[] _dataDirectories;

	public N2GeneratorSizingService(IEnumerable<string> dataDirectories)
	{
		_dataDirectories = dataDirectories
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Select(Path.GetFullPath)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public N2GeneratorSizingResult Calculate(N2GeneratorSizingRequest request, string? licensePath)
	{
		return WithWorkbook(licensePath, worksheet =>
		{
			SetCell(worksheet, "D2", request.InletPressure);
			SetCell(worksheet, "D4", request.InletTemperature);
			SetCell(worksheet, "D5", request.ProductRange);
			SetCell(worksheet, "D6", "Havayar");
			SetCell(worksheet, "D7", request.RequiredN2Flow);
			SetCell(worksheet, "D8", request.Purity);
			SetCell(worksheet, "D9", request.GeneratorsOnDuty);
			SetCell(worksheet, "D10", request.CmsManufacturer);
			SetCell(worksheet, "D11", request.AirConsumptionMethod);
			SetCell(worksheet, "D29", request.N2GeneratorModel);
			worksheet.Workbook.CalculateFormula();

			return new N2GeneratorSizingResult
			{
				DeliveryPressure = ReadRoundedOrNap(worksheet, "D12"),
				RequiredCmsPerGeneratorLit = ReadRoundedOrNap(worksheet, "D26"),
				RequiredCmsPerGeneratorKg = ReadRoundedOrNap(worksheet, "D27"),
				N2GeneratorModel = ReadText(worksheet, "D28"),
				CapacityRatioOneSizeLess = ReadRoundedOrNap(worksheet, "D30"),
				CapacityRatioSelected = ReadRoundedOrNap(worksheet, "D31"),
				RequiredCmsFillingPerGenerator = ReadRoundedOrNap(worksheet, "D34"),
				TowerNetVolume = ReadRoundedOrNap(worksheet, "D35"),
				TotalAvailableN2Flow = ReadRoundedOrNap(worksheet, "D36"),
				DryAirConsumption = ReadRoundedOrNap(worksheet, "D37"),
				RequiredDryAirReceiverSize = ReadRoundedOrNap(worksheet, "D38"),
				DryAirReceiverRange = ReadText(worksheet, "C39"),
				DryAirReceiverModel = ReadText(worksheet, "D39"),
				RequiredN2ReceiverSize = ReadRoundedOrNap(worksheet, "D40"),
				N2ReceiverRange = ReadText(worksheet, "C41"),
				N2ReceiverModel = ReadText(worksheet, "D41")
			};
		});
	}

	private T WithWorkbook<T>(string? licensePath, Func<Worksheet, T> action)
	{
		TrySetLicense(licensePath);

		var path = ResolveTemplatePath();
		byte[] templateBytes;
		using (var templateFile = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			templateBytes = new byte[templateFile.Length];
			templateFile.ReadExactly(templateBytes);
		}

		using var stream = new MemoryStream(templateBytes);
		var workbook = new Workbook(stream);
		var worksheet = workbook.Worksheets[SheetName];
		if (worksheet == null)
			throw new InvalidDataException($"Worksheet '{SheetName}' was not found in {TemplateFileName}.");
		return action(worksheet);
	}

	private string ResolveTemplatePath()
	{
		foreach (var directory in _dataDirectories)
		{
			if (!Directory.Exists(directory))
				continue;

			var path = Path.Combine(directory, TemplateFileName);
			if (File.Exists(path))
				return path;
		}

		throw new FileNotFoundException(
			$"Sizing Excel template '{TemplateFileName}' was not found. Checked: "
			+ string.Join("; ", _dataDirectories));
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

	private static string ReadRoundedOrNap(Worksheet worksheet, string cell)
	{
		var excelCell = worksheet.Cells[cell];
		// HTS Excel Interop returns CVErr as a negative Int32. That path displays "NAP".
		if (excelCell.IsErrorValue)
			return "NAP";

		var value = excelCell.Value;
		if (!TryToDecimal(value, out var number))
			throw new InvalidOperationException($"Cell {cell} is not numeric after calculation.");

		if (number < 0)
			return "NAP";

		var rounded = Math.Round(number, 2, MidpointRounding.ToEven);
		return rounded.ToString(CultureInfo.InvariantCulture);
	}

	private static string ReadText(Worksheet worksheet, string cell)
	{
		var excelCell = worksheet.Cells[cell];
		if (excelCell.IsErrorValue)
			return "NAP";

		var value = excelCell.Value;
		if (value == null)
			return string.Empty;

		return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
	}

	private static bool TryToDecimal(object? value, out decimal number)
	{
		switch (value)
		{
			case null:
				number = 0;
				return false;
			case decimal dec:
				number = dec;
				return true;
			case double dbl:
				number = (decimal)dbl;
				return true;
			case float flt:
				number = (decimal)flt;
				return true;
			case int i:
				number = i;
				return true;
			case long l:
				number = l;
				return true;
			case string text when decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out number):
				return true;
			case string text when decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out number):
				return true;
			default:
				number = 0;
				return false;
		}
	}

	private static bool TryParseDouble(string value, out double number)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			number = 0;
			return false;
		}

		value = NormalizeDigits(value);
		if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
			return true;
		return double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out number);
	}

	private static string NormalizeDigits(string value)
	{
		var chars = value.ToCharArray();
		var changed = false;
		for (var i = 0; i < chars.Length; i++)
		{
			var c = chars[i];
			if (c is >= '\u06F0' and <= '\u06F9')
			{
				chars[i] = (char)('0' + (c - '\u06F0'));
				changed = true;
			}
			else if (c is >= '\u0660' and <= '\u0669')
			{
				chars[i] = (char)('0' + (c - '\u0660'));
				changed = true;
			}
			else if (c == '\u066B')
			{
				chars[i] = '.';
				changed = true;
			}
		}

		return changed ? new string(chars) : value;
	}
}
