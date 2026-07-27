using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inq.Enums
{
    public enum TechnicalInquiryStatusEnum
    {
        [Display(Name = "ثبت اولیه")]
        InitialRegistration = 1,

        [Display(Name = "ویرایش شده")]
        Modified = 2,

        [Display(Name = "ثبت توسط درخواست دهنده - منتظر اقدام مهندسی")]
        SendToEngineering = 3,

        [Display(Name = "نیاز به اصلاح (عدم تائید مدیر پروژه)")]
        ManagerReject = 4,

        [Display(Name = "تایید مدیر پروژه - منتظر اقدام تامین و خرید")]
        ManagerApproved = 5,

        [Display(Name = "ارسال به مالی")]
        SendToFinancial = 6,

        [Display(Name = "تایید تدارکات")]
        SuppliesApprove = 7,

        [Display(Name = "تایید تامین و خرید - ارسال به پیمانکار")]
        IssuedToVendor = 8,

        [Display(Name = "تکمیل مدارک توسط پیمانکار-منتظر اقدام تامین و خرید")]
        VendorIssued = 9,

        [Display(Name = "پروژه نقدینگی دارد")]
        ProjectHasLiquidity = 10,

        [Display(Name = "پروژه نقدینگی ندارد")]
        ProjectHasNoLiquidity = 11,

        [Display(Name = "عدم تایید مدارک پیمانکار توسط تامین و خرید")]
        SuppliesRejectVendorDosc = 12,

        [Display(Name = "تایید مدارک پیمانکار توسط تامین و خرید-منتظر اقدام مهندسی ")]
        SuppliesApprovedVendorDocs = 13,

        [Display(Name = "تایید مدارک پیمانکار توسط مهندسی")]
        EngineeringApprovedVendorDocs = 14,

        [Display(Name = "عدم تایید مدارک پیمانکار توسط مهندسی")]
        EngineeringRejectedVendorDosc = 15,

        [Display(Name = "پایان")]
        Final = 16,

        [Display(Name = "بررسی پیشنهاد تامین کنندگان توسط مهندسی")]
        ReviewSupplierProposalsByEngineering = 17,

        [Display(Name = "ارسال مهندسی به مدیر پروژه")]
        SendEngineeringToTheProjectManager = 18,

        [Display(Name = "ثبت درخواست مالی از تامین کنندگان")]
        RegisteringFinancialRequestsFromSuppliers = 19,

        [Display(Name = "وضعیت تغییر در تأمین کالا")]
        SupplyChangeStatus = 20,

        [Display(Name = "آپلود فایل توسط پیمانکار")]
        VendorUploadFile = 21,

        [Display(Name = "رد مدارک پیمانکار")]
        VendorUploadFileReject = 22,

        [Display(Name = "تائید مدارک پیمانکار")]
        VendorUploadFileApprove = 23,

        [Display(Name = "بازنگری فایل توسط مهندسی - منتظر اقدام تامین و خرید")]
        Reviewfilebyengineeringandsendingtosupplyandprocurement = 24,

        [Display(Name = "تایید مشروط مدارک پیمانکار توسط مهندسی")]
        EngineeringRoleApprovedVendorDosc = 25,

        [Display(Name = "عدم تایید مدارک توسط تامین و خرید برگشت به مهندسی")]
        SupplierRejectToEngineering = 26
    }

    public enum QueryTypeEnum
    {
        [Display(Name = "داخلی")]
        Internal = 1,

        [Display(Name = "خارجی")]
        External = 2
    }

}
