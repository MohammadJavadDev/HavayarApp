using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Rpr;
using Entities.App.SLS;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{

	[Display(Name = "درخواست خدمات")]
	[Table("ServiceRequest", Schema = "Sale")]
	public class ServiceRequest : BaseEntity
	{

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Customer? Customer { get; set; }
		public long? CustomerId { get; set; }


		[DisplayName("سایت مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Customer? CustomerAgency { get; set; }
		public long? CustomerAgencyId { get; set; }

		[DisplayName("کد و عنوان مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? Party { get; set; }
		public long? PartyId { get; set; }


		[DisplayName("نام نماینده مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? CustomerAgentName { get; set; }


		[DisplayName("سمت نماینده مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? CustomerAgentJobPosition { get; set; }


		[DisplayName("شماره همراه نماینده مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? CustomerAgentMobile { get; set; }


		[DisplayName("آدرس ایمیل نماینده مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? CustomerAgentEmailAddress { get; set; }


		[DisplayName("تاریخ تماس شمسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ContactShamsiDate { get; set; }

		[DisplayName("تاریخ تماس میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ContactMiladiDate { get; set; }


		[DisplayName("شماره شناسنامه های تعمیرات")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual RepairRequest RepairRequest { get; set; }
		public long? RepairRequestId { get; set; }


		[DisplayName("غیر واقعی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Unreal { get; set; }


		[DisplayName("علت اعزام کارشناس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ReasonSendingExpert { get; set; }


		[DisplayName("مسئول منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Personel ResponsibleZone { get; set; }
		public long? ResponsibleZoneId { get; set; }

		[DisplayName("نام منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Region? DispachZone { get; set; }
		public long? DispachZoneId { get; set; }


		[DisplayName("شناسه کارشناس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ExpertNameId { get; set; }

		[DisplayName("کارشناس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ExpertName { get; set; }



		[DisplayName("گزارش کار")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(SaleReportId))]
		public SaleReport? SaleReport { get; set; }
		public long? SaleReportId { get; set; }

	}
}
