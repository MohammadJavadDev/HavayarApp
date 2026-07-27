using System.ComponentModel.DataAnnotations;

public enum PayMethodEnum
{
    [Display(Name = "فکس")]
    Fax = 1,

    [Display(Name = "ایمیل")]
    Email = 2,

    [Display(Name = "شبکه های اجتماعی")]
    SocialNetworks = 3,

    [Display(Name = "پست")]
    Post = 4,

    [Display(Name = "در انتظار تایید")]
    PendingApproval = 5,

    [Display(Name = "تایید")]
    Approved = 6,

    [Display(Name = "عدم تایید")]
    NotApproved = 7,

    [Display(Name = "درحال بررسی")]
    UnderReview = 8,

    [Display(Name = "لغو")]
    Canceled = 9,

    [Display(Name = "فراخوان عمومی")]
    PublicAnnouncement = 10,

    [Display(Name = "اختصاصی")]
    PrivateAnnouncement = 11
}
