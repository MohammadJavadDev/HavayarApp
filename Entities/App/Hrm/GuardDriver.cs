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
	/// <summary>راننده حراست/دژبان — معادل HTS <c>HRM_Guard_Driver</c>.</summary>
	[Display(Name = "راننده (حراست/دژبان)")]
	[Table("GuardDriver", Schema = "Hrm")]
	public class GuardDriver : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string Name { get; set; } = "";

		[DisplayName("نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string? Family { get; set; }

		[DisplayName("نام و نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(101)]
		public string? FullName { get; set; }

		[DisplayName("نوع وسیله نقلیه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public GuardVehicleTypeEnum CarType { get; set; }

		[DisplayName("وسیله نقلیه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CarName { get; set; }

		[DisplayName("پلاک وسیله نقلیه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? LicensePlate { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class GuardDriverConfiguration : IEntityTypeConfiguration<GuardDriver>
	{
		public void Configure(EntityTypeBuilder<GuardDriver> builder)
		{
			builder.Property(x => x.FullName)
				.HasComputedColumnSql("(ltrim(rtrim(isnull([Name],'')+' '+isnull([Family],''))))", stored: false);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_GuardDriver_HtsId");
		}
	}
}
