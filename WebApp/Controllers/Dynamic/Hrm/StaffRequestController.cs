using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Hrm.Enums;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using Services.Auth;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Hrm
{
	[Route("Panel/Hrm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مدیریت درخواست نیروی انسانی", typeof(StaffRequest))]
	public class StaffRequestController(IUnitOfWork unitOfWork, IWebHostEnvironment env, IUserService userService) : BaseController
	{
		public const string FullRole = "مدیرسیستم جذب واستخدام";
		public const string ViewRole = "دسترسی مشاهده-سیستم جذب و استخدام";
		public const string RequesterRole = "درخواست کنندگان نیرو";
		public const string EngineeringRole = "کارشناسان سیستم مهندسی دارای دسترسی سیستم منابع انسانی";

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			ViewBag.IsFull = IsAdministrator || CurrentUserHasRole(FullRole);
			ViewBag.IsViewer = CurrentUserHasRole(ViewRole);
			ViewBag.IsRequester = CurrentUserHasRole(RequesterRole);
			ViewBag.IsEngineering = CurrentUserHasRole(EngineeringRole);
			return View(@"\Views\Panel\Hrm\StaffRequest\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			var entity = id is > 0
				? await unitOfWork.Repository<StaffRequest>().TableNoTracking
					.Include(x => x.Languages).Include(x => x.Skills).Include(x => x.Softwares)
					.FirstOrDefaultAsync(x => x.Id == id, cn)
				: new StaffRequest { IsActive = IsActiveEnum.Active, RequiredNumber = 1 };
			if (entity == null) return NotFound();
			return View(@"\Views\Panel\Hrm\StaffRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Hrm\StaffRequest\Edit.cshtml", new StaffRequest { IsActive = IsActiveEnum.Active, RequiredNumber = 1 });

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public Task<IActionResult> Add(StaffRequest model, CancellationToken cn) => Save(model, true, false, cn);

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public Task<IActionResult> Update(StaffRequest model, CancellationToken cn) => Save(model, false, false, cn);

		[HttpPost("[action]")]
		public Task<IActionResult> Approve(long id, CancellationToken cn)
		{
			if (!(IsAdministrator || CurrentUserHasRole(FullRole)))
				return Task.FromResult<IActionResult>(BadRequest("تایید مدیر فقط برای مدیر سیستم جذب است"));
			return Save(new StaffRequest { Id = id, HasManagerAccept = true }, false, true, cn);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var used = await unitOfWork.Repository<Resume>().TableNoTracking.AnyAsync(x => x.StaffRequestId == id, cn);
			if (used) return BadRequest("برای این درخواست رزومه ثبت شده و حذف نمی‌شود");
			var entity = await unitOfWork.Repository<StaffRequest>().Table.FirstOrDefaultAsync(x => x.Id == id, cn);
			if (entity != null)
				await unitOfWork.Repository<StaffRequest>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			await unitOfWork.Repository<StaffRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
			memoryStream.Position = 0;
			return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "staffRequest.xlsx");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<StaffRequest>().FetchDataAsync(request, cn));

		[HttpGet("[action]")]
		public async Task<IActionResult> Lookups(CancellationToken cn)
		{
			var orgs = await unitOfWork.Repository<OrgUnit>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var jobs = await unitOfWork.Repository<Job>().TableNoTracking.Where(x => x.IsActive == IsActiveEnum.Active).OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var people = await unitOfWork.Repository<Personel>().TableNoTracking.OrderBy(x => x.Family).Select(x => new { x.Id, Title = (x.Name ?? "") + " " + (x.Family ?? "") }).ToListAsync(cn);
			var courses = await unitOfWork.Repository<StudyCourse>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var languages = await unitOfWork.Repository<TrainingLanguage>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var skills = await unitOfWork.Repository<Skill>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var softwares = await unitOfWork.Repository<TrainingSoftware>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			return Ok(new { orgs, jobs, people, courses, languages, skills, softwares });
		}

		private async Task<IActionResult> Save(StaffRequest model, bool isNew, bool approveOnly, CancellationToken cn)
		{
			StaffRequest entity;
			if (approveOnly)
			{
				entity = await unitOfWork.Repository<StaffRequest>().Table.FirstOrDefaultAsync(x => x.Id == model.Id, cn);
				if (entity == null) return NotFound();
				entity.HasManagerAccept = true;
				await unitOfWork.Repository<StaffRequest>().UpdateAsync(entity, cn, true);
				return Ok(entity);
			}

			if (model.RequestOrgUnitId is not > 0) return BadRequest("واحد سازمانی الزامی است");
			if (model.RequestPersonelId is not > 0) return BadRequest("پرسنل درخواست‌کننده الزامی است");
			if (model.JobId is not > 0) return BadRequest("شغل الزامی است");
			if (model.RequiredNumber == 0) return BadRequest("تعداد مورد نیاز الزامی است");
			if (model.CooperationType == null) return BadRequest("نوع همکاری الزامی است");
			if (model.EmploymentKind == null) return BadRequest("وضعیت جایگزینی الزامی است");
			if (model.Workplace == null) return BadRequest("محل کار الزامی است");
			if (string.IsNullOrWhiteSpace(model.TasksExpected)) return BadRequest("شرح وظایف الزامی است");
			if (model.ApproverPersonelId is not > 0) return BadRequest("تاییدکننده الزامی است");

			var orgTitle = CurrentOrganizationUnitId is > 0
				? await unitOfWork.Repository<OrgUnit>().TableNoTracking.Where(x => x.Id == CurrentOrganizationUnitId).Select(x => x.Title).FirstOrDefaultAsync(cn)
				: null;
			var isHr = string.Equals(orgTitle?.Trim(), "منابع انسانی", StringComparison.Ordinal);

			if (isNew)
			{
				model.Id = null;
				model.HtsId = 0;
				model.RequestUserId = CurrentUserId;
				model.RequestShamsiDate = DateTime.Now.ToShamsiDate();
				model.RequestTime = DateTime.Now.ToString("HH:mm");
				if (!isHr)
					model.Status = StaffRequestStatusEnum.InProgress;
				model.IsActive ??= IsActiveEnum.Active;
				var languages = model.Languages?.ToList() ?? [];
				var skills = model.Skills?.ToList() ?? [];
				var softwares = model.Softwares?.ToList() ?? [];
				model.Languages = [];
				model.Skills = [];
				model.Softwares = [];
				entity = await unitOfWork.Repository<StaffRequest>().AddAsync(model, cn, true);
				await ReplaceChildren(entity.Id!.Value, languages, skills, softwares, cn);
				await unitOfWork.SaveChangesAsync(cn);
				await PublishJob(entity.JobId, cn);
				await NotifyCreated(entity, cn);
				return Ok(entity);
			}

			entity = await unitOfWork.Repository<StaffRequest>().Table.FirstOrDefaultAsync(x => x.Id == model.Id, cn);
			if (entity == null) return NotFound();
			var becameHrReview = model.Status == StaffRequestStatusEnum.HrReview && entity.Status != StaffRequestStatusEnum.HrReview;
			Copy(entity, model);
			await unitOfWork.Repository<StaffRequest>().UpdateAsync(entity, cn, false);
			await ReplaceChildren(entity.Id!.Value, model.Languages?.ToList() ?? [], model.Skills?.ToList() ?? [], model.Softwares?.ToList() ?? [], cn);
			await unitOfWork.SaveChangesAsync(cn);
			if (becameHrReview)
				await NotifyHrReview(entity, cn);
			return Ok(entity);
		}

		private static void Copy(StaffRequest target, StaffRequest source)
		{
			target.RequestOrgUnitId = source.RequestOrgUnitId;
			target.RequestPersonelId = source.RequestPersonelId;
			target.ApproverPersonelId = source.ApproverPersonelId;
			target.JobId = source.JobId;
			target.RequiredNumber = source.RequiredNumber;
			target.CooperationType = source.CooperationType;
			target.Sex = source.Sex;
			target.MaritalStatus = source.MaritalStatus;
			target.FromAge = source.FromAge;
			target.ToAge = source.ToAge;
			target.EmploymentKind = source.EmploymentKind;
			target.ReplacePersonelId = source.ReplacePersonelId;
			target.ReplacePersonelTitle = source.ReplacePersonelTitle;
			target.ExperienceRequired = source.ExperienceRequired;
			target.EducationDiploma = source.EducationDiploma;
			target.EducationAssociate = source.EducationAssociate;
			target.EducationBachelor = source.EducationBachelor;
			target.EducationMaster = source.EducationMaster;
			target.StudyCourseOneId = source.StudyCourseOneId;
			target.StudyCourseTwoId = source.StudyCourseTwoId;
			target.StudyCourseThreeId = source.StudyCourseThreeId;
			target.Workplace = source.Workplace;
			target.TasksExpected = source.TasksExpected;
			target.PersonalityFeature = source.PersonalityFeature;
			target.Comment = source.Comment;
			target.Status = source.Status;
			target.HiddenFromSupervisor = source.HiddenFromSupervisor;
			if (source.HasManagerAccept == true)
				target.HasManagerAccept = true;
		}

		private async Task ReplaceChildren(long id, List<StaffRequestLanguage> languages, List<StaffRequestSkill> skills, List<StaffRequestSoftware> softwares, CancellationToken cn)
		{
			foreach (var row in await unitOfWork.Repository<StaffRequestLanguage>().Table.Where(x => x.StaffRequestId == id).ToListAsync(cn))
				await unitOfWork.Repository<StaffRequestLanguage>().DeleteAsync(row, cn, false);
			foreach (var row in await unitOfWork.Repository<StaffRequestSkill>().Table.Where(x => x.StaffRequestId == id).ToListAsync(cn))
				await unitOfWork.Repository<StaffRequestSkill>().DeleteAsync(row, cn, false);
			foreach (var row in await unitOfWork.Repository<StaffRequestSoftware>().Table.Where(x => x.StaffRequestId == id).ToListAsync(cn))
				await unitOfWork.Repository<StaffRequestSoftware>().DeleteAsync(row, cn, false);
			foreach (var row in languages)
			{
				row.Id = null;
				row.HtsId = 0;
				row.StaffRequestId = id;
				row.IsActive ??= IsActiveEnum.Active;
				await unitOfWork.Repository<StaffRequestLanguage>().AddAsync(row, cn, false);
			}
			foreach (var row in skills)
			{
				row.Id = null;
				row.HtsId = 0;
				row.StaffRequestId = id;
				row.IsActive ??= IsActiveEnum.Active;
				await unitOfWork.Repository<StaffRequestSkill>().AddAsync(row, cn, false);
			}
			foreach (var row in softwares)
			{
				row.Id = null;
				row.HtsId = 0;
				row.StaffRequestId = id;
				row.IsActive ??= IsActiveEnum.Active;
				await unitOfWork.Repository<StaffRequestSoftware>().AddAsync(row, cn, false);
			}
		}

		private async Task PublishJob(long? jobId, CancellationToken cn)
		{
			if (jobId is not > 0) return;
			var job = await unitOfWork.Repository<Job>().Table.FirstOrDefaultAsync(x => x.Id == jobId, cn);
			if (job == null || job.ShowPublic == true) return;
			job.ShowPublic = true;
			await unitOfWork.Repository<Job>().UpdateAsync(job, cn, true);
		}

		private async Task NotifyCreated(StaffRequest entity, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<StaffRequest>().TableNoTracking
				.Include(x => x.RequestOrgUnit).Include(x => x.RequestPersonel).Include(x => x.Job).Include(x => x.RequestUser)
				.FirstOrDefaultAsync(x => x.Id == entity.Id, cn);
			if (row == null) return;
			var body = new StringBuilder();
			body.Append("<div style='direction:rtl;text-align:right'>با سلام و احترام<br/>به استحضار می‌رساند درخواست نیروی جدید ثبت گردید<ul>");
			body.Append("<li>واحد سازمانی: ").Append(WebUtility.HtmlEncode(row.RequestOrgUnit?.Title)).Append("</li>");
			body.Append("<li>درخواست‌کننده: ").Append(WebUtility.HtmlEncode((row.RequestPersonel?.Name + " " + row.RequestPersonel?.Family)?.Trim())).Append("</li>");
			body.Append("<li>شغل: ").Append(WebUtility.HtmlEncode(row.Job?.Title)).Append("</li>");
			body.Append("<li>تعداد نیاز: ").Append(row.RequiredNumber).Append("</li>");
			body.Append("<li>شرح وظایف: ").Append(WebUtility.HtmlEncode(row.TasksExpected)).Append("</li>");
			body.Append("</ul></div>");
			var to = "moeini.m@havayar.com;Asgharzadeh.e@havayar.com;Hosseini.he@havayar.com";
			if (!string.IsNullOrWhiteSpace(row.RequestUser?.Email))
				to += ";" + row.RequestUser.Email;
			else if (!string.IsNullOrWhiteSpace(CurrentUserName))
				to += ";" + CurrentUserName + "@havayar.com";
			await Queue(row.Id!.Value, "درخواست نیرو", body.ToString(), to, cn);
		}

		private async Task NotifyHrReview(StaffRequest entity, CancellationToken cn)
		{
			var job = await unitOfWork.Repository<Job>().TableNoTracking.Where(x => x.Id == entity.JobId).Select(x => x.Title).FirstOrDefaultAsync(cn);
			var user = await userService.TableNoTracking.Where(x => x.Id == entity.RequestUserId).Select(x => new { x.Email, x.Username }).FirstOrDefaultAsync(cn);
			var mail = user?.Email;
			if (string.IsNullOrWhiteSpace(mail) && !string.IsNullOrWhiteSpace(user?.Username))
				mail = user.Username + "@havayar.com";
			await Queue(entity.Id!.Value, "درخواست نیرو", "<div style='direction:rtl'>درخواست نیروی شما جهت شغل " + WebUtility.HtmlEncode(job) + " توسط منابع انسانی در حال بررسی است.</div>", mail, cn);
		}

		private async Task Queue(long id, string subject, string body, string? to, CancellationToken cn)
		{
			var tos = (to ?? "").Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Contains('@')).Distinct().ToList();
			if (tos.Count == 0) return;
			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = subject,
				Body = body,
				EntityId = id,
				OwnerId = CurrentUserId ?? 1,
				ViewPath = "/Panel/Hrm/StaffRequest/List",
				ToEmails = tos,
				IsSend = false,
				IsRead = false
			}, cn, true);
		}
	}
}
