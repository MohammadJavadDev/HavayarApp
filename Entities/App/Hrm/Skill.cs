using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>مهارت — معادل HTS <c>HRM_Skill</c>.</summary>
	[Display(Name = "مهارت")]
	[Table("Skill", Schema = "Hrm")]
	public class Skill : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(400)]
		public string? Title { get; set; }
	}

	public class SkillConfiguration : IEntityTypeConfiguration<Skill>
	{
		public void Configure(EntityTypeBuilder<Skill> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_Skill_HtsId");
		}
	}
}
