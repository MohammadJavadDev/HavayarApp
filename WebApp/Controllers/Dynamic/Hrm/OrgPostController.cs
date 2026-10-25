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
	[ControllerInfo("پست های سازمانی", typeof(OrgPost))]
	public class OrgPostController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(OrgPost model, CancellationToken cn)
		{
			if (model.Id is not > 0)
				return BadRequest("ثبت پست جدید از این صفحه انجام نمی‌شود");
			if (string.IsNullOrWhiteSpace(model.Title))
				return BadRequest("عنوان نمی‌تواند خالی باشد");

			var old = await unitOfWork.Repository<OrgPost>().TableNoTracking
				.Where(c => c.Id == model.Id)
				.Select(c => new { c.HtsId, c.HamkaranPostId })
				.FirstOrDefaultAsync(cn);
			if (old == null)
				return NotFound();

			if (model.HtsId <= 0 && old.HtsId > 0)
				model.HtsId = old.HtsId;
			if (model.HamkaranPostId is not > 0 && old.HamkaranPostId is > 0)
				model.HamkaranPostId = old.HamkaranPostId;

			model.Title = model.Title.Trim();
			return Ok(await unitOfWork.Repository<OrgPost>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<OrgPost>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<OrgPost>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id is not > 0)
				return BadRequest("پست را از لیست انتخاب کنید");
			var entity = unitOfWork.Repository<OrgPost>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity == null)
				return NotFound();
			return View(@"\Views\Panel\Hrm\OrgPost\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\OrgPost\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			await unitOfWork.Repository<OrgPost>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
			memoryStream.Position = 0;
			return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<OrgPost>().FetchDataAsync(request, cn));
	}
}
