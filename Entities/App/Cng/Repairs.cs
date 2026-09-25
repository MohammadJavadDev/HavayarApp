using Common.Attributes;
using Entities.App.Cng.Enums;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Inv;
using Entities.App.Rpr.Enums;
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
	/// <summary>تعمیرات CNG — معادل HTS <c>Cng_Repairs</c>.</summary>
	[Display(Name = "تعمیرات")]
	[Table("Repairs", Schema = "Cng")]
	public class Repairs : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("شماره شناسنامه")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(20)]
		public string? IdNumber { get; set; }

		[DisplayName("تجهیز")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(ProductId))]
		public virtual Part? Product { get; set; }
		public long ProductId { get; set; }

		[DisplayName("نمایندگی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(AgencyId))]
		public virtual Customer? Agency { get; set; }
		public long AgencyId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(CustomerId))]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("تفصیل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(DlId))]
		public virtual DL? Dl { get; set; }
		public long? DlId { get; set; }

		[DisplayName("سریال")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Serial { get; set; }

		[DisplayName("تعداد")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int Count { get; set; }

		[DisplayName("گارانتی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsGuarantee { get; set; }

		[DisplayName("نیاز به ارسال کارفرما")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool NeedForSendToEmployer { get; set; }

		[DisplayName("تاریخ ورود میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date, required: true)]
		public DateTime EntryMiladiDate { get; set; }

		[DisplayName("تاریخ ورود")]
		[MaxLength(10)]
		[DisplayInfo("EntryMiladiDate", true, type: SystemType.DateShamsi, required: true)]
		public string? EntryShamsiDate { get; set; }

		[DisplayName("تاریخ تحویل انبار میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? WarehouseDeliveryMiladiDate { get; set; }

		[DisplayName("تاریخ تحویل انبار")]
		[MaxLength(10)]
		[DisplayInfo("WarehouseDeliveryMiladiDate", true, type: SystemType.DateShamsi)]
		public string? WarehouseDeliveryShamsiDate { get; set; }

		[DisplayName("تاریخ خروج انبار میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? WarehouseExitMiladiDate { get; set; }

		[DisplayName("تاریخ خروج انبار")]
		[MaxLength(10)]
		[DisplayInfo("WarehouseExitMiladiDate", true, type: SystemType.DateShamsi)]
		public string? WarehouseExitShamsiDate { get; set; }

		[DisplayName("مقصد پس از تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public DestinationAfterRepairEnum? DestinationAfterRepair { get; set; }

		[DisplayName("آدرس مقصد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? DestinationAddress { get; set; }

		[DisplayName("تلفن مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CustomerPhone { get; set; }

		[DisplayName("تلفن از سوابق")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2048)]
		public string? CustomerPhoneFromHistory { get; set; }

		[DisplayName("شماره پیش‌فاکتور")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? PreFactorNumber { get; set; }

		[DisplayName("شماره فرم تحویل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(80)]
		public string? DeliveryFormNumber { get; set; }

		[DisplayName("شماره امانت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? LoanFormNumber { get; set; }

		[DisplayName("تاییدکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ConfirmerId))]
		public virtual User? Confirmer { get; set; }
		public long? ConfirmerId { get; set; }

		[DisplayName("تاریخ تایید میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? ConfirmMiladiDate { get; set; }

		[DisplayName("تاریخ تایید")]
		[MaxLength(10)]
		[DisplayInfo("ConfirmMiladiDate", true, type: SystemType.DateShamsi)]
		public string? ConfirmShamsiDate { get; set; }

		[DisplayName("ساعت تایید")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? ConfirmTime { get; set; }

		[DisplayName("تایید شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsConfirmed { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public RepairsStatusEnum? Status { get; set; }

		[DisplayName("پرسنل مسئول")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(ResponsiblePersonnelId))]
		public virtual Personel? ResponsiblePersonnel { get; set; }
		public long? ResponsiblePersonnelId { get; set; }

		[DisplayName("تاریخ تخمینی تعمیر میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? EstimatedRepairMiladiDate { get; set; }

		[DisplayName("تاریخ تخمینی تعمیر")]
		[MaxLength(10)]
		[DisplayInfo("EstimatedRepairMiladiDate", false, type: SystemType.DateShamsi)]
		public string? EstimatedRepairShamsiDate { get; set; }

		[DisplayName("علت خرابی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? FailureReason { get; set; }

		[DisplayName("توضیح قطعه متعلق")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? PartDescriptionBelonging { get; set; }

		[DisplayName("برون‌سپاری")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasVendor { get; set; }

		[DisplayName("نام فروشنده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? VendorName { get; set; }

		[DisplayName("هزینه برون‌سپاری")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? VendoringCost { get; set; }

		[DisplayName("نحوه ارسال")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public RepairsSendMethodEnum? SendMethod { get; set; }

		[DisplayName("مدت تایید")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? ApprovalDuration { get; set; }

		[DisplayName("مدت تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? RepairsDuration { get; set; }

		[DisplayName("اعلام ورود قطعه")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(PartEntryNotifyId))]
		public virtual PartEntryNotify? PartEntryNotify { get; set; }
		public long? PartEntryNotifyId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class RepairsConfiguration : IEntityTypeConfiguration<Repairs>
	{
		public void Configure(EntityTypeBuilder<Repairs> builder)
		{
			builder.Property(x => x.IsConfirmed)
				.HasComputedColumnSql("CONVERT([bit],isnull([ConfirmerId],(0)))", stored: false);

			builder.Property(x => x.ApprovalDuration)
				.HasComputedColumnSql("(datediff(day,[ConfirmMiladiDate],getdate()))", stored: false);

			builder.Property(x => x.RepairsDuration)
				.HasComputedColumnSql("(datediff(day,[EntryMiladiDate],getdate()))", stored: false);

			builder.HasOne(x => x.Product)
				.WithMany()
				.HasForeignKey(x => x.ProductId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Agency)
				.WithMany()
				.HasForeignKey(x => x.AgencyId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Customer)
				.WithMany()
				.HasForeignKey(x => x.CustomerId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Dl)
				.WithMany()
				.HasForeignKey(x => x.DlId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Confirmer)
				.WithMany()
				.HasForeignKey(x => x.ConfirmerId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.ResponsiblePersonnel)
				.WithMany()
				.HasForeignKey(x => x.ResponsiblePersonnelId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.PartEntryNotify)
				.WithMany()
				.HasForeignKey(x => x.PartEntryNotifyId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_Repairs_HtsId");
		}
	}
}
