using Common.Attributes;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>فاکتور CNG — معادل HTS <c>Cng_Invoice</c>.</summary>
	[Display(Name = "فاکتور")]
	[Table("Invoice", Schema = "Cng")]
	public class Invoice : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(CustomerId))]
		public virtual Customer? Customer { get; set; }
		public long CustomerId { get; set; }

		[DisplayName("شماره فاکتور")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string InvoiceNumber { get; set; } = "";

		/// <summary>تاریخ فاکتور شمسی — معادل HTS <c>InvoiceDate</c> (nvarchar).</summary>
		[DisplayName("تاریخ فاکتور")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string InvoiceDate { get; set; } = "";

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

		/// <summary>اقلام فرم — فقط با ذخیره هدر نوشته می‌شوند.</summary>
		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public List<InvoiceItem>? Items { get; set; }
	}

	public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
	{
		public void Configure(EntityTypeBuilder<Invoice> builder)
		{
			builder.HasOne(x => x.Customer)
				.WithMany()
				.HasForeignKey(x => x.CustomerId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.CreatedUser)
				.WithMany()
				.HasForeignKey(x => x.CreatedUserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_Invoice_HtsId");
		}
	}
}
