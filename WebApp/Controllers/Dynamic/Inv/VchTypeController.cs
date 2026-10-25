using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Inv/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("نوع رسید انبار", typeof(VchType))]
	public class VchTypeController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(VchType vchType, CancellationToken cn)
		{
			var error = await ValidateAsync(vchType, cn);
			if (error != null)
				return BadRequest(error);

			if (vchType.Id == null || vchType.Id == 0)
				return await Add(vchType, cn);

			var exist = await unitOfWork.Repository<VchType>().TableNoTracking.AnyAsync(c => c.Id == vchType.Id, cn);
			if (exist)
				return await Update(vchType, cn);
			return await Add(vchType, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(VchType vchType, CancellationToken cn)
		{
			var error = await ValidateAsync(vchType, cn);
			if (error != null)
				return BadRequest(error);

			var entity = await unitOfWork.Repository<VchType>().SaveAsync(vchType, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(VchType vchType, CancellationToken cn)
		{
			var error = await ValidateAsync(vchType, cn);
			if (error != null)
				return BadRequest(error);

			var entity = await unitOfWork.Repository<VchType>().UpdateAsync(vchType, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var used = await unitOfWork.Repository<ProjectUtilizedMaterial>().TableNoTracking
				.AnyAsync(c => c.VchTypeId == id, cn);
			if (used)
				return BadRequest("این نوع رسید در اقلام مصرفی پروژه استفاده شده و قابل حذف نیست.");

			var model = await unitOfWork.Repository<VchType>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (model != null)
				await unitOfWork.Repository<VchType>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<VchType>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Inv\VchType\Edit.cshtml", entity);
			}
			return View(@"\Views\Panel\Inv\VchType\Edit.cshtml", new VchType());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Inv\VchType\Edit.cshtml", new VchType());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
			=> View(@"\Views\Panel\Inv\VchType\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<VchType>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<VchType>().FetchDataAsync(request, cn));

		private async Task<string?> ValidateAsync(VchType vchType, CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(vchType.Title))
				return "عنوان نمی‌تواند خالی باشد";
			if (vchType.HamkaranId <= 0)
				return "شناسه راهکاران نمی‌تواند خالی باشد";

			var duplicate = await unitOfWork.Repository<VchType>().TableNoTracking
				.AnyAsync(c => c.HamkaranId == vchType.HamkaranId && c.Id != vchType.Id, cn);
			if (duplicate)
				return "شناسه راهکاران تکراری است";
			return null;
		}
	}
}
