using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{


    [Display(Name = "پارت لیست مصرفی")]
    [Table("ProjectUtilizedMaterial", Schema = "Sale")]
    public class ProjectUtilizedMaterial : BaseEntity
    {

        [DisplayName("تفصیل")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual DL DL { get; set; }
        public long? DLId { get; set; }

        [DisplayName("سال")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int Year { get; set; }

        [DisplayName("شماره سند")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? VchNum { get; set; }

        [DisplayName("تاریخ سند میلادی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? VchMiladiDate { get; set; } = null;

        [DisplayName("تاریخ سند شمسی")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? VchDateShamsiDate { get; set; } = null;

        [DisplayName("مقدار")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public double Qty { get; set; }

        [DisplayName("کالا")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Part? Part { get; set; }
        public long? PartId { get; set; }

        [DisplayName("نوع تفصیل")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public DLType? DLType { get; set; }
        public byte DLTypeId { get; set; }

    }
}
