using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Hrm.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Notification;
using Entities.Base.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Sms;
using System.Net;
using System.Text;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Hrm
{
	[Route("Panel/Hrm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مدیریت رزومه (New)", typeof(Resume))]
	public partial class ResumeController(IUnitOfWork unitOfWork, IWebHostEnvironment env, ISmsService sms) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			ViewBag.IsAdmin = IsAdministrator;
			ViewBag.IsExpert = IsAdministrator || CurrentUserHasRole(ResumeRoles.Expert);
			ViewBag.IsUnitManager = CurrentUserHasRole(ResumeRoles.UnitManager);
			ViewBag.IsHrmManager = CurrentUserHasRole(ResumeRoles.HrmManager);
			ViewBag.IsBoard = CurrentUserHasRole(ResumeRoles.Board);
			ViewBag.IsOffice = CurrentUserHasRole(ResumeRoles.Office);
			return View(@"\Views\Panel\Hrm\Resume\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, long? copyFrom, CancellationToken cn)
		{
			var sourceId = id is > 0 ? id : copyFrom;
			var entity = sourceId is > 0 ? await LoadAsync(sourceId.Value, cn) : NewResume();
			if (entity == null) return NotFound();
			if (copyFrom is > 0)
			{
				entity.Id = null;
				entity.HtsId = 0;
				entity.ResumeFileId = null;
				entity.ResumeFileAddress = null;
				foreach (var row in entity.Languages) { row.Id = null; row.HtsId = 0; }
				foreach (var row in entity.Softwares) { row.Id = null; row.HtsId = 0; }
				foreach (var row in entity.Skills) { row.Id = null; row.HtsId = 0; }
				foreach (var row in entity.WorkExperiences) { row.Id = null; row.HtsId = 0; }
			}
			return View(@"\Views\Panel\Hrm\Resume\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New() => View(@"\Views\Panel\Hrm\Resume\Edit.cshtml", NewResume());

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public Task<IActionResult> Add(Resume model, CancellationToken cn) => Save(model, true, cn);

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public Task<IActionResult> Update(Resume model, CancellationToken cn) => Save(model, false, cn);

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Resume>().Table.FirstOrDefaultAsync(x => x.Id == id, cn);
			if (entity != null)
				await unitOfWork.Repository<Resume>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			await unitOfWork.Repository<Resume>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
			memoryStream.Position = 0;
			return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "resume.xlsx");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<Resume>().FetchDataAsync(request, cn));

		[HttpGet("[action]")]
		public async Task<IActionResult> Lookups(CancellationToken cn)
		{
			var courses = await unitOfWork.Repository<StudyCourse>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var trends = await unitOfWork.Repository<TrendOfStudyCourse>().TableNoTracking.Select(x => new { x.Id, x.Title, x.StudyCourseId }).ToListAsync(cn);
			var posts = await unitOfWork.Repository<OrgPost>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var cities = await unitOfWork.Repository<Entities.App.Gnr.Region>().TableNoTracking.Where(x => x.Type == Entities.App.Gnr.Enums.RegionTypeEnum.City).OrderBy(x => x.Name).Select(x => new { x.Id, Title = x.Name }).ToListAsync(cn);
			var sources = await unitOfWork.Repository<IncomingSource>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var universities = await unitOfWork.Repository<University>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var jobs = await unitOfWork.Repository<Job>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var orgs = await unitOfWork.Repository<OrgUnit>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var people = await unitOfWork.Repository<Personel>().TableNoTracking.Where(x => x.IsActive == IsActiveEnum.Active).OrderBy(x => x.Family).Select(x => new { x.Id, Title = (x.Name ?? "") + " " + (x.Family ?? "") }).ToListAsync(cn);
			var languages = await unitOfWork.Repository<TrainingLanguage>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var softwares = await unitOfWork.Repository<TrainingSoftware>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var skills = await unitOfWork.Repository<Skill>().TableNoTracking.OrderBy(x => x.Title).Select(x => new { x.Id, x.Title }).ToListAsync(cn);
			var requests = await unitOfWork.Repository<StaffRequest>().TableNoTracking
				.OrderByDescending(x => x.HtsId)
				.Select(x => new { x.Id, Text = x.HtsId + " | " + (x.RequestOrgUnit != null ? x.RequestOrgUnit.Title : "") + " | " + (x.RequestPersonel != null ? (x.RequestPersonel.Name + " " + x.RequestPersonel.Family) : "") + " | " + (x.Job != null ? x.Job.Title : "") })
				.ToListAsync(cn);
			return Ok(new { courses, trends, posts, cities, sources, universities, jobs, orgs, people, languages, softwares, skills, requests });
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> DownloadResume(long id, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().TableNoTracking
				.Include(x => x.ResumeFile)
				.FirstOrDefaultAsync(x => x.Id == id, cn);
			if (row == null) return NotFound();
			var path = row.ResumeFile?.PhysicalPath ?? row.ResumeFileAddress;
			if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
				return BadRequest("فایل رزومه پیدا نشد");
			var name = $"{row.Name} {row.Family}.pdf";
			return PhysicalFile(path, "application/pdf", name);
		}

		[HttpGet("[action]")]
		public IActionResult Print(long id)
		{
			var filter = "{\"values\":[\"" + id + "\"],\"condition\":\"=\"}";
			return Ok(new { url = "/System/ReportBuilder/ViewReportByName/HrmResumePrint?resumeId=" + Uri.EscapeDataString(filter) + "&autoRun=True" });
		}

		private async Task<IActionResult> Save(Resume model, bool isNew, CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Family))
				return BadRequest("نام و نام خانوادگی الزامی است");
			if (string.IsNullOrWhiteSpace(model.MobileNumber))
				return BadRequest("موبایل الزامی است");
			model.MobileNumber = model.MobileNumber.Trim();
			var dup = await unitOfWork.Repository<Resume>().TableNoTracking
				.AnyAsync(x => x.MobileNumber == model.MobileNumber && x.Id != model.Id, cn);
			if (isNew && dup)
				return BadRequest("شماره موبایل تکراری است");

			Resume? existing = null;
			if (!isNew)
			{
				existing = await unitOfWork.Repository<Resume>().Table.FirstOrDefaultAsync(x => x.Id == model.Id, cn);
				if (existing == null) return NotFound();
				if (existing.StaffRequestId != model.StaffRequestId)
					await AddComment(existing.Id!.Value, "ویرایش درخواست نیرو", await StaffRequestComment(model.StaffRequestId, cn), null, cn);
				CopyEditable(existing, model);
				await unitOfWork.Repository<Resume>().UpdateAsync(existing, cn, false);
				await ReplaceChildren(existing.Id!.Value, model, cn);
			}
			else
			{
				model.Id = null;
				model.HtsId = 0;
				model.IsActive ??= IsActiveEnum.Active;
				var languages = model.Languages?.ToList() ?? [];
				var softwares = model.Softwares?.ToList() ?? [];
				var skills = model.Skills?.ToList() ?? [];
				var works = model.WorkExperiences?.ToList() ?? [];
				model.Languages = [];
				model.Softwares = [];
				model.Skills = [];
				model.WorkExperiences = [];
				var saved = await unitOfWork.Repository<Resume>().AddAsync(model, cn, true);
				saved.Languages = languages;
				saved.Softwares = softwares;
				saved.Skills = skills;
				saved.WorkExperiences = works;
				saved.Education = model.Education;
				saved.StudyCourseId = model.StudyCourseId;
				saved.TrendOfStudyCourseId = model.TrendOfStudyCourseId;
				await ReplaceChildren(saved.Id!.Value, saved, cn);
				existing = saved;
			}

			await unitOfWork.SaveChangesAsync(cn);
			return Ok(existing);
		}

		private static void CopyEditable(Resume target, Resume source)
		{
			target.StaffRequestId = source.StaffRequestId;
			target.Name = source.Name;
			target.Family = source.Family;
			target.Sex = source.Sex;
			target.Education = source.Education;
			target.StudyCourseId = source.StudyCourseId;
			target.TrendOfStudyCourseId = source.TrendOfStudyCourseId;
			target.WorkExperience = source.WorkExperience;
			target.PhoneNumber = source.PhoneNumber;
			target.MobileNumber = source.MobileNumber;
			target.StudentTerm = source.StudentTerm;
			target.BirthDate = source.BirthDate;
			target.EmailAddress = source.EmailAddress;
			target.InterestedPostId = source.InterestedPostId;
			target.CityId = source.CityId;
			target.IncomingSourceId = source.IncomingSourceId;
			target.RepresentingPersonelId = source.RepresentingPersonelId;
			target.UniversityId = source.UniversityId;
			target.Organization = source.Organization;
			target.ResumeStatus = source.ResumeStatus;
			target.ResumeReviewDeadline = source.ResumeReviewDeadline;
			target.SuitableForJobId = source.SuitableForJobId;
			if (!string.IsNullOrWhiteSpace(source.ResumeFileAddress))
				target.ResumeFileAddress = source.ResumeFileAddress;
		}

		private async Task ReplaceChildren(long resumeId, Resume model, CancellationToken cn)
		{
			await Clear<ResumeLanguage>(resumeId, cn);
			await Clear<ResumeSoftware>(resumeId, cn);
			await Clear<ResumeSkill>(resumeId, cn);
			await Clear<ResumeWorkExperience>(resumeId, cn);
			await Clear<ResumeEducationalStatus>(resumeId, cn);

			foreach (var row in model.Languages ?? [])
				await unitOfWork.Repository<ResumeLanguage>().AddAsync(ResetChild(row, resumeId), cn, false);
			foreach (var row in model.Softwares ?? [])
				await unitOfWork.Repository<ResumeSoftware>().AddAsync(ResetChild(row, resumeId), cn, false);
			foreach (var row in model.Skills ?? [])
				await unitOfWork.Repository<ResumeSkill>().AddAsync(ResetChild(row, resumeId), cn, false);
			foreach (var row in model.WorkExperiences ?? [])
				await unitOfWork.Repository<ResumeWorkExperience>().AddAsync(ResetChild(row, resumeId), cn, false);

			if (model.Education != null)
			{
				await unitOfWork.Repository<ResumeEducationalStatus>().AddAsync(new ResumeEducationalStatus
				{
					HtsId = 0,
					ResumeId = resumeId,
					Degree = model.Education,
					HtsDegreeId = (int)model.Education,
					StudyCourseId = model.StudyCourseId,
					TrendOfStudyCourseId = model.TrendOfStudyCourseId,
					IsStudent = false,
					IsActive = IsActiveEnum.Active
				}, cn, false);
			}
		}

		private static T ResetChild<T>(T row, long resumeId) where T : BaseEntity, new()
		{
			row.Id = null;
			row.IsActive ??= IsActiveEnum.Active;
			switch (row)
			{
				case ResumeLanguage x: x.HtsId = 0; x.ResumeId = resumeId; break;
				case ResumeSoftware x: x.HtsId = 0; x.ResumeId = resumeId; break;
				case ResumeSkill x: x.HtsId = 0; x.ResumeId = resumeId; break;
				case ResumeWorkExperience x: x.HtsId = 0; x.ResumeId = resumeId; break;
			}
			return row;
		}

		private async Task Clear<T>(long resumeId, CancellationToken cn) where T : BaseEntity, new()
		{
			var rows = await unitOfWork.Repository<T>().Table.Where(x => EF.Property<long>(x, "ResumeId") == resumeId).ToListAsync(cn);
			foreach (var row in rows)
				await unitOfWork.Repository<T>().DeleteAsync(row, cn, false);
		}

		private async Task<Resume?> LoadAsync(long id, CancellationToken cn)
			=> await unitOfWork.Repository<Resume>().TableNoTracking
				.Include(x => x.Languages)
				.Include(x => x.Softwares)
				.Include(x => x.Skills)
				.Include(x => x.WorkExperiences)
				.FirstOrDefaultAsync(x => x.Id == id, cn);

		private static Resume NewResume() => new() { IsActive = IsActiveEnum.Active };

		private async Task AddComment(long resumeId, string title, string? comment, int? statusId, CancellationToken cn)
		{
			var now = DateTime.Now;
			await unitOfWork.Repository<ResumeComment>().AddAsync(new ResumeComment
			{
				HtsId = 0,
				ResumeId = resumeId,
				Title = title,
				Comment = string.IsNullOrWhiteSpace(comment) ? title : comment.Trim(),
				HtsStatusId = statusId,
				CreatedDateTimeInText = now.ToShamsiDateTime(),
				IsActive = IsActiveEnum.Active
			}, cn, true);
		}

		private async Task<string> StaffRequestComment(long? staffRequestId, CancellationToken cn)
		{
			if (staffRequestId is not > 0) return "درخواست نیرو حذف شد";
			var row = await unitOfWork.Repository<StaffRequest>().TableNoTracking
				.Include(x => x.RequestOrgUnit)
				.Include(x => x.RequestPersonel)
				.Include(x => x.Job)
				.FirstOrDefaultAsync(x => x.Id == staffRequestId, cn);
			if (row == null) return "درخواست نیرو";
			var person = row.RequestPersonel == null ? "" : row.RequestPersonel.Name + " " + row.RequestPersonel.Family;
			return "درخواست نیروی ویرایش شده: " + row.HtsId + " | " + row.RequestOrgUnit?.Title + " | " + person + " | " + row.Job?.Title;
		}

		private async Task QueueEmail(string subject, string body, string? to, string? cc, long resumeId, CancellationToken cn)
		{
			var tos = SplitMails(to);
			if (tos.Count == 0) return;
			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = subject,
				Body = body,
				EntityId = resumeId,
				OwnerId = CurrentUserId ?? 1,
				ViewPath = "/Panel/Hrm/Resume/List",
				IsRead = false,
				IsSend = false,
				ToEmails = tos,
				CcEmails = SplitMails(cc)
			}, cn, true);
		}

		private static List<string> SplitMails(string? raw)
			=> (raw ?? "").Split(new[] { ';', ',', '،' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.Trim())
				.Where(x => x.Contains('@'))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

		private static string H(string? value) => WebUtility.HtmlEncode(value ?? "");

		private static string MailShell(string inner)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='672'>");
			sb.AppendLine("<tr style='background:#000aa0'><td colspan='2'><div style='font-size:14pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار<br/>اعلان تغییر وضعیت رزومه</div></td></tr>");
			sb.AppendLine("<tr><td colspan='2'><div style='font-size:14pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>با سلام و احترام <br />");
			sb.AppendLine(inner);
			sb.AppendLine("<br/></div></td></tr></table></div>");
			return sb.ToString();
		}
	}
}
