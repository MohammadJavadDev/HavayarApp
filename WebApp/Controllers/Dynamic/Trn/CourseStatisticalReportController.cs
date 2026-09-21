using Common.Attributes;
using Common.Auth.Enums;
using Data.Services.Trn;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic;

[Route("Panel/Trn/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("گزارش آماری دوره‌ها")]
public sealed class CourseStatisticalReportController(
	ICourseStatisticalReportService reportService) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("گزارش آماری دوره‌ها", ActionAccessType.View, ActionAccessItemType.Dashbord)]
	public IActionResult Report()
	{
		return View(@"\Views\Panel\Trn\CourseStatisticalReport\Report.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("دریافت اطلاعات گزارش آماری دوره‌ها", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetData(
		[FromBody] CourseStatisticalReportFilter? filter,
		CancellationToken cn)
	{
		return Ok(await reportService.GetReportAsync(filter ?? new CourseStatisticalReportFilter(), cn));
	}
}
