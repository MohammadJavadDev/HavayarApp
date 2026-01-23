using Common.Attributes;
using Common.Entities;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace Entities.Auth;

[Table(name:"User",Schema = "system")]
public class User
{

    [Key]
    [DisplayName("شناسه")]
    [DisplayInfo(null, false, systemProprty: true)]
    public long? Id { get; set; }
    [DisplayName("شناسه ایجاد کننده")]
    [DisplayInfo(null, false, systemProprty: true)]

    public long? CreatedById { get; set; }

    [DisplayName("شناسه ویرایش کننده")]
    [DisplayInfo(null, false, systemProprty: true)]
    public long? ModifiedById { get; set; }

    [DisplayName("نام ایجاد کننده")]
    [DisplayInfo(null, true, systemProprty: true)]
    [MaxLength(150)]
    public string? CreatedByName { get; set; }

    [DisplayName("نام ویرایش کننده")]
    [DisplayInfo(null, false, systemProprty: true)]
    [MaxLength(150)]

    public string? ModifiedByName { get; set; }


    [DisplayName("تاریخ میلادی ویرایش")]
    [DisplayInfo(null, false, SystemType.DateTime, systemProprty: true)]
    public DateTime? ModifiedDateMiladiDateTime { get; set; } = null;

    [DisplayName("تاریخ شمسی ویرایش")]
    [MaxLength(30)]
    [DisplayInfo("ModifiedDateMiladiDateTime", true, SystemType.DateTimeShamsi, systemProprty: true)]
    public string? ModifiedDateShamsiDateTime { get; set; } = null;

    [DisplayName("تاریخ میلادی ایجاد")]

    [DisplayInfo(null, false,SystemType.DateTime, systemProprty: true)]
    public DateTime? CreatedOnMiladiDateTime { get; set; } = null;

    [DisplayName("تاریخ شمسی ایجاد")]
    [MaxLength(30)]
    [DisplayInfo("CreatedOnMiladiDateTime", false, SystemType.DateTimeShamsi, systemProprty: true)]
    public string? CreatedOnShamsiDateTime { get; set; } = null;

    [DisplayName("وضعیت فعال بودن")]
    [DisplayInfo(null, true, systemProprty: true,type:SystemType.Select)]
    public IsActiveEnum IsActive { get; set; } =IsActiveEnum.Active;


    [Required(ErrorMessage = AppMessages.ReqiredMessage + "نام")]
    [DisplayName("نام")]
    [DisplayInfo(null, false, required: true, showInRelationData: true, type: SystemType.String)]

    public string Name { get; set; }
    [Required(ErrorMessage = AppMessages.ReqiredMessage + "نام کاربری")]

    [DisplayName("نام کاربری")]
    [DisplayInfo(null, false, required: true, type: SystemType.String)]
    public string Username { get; set; }

    [DisplayName("رمزعبور")]
    [DisplayInfo(null, false)]
    public string? Password { get; set; }

    [Required(ErrorMessage = AppMessages.ReqiredMessage + "نقش ها")]
    [DisplayName("نقش ها")]
	[DisplayInfo(null, false, SystemType.ListString )]
	public string[] Roles { get; set; }

	[DisplayName("شناسه نقش ها")]
	[DisplayInfo(null, false, SystemType.ListLong)]
	public List<long> RoleIds { get; set; } = [];


	[DisplayName("تصویر پروفایل")]
    [DisplayInfo(null, false, type: SystemType.File, fileTypes: "image/png,image/jpg,image/jpge")]

    public string? ProfileUrl { get; set; }


	[DisplayName("ایمیل")]
	[DisplayInfo(null, false, type: SystemType.String)]

	public string? Email { get; set; }

	[DisplayName("آخرین آنلاینی")]
    [DisplayInfo(null, false, SystemType.DateTime)]
    public DateTime? LastOnline { get; set; }

	[DisplayName("نوع احراز")]
	[DisplayInfo(null, false, SystemType.Select)]

	public AuthorizationTypeEnum AuthorizationType { get; set; }

	public long? HamkaranId { get; set; }

	[NotMapped]
	public List<RoleAccess> RoleAccesses { get; set; } = new();

}

public enum AuthorizationTypeEnum
{
	[Display(Name ="سیستم")]
	System,
	[Display(Name = "اکتیودایرکتوری")]
	ActiveDirectory,
	[Display(Name = "متفرقه")]
	Custom
}