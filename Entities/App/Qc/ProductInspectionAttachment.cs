using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Qc
{
	[Display(Name = "پیوست بازرسی محصول")]
	[Table("ProductInspectionAttachment", Schema = "Qc")]
	public class ProductInspectionAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long ProductInspectionId { get; set; }

		[ForeignKey(nameof(ProductInspectionId))]
		public virtual ProductInspection? ProductInspection { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File, maxFileSize: 20)]
		[ForeignKey(nameof(FileId))]
		public virtual FileEntity? File { get; set; }

		public long? FileId { get; set; }

		[DisplayName("عنوان فایل پیوست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? OriginalFileName { get; set; }

		[DisplayName("مسیر فایل قدیم")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? SourceFilePath { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		[NotMapped]
		public long? FileId2 { get; set; }

		[NotMapped]
		public long? FileId3 { get; set; }
	}

	public class ProductInspectionAttachmentConfiguration : IEntityTypeConfiguration<ProductInspectionAttachment>
	{
		public void Configure(EntityTypeBuilder<ProductInspectionAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Qc_ProductInspectionAttachment_HtsId");

			builder.HasOne(x => x.File)
				.WithMany()
				.HasForeignKey(x => x.FileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
