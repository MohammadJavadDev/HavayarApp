using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.App.Trn.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "دوره آموزشی")]
	[Table("Course", Schema = "Trn")]
	[Index(nameof(TrainingCode), IsUnique = true)]
	public class Course : BaseEntity
	{
		// ---- Identity / catalog ----
		[DisplayName("بانک دوره (زمینه)")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public long CourseBankId { get; set; }
		public virtual CourseBank? CourseBank { get; set; }

		[DisplayName("کد آموزش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? TrainingCode { get; set; }   // auto-generated (R1), read-only in UI

		[DisplayName("نوع دوره (CNG و ...)")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CourseTypeEnum? CngCourseType { get; set; }   // legacy CngCourseType (LookupType 123)

		[DisplayName("شیوه برگزاری")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public CourseExecutingMethodEnum ExecutingMethod { get; set; } = CourseExecutingMethodEnum.PublicCall;

		[DisplayName("نحوه برگزاری")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CourseMethodOfHoldingEnum? MethodOfHolding { get; set; }

		// ---- Scheduling ----
		[DisplayName("تاریخ شروع")]
		[DisplayInfo(null, true, type: SystemType.Date, required: true)]
		public DateTime? StartMiladiDate { get; set; }
		[DisplayName("تاریخ شروع (شمسی)")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? StartShamsiDate { get; set; }

		[DisplayName("تاریخ پایان")]
		[DisplayInfo(null, true, type: SystemType.Date, required: true)]
		public DateTime? EndMiladiDate { get; set; }
		[DisplayName("تاریخ پایان (شمسی)")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? EndShamsiDate { get; set; }

		[DisplayName("مدت (دقیقه)")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int? DurationInMinute { get; set; }

		[DisplayName("تعداد روز")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Days { get; set; }

		[DisplayName("محل برگزاری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public long? LocationId { get; set; }     // → Gnr.Region (legacy Gnr_City)
		public virtual Region? Location { get; set; }

		[DisplayName("محل اجرا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? ExecutingPlace { get; set; }

		[DisplayName("جایگاه CNG")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CngPlace { get; set; }

		[DisplayName("آخرین مهلت ثبت نام")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? RegistrationDeadlineMiladiDate { get; set; }
		[DisplayName("آخرین مهلت ثبت نام (شمسی)")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? RegistrationDeadlineShamsiDate { get; set; }

		// ---- People / organization ----
		[DisplayName("مدرس")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public long TeacherId { get; set; }
		public virtual TeacherBank? Teacher { get; set; }

		[DisplayName("مدرس دوم")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public long? SecondTeacherId { get; set; }
		public virtual TeacherBank? SecondTeacher { get; set; }

		[DisplayName("سابقه همکاری")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasSynergyHistory { get; set; }

		[DisplayName("نماینده / بازاریاب")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public long? AgentId { get; set; }
		public virtual Agent? Agent { get; set; }

		[DisplayName("شرکت (برای دوره اختصاصی)")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public long? CompanyId { get; set; }      // required when ExecutingMethod == Private (rule R3)
		public virtual Company? Company { get; set; }

		[DisplayName("واحد مرتبط")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public long? RelatedUnitId { get; set; }  // → Hrm.OrgUnit (legacy HRM_OrgUnit)
		public virtual OrgUnit? RelatedUnit { get; set; }

		// ---- Capacity ----
		[DisplayName("ظرفیت")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public short Capacity { get; set; }

		[DisplayName("ظرفیت باقی‌مانده")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? RemainingCapacity { get; set; }  // maintained by participant add/remove (rule R4 in step 07)

		// ---- Status / workflow ----
		[DisplayName("وضعیت دوره")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public CourseStatusEnum? Status { get; set; }              // legacy StatusId (108)

		[DisplayName("وضعیت مالی")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public CourseFinancialStatusEnum? FinancialStatus { get; set; }  // legacy FinancialStatusId (109)

		[DisplayName("وضعیت گواهی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CourseCertificateStatusEnum? CertificateStatus { get; set; }  // legacy CertificateStatusId (320)

		[DisplayName("وضعیت ارسال دعوت‌نامه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CourseSendInvitationEnum? SendInvitation { get; set; }        // legacy SendInvitationId (319)

		[DisplayName("نوع پرداخت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CoursePaymentTypeEnum? PaymentType { get; set; }              // legacy PaymentTypeId (321)

		[DisplayName("وضعیت حضور")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CourseAttendanceStatusEnum? AttendanceStatus { get; set; }    // legacy AttendanceStatusId (322)

		// Legacy flipped an IsEvaluated-style flag on the course once any evaluation existed (Step 08, R6).
		// Course counts as evaluated iff >= 1 CourseEvaluation row exists; maintained by CourseEvaluationAction.
		[DisplayName("ارزیابی شده")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool IsEvaluated { get; set; }

		// ---- Flags / financial ----
		[DisplayName("هزینه دوره")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? Cost { get; set; }

		[DisplayName("نقطه سر به سری")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? HeadToTheSeriesPoint { get; set; }

		[DisplayName("گواهینامه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasCertificates { get; set; }

		[DisplayName("آزمون")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasExam { get; set; }

		[DisplayName("مالیات")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? HasVat { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Description { get; set; }

		// ---- Warranty-period information (اطلاعات دوره گارانتی) ----
		[DisplayName("نام مامور")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? AgentName { get; set; }

		[DisplayName("شماره تماس 1")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? FirstPhoneNumber { get; set; }

		[DisplayName("شماره تماس 2")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? SecondPhoneNumber { get; set; }

		[DisplayName("نمایش تاریخ راه اندازی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? ShowStartupDate { get; set; }

		[DisplayName("تعداد دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public byte? NumberOfDevices { get; set; }

		[DisplayName("تعداد نفرات")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? NumberOfPeople { get; set; }

		[DisplayName("ارسال دعوت نامه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? InvitationSent { get; set; }

		[DisplayName("تاریخ ارسال دعوت نامه")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? SendInvitationMiladiDate { get; set; }
		[DisplayName("تاریخ ارسال دعوت نامه (شمسی)")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? SendInvitationShamsiDate { get; set; }

		[DisplayName("نام و نام خانوادگی گیرنده دعوت نامه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? RecipientOfInvitation { get; set; }

		[DisplayName("تاریخ حضور در دوره آموزشی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? AttendingTrainingCourseMiladiDate { get; set; }
		[DisplayName("تاریخ حضور در دوره آموزشی (شمسی)")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? AttendingTrainingCourseShamsiDate { get; set; }

		[DisplayName("محل برگزاری دوره آموزشی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? PlaceOfTrainingCourse { get; set; }

		// ---- Training course titles (not shown in list grid) ----
		[DisplayName("عناوین دوره های آموزشی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles { get; set; }

		[DisplayName("عناوین دوره های آموزشی 1")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles1 { get; set; }

		[DisplayName("عناوین دوره های آموزشی 2")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles2 { get; set; }

		[DisplayName("عناوین دوره های آموزشی 3")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles3 { get; set; }

		[DisplayName("عناوین دوره های آموزشی 4")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles4 { get; set; }

		[DisplayName("عناوین دوره های آموزشی 5")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles5 { get; set; }

		[DisplayName("عناوین دوره های آموزشی 6")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles6 { get; set; }

		[DisplayName("عناوین دوره های آموزشی 7")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles7 { get; set; }

		[DisplayName("عناوین دوره های آموزشی 8")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles8 { get; set; }

		[DisplayName("عناوین دوره های آموزشی 9")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TrainingCourseTitles9 { get; set; }

		/// <summary>
		/// Track عمومی (مثل HTS): فراخوان عمومی، یا کد آموزش TRCNG.
		/// </summary>
		public bool IsPublicParticipantTrack()
		{
			if (ExecutingMethod == CourseExecutingMethodEnum.PublicCall)
				return true;
			return TrainingCode != null && TrainingCode.StartsWith("TRCNG");
		}

		public virtual ICollection<CourseParticipant> CourseParticipants { get; set; } = new List<CourseParticipant>();
		public virtual ICollection<CourseCompanyParticipant> CourseCompanyParticipants { get; set; } = new List<CourseCompanyParticipant>();
		public virtual ICollection<CourseEvaluation> CourseEvaluations { get; set; } = new List<CourseEvaluation>();
		public virtual ICollection<CertificateEmailLog> CertificateEmailLogs { get; set; } = new List<CertificateEmailLog>();
	}
}
