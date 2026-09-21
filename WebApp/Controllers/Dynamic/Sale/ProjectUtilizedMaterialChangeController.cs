using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("سوابق تغییرات اقلام مصرفی در پروژه", typeof(ProjectUtilizedMaterialChange))]
	public class ProjectUtilizedMaterialChangeController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long projectUtilizedMaterialId)
		{
			ViewBag.ParentId = projectUtilizedMaterialId;
			return View(@"\Views\Panel\Sale\ProjectUtilizedMaterialChange\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? projectUtilizedMaterialId, CancellationToken cn)
		{
			if (projectUtilizedMaterialId == null || projectUtilizedMaterialId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<ProjectUtilizedMaterialChange>().TableNoTracking
				.Where(c => c.ProjectUtilizedMaterialId == projectUtilizedMaterialId)
				.OrderByDescending(c => c.Id)
				.Select(c => new { c.Id, c.Comment, c.CreatedByName, c.CreatedOnShamsiDateTime })
				.ToListAsync(cn);
			return Ok(items);
		}
	}
}
