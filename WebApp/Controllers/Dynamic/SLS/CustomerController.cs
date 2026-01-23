using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.SLS;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مشتری", typeof(Customer))]
	public class CustomerController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Customer customer, CancellationToken cn)
		{
			if (customer.Id == null || customer.Id == 0)
			{
				return await Add(customer, cn);
			}
			var exist = await unitOfWork.Repository<Customer>().TableNoTracking.AnyAsync(c => c.Id == customer.Id);
			if (exist)
			{
				return await Update(customer, cn);
			}
			return await Add(customer, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Customer customer, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Customer>().SaveAsync(customer, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Customer customer, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Customer>().UpdateAsync(customer, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Customer>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Customer>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Customer>().TableNoTracking
					.Include(c => c.Party)
					.Include(c => c.Industry)
					.Include(c => c.Province)
					.Include(c => c.City)
					.Include(c => c.Area)
					.Include(c => c.AirRelation)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\SLS\Customer\Edit.cshtml", entity);
			}
			var newEntity = new Customer();
			return View(@"\Views\Panel\SLS\Customer\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new Customer();
			return View(@"\Views\Panel\SLS\Customer\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\SLS\Customer\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Customer>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Customer>().FetchDataAsync(request, cn));
		}
	}
}
