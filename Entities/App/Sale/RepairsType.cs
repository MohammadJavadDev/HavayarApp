using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
    [Display(Name = "کد های عملیاتی")]
    [Table("RepairsType", Schema = "Sale")]
    public class RepairsType : BaseEntity
    {

        [DisplayName("عنوان")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Title { get; set; }

        [DisplayName("کد")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Code { get; set; }
    }
}


