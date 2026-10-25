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
[ControllerInfo("داشبورد خدمات پس از فروش")]
public sealed class AfterSalesExecutiveDashboardController(
	IAfterSalesExecutiveDashboardService dashboardService) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("داشبورد مدیریتی خدمات پس از فروش", ActionAccessType.View, ActionAccessItemType.Dashbord)]
	public IActionResult Report()
	{
		return View(@"\Views\Panel\Sale\AfterSalesExecutiveDashboard\Report.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("دریافت اطلاعات داشبورد خدمات پس از فروش", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetData(
		[FromBody] AfterSalesExecutiveDashboardFilter? filter,
		CancellationToken cn)
	{
		return Ok(await dashboardService.GetDataAsync(filter ?? new AfterSalesExecutiveDashboardFilter(), cn));
	}
}
