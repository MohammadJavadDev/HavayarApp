using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Inv;
using Entities.App.Sale.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
    [Display(Name = "درخواست کالا_نمایندگان")]
    [Table("ServiceRequestPartAgency", Schema = "Sale")]
    public class ServiceRequestPartAgency : BaseEntity
    {

        [DisplayName("درخواست خدمات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual ServiceRequest ServiceRequest { get; set; }
        public long? ServiceRequestId { get; set; }


        [DisplayName("شماره سند ")]
        [DisplayInfo(null, true, type: SystemType.Long)]
        public long? VoucherNumber { get; set; }


        [DisplayName("تاریخ پروژه میلادی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? VoucherMiladiDate { get; set; } = null;


        [DisplayName("تاریخ پروژه شمسی")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? VoucherShamsiDate { get; set; } = null;


        [DisplayName("قطعه")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual Part Part { get; set; }
        public long? PartId { get; set; }


        [DisplayName("مقدار")]
        [DisplayInfo(null, true, type: SystemType.Decimal)]
        public decimal? Mount { get; set; }


        [DisplayName("محصول مرتبط")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? RelatedProducts { get; set; }


        [DisplayName("قطعه جایگزین")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual Part ReplacePart { get; set; }
        public long? ReplacePartId { get; set; }


        [DisplayName("مبلغ")]
        [DisplayInfo(null, true, type: SystemType.Long)]
        public long? UnitPrice { get; set; }


        [DisplayName("نوع خدمات")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public ServiceTypesEnum ServiceTypes { get; set; }


        [DisplayName("مرکز هزینه")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual CostCenter CostCenter { get; set; }
        public long? CostCenterId { get; set; }


        [DisplayName("سایر مراکز هزینه")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual Supplier Supplier { get; set; }
        public long? SupplierId { get; set; }


        [DisplayName("داغی دارد")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool HasReturn_DamagedPart { get; set; } = false;


        [DisplayName("مجوز برگشت دارد")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool ReturnLicence { get; set; } = false;


        [StringLength(2048)]
        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Comment { get; set; }











        //[DisplayName("مرکز فروش")]
        //[DisplayInfo(null, true, type: SystemType.Entity)]
        //public virtual Branch Branch { get; set; }
        //public long? BranchId { get; set; }


        //[DisplayName("جزئیات سفارش فروش")]
        //[DisplayInfo(null, true, type: SystemType.Entity)]
        //public virtual OrderDetail OrderDetail { get; set; }
        //public long? OrderDetailId { get; set; }


        //[DisplayName("کالا")]
        //[DisplayInfo(null, true, type: SystemType.Entity)]
        //public virtual Part ProjectVchPart { get; set; }
        //public long? ProjectVchPartId { get; set; }


        //[DisplayName("واحد اندازه گیری")]
        //[DisplayInfo(null, true, type: SystemType.Entity)]
        //public PartUnit? ProjectPartUnit { get; set; }
        //public long? ProjectPartUnitId { get; set; }


        //[DisplayName("سال مالی")]
        //[DisplayInfo(null, true, type: SystemType.Entity)]
        //public short? ProjectVchYear { get; set; }



        //[DisplayName("قطعه داغی")]
        //[DisplayInfo(null, true, type: SystemType.Entity)]
        //public virtual Part DamagedPart { get; set; }
        //public long? DamagedPartId { get; set; }


        //[DisplayName("وضعیت داغی")]
        //[DisplayInfo(null, true, type: SystemType.Select)]
        //public DamagedPartTypeEnum DamagedPartType { get; set; }


        //[DisplayName("توضیح قطعه داغی")]
        //[DisplayInfo(null, true, type: SystemType.String)]
        //public string? DamagedPartDescription { get; set; }


        //[DisplayName("خرابی دارد")]
        //[DisplayInfo(null, true, type: SystemType.Boolean)]
        //public bool HasFailure { get; set; } = false;


        //[DisplayName("قطعه معیوب")]
        //[DisplayInfo(null, true, type: SystemType.Entity)]
        //public virtual Part FailurePart { get; set; }
        //public long? FailurePartId { get; set; }


        //[StringLength(2048)]
        //[DisplayName("شرح خرابی")]
        //[DisplayInfo(null, true, type: SystemType.String)]
        //public string? FailureDescription { get; set; }


    }
}
