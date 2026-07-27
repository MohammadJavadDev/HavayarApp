using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
    [Display(Name = "کسری قطعات")]
    [Table("RepairRequestPartFraction", Schema = "Rpr")]
    public class RepairRequestPartFraction : BaseEntity
    {

        [DisplayName("عنوان")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [StringLength(4000)]
        public string? Title { get; set; }


        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [StringLength(4000)]
        public string? Description { get; set; }


    }
}
