using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Cng;
using Entities.App.Inv;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Cng/RepairsPart")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("قطعات مصرفی تعمیرات CNG", typeof(RepairsPart))]
	public class CngRepairsPartController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(RepairsPart model, CancellationToken cn)
		{
			if (model.RepairsId == 0)
				return BadRequest("شناسه والد خالی است");
			if (model.PartId == 0)
				return BadRequest("کالا نمی‌تواند خالی باشد");

			ClearNav(model);

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<RepairsPart>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(RepairsPart model, CancellationToken cn)
		{
			ClearNav(model);

			var part = await unitOfWork.Repository<Part>().TableNoTracking
				.FirstOrDefaultAsync(p => p.Id == model.PartId, cn);
			if (part != null)
				model.PartUnitId = part.UnitId;

			var repairs = await unitOfWork.Repository<Repairs>().TableNoTracking
				.Include(r => r.Dl)
				.FirstOrDefaultAsync(r => r.Id == model.RepairsId, cn);
			if (repairs != null)
			{
				if (repairs.DlId != null && repairs.DlId != 0)
					model.DlId = repairs.DlId.Value;
				else if (model.DlId == 0)
					return BadRequest("تفصیل تعمیر والد خالی است");

				if (repairs.Dl?.HamkaranId != null && repairs.Dl.HamkaranId <= int.MaxValue)
					model.HamkaranDlFk = (int)repairs.Dl.HamkaranId.Value;
			}

			return Ok(await unitOfWork.Repository<RepairsPart>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(RepairsPart model, CancellationToken cn)
		{
			ClearNav(model);

			var existing = await unitOfWork.Repository<RepairsPart>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return BadRequest("رکورد یافت نشد");

			// حفظ Dl / Hamkaran از رکورد اصلی (مثل HTS Update path)
			model.DlId = existing.DlId;
			model.HamkaranDlFk = existing.HamkaranDlFk;
			model.RepairsId = existing.RepairsId;
			model.HtsId = existing.HtsId;
			if (model.PartUnitId == null || model.PartUnitId == 0)
			{
				var part = await unitOfWork.Repository<Part>().TableNoTracking
					.FirstOrDefaultAsync(p => p.Id == model.PartId, cn);
				model.PartUnitId = part?.UnitId ?? existing.PartUnitId;
			}

			return Ok(await unitOfWork.Repository<RepairsPart>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? repairsId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<RepairsPart>().TableNoTracking
					.Include(c => c.Part)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Cng\RepairsPart\Edit.cshtml",
					entity ?? new RepairsPart { RepairsId = repairsId ?? 0 });
			}
			return View(@"\Views\Panel\Cng\RepairsPart\Edit.cshtml",
				new RepairsPart { RepairsId = repairsId ?? 0 });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? repairsId = null)
			=> View(@"\Views\Panel\Cng\RepairsPart\Edit.cshtml", new RepairsPart { RepairsId = repairsId ?? 0 });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long repairsId)
		{
			ViewBag.ParentId = repairsId;
			return View(@"\Views\Panel\Cng\RepairsPart\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? repairsId, CancellationToken cn)
		{
			if (repairsId == null || repairsId == 0)
				return BadRequest("شناسه والد خالی است");

			var items = await unitOfWork.Repository<RepairsPart>().TableNoTracking
				.Where(c => c.RepairsId == repairsId)
				.Select(c => new
				{
					c.Id,
					c.PartId,
					PartCode = c.Part != null ? c.Part.Code : null,
					PartName = c.Part != null ? c.Part.Name : null,
					c.DamagedPart,
					c.BuyPrice,
					c.SalePrice,
					c.UsedMount,
					c.ReturnMount,
					c.TotalPrice,
					UnitName = c.PartUnit != null ? c.PartUnit.Title : c.UnitName
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<RepairsPart>().FetchDataAsync(request, cn));

		private static void ClearNav(RepairsPart model)
		{
			model.Repairs = null;
			model.Dl = null;
			model.Part = null;
			model.PartUnit = null;
		}
	}
}
