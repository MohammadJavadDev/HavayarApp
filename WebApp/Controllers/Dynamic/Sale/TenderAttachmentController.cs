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
	[ControllerInfo("پیوست مناقصه", typeof(TenderAttachment))]
	public class TenderAttachmentController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TenderAttachment model, CancellationToken cn)
		{
			model.Tender = null;
			model.File = null;
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<TenderAttachment>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TenderAttachment model, CancellationToken cn)
		{
			model.Tender = null;
			model.File = null;
			return Ok(await unitOfWork.Repository<TenderAttachment>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TenderAttachment model, CancellationToken cn)
		{
			model.Tender = null;
			model.File = null;
			return Ok(await unitOfWork.Repository<TenderAttachment>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<TenderAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<TenderAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? tenderId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<TenderAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\TenderAttachment\Edit.cshtml", entity ?? new TenderAttachment { TenderId = tenderId });
			}
			return View(@"\Views\Panel\Sale\TenderAttachment\Edit.cshtml", new TenderAttachment { TenderId = tenderId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? tenderId = null)
			=> View(@"\Views\Panel\Sale\TenderAttachment\Edit.cshtml", new TenderAttachment { TenderId = tenderId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\TenderAttachment\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long tenderId)
		{
			ViewBag.ParentId = tenderId;
			return View(@"\Views\Panel\Sale\TenderAttachment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? tenderId, CancellationToken cn)
		{
			if (tenderId == null || tenderId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<TenderAttachment>().TableNoTracking
				.Where(c => c.TenderId == tenderId)
				.Select(c => new
				{
					c.Id,
					c.Title,
					c.FileId,
					FileName = c.File != null ? c.File.OriginalName : null,
					c.Comment
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
				await unitOfWork.Repository<TenderAttachment>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<TenderAttachment>().FetchDataAsync(request, cn));
	}
}
