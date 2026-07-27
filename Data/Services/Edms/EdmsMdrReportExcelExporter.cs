using Aspose.Cells;
using System.Drawing;
using System.Globalization;

namespace Data.Services.Edms;

public static class EdmsMdrReportExcelExporter
{
	private const int SummaryColumns = 33;
	private const int RevisionBlockSize = 14;
	private const int MaxRevisionBlocks = 31; // Revision 0 .. 30
	private const int TotalColumns = SummaryColumns + MaxRevisionBlocks * RevisionBlockSize; // 467

	private static readonly string[] SummaryHeaders =
	[
		"Project Name", "Document No.", "Document Title", "Department", "Discipline",
		"Responsible", "Checker", "Approver", "Overall Progress", "Weight Factor",
		"Progress", "Class", "Type", "ConsumedManHours", "First Issue Date", "Re-Plan Date",
		"Hold Documents", "Responsible Of Hold", "Description For Hold Documents",
		"Last Rev", "Final Status", "Internal Status", "Havayar Replied Time", "Client Replied Time",
		"Havayar Last Send Date", "Havayar Last Send Transmittal", "Havayar RDS Date", "Havayar Rds No.",
		"Last POI", "Client Last Sent Date", "Client Last Replied Transmittal",
		"Client Last Replied RDS Date", "Client Last Replied RDS No."
	];

	private static readonly string[] RevisionHeaders =
	[
		"Rev No.", "Havayar Sending Date", "Havayar Sending Transmittal", "Havayar RDS Date", "Havayar RDS No.",
		"POI", "Client Time", "Client Reply Date", "Client Reply Transmittal", "Client RDS Date", "Client RDS No.",
		"Havayar Delay", "Client Delay", "Status"
	];

	private static readonly (int StartCol, int EndCol, string Title)[] SectionRow =
	[
		(0, 2, "General Data"),
		(3, 13, "General Data"),
		(14, 18, "Schedule Data"),
		(19, 32, "Final Status")
	];

	public static string BuildFileName()
	{
		var stamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		var suffix = Random.Shared.Next(100000, 999999);
		return $"Portal_Edms_Mdr_Report_{stamp}_{suffix}.xlsx";
	}

	public static void Export(MdrReportResponse report, Stream outputStream, string licensePath)
	{
		var workbook = new Workbook();
		if (!workbook.IsLicensed && File.Exists(licensePath))
			new License().SetLicense(licensePath);

		var worksheet = workbook.Worksheets[0];
		worksheet.Name = "Sheet1";

		var headerStyle = CreateHeaderStyle(workbook);
		WriteHeaderRows(worksheet, headerStyle);
		WriteDataRows(worksheet, report.Items);

		worksheet.AutoFitColumns(0, TotalColumns - 1);
		worksheet.FreezePanes(2, 3, 2, 3);

		workbook.Save(outputStream, SaveFormat.Xlsx);
	}

	private static Style CreateHeaderStyle(Workbook workbook)
	{
		var style = workbook.CreateStyle();
		style.Font.Name = "Calibri";
		style.Font.Size = 11;
		style.Font.Color = Color.White;
		style.ForegroundColor = Color.FromArgb(0x7A, 0x7A, 0x7A);
		style.Pattern = BackgroundType.Solid;
		style.HorizontalAlignment = TextAlignmentType.Center;
		style.VerticalAlignment = TextAlignmentType.Center;
		style.IsTextWrapped = true;
		return style;
	}

	private static void WriteHeaderRows(Worksheet ws, Style headerStyle)
	{
		foreach (var section in SectionRow)
		{
			ws.Cells.Merge(0, section.StartCol, 1, section.EndCol - section.StartCol + 1);
			var cell = ws.Cells[0, section.StartCol];
			cell.PutValue(section.Title);
			cell.SetStyle(headerStyle);
		}

		for (var rev = 0; rev < MaxRevisionBlocks; rev++)
		{
			var startCol = SummaryColumns + rev * RevisionBlockSize;
			ws.Cells.Merge(0, startCol, 1, RevisionBlockSize);
			var cell = ws.Cells[0, startCol];
			cell.PutValue($"Revision {rev}");
			cell.SetStyle(headerStyle);
		}

		for (var col = 0; col < SummaryHeaders.Length; col++)
		{
			var cell = ws.Cells[1, col];
			cell.PutValue(SummaryHeaders[col]);
			cell.SetStyle(headerStyle);
		}

		for (var rev = 0; rev < MaxRevisionBlocks; rev++)
		{
			var startCol = SummaryColumns + rev * RevisionBlockSize;
			for (var i = 0; i < RevisionHeaders.Length; i++)
			{
				var cell = ws.Cells[1, startCol + i];
				cell.PutValue(RevisionHeaders[i]);
				cell.SetStyle(headerStyle);
			}
		}
	}

	private static void WriteDataRows(Worksheet ws, List<MdrReportItemDto> items)
	{
		var rowIndex = 2;
		foreach (var item in items)
		{
			WriteSummaryRow(ws, rowIndex, item);
			WriteRevisionBlocks(ws, rowIndex, item);
			rowIndex++;
		}
	}

	private static void WriteSummaryRow(Worksheet ws, int rowIndex, MdrReportItemDto item)
	{
		PutValue(ws, rowIndex, 0, item.ProjectName);
		PutValue(ws, rowIndex, 1, item.DocumentNumber);
		PutValue(ws, rowIndex, 2, item.DocumentTitle);
		PutValue(ws, rowIndex, 3, item.BeneficiaryUnit);
		PutValue(ws, rowIndex, 4, item.Disipline);
		PutValue(ws, rowIndex, 5, item.Responsible);
		PutValue(ws, rowIndex, 6, item.Checker);
		PutValue(ws, rowIndex, 7, item.Approver);
		PutNumber(ws, rowIndex, 8, item.OverallProgress);
		PutNumber(ws, rowIndex, 9, item.WeightFactor ?? 0m);
		PutNumber(ws, rowIndex, 10, item.Progress);
		PutValue(ws, rowIndex, 11, item.DocClass);
		PutValue(ws, rowIndex, 12, item.DocType);
		PutValue(ws, rowIndex, 13, item.ConsumedManHoursInText);
		PutDate(ws, rowIndex, 14, item.FirstIssueDate);
		PutDate(ws, rowIndex, 15, item.RePlanDate);
		ws.Cells[rowIndex, 16].PutValue(item.HoldDocuments);
		PutValue(ws, rowIndex, 17, item.HoldResponsible);
		PutValue(ws, rowIndex, 18, item.DescriptionForHoldDocuments);
		PutValue(ws, rowIndex, 19, item.LastRev);
		PutValue(ws, rowIndex, 20, item.FinalStatus);
		PutValue(ws, rowIndex, 21, item.InternalStatus);
		PutValue(ws, rowIndex, 22, item.HavayarRepliedTime);
		PutValue(ws, rowIndex, 23, item.ClientRepliedTime);
		PutDate(ws, rowIndex, 24, item.HavayarLastSendDate);
		PutValue(ws, rowIndex, 25, item.HavayarLastSendTransmittal);
		PutDate(ws, rowIndex, 26, item.HavayarRdsDate);
		PutValue(ws, rowIndex, 27, item.HavayarRdsNo);
		PutValue(ws, rowIndex, 28, item.LastPoi);
		PutDate(ws, rowIndex, 29, item.ClientLastSentDate);
		PutValue(ws, rowIndex, 30, item.ClientLastRepliedTransmittal);
		PutDate(ws, rowIndex, 31, item.ClientLastRepliedRdsDate);
		PutValue(ws, rowIndex, 32, item.ClientLastRepliedRdsNo);
	}

	private static void WriteRevisionBlocks(Worksheet ws, int rowIndex, MdrReportItemDto item)
	{
		var revisionsByIndex = (item.Revisions ?? [])
			.GroupBy(r => r.Index)
			.ToDictionary(g => g.Key, g => g.Last());

		for (var rev = 0; rev < MaxRevisionBlocks; rev++)
		{
			var startCol = SummaryColumns + rev * RevisionBlockSize;
			if (!revisionsByIndex.TryGetValue(rev, out var revision))
				continue;

			PutValue(ws, rowIndex, startCol + 0, revision.RevNo);
			PutDate(ws, rowIndex, startCol + 1, revision.HavayarSendingDate);
			PutValue(ws, rowIndex, startCol + 2, revision.HavayarSendingTransmittal);
			PutDate(ws, rowIndex, startCol + 3, revision.HavayarRdsDate);
			PutValue(ws, rowIndex, startCol + 4, revision.HavayarRdsNo);
			PutValue(ws, rowIndex, startCol + 5, revision.Poi);
			PutValue(ws, rowIndex, startCol + 6, revision.ClientTime);
			PutDate(ws, rowIndex, startCol + 7, revision.ClientReplyDate);
			PutValue(ws, rowIndex, startCol + 8, revision.ClientReplyTransmittal);
			PutDate(ws, rowIndex, startCol + 9, revision.ClientRdsDate);
			PutValue(ws, rowIndex, startCol + 10, revision.ClientRdsNo);
			PutValue(ws, rowIndex, startCol + 11, revision.HavayarDelay);
			PutValue(ws, rowIndex, startCol + 12, revision.ClientDelay);
			PutValue(ws, rowIndex, startCol + 13, revision.Status);
		}
	}

	private static void PutValue(Worksheet ws, int row, int col, string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return;
		ws.Cells[row, col].PutValue(value);
	}

	private static void PutNumber(Worksheet ws, int row, int col, decimal? value)
	{
		if (value is null)
			return;
		ws.Cells[row, col].PutValue(Convert.ToDouble(value.Value, CultureInfo.InvariantCulture));
	}

	private static void PutNumber(Worksheet ws, int row, int col, int value)
	{
		ws.Cells[row, col].PutValue(value);
	}

	private static void PutDate(Worksheet ws, int row, int col, string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return;

		if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
		    || DateTime.TryParseExact(value, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
		{
			ws.Cells[row, col].PutValue(dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture));
			return;
		}

		ws.Cells[row, col].PutValue(value);
	}
}
