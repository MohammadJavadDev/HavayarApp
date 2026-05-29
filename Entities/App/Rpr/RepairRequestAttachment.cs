using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Prp
{
    [Display(Name = "پیوست درخواست تعمیرات")]
    [Table("RepairRequestAttachment", Schema = "Rpr")]
    public class RepairRequestAttachment : BaseEntity
    {

        [DisplayName("تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public RepairRequest? RepairRequest_FK { get; set; }
        public long? RepairRequestId { get; set; }

        [DisplayName("پیوست")]
        [DisplayInfo(null, true, type: SystemType.File, required: true, fileTypes: ".pdf,.zip,.png,.jpg")]
        public FileEntity AttachmentRepair { get; set; }
        public long AttachmentRepairId { get; set; }

    }
}