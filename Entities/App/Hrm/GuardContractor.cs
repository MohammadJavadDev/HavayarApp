using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>پیمانکار حراست/دژبان — معادل HTS <c>HRM_Guard_Contractor</c>.</summary>
	[Display(Name = "پیمانکار (حراست/دژبان)")]
	[Table("GuardContractor", Schema = "Hrm")]
	public class GuardContractor : BaseEntity
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

		[DisplayName("شماره تماس")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string? PhoneNumber { get; set; }

		[DisplayName("آدرس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? Address { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class GuardContractorConfiguration : IEntityTypeConfiguration<GuardContractor>
	{
		public void Configure(EntityTypeBuilder<GuardContractor> builder)
		{
			builder.Property(x => x.FullName)
				.HasComputedColumnSql("(ltrim(rtrim(isnull([Name],'')+' '+isnull([Family],''))))", stored: false);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_GuardContractor_HtsId");
		}
	}
}
