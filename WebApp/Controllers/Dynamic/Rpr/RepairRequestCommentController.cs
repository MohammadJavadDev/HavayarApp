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
	[ControllerInfo("یادداشت درخواست تعمیر", typeof(RepairRequestComment))]
	public class RepairRequestCommentController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(RepairRequestComment model, CancellationToken cn)
		{
			model.RepairRequest = null;
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<RepairRequestComment>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(RepairRequestComment model, CancellationToken cn)
		{
			model.RepairRequest = null;
			return Ok(await unitOfWork.Repository<RepairRequestComment>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(RepairRequestComment model, CancellationToken cn)
		{
			model.RepairRequest = null;
			return Ok(await unitOfWork.Repository<RepairRequestComment>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<RepairRequestComment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<RepairRequestComment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? repairRequestId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<RepairRequestComment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Rpr\RepairRequestComment\Edit.cshtml", entity ?? new RepairRequestComment() { RepairRequestId = repairRequestId });
			}
			return View(@"\Views\Panel\Rpr\RepairRequestComment\Edit.cshtml", new RepairRequestComment() { RepairRequestId = repairRequestId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? repairRequestId = null)
			=> View(@"\Views\Panel\Rpr\RepairRequestComment\Edit.cshtml", new RepairRequestComment() { RepairRequestId = repairRequestId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Rpr\RepairRequestComment\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long repairRequestId)
		{
			ViewBag.ParentId = repairRequestId;
			return View(@"\Views\Panel\Rpr\RepairRequestComment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? repairRequestId, CancellationToken cn)
		{
			if (repairRequestId == null || repairRequestId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<RepairRequestComment>().TableNoTracking
				.Where(c => c.RepairRequestId == repairRequestId)
				.Select(c => new {
					c.Id,
					c.Comment,
					c.Cost
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
				await unitOfWork.Repository<RepairRequestComment>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<RepairRequestComment>().FetchDataAsync(request, cn));
	}
}
