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
			if (project.Id == null || project.Id == 0)
			{
				return await Add(project, cn);
			}
			var exist = await unitOfWork.Repository<Project>().TableNoTracking.AnyAsync(c => c.Id == project.Id);
			if (exist)
			{
				return await Update(project, cn);
			}
			return await Add(project, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Project project, CancellationToken cn)
		{

			if(!project.Code.HasValue())
				project.Code = GetProjectCode(project);

			var entity = await unitOfWork.Repository<Project>().SaveAsync(project, cn, true);
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


		private   string GetOrganizationUnit(long organizationUnitId)
		{

			var orgUnitCode = unitOfWork.Repository<OrgUnit>()
				.TableNoTracking
				.Select(c => new { c.Id, c.ProjectPrefixCode })
				.FirstOrDefault(c => c.Id == organizationUnitId);

			if (orgUnitCode == null)
				throw new Exception("واحد سازمانی مورد نظر یافت نشد.");


			if (!orgUnitCode.ProjectPrefixCode.HasValue())
				throw new Exception("برای واحد سازمانی انتخاب شده پیش کد پروژه تعریف نشده .");

			return orgUnitCode.ProjectPrefixCode;



			//    1   مهندسی فروش NULL    121000002
			//    24  هئیت مدیره  NULL    121000082
			//    30  فروش گازهای صنعتی NULL    121000090
			//    31  فروش کمپرسورهای فرآیندی NULL    121000091
			//    32  فروش کمپرسورهای صنعتی NULL    121000092
			//    33  فروش تجهیزات پزشکی NULL    121000093
			//    140 فروش کمپرسورهای مهندسی NULL    121000133
			//    142 فروش توربو ماشین NULL    121000135
			//    225 فروش صنعتی  NULL    121000149

			switch (organizationUnitId)
			{
				case 1:
				case 140:
					return "EC";
				case 24:
					return "EN";
				case 30:
					return "IG";
				case 31:
					return "EN";
				case 32:
					return "IC";
				case 33:
					return "ME";
				case 41:
					return "TC";
				case 131:
					return "IG";
				case 142:
					return "TM";
				case 225:
					return "IS";
				case 226:
					return "HGI";
			}

	 
		}

		private string GetProjectCode( Project projectEntity)
		{
			//XX / XX / XX / 00
			//SP , VP / شمارنده اتوماتیک / سال / مخفف واحدهای فروش

			//SP: Special project
			//VP: Vendor project
			//TP: Takvin project
			 
				var latestOrganizationUnit = unitOfWork.Repository<Project>().TableNoTracking
					.Where(p => p.SubjectUnitId == projectEntity.SubjectUnitId)
				    .OrderByDescending(p => p.Id)
				    .FirstOrDefault();

				var latestOrganizationUnitCounterInText = latestOrganizationUnit != null ? latestOrganizationUnit.Code.Split('/').Last() : "00";

				var now = DateTime.Now;
				var year = now.GetShamsiYear();

				if (latestOrganizationUnit != null)
				{
					var latestOrganizationUnitCodeYear =  latestOrganizationUnit.Code.Split('/')[2];
						if(latestOrganizationUnitCodeYear != year.ToString())
							{
								latestOrganizationUnitCounterInText = "00";
							}
				}
				
				
				var projectType = projectEntity.IsTakvinProject ? "TP" : projectEntity.ProjectIsVendoriType ? "VP" : "SP";
				var counter = $"{(Convert.ToInt32(latestOrganizationUnitCounterInText) + 1):00}";

			SetAgian:

				var projectCode = $"{projectType}/{GetOrganizationUnit(projectEntity.SubjectUnitId)}/{now.GetShamsiYear()}/{counter}";

				var isAlreadyExist = unitOfWork.Repository<Project>().TableNoTracking.
					Any(p => p.Code == projectCode);
				if (!isAlreadyExist)
					return projectCode;


				counter += 1;
				goto SetAgian;
			 
			 
		}
	}
}
