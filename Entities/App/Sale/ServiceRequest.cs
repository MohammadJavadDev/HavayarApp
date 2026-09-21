using Common.Attributes;
using Entities.App.Crm;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Inv;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "درخواست پشتیبانی")]
	[Table("ServiceRequest", Schema = "Sale")]
	public class ServiceRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("تلفن از سوابق")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? CustomerPhoneFromHistory { get; set; }

		[DisplayName("تاریخ تماس میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date, required: true)]
		public DateTime? ContactMiladiDate { get; set; }

		[DisplayName("تاریخ تماس شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ContactShamsiDate { get; set; }

		[DisplayName("سایت مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual CustomerAddress? CustomerAddress { get; set; }
		public long? CustomerAddressId { get; set; }

		[DisplayName("منطقه مسئول")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Zone? ResponsibleZone { get; set; }
		public long? ResponsibleZoneId { get; set; }

		[DisplayName("مسئول منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(ResponsiblePersonelId))]
		public virtual Personel? ResponsiblePersonel { get; set; }
		public long? ResponsiblePersonelId { get; set; }

		[DisplayName("منطقه اعزام")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(DispatchZoneId))]
		public virtual Zone? DispatchZone { get; set; }
		public long? DispatchZoneId { get; set; }

		[DisplayName("ماموریت کارشناس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? ExpertMission { get; set; }

		[DisplayName("نام محصولات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? AllProductName { get; set; }

		[DisplayName("صوری")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Unreal { get; set; }

		[DisplayName("تاریخ اعزام میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? DispatchMiladiDate { get; set; }

		[DisplayName("تاریخ اعزام شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? DispatchShamsiDate { get; set; }

		[DisplayName("نام نماینده مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? CustomerAgentName { get; set; }

		[DisplayName("سمت نماینده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CustomerAgentJobPosition { get; set; }

		[DisplayName("تلفن نماینده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CustomerAgentTell { get; set; }

		[DisplayName("ایمیل نماینده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? CustomerAgentEmailAddress { get; set; }

		[DisplayName("واحد ایجادکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrgUnit? CreatedOrgUnit { get; set; }
		public long? CreatedOrgUnitId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		[DisplayName("در انتظار اعزام")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsWaitingToSend { get; set; }

		[DisplayName("شناسه درخواست‌های تعمیر HTS")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? LegacyRepairRequestIds { get; set; }

		[NotMapped]
		[DisplayName("کارشناسان ماموریت")]
		public string? ExpertPersonelIds { get; set; }

		public virtual ICollection<ServiceRequestDetail> Details { get; set; } = new List<ServiceRequestDetail>();
		public virtual ICollection<ServiceRequestExpertMission> ExpertMissions { get; set; } = new List<ServiceRequestExpertMission>();
		public virtual ICollection<ServiceRequestAttachment> Attachments { get; set; } = new List<ServiceRequestAttachment>();
	}

	public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
	{
		public void Configure(EntityTypeBuilder<ServiceRequest> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_ServiceRequest_HtsId");
			builder.HasIndex(x => x.ResponsiblePersonelId)
				.HasDatabaseName("IX_Sale_ServiceRequest_ResponsiblePersonelId");
		}
	}

	[Display(Name = "جزئیات درخواست پشتیبانی")]
	[Table("ServiceRequestDetail", Schema = "Sale")]
	public class ServiceRequestDetail : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? ServiceRequestId { get; set; }
		public virtual ServiceRequest? ServiceRequest { get; set; }

		[DisplayName("نوع درخواست")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ServiceRequestTypeEnum RequestType { get; set; }

		[DisplayName("قلم حواله")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrderDetail? OrderDetail { get; set; }
		public long? OrderDetailId { get; set; }

		[DisplayName("سریال")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrderDetailSerial? OrderDetailSerial { get; set; }
		public long? OrderDetailSerialId { get; set; }

		[DisplayName("تاریخ اعلام شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? NoticeShamsiDate { get; set; }

		[DisplayName("ساعت اعلام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? NoticeTime { get; set; }

		[DisplayName("قطعه سایر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Part? OtherPart { get; set; }
		public long? OtherPartId { get; set; }

		[DisplayName("سریال سایر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(160)]
		public string? OtherPartSerial { get; set; }
	}

	public class ServiceRequestDetailConfiguration : IEntityTypeConfiguration<ServiceRequestDetail>
	{
		public void Configure(EntityTypeBuilder<ServiceRequestDetail> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_ServiceRequestDetail_HtsId");
		}
	}

	[Display(Name = "اعزام کارشناس")]
	[Table("ServiceRequestExpertMission", Schema = "Sale")]
	public class ServiceRequestExpertMission : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? ServiceRequestId { get; set; }
		public virtual ServiceRequest? ServiceRequest { get; set; }

		[DisplayName("کارشناس")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual User? Expert { get; set; }
		public long? ExpertId { get; set; }

		[DisplayName("پرسنل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Personel? Personel { get; set; }
		public long? PersonelId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }
	}

	[Display(Name = "پیوست درخواست پشتیبانی")]
	[Table("ServiceRequestAttachment", Schema = "Sale")]
	public class ServiceRequestAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? ServiceRequestId { get; set; }
		public virtual ServiceRequest? ServiceRequest { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Title { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }
	}
}
