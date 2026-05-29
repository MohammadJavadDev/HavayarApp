using Common.Attributes;
using Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Base;


public abstract class BaseEntity<TId> where TId : struct
{
    [Key]
    [DisplayName("شناسه")]
    [DisplayInfo(null, false, SystemType.Long, systemProprty: true)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public virtual TId? Id { get; set; }
	[DisplayName("شناسه ایجاد کننده")]
    [DisplayInfo(null, false, SystemType.Long, systemProprty: true)]

    public	long? CreatedById { get; set; }

    [DisplayName("شناسه ویرایش کننده")]
    [DisplayInfo(null, false, SystemType.Long, systemProprty: true)]
    public long? ModifiedById { get; set; }

	[DisplayName("ویرایش کننده")]
	[DisplayInfo(null, true, SystemType.Entity, systemProprty: true)]
 
	[NotMapped]
	public User? ModifiedBy { get; set; }

	[DisplayName("نام ایجاد کننده")]
    [DisplayInfo(null, true, SystemType.String,systemProprty: true)]
    [MaxLength(150)]
    public string? CreatedByName { get; set; }

	[DisplayName("ایجاد کننده")]
	[DisplayInfo(null, true, SystemType.Entity, systemProprty: true)]
	[NotMapped]
	public User? CreatedBy { get; set; }

	[DisplayName("نام ویرایش کننده")]
    [DisplayInfo(null, false, SystemType.String, systemProprty: true)]
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

    [DisplayInfo(null, false, SystemType.DateTime, systemProprty: true)]
    public DateTime? CreatedOnMiladiDateTime { get; set; } = null;

    [DisplayName("تاریخ شمسی ایجاد")]
    [MaxLength(30)]
    [DisplayInfo("CreatedOnMiladiDateTime", false, SystemType.DateTimeShamsi, systemProprty: true)]
    public string? CreatedOnShamsiDateTime { get; set; } = null;

    [DisplayName("وضعیت فعال بودن")]
    [DisplayInfo(null, true, SystemType.Select, systemProprty: true)]
    public IsActiveEnum? IsActive { get; set; }

}
public abstract class BaseEntity : BaseEntity<long>
{
}


public enum IsActiveEnum
{
    [Display(Name = "غیر فعال")]
    DeActive,
    [Display(Name = "فعال")]
    Active,
	[Display(Name = "حذف شده")]
	Deleted,
}

 

 
//public class BaseEntityConfiguration : IEntityTypeConfiguration<BaseEntity>
//{
//    public void Configure(EntityTypeBuilder<BaseEntity> builder)
//    {
//        builder.HasKey(c => c.Id);

//    }
//}