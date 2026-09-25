using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Inv;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Inv/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("قطعات یدکی کالا", typeof(PartSparePart))]
	public class PartSparePartController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartSparePart model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<PartSparePart>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartSparePart model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null)
				return BadRequest(error);
			ClearNav(model);
			return Ok(await unitOfWork.Repository<PartSparePart>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PartSparePart model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null)
				return BadRequest(error);
			ClearNav(model);
			return Ok(await unitOfWork.Repository<PartSparePart>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<PartSparePart>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<PartSparePart>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? partId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<PartSparePart>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Inv\PartSparePart\Edit.cshtml", entity ?? new PartSparePart { PartId = partId ?? 0 });
			}
			return View(@"\Views\Panel\Inv\PartSparePart\Edit.cshtml", new PartSparePart { PartId = partId ?? 0 });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? partId = null)
			=> View(@"\Views\Panel\Inv\PartSparePart\Edit.cshtml", new PartSparePart { PartId = partId ?? 0 });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Inv\PartSparePart\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long partId)
		{
			ViewBag.ParentId = partId;
			return View(@"\Views\Panel\Inv\PartSparePart\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? partId, CancellationToken cn)
		{
			if (partId == null || partId == 0)
				return BadRequest("شناسه کالا خالی است");

			var items = await unitOfWork.Repository<PartSparePart>().TableNoTracking
				.Where(c => c.PartId == partId)
				.Select(c => new
				{
					c.Id,
					c.SparePartId,
					SparePartCode = c.SparePart != null ? c.SparePart.Code : null,
					SparePartName = c.SparePart != null ? c.SparePart.Name : null,
					c.WorkingHours,
					c.Description
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartSparePart>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<PartSparePart>().FetchDataAsync(request, cn));

		private static string? Validate(PartSparePart model)
		{
			if (model.PartId == 0)
				return "شناسه کالا خالی است";
			if (model.SparePartId == null || model.SparePartId == 0)
				return "قطعه یدکی را انتخاب کنید";
			return null;
		}

		private static void ClearNav(PartSparePart model)
		{
			model.Part = null!;
			model.SparePart = null;
		}
	}
}
