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
	[Display(Name = "وضعیت تحصیل رزومه")]
	[Table("ResumeEducationalStatus", Schema = "Hrm")]
	public class ResumeEducationalStatus : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[ForeignKey(nameof(ResumeId))]
		public virtual Resume? Resume { get; set; }

		public long ResumeId { get; set; }

		[DisplayName("مقطع")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeEducationEnum? Degree { get; set; }

		/// <summary>شناسه لوکاپ مقطع وقتی با enum مقطع فرم یکی نیست.</summary>
		[DisplayName("شناسه مقطع HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HtsDegreeId { get; set; }

		[DisplayName("رشته")]
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
		public string? StudyFieldInText { get; set; }

		[DisplayName("گرایش (متن)")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(128)]
		public string? StudyMajorInText { get; set; }

		[DisplayName("موسسه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? EducationalInstitutionTitle { get; set; }

		[DisplayName("محل تحصیل")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(64)]
		public string? EducationPlace { get; set; }

		[DisplayName("تاریخ فارغ‌التحصیلی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(10)]
		public string? GraduationDate { get; set; }

		[DisplayName("معدل")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(10)]
		public string? GradeAverage { get; set; }

		[DisplayName("دانشجو است")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? IsStudent { get; set; }
	}

	public class ResumeEducationalStatusConfiguration : IEntityTypeConfiguration<ResumeEducationalStatus>
	{
		public void Configure(EntityTypeBuilder<ResumeEducationalStatus> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_ResumeEducationalStatus_HtsId");
			builder.HasOne(x => x.Resume).WithMany(x => x.EducationalStatuses).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.StudyCourse).WithMany().HasForeignKey(x => x.StudyCourseId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.TrendOfStudyCourse).WithMany().HasForeignKey(x => x.TrendOfStudyCourseId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
