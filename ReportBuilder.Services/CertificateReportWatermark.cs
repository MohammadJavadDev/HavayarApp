using System.Drawing;
using Stimulsoft.Base.Drawing;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;

namespace ReportBuilder.Services;

/// <summary>
/// Applies HTS certificate backgrounds and static stamp/signature images
/// from wwwroot/report-assets/certificates/{ReportName}Watermark.jpg
/// and {ReportName}_{ImageName}.png
/// </summary>
public static class CertificateReportWatermark
{
	public static void Apply(StiReport report)
	{
		if (report == null || string.IsNullOrWhiteSpace(report.ReportName))
			return;

		var imagePath = ResolveAssetPath(report.ReportName + "Watermark.jpg")
			?? ResolveAssetPath(report.ReportName + "Watermark.png");
		if (imagePath != null)
		{
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

		ApplyNamedImages(report);
	}

	private static void ApplyNamedImages(StiReport report)
	{
		foreach (StiPage page in report.Pages)
		{
			foreach (StiComponent component in page.GetComponents())
			{
				if (component is not StiImage image)
					continue;

				var path = ResolveAssetPath(report.ReportName + "_" + image.Name + ".png")
					?? ResolveAssetPath(report.ReportName + "_" + image.Name + ".jpg");
				if (path == null)
					continue;

				image.ImageBytes = File.ReadAllBytes(path);
			}
		}
	}

	private static string? ResolveAssetPath(string fileName)
	{
		var current = Directory.GetCurrentDirectory();
		var baseDir = AppContext.BaseDirectory;
		var candidates = new[]
		{
			Path.Combine(current, "wwwroot", "report-assets", "certificates", fileName),
			Path.Combine(baseDir, "wwwroot", "report-assets", "certificates", fileName),
			Path.Combine(current, "..", "Data", "Seed", "TrnReports", fileName),
			Path.Combine(current, "Data", "Seed", "TrnReports", fileName),
		};

		foreach (var path in candidates)
		{
			try
			{
				var full = Path.GetFullPath(path);
				if (File.Exists(full))
					return full;
			}
			catch
			{
				// ignore invalid path combos
			}
		}

		return null;
	}
}
