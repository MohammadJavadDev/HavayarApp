using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Qc
{
	[Display(Name = "پارامتر اندازه‌گیری بازرسی محصول")]
	[Table("ProductInspectionMeasuredParameter", Schema = "Qc")]
	public class ProductInspectionMeasuredParameter : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long ProductInspectionId { get; set; }

		[ForeignKey(nameof(ProductInspectionId))]
		public virtual ProductInspection? ProductInspection { get; set; }

		[DisplayName("پارامتر اندازه گیری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(MeasurementParameterItemId))]
		public virtual MeasurementParameterItem? MeasurementParameterItem { get; set; }

		public long MeasurementParameterItemId { get; set; }

		[DisplayName("اقلام پارامتر اندازه گیری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(MeasurementParameterSubItemId))]
		public virtual MeasurementParameterSubItem? MeasurementParameterSubItem { get; set; }

		public long? MeasurementParameterSubItemId { get; set; }

		[DisplayName("تاریخ شروع")]
		[MaxLength(10)]
		[DisplayInfo("MeasurementStartMiladiDate", false, SystemType.DateShamsi)]
		public string? MeasurementStartShamsiDate { get; set; }

		[DisplayName("تاریخ شروع")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? MeasurementStartMiladiDate { get; set; }

		[DisplayName("ساعت شروع")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(5)]
		public string? MeasurementStartTime { get; set; }

		[DisplayName("تاریخ پایان")]
		[MaxLength(10)]
		[DisplayInfo("MeasurementEndMiladiDate", false, SystemType.DateShamsi)]
		public string? MeasurementEndShamsiDate { get; set; }

		[DisplayName("تاریخ پایان")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? MeasurementEndMiladiDate { get; set; }

		[DisplayName("ساعت خاتمه")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(5)]
		public string? MeasurementEndTime { get; set; }

		[DisplayName("نتیجه اندازه گیری")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool MeasurementResult { get; set; }

		[DisplayName("مقدار اندازه گیری")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		[Column(TypeName = "decimal(18,2)")]
		public decimal? MeasurementValue { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }
	}

	public class ProductInspectionMeasuredParameterConfiguration : IEntityTypeConfiguration<ProductInspectionMeasuredParameter>
	{
		public void Configure(EntityTypeBuilder<ProductInspectionMeasuredParameter> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Qc_ProductInspectionMeasuredParameter_HtsId");

			builder.HasOne(x => x.MeasurementParameterItem)
				.WithMany()
				.HasForeignKey(x => x.MeasurementParameterItemId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.MeasurementParameterSubItem)
				.WithMany()
				.HasForeignKey(x => x.MeasurementParameterSubItemId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
