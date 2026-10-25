using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>مکان/منطقه حراست/دژبان — معادل HTS <c>HRM_Guard_Location</c>.</summary>
	[Display(Name = "اماکن / مناطق (حراست/دژبان)")]
	[Table("GuardLocation", Schema = "Hrm")]
	public class GuardLocation : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(128)]
		public string Title { get; set; } = "";

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class GuardLocationConfiguration : IEntityTypeConfiguration<GuardLocation>
	{
		public void Configure(EntityTypeBuilder<GuardLocation> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_GuardLocation_HtsId");
		}
	}
}
