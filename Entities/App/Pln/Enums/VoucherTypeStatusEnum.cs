using System.ComponentModel.DataAnnotations;

namespace Entities.App.Pln.Enums
{
    public enum VoucherTypeStatusEnum
    {
        [Display(Name = "KG")]
        KG = 1,

        [Display(Name = "Pcs")]
        PiePcsce = 2,

        [Display(Name = "Meter")]
        Meter = 3,

        [Display(Name = "Set")]
        Set = 4,

        [Display(Name = "نفر ساعت")]
        PersonHour = 5,

        [Display(Name = "hr")]
        HR = 6,

        [Display(Name = "m3")]
        M3 = 7,

    }


}
