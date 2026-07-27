using System.ComponentModel.DataAnnotations;

namespace Entities.App.Pln.Enums
{
    public enum StatusEnum
    {
        [Display(Name = "ایجاد")]
        Issue = 1,

        [Display(Name = "تایید")]
        Approve = 2,

        [Display(Name = "عدم تایید")]
        Reject = 3,

        [Display(Name = "تغییر قطعه")]
        PartChanged = 4,

    }
}
