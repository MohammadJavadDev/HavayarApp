using Common.Attributes;
using Common.Auth.Enums;
using Data.Services.Pln;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic;

[Route("Panel/Pln/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("گزارش تحویل به‌موقع")]
public sealed class PlnTimelyDeliveryReportController(
	ITimelyDeliveryReportService reportService,
	IWebHostEnvironment webHostEnvironment) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("گزارش انحراف از تحویل به‌موقع", ActionAccessType.View, ActionAccessItemType.Dashbord)]
	public IActionResult Report()
	{
		return View(@"\Views\Panel\Pln\PlnTimelyDeliveryReport\Report.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("دریافت اطلاعات گزارش", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetData(
		[FromBody] TimelyDeliveryReportFilter? filter,
		CancellationToken cn)
	{
		return Ok(await reportService.GetReportAsync(filter ?? new TimelyDeliveryReportFilter(), cn));
	}

	[HttpGet("[action]")]
	[ActionDisplayName("لیست شماره سفارش‌های ساخت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetProductionOrderNumbers(CancellationToken cn)
	{
		return Ok(await reportService.GetProductionOrderNumbersAsync(cn));
	}

	[HttpPost("[action]")]
	[ActionDisplayName("خروجی اکسل گزارش", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> ExportToExcel(
		[FromBody] TimelyDeliveryReportFilter? filter,
		CancellationToken cn)
	{
		var licensePath = webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
		var memoryStream = new MemoryStream();
		try
		{
			var report = await reportService.GetReportAsync(filter ?? new TimelyDeliveryReportFilter(), cn);
			TimelyDeliveryReportExcelExporter.Export(report, memoryStream, licensePath);
			memoryStream.Position = 0;
			return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				TimelyDeliveryReportExcelExporter.BuildFileName());
		}
		catch (Exception ex)
		{
			return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
		}
	}
}
