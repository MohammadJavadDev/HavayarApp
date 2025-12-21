using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Eng;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Eng/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("دسته بندی پارت لیست", typeof(PartListGroup))]
	public class PartListGroupController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartListGroup partListGroup, CancellationToken cn)
		{
			if (partListGroup.Id == null || partListGroup.Id == 0)
			{
				return await Add(partListGroup, cn);
			}
			var exist = await unitOfWork.Repository<PartListGroup>().TableNoTracking.AnyAsync(c => c.Id == partListGroup.Id);
			if (exist)
			{
				return await Update(partListGroup, cn);
			}
			return await Add(partListGroup, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartListGroup partListGroup, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartListGroup>().SaveAsync(partListGroup, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PartListGroup partListGroup, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartListGroup>().UpdateAsync(partListGroup, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<PartListGroup>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<PartListGroup>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<PartListGroup>().TableNoTracking
					.Include(c => c.Product)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Eng\PartListGroup\Edit.cshtml", entity);
			}
			var newEntity = new PartListGroup();
			return View(@"\Views\Panel\Eng\PartListGroup\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new PartListGroup();
			return View(@"\Views\Panel\Eng\PartListGroup\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Eng\PartListGroup\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartListGroup>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<PartListGroup>().FetchDataAsync(request, cn));
		}
	}
}
