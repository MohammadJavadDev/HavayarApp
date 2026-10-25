using Common.Attributes;
using Entities.App.Sale;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Qc
{
	[Display(Name = "خرابی بازرسی محصول")]
	[Table("ProductInspectionFailure", Schema = "Qc")]
	public class ProductInspectionFailure : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long ProductInspectionId { get; set; }

		[ForeignKey(nameof(ProductInspectionId))]
		public virtual ProductInspection? ProductInspection { get; set; }

		[DisplayName("عنوان خرابی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(FailureTypeId))]
		public virtual FailureType? FailureType { get; set; }

		public long FailureTypeId { get; set; }

		public long? ProductionOrderItemCommentId { get; set; }

		[ForeignKey(nameof(ProductionOrderItemCommentId))]
		public virtual ProductionOrderItemComment? ProductionOrderItemComment { get; set; }

		[DisplayName("تاریخ شروع گزارش خرابی")]
		[MaxLength(10)]
		[DisplayInfo("FailureStartMiladiDate", true, SystemType.DateShamsi, required: true)]
		public string? FailureStartShamsiDate { get; set; }

		[DisplayName("تاریخ شروع گزارش خرابی")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? FailureStartMiladiDate { get; set; }

		[DisplayName("ساعت شروع")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? FailureStartTime { get; set; }

		[DisplayName("تاریخ پایان گزارش خرابی")]
		[MaxLength(10)]
		[DisplayInfo("FailureEndMiladiDate", true, SystemType.DateShamsi, required: true)]
		public string? FailureEndShamsiDate { get; set; }

		[DisplayName("تاریخ پایان گزارش خرابی")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? FailureEndMiladiDate { get; set; }

		[DisplayName("ساعت خاتمه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? FailureEndTime { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }
	}

	public class ProductInspectionFailureConfiguration : IEntityTypeConfiguration<ProductInspectionFailure>
	{
		public void Configure(EntityTypeBuilder<ProductInspectionFailure> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Qc_ProductInspectionFailure_HtsId");

			builder.HasIndex(x => x.ProductionOrderItemCommentId)
				.HasDatabaseName("IX_Qc_ProductInspectionFailure_CommentId");

			builder.HasOne(x => x.FailureType)
				.WithMany()
				.HasForeignKey(x => x.FailureTypeId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.ProductionOrderItemComment)
				.WithMany()
				.HasForeignKey(x => x.ProductionOrderItemCommentId)
				.OnDelete(DeleteBehavior.SetNull);
		}
	}
}
