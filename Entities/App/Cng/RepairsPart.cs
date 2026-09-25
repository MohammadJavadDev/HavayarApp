using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>قطعات مصرفی تعمیرات CNG — معادل HTS <c>Cng_RepairsPart</c>.</summary>
	[Display(Name = "قطعات مصرفی تعمیرات")]
	[Table("RepairsPart", Schema = "Cng")]
	public class RepairsPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(RepairsId))]
		public virtual Repairs? Repairs { get; set; }
		public long RepairsId { get; set; }

		[DisplayName("تفصیل")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(DlId))]
		public virtual DL? Dl { get; set; }
		public long DlId { get; set; }

		[DisplayName("تفصیل همکاران")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HamkaranDlFk { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PartId))]
		public virtual Part? Part { get; set; }
		public long PartId { get; set; }

		[DisplayName("واحد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PartUnitId))]
		public virtual PartUnit? PartUnit { get; set; }
		public long? PartUnitId { get; set; }

		[DisplayName("شناسه قلم سند انبار همکاران")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HamkaranInvVchItmFk { get; set; }

		[DisplayName("قطعه آسیب‌دیده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool DamagedPart { get; set; }

		[DisplayName("شماره فرم تحویل قطعه آسیب‌دیده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(32)]
		public string? DamagedPartDeliveryFormNumber { get; set; }

		[DisplayName("مرجع انبار")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? StockRef { get; set; }

		[DisplayName("نام انبار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? StockName { get; set; }

		[DisplayName("نام واحد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? UnitName { get; set; }

		[DisplayName("مقدار مصرف")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal UsedMount { get; set; }

		[DisplayName("مقدار برگشتی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal ReturnMount { get; set; }

		[DisplayName("قیمت خرید")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? BuyPrice { get; set; }

		[DisplayName("تاریخ آخرین قیمت خرید")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(80)]
		public string? LastBuyPriceDateShamsi { get; set; }

		[DisplayName("قیمت فروش")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? SalePrice { get; set; }

		[DisplayName("قیمت کل")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? TotalPrice { get; set; }

		[DisplayName("رکورد همکاران")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsHamkaranRecord { get; set; }
	}

	public class RepairsPartConfiguration : IEntityTypeConfiguration<RepairsPart>
	{
		public void Configure(EntityTypeBuilder<RepairsPart> builder)
		{
			builder.Property(x => x.TotalPrice)
				.HasComputedColumnSql("([SalePrice]*([UsedMount]-[ReturnMount]))", stored: false);

			builder.Property(x => x.IsHamkaranRecord)
				.HasComputedColumnSql("(CONVERT([bit],case isnull([HamkaranDlFk],(0)) when (0) then (0) else (1) end))", stored: false);

			builder.HasOne(x => x.Repairs)
				.WithMany()
				.HasForeignKey(x => x.RepairsId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Dl)
				.WithMany()
				.HasForeignKey(x => x.DlId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.PartUnit)
				.WithMany()
				.HasForeignKey(x => x.PartUnitId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_RepairsPart_HtsId");
		}
	}
}
