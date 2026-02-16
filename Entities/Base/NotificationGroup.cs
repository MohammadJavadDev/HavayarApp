using Common.Attributes;
using Common.Entities;
using Common.Utilities;
using Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Base
{

	[Display(Name = "گروه اعلانات")]
	[Table(name: "NotificationGroup", Schema = "system")]
	public class NotificationGroup : BaseEntity
	{
		[DisplayInfo(null, true, type: SystemType.String)]
		[DisplayName("کد")]
		[MaxLength(100)]
		public string Code { get; set; } = string.Empty;

	 
		[Required]
		[MaxLength(200)]
		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string DisplayName { get; set; } = string.Empty;

	 
		[MaxLength(1500)]
		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }

		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		[DisplayName("عضو های گروه")]
		public virtual ICollection<NotificationGroupMember> Members { get; set; } = new List<NotificationGroupMember>();
	}

	[Display(Name = "اعضای گروه اعلانات")]
	[Table(name: "NotificationGroupMember", Schema = "system")]
	public class NotificationGroupMember : BaseEntity
	{
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[DisplayName("گروه")]
		public virtual NotificationGroup NotificationGroup { get; set; } = null!;
		public long NotificationGroupId { get; set; }

		[DisplayInfo(null, true, type: SystemType.Entity)]
		[DisplayName("کاربر")]
		public virtual User User { get; set; } = null!;
		public long UserId { get; set; }


		[DisplayInfo(null, true, type: SystemType.String)]
		[DisplayName("کاربر")]
		[MaxLength(200)]
		public string Email { get; set; } = string.Empty;


		[DisplayInfo(null, true, type: SystemType.String)]
		[DisplayName("نام کامل")]
		[MaxLength(200)]
		 
		public string FullName { get; set; } = string.Empty;
		
	}

	public class NotificationGroupConfiguration : IEntityTypeConfiguration<NotificationGroup>
	{
		public void Configure(EntityTypeBuilder<NotificationGroup> builder)
		{
			builder.ToTable("NotificationGroup", "system");
			builder.HasKey(t => t.Id);

			//Smaple Date For Add Seeds
			var now = DateTime.Parse("2/7/2026 2:02:17 PM");

			//For System NotificationGroup Check in Code we Use Seed For Create NotificationGroup 
			//after publish project add users to this NotificationGroup 

			#region Sup.OpenOrderRequest Role Seed 

			builder.HasData(new NotificationGroup()
			{
				Id = 1,
				DisplayName = "تامین و خرید -درخواست های باز - واحد تدارکات ",
				Code= "Sup.OpenOrderRequest.SupplyUnit",
				Description= "گروه تدارکات استفاده شده در OpenOrderRequestController",
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


			builder.HasData(new NotificationGroup()
			{
				Id = 2,
				DisplayName = "تامین و خرید -درخواست های باز - واحد صنایع ",
				Code = "Sup.OpenOrderRequest.Industrial",
				Description = "گروه صنایع استفاده شده در OpenOrderRequestController",
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

}
