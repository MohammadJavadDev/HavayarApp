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
	[ControllerInfo("پیمانکار درخواست تعمیر", typeof(RepairRequestContractor))]
	public class RepairRequestContractorController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(RepairRequestContractor model, CancellationToken cn)
		{
			ClearNavigations(model);
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<RepairRequestContractor>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(RepairRequestContractor model, CancellationToken cn)
		{
			ClearNavigations(model);
			return Ok(await unitOfWork.Repository<RepairRequestContractor>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(RepairRequestContractor model, CancellationToken cn)
		{
			ClearNavigations(model);
			return Ok(await unitOfWork.Repository<RepairRequestContractor>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<RepairRequestContractor>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<RepairRequestContractor>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? repairRequestId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<RepairRequestContractor>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Rpr\RepairRequestContractor\Edit.cshtml", entity ?? new RepairRequestContractor() { RepairRequestId = repairRequestId });
			}
			return View(@"\Views\Panel\Rpr\RepairRequestContractor\Edit.cshtml", new RepairRequestContractor() { RepairRequestId = repairRequestId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? repairRequestId = null)
			=> View(@"\Views\Panel\Rpr\RepairRequestContractor\Edit.cshtml", new RepairRequestContractor() { RepairRequestId = repairRequestId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Rpr\RepairRequestContractor\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long repairRequestId)
		{
			ViewBag.ParentId = repairRequestId;
			return View(@"\Views\Panel\Rpr\RepairRequestContractor\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? repairRequestId, CancellationToken cn)
		{
			if (repairRequestId == null || repairRequestId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<RepairRequestContractor>().TableNoTracking
				.Where(c => c.RepairRequestId == repairRequestId)
				.Select(c => new {
					c.Id,
					ContractorTitle = c.Contractor.Title ?? c.ContractorTitle,
					c.Cost,
					PartTitle = c.Part.Name ?? c.PartTitle,
					c.SendPartShamsiDate,
					c.CostAnnouncementShamsiDate,
					c.AcceptShamsiDate,
					c.AgreementShamsiDate,
					c.ReturnShamsiDate,
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
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<RepairRequestContractor>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<RepairRequestContractor>().FetchDataAsync(request, cn));

		private static void ClearNavigations(RepairRequestContractor model)
		{
			model.RepairRequest = null;
			model.Contractor = null;
			model.Part = null;
		}
	}
}
