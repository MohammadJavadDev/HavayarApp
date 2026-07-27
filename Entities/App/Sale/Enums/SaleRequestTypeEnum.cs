using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
    public enum SaleRequestTypeEnum
    {
        [Display(Name = "آموزش")]
        Learning = 1,

        [Display(Name = "اورهال")]
        Overhaul = 2,

        [Display(Name = "بازدید و مشاوره")]
        InspectionAndConsultation = 3,

        [Display(Name = "تعمیرات گارانتی")]
        WarrantyRepair = 4,

        [Display(Name = "تعمیرات وارانتی")]
        NonWarrantyRepair = 5,

        [Display(Name = "راه اندازی اولیه")]
        InitialStartup = 6,

        [Display(Name = "پایپینگ")]
        Piping = 7,

        [Display(Name = "سرویس دوره ای")]
        PeriodicService = 8,

        [Display(Name = "سایر موارد")]
        Other = 9,

        [Display(Name = "OPI")]
        OPI = 10,

        [Display(Name = "راه اندازی تجهیز تعمیری")]
        RepairedEquipmentStartup = 11,

        [Display(Name = "پیش راه اندازی")]
        PreCommissioning = 12,

        [Display(Name = "نصب")]
        Installation = 13
    }





    public enum MissionResultEnum
    {
        [Display(Name = "انجام شده")]
        Done = 1,

        [Display(Name = "انجام نشده")]
        NotDone = 2,

        [Display(Name = "انجام شده با نقص")]
        DoneWithIssues = 3
    }


}
