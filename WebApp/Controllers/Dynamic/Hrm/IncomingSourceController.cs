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
	[ControllerInfo("کانال ارتباطی", typeof(IncomingSource))]
	public class IncomingSourceController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private static string? Validate(IncomingSource model)
		{
			if (string.IsNullOrWhiteSpace(model.Title))
				return "عنوان نمی‌تواند خالی باشد";
			model.Title = model.Title.Trim();
			return null;
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(IncomingSource model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			model.Id = null;
			model.HtsId = null;
			model.IsActive ??= IsActiveEnum.Active;
			return Ok(await unitOfWork.Repository<IncomingSource>().AddAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(IncomingSource model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			if (model.Id is not > 0)
				return BadRequest("رکورد یافت نشد");

			var old = await unitOfWork.Repository<IncomingSource>().TableNoTracking
				.Where(c => c.Id == model.Id)
				.Select(c => new { c.HtsId, c.IsActive })
				.FirstOrDefaultAsync(cn);
			if (old == null)
				return NotFound();

			if (model.HtsId is null && old.HtsId is not null)
				model.HtsId = old.HtsId;
			model.IsActive ??= old.IsActive ?? IsActiveEnum.Active;
			return Ok(await unitOfWork.Repository<IncomingSource>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<IncomingSource>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<IncomingSource>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = id is > 0
				? unitOfWork.Repository<IncomingSource>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new IncomingSource();
			return View(@"\Views\Panel\Hrm\IncomingSource\Edit.cshtml", entity ?? new IncomingSource());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Hrm\IncomingSource\Edit.cshtml", new IncomingSource { IsActive = IsActiveEnum.Active });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\IncomingSource\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			await unitOfWork.Repository<IncomingSource>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
			memoryStream.Position = 0;
			return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<IncomingSource>().FetchDataAsync(request, cn));
	}
}
