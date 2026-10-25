using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>قطعه ورود/خروج اضطراری — معادل HTS <c>Enex_Part</c>. صفحه جدا ندارد؛ کمبوی فرم ورود از همین کاتالوگ است.</summary>
	[Display(Name = "قطعه ورود اضطراری")]
	[Table("EnexPart", Schema = "Hrm")]
	public class EnexPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(256)]
		public string Title { get; set; } = "";

		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(50)]
		public string? Code { get; set; }

		[DisplayName("واحد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PartUnitId))]
		public virtual PartUnit? PartUnit { get; set; }
		public long? PartUnitId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class EnexPartConfiguration : IEntityTypeConfiguration<EnexPart>
	{
		public void Configure(EntityTypeBuilder<EnexPart> builder)
		{
			builder.HasOne(x => x.PartUnit).WithMany().HasForeignKey(x => x.PartUnitId).OnDelete(DeleteBehavior.Restrict);
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_EnexPart_HtsId");
		}
	}
}
