using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>پیوست تعمیرات CNG — معادل HTS <c>Cng_RepairsAttachment</c>.</summary>
	[Display(Name = "پیوست تعمیرات")]
	[Table("RepairsAttachment", Schema = "Cng")]
	public class RepairsAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(RepairsId))]
		public virtual Repairs? Repairs { get; set; }
		public long RepairsId { get; set; }

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
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class RepairsAttachmentConfiguration : IEntityTypeConfiguration<RepairsAttachment>
	{
		public void Configure(EntityTypeBuilder<RepairsAttachment> builder)
		{
			builder.HasOne(x => x.Repairs)
				.WithMany()
				.HasForeignKey(x => x.RepairsId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.File)
				.WithMany()
				.HasForeignKey(x => x.FileId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_RepairsAttachment_HtsId");
		}
	}
}
