using Common.Attributes;
using Entities.App.Hrm;
using Entities.App.Rpr.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
    [Display(Name = "سفارش پیمانکاران")]
    [Table("ContractorOrder", Schema = "Rpr")]
    public class ContractorOrder : BaseEntity
    {

        [DisplayName("درخواست تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public RepairRequest? RepairRequest { get; set; }
        public long? RepairRequestId { get; set; }


        [DisplayName(" واحد سازمانی")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public OrgUnit? FromUnit { get; set; }
        public long? FromUnitId { get; set; }


        [DisplayName("آخرین وضعیت")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public RepairRequestLastStatusEnum LastStatus { get; set; }


        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Comment { get; set; }


        [DisplayName("وضعیت")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public RepairRequestStatusEnum RepairRequestStatus { get; set; }

        [DisplayName("قرارداد سفارش قطعه")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual ICollection<ContractorOrderPart> ContractorOrderPart { get; set; }
    }
}