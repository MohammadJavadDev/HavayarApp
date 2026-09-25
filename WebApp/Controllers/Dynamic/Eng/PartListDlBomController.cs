using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Eng;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Eng
{
	[Route("Panel/Eng/PartListDlBom")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پارت لیست مصرفی", typeof(PartListDlBom))]
	public class PartListDlBomController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Eng\PartListDlBom\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id is null or 0)
				return BadRequest("ردیف مشخص نشده است.");

			var entity = unitOfWork.Repository<PartListDlBom>().TableNoTracking
				.Include(c => c.Part)
				.Include(c => c.Product)
				.Include(c => c.ProductSection)
				.Include(c => c.DL)
				.FirstOrDefault(c => c.Id == id);
			if (entity == null)
				return NotFound();

			return View(@"\Views\Panel\Eng\PartListDlBom\Edit.cshtml", entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save([FromBody] PartListDlBom model, CancellationToken cn)
		{
			if (model.Id is null or 0)
				return BadRequest("افزودن ردیف جدید مجاز نیست.");

			var existing = await unitOfWork.Repository<PartListDlBom>().Table
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return NotFound();

			// HTS DoOperation(Update): preserve created dates
			existing.ProductSectionId = model.ProductSectionId;
			existing.Qty = model.Qty;
			existing.Order = model.Order;
			existing.IsIgnored = model.IsIgnored;
			existing.Comment = model.Comment;

			var updated = await unitOfWork.Repository<PartListDlBom>().UpdateAsync(existing, cn, true);
			return Ok(updated);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<PartListDlBom>().FetchDataAsync(request, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartListDlBom>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("حذف و بازخوانی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Renew([FromBody] RenewRequest req, CancellationToken cn)
		{
			if ((req.DlId is > 0) == (req.ProductId is > 0))
				return BadRequest("جستجو باید فقط با مرکز پروژه یا فقط با محصول باشد.");

			if (req.DlId is > 0)
			{
				await RenewByDlId(req.DlId.Value, cn);
				return Ok(true);
			}

			await RenewByProductId(req.ProductId!.Value, cn);
			return Ok(true);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده", ActionAccessType.View, ActionAccessItemType.Show)]
		public async Task<IActionResult> Print(long? dlId, long? productId, CancellationToken cn)
		{
			if (dlId is null or <= 0)
				return BadRequest("برای چاپ، مرکز پروژه الزامی است.");

			// HTS ReadData.GetPartListSections(productId) + GetPartListSectionItems(dlId):
			// master = all product sections by Order; detail = DlBom for that DL with IsIgnored = 0,
			// ordered by section Order then item Order. Relation: section.Id = item.ProductSectionId.
			var resolvedProductId = productId is > 0
				? productId
				: await unitOfWork.Repository<PartListDlBom>().TableNoTracking
					.Where(c => c.DlId == dlId)
					.Select(c => (long?)c.ProductId)
					.FirstOrDefaultAsync(cn);

			if (resolvedProductId is null or <= 0)
				return View(@"\Views\Panel\Eng\PartListDlBom\Print.cshtml", new PartListPrintVm());

			var product = await unitOfWork.Repository<Part>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == resolvedProductId, cn);

			var sectionRows = await unitOfWork.Repository<PartListProductSection>().TableNoTracking
				.Where(c => c.ProductId == resolvedProductId)
				.OrderBy(c => c.Order)
				.ThenBy(c => c.Id)
				.ToListAsync(cn);

			var items = await (
				from d in unitOfWork.Repository<PartListDlBom>().TableNoTracking
				join p in unitOfWork.Repository<Part>().TableNoTracking on d.PartId equals p.Id
				join s in unitOfWork.Repository<PartListProductSection>().TableNoTracking on d.ProductSectionId equals s.Id
				where d.DlId == dlId && !d.IsIgnored && d.ProductSectionId != null
				orderby s.Order, d.Order
				select new PartListPrintItemVm
				{
					SectionId = d.ProductSectionId!.Value,
					PartCode = p.Code,
					LatinName = p.LatinTitle,
					Qty = d.Qty,
					Order = d.Order,
					ProductId = d.ProductId
				}).ToListAsync(cn);

			var itemsBySection = items
				.GroupBy(c => c.SectionId)
				.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());

			var sections = sectionRows.Select(s =>
			{
				var sid = s.Id ?? 0;
				return new PartListPrintSectionVm
				{
					Title = s.Title,
					Order = s.Order,
					PictureId = s.PictureId,
					PictureUrl = s.PictureId.HasValue ? $"/File/download/{s.PictureId}" : null,
					Items = itemsBySection.TryGetValue(sid, out var list) ? list : new List<PartListPrintItemVm>()
				};
			}).ToList();

			var vm = new PartListPrintVm
			{
				ProductCode = product?.Code,
				ProductName = product?.Name,
				// HTS Part.ModelTitle → Inv.Part.Brand in HavayarApp
				ProductModel = product?.Brand,
				Sections = sections
			};
			return View(@"\Views\Panel\Eng\PartListDlBom\Print.cshtml", vm);
		}

		/// <summary>HTS EngPartListDlBomService.GetModelByDlId + RenewOperation</summary>
		private async Task RenewByDlId(long dlId, CancellationToken cn)
		{
			var existing = await unitOfWork.Repository<PartListDlBom>().Table
				.Where(c => c.DlId == dlId)
				.ToListAsync(cn);
			foreach (var e in existing)
				await unitOfWork.Repository<PartListDlBom>().DeleteAsync(e, cn, false);
			await unitOfWork.SaveChangesAsync(cn);

			// VchTypeId = 7 برگشت از مصرف پروژه
			var utilized = await unitOfWork.Repository<ProjectUtilizedMaterial>().TableNoTracking
				.Include(c => c.DL)
				.Where(c => c.DLId == dlId && c.VchTypeId != 7)
				.ToListAsync(cn);

			var dlPartListItems = utilized
				.GroupBy(c => c.PartId)
				.Select(g => g.First())
				.Where(c => c.PartId.HasValue)
				.Select(c => new { PartId = c.PartId!.Value, Acc_DLTitle = c.DL?.Title, c.Qty })
				.ToList();

			if (dlPartListItems.Count == 0)
				return;

			var dlTitle = dlPartListItems.First().Acc_DLTitle ?? "";
			var slash = dlTitle.LastIndexOf('/');
			var productSerial = slash >= 0 && slash < dlTitle.Length - 1
				? dlTitle[(slash + 1)..]
				: dlTitle;

			var productOrderItem = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Serial == productSerial, cn);
			if (productOrderItem?.PartId is null or 0)
				return;

			var productId = productOrderItem.PartId.Value;
			await BuildAndInsertDlBomItems(dlId, productId, dlPartListItems.Select(c => (c.PartId, (decimal)c.Qty)).ToList(), cn);
		}

		/// <summary>HTS EngPartListDlBomService.GetModelByPartId + RenewOperation</summary>
		private async Task RenewByProductId(long productId, CancellationToken cn)
		{
			var existing = await unitOfWork.Repository<PartListDlBom>().Table
				.Where(c => c.ProductId == productId && (c.DlId == null || c.DlId <= 0))
				.ToListAsync(cn);
			foreach (var e in existing)
				await unitOfWork.Repository<PartListDlBom>().DeleteAsync(e, cn, false);
			await unitOfWork.SaveChangesAsync(cn);

			var hasOrder = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.AnyAsync(c => c.PartId == productId, cn);
			if (!hasOrder)
				return;

			await BuildAndInsertDlBomItems(null, productId, new List<(long PartId, decimal Qty)>(), cn);
		}

		private async Task BuildAndInsertDlBomItems(
			long? dlId,
			long productId,
			List<(long PartId, decimal Qty)> utilizedParts,
			CancellationToken cn)
		{
			var originalPartList = await unitOfWork.Repository<PartListProductBom>().TableNoTracking
				.Where(c => c.ProductId == productId && c.ProductSectionId != null)
				.ToListAsync(cn);

			var originalPartIds = originalPartList.Select(c => c.PartId).ToArray();
			var similarPartList = await unitOfWork.Repository<SimilarPart>().TableNoTracking
				.Where(c => originalPartIds.Contains(c.BasePartId) || originalPartIds.Contains(c.SimilarPartId))
				.ToListAsync(cn);

			var now = DateTime.Now;
			var items = new List<PartListDlBom>();

			#region مواردی که در مصرف صنایع و/یا کالای مشابه موجود بود
			foreach (var p in utilizedParts)
			{
				var originalItem = originalPartList.FirstOrDefault(f => f.PartId == p.PartId);
				var isExist = originalItem != null;
				var isExistOnSimilarPart = false;

				if (!isExist)
				{
					var similarPartItem = similarPartList.FirstOrDefault(s =>
						s.BasePartId == p.PartId || s.SimilarPartId == p.PartId);
					if (similarPartItem != null)
					{
						isExistOnSimilarPart = true;
						originalItem = originalPartList.FirstOrDefault(f =>
							f.PartId == similarPartItem.BasePartId || f.PartId == similarPartItem.SimilarPartId);
						if (originalItem != null)
							isExist = true;
					}
				}

				items.Add(new PartListDlBom
				{
					DlId = dlId,
					ProductId = productId,
					PartId = p.PartId,
					ProductSectionId = isExist ? originalItem!.ProductSectionId : null,
					IsExistInOriginalBom = isExist,
					IsReadFromSimilarPart = isExistOnSimilarPart,
					IsOnlyExistInOriginalBom = false,
					Order = isExist ? originalItem!.Order : (short)0,
					Qty = p.Qty,
					CreatedOnMiladiDateTime = now
				});
			}
			#endregion

			#region مواردی که فقط در پارت لیست مهندسی هست
			foreach (var p in originalPartList)
			{
				var hasItemInItems = items.Any(x =>
					x.PartId == p.PartId &&
					x.Order == p.Order &&
					x.ProductSectionId == p.ProductSectionId &&
					x.Qty == p.Qty);
				if (hasItemInItems)
					continue;

				var isExistInSimilarParts = similarPartList
					.Where(a => a.BasePartId == p.PartId || a.SimilarPartId == p.PartId)
					.ToList();
				if (isExistInSimilarParts.Count > 0)
				{
					var similarPartIds = isExistInSimilarParts
						.SelectMany(s => new[] { s.BasePartId, s.SimilarPartId })
						.Where(id => id != p.PartId)
						.Distinct()
						.ToList();
					if (items.Any(a => similarPartIds.Contains(a.PartId)))
						continue;
				}

				var isExistOnSimilarPart = false;
				var isExist = false;
				if (dlId is null or <= 0)
				{
					var similarPartItem = similarPartList.FirstOrDefault(s =>
						s.BasePartId == p.PartId || s.SimilarPartId == p.PartId);
					if (similarPartItem != null)
					{
						isExistOnSimilarPart = true;
						var originalItem = originalPartList.FirstOrDefault(f =>
							f.PartId == similarPartItem.BasePartId || f.PartId == similarPartItem.SimilarPartId);
						if (originalItem != null)
							isExist = true;
					}
				}

				items.Add(new PartListDlBom
				{
					DlId = dlId,
					ProductId = productId,
					PartId = p.PartId,
					ProductSectionId = p.ProductSectionId,
					IsExistInOriginalBom = isExist,
					IsOnlyExistInOriginalBom = true,
					IsReadFromSimilarPart = isExistOnSimilarPart,
					Order = p.Order,
					Qty = p.Qty,
					CreatedOnMiladiDateTime = now
				});
			}
			#endregion

			foreach (var item in items)
				await unitOfWork.Repository<PartListDlBom>().SaveAsync(item, cn, false);
			await unitOfWork.SaveChangesAsync(cn);
		}

		public class RenewRequest
		{
			public long? DlId { get; set; }
			public long? ProductId { get; set; }
		}
	}

	public class PartListPrintVm
	{
		public string? ProductCode { get; set; }
		public string? ProductName { get; set; }
		public string? ProductModel { get; set; }
		public List<PartListPrintSectionVm> Sections { get; set; } = new();
	}

	public class PartListPrintSectionVm
	{
		public string Title { get; set; } = "";
		public byte Order { get; set; }
		public long? PictureId { get; set; }
		/// <summary>Same URL as PartListProduct manage page: /File/download/{PictureId}</summary>
		public string? PictureUrl { get; set; }
		public List<PartListPrintItemVm> Items { get; set; } = new();
	}

	public class PartListPrintItemVm
	{
		public long SectionId { get; set; }
		public string SectionTitle { get; set; } = "";
		public byte SectionOrder { get; set; }
		public long? PictureId { get; set; }
		public string? PartCode { get; set; }
		public string? LatinName { get; set; }
		public decimal Qty { get; set; }
		public short Order { get; set; }
		public long ProductId { get; set; }
	}
}
