using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>پیوست درخواست پیک — معادل ردیف‌های <c>Srv_Attachment</c> با <c>AmbassadorRequestId</c>.</summary>
	[Display(Name = "پیوست درخواست پیک")]
	[Table("AmbassadorRequestAttachment", Schema = "Srv")]
	public class AmbassadorRequestAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست پیک")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(AmbassadorRequestId))]
		public virtual AmbassadorRequest? AmbassadorRequest { get; set; }
		public long AmbassadorRequestId { get; set; }

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
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

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

	public class AmbassadorRequestAttachmentConfiguration : IEntityTypeConfiguration<AmbassadorRequestAttachment>
	{
		public void Configure(EntityTypeBuilder<AmbassadorRequestAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_AmbassadorRequestAttachment_HtsId");

			builder.HasOne(x => x.AmbassadorRequest)
				.WithMany(x => x.Attachments)
				.HasForeignKey(x => x.AmbassadorRequestId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
