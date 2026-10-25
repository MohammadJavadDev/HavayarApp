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
	[Display(Name = "زبان رزومه")]
	[Table("ResumeLanguage", Schema = "Hrm")]
	public class ResumeLanguage : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[ForeignKey(nameof(ResumeId))]
		public virtual Resume? Resume { get; set; }

		public long ResumeId { get; set; }

		[DisplayName("زبان")]
		[DisplayInfo(null, true, type: SystemType.Entity, showInRelationData: true)]
		[ForeignKey(nameof(LanguageId))]
		public virtual TrainingLanguage? Language { get; set; }

		public long? LanguageId { get; set; }

		[DisplayName("سطح آشنایی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeKnowledgeLevelEnum? KnowledgeLevel { get; set; }

		[DisplayName("مکاتبه عمومی")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public ResumeKnowledgeLevelEnum? NormalCorrespondenceLevel { get; set; }

		[DisplayName("مکاتبه تخصصی")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public ResumeKnowledgeLevelEnum? SpecializedCorrespondenceLevel { get; set; }

		[DisplayName("درک مطلب")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public ResumeKnowledgeLevelEnum? UnderstandTextLevel { get; set; }
	}

	public class ResumeLanguageConfiguration : IEntityTypeConfiguration<ResumeLanguage>
	{
		public void Configure(EntityTypeBuilder<ResumeLanguage> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_ResumeLanguage_HtsId");
			builder.HasOne(x => x.Resume).WithMany(x => x.Languages).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.Language).WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
