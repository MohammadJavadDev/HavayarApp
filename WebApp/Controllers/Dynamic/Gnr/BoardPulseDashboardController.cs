using Common.Attributes;
using Common.Auth.Enums;
using Data.Services.Dashboard;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic;

[Route("Panel/Gnr/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("نبض هیئت‌مدیره")]
public sealed class BoardPulseDashboardController(
	IBoardPulseDashboardService dashboardService) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("نبض هیئت‌مدیره", ActionAccessType.View, ActionAccessItemType.Dashbord)]
	public IActionResult Report()
	{
		return View(@"\Views\Panel\Gnr\BoardPulseDashboard\Report.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("دریافت اطلاعات نبض هیئت‌مدیره", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetData(CancellationToken cn)
	{
		return Ok(await dashboardService.GetReportAsync(cn));
	}
}
