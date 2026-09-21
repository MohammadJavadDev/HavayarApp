using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.Services.Eng.CompressorSizing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ReportBuilder.Entities;
using ReportBuilder.Services;
using Stimulsoft.Base;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Eng;

[Route("Panel/Eng/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("سایزینگ کمپرسور")]
public sealed class CompressorSizingController(
	ICompressorSizingService compressorSizingService,
	IWebHostEnvironment webHostEnvironment,
	IMemoryCache memoryCache,
	IUnitOfWork unitOfWork) : BaseController
{
	public static readonly string[] CompressorModels =
	[
		"HYTC400-1", "HYTC400-2", "HYTC400-3", "HYTC400-4",
		"HYTC500-1", "HYTC500-2", "HYTC500-3", "HYTC500-4",
		"HYTC600-1", "HYTC600-2", "HYTC600-3", "HYTC600-4",
		"HYTB700-1", "HYTB700-2", "HYTB700-3", "HYTB700-4",
		"HYTB800-1", "HYTB800-2", "HYTB800-3", "HYTB800-4",
		"HYTB900-1", "HYTB900-2", "HYTB900-3", "HYTB900-4",
		"HYTA1000-1", "HYTA1000-2", "HYTA1000-3", "HYTA1000-4",
		"HYTA1250-1", "HYTA1250-2", "HYTA1250-3", "HYTA1250-4",
		"HYTA1500-1", "HYTA1500-2", "HYTA1500-3", "HYTA1500-4"
	];

	[HttpGet("[action]")]
	[ActionDisplayName("صفحه سایزینگ کمپرسور", ActionAccessType.View, ActionAccessItemType.Show)]
	public IActionResult Calculator()
	{
		ViewBag.CompressorModels = CompressorModels;
		return View(@"\Views\Panel\Eng\CompressorSizing\Calculator.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("انتخاب کمپرسور بر اساس راندمان", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public IActionResult SelectByEfficiency([FromBody] CompressorSizingRequest request)
	{
		return Execute(() => compressorSizingService.SelectByEfficiency(request));
	}

	[HttpPost("[action]")]
	[ActionDisplayName("انتخاب کمپرسور بر اساس قابلیت اطمینان", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public IActionResult SelectByReliability([FromBody] CompressorSizingRequest request)
	{
		return Execute(() => compressorSizingService.SelectByReliability(request));
	}

	[HttpPost("[action]")]
	[ActionDisplayName("آنالیز سایزینگ کمپرسور", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public IActionResult Analyze([FromBody] CompressorSizingRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.modelName))
			return BadRequest("Compressor cannot be empty.");
		if (!CompressorModels.Contains(request.modelName, StringComparer.OrdinalIgnoreCase))
			return BadRequest("Selected compressor is not valid.");

		return Execute(() =>
		{
			var result = compressorSizingService.Analyze(request);
			memoryCache.Set(ReportCacheKey(), request, new MemoryCacheEntryOptions
			{
				SlidingExpiration = TimeSpan.FromHours(4)
			});
			return result;
		});
	}

	[HttpPost("[action]")]
	[ActionDisplayName("خروجی دیتاشیت کمپرسور", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public IActionResult ExportDatasheet([FromBody] CompressorSizingDatasheetRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.modelName))
			return BadRequest("Compressor cannot be empty.");
		if (!CompressorModels.Contains(request.modelName, StringComparer.OrdinalIgnoreCase))
			return BadRequest("Selected compressor is not valid.");
		if (string.IsNullOrWhiteSpace(request.SystemUnit))
			return BadRequest("Datasheet cannot be empty.");

		try
		{
			var licensePath = Path.Combine(webHostEnvironment.WebRootPath, "Aspose.Total.NET.lic");
			var file = compressorSizingService.ExportDatasheet(request, licensePath);
			return File(
				file.Content,
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				file.FileName);
		}
		catch (FileNotFoundException ex)
		{
			return BadRequest(ex.Message);
		}
		catch (InvalidDataException ex)
		{
			return BadRequest(ex.Message);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(ex.Message);
		}
	}

	[HttpGet("[action]")]
	[ActionDisplayName("نمایش گزارش سایزینگ کمپرسور", ActionAccessType.View, ActionAccessItemType.Show)]
	public IActionResult ReportViewer()
	{
		ApplyStimulsoftLicense();
		return View(@"\Views\Panel\Eng\CompressorSizing\ReportViewer.cshtml");
	}

	[HttpGet("[action]")]
	[HttpPost("[action]")]
	[ActionDisplayName("دریافت گزارش سایزینگ کمپرسور", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public IActionResult GetViewReport()
	{
		ApplyStimulsoftLicense();
		try
		{
			var report = BuildPreparedReport();
			if (report == null)
				return BadRequest("Run ANALYZE first.");
			return StiNetCoreViewer.GetReportResult(this, report);
		}
		catch (FileNotFoundException ex)
		{
			return BadRequest(ex.Message);
		}
		catch (InvalidDataException ex)
		{
			return BadRequest(ex.Message);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(ex.Message);
		}
	}

	[HttpGet("[action]")]
	[HttpPost("[action]")]
	[ActionDisplayName("رویداد نمایشگر گزارش سایزینگ", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public IActionResult ViewerEvent()
	{
		ApplyStimulsoftLicense();
		var report = BuildPreparedReport();
		if (report == null)
			return BadRequest("Run ANALYZE first.");
		return StiNetCoreViewer.ViewerEventResult(this, report);
	}

	private StiReport? BuildPreparedReport()
	{
		if (!memoryCache.TryGetValue(ReportCacheKey(), out CompressorSizingRequest? request) || request == null)
			return null;

		var dataSet = compressorSizingService.BuildReportDataSet(request);
		var report = StiReport.CreateNewReport();
		report.LoadFromJson(LoadTemplateJson());
		report.CalculationMode = StiCalculationMode.Interpretation;
		report.RegData(dataSet);
		report.Dictionary.Synchronize();
		CompressorSizingReportLetterhead.Apply(report, compressorSizingService.ResolveWatermarkImagePath());
		CompressorSizingChartBinder.Bind(report, dataSet);
		return report;
	}

	private string LoadTemplateJson()
	{
		var stored = unitOfWork.Repository<ReportBuilderReport>()
			.TableNoTracking
			.FirstOrDefault(c => c.Name == "CompressorSizing");
		if (!string.IsNullOrWhiteSpace(stored?.Content))
			return stored.Content;

		return System.IO.File.ReadAllText(compressorSizingService.ResolveReportTemplatePath());
	}

	private string ReportCacheKey()
	{
		return "CompressorSizing.Report." + (User?.Identity?.Name ?? "anon");
	}

	private static void ApplyStimulsoftLicense()
	{
		StiLicense.Key = "6vJhGtLLLz2GNviWmUTrhSqnOItdDwjBylQzQcAOiHkO46nMQvol4ASeg91in+mGJLnn2KMIpg3eSXQSgaFOm15+0l" +
			"hekKip+wRGMwXsKpHAkTvorOFqnpF9rchcYoxHXtjNDLiDHZGTIWq6D/2q4k/eiJm9fV6FdaJIUbWGS3whFWRLPHWC" +
			"BsWnalqTdZlP9knjaWclfjmUKf2Ksc5btMD6pmR7ZHQfHXfdgYK7tLR1rqtxYxBzOPq3LIBvd3spkQhKb07LTZQoyQ" +
			"3vmRSMALmJSS6ovIS59XPS+oSm8wgvuRFqE1im111GROa7Ww3tNJTA45lkbXX+SocdwXvEZyaaq61Uc1dBg+4uFRxv" +
			"yRWvX5WDmJz1X0VLIbHpcIjdEDJUvVAN7Z+FW5xKsV5ySPs8aegsY9ndn4DmoZ1kWvzUaz+E1mxMbOd3tyaNnmVhPZ" +
			"eIBILmKJGN0BwnnI5fu6JHMM/9QR2tMO1Z4pIwae4P92gKBrt0MqhvnU1Q6kIaPPuG2XBIvAWykVeH2a9EP6064e11" +
			"PFCBX4gEpJ3XFD0peE5+ddZh+h495qUc1H2B";
	}

	private IActionResult Execute<T>(Func<T> action)
	{
		try
		{
			return Ok(action());
		}
		catch (FileNotFoundException ex)
		{
			return BadRequest(ex.Message);
		}
		catch (InvalidDataException ex)
		{
			return BadRequest(ex.Message);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(ex.Message);
		}
	}
}
