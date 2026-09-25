using Common.Attributes;
using Entities.App.Inv;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>قلم فاکتور CNG — معادل HTS <c>Cng_InvoiceItems</c>.</summary>
	[Display(Name = "اقلام فاکتور")]
	[Table("InvoiceItem", Schema = "Cng")]
	public class InvoiceItem : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("فاکتور")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(InvoiceId))]
		public virtual Invoice? Invoice { get; set; }
		public long InvoiceId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PartId))]
		public virtual Part? Part { get; set; }
		public long PartId { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public short Quantity { get; set; }

		[DisplayName("مبلغ")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long Price { get; set; }

		[DisplayName("کاربر ایجادکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(CreatedUserId))]
		public virtual User? CreatedUser { get; set; }
		public long CreatedUserId { get; set; }

		[DisplayName("تاریخ ایجاد میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime CreatedDate { get; set; }

		[DisplayName("تاریخ ایجاد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string CreatedDateInText { get; set; } = "";
	}

	public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
	{
		public void Configure(EntityTypeBuilder<InvoiceItem> builder)
		{
			builder.HasOne(x => x.Invoice)
				.WithMany()
				.HasForeignKey(x => x.InvoiceId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.CreatedUser)
				.WithMany()
				.HasForeignKey(x => x.CreatedUserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_InvoiceItem_HtsId");
		}
	}
}
