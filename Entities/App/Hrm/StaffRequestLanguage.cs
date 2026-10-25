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
	[Display(Name = "زبان درخواست نیرو")]
	[Table("StaffRequestLanguage", Schema = "Hrm")]
	public class StaffRequestLanguage : BaseEntity
	{
		public long HtsId { get; set; }

		[ForeignKey(nameof(StaffRequestId))]
		public virtual StaffRequest? StaffRequest { get; set; }

		public long StaffRequestId { get; set; }

		[DisplayName("زبان")]
		[DisplayInfo(null, true, type: SystemType.Entity, showInRelationData: true)]
		[ForeignKey(nameof(LanguageId))]
		public virtual TrainingLanguage? Language { get; set; }

		public long? LanguageId { get; set; }

		[DisplayName("سطح")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeKnowledgeLevelEnum? KnowledgeLevel { get; set; }
	}

	public class StaffRequestLanguageConfiguration : IEntityTypeConfiguration<StaffRequestLanguage>
	{
		public void Configure(EntityTypeBuilder<StaffRequestLanguage> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)").HasDatabaseName("IX_Hrm_StaffRequestLanguage_HtsId");
			builder.HasOne(x => x.StaffRequest).WithMany(x => x.Languages).HasForeignKey(x => x.StaffRequestId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.Language).WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
