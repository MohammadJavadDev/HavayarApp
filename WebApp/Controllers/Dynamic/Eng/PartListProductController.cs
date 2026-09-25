using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Eng;
using Entities.App.Inv;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Eng
{
	[Route("Panel/Eng/PartListProduct")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پارت لیست", typeof(PartListProductSection))]
	public class PartListProductController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Eng\PartListProduct\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			return View(@"\Views\Panel\Eng\PartListProduct\New.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? productId)
		{
			var pid = productId;
			if (pid is null or 0 && id is > 0)
			{
				var section = unitOfWork.Repository<PartListProductSection>().TableNoTracking
					.FirstOrDefault(c => c.Id == id);
				pid = section?.ProductId;
				if (pid is null or 0)
				{
					// id may be ProductId from the product-summary data profile (MIN section id OR product id)
					var byProduct = unitOfWork.Repository<Part>().TableNoTracking.Any(c => c.Id == id);
					if (byProduct)
						pid = id;
				}
			}
			if (pid is null or 0)
				return BadRequest("محصول مشخص نشده است.");

			return Manage(pid.Value);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مدیریت پارت لیست محصول", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Manage(long productId)
		{
			var product = unitOfWork.Repository<Part>().TableNoTracking.FirstOrDefault(c => c.Id == productId);
			if (product == null)
				return NotFound("محصول یافت نشد.");

			ViewBag.ProductId = productId;
			ViewBag.ProductName = product.Name;
			ViewBag.ProductCode = product.Code;
			return View(@"\Views\Panel\Eng\PartListProduct\Manage.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<PartListProductSection>().FetchDataAsync(request, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartListProductSection>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetManageData(long productId, CancellationToken cn)
		{
			var product = await unitOfWork.Repository<Part>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == productId, cn);
			if (product == null)
				return NotFound("محصول یافت نشد.");

			var sections = await unitOfWork.Repository<PartListProductSection>().TableNoTracking
				.Include(c => c.Picture)
				.Where(c => c.ProductId == productId)
				.OrderBy(c => c.Order)
				.ThenBy(c => c.Id)
				.ToListAsync(cn);

			var sectionIds = sections.Select(c => c.Id).ToList();
			var boms = await unitOfWork.Repository<PartListProductBom>().TableNoTracking
				.Include(c => c.Part)
				.Where(c => c.ProductId == productId)
				.OrderBy(c => c.Order)
				.ThenBy(c => c.Id)
				.ToListAsync(cn);

			return Ok(new
			{
				product = new { id = product.Id, name = product.Name, code = product.Code },
				sections = sections.Select(s => new
				{
					id = s.Id,
					title = s.Title,
					order = s.Order,
					isCover = s.IsCover,
					isDisabled = s.IsDisabled,
					comment = s.Comment,
					pictureId = s.PictureId,
					picturePath = s.PicturePath,
					pictureUrl = s.PictureId.HasValue ? $"/File/download/{s.PictureId}" : null,
					parts = boms.Where(b => b.ProductSectionId == s.Id).Select(MapBom).ToList()
				}),
				orphanParts = boms.Where(b => b.ProductSectionId == null || !sectionIds.Contains(b.ProductSectionId.Value))
					.Select(MapBom).ToList()
			});
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره دسته", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> SaveSection([FromBody] PartListProductSection model, CancellationToken cn)
		{
			if (model.ProductId <= 0)
				return BadRequest("محصول مشخص نشده است.");
			if (string.IsNullOrWhiteSpace(model.Title))
				return BadRequest("عنوان دسته الزامی است.");

			if (model.Id is null or 0)
			{
				if (model.Order <= 0)
				{
					var maxOrder = await unitOfWork.Repository<PartListProductSection>().TableNoTracking
						.Where(c => c.ProductId == model.ProductId)
						.Select(c => (byte?)c.Order)
						.MaxAsync(cn) ?? 0;
					model.Order = (byte)(maxOrder + 1);
				}
				var saved = await unitOfWork.Repository<PartListProductSection>().SaveAsync(model, cn, true);
				return Ok(saved);
			}

			var existing = await unitOfWork.Repository<PartListProductSection>().Table
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return NotFound();

			existing.Title = model.Title;
			if (model.Order > 0)
				existing.Order = model.Order;
			existing.IsCover = model.IsCover;
			existing.IsDisabled = model.IsDisabled;
			existing.Comment = model.Comment;
			existing.PictureId = model.PictureId;
			var updated = await unitOfWork.Repository<PartListProductSection>().UpdateAsync(existing, cn, true);
			return Ok(updated);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تغییر ترتیب دسته", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> ReorderSection([FromBody] ReorderSectionRequest req, CancellationToken cn)
		{
			if (req.SectionId <= 0 || req.NewOrder < 1)
				return BadRequest("پارامتر نامعتبر است.");

			var sections = await unitOfWork.Repository<PartListProductSection>().Table
				.Where(c => c.ProductId == req.ProductId)
				.OrderBy(c => c.Order)
				.ThenBy(c => c.Id)
				.ToListAsync(cn);

			var changed = sections.FirstOrDefault(c => c.Id == req.SectionId);
			if (changed == null)
				return NotFound();

			var newOrder = (byte)Math.Min(req.NewOrder, sections.Count);
			foreach (var s in sections.Where(c => c.Id != changed.Id && c.Order >= newOrder))
				s.Order = (byte)(s.Order + 1);
			changed.Order = newOrder;

			var ordered = sections.OrderBy(c => c.Order).ThenBy(c => c.Id == changed.Id ? 0 : 1).ThenBy(c => c.Id).ToList();
			for (var i = 0; i < ordered.Count; i++)
				ordered[i].Order = (byte)(i + 1);

			foreach (var s in ordered)
				await unitOfWork.Repository<PartListProductSection>().UpdateAsync(s, cn, false);
			await unitOfWork.SaveChangesAsync(cn);
			return Ok(true);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف دسته", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> DeleteSection(long id, CancellationToken cn)
		{
			var section = await unitOfWork.Repository<PartListProductSection>().Table
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (section == null)
				return Ok();

			var boms = await unitOfWork.Repository<PartListProductBom>().Table
				.Where(c => c.ProductSectionId == id)
				.ToListAsync(cn);
			foreach (var b in boms)
				await unitOfWork.Repository<PartListProductBom>().DeleteAsync(b, cn, false);

			await unitOfWork.Repository<PartListProductSection>().DeleteAsync(section, cn, false);
			await unitOfWork.SaveChangesAsync(cn);

			var remaining = await unitOfWork.Repository<PartListProductSection>().Table
				.Where(c => c.ProductId == section.ProductId)
				.OrderBy(c => c.Order)
				.ThenBy(c => c.Id)
				.ToListAsync(cn);
			for (var i = 0; i < remaining.Count; i++)
			{
				remaining[i].Order = (byte)(i + 1);
				await unitOfWork.Repository<PartListProductSection>().UpdateAsync(remaining[i], cn, false);
			}
			await unitOfWork.SaveChangesAsync(cn);
			return Ok();
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره قطعه", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> SaveBom([FromBody] PartListProductBom model, CancellationToken cn)
		{
			if (model.ProductId <= 0 || model.PartId <= 0)
				return BadRequest("محصول و کالا الزامی هستند.");

			if (model.Id is null or 0)
			{
				var maxOrder = await unitOfWork.Repository<PartListProductBom>().TableNoTracking
					.Where(c => c.ProductSectionId == model.ProductSectionId)
					.Select(c => (short?)c.Order)
					.MaxAsync(cn) ?? 0;
				if (model.Order <= 0)
					model.Order = (short)(maxOrder + 1);
				var saved = await unitOfWork.Repository<PartListProductBom>().SaveAsync(model, cn, true);
				return Ok(saved);
			}

			var existing = await unitOfWork.Repository<PartListProductBom>().Table
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return NotFound();

			existing.PartId = model.PartId;
			existing.ProductSectionId = model.ProductSectionId;
			existing.Qty = model.Qty;
			existing.Order = model.Order;
			existing.IsIgnored = model.IsIgnored;
			existing.Comment = model.Comment;
			var updated = await unitOfWork.Repository<PartListProductBom>().UpdateAsync(existing, cn, true);
			return Ok(updated);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف قطعه", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> DeleteBom(long id, CancellationToken cn)
		{
			var bom = await unitOfWork.Repository<PartListProductBom>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (bom != null)
				await unitOfWork.Repository<PartListProductBom>().DeleteAsync(bom, cn, true);
			return Ok();
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تنظیم تصویر دسته", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> SetSectionPicture([FromBody] SetPictureRequest req, CancellationToken cn)
		{
			var section = await unitOfWork.Repository<PartListProductSection>().Table
				.FirstOrDefaultAsync(c => c.Id == req.SectionId, cn);
			if (section == null)
				return NotFound();

			section.PictureId = req.PictureId;
			if (req.PictureId is null)
				section.PicturePath = null;
			await unitOfWork.Repository<PartListProductSection>().UpdateAsync(section, cn, true);
			return Ok(section);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> BomForm(long? id, long productId, long? sectionId, CancellationToken cn)
		{
			PartListProductBom model;
			if (id is > 0)
			{
				model = await unitOfWork.Repository<PartListProductBom>().TableNoTracking
					.Include(c => c.Part)
					.FirstOrDefaultAsync(c => c.Id == id, cn) ?? new PartListProductBom();
			}
			else
			{
				model = new PartListProductBom
				{
					ProductId = productId,
					ProductSectionId = sectionId,
					Qty = 1,
					Order = 1
				};
			}
			// Use View (ProcessedViewResult) so X-Partial-Request returns encoded JSON
			// that appController.decodePartialResponse can unpack — PartialView returns raw HTML
			// and decodePartialResponse treats it as empty content.
			return View(@"\Views\Panel\Eng\PartListProduct\_BomForm.cshtml", model);
		}

		[HttpGet("[action]")]
		public IActionResult SectionForm(long productId, long? id)
		{
			PartListProductSection model;
			if (id is > 0)
			{
				model = unitOfWork.Repository<PartListProductSection>().TableNoTracking
					.FirstOrDefault(c => c.Id == id) ?? new PartListProductSection { ProductId = productId };
			}
			else
			{
				model = new PartListProductSection { ProductId = productId, Title = "", Order = 0 };
			}
			return View(@"\Views\Panel\Eng\PartListProduct\_SectionForm.cshtml", model);
		}

		private static object MapBom(PartListProductBom b) => new
		{
			id = b.Id,
			partId = b.PartId,
			partCode = b.Part?.Code,
			partName = b.Part?.Name,
			qty = b.Qty,
			order = b.Order,
			isIgnored = b.IsIgnored,
			comment = b.Comment,
			productSectionId = b.ProductSectionId
		};

		public class ReorderSectionRequest
		{
			public long ProductId { get; set; }
			public long SectionId { get; set; }
			public int NewOrder { get; set; }
		}

		public class SetPictureRequest
		{
			public long SectionId { get; set; }
			public long? PictureId { get; set; }
		}
	}
}
