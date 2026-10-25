using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Hrm;
using Entities.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Hrm
{
	[Route("Panel/Hrm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("گرایش رشته تحصیلی", typeof(TrendOfStudyCourse))]
	public class TrendOfStudyCourseController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetByStudyCourse(long studyCourseId, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<TrendOfStudyCourse>().TableNoTracking
				.Where(c => c.StudyCourseId == studyCourseId)
				.OrderBy(c => c.Title)
				.Select(c => new { c.Id, c.Title, c.HtsId, c.StudyCourseId })
				.ToListAsync(cn);
			return Ok(rows);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TrendOfStudyCourse model, CancellationToken cn)
		{
			var error = await Validate(model, cn);
			if (error != null) return BadRequest(error);
			model.Id = null;
			model.HtsId = 0;
			model.IsActive ??= IsActiveEnum.Active;
			return Ok(await unitOfWork.Repository<TrendOfStudyCourse>().AddAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TrendOfStudyCourse model, CancellationToken cn)
		{
			var error = await Validate(model, cn);
			if (error != null) return BadRequest(error);
			if (model.Id is not > 0)
				return BadRequest("رکورد یافت نشد");

			var old = await unitOfWork.Repository<TrendOfStudyCourse>().TableNoTracking
				.Where(c => c.Id == model.Id)
				.Select(c => new { c.HtsId, c.IsActive })
				.FirstOrDefaultAsync(cn);
			if (old == null)
				return NotFound();

			if (model.HtsId <= 0 && old.HtsId > 0)
				model.HtsId = old.HtsId;
			model.IsActive ??= old.IsActive ?? IsActiveEnum.Active;
			return Ok(await unitOfWork.Repository<TrendOfStudyCourse>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<TrendOfStudyCourse>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<TrendOfStudyCourse>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = id is > 0
				? unitOfWork.Repository<TrendOfStudyCourse>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new TrendOfStudyCourse();
			return View(@"\Views\Panel\Hrm\TrendOfStudyCourse\Edit.cshtml", entity ?? new TrendOfStudyCourse());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long studyCourseId)
			=> View(@"\Views\Panel\Hrm\TrendOfStudyCourse\Edit.cshtml", new TrendOfStudyCourse
			{
				StudyCourseId = studyCourseId,
				IsActive = IsActiveEnum.Active
			});

		private async Task<string?> Validate(TrendOfStudyCourse model, CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(model.Title))
				return "عنوان نمی‌تواند خالی باشد";
			if (model.StudyCourseId <= 0)
				return "رشته تحصیلی را انتخاب کنید";
			if (!await unitOfWork.Repository<StudyCourse>().TableNoTracking.AnyAsync(c => c.Id == model.StudyCourseId, cn))
				return "رشته تحصیلی یافت نشد";
			model.Title = model.Title.Trim();
			return null;
		}
	}
}
