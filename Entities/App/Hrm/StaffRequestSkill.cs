using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	[Display(Name = "مهارت درخواست نیرو")]
	[Table("StaffRequestSkill", Schema = "Hrm")]
	public class StaffRequestSkill : BaseEntity
	{
		public long HtsId { get; set; }

		[ForeignKey(nameof(StaffRequestId))]
		public virtual StaffRequest? StaffRequest { get; set; }

		public long StaffRequestId { get; set; }

		[DisplayName("مهارت")]
		[DisplayInfo(null, true, type: SystemType.Entity, showInRelationData: true)]
		[ForeignKey(nameof(SkillId))]
		public virtual Skill? Skill { get; set; }

		public long? SkillId { get; set; }
	}

	public class StaffRequestSkillConfiguration : IEntityTypeConfiguration<StaffRequestSkill>
	{
		public void Configure(EntityTypeBuilder<StaffRequestSkill> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)").HasDatabaseName("IX_Hrm_StaffRequestSkill_HtsId");
			builder.HasOne(x => x.StaffRequest).WithMany(x => x.Skills).HasForeignKey(x => x.StaffRequestId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.Skill).WithMany().HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
