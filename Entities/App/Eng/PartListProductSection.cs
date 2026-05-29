using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Eng
{
    [Display(Name = "لیست قطعات و محصولات")]
    [Table("PartListProductSection", Schema = "Eng")]
    public class PartListProductSection : BaseEntity
    {
        [DisplayName("کالا")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Part? Part { get; set; }
        public long? PartId { get; set; }

        [DisplayName("عنوان")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Title { get; set; }

        [DisplayName("سفارش")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? Order { get; set; }

        [DisplayName("مسیر عکس")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [MaxLength(500)]
        public string? PicturePath { get; set; }

        [DisplayName("دارای کاور")]
        [DisplayInfo(null, true, type: SystemType.Boolean)]
        public bool IsCover { get; set; }


    }
}
