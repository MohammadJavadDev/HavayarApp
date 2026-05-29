using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


[Display(Name = "پیمانکار")]
[Table("Contractor", Schema = "Rpr")]
public class Contractor : BaseEntity
{

    [DisplayName("عنوان")]
    [DisplayInfo(null, true, type: SystemType.String)]
    public string? Title { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, true, type: SystemType.Entity)]
    public string? Comment { get; set; }

}


