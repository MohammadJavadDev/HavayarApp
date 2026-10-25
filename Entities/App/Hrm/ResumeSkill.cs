using Common.Attributes;
using Entities.Base;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	[Display(Name = "مهارت رزومه")]
	[Table("ResumeSkill", Schema = "Hrm")]
	public class ResumeSkill : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[ForeignKey(nameof(ResumeId))]
		public virtual Resume? Resume { get; set; }

		public long ResumeId { get; set; }

		[DisplayName("مهارت")]
		[DisplayInfo(null, true, type: SystemType.Entity, showInRelationData: true)]
		[ForeignKey(nameof(SkillId))]
		public virtual Skill? Skill { get; set; }

		public long? SkillId { get; set; }
	}

	public class ResumeSkillConfiguration : IEntityTypeConfiguration<ResumeSkill>
	{
		public void Configure(EntityTypeBuilder<ResumeSkill> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_ResumeSkill_HtsId");
			builder.HasOne(x => x.Resume).WithMany(x => x.Skills).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.Skill).WithMany().HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
