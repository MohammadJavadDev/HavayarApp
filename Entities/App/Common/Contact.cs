using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common.Attributes;
using Entities.Base;

namespace Entities.App;

[Display(Name = "مخاطب")]
[Table("Contact", Schema = "dbo")]
public class Contact : BaseEntity
{
	[DisplayName("نام")]
	[DisplayInfo(null, true, type:  SystemType.String, required: true , showInRelationData:true)]
	[MaxLength(150)]
	public string Name { get; set; }

	[DisplayName("نام خانوادگی")]
	[DisplayInfo(null, true, type: SystemType.String)]
	[MaxLength(200)]
	public string? LastName { get; set; }

	[DisplayName("کد ملی")]
	[DisplayInfo(null, true, type: SystemType.String)]
	[MaxLength(10)]
	public string? NationalCode { get; set; }

	[DisplayName("شناسه ملی")]
	[DisplayInfo(null, true, type: SystemType.String)]
	[MaxLength(12)]
	public string? NationalId { get; set; }

    [DisplayName("نوع مخاطب")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public ContactType ContactType { get; set; }

    [DisplayName("تامین کننده است")]
    [DisplayInfo(null, false, type: SystemType.Boolean)]
    public bool IsSupplier{ get; set; }
	[DisplayName("آدرس ها")]
	[DisplayInfo(null, false, type: SystemType.ListEntity)]
	public List<ContactAddress> Address { get; set; }
}
public class ContactAddress:BaseEntity
{
	[DisplayName("آدرس")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string Address { get; set; }
	[DisplayName("نوع")]
	[DisplayInfo(null, true, type: SystemType.Select)]
	public AddressType Type { get; set; }

	[DisplayName("شناسه شهر")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? CityId { get; set; }

	[DisplayName("شهر")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public Region City { get; set; }
 
}

public class Region : BaseEntity
{
	[DisplayName("نام")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string Name { get; set; }
	[DisplayName("نوع")]
	[DisplayInfo(null, true, type: SystemType.Select)]
	public RegionType Type { get; set; }

	[DisplayName("شناسه والد")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? ParentId { get; set; }

	[DisplayName("والد")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public Region? Parent { get; set; }
}
public enum RegionType
{

	[Display(Name = "کشور")]
	Country,
	[Display(Name = "استاد")]
	State,
	[Display(Name = "شهر")]
	City,
	[Display(Name = "شهرستان")]
	MiddleCity
}

public enum AddressType
{
	[Display(Name = "خونه")]
	Home,
	[Display(Name = "محل کار")]
	Work,
	[Display(Name = "سایر")]
	Other
}

public enum ContactType
{
    [Display(Name = "حقیقی")]
    Real,
    [Display(Name = "حقوقی")]
    Legal,
}