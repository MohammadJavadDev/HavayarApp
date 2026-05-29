using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Prp
{
    [Display(Name = "توضیحات درخواست تعمیر")]
    [Table("RepairRequestComment", Schema = "Rpr")]
    public class RepairRequestComment : BaseEntity
    {
        public RepairRequestComment()
        {
            RepairRequestId = 10;
        }

        [DisplayName("تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public RepairRequest? RepairRequest { get; set; }
        public long? RepairRequestId { get; set; } 

        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Comment { get; set; }

        [DisplayName("هزینه")]
        [DisplayInfo(null, true, type: SystemType.Long)]
        public long? Cost { get; set; }

    }
}