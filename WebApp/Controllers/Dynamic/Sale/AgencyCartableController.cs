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
	[ControllerInfo("کارتابل نمایندگی", typeof(AgencyCartable))]
	public class AgencyCartableController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(AgencyCartable model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<AgencyCartable>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(AgencyCartable model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<AgencyCartable>().SaveAsync(model, cn, true));

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(AgencyCartable model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<AgencyCartable>().UpdateAsync(model, cn, true));

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<AgencyCartable>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<AgencyCartable>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<AgencyCartable>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new AgencyCartable();
			return View(@"\Views\Panel\Sale\AgencyCartable\Edit.cshtml", entity ?? new AgencyCartable());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New() => View(@"\Views\Panel\Sale\AgencyCartable\Edit.cshtml", new AgencyCartable());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\AgencyCartable\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<AgencyCartable>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<AgencyCartable>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("پذیرش کارتابل نمایندگی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Accept(long id, CancellationToken cn)
			=> await SetDone(id, true, cn);

		[HttpPost("[action]")]
		[ActionDisplayName("رد کارتابل نمایندگی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Reject(long id, CancellationToken cn)
			=> await SetDone(id, false, cn);

		private async Task<IActionResult> SetDone(long id, bool isDone, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<AgencyCartable>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("کارتابل نمایندگی یافت نشد");
			entity.IsDone = isDone;
			try
			{
				return Ok(await unitOfWork.Repository<AgencyCartable>().UpdateAsync(entity, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}
	}
}
