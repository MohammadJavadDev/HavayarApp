using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Prp
{
    /// <summary>
    /// اقلام مصرفی 
    /// </summary>
    [Display(Name = "درخواست تعمیر قطعه")]
    [Table("RepairRequestPart", Schema = "Rpr")]
    public class RepairRequestPart : BaseEntity
    {

        [DisplayName("درخواست تعمیر")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public RepairRequest? RepairRequest { get; set; }
        public long? RepairRequestId { get; set; }


        [DisplayName("مشتری")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public PartRequestItem? PartRequestItem { get; set; }
        public long? PartRequestItemId { get; set; }


        //[DisplayName("")]
        //[DisplayInfo(null, true, type: SystemType.Entity)]
        //public PartRequestItem? AccDL { get; set; }
        //public long? AccDLId { get; set; }

        [DisplayName("شماره درخواست")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? RequestNo { get; set; }

        /// <summary>
        /// به چی باید وصل بشه؟
        /// </summary>
        //public int? Hamkaran_Dl_Fk { get; set; }



        [DisplayName("کالا")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Part? Part { get; set; }
        public long? PartId { get; set; }



        [DisplayName("واحد اندازه گیری")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public PartUnit? PartUnit { get; set; }
        public long? PartUnitId { get; set; }



        [DisplayName("حساب تفصیلی")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public DL? AccDL { get; set; }
        public long? AccDLId { get; set; }


        /// <summary>
        /// به چی؟
        /// و دیتا هم نداره
        /// </summary>
        //public int Hamkaran_InvVchItm_FK { get; set; }

        [DisplayName("داغی دارد؟")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool DamagedPart { get; set; }


        [DisplayName("مرجع سهام")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? StockRef { get; set; }

        [DisplayName("نام سهام")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? StockName { get; set; }



        [DisplayName("نام واحد")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? UnitName { get; set; }



        [DisplayName("مقدار مصرفی")]
        [DisplayInfo(null, true, type: SystemType.Decimal)]
        public decimal UsedMount { get; set; }


        [DisplayName("مقدار برگشتی")]
        [DisplayInfo(null, true, type: SystemType.Decimal)]
        public decimal ReturnMount { get; set; }


        [DisplayName("مبلغ خرید")]
        [DisplayInfo(null, true, type: SystemType.Long)]
        public long? BuyPrice { get; set; }


        [DisplayName("آخرین قیمت خرید به شمسی")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? LastBuyPriceDate_Shamsi { get; set; }

        [DisplayName("قیمت فروش")]
        [DisplayInfo(null, true, type: SystemType.Long)]
        public long? SalePrice { get; set; }

        [DisplayName("مجموع قیمت")]
        [DisplayInfo(null, true, type: SystemType.Decimal)]
        public decimal? TotalPrice { get; set; }

        [DisplayName("مجموع مبلغ خرید")]
        [DisplayInfo(null, true, type: SystemType.Decimal)]
        public decimal? TotalBuyPrice { get; set; }



        [DisplayName("مجموع  تغییرات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? ChangesSet { get; set; }



        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Description { get; set; }


        [DisplayName("توضیحات قطعه داغی")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? ReplacementPartDescription { get; set; }

        [DisplayName("توضیحات راهکاران")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? RahkaranDescription { get; set; }


        [DisplayName("شماره فرم قطعه داغی")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? ReplacementPartFormNumber { get; set; }


    }
}