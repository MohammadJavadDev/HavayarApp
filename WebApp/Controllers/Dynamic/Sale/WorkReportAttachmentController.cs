using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پیوست گزارش کار", typeof(WorkReportAttachment))]
	public class WorkReportAttachmentController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(WorkReportAttachment model, CancellationToken cn)
		{
			model.WorkReport = null;
			model.File = null;
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<WorkReportAttachment>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(WorkReportAttachment model, CancellationToken cn)
		{
			model.WorkReport = null;
			model.File = null;
			return Ok(await unitOfWork.Repository<WorkReportAttachment>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(WorkReportAttachment model, CancellationToken cn)
		{
			model.WorkReport = null;
			model.File = null;
			return Ok(await unitOfWork.Repository<WorkReportAttachment>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<WorkReportAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<WorkReportAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? workReportId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<WorkReportAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\WorkReportAttachment\Edit.cshtml", entity ?? new WorkReportAttachment { WorkReportId = workReportId });
			}
			return View(@"\Views\Panel\Sale\WorkReportAttachment\Edit.cshtml", new WorkReportAttachment { WorkReportId = workReportId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? workReportId = null)
			=> View(@"\Views\Panel\Sale\WorkReportAttachment\Edit.cshtml", new WorkReportAttachment { WorkReportId = workReportId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\WorkReportAttachment\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long workReportId)
		{
			ViewBag.ParentId = workReportId;
			return View(@"\Views\Panel\Sale\WorkReportAttachment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? workReportId, CancellationToken cn)
		{
			if (workReportId == null || workReportId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<WorkReportAttachment>().TableNoTracking
				.Where(c => c.WorkReportId == workReportId)
				.Select(c => new
				{
					c.Id,
					c.Title,
					c.FileId,
					FileName = c.File != null ? c.File.OriginalName : null,
					c.CreatedOnShamsiDateTime
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
				await unitOfWork.Repository<WorkReportAttachment>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<WorkReportAttachment>().FetchDataAsync(request, cn));
	}
}
