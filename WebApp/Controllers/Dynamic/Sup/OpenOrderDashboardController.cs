using Common.Attributes;
using Common.Auth.Enums;
using Data.Services.Dashboard;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic;

[Route("Panel/Sup/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("داشبورد درخواست‌های باز خرید")]
public sealed class OpenOrderDashboardController(
	IOpenOrderDashboardService dashboardService) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("داشبورد درخواست‌های باز خرید", ActionAccessType.View, ActionAccessItemType.Dashbord)]
	public IActionResult Report()
	{
		return View(@"\Views\Panel\Sup\OpenOrderDashboard\Report.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("دریافت اطلاعات داشبورد درخواست‌های باز خرید", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetData(
		[FromBody] OpenOrderDashboardFilter? filter,
		CancellationToken cn)
	{
		return Ok(await dashboardService.GetDashboardAsync(filter, cn));
	}
}
