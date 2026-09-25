using Common.Attributes;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>قطعه مصرفی گزارش کار CNG — معادل HTS <c>Cng_WorkReportConsumedPart</c>.
	/// ستون HTS <c>SaleOrderDetailId</c> در واقع شناسه <c>Sale_Order.Order_ID</c> است → <see cref="SaleOrderId"/>.</summary>
	[Display(Name = "قطعات مصرفی گزارش کار")]
	[Table("WorkReportConsumedPart", Schema = "Cng")]
	public class WorkReportConsumedPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("گزارش کار")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(WorkReportId))]
		public virtual WorkReport? WorkReport { get; set; }
		public long WorkReportId { get; set; }

		/// <summary>شماره فرم حواله — FK به Sale.Order (نه OrderDetail).</summary>
		[DisplayName("شماره فرم حواله")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(SaleOrderId))]
		public virtual Order? SaleOrder { get; set; }
		public long? SaleOrderId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PartId))]
		public virtual Part? Part { get; set; }
		public long PartId { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		public decimal Mount { get; set; }

		[DisplayName("وضعیت داغی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? HotnessCondition { get; set; }

		[DisplayName("توضیح")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class WorkReportConsumedPartConfiguration : IEntityTypeConfiguration<WorkReportConsumedPart>
	{
		public void Configure(EntityTypeBuilder<WorkReportConsumedPart> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_WorkReportConsumedPart_HtsId");

			builder.HasOne(x => x.WorkReport)
				.WithMany()
				.HasForeignKey(x => x.WorkReportId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.SaleOrder)
				.WithMany()
				.HasForeignKey(x => x.SaleOrderId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Property(x => x.Mount).HasPrecision(18, 4);
		}
	}
}
