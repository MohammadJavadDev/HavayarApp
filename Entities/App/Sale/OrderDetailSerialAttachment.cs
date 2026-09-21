using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "پیوست سریال حواله فروش")]
	[Table("OrderDetailSerialAttachment", Schema = "Sale")]
	public class OrderDetailSerialAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("سریال")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(OrderDetailSerialId))]
		public virtual OrderDetailSerial? OrderDetailSerial { get; set; }
		public long? OrderDetailSerialId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? Title { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File, required: true)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2400)]
		public string? Comment { get; set; }
	}

	public class OrderDetailSerialAttachmentConfiguration : IEntityTypeConfiguration<OrderDetailSerialAttachment>
	{
		public void Configure(EntityTypeBuilder<OrderDetailSerialAttachment> builder)
		{
			builder.HasOne(x => x.OrderDetailSerial)
				.WithMany()
				.HasForeignKey(x => x.OrderDetailSerialId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_OrderDetailSerialAttachment_HtsId");
		}
	}
}
