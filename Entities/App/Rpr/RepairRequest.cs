using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Inv;
using Entities.App.Prp.Enums;
using Entities.App.Sale;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Prp
{
    [Display(Name = "درخواست تعمیرات")]
    [Table("RepairRequest", Schema = "Rpr")]
    public class RepairRequest : BaseEntity
    {

        [DisplayName("مشتری")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Customer? Customer { get; set; }
        public long? CustomerId { get; set; }

        /// <summary>
        /// TODO MJ 
        /// </summary>
        //public int? CustomerAddressId { get; set; }

        [DisplayName("سایت مشتری(دستی)")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [MaxLength(500)]
        public string? CustomerAddressManual { get; set; }

        [DisplayName("تجهیزات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Part? Product { get; set; }
        public long? ProductId { get; set; }

        [DisplayName("شماره سریال")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Serial { get; set; }


        [DisplayName("منطقه")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Region? Zone { get; set; }
        public long? ZoneId { get; set; }


        [DisplayName("سرپرست منطقه")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual User ZoneSupervisor { get; set; }
        public long? ZoneSupervisorId { get; set; }


        [DisplayName("تفصیل")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual DL DL { get; set; }
        public long? DLId { get; set; }


        [DisplayName("گارانتی دارد")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool IsGuarantee { get; set; }

        //نیاز نداریم
        //[DisplayName("گارانتی دارد")]
        //[DisplayInfo(null, true, type: SystemType.Boolean)]
        //public bool Unrepairable { get; set; }

        //[DisplayName("ارسال به کارفرما")]
        //[DisplayInfo(null, true, type: SystemType.Boolean)]
        //public bool NeedForSendToEmployer { get; set; }


        [DisplayName("تاریخ ورود میلادی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? EntryMiladiDate { get; set; } = null;


        [DisplayName("تاریخ ورود شمسی")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? EntryShamsiDate { get; set; } = null;


        [DisplayName("تاریخ نیاز مشتری میلادی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? CustomerNeedMiladiDate { get; set; } = null;


        [DisplayName("تاریخ نیاز مشتری به شمسی")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? CustomerNeedShamsiDate { get; set; } = null;


        [DisplayName("تاریخ میلادی تحویل به انبار")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? DeliveryWarehouseMiladiDate { get; set; } = null;

        [DisplayName("تاریخ شمسی تحویل به انبار ")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? DeliveryWarehouseDateShamsi { get; set; } = null;


        [DisplayName("کسری قطعات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual RepairRequestPartFraction RepairRequestPartFractions { get; set; }
        public long? RepairRequestPartFractionsId { get; set; }


        [DisplayName("محصول مادر")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual ProductionOrderItem ProductionOrderItem { get; set; }
        public long? ProductionOrderItemId { get; set; }


        [DisplayName("تاریخ میلادی خروج")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? ExitMiladiDate { get; set; } = null;

        [DisplayName("تاریخ شمسی خروج ")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? ExitShamsiDate { get; set; } = null;

        [DisplayName("دریافت از نمایندگی")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? RepresentationReception { get; set; }


        [DisplayName("مقصد پس از تعمیر")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public RepairRequestDestinationAfterRepairEnum DestinationAfterRepair { get; set; }


        [DisplayName("انوع تجهیزات تعمیری")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public TypeRepairEquipmentEnum TypeRepairEquipment { get; set; }


        [DisplayName("ساعت کارکرد دستگاه ")]
        [DisplayInfo(null, true, type: SystemType.Long)]
        public long? OperatingHoursOfTheDevice { get; set; }

        [DisplayName("آدرس مقصد")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? DestinationAddress { get; set; }

        [DisplayName("شماره تماس مشتری")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? CustomerPhone { get; set; }

        [DisplayName("شماره تماس اولیه")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? CustomerPhoneFromHistory { get; set; }


        [DisplayName("تحویل گیرنده")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? DeliveringRecipient { get; set; }


        [DisplayName("شماره پیش فاکتور")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? PreFactorNumber { get; set; }


        [DisplayName("شماره فرم تحویل")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? DeliveryFormNumber { get; set; }

        [DisplayName("شماره فرم حواله")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? LoanFormNumber { get; set; }


        [DisplayName("تخفیف")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public long? Discount { get; set; }


        [DisplayName("تاریخ میلادی تایید")]
        [DisplayInfo(null, false, SystemType.DateTime)]
        public DateTime? ConfirmMiladiDateTime { get; set; } = null;


        [DisplayName("تاریخ شمسی تایید")]
        [DisplayInfo(null, false, SystemType.DateTimeShamsi)]
        public string? ConfirmShamsiDateTime { get; set; } = null;



        [DisplayName("تایید شده")]
        [DisplayInfo(null, false, SystemType.Boolean)]
        public bool? IsConfirmed { get; set; }


        [DisplayName("وضعیت")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public RepairRequestStatusEnum RepairRequestStatus { get; set; }


        [DisplayName("شخص مسئول")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual Personel ResponsiblePersonnel { get; set; }
        public long? ResponsiblePersonnelId { get; set; }




        //[DisplayName("مسئول بازرسی")]
        //[DisplayInfo(null, true, SystemType.Entity)]
        //public virtual Personel InspectionPersonel { get; set; }
        //public long? InspectionPersonelId { get; set; }



        [DisplayName("کارشناس")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual Personel Expert { get; set; }
        public long? ExpertId { get; set; }



        [DisplayName("علت خرابی")]
        [DisplayInfo(null, false, SystemType.String)]
        [MaxLength(1024)]
        public string? FailureReason { get; set; }


        [DisplayName("تاخیر دارد")]
        [DisplayInfo(null, false, SystemType.Boolean)]
        public bool HasDelay { get; set; }


        /// <summary>
        /// TODO As
        /// </summary>
        [DisplayName("مدت اخذ تاییدیه")]
        [DisplayInfo(null, false, SystemType.Int)]
        public int? ApprovalDuration { get; set; }


        /// <summary>
        /// TODO As
        /// </summary>
        [DisplayName("مدت حضور در تعمیرات")]
        [DisplayInfo(null, false, SystemType.Int)]
        public int? RepairsDuration { get; set; }



        [DisplayName("تاریخ میلادی پیش بررسی")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? PreCheckMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی پیش بررسی")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? PreCheckShamsiDate { get; set; } = null;



        [DisplayName("از قبل بررسی شده؟")]
        [DisplayInfo(null, false, SystemType.Boolean)]
        public bool? IsPreChecked { get; set; }


        [DisplayName("تاریخ میلادی پیشنهاد مالی ")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? FinancialProposalMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی پیشنهاد مالی ")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? FinancialProposalShamsiDate { get; set; } = null;



        [DisplayName("تایید پیشنهاد مالی ")]
        [DisplayInfo(null, false, SystemType.Boolean)]
        public bool? IsApprovedFinancialProposalDate { get; set; }

        [DisplayName("شرح کار")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public RepairRequestJobDescriptionEnum JobDescription { get; set; }


        [DisplayName("شماره فرم برگشت امانی")]
        [DisplayInfo(null, false, SystemType.String)]
        public string? SafekeepingReturnFormNumber { get; set; }


        [DisplayName("فروش پیمانکار")]
        [DisplayInfo(null, false, SystemType.Long)]
        public long? ContractorSalePrice { get; set; }


        [DisplayName("شناسه اعلان ورود کالا")]
        [DisplayInfo(null, false, SystemType.Int)]
        public int? PartEntryNotifyId { get; set; }


        [DisplayName("تاریخ میلادی شروع")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? StartMiladiDate { get; set; } = null;

        [DisplayName("تاریخ شمسی شروع")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? StartShamsiDate { get; set; } = null;


        [DisplayName("تاریخ میلادی پایان")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? EndMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی پایان")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? EndShamsiDate { get; set; } = null;


        [DisplayName("شناسه متخصصین")]
        [DisplayInfo(null, true, SystemType.String)]
        public string? ExpertIds { get; set; }

        [DisplayName("نام متخصصین")]
        [DisplayInfo(null, true, SystemType.String)]
        public string? ExpertsNames { get; set; }



        [DisplayName("دارای تصویربرداری تولید محتوا")]
        [DisplayInfo(null, false, SystemType.Boolean)]
        public bool HasContentProductionImaging { get; set; } = false;

        [DisplayName("پیمانکار دارد؟")]
        [DisplayInfo(null, false, SystemType.Boolean)]
        public bool HasContractor { get; set; }


        [DisplayName("تاریخ میلادی درخواست")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? BuyRequestMiladiDate { get; set; } = null;

        [DisplayName("تاریخ شمسی درخواست")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? BuyRequestShamsiDate { get; set; } = null;

        [DisplayName("تاریخ میلادی تامین")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? SupplyMiladiDate { get; set; } = null;

        [DisplayName("تاریخ شمسی درخواست تامین")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? SupplyShamsiDate { get; set; } = null;


        [DisplayName("تاریخ میلادی درخواست تست")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? QcRequestMiladiDate { get; set; } = null;

        [DisplayName("تاریخ شمسی درخواست تست")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? QcRequestShamsiDate { get; set; } = null;


        //[DisplayName("جایگاه تعمیرات")]
        //[DisplayInfo(null, true, type: SystemType.Entity)]
        //public Station? Station { get; set; }
        //public long? StationId { get; set; }


        [DisplayName("تاریخ میلادی ورود به تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? RepairEnteranceMiladiDate { get; set; } = null;

        [DisplayName("تاریخ شمسی ورود به تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? RepairEnteranceShamsiDate { get; set; } = null;


        [DisplayName("نیاز به تست")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool? NeedTest { get; set; }

        [DisplayName("نیاز به نقاشی")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool? NeedPainting { get; set; }


        [DisplayName("ارسال به صنایع")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool? IsSendUsedPart { get; set; }


        [DisplayName("تاریخ شمسی ارسال به صنایع")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? SendUsedPartShamsiDateTime { get; set; } = null;


        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [StringLength(4000)]
        public string? Comment { get; set; }


        [DisplayName("شرح اقدامات انجام شده")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? RepairDescription { get; set; }


        [DisplayName("مرکز هزینه")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual CostCenter CostCenter { get; set; }
        public long? CostCenterId { get; set; }


        [DisplayName("نسخه")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? Revision { get; set; }


        [DisplayName("نمایندگی مشتری")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual Customer? CustomerAgency { get; set; }
        public long? CustomerAgencyId { get; set; }


        [DisplayName("ارسال پیشنهاد مالی")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool? IsSentFinancialProposal { get; set; }


        [DisplayName("تاریخ میلادی پیشنهاد مالی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? FinancialProposalSentMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی پیشنهاد مالی")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? FinancialProposalSentShamsiDate { get; set; } = null;


        [DisplayName("تاریخ میلادی برگشت امانی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? SafekeepingReturnMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی برگشت امانی")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? SafekeepingReturnShamsiDate { get; set; } = null;



        [DisplayName("تجهیزات هوایاری است؟")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool IsHavayarEquipment { get; set; } = false;


        [DisplayName("2تاریخ میلادی پیش بررسی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? PreChec2MiladikDate { get; set; } = null;

        [DisplayName("2تاریخ شمسی پیش بررسی")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? PreCheck2ShamsiDate { get; set; } = null;


        [DisplayName("از قبل بررسی شده؟")]
        [DisplayInfo(null, false, SystemType.Boolean)]
        public bool? IsPreChecked2 { get; set; }



        [DisplayName("تاریخ میلادی پیشنهاد مالی 2")]
        [DisplayInfo(null, false, SystemType.DateTime)]
        public DateTime? FinancialProposal2MiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی پیشنهاد مالی 2")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? FinancialProposal2ShamsiDate { get; set; } = null;


        [DisplayName("2تایید تاریخ پیشنهاد مالی")]
        [DisplayInfo(null, false, SystemType.Boolean)]
        public bool? IsApprovedFinancialProposalDate2 { get; set; }


        [DisplayName("تاریخ میلادی تایید 2")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? Confirm2MiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی تایید 2")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? Confirm2ShamsiDate { get; set; } = null;

        [DisplayName("2تایید تاریخ پیشنهاد مالی")]
        [DisplayInfo(null, false, SystemType.Boolean)]
        public bool? IsConfirmed2 { get; set; }

        [DisplayName("تاریخ میلادی توافقی")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? AgreedMiladiDate { get; set; } = null;

        [DisplayName("تاریخ میلادی شمسی")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? AgreedShamsiDate { get; set; } = null;


        [DisplayName("تاریخ میلادی توافقی 2")]
        [DisplayInfo(null, false, SystemType.Date)]
        public DateTime? Agreed2MiladiDate { get; set; } = null;

        [DisplayName("تاریخ شمسی توافقی 2")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? AgreedShamsiDate2 { get; set; } = null;

        [DisplayName("تاریخ میلادی توافقی 3")]
        [DisplayInfo(null, false, SystemType.DateTime)]
        public DateTime? Agreed3MiladiDate { get; set; } = null;

        [DisplayName("تاریخ شمسی توافقی 3")]
        [DisplayInfo(null, false, SystemType.DateShamsi)]
        public string? Agreed3ShamsiDate { get; set; } = null;


        [DisplayName("کاربر تایید کننده")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public User? ConfirmerUser { get; set; }
        public long? ConfirmerUserId { get; set; }


        [DisplayName("سفارش پیمانکاران")]
        [DisplayInfo(null, true, type: SystemType.ListEntity)]
        public virtual ICollection<ContractorOrder> ContractorOrder { get; set; }


        [DisplayName("هزینه تخمینی")]
        [DisplayInfo(null, true, type: SystemType.ListEntity)]
        public virtual ICollection<EstimatedCost> EstimatedCost { get; set; }


        [DisplayName("پیوست تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.ListEntity)]
        public virtual ICollection<RepairRequestAttachment> RepairRequestAttachment { get; set; }


        [DisplayName("توضیحات درخواست تعمیر")]
        [DisplayInfo(null, true, type: SystemType.ListEntity)]
        public virtual ICollection<RepairRequestComment> RepairRequestComment { get; set; }

        [DisplayName("درخواست تعمیر قطعه")]
        [DisplayInfo(null, true, type: SystemType.ListEntity)]
        public virtual ICollection<RepairRequestPart> RepairRequestPart { get; set; }


        [DisplayName("درخواست تعمیر پیمانکار")]
        [DisplayInfo(null, true, type: SystemType.ListEntity)]
        public virtual ICollection<RepairRequestContractor> RepairRequestContractors { get; set; }


        [DisplayName("درخواست تعمیر قطعه استفاده شده")]
        [DisplayInfo(null, true, type: SystemType.ListEntity)]
        public virtual ICollection<RepairRequestUsedPart> Rpr_RepairRequestUsedPart { get; set; }

        [DisplayName("نفر ساعت")]
        [DisplayInfo(null, true, type: SystemType.ListEntity)]
        public virtual ICollection<RepairRequestManHours> RepairRequestManHours { get; set; }


    }

}