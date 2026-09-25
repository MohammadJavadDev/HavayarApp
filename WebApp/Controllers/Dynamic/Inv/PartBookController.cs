using Common.Attributes;
using Common.Auth.Enums;
using Data;
using Data.Contracts;
using Entities.App.Inv;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Inv
{
	[Route("Panel/Inv/PartBook")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("دفترچه قطعات", typeof(PartPartBookSection))]
	public class PartBookController(IUnitOfWork unitOfWork, ApplicationDbContext db, IWebHostEnvironment env) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Inv\PartBook\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			return View(@"\Views\Panel\Inv\PartBook\New.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? partId, long? productId)
		{
			var pid = partId ?? productId;
			if (pid is null or 0 && id is > 0)
			{
				var link = unitOfWork.Repository<PartPartBookSection>().TableNoTracking
					.FirstOrDefault(c => c.Id == id);
				pid = link?.PartId;
				if (pid is null or 0)
				{
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
		[ActionDisplayName("مدیریت دفترچه قطعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Manage(long partId)
		{
			var product = unitOfWork.Repository<Part>().TableNoTracking.FirstOrDefault(c => c.Id == partId);
			if (product == null)
				return NotFound("محصول یافت نشد.");

			ViewBag.PartId = partId;
			ViewBag.ProductName = product.Name;
			ViewBag.ProductCode = product.Code;
			return View(@"\Views\Panel\Inv\PartBook\Manage.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<PartPartBookSection>().FetchDataAsync(request, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartPartBookSection>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetManageData(long partId, CancellationToken cn)
		{
			var product = await unitOfWork.Repository<Part>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == partId, cn);
			if (product == null)
				return NotFound("محصول یافت نشد.");

			var links = await unitOfWork.Repository<PartPartBookSection>().TableNoTracking
				.Include(c => c.PartBookSection!)
				.ThenInclude(s => s.Picture)
				.Where(c => c.PartId == partId)
				.ToListAsync(cn);

			var sections = links
				.Where(l => l.PartBookSection != null)
				.OrderBy(l => l.PartBookSection!.Order)
				.ThenBy(l => l.PartBookSectionId)
				.Select(l => l.PartBookSection!)
				.ToList();

			var sectionIds = sections.Select(s => s.Id!.Value).ToList();

			// HTS GetPartBookSectionItems: BOM parts whose own book sections intersect this product's sections
			var bomParts = await (
				from bom in db.vw_ProductItems.AsNoTracking()
				join part in unitOfWork.Repository<Part>().TableNoTracking on bom.PartItemId equals part.Id
				join link in unitOfWork.Repository<PartPartBookSection>().TableNoTracking on part.Id equals link.PartId
				where bom.PartId == partId && sectionIds.Contains(link.PartBookSectionId)
				select new
				{
					SectionId = link.PartBookSectionId,
					PartId = part.Id,
					PartCode = part.Code,
					PartName = part.Name,
					LatinName = part.LatinTitle,
					UsingRate = bom.UsingRate,
					Order = (int?)null,
					IsCustom = false
				}
			).ToListAsync(cn);

			var customParts = await (
				from c in unitOfWork.Repository<PartCustomBom>().TableNoTracking
				join part in unitOfWork.Repository<Part>().TableNoTracking on c.PartId equals part.Id
				join link in unitOfWork.Repository<PartPartBookSection>().TableNoTracking on c.ProductId equals link.PartId
				join sec in unitOfWork.Repository<PartBookSection>().TableNoTracking on link.PartBookSectionId equals sec.Id
				where c.ProductId == partId && sec.IsForCustomBom && sectionIds.Contains(sec.Id!.Value)
				select new
				{
					SectionId = sec.Id!.Value,
					PartId = part.Id,
					PartCode = part.Code,
					PartName = part.Name,
					LatinName = part.LatinTitle,
					UsingRate = c.UsingRate,
					Order = (int?)c.Order,
					IsCustom = true,
					IsDisabled = c.IsDisabled,
					Comment = c.Comment
				}
			).ToListAsync(cn);

			return Ok(new
			{
				product = new { id = product.Id, name = product.Name, code = product.Code },
				sections = sections.Select(s =>
				{
					var sid = s.Id!.Value;
					var fromBom = bomParts.Where(p => p.SectionId == sid).Select(p => new
					{
						partId = p.PartId,
						partCode = p.PartCode,
						partName = p.PartName,
						latinName = p.LatinName,
						usingRate = p.UsingRate,
						order = p.Order,
						isCustom = false,
						isDisabled = false,
						comment = (string?)null
					});
					var fromCustom = customParts.Where(p => p.SectionId == sid).Select(p => new
					{
						partId = p.PartId,
						partCode = p.PartCode,
						partName = p.PartName,
						latinName = p.LatinName,
						usingRate = p.UsingRate,
						order = p.Order,
						isCustom = true,
						isDisabled = p.IsDisabled,
						comment = p.Comment
					});
					var parts = fromBom.Concat(fromCustom)
						.OrderBy(p => p.order ?? int.MaxValue)
						.ThenBy(p => p.partCode)
						.ToList();

					return new
					{
						id = sid,
						linkId = links.First(l => l.PartBookSectionId == sid).Id,
						title = s.Title,
						order = s.Order,
						isForCustomBom = s.IsForCustomBom,
						comment = s.Comment,
						pictureId = s.PictureId,
						pictureUrl = s.PictureId.HasValue ? $"/File/download/{s.PictureId}" : null,
						parts
					};
				})
			});
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> ListAvailableSections(long partId, CancellationToken cn)
		{
			var assigned = await unitOfWork.Repository<PartPartBookSection>().TableNoTracking
				.Where(c => c.PartId == partId)
				.Select(c => c.PartBookSectionId)
				.ToListAsync(cn);

			var sections = await unitOfWork.Repository<PartBookSection>().TableNoTracking
				.Where(c => !assigned.Contains(c.Id!.Value))
				.OrderBy(c => c.Order)
				.ThenBy(c => c.Id)
				.Select(c => new { id = c.Id, title = c.Title, order = c.Order, comment = c.Comment, isForCustomBom = c.IsForCustomBom })
				.ToListAsync(cn);

			return Ok(sections);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("افزودن دسته به محصول", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> AssignSection([FromBody] AssignSectionRequest req, CancellationToken cn)
		{
			if (req.PartId <= 0 || req.PartBookSectionId <= 0)
				return BadRequest("محصول و دسته الزامی هستند.");

			var exists = await unitOfWork.Repository<PartPartBookSection>().TableNoTracking
				.AnyAsync(c => c.PartId == req.PartId && c.PartBookSectionId == req.PartBookSectionId, cn);
			if (exists)
				return Ok(true);

			var section = await unitOfWork.Repository<PartBookSection>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == req.PartBookSectionId, cn);
			if (section == null)
				return NotFound("دسته یافت نشد.");

			await unitOfWork.Repository<PartPartBookSection>().SaveAsync(new PartPartBookSection
			{
				PartId = req.PartId,
				PartBookSectionId = req.PartBookSectionId,
				Order = section.Order
			}, cn, true);

			await RefreshPartBookSectionTitleAsync(req.PartId, cn);
			return Ok(true);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره دسته", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> SaveSection([FromBody] SaveSectionRequest model, CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(model.Title))
				return BadRequest("عنوان دسته الزامی است.");

			PartBookSection section;
			if (model.Id is null or 0)
			{
				if (model.Order <= 0)
				{
					var maxOrder = await unitOfWork.Repository<PartBookSection>().TableNoTracking
						.Select(c => (int?)c.Order)
						.MaxAsync(cn) ?? 0;
					model.Order = maxOrder + 1;
				}
				section = await unitOfWork.Repository<PartBookSection>().SaveAsync(new PartBookSection
				{
					Title = model.Title.Trim(),
					Order = model.Order,
					IsForCustomBom = model.IsForCustomBom,
					Comment = model.Comment,
					PictureId = model.PictureId,
					PictureTitle = model.PictureTitle
				}, cn, true);

				if (model.PartId is > 0)
				{
					await unitOfWork.Repository<PartPartBookSection>().SaveAsync(new PartPartBookSection
					{
						PartId = model.PartId.Value,
						PartBookSectionId = section.Id!.Value,
						Order = section.Order
					}, cn, true);
					await RefreshPartBookSectionTitleAsync(model.PartId.Value, cn);
				}
				return Ok(section);
			}

			section = await unitOfWork.Repository<PartBookSection>().Table
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (section == null)
				return NotFound();

			var oldTitle = section.Title;
			section.Title = model.Title.Trim();
			if (model.Order > 0)
				section.Order = model.Order;
			section.IsForCustomBom = model.IsForCustomBom;
			section.Comment = model.Comment;
			if (model.PictureId.HasValue || model.ClearPicture)
				section.PictureId = model.ClearPicture ? null : model.PictureId;
			if (!string.IsNullOrWhiteSpace(model.PictureTitle))
				section.PictureTitle = model.PictureTitle;

			var updated = await unitOfWork.Repository<PartBookSection>().UpdateAsync(section, cn, true);

			if (!string.Equals(oldTitle, updated.Title, StringComparison.Ordinal))
			{
				var productIds = await unitOfWork.Repository<PartPartBookSection>().TableNoTracking
					.Where(c => c.PartBookSectionId == updated.Id)
					.Select(c => c.PartId)
					.Distinct()
					.ToListAsync(cn);
				foreach (var pid in productIds)
					await RefreshPartBookSectionTitleAsync(pid, cn);
			}

			return Ok(updated);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تغییر ترتیب دسته", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> ReorderSection([FromBody] ReorderSectionRequest req, CancellationToken cn)
		{
			if (req.SectionId <= 0 || req.NewOrder < 1 || req.PartId <= 0)
				return BadRequest("پارامتر نامعتبر است.");

			var links = await unitOfWork.Repository<PartPartBookSection>().TableNoTracking
				.Where(c => c.PartId == req.PartId)
				.Select(c => c.PartBookSectionId)
				.ToListAsync(cn);

			var sections = await unitOfWork.Repository<PartBookSection>().Table
				.Where(c => links.Contains(c.Id!.Value))
				.OrderBy(c => c.Order)
				.ThenBy(c => c.Id)
				.ToListAsync(cn);

			var changed = sections.FirstOrDefault(c => c.Id == req.SectionId);
			if (changed == null)
				return NotFound();

			var newOrder = Math.Min(req.NewOrder, sections.Count);
			foreach (var s in sections.Where(c => c.Id != changed.Id && c.Order >= newOrder))
				s.Order = s.Order + 1;
			changed.Order = newOrder;

			var ordered = sections.OrderBy(c => c.Order).ThenBy(c => c.Id == changed.Id ? 0 : 1).ThenBy(c => c.Id).ToList();
			for (var i = 0; i < ordered.Count; i++)
				ordered[i].Order = i + 1;

			foreach (var s in ordered)
				await unitOfWork.Repository<PartBookSection>().UpdateAsync(s, cn, false);
			await unitOfWork.SaveChangesAsync(cn);
			return Ok(true);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف اتصال دسته", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> RemoveSection(long partId, long sectionId, CancellationToken cn)
		{
			var link = await unitOfWork.Repository<PartPartBookSection>().Table
				.FirstOrDefaultAsync(c => c.PartId == partId && c.PartBookSectionId == sectionId, cn);
			if (link == null)
				return Ok();

			await unitOfWork.Repository<PartPartBookSection>().DeleteAsync(link, cn, true);
			await RefreshPartBookSectionTitleAsync(partId, cn);
			return Ok();
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تنظیم تصویر دسته", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> SetSectionPicture([FromBody] SetPictureRequest req, CancellationToken cn)
		{
			var section = await unitOfWork.Repository<PartBookSection>().Table
				.FirstOrDefaultAsync(c => c.Id == req.SectionId, cn);
			if (section == null)
				return NotFound();

			section.PictureId = req.PictureId;
			await unitOfWork.Repository<PartBookSection>().UpdateAsync(section, cn, true);
			return Ok(section);
		}

		[HttpGet("[action]")]
		public IActionResult SectionForm(long? partId, long? id)
		{
			PartBookSection model;
			if (id is > 0)
			{
				model = unitOfWork.Repository<PartBookSection>().TableNoTracking
					.FirstOrDefault(c => c.Id == id) ?? new PartBookSection();
			}
			else
			{
				model = new PartBookSection { Title = "", Order = 0 };
			}
			ViewBag.PartId = partId;
			return View(@"\Views\Panel\Inv\PartBook\_SectionForm.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده", ActionAccessType.View, ActionAccessItemType.Show)]
		public async Task<IActionResult> Print(long? partId, CancellationToken cn)
		{
			if (partId is null or <= 0)
				return BadRequest("محصول مشخص نشده است.");

			var product = await unitOfWork.Repository<Part>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == partId, cn);

			var links = await unitOfWork.Repository<PartPartBookSection>().TableNoTracking
				.Include(c => c.PartBookSection!)
				.ThenInclude(s => s.Picture)
				.Where(c => c.PartId == partId)
				.ToListAsync(cn);

			var sections = links
				.Where(l => l.PartBookSection != null)
				.OrderBy(l => l.PartBookSection!.Order)
				.ThenBy(l => l.PartBookSectionId)
				.Select(l => l.PartBookSection!)
				.ToList();

			var sectionIds = sections.Select(s => s.Id!.Value).ToList();

			var bomItems = await (
				from bom in db.vw_ProductItems.AsNoTracking()
				join part in unitOfWork.Repository<Part>().TableNoTracking on bom.PartItemId equals part.Id
				join link in unitOfWork.Repository<PartPartBookSection>().TableNoTracking on part.Id equals link.PartId
				where bom.PartId == partId && sectionIds.Contains(link.PartBookSectionId)
				select new PartBookPrintItemVm
				{
					SectionId = link.PartBookSectionId,
					Order = 0,
					PartCode = part.Code,
					UsingRate = bom.UsingRate,
					LatinName = part.LatinTitle
				}
			).ToListAsync(cn);

			var customItems = await (
				from c in unitOfWork.Repository<PartCustomBom>().TableNoTracking
				join part in unitOfWork.Repository<Part>().TableNoTracking on c.PartId equals part.Id
				join link in unitOfWork.Repository<PartPartBookSection>().TableNoTracking on c.ProductId equals link.PartId
				join sec in unitOfWork.Repository<PartBookSection>().TableNoTracking on link.PartBookSectionId equals sec.Id
				where c.ProductId == partId && !c.IsDisabled && sec.IsForCustomBom && sectionIds.Contains(sec.Id!.Value)
				select new PartBookPrintItemVm
				{
					SectionId = sec.Id!.Value,
					Order = c.Order,
					PartCode = part.Code,
					UsingRate = c.UsingRate,
					LatinName = part.LatinTitle
				}
			).ToListAsync(cn);

			var allItems = bomItems.Concat(customItems)
				.OrderBy(i => i.Order)
				.ThenBy(i => i.PartCode)
				.ToList();
			var bySection = allItems.GroupBy(i => i.SectionId).ToDictionary(g => g.Key, g => g.ToList());

			var vm = new PartBookPrintVm
			{
				ProductCode = product?.Code,
				ProductModel = product?.Brand ?? product?.Name,
				Sections = sections.Select(s => new PartBookPrintSectionVm
				{
					Id = s.Id!.Value,
					Title = s.Title,
					PictureUrl = s.PictureId.HasValue ? $"/File/download/{s.PictureId}" : null,
					Items = bySection.TryGetValue(s.Id!.Value, out var list) ? list : new List<PartBookPrintItemVm>()
				}).ToList()
			};

			return View(@"\Views\Panel\Inv\PartBook\Print.cshtml", vm);
		}

		private async Task RefreshPartBookSectionTitleAsync(long partId, CancellationToken cn)
		{
			var titles = await (
				from l in unitOfWork.Repository<PartPartBookSection>().TableNoTracking
				join s in unitOfWork.Repository<PartBookSection>().TableNoTracking on l.PartBookSectionId equals s.Id
				where l.PartId == partId
				orderby s.Order, s.Id
				select s.Title
			).ToListAsync(cn);

			var part = await unitOfWork.Repository<Part>().Table.FirstOrDefaultAsync(c => c.Id == partId, cn);
			if (part == null)
				return;

			part.PartBookSection = titles.Count == 0 ? null : string.Join(", ", titles);
			await unitOfWork.Repository<Part>().UpdateAsync(part, cn, true);
		}

		public class AssignSectionRequest
		{
			public long PartId { get; set; }
			public long PartBookSectionId { get; set; }
		}

		public class SaveSectionRequest
		{
			public long? Id { get; set; }
			public long? PartId { get; set; }
			public string Title { get; set; } = "";
			public int Order { get; set; }
			public bool IsForCustomBom { get; set; }
			public string? Comment { get; set; }
			public long? PictureId { get; set; }
			public string? PictureTitle { get; set; }
			public bool ClearPicture { get; set; }
		}

		public class ReorderSectionRequest
		{
			public long PartId { get; set; }
			public long SectionId { get; set; }
			public int NewOrder { get; set; }
		}

		public class SetPictureRequest
		{
			public long SectionId { get; set; }
			public long? PictureId { get; set; }
		}
	}

	public class PartBookPrintVm
	{
		public string? ProductCode { get; set; }
		public string? ProductModel { get; set; }
		public List<PartBookPrintSectionVm> Sections { get; set; } = new();
	}

	public class PartBookPrintSectionVm
	{
		public long Id { get; set; }
		public string Title { get; set; } = "";
		public string? PictureUrl { get; set; }
		public List<PartBookPrintItemVm> Items { get; set; } = new();
	}

	public class PartBookPrintItemVm
	{
		public long SectionId { get; set; }
		public int Order { get; set; }
		public string? PartCode { get; set; }
		public decimal UsingRate { get; set; }
		public string? LatinName { get; set; }
	}
}
