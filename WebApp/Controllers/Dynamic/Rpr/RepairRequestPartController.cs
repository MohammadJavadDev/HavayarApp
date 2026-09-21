using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Rpr;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Rpr/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("قطعه درخواست تعمیر", typeof(RepairRequestPart))]
	public class RepairRequestPartController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(RepairRequestPart model, CancellationToken cn)
		{
			model.RepairRequest = null;
			model.Part = null;
			model.PartUnit = null;
			model.Dl = null;
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<RepairRequestPart>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(RepairRequestPart model, CancellationToken cn)
		{
			model.RepairRequest = null;
			model.Part = null;
			model.PartUnit = null;
			model.Dl = null;
			return Ok(await unitOfWork.Repository<RepairRequestPart>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(RepairRequestPart model, CancellationToken cn)
		{
			model.RepairRequest = null;
			model.Part = null;
			model.PartUnit = null;
			model.Dl = null;
			return Ok(await unitOfWork.Repository<RepairRequestPart>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<RepairRequestPart>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<RepairRequestPart>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? repairRequestId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<RepairRequestPart>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Rpr\RepairRequestPart\Edit.cshtml", entity ?? new RepairRequestPart() { RepairRequestId = repairRequestId });
			}
			return View(@"\Views\Panel\Rpr\RepairRequestPart\Edit.cshtml", new RepairRequestPart() { RepairRequestId = repairRequestId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? repairRequestId = null)
			=> View(@"\Views\Panel\Rpr\RepairRequestPart\Edit.cshtml", new RepairRequestPart() { RepairRequestId = repairRequestId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Rpr\RepairRequestPart\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long repairRequestId)
		{
			ViewBag.ParentId = repairRequestId;
			return View(@"\Views\Panel\Rpr\RepairRequestPart\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? repairRequestId, CancellationToken cn)
		{
			if (repairRequestId == null || repairRequestId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<RepairRequestPart>().TableNoTracking
				.Where(c => c.RepairRequestId == repairRequestId)
				.Select(c => new
				{
					c.Id,
					IdNumber = c.RepairRequest != null ? c.RepairRequest.IdNumber : null,
					ProjectCode = c.Dl != null ? c.Dl.Code
						: (c.RepairRequest != null && c.RepairRequest.Dl != null ? c.RepairRequest.Dl.Code : null),
					PartCode = c.Part != null ? c.Part.Code : null,
					PartName = c.Part != null ? c.Part.Name : null,
					c.StockName,
					UnitName = c.UnitName ?? (c.PartUnit != null ? c.PartUnit.Title : null),
					c.DamagedPart,
					c.ReplacementPartDescription,
					c.BuyPrice,
					c.SalePrice,
					c.UsedMount,
					c.ReturnMount,
					ProductName = c.RepairRequest != null && c.RepairRequest.Product != null ? c.RepairRequest.Product.Name : null,
					c.Description
				})
				.ToListAsync(cn);
			var mapped = items.Select(c =>
			{
				var qty = c.UsedMount - c.ReturnMount;
				return new
				{
					c.Id,
					c.IdNumber,
					c.ProjectCode,
					c.PartCode,
					c.PartName,
					c.StockName,
					c.UnitName,
					c.DamagedPart,
					c.ReplacementPartDescription,
					c.BuyPrice,
					c.SalePrice,
					c.UsedMount,
					c.ReturnMount,
					TotalPrice = c.SalePrice == null ? (decimal?)null : (decimal)c.SalePrice.Value * qty,
					TotalBuyPrice = c.BuyPrice == null ? (decimal?)null : (decimal)c.BuyPrice.Value * qty,
					c.ProductName,
					c.Description
				};
			});
			return Ok(mapped);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<RepairRequestPart>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<RepairRequestPart>().FetchDataAsync(request, cn));
	}
}
