using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Pln;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Pln/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("کسری اقلام سفارش ساخت", typeof(ProductionOrderItemDeficit))]
	public class ProductionOrderItemDeficitController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductionOrderItemDeficit productionOrderItemDeficit, CancellationToken cn)
		{
			if (productionOrderItemDeficit.Id == null || productionOrderItemDeficit.Id == 0)
			{
				return await Add(productionOrderItemDeficit, cn);
			}
			var exist = await unitOfWork.Repository<ProductionOrderItemDeficit>().TableNoTracking.AnyAsync(c => c.Id == productionOrderItemDeficit.Id);
			if (exist)
			{
				return await Update(productionOrderItemDeficit, cn);
			}
			return await Add(productionOrderItemDeficit, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProductionOrderItemDeficit productionOrderItemDeficit, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrderItemDeficit>().SaveAsync(productionOrderItemDeficit, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductionOrderItemDeficit productionOrderItemDeficit, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrderItemDeficit>().UpdateAsync(productionOrderItemDeficit, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProductionOrderItemDeficit>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProductionOrderItemDeficit>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProductionOrderItemDeficit>().TableNoTracking
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Pln\ProductionOrderItemDeficit\Edit.cshtml", entity);
			}
			var newEntity = new ProductionOrderItemDeficit();
			return View(@"\Views\Panel\Pln\ProductionOrderItemDeficit\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ProductionOrderItemDeficit();
			return View(@"\Views\Panel\Pln\ProductionOrderItemDeficit\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Pln\ProductionOrderItemDeficit\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductionOrderItemDeficit>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<ProductionOrderItemDeficit>().FetchDataAsync(request, cn));
		}
	}
}
