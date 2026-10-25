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
	/// <summary>کالای حراست/دژبان — معادل HTS <c>HRM_Guard_Part</c>.</summary>
	[Display(Name = "کالا (حراست/دژبان)")]
	[Table("GuardPart", Schema = "Hrm")]
	public class GuardPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(256)]
		public string Title { get; set; } = "";

		[DisplayName("واحد شمارش")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public GuardPartUnitEnum? EnumerationUnit { get; set; }

		[DisplayName("غیرفعال")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsDisable { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class GuardPartConfiguration : IEntityTypeConfiguration<GuardPart>
	{
		public void Configure(EntityTypeBuilder<GuardPart> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_GuardPart_HtsId");
		}
	}
}
