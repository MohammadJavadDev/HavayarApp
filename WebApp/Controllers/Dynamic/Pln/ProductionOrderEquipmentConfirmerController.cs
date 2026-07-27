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
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("تاییدکننده تجهیز سفارش ساخت", typeof(ProductionOrderEquipmentConfirmer))]
	public class ProductionOrderEquipmentConfirmerController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductionOrderEquipmentConfirmer productionOrderEquipmentConfirmer, CancellationToken cn)
		{
			if (productionOrderEquipmentConfirmer.Id == null || productionOrderEquipmentConfirmer.Id == 0)
			{
				return await Add(productionOrderEquipmentConfirmer, cn);
			}
			var exist = await unitOfWork.Repository<ProductionOrderEquipmentConfirmer>().TableNoTracking.AnyAsync(c => c.Id == productionOrderEquipmentConfirmer.Id);
			if (exist)
			{
				return await Update(productionOrderEquipmentConfirmer, cn);
			}
			return await Add(productionOrderEquipmentConfirmer, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProductionOrderEquipmentConfirmer productionOrderEquipmentConfirmer, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrderEquipmentConfirmer>().SaveAsync(productionOrderEquipmentConfirmer, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductionOrderEquipmentConfirmer productionOrderEquipmentConfirmer, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrderEquipmentConfirmer>().UpdateAsync(productionOrderEquipmentConfirmer, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProductionOrderEquipmentConfirmer>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProductionOrderEquipmentConfirmer>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProductionOrderEquipmentConfirmer>().TableNoTracking
					.Include(c => c.MechanicalUser)
					.Include(c => c.ElectricalUser)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Pln\ProductionOrderEquipmentConfirmer\Edit.cshtml", entity);
			}
			var newEntity = new ProductionOrderEquipmentConfirmer();
			return View(@"\Views\Panel\Pln\ProductionOrderEquipmentConfirmer\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ProductionOrderEquipmentConfirmer();
			return View(@"\Views\Panel\Pln\ProductionOrderEquipmentConfirmer\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Pln\ProductionOrderEquipmentConfirmer\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductionOrderEquipmentConfirmer>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<ProductionOrderEquipmentConfirmer>().FetchDataAsync(request, cn));
		}
	}
}
