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
	[ControllerInfo("حق ماموریت پرسنل", typeof(MissionSalaryPersonnel))]
	public class MissionSalaryPersonnelController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(MissionSalaryPersonnel partAgency, CancellationToken cn)
		{
			if (partAgency.Id == null || partAgency.Id == 0)
			{
				return await Add(partAgency, cn);
			}
			var exist = await unitOfWork.Repository<MissionSalaryPersonnel>().TableNoTracking.AnyAsync(c => c.Id == partAgency.Id);
			if (exist)
			{
				return await Update(partAgency, cn);
			}
			return await Add(partAgency, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(MissionSalaryPersonnel partAgency, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<MissionSalaryPersonnel>().SaveAsync(partAgency, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(MissionSalaryPersonnel partAgency, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<MissionSalaryPersonnel>().UpdateAsync(partAgency, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<MissionSalaryPersonnel>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<MissionSalaryPersonnel>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<MissionSalaryPersonnel>().TableNoTracking
				    .FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\ServiceRequest\MissionSalaryPersonnel\Edit.cshtml", entity);
			}
			var newEntity = new MissionSalaryPersonnel();
			return View(@"\Views\Panel\Sale\ServiceRequest\MissionSalaryPersonnel\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new MissionSalaryPersonnel();
			return View(@"\Views\Panel\Sale\ServiceRequest\MissionSalaryPersonnel\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\ServiceRequest\MissionSalaryPersonnel\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<MissionSalaryPersonnel>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<MissionSalaryPersonnel>().FetchDataAsync(request, cn));
		}

	}
}
