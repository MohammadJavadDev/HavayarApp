using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Inv;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Inv/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("تامین کنندگان محصولات", typeof(PartCompany))]
	public class PartCompanyController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartCompany partCompany, CancellationToken cn)
		{
			if (partCompany.Id == null || partCompany.Id == 0)
			{
				return await Add(partCompany, cn);
			}
			var exist = await unitOfWork.Repository<PartCompany>().TableNoTracking.AnyAsync(c => c.Id == partCompany.Id);
			if (exist)
			{
				return await Update(partCompany, cn);
			}
			return await Add(partCompany, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartCompany partCompany, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartCompany>().SaveAsync(partCompany, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PartCompany partCompany, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartCompany>().UpdateAsync(partCompany, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<PartCompany>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<PartCompany>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<PartCompany>().TableNoTracking
					.Include(c => c.Part)
					.Include(c => c.Supplier)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Inv\PartCompany\Edit.cshtml", entity);
			}
			var newEntity = new PartCompany();
			return View(@"\Views\Panel\Inv\PartCompany\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new PartCompany();
			return View(@"\Views\Panel\Inv\PartCompany\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Inv\PartCompany\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartCompany>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<PartCompany>().FetchDataAsync(request, cn));
		}
	}
}
