using Common.Attributes;
using Common.Auth.Enums;
using Data.Services.Dashboard;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic;

[Route("Panel/Sale/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("داشبورد سفارش ساخت")]
public sealed class SaleProductionDashboardController(
	ISaleProductionDashboardService dashboardService) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("داشبورد سفارش ساخت", ActionAccessType.View, ActionAccessItemType.Dashbord)]
	public IActionResult Report()
	{
		return View(@"\Views\Panel\Sale\SaleProductionDashboard\Report.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("دریافت اطلاعات داشبورد سفارش ساخت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetData(CancellationToken cn)
	{
		return Ok(await dashboardService.GetDataAsync(cn));
	}
}
