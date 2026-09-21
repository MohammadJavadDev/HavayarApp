using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Trn;
using Entities.App.Trn.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Trn/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("لاگ ارسال ایمیل گواهی", typeof(CertificateEmailLog))]
	public class CertificateEmailLogController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment _webHostEnvironment,
		IConfiguration _configuration) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست لاگ ایمیل گواهی دوره", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByCourseId(long courseId)
		{
			if (courseId == 0)
				throw new Exception("شناسه دوره نمیتواند خالی باشد.");

			var course = unitOfWork.Repository<Course>().TableNoTracking
				.FirstOrDefault(c => c.Id == courseId);

			var model = new CertificateEmailLogListByParentViewModel
			{
				CourseId = courseId,
				CourseTitle = course != null ? course.TrainingCode ?? string.Empty : string.Empty
			};

			return View(@"\Views\Panel\Trn\CertificateEmailLog\ListByCourseId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لاگ ایمیل گواهی دوره", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? courseId, CancellationToken cn)
		{
			if (courseId == null || courseId == 0)
				return BadRequest("شناسه دوره نمیتواند خالی باشد.");

			var items = await unitOfWork.Repository<CertificateEmailLog>()
				.TableNoTracking
				.Include(l => l.Certificate)
				.Where(l => l.CourseId == courseId)
				.OrderByDescending(l => l.Id)
				.Select(l => new CertificateEmailLogListItemViewModel
				{
					Id = l.Id,
					CourseId = l.CourseId,
					CertificateId = l.CertificateId,
					CertificateNo = l.Certificate != null ? l.Certificate.CertificateNo : null,
					RecipientEmail = l.RecipientEmail,
					IsSuccess = l.IsSuccess,
					ResultMessage = l.ResultMessage,
					CreatedOnShamsiDateTime = l.CreatedOnShamsiDateTime
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		// "ارسال ایمیل" / "ارسال مجدد" — every attempt appends a new log row (history is never overwritten).
		// A resend passes logId; a first-time send passes certificateId (+ optional email override).
		[HttpPost("[action]")]
		[ActionDisplayName("ارسال ایمیل گواهی", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> SendEmail(CertificateSendEmailRequest request, CancellationToken cn)
		{
			long? certificateId = request.CertificateId;
			var emailOverride = request.Email;

			if (request.LogId != null && request.LogId != 0)
			{
				var source = unitOfWork.Repository<CertificateEmailLog>().TableNoTracking
					.FirstOrDefault(l => l.Id == request.LogId);
				if (source == null)
					return BadRequest("لاگ مورد نظر یافت نشد.");
				certificateId ??= source.CertificateId;
				emailOverride ??= source.RecipientEmail;
			}

			if (certificateId == null || certificateId == 0)
				return BadRequest("گواهی مشخص نشده است.");

			var certificate = unitOfWork.Repository<Certificate>().TableNoTracking
				.Include(c => c.CourseParticipant).ThenInclude(p => p != null ? p.Participant : null)
				.Include(c => c.CourseParticipant).ThenInclude(p => p != null ? p.Course : null)
				.Include(c => c.CourseCompanyParticipant).ThenInclude(p => p != null ? p.Participant : null)
				.Include(c => c.CourseCompanyParticipant).ThenInclude(p => p != null ? p.Course : null)
				.FirstOrDefault(c => c.Id == certificateId);
			if (certificate == null)
				return BadRequest("گواهی یافت نشد.");

			var participant = certificate.CourseParticipant?.Participant
				?? certificate.CourseCompanyParticipant?.Participant;
			var course = certificate.CourseParticipant?.Course
				?? certificate.CourseCompanyParticipant?.Course;

			var recipient = emailOverride.HasValue() ? emailOverride : participant?.Email;
			if (!recipient.HasValue())
			{
				var missingLog = new CertificateEmailLog
				{
					CourseId = course?.Id ?? 0,
					CertificateId = certificate.Id,
					RecipientEmail = null,
					IsSuccess = false,
					ResultMessage = "ایمیل شرکت‌کننده ثبت نشده است — ارسال دستی انجام شود."
				};
				await unitOfWork.Repository<CertificateEmailLog>().SaveAsync(missingLog, cn, true);
				return Ok(missingLog);
			}

			var log = new CertificateEmailLog
			{
				CourseId = course?.Id ?? 0,
				CertificateId = certificate.Id,
				RecipientEmail = recipient
			};

			try
			{
				await SendCertificateMailAsync(recipient!, participant, course, certificate, cn);
				log.IsSuccess = true;
				log.ResultMessage = "ایمیل با موفقیت ارسال شد.";
			}
			catch (Exception ex)
			{
				log.IsSuccess = false;
				log.ResultMessage = "خطا در ارسال ایمیل: " + ex.Message;
			}

			await unitOfWork.Repository<CertificateEmailLog>().SaveAsync(log, cn, true);
			return Ok(log);
		}

		// Uses the same Email:* settings contract as App.BackgroundJob EmailJob (no new mailer built).
		// No attachment rendering here — PDF preview/print goes through the ReportBuilder template (Step 10).
		private async Task SendCertificateMailAsync(
			string recipient,
			Participant? participant,
			Course? course,
			Certificate certificate,
			CancellationToken cn)
		{
			var smtpHost = _configuration["Email:SmtpHost"];
			if (!smtpHost.HasValue())
				throw new Exception("تنظیمات ایمیل (Email:SmtpHost) یافت نشد — ارسال دستی انجام شود.");

			var salutation = participant?.Gender == GenderEnum.Female ? "سرکار خانم" : "جناب آقای";
			var fullName = ((participant?.FirstName ?? string.Empty) + " " + (participant?.LastName ?? string.Empty)).Trim();
			var subject = "گواهی‌نامه دوره آموزشی " + (course?.TrainingCode ?? string.Empty);
			var body = "با سلام " + salutation + " " + fullName
				+ "،<br/>ضمن تشکر از بابت انتخاب خدمات آموزشی شرکت هوایار، بدینوسیله گواهی‌نامه دوره آموزشی "
				+ "به شماره " + (certificate.CertificateNo ?? string.Empty)
				+ " که در تاریخ " + (course?.StartShamsiDate ?? string.Empty)
				+ " برگزار شد، ارسال می‌شود.<br/>مزید امتنان است اعلام وصول فرمایید.<br/>سپاس";

			using var message = new MailMessage
			{
				From = new MailAddress(
					_configuration["Email:FromAddress"] ?? throw new Exception("آدرس فرستنده ایمیل (Email:FromAddress) تنظیم نشده است."),
					_configuration["Email:FromDisplayName"] ?? string.Empty),
				Subject = subject,
				Body = body,
				IsBodyHtml = true
			};
			message.To.Add(recipient.Trim());

			using var client = new SmtpClient(smtpHost!, int.TryParse(_configuration["Email:SmtpPort"], out var port) ? port : 587)
			{
				EnableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var enableSsl) || enableSsl,
				DeliveryMethod = SmtpDeliveryMethod.Network
			};

			var username = _configuration["Email:Username"];
			if (username.HasValue())
				client.Credentials = new NetworkCredential(username, _configuration["Email:Password"] ?? string.Empty);

			await client.SendMailAsync(message, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<CertificateEmailLog>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<CertificateEmailLog>().FetchDataAsync(request, cn));
		}
	}

	public class CertificateSendEmailRequest
	{
		public long? CertificateId { get; set; }
		public long? LogId { get; set; }
		public string? Email { get; set; }
	}

	public class CertificateEmailLogListByParentViewModel
	{
		public long CourseId { get; set; }
		public string CourseTitle { get; set; } = string.Empty;
	}

	public class CertificateEmailLogListItemViewModel
	{
		public long? Id { get; set; }
		public long? CourseId { get; set; }
		public long? CertificateId { get; set; }
		public string? CertificateNo { get; set; }
		public string? RecipientEmail { get; set; }
		public bool IsSuccess { get; set; }
		public string? ResultMessage { get; set; }
		public string? CreatedOnShamsiDateTime { get; set; }
	}
}
