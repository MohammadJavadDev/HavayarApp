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
	/// <summary>درخواست کالا CNG — معادل HTS <c>Cng_PartRequest</c>.</summary>
	[Display(Name = "درخواست کالا")]
	[Table("PartRequest", Schema = "Cng")]
	public class PartRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("شرح درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? RequestDescription { get; set; }

		[DisplayName("نام فایل پیوست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? AttachmentFileName { get; set; }

		[DisplayName("حجم فایل پیوست")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? AttachmentFileSize { get; set; }

		[DisplayName("مسیر فایل پیوست")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(255)]
		public string? AttachmentFilePath { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PartRequestStatusEnum LastStatusId { get; set; }

		[DisplayName("کامنت وضعیت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? LastStatusComment { get; set; }

		[DisplayName("شماره پیش فاکتور")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(25)]
		public string? InvoiceNumber { get; set; }

		[DisplayName("ارسال فوری")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? ImmediateSending { get; set; }

		[DisplayName("محل ارسال")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? SendingLocation { get; set; }

		[DisplayName("گیرنده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? Receiver { get; set; }

		/// <summary>ستون گرید / DB — بدون اینپوت در فرم (همه ردیف‌های HTS خالی‌اند).</summary>
		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }

		/// <summary>اقلام فرم — فقط با ذخیره هدر نوشته می‌شوند؛ در DB این جدول جداست.</summary>
		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public List<PartRequestItem>? Items { get; set; }
	}

	public class PartRequestConfiguration : IEntityTypeConfiguration<PartRequest>
	{
		public void Configure(EntityTypeBuilder<PartRequest> builder)
		{
			builder.HasOne(x => x.File)
				.WithMany()
				.HasForeignKey(x => x.FileId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_PartRequest_HtsId");
		}
	}
}
