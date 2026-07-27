using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
    [Display(Name = "درخواست تعمیر قطعه استفاده شده")]
    [Table("RepairRequestUsedPart", Schema = "Rpr")]
    public class RepairRequestUsedPart : BaseEntity
    {

        [DisplayName("تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public RepairRequest? RepairRequest { get; set; }
        public long? RepairRequestId { get; set; }

        /// <summary>
        // TODO After Imoliment Entity Acc_DL
        /// </summary>
        //public int AccDL_FK { get; set; }

        ///<summary>
        /// از کجا داره پر میشه 
        ///</summary>
        //public int? Hamkaran_Dl_Fk { get; set; }



        [DisplayName("کالا")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Part? Part { get; set; }
        public long? PartId { get; set; }



        [DisplayName("واحد اندازه گیری")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public PartUnit? PartUnit { get; set; }
        public long? PartUnitId { get; set; }


        /// <summary>
        /// از کجا داره پر میشه ؟
        /// </summary>
        //public int? Hamkaran_InvVchItm_FK { get; set; }

        [DisplayName("داغی دارد؟")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool? DamagedPart { get; set; }


        [DisplayName("مرجع سهام")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? StockRef { get; set; }


        [DisplayName("نام سهام")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? StockName { get; set; }


        [DisplayName("واحد")]
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
        public string? LastBuyPriceShamsiDate { get; set; }

        [DisplayName("قیمت فروش")]
        [DisplayInfo(null, true, type: SystemType.Long)]
        public long? SalePrice { get; set; }


        [DisplayName("مجموع قیمت")]
        [DisplayInfo(null, true, type: SystemType.Decimal)]
        public decimal? TotalPrice { get; set; }


        [DisplayName("مجموع مبلغ خرید")]
        [DisplayInfo(null, true, type: SystemType.Decimal)]
        public decimal? TotalBuyPrice { get; set; }



        [DisplayName("نسخه")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? Revision { get; set; }



        //public virtual Acc_DL Acc_DL { get; set; }


    }
}