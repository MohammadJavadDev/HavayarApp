using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Hrm;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Hrm
{
	[Route("Panel/Hrm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مهارت", typeof(Skill))]
	public class SkillController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private static string? Validate(Skill model)
		{
			if (string.IsNullOrWhiteSpace(model.Title))
				return "عنوان نمی‌تواند خالی باشد";
			model.Title = model.Title.Trim();
			return null;
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Skill model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			model.Id = null;
			model.HtsId = 0;
			model.IsActive ??= IsActiveEnum.Active;
			return Ok(await unitOfWork.Repository<Skill>().AddAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Skill model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			if (model.Id is not > 0)
				return BadRequest("رکورد یافت نشد");

			var old = await unitOfWork.Repository<Skill>().TableNoTracking
				.Where(c => c.Id == model.Id)
				.Select(c => new { c.HtsId, c.IsActive })
				.FirstOrDefaultAsync(cn);
			if (old == null)
				return NotFound();

			if (model.HtsId <= 0 && old.HtsId > 0)
				model.HtsId = old.HtsId;
			model.IsActive ??= old.IsActive ?? IsActiveEnum.Active;
			return Ok(await unitOfWork.Repository<Skill>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<Skill>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<Skill>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = id is > 0
				? unitOfWork.Repository<Skill>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new Skill();
			return View(@"\Views\Panel\Hrm\Skill\Edit.cshtml", entity ?? new Skill());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Hrm\Skill\Edit.cshtml", new Skill { IsActive = IsActiveEnum.Active });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\Skill\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			await unitOfWork.Repository<Skill>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
			memoryStream.Position = 0;
			return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<Skill>().FetchDataAsync(request, cn));
	}
}
