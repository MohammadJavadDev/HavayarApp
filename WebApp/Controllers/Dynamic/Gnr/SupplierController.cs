using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Gnr;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("تامین کننده", typeof(Supplier))]
	public class SupplierController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Supplier supplier, CancellationToken cn)
		{
			if (supplier.Id == null || supplier.Id == 0)
			{
				return await Add(supplier, cn);
			}
			var exist = await unitOfWork.Repository<Supplier>().TableNoTracking.AnyAsync(c => c.Id == supplier.Id);
			if (exist)
			{
				return await Update(supplier, cn);
			}
			return await Add(supplier, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Supplier supplier, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Supplier>().SaveAsync(supplier, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Supplier supplier, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Supplier>().UpdateAsync(supplier, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Supplier>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Supplier>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Supplier>().TableNoTracking
					.Include(c => c.Party)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Gnr\Supplier\Edit.cshtml", entity);
			}
			var newEntity = new Supplier();
			return View(@"\Views\Panel\Gnr\Supplier\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new Supplier();
			return View(@"\Views\Panel\Gnr\Supplier\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Gnr\Supplier\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Supplier>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Supplier>().FetchDataAsync(request, cn));
		}
	}
}
