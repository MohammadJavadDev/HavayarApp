using Common.Attributes;
using Entities.App.Inv;
using Entities.App.Rpr.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
    [Display(Name = "قرارداد سفارش قطعه")]
    [Table("ContractorOrderPart", Schema = "Rpr")]
    public class ContractorOrderPart : BaseEntity
    {

        [DisplayName("مسئول بازرسی")]
        [DisplayInfo(null, true, SystemType.Entity)]
        public virtual ContractorOrder ContractorOrder { get; set; }
        public long? ContractorOrderId { get; set; }

        [DisplayName("کالا")]
        [DisplayInfo(null, true, SystemType.Entity)]
        public virtual Part Part { get; set; }
        public long? PartId { get; set; }

        [DisplayName("مبلغ")]
        [DisplayInfo(null, true, SystemType.Decimal)]
        public decimal? Mount { get; set; }

        [DisplayName("علت خرابی")]
        [DisplayInfo(null, true, SystemType.String)]
        public string? FailureReason { get; set; }

        [DisplayName("وضعیت")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public RepairRequestStatusEnum RepairRequestStatus { get; set; }

        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Comment { get; set; }
    }
}