using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Inv;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Inv
{
	[Route("Panel/Inv/PartCustomBom")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("BOM سفارشی دفترچه قطعات", typeof(PartCustomBom))]
	public class PartCustomBomController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Inv\PartCustomBom\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			PartCustomBom model;
			if (id is > 0)
			{
				model = await unitOfWork.Repository<PartCustomBom>().TableNoTracking
					.Include(c => c.Product)
					.Include(c => c.Part)
					.FirstOrDefaultAsync(c => c.Id == id, cn) ?? new PartCustomBom();
			}
			else
			{
				model = new PartCustomBom { UsingRate = 1, Order = 1 };
			}
			return View(@"\Views\Panel\Inv\PartCustomBom\_Form.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			return View(@"\Views\Panel\Inv\PartCustomBom\_Form.cshtml", new PartCustomBom { UsingRate = 1, Order = 1 });
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save([FromBody] PartCustomBom model, CancellationToken cn)
		{
			if (model.ProductId <= 0 || model.PartId <= 0)
				return BadRequest("محصول و کالا الزامی هستند.");

			if (model.Id is null or 0)
			{
				if (model.Order <= 0)
				{
					var maxOrder = await unitOfWork.Repository<PartCustomBom>().TableNoTracking
						.Where(c => c.ProductId == model.ProductId)
						.Select(c => (int?)c.Order)
						.MaxAsync(cn) ?? 0;
					model.Order = maxOrder + 1;
				}
				var saved = await unitOfWork.Repository<PartCustomBom>().SaveAsync(model, cn, true);
				return Ok(saved);
			}

			var existing = await unitOfWork.Repository<PartCustomBom>().Table
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return NotFound();

			existing.ProductId = model.ProductId;
			existing.PartId = model.PartId;
			existing.UsingRate = model.UsingRate;
			existing.Order = model.Order;
			existing.IsDisabled = model.IsDisabled;
			existing.Comment = model.Comment;
			var updated = await unitOfWork.Repository<PartCustomBom>().UpdateAsync(existing, cn, true);
			return Ok(updated);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = await unitOfWork.Repository<PartCustomBom>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (model != null)
				await unitOfWork.Repository<PartCustomBom>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<PartCustomBom>().FetchDataAsync(request, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartCustomBom>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}
	}
}
