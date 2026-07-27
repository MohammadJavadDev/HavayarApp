using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
    [Display(Name = "مراکز تعمیر")]
    [Table("Station", Schema = "Rpr")]
    public class Station : BaseEntity
    {


        [DisplayName("عنوان")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Title { get; set; }


        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public string? Comment { get; set; }

    }
}

