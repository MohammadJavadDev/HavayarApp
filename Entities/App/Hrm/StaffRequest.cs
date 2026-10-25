using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Hrm.Enums;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>درخواست نیرو — معادل HTS <c>HRM_StaffRequest</c>.</summary>
	[Display(Name = "درخواست نیرو")]
	[Table("StaffRequest", Schema = "Hrm")]
	public class StaffRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("واحد درخواست‌کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(RequestOrgUnitId))]
		public virtual OrgUnit? RequestOrgUnit { get; set; }

		public long? RequestOrgUnitId { get; set; }

		[DisplayName("شناسه واحد HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsRequestOrgUnitId { get; set; }

		[DisplayName("پرسنل درخواست‌کننده")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(RequestPersonelId))]
		public virtual Personel? RequestPersonel { get; set; }

		public long? RequestPersonelId { get; set; }

		[DisplayName("شناسه پرسنل HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsRequestPersonelId { get; set; }

		[DisplayName("کاربر درخواست‌کننده")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(RequestUserId))]
		public virtual User? RequestUser { get; set; }

		public long? RequestUserId { get; set; }

		[DisplayName("شناسه کاربر HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsRequestUserId { get; set; }

		[DisplayName("شغل")]
		[DisplayInfo(null, true, type: SystemType.Entity, showInRelationData: true)]
		[ForeignKey(nameof(JobId))]
		public virtual Job? Job { get; set; }

		public long? JobId { get; set; }

		[DisplayName("تاریخ درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? RequestShamsiDate { get; set; }

		[DisplayName("ساعت درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? RequestTime { get; set; }

		[DisplayName("تاییدکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ApproverPersonelId))]
		public virtual Personel? ApproverPersonel { get; set; }

		public long? ApproverPersonelId { get; set; }

		[DisplayName("شناسه تاییدکننده HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsApproverPersonelId { get; set; }

		[DisplayName("تعداد مورد نیاز")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public byte RequiredNumber { get; set; }

		[DisplayName("نوع همکاری")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ResumeEmploymentTypeEnum? CooperationType { get; set; }

		[DisplayName("جنسیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public StaffRequestSexEnum? Sex { get; set; }

		[DisplayName("تاهل")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public StaffRequestMaritalEnum? MaritalStatus { get; set; }

		[DisplayName("از سن")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public byte? FromAge { get; set; }

		[DisplayName("تا سن")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public byte? ToAge { get; set; }

		[DisplayName("وضعیت جایگزینی")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public StaffRequestEmploymentKindEnum? EmploymentKind { get; set; }

		[DisplayName("نیروی جایگزین")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ReplacePersonelId))]
		public virtual Personel? ReplacePersonel { get; set; }

		public long? ReplacePersonelId { get; set; }

		[DisplayName("شناسه جایگزین HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsReplacePersonelId { get; set; }

		[DisplayName("عنوان جایگزین")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(64)]
		public string? ReplacePersonelTitle { get; set; }

		[DisplayName("سابقه مورد نیاز")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public byte? ExperienceRequired { get; set; }

		[DisplayName("دیپلم")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? EducationDiploma { get; set; }

		[DisplayName("فوق دیپلم")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? EducationAssociate { get; set; }

		[DisplayName("لیسانس")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? EducationBachelor { get; set; }

		[DisplayName("فوق لیسانس")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? EducationMaster { get; set; }

		[DisplayName("رشته اول")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(StudyCourseOneId))]
		public virtual StudyCourse? StudyCourseOne { get; set; }

		public long? StudyCourseOneId { get; set; }

		[DisplayName("رشته دوم")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(StudyCourseTwoId))]
		public virtual StudyCourse? StudyCourseTwo { get; set; }

		public long? StudyCourseTwoId { get; set; }

		[DisplayName("رشته سوم")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(StudyCourseThreeId))]
		public virtual StudyCourse? StudyCourseThree { get; set; }

		public long? StudyCourseThreeId { get; set; }

		[DisplayName("شهر محل خدمت")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(CityId))]
		public virtual Region? City { get; set; }

		public long? CityId { get; set; }

		[DisplayName("شناسه شهر HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsCityId { get; set; }

		[DisplayName("محل کار")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public StaffRequestWorkplaceEnum? Workplace { get; set; }

		[DisplayName("شرح وظایف")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(4000)]
		public string? TasksExpected { get; set; }

		[DisplayName("ویژگی شخصیتی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? PersonalityFeature { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public StaffRequestStatusEnum? Status { get; set; }

		[DisplayName("تایید مدیر")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? HasManagerAccept { get; set; }

		[DisplayName("مخفی از سرپرست")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? HiddenFromSupervisor { get; set; }

		[DisplayName("شناسه ویرایش‌کننده HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsUpdatedUserId { get; set; }

		public virtual ICollection<StaffRequestLanguage> Languages { get; set; } = new List<StaffRequestLanguage>();
		public virtual ICollection<StaffRequestSkill> Skills { get; set; } = new List<StaffRequestSkill>();
		public virtual ICollection<StaffRequestSoftware> Softwares { get; set; } = new List<StaffRequestSoftware>();
		public virtual ICollection<StaffRequestStaffer> Staffers { get; set; } = new List<StaffRequestStaffer>();
		public virtual ICollection<StaffRequestConfirm> Confirms { get; set; } = new List<StaffRequestConfirm>();
	}

	public class StaffRequestConfiguration : IEntityTypeConfiguration<StaffRequest>
	{
		public void Configure(EntityTypeBuilder<StaffRequest> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_StaffRequest_HtsId");

			builder.HasOne(x => x.RequestOrgUnit).WithMany().HasForeignKey(x => x.RequestOrgUnitId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.RequestPersonel).WithMany().HasForeignKey(x => x.RequestPersonelId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.RequestUser).WithMany().HasForeignKey(x => x.RequestUserId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.ApproverPersonel).WithMany().HasForeignKey(x => x.ApproverPersonelId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.ReplacePersonel).WithMany().HasForeignKey(x => x.ReplacePersonelId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.StudyCourseOne).WithMany().HasForeignKey(x => x.StudyCourseOneId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.StudyCourseTwo).WithMany().HasForeignKey(x => x.StudyCourseTwoId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.StudyCourseThree).WithMany().HasForeignKey(x => x.StudyCourseThreeId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
