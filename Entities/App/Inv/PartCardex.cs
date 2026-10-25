using Common.Attributes;
using Entities.App.FIN;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	/// <summary>
	/// کاردکس کالا (کپی همگام‌شده از راهکاران) — معادل HTS <c>dbo.Inv_PartCardex</c>.
	/// <c>HamkaranId</c> همان <c>VchItemId</c> (InventoryVoucherItemID) و <c>HtsId</c> همان <c>Inv_PartCardex.Id</c> قدیم است
	/// (برای حفظ لینک <c>Sale.OrderPoint.HtsPartCardexId</c>).
	/// </summary>
	[Display(Name = "کاردکس کالا")]
	[Table("PartCardex", Schema = "Inv")]
	public class PartCardex : BaseEntity
	{
		[DisplayName("شناسه آیتم سند راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HamkaranId { get; set; }

		[DisplayName("شناسه هدر سند راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? VchHeaderHamkaranId { get; set; }

		[DisplayName("سال مالی")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short Year { get; set; }

		[DisplayName("نوع سند")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string VchType { get; set; } = string.Empty;

		[DisplayName("شماره سند")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int VchNum { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long PartId { get; set; }

		[DisplayName("انبار")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Stock? Stock { get; set; }

		public long StockId { get; set; }

		[DisplayName("تاریخ سند میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? VchDateMiladiDate { get; set; }

		[DisplayName("تاریخ سند")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? VchDateShamsiDate { get; set; }

		[DisplayName("تفضیل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public DL? Dl { get; set; }

		public long? DlId { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public double Qty { get; set; }

		[DisplayName("مقدار تاثیر در موجودی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public double VQtyRatio { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string Status { get; set; } = string.Empty;

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? ItemDescription { get; set; }

		[DisplayName("سند دائمی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsPermanentVoucher { get; set; }

		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }
	}

	public class PartCardexConfiguration : IEntityTypeConfiguration<PartCardex>
	{
		public void Configure(EntityTypeBuilder<PartCardex> builder)
		{
			builder.HasIndex(x => x.HamkaranId)
				.IsUnique()
				.HasDatabaseName("IX_Inv_PartCardex_HamkaranId");
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> 0")
				.HasDatabaseName("IX_Inv_PartCardex_HtsId");
			builder.HasIndex(x => new { x.PartId, x.StockId, x.Year })
				.HasDatabaseName("IX_Inv_PartCardex_Part_Stock_Year");
			builder.HasIndex(x => new { x.Year, x.IsPermanentVoucher, x.StockId })
				.HasDatabaseName("IX_Inv_PartCardex_Year_Perm_Stock");

			builder.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Dl).WithMany().HasForeignKey(x => x.DlId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
