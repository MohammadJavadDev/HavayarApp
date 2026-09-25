using Common.Attributes;
using Common.Auth.Enums;
using Data.Services.Eng.FilterWaterTrap;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Eng;

[Route("Panel/Eng/FilterWaterTrap")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("Filter & WaterTrap")]
public sealed class FilterWaterTrapController(
	IFilterWaterTrapService filterWaterTrapService,
	IWebHostEnvironment webHostEnvironment) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("صفحه Filter و Water Trap", ActionAccessType.View, ActionAccessItemType.Show)]
	public IActionResult Calculator()
	{
		return View(@"\Views\Panel\Eng\FilterWaterTrap\Calculator.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("محاسبه Water Trap", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public IActionResult WaterTrapCalculation([FromBody] WaterTrapCalculationRequest request)
	{
		return Execute(() => filterWaterTrapService.CalculateWaterTrap(request, LicensePath()));
	}

	[HttpPost("[action]")]
	[ActionDisplayName("محاسبه Filter", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public IActionResult FilterSelectionCalculation([FromBody] FilterCalculationRequest request)
	{
		return Execute(() => filterWaterTrapService.CalculateFilter(request, LicensePath()));
	}

	[HttpPost("[action]")]
	[ActionDisplayName("محاسبه HDT", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public IActionResult HdtSelectionCalculation([FromBody] HdtCalculationRequest request)
	{
		return Execute(() => filterWaterTrapService.CalculateHdt(request, LicensePath()));
	}

	[HttpPost("[action]")]
	[ActionDisplayName("خروجی اکسل HDT", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public IActionResult HdtToExcelFile()
	{
		try
		{
			var bytes = filterWaterTrapService.GetHdtTemplateBytes();
			return File(
				bytes,
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				"HDT.xlsx");
		}
		catch (FileNotFoundException ex)
		{
			return BadRequest(ex.Message);
		}
	}

	private string LicensePath()
	{
		return Path.Combine(webHostEnvironment.WebRootPath ?? string.Empty, "Aspose.Total.NET.lic");
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
