using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Hrm.Enums;
using Entities.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Sms;
using System.Text;

namespace WebApp.Controllers.Dynamic.Hrm
{
	public partial class ResumeController
	{
		public record IdRequest(long ResumeId);
		public record IdsRequest(List<long> ResumeIds, string? ManagerComment);
		public record CommentRequest(long ResumeId, string? Title, string? Comment);
		public record ReviewRequest(long ResumeId, string? RecommendedDateInText, string? RecommendedTimeInText, string? PriorityToInvite, ResumeReviewStatusEnum? StatusId, string? Status, long? UnitIntroducedId, long? SuitableForJobId, string? ReasonForIntroduction, string? ReasonForSuitableJob, string? ManagerDescription);
		public record InterviewRequest(long ResumeId, bool CancelByJobApplicant, string? JobApplicantCancelReason, string? ExpertDescription, string? SessionDate, string? SessionTime, string? SessionPlace, string? BeneficiaryIds, string? BeneficiaryNames, string? EmailRecipientEmails);
		public record SpecializedRequest(long ResumeId, long SpecializedInterviewerId, ResumeSpecializedInterviewStatusEnum SpecializedInterviewStatusId, string? SpecializedInterviewStatus, string? SpecializedInterviewPriority, long? SpecializedInterviewUnitIntroducedId, string? SpecializedInterviewReasonForIntroduction, long? SpecializedInterviewUnitSuitableId, string? SpecializedInterviewReasonForSuitableJob, string? SpecializedInterviewDescription, string? SpecializedInterviewStatusPositivePoints, string? SpecializedInterviewStatusNegativePoints);
		public record HrInterviewRequest(long ResumeId, long HrInterviewerId, ResumeHrInterviewStatusEnum HrInterviewerStatusId, string? HrInterviewerStatus, string? HrInterviewerStatusPositivePoints, string? HrInterviewerStatusNegativePoints, long? ContractAmount, string? HrInterviewerDescription);
		public record BoardRequest(long ResumeId, string? Checker, ResumeBoardCheckStatusEnum StatusId, string? Status);
		public record CompanyRequest(long ResumeId, string? PreviousCompanyName, string? PreviousManagerPhoneNumber, string? PreviousCompanyPhoneNumber, string? PreviousCompanyMobileNumber, string? PreviousCompanyDescription, bool ReferenceCheckIsApproved, string? BoardOfDirectorIds, string? BoardOfDirectorNames);
		public record FinalInfoRequest(ResumeFinalInformation Entity);

		[HttpPost("[action]")]
		public async Task<IActionResult> AcceptAndContinue(IdsRequest request, CancellationToken cn)
		{
			if (request.ResumeIds == null || request.ResumeIds.Count == 0)
				return BadRequest("ردیفی انتخاب نشده است");
			foreach (var id in request.ResumeIds)
			{
				var row = await unitOfWork.Repository<Resume>().Table.Include(x => x.StaffRequest).ThenInclude(x => x!.RequestPersonel).FirstOrDefaultAsync(x => x.Id == id, cn);
				if (row == null) continue;
				var level = CartableLevel(row, request.ManagerComment);
				if (level == -1)
					return BadRequest("تایید مدیر واحد ثبت نشده است");
				row.InExpertCartable = level == 0 && row.Status != ResumeReviewStatusEnum.Rejected;
				row.InManagerCartable = level == 1 || row.Status == ResumeReviewStatusEnum.Rejected;
				row.InHrmManagerCartable = level == 2;
				row.InBoardOfDirectorsCartable = level == 3;
				row.InOfficeExpertCartable = level == 4;
				if (row.Status == ResumeReviewStatusEnum.Rejected)
					row.Status = null;
				await unitOfWork.Repository<Resume>().UpdateAsync(row, cn, true);
				var title = row.InExpertCartable == true ? "ارسال به کارتابل کارشناس توسعه منابع انسانی"
					: row.InManagerCartable == true ? "ارسال به کارتابل مدیر/سرپرست"
					: row.InHrmManagerCartable == true ? "ارسال به کارتابل مدیر یا سرپرست توسعه منابع انسانی"
					: row.InBoardOfDirectorsCartable == true ? "ارسال به کارتابل هیئت مدیره"
					: "ارسال به کارتابل امور اداری";
				await AddComment(id, title, "رزومه جهت بررسی به کارتابل مربوطه ارسال گردید.", (int)ResumeReviewStatusEnum.UnderReview, cn);
				if (!string.IsNullOrWhiteSpace(request.ManagerComment))
					await AddComment(id, "تایید مصاحبه تخصصی", request.ManagerComment, null, cn);
				var receiver = row.InManagerCartable == true ? row.StaffRequest?.RequestPersonel?.Email : "Asgharzadeh.e@havayar.com";
				var post = row.InterestedPostId == null ? "" : await unitOfWork.Repository<OrgPost>().TableNoTracking.Where(x => x.Id == row.InterestedPostId).Select(x => x.Title).FirstOrDefaultAsync(cn);
				await QueueEmail("رزومه جدید در کارتابل #" + row.HtsId,
					MailShell($"رزومه به نام <span style='font-weight:bold;'>{H(row.Name)} {H(row.Family)}</span> برای سمت <span style='font-weight:bold;color:red;'>{H(post)}</span> جهت درخواست نیرو <span style='font-weight:bold;color:red;'>{row.StaffRequest?.HtsId}</span> به کارتابل شما اضافه گردید."),
					receiver, null, id, cn);
			}
			return Ok();
		}

		private static int CartableLevel(Resume row, string? managerComment)
		{
			var status = (int?)row.Status;
			if (!(status > 0)) return 1;
			if ((row.CancelByJobApplicant == null || row.SessionDate == null) && (row.Status is ResumeReviewStatusEnum.InviteToInterview or ResumeReviewStatusEnum.Reserve))
				return 0;
			if (row.SessionDate != null && !(row.SpecializedInterviewerId > 0))
				return 1;
			if (row.SpecializedInterviewerId > 0 && !(row.HrInterviewerId > 0) && row.SpecializedInterviewStatus is ResumeSpecializedInterviewStatusEnum.Approved or ResumeSpecializedInterviewStatusEnum.Reserve && row.InManagerCartable == true)
				return string.IsNullOrWhiteSpace(managerComment) ? -1 : 0;
			if (status > 0 && row.SessionDate != null && row.SpecializedInterviewerId > 0 && row.SpecializedInterviewStatus is ResumeSpecializedInterviewStatusEnum.Approved or ResumeSpecializedInterviewStatusEnum.Reserve && !(row.HrInterviewerId > 0) && row.InExpertCartable == true)
				return 2;
			if (row.HrInterviewerId > 0 && row.HrInterviewerStatus == ResumeHrInterviewStatusEnum.Approved && row.PreviousCompanyName == null)
				return 3;
			if (row.PreviousCompanyName != null)
			{
				if (row.LastCheckStatusByManager == null) return 3;
				if (row.LastCheckStatusByManager != ResumeBoardCheckStatusEnum.Approved) return 0;
				return 4;
			}
			return 0;
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> AddCommentAction(CommentRequest request, CancellationToken cn)
		{
			var exists = await unitOfWork.Repository<Resume>().TableNoTracking.AnyAsync(x => x.Id == request.ResumeId, cn);
			if (!exists) return NotFound();
			await AddComment(request.ResumeId, string.IsNullOrWhiteSpace(request.Title) ? "توضیحات" : request.Title, request.Comment, null, cn);
			return Ok();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> ResumeReviewStatus(ReviewRequest request, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().Table.FirstOrDefaultAsync(x => x.Id == request.ResumeId, cn);
			if (row == null) return NotFound();
			row.RecommendedDateInText = request.RecommendedDateInText;
			row.RecommendedTimeInText = request.RecommendedTimeInText;
			row.PriorityToInvite = request.PriorityToInvite;
			row.Status = request.StatusId;
			row.UnitIntroducedId = request.UnitIntroducedId;
			row.SuitableForJobId = request.SuitableForJobId;
			row.ReasonForIntroduction = request.ReasonForIntroduction;
			row.ReasonForSuitableJob = request.ReasonForSuitableJob;
			row.ManagerDescription = request.ManagerDescription;
			await unitOfWork.Repository<Resume>().UpdateAsync(row, cn, true);
			await AddComment(row.Id!.Value, "بررسی توسط مدیر / سرپرست", request.ManagerDescription, (int?)request.StatusId, cn);
			var color = (request.Status ?? "").Contains("عدم تایید") ? "red" : "green";
			await QueueEmail("بررسی توسط مدیر/سرپرست -رزومه #" + row.HtsId,
				MailShell($"رزومه #{row.HtsId} توسط مدیر/سرپرست بررسی شده و در وضعیت <span style='color:{color};font-weight:bold;'>{H(request.Status)}</span> قرار داده شده است.<br/>نام کارجو: {H(row.Name)} {H(row.Family)}<br/>شماره تماس: {H(row.MobileNumber)}<br/>زمان پیشنهادی: {H(row.RecommendedDateInText)} {H(row.RecommendedTimeInText)}<br/>مدیر/سرپرست: {H(CurrentUserFullName)}"),
				"Asgharzadeh.e@havayar.com", null, row.Id!.Value, cn);
			return Ok();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> InterviewStatus(InterviewRequest request, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().Table.Include(x => x.StaffRequest).ThenInclude(x => x!.RequestUser).Include(x => x.InterestedPost).FirstOrDefaultAsync(x => x.Id == request.ResumeId, cn);
			if (row == null) return NotFound();
			row.CancelByJobApplicant = request.CancelByJobApplicant;
			row.JobApplicantCancelReason = request.JobApplicantCancelReason;
			row.ExpertDescription = request.ExpertDescription;
			row.SessionDate = request.SessionDate;
			row.SessionTime = request.SessionTime;
			row.SessionPlace = request.SessionPlace;
			row.BeneficiaryIds = request.BeneficiaryIds;
			row.BeneficiaryNames = request.BeneficiaryNames;
			row.EmailRecipientEmails = request.EmailRecipientEmails;
			await unitOfWork.Repository<Resume>().UpdateAsync(row, cn, true);
			var status = request.CancelByJobApplicant ? "انصراف کار جو" : "آمادگی کار جو جهت انجام مصاحبه";
			await AddComment(row.Id!.Value, "نتیجه بررسی رزومه", status, null, cn);
			if (!request.CancelByJobApplicant)
			{
				await QueueEmail("دعوت به مصاحبه حضوری #" + row.HtsId, MailShell("احتراماً جدول زمانبندی مصاحبه به شرح ذیل حضورتان ارسال می‌گردد.<br/>" + H(row.Name) + " " + H(row.Family) + " | " + H(row.InterestedPost?.Title) + " | " + H(row.BeneficiaryNames) + " | " + H(row.SessionDate) + " " + H(row.SessionTime) + " | " + H(row.SessionPlace)), request.EmailRecipientEmails, null, row.Id!.Value, cn);
				var placeExtra = (row.SessionPlace ?? "").Contains("دفتر")
					? "Asgharzadeh.e@havayar.com;moeini.m@havayar.com;coguard@havayar.com;Tashrifat.co@havayar.com"
					: "Asgharzadeh.e@havayar.com;moeini.m@havayar.com;Gharedaghi.a@havayar.com;Guard@havayar.com;Tashrifat.fact@havayar.com";
				await QueueEmail("دعوت به مصاحبه حضوری", MailShell("ضمن تشکر از شما بابت ارسال رزومه و شرکت در فرآیند جذب و استخدام گروه صنعتی هوایار، احتراماً جدول زمانبندی مصاحبه شما به شرح ذیل حضورتان ارسال می‌گردد.<br/>" + H(row.SessionDate) + " " + H(row.SessionTime) + " " + H(row.SessionPlace)), row.EmailAddress, "Asgharzadeh.e@havayar.com;moeini.m@havayar.com;" + placeExtra, row.Id!.Value, cn);
				var smsText = InterviewSms(row);
				if (!string.IsNullOrWhiteSpace(row.MobileNumber))
					await sms.TryEnqueueAsync("Hrm.ResumeInterviewInvite", row.MobileNumber, smsText, nameof(Resume), row.Id, ct: cn);
			}
			else
			{
				await QueueEmail("انصراف کارجو #" + row.HtsId, MailShell("کارجو از حضور در مصاحبه انصراف داده است.<br/>" + H(row.Name) + " " + H(row.Family) + "<br/>علت: " + H(row.JobApplicantCancelReason)), request.EmailRecipientEmails, null, row.Id!.Value, cn);
			}
			return Ok();
		}

		private static string InterviewSms(Resume row)
		{
			var sb = new StringBuilder();
			sb.AppendLine("\"گروه صنعتی هوایار\"");
			sb.AppendLine("کارجوی گرامی");
			sb.AppendLine("با سلام و احترام");
			sb.AppendLine("ضمن تشکر از شما جهت شرکت در فرآیند جذب، احتراماً اطلاعات زمانبندی مصاحبه شما به شرح ذیل حضورتان ارسال می‌گردد.");
			sb.AppendLine("پست موردعلاقه: " + row.InterestedPost?.Title);
			sb.AppendLine("مصاحبه کننده: " + row.BeneficiaryNames);
			sb.AppendLine("روز : " + row.RecommendedDateInText + " " + row.RecommendedTimeInText);
			sb.AppendLine("ساعت : " + row.SessionTime);
			sb.AppendLine("محل مصاحبه: " + row.SessionPlace);
			return sb.ToString();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SpecializedInterviewStatus(SpecializedRequest request, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().Table.Include(x => x.StaffRequest).ThenInclude(x => x!.Job).FirstOrDefaultAsync(x => x.Id == request.ResumeId, cn);
			if (row == null) return NotFound();
			row.SpecializedInterviewerId = request.SpecializedInterviewerId;
			row.HtsSpecializedInterviewerId = null;
			row.SpecializedInterviewStatus = request.SpecializedInterviewStatusId;
			row.SpecializedInterviewPriority = request.SpecializedInterviewPriority;
			row.SpecializedInterviewUnitIntroducedId = request.SpecializedInterviewUnitIntroducedId;
			row.SpecializedInterviewReasonForIntroduction = request.SpecializedInterviewReasonForIntroduction;
			row.SpecializedInterviewUnitSuitableId = request.SpecializedInterviewUnitSuitableId;
			row.SpecializedInterviewReasonForSuitableJob = request.SpecializedInterviewReasonForSuitableJob;
			row.SpecializedInterviewDescription = request.SpecializedInterviewDescription;
			row.SpecializedInterviewStatusPositivePoints = request.SpecializedInterviewStatusPositivePoints;
			row.SpecializedInterviewStatusNegativePoints = request.SpecializedInterviewStatusNegativePoints;
			await unitOfWork.Repository<Resume>().UpdateAsync(row, cn, true);
			await AddComment(row.Id!.Value, "نتیجه مصاحبه تخصصی", "مصاحبه توسط مدیر/سرپرست درخواست کننده نیرو در وضعیت " + request.SpecializedInterviewStatus + " قرار گرفت.", (int)request.SpecializedInterviewStatusId, cn);
			var extra = request.SpecializedInterviewDescription ?? "";
			if (!string.IsNullOrWhiteSpace(request.SpecializedInterviewStatusPositivePoints)) extra += " نکات مثبت: " + request.SpecializedInterviewStatusPositivePoints;
			if (!string.IsNullOrWhiteSpace(request.SpecializedInterviewStatusNegativePoints)) extra += " نکات منفی: " + request.SpecializedInterviewStatusNegativePoints;
			await AddComment(row.Id!.Value, "کامنت مصاحبه تخصصی", extra, null, cn);
			var color = (request.SpecializedInterviewStatus ?? "").Contains("عدم تایید") ? "red" : "green";
			await QueueEmail("نتیجه مصاحبه تخصصی -رزومه #" + row.HtsId,
				MailShell($"نتیجه مصاحبه تخصصی رزومه شناسه #{row.HtsId} به نام {H(row.Name)} {H(row.Family)} جهت سمت {H(row.StaffRequest?.Job?.Title)} به شماره درخواست {row.StaffRequest?.HtsId} <span style='color:{color};font-weight:bold;'>{H(request.SpecializedInterviewStatus)}</span> گردید."),
				"Asgharzadeh.e@havayar.com", null, row.Id!.Value, cn);
			return Ok();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> HrInterviewStatus(HrInterviewRequest request, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().Table.FirstOrDefaultAsync(x => x.Id == request.ResumeId, cn);
			if (row == null) return NotFound();
			row.HrInterviewerId = request.HrInterviewerId;
			row.HrInterviewerStatus = request.HrInterviewerStatusId;
			row.ContractAmount = request.ContractAmount;
			row.HrInterviewerStatusPositivePoints = request.HrInterviewerStatusPositivePoints;
			row.HrInterviewerStatusNegativePoints = request.HrInterviewerStatusNegativePoints;
			row.HrInterviewerDescription = request.HrInterviewerDescription;
			await unitOfWork.Repository<Resume>().UpdateAsync(row, cn, true);
			await AddComment(row.Id!.Value, "نتیجه مصاحبه منابع انسانی", "مصاحبه توسط مدیریت توسعه منابع انسانی در وضعیت " + request.HrInterviewerStatus + " قرار گرفت.", (int)request.HrInterviewerStatusId, cn);
			var extra = request.HrInterviewerDescription ?? "";
			if (!string.IsNullOrWhiteSpace(request.HrInterviewerStatusPositivePoints)) extra += " نکات مثبت: " + request.HrInterviewerStatusPositivePoints;
			if (!string.IsNullOrWhiteSpace(request.HrInterviewerStatusNegativePoints)) extra += " نکات منفی: " + request.HrInterviewerStatusNegativePoints;
			await AddComment(row.Id!.Value, "کامنت منابع انسانی", extra, null, cn);
			await QueueEmail("نتیجه مصاحبه منابع انسانی -رزومه #" + row.HtsId,
				MailShell($"نتیجه رزومه #{row.HtsId} توسط مدیر منابع انسانی در سیستم ثبت و در وضعیت <span style='color:red;font-weight:bold;'>{H(request.HrInterviewerStatus)}</span> قرار داده شده است."),
				"Asgharzadeh.e@havayar.com", null, row.Id!.Value, cn);
			return Ok();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> CheckByManager(BoardRequest request, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().Table.FirstOrDefaultAsync(x => x.Id == request.ResumeId, cn);
			if (row == null) return NotFound();
			row.LastCheckStatusByManager = request.StatusId;
			await unitOfWork.Repository<Resume>().UpdateAsync(row, cn, true);
			await AddComment(row.Id!.Value, "بررسی توسط هیئت مدیره", "رزومه توسط " + request.Checker + " بررسی و در وضعیت \"" + request.Status + "\" قرار گرفت.", (int)request.StatusId, cn);
			await QueueEmail("نتیجه بررسی رزومه #" + row.HtsId + " توسط هیئت مدیره",
				MailShell($"بررسی رزومه #{row.HtsId} توسط هیئت مدیره بررسی شده و در وضعیت <span style='color:red;font-weight:bold;'>{H(request.Status)}</span> قرار داده شده است."),
				"Asgharzadeh.e@havayar.com", null, row.Id!.Value, cn);
			if (request.StatusId == ResumeBoardCheckStatusEnum.Approved)
			{
				var finalInfo = await unitOfWork.Repository<ResumeFinalInformation>().TableNoTracking.Include(x => x.Department).Include(x => x.Job).FirstOrDefaultAsync(x => x.ResumeId == row.Id, cn);
				if (finalInfo != null)
				{
					var office = (finalInfo.Workplace ?? "").Contains("دفتر");
					await QueueEmail("نامه پیشنهاد شغلی", MailShell($"{H(finalInfo.FullName)} عزیز<br/>از اینکه موقعیت شغلی و شرایط زیر رو بهتون پیشنهاد میدیم، بسیار خوشحالیم.<br/>نام دپارتمان: {H(finalInfo.Department?.Title)}<br/>موقعیت شغلی: {H(finalInfo.Job?.Title)}<br/>تاریخ شروع: {H(finalInfo.StartDateOfAgreement)}<br/>محل خدمت: {H(finalInfo.Workplace)}"),
						(office ? "Ketabi.m@havayar.com;" : "Gharedaghi.a@havayar.com;") + row.EmailAddress, null, row.Id!.Value, cn);
					var startReceivers = office
						? "Asgharzadeh.e@havayar.com;Hasani.mo@havayar.com;Kouhnavard.r@havayar.com;Ketabi.m@havayar.com;coguard@havayar.com;Tashrifat.co@havayar.com"
						: "Asgharzadeh.e@havayar.com;Barzegar.t@havayar.com;Ahrarnejad.n@havayar.com;Dehghan.mo@havayar.com;Gharedaghi.a@havayar.com;Guard@havayar.com;Tashrifat.fact@havayar.com";
					await QueueEmail("اعلان زمان شروع به کار نیروی جدید", MailShell($"آقا/خانم {H(finalInfo.FullName)} به تازگی به جمع ما اضافه شده اند. زمان شروع به کار ایشان {H(finalInfo.StartDateOfAgreement)} می باشد."), startReceivers, null, row.Id!.Value, cn);
				}
			}
			return Ok();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> Authorization(CompanyRequest request, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().Table.FirstOrDefaultAsync(x => x.Id == request.ResumeId, cn);
			if (row == null) return NotFound();
			row.PreviousCompanyName = request.PreviousCompanyName;
			row.PreviousManagerPhoneNumber = request.PreviousManagerPhoneNumber;
			row.PreviousCompanyPhoneNumber = request.PreviousCompanyPhoneNumber;
			row.PreviousCompanyMobileNumber = request.PreviousCompanyMobileNumber;
			row.PreviousCompanyDescription = request.PreviousCompanyDescription;
			row.ReferenceCheckIsApproved = request.ReferenceCheckIsApproved;
			row.BoardOfDirectorIds = request.BoardOfDirectorIds;
			row.BoardOfDirectorNames = request.BoardOfDirectorNames;
			await unitOfWork.Repository<Resume>().UpdateAsync(row, cn, true);
			await unitOfWork.Repository<ResumeWorkExperience>().AddAsync(new ResumeWorkExperience
			{
				HtsId = 0,
				ResumeId = row.Id!.Value,
				CompanyName = request.PreviousCompanyName,
				OldManagerPhoneNumber = request.PreviousManagerPhoneNumber,
				LeavingReason = request.PreviousCompanyDescription,
				CompanyPhone = request.PreviousCompanyPhoneNumber,
				CompanyMobile = request.PreviousCompanyMobileNumber,
				IsActive = IsActiveEnum.Active
			}, cn, true);
			return Ok();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> FinalInformation(FinalInfoRequest request, CancellationToken cn)
		{
			var entity = request.Entity;
			if (entity == null || entity.ResumeId <= 0) return BadRequest("رزومه مشخص نیست");
			entity.Id = null;
			entity.HtsId = 0;
			entity.CreatedDateInText = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
			entity.IsActive = IsActiveEnum.Active;
			await unitOfWork.Repository<ResumeFinalInformation>().AddAsync(entity, cn, true);
			await AddComment(entity.ResumeId, "اطلاعات تکمیلی", "ثبت اطلاعات تکمیلی رزومه انجام شد.", null, cn);
			return Ok();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SendInterviewResult(IdRequest request, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.ResumeId, cn);
			if (row == null) return NotFound();
			if (string.IsNullOrWhiteSpace(row.EmailAddress)) return BadRequest("ایمیل کارجو معتبر نیست");
			if (row.SpecializedInterviewStatus == null && row.HrInterviewerStatus == null)
				return BadRequest("وضعیت مصاحبه ثبت نشده است");
			var name = row.Name + " " + row.Family;
			string subject;
			string text;
			if (row.SpecializedInterviewStatus == ResumeSpecializedInterviewStatusEnum.Approved && row.HrInterviewerStatus == null)
			{
				subject = "موضوع: پذیرش فرد در مصاحبه";
				text = $"خانم / آقای {H(name)} عزیز<br/>از زمانی که برای مصاحبه با ما صرف کرده‌اید بسیار خرسندیم. ارزیابی مصاحبه اولیه شما مثبت بوده و به زودی همکاران ما جهت هماهنگی مصاحبه با تیم منابع انسانی با شما تماس خواهند گرفت.";
			}
			else if (row.SpecializedInterviewStatus == ResumeSpecializedInterviewStatusEnum.Reserve && row.HrInterviewerStatus == null)
			{
				subject = "موضوع: ذخیره‌ی رزومه ی فرد برای موقعیت های آینده";
				text = $"خانم / آقای {H(name)} عزیز<br/>مهارت ها و توانایی های شما قابل تحسین است اما متاسفانه در حال حاضر امکان همکاری با یکدیگر را نداریم.";
			}
			else if (row.SpecializedInterviewStatus == ResumeSpecializedInterviewStatusEnum.Rejected && row.HrInterviewerStatus == null)
			{
				subject = "موضوع: نامه ی رد درخواست کار";
				text = $"خانم / آقای {H(name)} عزیز<br/>علیرغم شایستگی های شما، در این بازه زمانی قادر به همکاری با یکدیگر نمی‌باشیم.";
			}
			else if (row.HrInterviewerStatus == ResumeHrInterviewStatusEnum.Approved)
			{
				subject = "موضوع: پذیرش فرد";
				text = $"خانم / آقای {H(name)} عزیز<br/>ارزیابی مصاحبه شما مثبت بوده است.";
			}
			else
			{
				subject = "موضوع: نامه ی رد درخواست کار";
				text = $"خانم / آقای {H(name)} عزیز<br/>در این بازه زمانی قادر به همکاری با یکدیگر نمی‌باشیم.";
			}
			await QueueEmail(subject, MailShell(text + "<br/>با احترام<br/>واحد منابع انسانی گروه صنعتی هوایار"), row.EmailAddress, "Asgharzadeh.e@havayar.com", row.Id!.Value, cn);
			return Ok();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SendRejection(IdRequest request, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.ResumeId, cn);
			if (row == null) return NotFound();
			if (string.IsNullOrWhiteSpace(row.EmailAddress)) return BadRequest("ایمیل کارجو معتبر نیست");
			await QueueEmail("نامه ی رد درخواست کار",
				MailShell($"خانم / آقای {H(row.Name)} {H(row.Family)} عزیز<br/>ملاقات با شما و آشنایی با توانمندی هایتان برای ما بسیار خوشایند بود. علیرغم شایستگی های شما، در این بازه ی زمانی قادر به همکاری با یکدیگر نمی‌باشیم.<br/>با احترام<br/>واحد منابع انسانی گروه صنعتی هوایار"),
				row.EmailAddress, "Asgharzadeh.e@havayar.com", row.Id!.Value, cn);
			await AddComment(row.Id!.Value, "نتیجه بررسی رزومه", "ارسال ایمیل ریجکشن", null, cn);
			return Ok();
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SendSurvey(IdRequest request, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<Resume>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.ResumeId, cn);
			if (row == null) return NotFound();
			if (string.IsNullOrWhiteSpace(row.EmailAddress)) return BadRequest("ایمیل کارجو معتبر نیست");
			await QueueEmail("تکمیل فرم نظرسنجی از فرآیند جذب و استخدام هوایار",
				MailShell($"خانم / آقای {H(row.Name)} {H(row.Family)} عزیز<br/>خواهشمند است فرم نظرسنجی را تکمیل فرمایید.<br/>لینک نظرسنجی: https://survey.porsline.ir/s/i6ebEajf<br/>با احترام<br/>واحد منابع انسانی گروه صنعتی هوایار"),
				row.EmailAddress, "Asgharzadeh.e@havayar.com", row.Id!.Value, cn);
			await AddComment(row.Id!.Value, "نظرسنجی", "ارسال ایمیل شرکت در نظرسنجی", null, cn);
			return Ok();
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> Comments(long resumeId, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<ResumeComment>().TableNoTracking.Where(x => x.ResumeId == resumeId).OrderByDescending(x => x.Id)
				.Select(x => new { x.Title, x.Comment, x.StatusTitle, x.CreatedByName, x.CreatedDateTimeInText }).ToListAsync(cn));

		[HttpGet("[action]")]
		public async Task<IActionResult> Attachments(long resumeId, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<ResumeAttachment>().TableNoTracking.Where(x => x.ResumeId == resumeId)
				.Select(x => new { x.Id, x.FileName, x.Comment, x.CreatedShamsiDate, x.FileId }).ToListAsync(cn));
	}
}
