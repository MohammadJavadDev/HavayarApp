using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Hrm.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>رزومه جذب — معادل HTS <c>HRM_Resume</c> صفحه مدیریت رزومه (New).</summary>
	[Display(Name = "رزومه")]
	[Table("Resume", Schema = "Hrm")]
	public class Resume : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست نیرو")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(StaffRequestId))]
		public virtual StaffRequest? StaffRequest { get; set; }

		public long? StaffRequestId { get; set; }

		[DisplayName("کد ملی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(10)]
		public string? NationalCode { get; set; }

		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? Name { get; set; }

		[DisplayName("نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? Family { get; set; }

		[DisplayName("جنسیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeSexEnum? Sex { get; set; }

		[DisplayName("مقطع تحصیلی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeEducationEnum? Education { get; set; }

		[DisplayName("رشته تحصیلی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(StudyCourseId))]
		public virtual StudyCourse? StudyCourse { get; set; }

		public long? StudyCourseId { get; set; }

		[DisplayName("گرایش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(TrendOfStudyCourseId))]
		public virtual TrendOfStudyCourse? TrendOfStudyCourse { get; set; }

		public long? TrendOfStudyCourseId { get; set; }

		[DisplayName("رشته (متن)")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(128)]
		public string? StudyCourseInText { get; set; }

		[DisplayName("گرایش (متن)")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(128)]
		public string? TrendOfStudyCourseInText { get; set; }

		[DisplayName("سابقه کار")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? WorkExperience { get; set; }

		[DisplayName("پست سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(InterestedPostId))]
		public virtual OrgPost? InterestedPost { get; set; }

		public long? InterestedPostId { get; set; }

		[DisplayName("ترم دانشجو")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public byte? StudentTerm { get; set; }

		[DisplayName("تاریخ تولد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? BirthDate { get; set; }

		[DisplayName("شهر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(CityId))]
		public virtual Region? City { get; set; }

		public long? CityId { get; set; }

		[DisplayName("شناسه شهر HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsCityId { get; set; }

		[DisplayName("کانال ارتباطی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(IncomingSourceId))]
		public virtual IncomingSource? IncomingSource { get; set; }

		public long? IncomingSourceId { get; set; }

		[DisplayName("معرف")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(RepresentingPersonelId))]
		public virtual Personel? RepresentingPersonel { get; set; }

		public long? RepresentingPersonelId { get; set; }

		[DisplayName("شناسه معرف HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsRepresentingPersonelId { get; set; }

		[DisplayName("دانشگاه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(UniversityId))]
		public virtual University? University { get; set; }

		public long? UniversityId { get; set; }

		[DisplayName("سازمان")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeOrganizationEnum? Organization { get; set; }

		[DisplayName("نام پدر")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? FatherName { get; set; }

		[DisplayName("محل تولد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? BirthPlace { get; set; }

		[DisplayName("محل صدور")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? IssuePlace { get; set; }

		[DisplayName("دین")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? Religion { get; set; }

		[DisplayName("گرایش متنی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? Trend { get; set; }

		[DisplayName("وضعیت تاهل HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public short? MaritalStatusId { get; set; }

		[DisplayName("تعداد فرزند")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public short? NumberOfChildren { get; set; }

		[DisplayName("وضعیت نظام وظیفه HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public short? MilitaryServiceStatusId { get; set; }

		[DisplayName("نوع معافیت HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public short? MilitaryExemptionTypeId { get; set; }

		[DisplayName("علت معافیت")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(128)]
		public string? MilitaryExemptionReason { get; set; }

		[DisplayName("تلفن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? PhoneNumber { get; set; }

		[DisplayName("موبایل")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(20)]
		public string? MobileNumber { get; set; }

		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? EmailAddress { get; set; }

		[DisplayName("استان محل سکونت")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(HomeProvinceId))]
		public virtual Region? HomeProvince { get; set; }

		public long? HomeProvinceId { get; set; }

		[DisplayName("شناسه استان HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsHomeProvinceId { get; set; }

		[DisplayName("شهر محل سکونت")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(HomeCityId))]
		public virtual Region? HomeCity { get; set; }

		public long? HomeCityId { get; set; }

		[DisplayName("شناسه شهر سکونت HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsHomeCityId { get; set; }

		[DisplayName("آدرس")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(128)]
		public string? HomeAddress { get; set; }

		[DisplayName("وضعیت رزومه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeStatusEnum? ResumeStatus { get; set; }

		[DisplayName("شناسه ایجادکننده HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsCreatorUserId { get; set; }

		[DisplayName("تاریخ رزومه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? ResumeShamsiDate { get; set; }

		[DisplayName("ساعت رزومه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? ResumeTime { get; set; }

		[DisplayName("سال سابقه")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? YearsOfExperience { get; set; }

		[DisplayName("ماه سابقه")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? MonthsOfExperience { get; set; }

		[DisplayName("روز سابقه")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? DaysOfExperience { get; set; }

		[DisplayName("سال بیمه")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? YearsOfInsurance { get; set; }

		[DisplayName("ماه بیمه")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? MonthsOfInsurance { get; set; }

		[DisplayName("روز بیمه")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? DaysOfInsurance { get; set; }

		[DisplayName("شغل مورد علاقه")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(InterestedJobId))]
		public virtual Job? InterestedJob { get; set; }

		public long? InterestedJobId { get; set; }

		[DisplayName("شغل مورد علاقه (متن)")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(128)]
		public string? InterestedJobInText { get; set; }

		[DisplayName("فایل رزومه")]
		[DisplayInfo(null, true, type: SystemType.File)]
		[ForeignKey(nameof(ResumeFileId))]
		public virtual FileEntity? ResumeFile { get; set; }

		public long? ResumeFileId { get; set; }

		[DisplayName("مسیر فایل رزومه")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? ResumeFileAddress { get; set; }

		[DisplayName("مصاحبه تلفنی")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? HasTelephoneInterview { get; set; }

		[DisplayName("مصاحبه رفتاری")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? HasBehaviourInterview { get; set; }

		[DisplayName("مصاحبه تخصصی انجام شده")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? HasSpecializedInterview { get; set; }

		[DisplayName("آخرین بررسی هیئت مدیره")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public ResumeBoardCheckStatusEnum? LastCheckStatusByManager { get; set; }

		[DisplayName("شناسه ویرایش‌کننده HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsUpdatedUserId { get; set; }

		[DisplayName("مهلت بررسی رزومه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(16)]
		public string? ResumeReviewDeadline { get; set; }

		[DisplayName("دعوت به مصاحبه تخصصی")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? InviteForSpecializedInterview { get; set; }

		[DisplayName("تاریخ پیشنهادی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? RecommendedDateInText { get; set; }

		[DisplayName("ساعت پیشنهادی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? RecommendedTimeInText { get; set; }

		[DisplayName("اولویت دعوت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? PriorityToInvite { get; set; }

		[DisplayName("وضعیت بررسی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeReviewStatusEnum? Status { get; set; }

		[DisplayName("واحد معرفی‌شده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(UnitIntroducedId))]
		public virtual OrgUnit? UnitIntroduced { get; set; }

		public long? UnitIntroducedId { get; set; }

		[DisplayName("شناسه واحد معرفی HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsUnitIntroducedId { get; set; }

		[DisplayName("علت معرفی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? ReasonForIntroduction { get; set; }

		[DisplayName("مناسب برای شغل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(SuitableForJobId))]
		public virtual Job? SuitableForJob { get; set; }

		public long? SuitableForJobId { get; set; }

		[DisplayName("علت مناسب بودن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? ReasonForSuitableJob { get; set; }

		[DisplayName("توضیحات مدیر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? ManagerDescription { get; set; }

		[DisplayName("انصراف کارجو")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? CancelByJobApplicant { get; set; }

		[DisplayName("علت انصراف")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? JobApplicantCancelReason { get; set; }

		[DisplayName("دعوت به مصاحبه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsInvitedToInterview { get; set; }

		[DisplayName("تاریخ جلسه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? SessionDate { get; set; }

		[DisplayName("ساعت جلسه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? SessionTime { get; set; }

		[DisplayName("محل مصاحبه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? SessionPlace { get; set; }

		[DisplayName("شناسه گیرندگان ایمیل")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(512)]
		public string? EmailRecipientIds { get; set; }

		[DisplayName("ایمیل گیرندگان")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(512)]
		public string? EmailRecipientEmails { get; set; }

		[DisplayName("توضیحات کارشناس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? ExpertDescription { get; set; }

		[DisplayName("مصاحبه‌کننده تخصصی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(SpecializedInterviewerId))]
		public virtual Personel? SpecializedInterviewer { get; set; }

		public long? SpecializedInterviewerId { get; set; }

		[DisplayName("شناسه مصاحبه‌کننده تخصصی HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsSpecializedInterviewerId { get; set; }

		[DisplayName("وضعیت مصاحبه تخصصی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeSpecializedInterviewStatusEnum? SpecializedInterviewStatus { get; set; }

		[DisplayName("نقاط مثبت مصاحبه تخصصی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? SpecializedInterviewStatusPositivePoints { get; set; }

		[DisplayName("نقاط منفی مصاحبه تخصصی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? SpecializedInterviewStatusNegativePoints { get; set; }

		[DisplayName("اولویت مصاحبه تخصصی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? SpecializedInterviewPriority { get; set; }

		[DisplayName("واحد معرفی تخصصی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(SpecializedInterviewUnitIntroducedId))]
		public virtual OrgUnit? SpecializedInterviewUnitIntroduced { get; set; }

		public long? SpecializedInterviewUnitIntroducedId { get; set; }

		[DisplayName("شناسه واحد معرفی تخصصی HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsSpecializedInterviewUnitIntroducedId { get; set; }

		[DisplayName("علت معرفی تخصصی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? SpecializedInterviewReasonForIntroduction { get; set; }

		[DisplayName("واحد مناسب تخصصی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(SpecializedInterviewUnitSuitableId))]
		public virtual OrgUnit? SpecializedInterviewUnitSuitable { get; set; }

		public long? SpecializedInterviewUnitSuitableId { get; set; }

		[DisplayName("شناسه واحد مناسب تخصصی HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsSpecializedInterviewUnitSuitableId { get; set; }

		[DisplayName("علت مناسب بودن تخصصی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? SpecializedInterviewReasonForSuitableJob { get; set; }

		[DisplayName("توضیحات مصاحبه تخصصی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? SpecializedInterviewDescription { get; set; }

		[DisplayName("مصاحبه‌کننده منابع انسانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(HrInterviewerId))]
		public virtual Personel? HrInterviewer { get; set; }

		public long? HrInterviewerId { get; set; }

		[DisplayName("شناسه مصاحبه‌کننده منابع انسانی HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsHrInterviewerId { get; set; }

		[DisplayName("وضعیت مصاحبه منابع انسانی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeHrInterviewStatusEnum? HrInterviewerStatus { get; set; }

		[DisplayName("نقاط مثبت منابع انسانی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? HrInterviewerStatusPositivePoints { get; set; }

		[DisplayName("نقاط منفی منابع انسانی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? HrInterviewerStatusNegativePoints { get; set; }

		[DisplayName("مبلغ قرارداد")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ContractAmount { get; set; }

		[DisplayName("توضیحات منابع انسانی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? HrInterviewerDescription { get; set; }

		[DisplayName("شناسه هیئت مدیره")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(64)]
		public string? BoardOfDirectorIds { get; set; }

		[DisplayName("نام هیئت مدیره")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(512)]
		public string? BoardOfDirectorNames { get; set; }

		[DisplayName("شناسه ذینفعان")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(64)]
		public string? BeneficiaryIds { get; set; }

		[DisplayName("نام ذینفعان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? BeneficiaryNames { get; set; }

		[DisplayName("در کارتابل کارشناس")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? InExpertCartable { get; set; }

		[DisplayName("در کارتابل مدیر")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? InManagerCartable { get; set; }

		[DisplayName("در کارتابل مدیر منابع انسانی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? InHrmManagerCartable { get; set; }

		[DisplayName("در کارتابل هیئت مدیره")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? InBoardOfDirectorsCartable { get; set; }

		[DisplayName("در کارتابل امور اداری")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? InOfficeExpertCartable { get; set; }

		[DisplayName("شناسه ورود کارجو")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? LoginId { get; set; }

		[DisplayName("نام شرکت قبلی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(64)]
		public string? PreviousCompanyName { get; set; }

		[DisplayName("تلفن مدیر قبلی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(11)]
		public string? PreviousManagerPhoneNumber { get; set; }

		[DisplayName("تلفن شرکت قبلی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(11)]
		public string? PreviousCompanyPhoneNumber { get; set; }

		[DisplayName("موبایل شرکت قبلی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(11)]
		public string? PreviousCompanyMobileNumber { get; set; }

		[DisplayName("توضیحات شرکت قبلی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(512)]
		public string? PreviousCompanyDescription { get; set; }

		[DisplayName("تایید استعلام")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? ReferenceCheckIsApproved { get; set; }

		public virtual ICollection<ResumeLanguage> Languages { get; set; } = new List<ResumeLanguage>();
		public virtual ICollection<ResumeSoftware> Softwares { get; set; } = new List<ResumeSoftware>();
		public virtual ICollection<ResumeSkill> Skills { get; set; } = new List<ResumeSkill>();
		public virtual ICollection<ResumeWorkExperience> WorkExperiences { get; set; } = new List<ResumeWorkExperience>();
		public virtual ICollection<ResumeEducationalStatus> EducationalStatuses { get; set; } = new List<ResumeEducationalStatus>();
		public virtual ICollection<ResumeComment> Comments { get; set; } = new List<ResumeComment>();
		public virtual ICollection<ResumeAttachment> Attachments { get; set; } = new List<ResumeAttachment>();
		public virtual ICollection<ResumeFinalInformation> FinalInformations { get; set; } = new List<ResumeFinalInformation>();
		public virtual ICollection<ResumeFurtherInformation> FurtherInformations { get; set; } = new List<ResumeFurtherInformation>();
	}

	public class ResumeConfiguration : IEntityTypeConfiguration<Resume>
	{
		public void Configure(EntityTypeBuilder<Resume> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_Resume_HtsId");
			builder.HasIndex(x => x.MobileNumber).HasDatabaseName("IX_Hrm_Resume_MobileNumber");

			builder.HasOne(x => x.StaffRequest).WithMany().HasForeignKey(x => x.StaffRequestId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.StudyCourse).WithMany().HasForeignKey(x => x.StudyCourseId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.TrendOfStudyCourse).WithMany().HasForeignKey(x => x.TrendOfStudyCourseId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.InterestedPost).WithMany().HasForeignKey(x => x.InterestedPostId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.IncomingSource).WithMany().HasForeignKey(x => x.IncomingSourceId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.RepresentingPersonel).WithMany().HasForeignKey(x => x.RepresentingPersonelId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.HomeProvince).WithMany().HasForeignKey(x => x.HomeProvinceId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.HomeCity).WithMany().HasForeignKey(x => x.HomeCityId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.InterestedJob).WithMany().HasForeignKey(x => x.InterestedJobId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.ResumeFile).WithMany().HasForeignKey(x => x.ResumeFileId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.UnitIntroduced).WithMany().HasForeignKey(x => x.UnitIntroducedId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.SuitableForJob).WithMany().HasForeignKey(x => x.SuitableForJobId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.SpecializedInterviewer).WithMany().HasForeignKey(x => x.SpecializedInterviewerId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.SpecializedInterviewUnitIntroduced).WithMany().HasForeignKey(x => x.SpecializedInterviewUnitIntroducedId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.SpecializedInterviewUnitSuitable).WithMany().HasForeignKey(x => x.SpecializedInterviewUnitSuitableId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.HrInterviewer).WithMany().HasForeignKey(x => x.HrInterviewerId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
