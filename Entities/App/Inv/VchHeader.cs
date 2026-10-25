using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	/// <summary>
	/// هدر سند انبار (کپی همگام‌شده از راهکاران) — معادل HTS <c>dbo.Inv_VchHeader</c>.
	/// <c>HamkaranId</c> همان <c>VchHeaderId</c> (InventoryVoucherID) است.
	/// </summary>
	[Display(Name = "هدر سند انبار")]
	[Table("VchHeader", Schema = "Inv")]
	public class VchHeader : BaseEntity
	{
		[DisplayName("شناسه سند راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HamkaranId { get; set; }

		[DisplayName("سال مالی")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short Year { get; set; }

		[DisplayName("نوع سند")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public VchType? VchType { get; set; }

		public long VchTypeId { get; set; }

		[DisplayName("شماره سند")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int VchNum { get; set; }

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

		[DisplayName("انبار")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Stock? Stock { get; set; }

		public long StockId { get; set; }

		[DisplayName("ایجاد کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? Creator { get; set; }

		public long? CreatorId { get; set; }

		[DisplayName("تایید کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? Confirmer { get; set; }

		public long? ConfirmerId { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Status { get; set; }

		[DisplayName("شرح سند")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? VchDescription { get; set; }

		[DisplayName("نام انبار مقابل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? OpositStockName { get; set; }

		[DisplayName("کد انبار مقابل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? OpositStockCode { get; set; }

		[DisplayName("نوع سند مبنا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? BaseVchType { get; set; }

		[DisplayName("شرح سند مبنا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? BaseVchDescription { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		[DisplayName("اعلان شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsNotified { get; set; }

		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int HtsId { get; set; }
	}

	public class VchHeaderConfiguration : IEntityTypeConfiguration<VchHeader>
	{
		public void Configure(EntityTypeBuilder<VchHeader> builder)
		{
			builder.HasIndex(x => x.HamkaranId)
				.IsUnique()
				.HasDatabaseName("IX_Inv_VchHeader_HamkaranId");
			builder.HasIndex(x => new { x.IsNotified, x.StockId })
				.HasDatabaseName("IX_Inv_VchHeader_IsNotified_Stock");

			builder.HasOne(x => x.VchType).WithMany().HasForeignKey(x => x.VchTypeId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Dl).WithMany().HasForeignKey(x => x.DlId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Confirmer).WithMany().HasForeignKey(x => x.ConfirmerId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
