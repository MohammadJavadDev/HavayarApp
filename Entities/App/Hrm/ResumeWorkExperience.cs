using Common.Attributes;
using Entities.App.Hrm.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	[Display(Name = "سابقه کار رزومه")]
	[Table("ResumeWorkExperience", Schema = "Hrm")]
	public class ResumeWorkExperience : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[ForeignKey(nameof(ResumeId))]
		public virtual Resume? Resume { get; set; }

		public long ResumeId { get; set; }

		[DisplayName("نام شرکت")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(128)]
		public string? CompanyName { get; set; }

		[DisplayName("سمت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? OrgPost { get; set; }

		[DisplayName("از تاریخ")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? StartDate { get; set; }

		[DisplayName("تا تاریخ")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? EndDate { get; set; }

		[DisplayName("نام مدیر قبلی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? OldManagerFullName { get; set; }

		[DisplayName("تلفن مدیر قبلی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(32)]
		public string? OldManagerPhoneNumber { get; set; }

		[DisplayName("نوع همکاری")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeEmploymentTypeEnum? EmploymentType { get; set; }

		[DisplayName("تعداد همکاران")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? CollaboratorsNumber { get; set; }

		[DisplayName("تعداد پرسنل تحت نظارت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? SupervisedPersonnelNumber { get; set; }

		[DisplayName("مبلغ حقوق")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? SalaryAmount { get; set; }

		[DisplayName("علت ترک")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? LeavingReason { get; set; }

		[DisplayName("تلفن شرکت")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(11)]
		public string? CompanyPhone { get; set; }

		[DisplayName("موبایل شرکت")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(11)]
		public string? CompanyMobile { get; set; }

		[DisplayName("هنوز مشغول است")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? IsStillWorking { get; set; }
	}

	public class ResumeWorkExperienceConfiguration : IEntityTypeConfiguration<ResumeWorkExperience>
	{
		public void Configure(EntityTypeBuilder<ResumeWorkExperience> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_ResumeWorkExperience_HtsId");
			builder.HasOne(x => x.Resume).WithMany(x => x.WorkExperiences).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
