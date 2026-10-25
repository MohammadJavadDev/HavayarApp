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
	[ControllerInfo("رشته تحصیلی", typeof(StudyCourse))]
	public class StudyCourseController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(StudyCourse model, CancellationToken cn)
		{
			if (model.Id is not > 0)
				return BadRequest("ثبت رشته جدید از این صفحه انجام نمی‌شود");
			if (string.IsNullOrWhiteSpace(model.Title))
				return BadRequest("عنوان نمی‌تواند خالی باشد");

			var old = await unitOfWork.Repository<StudyCourse>().TableNoTracking
				.Where(c => c.Id == model.Id)
				.Select(c => new { c.HtsId, c.HamkaranStudyCourseId, c.IsActive })
				.FirstOrDefaultAsync(cn);
			if (old == null)
				return NotFound();

			if (model.HtsId <= 0 && old.HtsId > 0)
				model.HtsId = old.HtsId;
			if (model.HamkaranStudyCourseId is not > 0 && old.HamkaranStudyCourseId is > 0)
				model.HamkaranStudyCourseId = old.HamkaranStudyCourseId;
			model.IsActive ??= old.IsActive ?? IsActiveEnum.Active;
			model.Title = model.Title.Trim();
			return Ok(await unitOfWork.Repository<StudyCourse>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			if (await unitOfWork.Repository<TrendOfStudyCourse>().TableNoTracking.AnyAsync(c => c.StudyCourseId == id, cn))
				return BadRequest("ابتدا گرایش‌های این رشته را حذف کنید");

			var entity = unitOfWork.Repository<StudyCourse>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<StudyCourse>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id is not > 0)
				return BadRequest("رشته را از لیست انتخاب کنید");
			var entity = unitOfWork.Repository<StudyCourse>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity == null)
				return NotFound();
			return View(@"\Views\Panel\Hrm\StudyCourse\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			ViewBag.CanAddTrend = CanTrend("add");
			ViewBag.CanEditTrend = CanTrend("update");
			ViewBag.CanDeleteTrend = CanTrend("delete");
			return View(@"\Views\Panel\Hrm\StudyCourse\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			await unitOfWork.Repository<StudyCourse>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
			memoryStream.Position = 0;
			return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<StudyCourse>().FetchDataAsync(request, cn));

		private bool CanTrend(string action)
		{
			if (IsAdministrator)
				return true;
			var path = "/panel/hrm/trendofstudycourse/" + action;
			return sdk.CurrentUser?.RoleAccess?.Any(c =>
				c.Path != null && c.Path.Equals(path, StringComparison.OrdinalIgnoreCase)) == true;
		}
	}
}
