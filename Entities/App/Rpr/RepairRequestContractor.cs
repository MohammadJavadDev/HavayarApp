using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
    [Display(Name = "درخواست تعمیر پیمانکار")]
    [Table("RepairRequestContractor", Schema = "Rpr")]
    public class RepairRequestContractor : BaseEntity
    {

        [DisplayName("تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public RepairRequest? RepairRequest { get; set; }
        public long? RepairRequestId { get; set; }


        [DisplayName(" پیمانکار")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Contractor? Contractor { get; set; }
        public long? ContractorId { get; set; }


        [DisplayName(" سفارش پیمانکار")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public ContractorOrder? ContractorOrder { get; set; }
        public long? ContractorOrderId { get; set; }


        [DisplayName("هزینه")]
        [DisplayInfo(null, true, type: SystemType.Long)]
        public decimal? Cost { get; set; }

        [DisplayName("کالا")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Part? Part { get; set; }
        public long? PartId { get; set; }

        [DisplayName("عنوان کالا")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? PartTitle { get; set; }


        [DisplayName("تاریخ میلادی ارسال کالا")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? SendPartMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی ارسال کالا")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? SendPartShamsiDate { get; set; } = null;


        [DisplayName("تاریخ میلادی تایید به پیمانکار")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? ContractorAcceptMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی تایید به پیمانکار")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? ContractorAcceptShamsiDate { get; set; } = null;


        [DisplayName("تاریخ میلادی توافق با پیمانکار")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? AgreegmentMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی توافق با پیمانکار")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? AgreegmentShamsiDate { get; set; } = null;


        [DisplayName("تاریخ میلادی  برگشت از پیمانکار")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? ContractorReturnMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی برگشت از پیمانکار")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? ContractorReturnShamsiDate { get; set; } = null;


        [DisplayName("تاریخ میلادی اعلام هزینه تعمیر")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? CostAnnouncementMiladiDate { get; set; } = null;


        [DisplayName("تاریخ شمسی اعلام هزینه تعمیر")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? CostAnnouncementShamsiDate { get; set; } = null;


        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [MaxLength(500)]
        public string? Comment { get; set; }

    }
}