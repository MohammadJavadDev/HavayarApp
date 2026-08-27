using System.Drawing;
using Stimulsoft.Base.Drawing;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;

namespace ReportBuilder.Services;

public static class CompressorSizingReportLetterhead
{
	public static void Apply(StiReport report, string? imagePath)
	{
		if (report == null || string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
			return;

		var bytes = File.ReadAllBytes(imagePath);
		foreach (StiPage page in report.Pages)
		{
			page.Brush = new StiEmptyBrush();
			page.Watermark.Enabled = true;
			page.Watermark.Text = string.Empty;
			page.Watermark.ImageBytes = bytes;
			page.Watermark.ImageStretch = true;
			page.Watermark.AspectRatio = true;
			page.Watermark.ImageTiling = false;
			page.Watermark.ShowImageBehind = true;
			page.Watermark.ShowBehind = true;
			page.Watermark.ImageTransparency = 0;
			page.Watermark.ImageAlignment = ContentAlignment.MiddleCenter;
			page.Watermark.ImageMultipleFactor = 1;
		}
	}
}
