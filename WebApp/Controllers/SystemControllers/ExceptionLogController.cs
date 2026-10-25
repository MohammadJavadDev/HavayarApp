using Common.Attributes;
using Common.Auth.Enums;
using Data;
using Entities.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers;

[ApiController]
[ApiResultFilter]
[Authorize("AuthenticatedUser")]
[Route("Panel/System/ExceptionLog")]
[ControllerInfo("لاگ خطاها", typeof(ExceptionLog))]
public class ExceptionLogController(ApplicationDbContext db) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("لیست خطاها", ActionAccessType.View, ActionAccessItemType.List)]
	public IActionResult List()
		=> View(@"\Views\Panel\System\ExceptionLog\List.cshtml");

	[HttpGet("[action]")]
	[ActionDisplayName("مشاهده خطا", ActionAccessType.Api, ActionAccessItemType.Show)]
	public async Task<IActionResult> Detail(long id, CancellationToken ct)
	{
		var row = await db.ExceptionLogs.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id, ct);
		if (row == null)
			return NotFound();
		return Ok(row);
	}
}
