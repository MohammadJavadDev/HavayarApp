using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>پیوست درخواست PM — معادل ردیف‌های <c>Srv_Attachment</c> با <c>PmRequestId</c>.</summary>
	[Display(Name = "پیوست درخواست PM")]
	[Table("PmRequestAttachment", Schema = "Srv")]
	public class PmRequestAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست PM")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PmRequestId))]
		public virtual PmRequest? PmRequest { get; set; }
		public long PmRequestId { get; set; }

		[DisplayName("نام فایل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? FileName { get; set; }

		[DisplayName("محتوای فایل")]
		[DisplayInfo(null, false, type: SystemType.File, required: true)]
		public byte[] FileContent { get; set; } = Array.Empty<byte>();

		[DisplayName("حجم فایل")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? FileSize { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(4000)]
		public string Comment { get; set; } = "";

		/// <summary>تاریخ ایجاد شمسی HTS (<c>Srv_Attachment.CreatedDate</c>، char(10)).</summary>
		[DisplayName("تاریخ ایجاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(10)]
		public string? CreatedDate { get; set; }

		[DisplayName("ساعت ایجاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(5)]
		public string? CreatedTime { get; set; }
	}

	public class PmRequestAttachmentConfiguration : IEntityTypeConfiguration<PmRequestAttachment>
	{
		public void Configure(EntityTypeBuilder<PmRequestAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_PmRequestAttachment_HtsId");

			builder.HasOne(x => x.PmRequest)
				.WithMany(x => x.Attachments)
				.HasForeignKey(x => x.PmRequestId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
