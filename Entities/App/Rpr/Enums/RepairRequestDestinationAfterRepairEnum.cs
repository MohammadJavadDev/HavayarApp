using System.ComponentModel.DataAnnotations;

namespace Entities.App.Prp.Enums
{
    public enum RepairRequestDestinationAfterRepairEnum
    {
        [Display(Name = "مشتری")]
        Customer = 293,

        [Display(Name = "نمایندگی")]
        Representation = 294,

        [Display(Name = "قطعات داغی هوایار")]
        HavayarPartHot = 295,

        [Display(Name = "قطعات برگشتی هوایار")]
        HavayarReturnPart = 296,

        [Display(Name = "هوایار")]
        Havayar = 2700,
    }

    public enum RepairRequestStatusEnum
    {
        [Display(Name = "در حال انجام")]
        InProgress = 301,

        [Display(Name = "توقف")]
        stop = 302,

        [Display(Name = "غیرقابل تعمیر")]
        Irreparable = 304,

        [Display(Name = "تکمیل شده")]
        Complete = 305,

        [Display(Name = "ارسال به پیمانکار")]
        SendToTheContractor = 1535,

        [Display(Name = "در انتظار تایید مشتری")]
        WaitingCustomerApproval = 1536,

        [Display(Name = "درانتظار خرید قطعه")]
        WaitingToBuyThePiece = 1537,

        [Display(Name = "در حال تهیه PreCheck")]
        PreparingForPreCheck = 1774,

        [Display(Name = "در انتظار تست")]
        WaitingForTest = 1871,

        [Display(Name = "عدم تایید مشتری")]
        CustomerDisapproval = 2092,

        [Display(Name = "تحویل به انبار")]
        DeliveryToTheWarehouse = 2093,

        [Display(Name = "آیتم صوری")]
        FormalItem = 2236,

        [Display(Name = "در انتظار تعمیر ")]
        AwaitingRepair = 2413,

        [Display(Name = "در انتظار بازدید مشتری")]
        WaitingForCustomerVisit = 3169,

    }

    public enum RepairRequestJobDescriptionEnum
    {
        [Display(Name = "اورهال کامل")]
        CompleteOverhaul = 1547,

        [Display(Name = "اورهال ایرند")]
        OverhaulIreland = 1548,

        [Display(Name = "اورهال موتور")]
        EngineOverhaul = 1549,

        [Display(Name = "تعمیر برد")]
        boardRepair = 1550,

        [Display(Name = "تعمیر رادیاتور")]
        RadiatorRepair = 1551,

        [Display(Name = "تعمیر")]
        Repair = 1922,
    }

    public enum RepairRequestLastStatusEnum
    {
        [Display(Name = "ثبت اولیه")]
        InitialRegistration = 1864,

        [Display(Name = "ویرایش شده")]
        Edited = 1865,

        [Display(Name = "تایید سرپرست/مدیر")]
        SupervisorManagerApproval = 1866,

        [Display(Name = "تایید QC")]
        QcConfirmation = 1867,

        [Display(Name = "بسته شده")]
        Closed = 1868,

    }

    /// <summary>
    /// نوع تجهیزات تعمیری
    /// </summary>
    public enum TypeRepairEquipmentEnum
    {
        [Display(Name = "درایر")]
        Dryer = 1,

        [Display(Name = "کمپرسور")]
        Compressor = 2,

        [Display(Name = "ایرند")]
        AirEnd = 3,

        [Display(Name = "الکتروموتور")]
        ElectroMotor = 4,

        [Display(Name = "برد PLC")]
        PlcBoard = 5,

        [Display(Name = "وکیوم پمپ")]
        VacuumPump = 6,

        [Display(Name = "رادیاتور")]
        Radiator = 7,

        [Display(Name = "آنلودر")]
        Unloader = 8,

        [Display(Name = "مینیمم پرشر ولو")]
        MinimumPressureValve = 9,

        [Display(Name = "اکسیژن ساز")]
        OxygenGenerator = 10,

        [Display(Name = "نیتروژن ساز")]
        NitrogenGenerator = 11,

        [Display(Name = "مخزن")]
        Tank = 12,

        [Display(Name = "تابلو اینورتر")]
        InverterPanel = 13,

        [Display(Name = "اینورتر")]
        Inverter = 14,

        [Display(Name = "تابلو سکوئنسر")]
        SequencerPanel = 15,

        [Display(Name = "سافت استارتر")]
        SoftStarter = 16
    }

}
