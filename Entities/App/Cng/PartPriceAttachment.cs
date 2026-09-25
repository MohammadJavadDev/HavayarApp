using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>پیوست قیمت قطعات CNG — معادل HTS <c>Sale_CngPartPriceAttachment</c>.</summary>
	[Display(Name = "پیوست قیمت قطعات")]
	[Table("PartPriceAttachment", Schema = "Cng")]
	public class PartPriceAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("قیمت قطعه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PartPriceId))]
		public virtual PartPrice? PartPrice { get; set; }
		public long PartPriceId { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File, required: true)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }

		/// <summary>موقت برای آپلود چندفایلی در یک ذخیره — ذخیره نمی‌شود.</summary>
		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? FileIds { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class PartPriceAttachmentConfiguration : IEntityTypeConfiguration<PartPriceAttachment>
	{
		public void Configure(EntityTypeBuilder<PartPriceAttachment> builder)
		{
			builder.HasOne(x => x.PartPrice)
				.WithMany()
				.HasForeignKey(x => x.PartPriceId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.File)
				.WithMany()
				.HasForeignKey(x => x.FileId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_PartPriceAttachment_HtsId");
		}
	}
}
