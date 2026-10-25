using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Qc;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Qc/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پیوست بازرسی محصول", typeof(ProductInspectionAttachment))]
	public class ProductInspectionAttachmentController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductInspectionAttachment model, CancellationToken cn)
		{
			if (model.ProductInspectionId <= 0)
				return BadRequest("شناسه بازرسی خالی است");

			var parentOk = await unitOfWork.Repository<ProductInspection>().TableNoTracking
				.AnyAsync(p => p.Id == model.ProductInspectionId && p.InspectionType == ProductInspectionConstants.InCompany, cn);
			if (!parentOk)
				return BadRequest("بازرسی یافت نشد");

			model.ProductInspection = null;
			model.File = null;

			if (model.Id is null or 0)
			{
				var fileIds = new[] { model.FileId, model.FileId2, model.FileId3 }
					.Where(id => id is > 0)
					.Select(id => id!.Value)
					.Distinct()
					.Take(3)
					.ToList();
				if (fileIds.Count == 0)
					return BadRequest("الصاق فایل نمی تواند خالی باشد");

				ProductInspectionAttachment? last = null;
				foreach (var fileId in fileIds)
				{
					var file = await unitOfWork.Repository<Entities.Base.FileEntity>().TableNoTracking
						.FirstOrDefaultAsync(f => f.Id == fileId, cn);
					last = await unitOfWork.Repository<ProductInspectionAttachment>().SaveAsync(new ProductInspectionAttachment
					{
						ProductInspectionId = model.ProductInspectionId,
						FileId = fileId,
						OriginalFileName = file?.OriginalName,
						Comment = model.Comment
					}, cn, true);
				}

				return Ok(last);
			}

			var existing = await unitOfWork.Repository<ProductInspectionAttachment>().Table
				.FirstOrDefaultAsync(c => c.Id == model.Id && c.ProductInspectionId == model.ProductInspectionId, cn);
			if (existing == null)
				return NotFound();

			if (model.FileId is > 0)
			{
				var file = await unitOfWork.Repository<Entities.Base.FileEntity>().TableNoTracking
					.FirstOrDefaultAsync(f => f.Id == model.FileId, cn);
				existing.FileId = model.FileId;
				existing.OriginalFileName = file?.OriginalName ?? existing.OriginalFileName;
			}

			existing.Comment = model.Comment;
			await unitOfWork.SaveChangesAsync(cn);
			return Ok(existing);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public Task<IActionResult> Add(ProductInspectionAttachment model, CancellationToken cn) => Save(model, cn);

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public Task<IActionResult> Update(ProductInspectionAttachment model, CancellationToken cn) => Save(model, cn);

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<ProductInspectionAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<ProductInspectionAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? productInspectionId)
		{
			if (id is > 0)
			{
				var entity = unitOfWork.Repository<ProductInspectionAttachment>().TableNoTracking
					.Include(c => c.File)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Qc\ProductInspectionAttachment\Edit.cshtml",
					entity ?? new ProductInspectionAttachment { ProductInspectionId = productInspectionId ?? 0 });
			}

			return View(@"\Views\Panel\Qc\ProductInspectionAttachment\Edit.cshtml",
				new ProductInspectionAttachment { ProductInspectionId = productInspectionId ?? 0 });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? productInspectionId)
			=> View(@"\Views\Panel\Qc\ProductInspectionAttachment\Edit.cshtml",
				new ProductInspectionAttachment { ProductInspectionId = productInspectionId ?? 0 });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long productInspectionId)
		{
			ViewBag.ParentId = productInspectionId;
			ViewBag.ProductionOrderNumber = unitOfWork.Repository<ProductInspection>().TableNoTracking
				.Where(p => p.Id == productInspectionId)
				.Select(p => p.ProductionOrderNumber)
				.FirstOrDefault();
			return View(@"\Views\Panel\Qc\ProductInspectionAttachment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? productInspectionId, CancellationToken cn)
		{
			if (productInspectionId is null or 0)
				return BadRequest("شناسه بازرسی خالی است");

			var items = await unitOfWork.Repository<ProductInspectionAttachment>().TableNoTracking
				.Where(c => c.ProductInspectionId == productInspectionId)
				.OrderByDescending(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.FileId,
					fileName = c.File != null ? c.File.OriginalName : c.OriginalFileName,
					fileSize = c.File != null ? c.File.Size : 0,
					c.CreatedByName,
					c.CreatedOnShamsiDateTime,
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
				await unitOfWork.Repository<ProductInspectionAttachment>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			=> Ok(await unitOfWork.Repository<ProductInspectionAttachment>().FetchDataAsync(request, cn));

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Qc\ProductInspectionAttachment\List.cshtml");
	}
}
