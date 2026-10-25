using Common.Attributes;
using Entities.App.Hcm;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	[Display(Name = "اطلاعات تکمیلی رزومه")]
	[Table("ResumeFinalInformation", Schema = "Hrm")]
	public class ResumeFinalInformation : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[ForeignKey(nameof(ResumeId))]
		public virtual Resume? Resume { get; set; }

		public long ResumeId { get; set; }

		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? FullName { get; set; }

		[DisplayName("موبایل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(11)]
		public string? MobileNumber { get; set; }

		[DisplayName("واحد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(DepartmentId))]
		public virtual OrgUnit? Department { get; set; }

		public long? DepartmentId { get; set; }

		[DisplayName("شناسه واحد HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsDepartmentId { get; set; }

		[DisplayName("شغل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(JobId))]
		public virtual Job? Job { get; set; }

		public long? JobId { get; set; }

		[DisplayName("مدیر")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(ManagerId))]
		public virtual Personel? Manager { get; set; }

		public long? ManagerId { get; set; }

		[DisplayName("شناسه مدیر HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsManagerId { get; set; }

		[DisplayName("سرپرست")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(SupervisorId))]
		public virtual Personel? Supervisor { get; set; }

		public long? SupervisorId { get; set; }

		[DisplayName("شناسه سرپرست HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsSupervisorId { get; set; }

		[DisplayName("تاریخ شروع قرارداد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? StartDateOfAgreement { get; set; }

		[DisplayName("محل کار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? Workplace { get; set; }

		[DisplayName("مبلغ قرارداد")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ContractAmount { get; set; }

		[DisplayName("شناسه هیئت مدیره")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? BoardOfDirectorIds { get; set; }

		[DisplayName("هیئت مدیره")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? BoardOfDirectorString { get; set; }

		[DisplayName("مبلغ سفته")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? PromissoryNoteAmount { get; set; }

		[DisplayName("نوع کامپیوتر")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(10)]
		public string? ComputerType { get; set; }

		[DisplayName("نرم‌افزارهای مورد نیاز")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? RequiredSoftwares { get; set; }

		[DisplayName("ایمیل سازمانی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(32)]
		public string? OrganizationEmailAddress { get; set; }

		[DisplayName("نام کاربری")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(32)]
		public string? HtsUsername { get; set; }

		[DisplayName("رمز سیستم")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(32)]
		public string? SystemPassword { get; set; }

		[DisplayName("تلفن داخلی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(4)]
		public string? InternalPhoneNumber { get; set; }

		[DisplayName("شناسه کاربر HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsCreatedUserId { get; set; }

		[DisplayName("تاریخ ثبت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(16)]
		public string? CreatedDateInText { get; set; }
	}

	public class ResumeFinalInformationConfiguration : IEntityTypeConfiguration<ResumeFinalInformation>
	{
		public void Configure(EntityTypeBuilder<ResumeFinalInformation> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_ResumeFinalInformation_HtsId");
			builder.HasOne(x => x.Resume).WithMany(x => x.FinalInformations).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Supervisor).WithMany().HasForeignKey(x => x.SupervisorId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
