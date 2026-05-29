using Common.Attributes;
using Common.Entities;
using Common.Utilities;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 

namespace Entities.Auth;
[Table(name: "Role", Schema = "system")]
public class Role:BaseEntity
{
    [Required(ErrorMessage = AppMessages.ReqiredMessage + "نام")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [DisplayName("نام")]
    public string Name { get; set; }
    [Required(ErrorMessage = AppMessages.ReqiredMessage + "عنوان")]
    [DisplayInfo(null, true, type: SystemType.String ,showInRelationData:true)]
    [DisplayName("عنوان")]
    public string Title { get; set; }
 
 
    public List<RoleAccess> RoleAccesses { get; set; } = new();
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
	public void Configure(EntityTypeBuilder<Role> builder)
	{
		builder.ToTable("Role", "system");
		builder.HasKey(t => t.Id);

		//Smaple Date For Add Seeds
		var now = DateTime.Parse("2/7/2026 2:02:17 PM");
		
		//For System Role Check in Code we Use Seed For Create Roles 
		//after publish project add users to this roles 

		#region Sup.OpenOrderRequest Role Seed 

		builder.HasData(new Role()
		{
			Id=100000,
			Name = "Sup.OpenOrderRequest.EngineeringAccept",
			Title = "تامین و خرید -درخواست های باز - تایید مهندسی ",
			CreatedById = 1,
			CreatedByName="admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});

		builder.HasData(new Role()
		{
			Id = 100001,
			Name = "Sup.OpenOrderRequest.SalesOrProjectAccept",
			Title = "تامین و خرید -درخواست های باز - تایید فروش/پروژه ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});
		builder.HasData(new Role()
		{
			Id = 100002,
			Name = "Sup.OpenOrderRequest.Stop",
			Title = "تامین و خرید -درخواست های باز - ثبت توقف ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});

		builder.HasData(new Role()
		{
			Id = 100003,
			Name = "Sup.OpenOrderRequest.Start",
			Title = "تامین و خرید -درخواست های باز - راه‌اندازی ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});


		builder.HasData(new Role()
		{
			Id = 100004,
			Name = "Sup.OpenOrderRequest.Sending",
			Title = "تامین و خرید -درخواست های باز - در راه ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});



		builder.HasData(new Role()
		{
			Id = 100005,
			Name = "Sup.OpenOrderRequest.Query",
			Title = "تامین و خرید -درخواست های باز - استعلام ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});


		builder.HasData(new Role()
		{
			Id = 100006,
			Name = "Sup.OpenOrderRequest.Terminate",
			Title = "تامین و خرید -درخواست های باز - خاتمه ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});

		builder.HasData(new Role()
		{
			Id = 100007,
			Name = "Sup.OpenOrderRequest.HasEngineering",
			Title = "تامین و خرید -درخواست های باز - دسترسی قابلیت‌های مهندسی ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});


		builder.HasData(new Role()
		{
			Id = 100008,
			Name = "Sup.OpenOrderRequest.ShowAll",
			Title = "تامین و خرید -درخواست های باز - نمایش همه درخواست‌ها ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active,
		});

		builder.HasData(new Role()
		{
			Id = 100009,
			Name = "Sup.OpenOrderRequest.Industrial",
			Title = "تامین و خرید -درخواست های باز - صنایع ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});

		builder.HasData(new Role()
		{
			Id = 100010,
			Name = "Sup.OpenOrderRequest.Supply",
			Title = "تامین و خرید -درخواست های باز - تامین و خرید ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});

		builder.HasData(new Role()
		{
			Id = 100011,
			Name = "Sup.OpenOrderRequest.ConfigManage",
			Title = "تامین و خرید -درخواست های باز - تنظیمات ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});


		builder.HasData(new Role()
		{
			Id = 100012,
			Name = "Sup.OpenOrderRequest.ConfigManageStaticPersonel",
			Title = "تامین و خرید -درخواست های باز - نفرات ثابت ذینفعان  ",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});


		#endregion

		#region Sup.OpenOrderRequest Role Seed 

		builder.HasData(new Role()
		{
			Id = 200000,
			Name = "Edms.Documents.DccUsers",
			Title = "مهندسی -مدارک - کاربران DCC",
			CreatedById = 1,
			CreatedByName = "admin",
			ModifiedById = 1,
			ModifiedByName = "admin",
			CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
			ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
			CreatedOnMiladiDateTime = now,
			ModifiedDateMiladiDateTime = now,
			IsActive = IsActiveEnum.Active
		});
		#endregion
	}
}

