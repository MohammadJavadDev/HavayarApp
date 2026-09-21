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
	[ControllerInfo("WBS نفرساعت تعمیر", typeof(RepairRequestWbs))]
	public class RepairRequestWbsController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(RepairRequestWbs model, CancellationToken cn)
		{
			model.RepairRequestManHour = null;
			model.WorkExplanation = null;
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<RepairRequestWbs>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(RepairRequestWbs model, CancellationToken cn)
		{
			model.RepairRequestManHour = null;
			model.WorkExplanation = null;
			return Ok(await unitOfWork.Repository<RepairRequestWbs>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(RepairRequestWbs model, CancellationToken cn)
		{
			model.RepairRequestManHour = null;
			model.WorkExplanation = null;
			return Ok(await unitOfWork.Repository<RepairRequestWbs>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<RepairRequestWbs>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<RepairRequestWbs>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? repairRequestManHourId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<RepairRequestWbs>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Rpr\RepairRequestWbs\Edit.cshtml", entity ?? new RepairRequestWbs() { RepairRequestManHourId = repairRequestManHourId });
			}
			return View(@"\Views\Panel\Rpr\RepairRequestWbs\Edit.cshtml", new RepairRequestWbs() { RepairRequestManHourId = repairRequestManHourId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? repairRequestManHourId = null)
			=> View(@"\Views\Panel\Rpr\RepairRequestWbs\Edit.cshtml", new RepairRequestWbs() { RepairRequestManHourId = repairRequestManHourId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Rpr\RepairRequestWbs\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long repairRequestManHourId)
		{
			ViewBag.ParentId = repairRequestManHourId;
			return View(@"\Views\Panel\Rpr\RepairRequestWbs\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? repairRequestManHourId, CancellationToken cn)
		{
			if (repairRequestManHourId == null || repairRequestManHourId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<RepairRequestWbs>().TableNoTracking
				.Where(c => c.RepairRequestManHourId == repairRequestManHourId)
				.Select(c => new {
					c.Id,
					WorkTitle = c.WorkExplanation != null ? c.WorkExplanation.Title : null,
					c.ManHour,
					c.Description
				})
				.ToListAsync(cn);
			return Ok(items);
		}


		[HttpGet("[action]")]
		[ActionDisplayName("لیست WBS درخواست تعمیر", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByRepairRequestId(long repairRequestId)
		{
			ViewBag.ParentId = repairRequestId;
			ViewBag.IsRepairRequest = true;
			return View(@"\Views\Panel\Rpr\RepairRequestWbs\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت WBS درخواست تعمیر", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByRepairRequestId(long? repairRequestId, CancellationToken cn)
		{
			if (repairRequestId == null || repairRequestId == 0)
				return BadRequest("شناسه درخواست تعمیر خالی است");
			var items = await unitOfWork.Repository<RepairRequestWbs>().TableNoTracking
				.Where(c => c.RepairRequestManHour != null && c.RepairRequestManHour.RepairRequestId == repairRequestId)
				.Select(c => new {
					c.Id,
					WorkTitle = c.WorkExplanation != null ? c.WorkExplanation.Title : null,
					c.ManHour,
					c.Description
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
				await unitOfWork.Repository<RepairRequestWbs>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<RepairRequestWbs>().FetchDataAsync(request, cn));
	}
}
