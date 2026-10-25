using Common.Attributes;
using Common.Auth.Enums;
using Data.Services.Eng.N2GeneratorSizing;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Eng;

[Route("Panel/Eng/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("سایزینگ نیتروژن ساز")]
public sealed class N2GeneratorSizingController(
	IN2GeneratorSizingService n2GeneratorSizingService,
	IWebHostEnvironment webHostEnvironment) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("صفحه سایزینگ نیتروژن ساز", ActionAccessType.View, ActionAccessItemType.Show)]
	public IActionResult Calculator()
	{
		return View(@"\Views\Panel\Eng\N2GeneratorSizing\Calculator.cshtml");
	}

	[HttpPost("[action]")]
	[ActionDisplayName("محاسبه سایزینگ نیتروژن ساز", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public IActionResult Calculate([FromBody] N2GeneratorSizingRequest request)
	{
		return Execute(() => n2GeneratorSizingService.Calculate(request, LicensePath()));
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
