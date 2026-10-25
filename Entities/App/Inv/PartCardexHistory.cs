using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	/// <summary>
	/// تاریخچه کاردکس ERP قدیمی (erp.newhavayar، ۲۰۰۴-۰۲-۲۰ تا ۲۰۲۳-۰۳-۲۰) — یک‌بار بارگذاری می‌شود و فقط‌خواندنی است.
	/// </summary>
	[Display(Name = "تاریخچه کاردکس کالا (ERP قدیمی)")]
	[Table("PartCardexHistory", Schema = "Inv")]
	public class PartCardexHistory : BaseEntity
	{
		[DisplayName("شناسه آیتم سند ERP قدیمی")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long OldErpItemId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long PartId { get; set; }

		[DisplayName("انبار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? StockName { get; set; }

		[DisplayName("شماره سند")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int VchNum { get; set; }

		[DisplayName("نوع سند")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? VchTypeTitle { get; set; }

		[DisplayName("تاریخ")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? VchDateShamsiDate { get; set; }

		[DisplayName("تاریخ میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? VchDateMiladiDate { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public double Qty { get; set; }

		[DisplayName("تفضیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? DlTitle { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Comment { get; set; }

		[DisplayName("مانده")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public double RemainingCount { get; set; }
	}

	public class PartCardexHistoryConfiguration : IEntityTypeConfiguration<PartCardexHistory>
	{
		public void Configure(EntityTypeBuilder<PartCardexHistory> builder)
		{
			builder.HasIndex(x => x.OldErpItemId)
				.IsUnique()
				.HasDatabaseName("IX_Inv_PartCardexHistory_OldErpItemId");
			builder.HasIndex(x => x.PartId)
				.HasDatabaseName("IX_Inv_PartCardexHistory_PartId");
			builder.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
