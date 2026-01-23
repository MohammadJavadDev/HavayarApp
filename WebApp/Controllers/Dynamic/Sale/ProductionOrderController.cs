using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Sale;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("سفارش ساخت", typeof(ProductionOrder))]
	public class ProductionOrderController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductionOrder productionOrder, CancellationToken cn)
		{
			if (productionOrder.Id == null || productionOrder.Id == 0)
			{
				return await Add(productionOrder, cn);
			}
			var exist = await unitOfWork.Repository<ProductionOrder>().TableNoTracking.AnyAsync(c => c.Id == productionOrder.Id);
			if (exist)
			{
				return await Update(productionOrder, cn);
			}
			return await Add(productionOrder, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProductionOrder productionOrder, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrder>().SaveAsync(productionOrder, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductionOrder productionOrder, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrder>().UpdateAsync(productionOrder, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProductionOrder>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProductionOrder>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProductionOrder>().TableNoTracking
					.Include(c => c.Contract)
					.Include(c => c.ProjectDl)
					.Include(c => c.SalesManager)
					.Include(c => c.ProjectManager)
					.Include(c => c.AlternativeExpert)
					.Include(c => c.SalesAgency)
					.Include(c => c.InstallationCity)
					.Include(c => c.SalesExpert)
					.Include(c => c.IntroducerExpert)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\ProductionOrder\Edit.cshtml", entity);
			}
			var newEntity = new ProductionOrder();
			return View(@"\Views\Panel\Sale\ProductionOrder\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ProductionOrder();
			return View(@"\Views\Panel\Sale\ProductionOrder\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\ProductionOrder\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductionOrder>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<ProductionOrder>().FetchDataAsync(request, cn));
		}
	}
}
