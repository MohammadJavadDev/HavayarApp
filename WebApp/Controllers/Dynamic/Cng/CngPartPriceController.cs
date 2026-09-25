using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Cng;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	// Unique ControllerName vs Sale.PartPriceController; route keeps /panel/cng/partprice/*
	[Route("Panel/Cng/PartPrice")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مدیریت قیمت قطعات", typeof(PartPrice))]
	public class CngPartPriceController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartPrice model, CancellationToken cn)
		{
			if (model.PartId == 0)
				return BadRequest("کالا نمی‌تواند خالی باشد");

			model.Part = null;

			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<PartPrice>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartPrice model, CancellationToken cn)
		{
			model.Part = null;
			return Ok(await unitOfWork.Repository<PartPrice>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PartPrice model, CancellationToken cn)
		{
			model.Part = null;
			return Ok(await unitOfWork.Repository<PartPrice>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<PartPrice>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<PartPrice>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<PartPrice>().TableNoTracking
					.Include(c => c.Part)
					.FirstOrDefault(c => c.Id == id)
				: CreateNewWithToday();
			return View(@"\Views\Panel\Cng\PartPrice\Edit.cshtml", entity ?? CreateNewWithToday());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Cng\PartPrice\Edit.cshtml", CreateNewWithToday());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Cng\PartPrice\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartPrice>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<PartPrice>().FetchDataAsync(request, cn));

		private static PartPrice CreateNewWithToday()
			=> new() { UpdatedDate = DateTime.Today };
	}
}
