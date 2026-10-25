using Common.Attributes;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Sale;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Qc
{
	[Display(Name = "بازرسی محصول در هوایار")]
	[Table("ProductInspection", Schema = "Qc")]
	public class ProductInspection : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("محل بازرسی")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public short InspectionType { get; set; } = ProductInspectionConstants.InCompany;

		[DisplayName("شماره سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int ProductionOrderNumber { get; set; }

		[DisplayName("قلم سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(ProductionOrderItemId))]
		public virtual ProductionOrderItem? ProductionOrderItem { get; set; }

		public long ProductionOrderItemId { get; set; }

		[DisplayName("سریال دستی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? ManualSerial { get; set; }

		[NotMapped]
		public long? ProductionOrderId { get; set; }

		[DisplayName("دپارتمان فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(SalesDepartmentId))]
		public virtual OrgUnit? SalesDepartment { get; set; }

		public long? SalesDepartmentId { get; set; }

		[DisplayName("عنوان پروژه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? ProjectTitle { get; set; }

		[DisplayName("پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Contractor { get; set; }

		[DisplayName("تاریخ شروع تست")]
		[MaxLength(10)]
		[DisplayInfo("TestStartMiladiDate", true, SystemType.DateShamsi, required: true)]
		public string? TestStartShamsiDate { get; set; }

		[DisplayName("تاریخ شروع تست")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? TestStartMiladiDate { get; set; }

		[DisplayName("ساعت شروع")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(5)]
		public string? TestStartTime { get; set; }

		[DisplayName("تاریخ پایان تست")]
		[MaxLength(10)]
		[DisplayInfo("TestEndMiladiDate", true, SystemType.DateShamsi, required: true)]
		public string? TestEndShamsiDate { get; set; }

		[DisplayName("تاریخ پایان تست")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? TestEndMiladiDate { get; set; }

		[DisplayName("ساعت خاتمه")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(5)]
		public string? TestEndTime { get; set; }

		[DisplayName("بازرس")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(TestPersonelId))]
		public virtual Personel? TestPersonel { get; set; }

		public long TestPersonelId { get; set; }

		[DisplayName("نتیجه بازرسی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool InspectionResult { get; set; }

		[DisplayName("تکمیل شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsCompleted { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		[NotMapped]
		public bool ConfirmEmptyMeasurement { get; set; }

		public virtual ICollection<ProductInspectionFailure> Failures { get; set; } = new List<ProductInspectionFailure>();

		public virtual ICollection<ProductInspectionMeasuredParameter> MeasuredParameters { get; set; } = new List<ProductInspectionMeasuredParameter>();

		public virtual ICollection<ProductInspectionAttachment> Attachments { get; set; } = new List<ProductInspectionAttachment>();
	}

	public class ProductInspectionConfiguration : IEntityTypeConfiguration<ProductInspection>
	{
		public void Configure(EntityTypeBuilder<ProductInspection> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Qc_ProductInspection_HtsId");

			builder.HasIndex(x => x.ProductionOrderItemId)
				.HasDatabaseName("IX_Qc_ProductInspection_ProductionOrderItemId");

			builder.HasOne(x => x.ProductionOrderItem)
				.WithMany()
				.HasForeignKey(x => x.ProductionOrderItemId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.SalesDepartment)
				.WithMany()
				.HasForeignKey(x => x.SalesDepartmentId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.TestPersonel)
				.WithMany()
				.HasForeignKey(x => x.TestPersonelId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasMany(x => x.Failures)
				.WithOne(x => x.ProductInspection)
				.HasForeignKey(x => x.ProductInspectionId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(x => x.MeasuredParameters)
				.WithOne(x => x.ProductInspection)
				.HasForeignKey(x => x.ProductInspectionId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(x => x.Attachments)
				.WithOne(x => x.ProductInspection)
				.HasForeignKey(x => x.ProductInspectionId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
