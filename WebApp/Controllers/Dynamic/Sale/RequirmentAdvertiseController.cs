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
	[ControllerInfo("اعلام نیازمندی", typeof(RequirmentAdvertise))]
	public class RequirmentAdvertiseController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(RequirmentAdvertise requirmentAdvertise, CancellationToken cn)
		{
			if (requirmentAdvertise.Id == null || requirmentAdvertise.Id == 0)
			{
				return await Add(requirmentAdvertise, cn);
			}
			var exist = await unitOfWork.Repository<RequirmentAdvertise>().TableNoTracking.AnyAsync(c => c.Id == requirmentAdvertise.Id);
			if (exist)
			{
				return await Update(requirmentAdvertise, cn);
			}
			return await Add(requirmentAdvertise, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(RequirmentAdvertise requirmentAdvertise, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<RequirmentAdvertise>().SaveAsync(requirmentAdvertise, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(RequirmentAdvertise requirmentAdvertise, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<RequirmentAdvertise>().UpdateAsync(requirmentAdvertise, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<RequirmentAdvertise>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<RequirmentAdvertise>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<RequirmentAdvertise>().TableNoTracking
					.Include(c => c.Items)
					.Include(c => c.OrganizationUnit)
					.Include(c => c.SalesExpert)
					.Include(c => c.SalesSupervisor)
					.Include(c => c.PersonOrCompany)
					.Include(c => c.SubIndustry)
					.Include(c => c.SalesRepresentative)
					.Include(c => c.ReferrerExpert)
					.Include(c => c.CountryOfInstallation)
					.Include(c => c.InstallationProvince)
					.Include(c => c.InstallationCity)
					.Include(c => c.Items)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\RequirmentAdvertise\Edit.cshtml", entity);
			}
			var newEntity = new RequirmentAdvertise();
			return View(@"\Views\Panel\Sale\RequirmentAdvertise\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new RequirmentAdvertise();
			return View(@"\Views\Panel\Sale\RequirmentAdvertise\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\RequirmentAdvertise\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<RequirmentAdvertise>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<RequirmentAdvertise>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public IActionResult RequirmentAdvertiseItemPartial()
		{
			return PartialView(@"\Views\Panel\Sale\RequirmentAdvertise\_RequirmentAdvertiseItemPartial.cshtml");
		}

		 
	}
}
