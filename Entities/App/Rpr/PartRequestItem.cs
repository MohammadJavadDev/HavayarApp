using Common.Attributes;
using Entities.App.Hcm;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Prp
{
    [Display(Name = "درخواست قطعه")]
    [Table("PartRequestItem", Schema = "Rpr")]
    public class PartRequestItem : BaseEntity
    {

        public int RequestHeaderId { get; set; }


        [DisplayName("درخواست تعمیر")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public RepairRequest? RepairRequest { get; set; }
        public long? RepairRequestId { get; set; }


        [DisplayName("شناسه ثبت درخواست در راهکاران")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? RahkaranRequesterId { get; set; }


        [DisplayName("پرستل")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Personel? Consumer { get; set; }
        public long? ConsumerId { get; set; }


        [DisplayName("نوع مصرف کننده در راهکاران")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? RahkaranConsumerTypeId { get; set; }


        [DisplayName("شناسه مصرف کننده در راهکاران")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? RahkaranConsumerId { get; set; }

        //public short YearId { get; set; }

        [DisplayName("شناسه نوع درخواست")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int RequestTypeId { get; set; }


        [DisplayName("نوع درخواست")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? RequestType { get; set; }


        [DisplayName("شماره درخواست")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? RequestNo { get; set; }


        [DisplayName("تاریخ میلادی درخواست")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? RequestMiladiDate { get; set; }


        [DisplayName("تاریخ شمسی درخواست")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? RequestShamsiDate { get; set; }


        [DisplayName("شناسه مصرف کننده")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? ConsumerDlId { get; set; }



        [DisplayName("سر صفحه")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [MaxLength(1024)]
        public string? HeaderComment { get; set; }


        [DisplayName("شماره ترتیب انبار")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [StringLength(1024)]
        public string? SequenceNumber { get; set; }



        [DisplayName("کالا")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Part? Part { get; set; }
        public long? PartId { get; set; }


        [DisplayName("کیفیت")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? Qty { get; set; }


        [DisplayName("ضریب")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? Ratio { get; set; }


        [DisplayName("توضیحات آیتم")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? ItemComment { get; set; }

        ///<summary
        ///
        ///</summary>
        //public int? DlFiveId { get; set; }

    }
}