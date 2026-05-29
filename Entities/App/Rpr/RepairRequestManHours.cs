using Common.Attributes;
using Entities.App.Hcm;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Prp
{
    [Display(Name = "نفر ساعت ")]
    [Table("RepairRequestManHours", Schema = "Rpr")]
    public class RepairRequestManHours : BaseEntity
    {

        [DisplayName("تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public RepairRequest? RepairRequest { get; set; }
        public long? RepairRequestId { get; set; }

        [DisplayName("پرسنل")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Personel? Personel { get; set; }
        public long? PersonelId { get; set; }

        [DisplayName("تاریخ")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? WorkDate { get; set; }

        [DisplayName("تاریخ شروع")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? StartTime { get; set; }

        [DisplayName("تاریخ پایان")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? EndTime { get; set; }

        [DisplayName("هزینه نفر ساعت")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public long? ManHourPrice { get; set; }

        [DisplayName("هزینه نفر ساعت فروش")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public long? ManHourSalePrice { get; set; }

        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [StringLength(4000)]
        public string? Comment { get; set; }
        
    }
}