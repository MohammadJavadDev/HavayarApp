using Aspose.Cells;
using System.Drawing;
using System.Globalization;

namespace Data.Services.Pln;

/// <summary>خروجی اکسل گزارش انحراف از تحویل به‌موقع: یک شیت خلاصه سفارش‌ها و یک شیت جزئیات تاخیرها</summary>
public static class TimelyDeliveryReportExcelExporter
{
	private static readonly string[] OrderHeaders =
	[
		"ردیف", "شماره سفارش ساخت", "مشتری", "دپارتمان فروش", "کارشناس فروش",
		"تاریخ مجاز تحویل", "تاریخ آماده‌سازی", "روز تاخیر", "مهلت توافقی (روز)",
		"درصد انحراف", "تاخیر ثبت‌شده (روز)", "تاخیر ثبت‌نشده (روز)",
		"اقلام بدون آماده‌سازی", "کل اقلام"
	];

	private static readonly string[] DelayHeaders =
	[
		"ردیف", "شماره سفارش ساخت", "مشتری", "تاریخ مجاز تحویل", "کالا/سریال",
		"عامل توقف", "تعداد روز", "از تاریخ", "تا تاریخ", "علت تاخیر", "توضیحات", "ثبت‌کننده"
	];

	private static readonly string[] UnitHeaders = ["واحد", "گروه", "درصد انحراف"];

	public static string BuildFileName()
	{
		var stamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		return $"Portal_Pln_TimelyDeliveryReport_{stamp}.xlsx";
	}

	public static void Export(TimelyDeliveryReportResponse report, Stream outputStream, string licensePath)
	{
		var workbook = new Workbook();
		if (!workbook.IsLicensed && File.Exists(licensePath))
			new License().SetLicense(licensePath);

		var headerStyle = CreateHeaderStyle(workbook);

		WriteOrdersSheet(workbook.Worksheets[0], report, headerStyle);
		WriteDelaysSheet(workbook.Worksheets.Add("جزئیات تاخیرها"), report, headerStyle);
		WriteUnitsSheet(workbook.Worksheets.Add("درصد واحدها"), report, headerStyle);

		workbook.Save(outputStream, SaveFormat.Xlsx);
	}

	private static Style CreateHeaderStyle(Workbook workbook)
	{
		var style = workbook.CreateStyle();
		style.Font.Name = "Tahoma";
		style.Font.Size = 10;
		style.Font.IsBold = true;
		style.Font.Color = Color.White;
		style.ForegroundColor = Color.FromArgb(0x7A, 0x7A, 0x7A);
		style.Pattern = BackgroundType.Solid;
		style.HorizontalAlignment = TextAlignmentType.Center;
		style.VerticalAlignment = TextAlignmentType.Center;
		style.IsTextWrapped = true;
		return style;
	}

	private static void WriteOrdersSheet(Worksheet ws, TimelyDeliveryReportResponse report, Style headerStyle)
	{
		ws.Name = "سفارش‌های تاخیردار";
		ws.DisplayRightToLeft = true;

		WriteHeaderRow(ws, 0, OrderHeaders, headerStyle);

		var row = 1;
		foreach (var order in report.Orders)
		{
			var col = 0;
			ws.Cells[row, col++].PutValue(row);
			PutNullableLong(ws, row, col++, order.ProductionOrderNumber);
			PutText(ws, row, col++, order.CustomerTitle);
			PutText(ws, row, col++, order.BranchTitle);
			PutText(ws, row, col++, order.SalesExpertName);
			PutText(ws, row, col++, order.DeliverDateShamsi);
			PutText(ws, row, col++, order.PreparationDateShamsi);
			ws.Cells[row, col++].PutValue(order.MaxDelayDays);
			ws.Cells[row, col++].PutValue(order.AgreedWindowDays);
			PutDecimal(ws, row, col++, order.TotalDeviationPercentage);
			ws.Cells[row, col++].PutValue(order.RegisteredDelayDays);
			ws.Cells[row, col++].PutValue(order.UnregisteredDelayDays);
			ws.Cells[row, col++].PutValue(order.MissingPreparationCount);
			ws.Cells[row, col].PutValue(order.TotalItemsCount);
			row++;
		}

		Finalize(ws, OrderHeaders.Length);
	}

	private static void WriteDelaysSheet(Worksheet ws, TimelyDeliveryReportResponse report, Style headerStyle)
	{
		ws.DisplayRightToLeft = true;
		WriteHeaderRow(ws, 0, DelayHeaders, headerStyle);

		var row = 1;
		foreach (var order in report.Orders)
		{
			foreach (var delay in order.Delays)
			{
				var col = 0;
				ws.Cells[row, col++].PutValue(row);
				PutNullableLong(ws, row, col++, order.ProductionOrderNumber);
				PutText(ws, row, col++, order.CustomerTitle);
				PutText(ws, row, col++, order.DeliverDateShamsi);
				PutText(ws, row, col++, BuildPartLabel(order));
				PutText(ws, row, col++, delay.DelayResponsibleTitle);
				if (delay.DelayDays.HasValue)
					ws.Cells[row, col].PutValue(delay.DelayDays.Value);
				col++;
				PutText(ws, row, col++, delay.FromDateShamsi);
				PutText(ws, row, col++, delay.ToDateShamsi);
				PutText(ws, row, col++, delay.DelayReason);
				PutText(ws, row, col++, delay.Description);
				PutText(ws, row, col, delay.CreatedByName);
				row++;
			}
		}

		Finalize(ws, DelayHeaders.Length);
	}

	private static void WriteUnitsSheet(Worksheet ws, TimelyDeliveryReportResponse report, Style headerStyle)
	{
		ws.DisplayRightToLeft = true;
		WriteHeaderRow(ws, 0, UnitHeaders, headerStyle);

		var row = 1;
		foreach (var unit in report.Units)
		{
			PutText(ws, row, 0, unit.Title);
			PutText(ws, row, 1, unit.Group == DelayUnitGroup.Executive ? "معاونت اجرایی" : "خارج معاونت اجرایی");
			PutDecimal(ws, row, 2, unit.Percentage);
			row++;
		}

		PutText(ws, row, 0, "انحراف از تحویل به‌موقع (کل)");
		PutDecimal(ws, row, 2, report.TotalDeviationPercentage);
		row++;
		PutText(ws, row, 0, "تحویل به‌موقع");
		PutDecimal(ws, row, 2, report.OnTimeDeliveryPercentage);

		Finalize(ws, UnitHeaders.Length);
	}

	private static void WriteHeaderRow(Worksheet ws, int row, string[] headers, Style headerStyle)
	{
		for (var col = 0; col < headers.Length; col++)
		{
			var cell = ws.Cells[row, col];
			cell.PutValue(headers[col]);
			cell.SetStyle(headerStyle);
		}
	}

	private static void Finalize(Worksheet ws, int columnCount)
	{
		ws.AutoFitColumns(0, columnCount - 1);
		ws.FreezePanes(1, 0, 1, 0);
	}

	private static string? BuildPartLabel(TimelyDeliveryOrderDto order)
	{
		if (string.IsNullOrWhiteSpace(order.PartName))
			return order.Serial;
		if (string.IsNullOrWhiteSpace(order.Serial))
			return order.PartName;
		return $"{order.PartName} / {order.Serial}";
	}

	private static void PutText(Worksheet ws, int row, int col, string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return;
		ws.Cells[row, col].PutValue(value);
	}

	private static void PutNullableLong(Worksheet ws, int row, int col, long? value)
	{
		if (!value.HasValue)
			return;
		ws.Cells[row, col].PutValue(value.Value);
	}

	private static void PutDecimal(Worksheet ws, int row, int col, decimal value)
	{
		ws.Cells[row, col].PutValue(Convert.ToDouble(value, CultureInfo.InvariantCulture));
	}
}
