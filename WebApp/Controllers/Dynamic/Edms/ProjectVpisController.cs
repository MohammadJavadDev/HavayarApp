using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
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
	[ControllerInfo("Vpis پروژه", typeof(ProjectVpis))]
	public class ProjectVpisController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProjectVpis projectVpis, CancellationToken cn)
		{
			if (projectVpis.Id == null || projectVpis.Id == 0)
			{
				return await Add(projectVpis, cn);
			}
			var exist = await unitOfWork.Repository<ProjectVpis>().TableNoTracking.AnyAsync(c => c.Id == projectVpis.Id);
			if (exist)
			{
				return await Update(projectVpis, cn);
			}
			return await Add(projectVpis, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProjectVpis projectVpis, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProjectVpis>().SaveAsync(projectVpis, cn, true);

			await unitOfWork.Repository<Document>().AddAsync(new()
			{
				DocumentVpisId = entity.Id,
				ProjectId = entity.ProjectNameId,
				Status = DocumentStatusEnums.NotIssue,
				Revision = 0,
				IsLatest = true,
				ApproverId = entity.ApproverId,
				ReviewerId = string.IsNullOrWhiteSpace(entity.ReviewersId)
					? null
					: entity.ReviewersId.Split(',')[0].ToInt(),
			}, cn);

			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProjectVpis projectVpis, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProjectVpis>().UpdateAsync(projectVpis, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProjectVpis>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProjectVpis>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProjectVpis>().TableNoTracking
					.Include(c => c.ProjectName)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Edms\ProjectVpis\Edit.cshtml", entity);
			}
			var newEntity = new ProjectVpis();
			return View(@"\Views\Panel\Edms\ProjectVpis\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ProjectVpis();
			return View(@"\Views\Panel\Edms\ProjectVpis\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Edms\ProjectVpis\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProjectVpis>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<ProjectVpis>().FetchDataAsync(request, cn));
		}
	}
}
