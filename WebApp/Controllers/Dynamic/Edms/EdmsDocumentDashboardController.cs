using Common.Attributes;
using Common.Auth.Enums;
using Data.Services.Dashboard;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic;

[Route("Panel/Edms/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("داشبورد مدارک مهندسی")]
public sealed class EdmsDocumentDashboardController(
	IEdmsDocumentDashboardService dashboardService) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("داشبورد مدارک مهندسی", ActionAccessType.View, ActionAccessItemType.Dashbord)]
	public IActionResult Report()
	{
		return View(@"\Views\Panel\Edms\EdmsDocumentDashboard\Report.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("دریافت اطلاعات داشبورد مدارک مهندسی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetData(CancellationToken cn)
	{
		return Ok(await dashboardService.GetDashboardAsync(cn));
	}
}
