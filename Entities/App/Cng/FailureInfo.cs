using Common.Attributes;
using Entities.App.Cng.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>پارامترهای فنی CNG — معادل HTS <c>Cng_FailureInfo</c>.</summary>
	[Display(Name = "پارامترهای فنی")]
	[Table("FailureInfo", Schema = "Cng")]
	public class FailureInfo : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("گروه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public FailureInfoGroupEnum? Group { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(255)]
		public string Title { get; set; } = "";

		[DisplayName("مقدار اولیه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? InitialValue { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class FailureInfoConfiguration : IEntityTypeConfiguration<FailureInfo>
	{
		public void Configure(EntityTypeBuilder<FailureInfo> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_FailureInfo_HtsId");
		}
	}
}
