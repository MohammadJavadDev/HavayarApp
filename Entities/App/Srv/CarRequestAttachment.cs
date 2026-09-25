using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>پیوست درخواست خودرو — معادل ردیف‌های <c>Srv_Attachment</c> با <c>CarRequestId</c>.</summary>
	[Display(Name = "پیوست درخواست خودرو")]
	[Table("CarRequestAttachment", Schema = "Srv")]
	public class CarRequestAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست خودرو")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(CarRequestId))]
		public virtual CarRequest? CarRequest { get; set; }
		public long CarRequestId { get; set; }

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

	public class CarRequestAttachmentConfiguration : IEntityTypeConfiguration<CarRequestAttachment>
	{
		public void Configure(EntityTypeBuilder<CarRequestAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_CarRequestAttachment_HtsId");

			builder.HasOne(x => x.CarRequest)
				.WithMany(x => x.Attachments)
				.HasForeignKey(x => x.CarRequestId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
