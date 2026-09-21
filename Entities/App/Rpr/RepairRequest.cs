using Common.Attributes;
using Entities.App.Crm;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Inv;
using Entities.App.Rpr.Enums;
using Entities.App.Sale;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
	[Display(Name = "درخواست تعمیر")]
	[Table("RepairRequest", Schema = "Rpr")]
	public class RepairRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("شماره")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(20)]
		public string? IdNumber { get; set; }

		[DisplayName("درخواست پشتیبانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual ServiceRequest? ServiceRequest { get; set; }
		public long? ServiceRequestId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("سایت مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual CustomerAddress? CustomerAddress { get; set; }
		public long? CustomerAddressId { get; set; }

		[DisplayName("آدرس دستی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? CustomerAddressManual { get; set; }

		[DisplayName("محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Part? Product { get; set; }
		public long? ProductId { get; set; }


		[DisplayName("محصول مادر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrderDetailSerial? MotherProduct { get; set; }
		public long? MotherProductId { get; set; }

		[DisplayName("سریال")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Serial { get; set; }

		[DisplayName("منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Zone? Zone { get; set; }
		public long? ZoneId { get; set; }

		[DisplayName("مسئول منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ZoneSupervisorId))]
		public virtual User? ZoneSupervisor { get; set; }
		public long? ZoneSupervisorId { get; set; }

		[DisplayName("آدرس نمایندگی مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(CustomerAgencyId))]
		public virtual Customer? CustomerAgency { get; set; }
		public long? CustomerAgencyId { get; set; }

		[DisplayName("تلفن مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CustomerPhone { get; set; }

		[DisplayName("تلفن از سوابق")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2048)]
		public string? CustomerPhoneFromHistory { get; set; }

		[DisplayName("پذیرش نمایندگی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(400)]
		public string? RepresentationReception { get; set; }

		[DisplayName("مقصد پس از تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public DestinationAfterRepairEnum? DestinationAfterRepair { get; set; }

		[DisplayName("آدرس مقصد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? DestinationAddress { get; set; }

		[DisplayName("شرح کار")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public RepairJobDescriptionEnum? JobDescription { get; set; }

		[DisplayName("شماره پیش‌فاکتور")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? PreFactorNumber { get; set; }

		[DisplayName("شماره حواله فروش")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? LoanFormNumber { get; set; }

		[DisplayName("شماره فرم تحویل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(80)]
		public string? DeliveryFormNumber { get; set; }

		[DisplayName("شماره برگشت امانی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? SafekeepingReturnFormNumber { get; set; }

		[DisplayName("تاریخ برگشت امانی میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? SafekeepingReturnMiladiDate { get; set; }

		[DisplayName("تاریخ برگشت امانی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? SafekeepingReturnShamsiDate { get; set; }

		[DisplayName("تصویربرداری تولید محتوا")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? HasContentProductionImaging { get; set; }

		[DisplayName("تجهیز هوایار")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsHavayarEquipment { get; set; }

		[DisplayName("تفصیل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual DL? Dl { get; set; }
		public long? DlId { get; set; }

		[DisplayName("بازرس")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Personel? InspectionPersonel { get; set; }
		public long? InspectionPersonelId { get; set; }

		[DisplayName("شناسه کارشناسان")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? ExpertIds { get; set; }

		[DisplayName("کارشناسان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? ExpertsInText { get; set; }

		[DisplayName("ایستگاه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual RepairStation? Station { get; set; }
		public long? StationId { get; set; }

		[DisplayName("گارانتی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsGuarantee { get; set; }

		[DisplayName("غیرقابل تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Unrepairable { get; set; }

		[DisplayName("نیاز به ارسال کارفرما")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool NeedForSendToEmployer { get; set; }

		[DisplayName("تاریخ ورود میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date, required: true)]
		public DateTime? EntryMiladiDate { get; set; }

		[DisplayName("تاریخ ورود شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? EntryShamsiDate { get; set; }

		[DisplayName("تاریخ نیاز مشتری میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? CustomerNeedMiladiDate { get; set; }

		[DisplayName("تاریخ نیاز مشتری شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? CustomerNeedShamsiDate { get; set; }

		[DisplayName("تاریخ خروج میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ExitMiladiDate { get; set; }

		[DisplayName("تاریخ خروج شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ExitShamsiDate { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public RepairRequestStatusEnum? Status { get; set; }

		[DisplayName("تاییدکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ConfirmerId))]
		public virtual User? Confirmer { get; set; }
		public long? ConfirmerId { get; set; }

		[DisplayName("تاریخ تایید میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? ConfirmMiladiDate { get; set; }

		[DisplayName("تاریخ تایید شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ConfirmShamsiDate { get; set; }

		[DisplayName("ساعت تایید")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? ConfirmTime { get; set; }

		[DisplayName("تاریخ تایید ۲ میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? Confirm2MiladiDate { get; set; }

		[DisplayName("تاریخ تایید ۲ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? Confirm2ShamsiDate { get; set; }

		[DisplayName("تایید ۲")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsConfirmed2 { get; set; }

		[DisplayName("مسئول تعمیر")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(ResponsibleId))]
		public virtual User? Responsible { get; set; }
		public long? ResponsibleId { get; set; }

		[DisplayName("تاریخ شروع میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? StartMiladiDate { get; set; }

		[DisplayName("تاریخ شروع شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? StartShamsiDate { get; set; }

		[DisplayName("تاریخ پایان میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? EndMiladiDate { get; set; }

		[DisplayName("تاریخ پایان شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? EndShamsiDate { get; set; }

		[DisplayName("تاریخ ورود تعمیر میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? RepairEntranceMiladiDate { get; set; }

		[DisplayName("تاریخ ورود تعمیر شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? RepairEntranceShamsiDate { get; set; }

		[DisplayName("تاریخ تحویل انبار میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? WarehouseDeliveryMiladiDate { get; set; }

		[DisplayName("تاریخ تحویل انبار شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? WarehouseDeliveryShamsiDate { get; set; }

		[DisplayName("تاریخ توافقی میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? AgreedMiladiDate { get; set; }

		[DisplayName("تاریخ توافقی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? AgreedShamsiDate { get; set; }

		[DisplayName("تاریخ توافقی ۲ میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? Agreed2MiladiDate { get; set; }

		[DisplayName("تاریخ توافقی ۲ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? Agreed2ShamsiDate { get; set; }

		[DisplayName("تاریخ توافقی ۳ میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? Agreed3MiladiDate { get; set; }

		[DisplayName("تاریخ توافقی ۳ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? Agreed3ShamsiDate { get; set; }

		[DisplayName("تحویل‌گیرنده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? DeliveringRecipient { get; set; }

		[DisplayName("تاریخ درخواست خرید میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? BuyRequestMiladiDate { get; set; }

		[DisplayName("تاریخ درخواست خرید شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? BuyRequestShamsiDate { get; set; }

		[DisplayName("تاریخ تأمین میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? SupplyMiladiDate { get; set; }

		[DisplayName("تاریخ تأمین شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? SupplyShamsiDate { get; set; }

		[DisplayName("تاریخ درخواست تست میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? QcRequestMiladiDate { get; set; }

		[DisplayName("تاریخ درخواست تست شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? QcRequestShamsiDate { get; set; }

		[DisplayName("شناسه کسر قطعات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? PartFractionIds { get; set; }

		[DisplayName("کسر قطعات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? PartFractionsInText { get; set; }

		[DisplayName("تاریخ پیش‌بررسی میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? PreCheckMiladiDate { get; set; }

		[DisplayName("تاریخ پیش‌بررسی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? PreCheckShamsiDate { get; set; }

		[DisplayName("تاریخ پیش‌بررسی ۲ میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? PreCheck2MiladiDate { get; set; }

		[DisplayName("تاریخ پیش‌بررسی ۲ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? PreCheck2ShamsiDate { get; set; }

		[DisplayName("پیش‌بررسی ۲")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsPreChecked2 { get; set; }

		[DisplayName("تاریخ پیشنهاد مالی میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? FinancialProposalMiladiDate { get; set; }

		[DisplayName("تاریخ پیشنهاد مالی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? FinancialProposalShamsiDate { get; set; }

		[DisplayName("تاریخ پیشنهاد مالی ۲ میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? FinancialProposal2MiladiDate { get; set; }

		[DisplayName("تاریخ پیشنهاد مالی ۲ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? FinancialProposal2ShamsiDate { get; set; }

		[DisplayName("تایید پیشنهاد مالی ۲")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsApprovedFinancialProposal2 { get; set; }

		[DisplayName("ارسال پیشنهاد مالی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsSentFinancialProposal { get; set; }

		[DisplayName("تاریخ ارسال پیشنهاد مالی میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? FinancialProposalSentMiladiDate { get; set; }

		[DisplayName("تاریخ ارسال پیشنهاد مالی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? FinancialProposalSentShamsiDate { get; set; }

		[DisplayName("دارای تاخیر")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasDelay { get; set; }

		[DisplayName("علت خرابی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? FailureReason { get; set; }

		[DisplayName("شرح تعمیر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? RepairDescription { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		[DisplayName("مرکز هزینه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual CostCenter? CostCenter { get; set; }
		public long? CostCenterId { get; set; }

		[DisplayName("دارای پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasContractor { get; set; }

		[NotMapped]
		public bool IsConfirmOperation { get; set; }
	}

	public class RepairRequestConfiguration : IEntityTypeConfiguration<RepairRequest>
	{
		public void Configure(EntityTypeBuilder<RepairRequest> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequest_HtsId");
			builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
			builder.HasOne(x => x.CustomerAgency).WithMany().HasForeignKey(x => x.CustomerAgencyId).OnDelete(DeleteBehavior.NoAction);
			builder.HasOne(x => x.ZoneSupervisor).WithMany().HasForeignKey(x => x.ZoneSupervisorId).OnDelete(DeleteBehavior.NoAction);
			builder.HasOne(x => x.Confirmer).WithMany().HasForeignKey(x => x.ConfirmerId).OnDelete(DeleteBehavior.NoAction);
			builder.HasOne(x => x.Responsible).WithMany().HasForeignKey(x => x.ResponsibleId).OnDelete(DeleteBehavior.NoAction);
		}
	}

	[Display(Name = "هزینه تقریبی تعمیر")]
	[Table("EstimatedCost", Schema = "Rpr")]
	public class EstimatedCost : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual RepairRequest? RepairRequest { get; set; }
		public long? RepairRequestId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("عنوان مشتری")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(255)]
		public string CustomerTitle { get; set; } = "";

		[DisplayName("منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Zone? Zone { get; set; }
		public long? ZoneId { get; set; }

		[DisplayName("محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Part? Product { get; set; }
		public long? ProductId { get; set; }

		[DisplayName("نفرساعت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? ManHour { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class EstimatedCostConfiguration : IEntityTypeConfiguration<EstimatedCost>
	{
		public void Configure(EntityTypeBuilder<EstimatedCost> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_EstimatedCost_HtsId");
		}
	}

	[Display(Name = "سفارش پیمانکار تعمیر")]
	[Table("ContractorOrder", Schema = "Rpr")]
	public class ContractorOrder : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual RepairRequest? RepairRequest { get; set; }
		public long? RepairRequestId { get; set; }

		[DisplayName("عنوان پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(256)]
		public string? ContractorTitle { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }
	}

	public class ContractorOrderConfiguration : IEntityTypeConfiguration<ContractorOrder>
	{
		public void Configure(EntityTypeBuilder<ContractorOrder> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_ContractorOrder_HtsId");
		}
	}
}
