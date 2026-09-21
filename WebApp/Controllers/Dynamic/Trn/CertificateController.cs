using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Trn;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Actions.Trn;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Trn/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("گواهی‌نامه‌های دوره", typeof(Certificate))]
	public class CertificateController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Certificate certificate, CancellationToken cn)
		{
			if (certificate.Id == null || certificate.Id == 0)
			{
				return await Add(certificate, cn);
			}
			var exist = await unitOfWork.Repository<Certificate>().TableNoTracking.AnyAsync(c => c.Id == certificate.Id);
			if (exist)
			{
				return await Update(certificate, cn);
			}
			return await Add(certificate, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Certificate certificate, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Certificate>().SaveAsync(certificate, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Certificate certificate, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Certificate>().UpdateAsync(certificate, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Certificate>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Certificate>().DeleteAsync(model, cn, true);
			return Ok();
		}

		// "صدور گواهی" — creates the Certificate row via R7+R8, then the page opens
		// the matching ReportBuilder template for preview/print (template itself comes in Step 10).
		[HttpPost("[action]")]
		[ActionDisplayName("صدور گواهی", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Issue(CertificateIssueRequest request, CancellationToken cn)
		{
			if ((request.CourseParticipantId == null || request.CourseParticipantId == 0)
				&& (request.CourseCompanyParticipantId == null || request.CourseCompanyParticipantId == 0))
				return BadRequest("ردیف ثبت‌نام مشخص نشده است.");

			var certificate = new Certificate
			{
				CourseParticipantId = request.CourseParticipantId != 0 ? request.CourseParticipantId : null,
				CourseCompanyParticipantId = request.CourseCompanyParticipantId != 0 ? request.CourseCompanyParticipantId : null
			};
			var entity = await unitOfWork.Repository<Certificate>().SaveAsync(certificate, cn, true);
			return Ok(new { certificateId = entity.Id, templateKey = entity.TemplateKey, certificateNo = entity.CertificateNo });
		}

		[HttpPost("[action]")]
		[ActionDisplayName("صدور همه گواهی‌ها", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> IssueAll(long courseId, CancellationToken cn)
		{
			if (courseId == 0)
				return BadRequest("شناسه دوره نمیتواند خالی باشد.");

			var publicMissing = await unitOfWork.Repository<CourseParticipant>().TableNoTracking
				.Where(p => p.CourseId == courseId && p.Certificate == null)
				.Select(p => p.Id)
				.ToListAsync(cn);

			var privateMissing = await unitOfWork.Repository<CourseCompanyParticipant>().TableNoTracking
				.Where(p => p.CourseId == courseId && p.Certificate == null)
				.Select(p => p.Id)
				.ToListAsync(cn);

			var issued = 0;
			var failed = 0;
			string? lastError = null;

			foreach (var enrollmentId in publicMissing)
			{
				try
				{
					await unitOfWork.Repository<Certificate>().SaveAsync(
						new Certificate { CourseParticipantId = enrollmentId }, cn, true);
					issued++;
				}
				catch (Exception ex)
				{
					failed++;
					lastError = ex.Message;
				}
			}

			foreach (var enrollmentId in privateMissing)
			{
				try
				{
					await unitOfWork.Repository<Certificate>().SaveAsync(
						new Certificate { CourseCompanyParticipantId = enrollmentId }, cn, true);
					issued++;
				}
				catch (Exception ex)
				{
					failed++;
					lastError = ex.Message;
				}
			}

			var templateKey = await ResolveCourseTemplateKeyAsync(courseId, cn);
			return Ok(new
			{
				issuedCount = issued,
				failedCount = failed,
				templateKey,
				message = failed > 0 ? lastError : null
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Certificate>().TableNoTracking
					.Include(c => c.CourseParticipant).ThenInclude(p => p != null ? p.Participant : null)
					.Include(c => c.CourseCompanyParticipant).ThenInclude(p => p != null ? p.Participant : null)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\Certificate\Edit.cshtml", entity);
			}
			var newEntity = new Certificate();
			return View(@"\Views\Panel\Trn\Certificate\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new Certificate();
			return View(@"\Views\Panel\Trn\Certificate\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست گواهی‌های دوره", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByCourseId(long courseId)
		{
			if (courseId == 0)
				throw new Exception("شناسه دوره نمیتواند خالی باشد.");

			var course = unitOfWork.Repository<Course>().TableNoTracking
				.FirstOrDefault(c => c.Id == courseId);

			var model = new CertificateListByParentViewModel
			{
				CourseId = courseId,
				CourseTitle = course != null ? course.TrainingCode ?? string.Empty : string.Empty
			};

			return View(@"\Views\Panel\Trn\Certificate\ListByCourseId.cshtml", model);
		}

		// Union of both tracks for the course: one row per enrolled participant with
		// certificate info attached when already issued (empty otherwise).
		[HttpGet("[action]")]
		[ActionDisplayName("دریافت گواهی‌های دوره", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? courseId, CancellationToken cn)
		{
			if (courseId == null || courseId == 0)
				return BadRequest("شناسه دوره نمیتواند خالی باشد.");

			var publicRows = await unitOfWork.Repository<CourseParticipant>()
				.TableNoTracking
				.Where(c => c.CourseId == courseId)
				.Select(c => new CertificateListItemViewModel
				{
					EnrollmentKind = "عمومی",
					CourseParticipantId = c.Id,
					CourseCompanyParticipantId = null,
					FirstName = c.Participant != null ? c.Participant.FirstName : null,
					LastName = c.Participant != null ? c.Participant.LastName : null,
					NationalCode = c.Participant != null ? c.Participant.NationalCode : null,
					CompanyTitle = c.Company != null
						? c.Company.Title
						: (c.Participant != null && c.Participant.Company != null ? c.Participant.Company.Title : null),
					CertificateId = c.Certificate != null ? c.Certificate.Id : null,
					CertificateNo = c.Certificate != null ? c.Certificate.CertificateNo : null,
					TemplateKey = c.Certificate != null ? c.Certificate.TemplateKey : null,
					IssueShamsiDate = c.Certificate != null ? c.Certificate.IssueShamsiDate : null
				})
				.ToListAsync(cn);

			var privateRows = await unitOfWork.Repository<CourseCompanyParticipant>()
				.TableNoTracking
				.Where(c => c.CourseId == courseId)
				.Select(c => new CertificateListItemViewModel
				{
					EnrollmentKind = "اختصاصی",
					CourseParticipantId = null,
					CourseCompanyParticipantId = c.Id,
					FirstName = c.Participant != null ? c.Participant.FirstName : null,
					LastName = c.Participant != null ? c.Participant.LastName : null,
					NationalCode = c.Participant != null ? c.Participant.NationalCode : null,
					CompanyTitle = c.Course != null && c.Course.Company != null
						? c.Course.Company.Title
						: (c.Participant != null && c.Participant.Company != null ? c.Participant.Company.Title : null),
					CertificateId = c.Certificate != null ? c.Certificate.Id : null,
					CertificateNo = c.Certificate != null ? c.Certificate.CertificateNo : null,
					TemplateKey = c.Certificate != null ? c.Certificate.TemplateKey : null,
					IssueShamsiDate = c.Certificate != null ? c.Certificate.IssueShamsiDate : null
				})
				.ToListAsync(cn);

			return Ok(publicRows.Concat(privateRows).ToList());
		}

		private async Task<string?> ResolveCourseTemplateKeyAsync(long courseId, CancellationToken cn)
		{
			var issuedKey = await unitOfWork.Repository<Certificate>().TableNoTracking
				.Where(c =>
					(c.CourseParticipant != null && c.CourseParticipant.CourseId == courseId)
					|| (c.CourseCompanyParticipant != null && c.CourseCompanyParticipant.CourseId == courseId))
				.Select(c => c.TemplateKey)
				.FirstOrDefaultAsync(cn);
			if (!string.IsNullOrWhiteSpace(issuedKey))
				return issuedKey;

			var course = await unitOfWork.Repository<Course>().TableNoTracking
				.Include(c => c.CourseBank)
				.FirstOrDefaultAsync(c => c.Id == courseId, cn);
			if (course == null)
				return null;

			return CertificateAction.ResolveTemplateKey(course, course.CourseBank, !course.IsPublicParticipantTrack());
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Certificate>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Certificate>().FetchDataAsync(request, cn));
		}
	}

	public class CertificateIssueRequest
	{
		public long? CourseParticipantId { get; set; }
		public long? CourseCompanyParticipantId { get; set; }
	}

	public class CertificateListByParentViewModel
	{
		public long CourseId { get; set; }
		public string CourseTitle { get; set; } = string.Empty;
	}

	public class CertificateListItemViewModel
	{
		public string EnrollmentKind { get; set; } = string.Empty;
		public long? CourseParticipantId { get; set; }
		public long? CourseCompanyParticipantId { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? NationalCode { get; set; }
		public string? CompanyTitle { get; set; }
		public long? CertificateId { get; set; }
		public string? CertificateNo { get; set; }
		public string? TemplateKey { get; set; }
		public string? IssueShamsiDate { get; set; }
	}
}
