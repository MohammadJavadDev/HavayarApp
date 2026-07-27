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
	[ControllerInfo("تحویل محصول به مشتری", typeof(ProductDeliveryCustomer))]
	public class ProductDeliveryCustomerController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductDeliveryCustomer serviceRequestPart, CancellationToken cn)
		{
			if (serviceRequestPart.Id == null || serviceRequestPart.Id == 0)
			{
				return await Add(serviceRequestPart, cn);
			}
			var exist = await unitOfWork.Repository<ProductDeliveryCustomer>().TableNoTracking.AnyAsync(c => c.Id == serviceRequestPart.Id);
			if (exist)
			{
				return await Update(serviceRequestPart, cn);
			}
			return await Add(serviceRequestPart, cn);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> Add(ProductDeliveryCustomer serviceRequestPart, CancellationToken cn)
		{
			try
			{
				var entity = await unitOfWork.Repository<ProductDeliveryCustomer>().SaveAsync(serviceRequestPart, cn);
				return Ok(entity);
			}
			catch (DbUpdateException ex)
			{
				var message = ex.InnerException?.Message ?? ex.Message;
				return BadRequest(new { Error = "خطا در ذخیره‌سازی", Details = message });
			}
		}


		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductDeliveryCustomer serviceRequestPart, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductDeliveryCustomer>().UpdateAsync(serviceRequestPart, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProductDeliveryCustomer>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProductDeliveryCustomer>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProductDeliveryCustomer>().TableNoTracking
				    .FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\ServiceRequest\PDC\ProductDeliveryCustomer\Edit.cshtml", entity);
			}
			var newEntity = new ProductDeliveryCustomer();
			return View(@"\Views\Panel\Sale\ServiceRequest\PDC\ProductDeliveryCustomer\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ProductDeliveryCustomer();
			return View(@"\Views\Panel\Sale\ServiceRequest\PDC\ProductDeliveryCustomer\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\ServiceRequest\PDC\ProductDeliveryCustomer\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductDeliveryCustomer>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<ProductDeliveryCustomer>().FetchDataAsync(request, cn));
		}

	}
}
