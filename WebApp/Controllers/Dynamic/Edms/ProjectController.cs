using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Edms;
using Entities.App.Hrm;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Edms/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پروژه ", typeof(Project))]
	public class ProjectController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Project project, CancellationToken cn)
		{
			project = await unitOfWork.Repository<Project>().SaveAsync(project, cn);
			return Ok(project);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Project project, CancellationToken cn)
		{
			 
			var entity = await unitOfWork.Repository<Project>().AddAsync(project, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Project project, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Project>().UpdateAsync(project, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Project>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Project>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Project>().TableNoTracking
					.Include(c=>c.ProgressPercentage)
					.Include(c => c.ProjectAttachments)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Edms\Project\Edit.cshtml", entity);
			}
			var newEntity = new Project();
			return View(@"\Views\Panel\Edms\Project\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new Project();
			return View(@"\Views\Panel\Edms\Project\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Edms\Project\List.cshtml");
		}


		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات پیوست پروژه", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ProjectAttachmentList()
		{

			return View(@"\Views\Panel\Edms\Project\ProjectAttachmentList.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Project>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Project>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public IActionResult ProjectProgressPercentagePartial()
		{
			return PartialView(@"\Views\Panel\Edms\Project\_ProjectProgressPercentagePartial.cshtml");
		}

		[HttpGet("[action]")]
		public IActionResult ProjectAttachmentsPartial()
		{
			return PartialView(@"\Views\Panel\Edms\Project\_ProjectAttachmentsPartial.cshtml");
		}


	}
}
