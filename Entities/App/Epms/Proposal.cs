using Common.Attributes;
using Entities.App.Epms.Enums;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 

namespace Entities.App.Epms
{

	[Display(Name = "پروپوزال")]
	[Table("Proposal", Schema = "Epms")]
	public class Proposal :BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(500)]
		public string Title { get; set; }
		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(500)]
		public string? Code { get; set; }

		[DisplayName("شناسه واحد درخواست دهنده")]
		[DisplayInfo(null, true, type: SystemType.Long)]
 

		public long RequestedOrgUnitId { get; set; }

		[DisplayName("واحد درخواست دهنده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
 
		public OrgUnit? RequestedOrgUnit { get; set; }

		[DisplayName("کارشناسان فروش")]
		[DisplayInfo(null, true, type: SystemType.ListString)]
		public string? SalesExperts { get; set; }  

		[DisplayName("شناسه کارشناسان فروش")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? SalesExpertIds { get; set; } 

		[DisplayName("شناسه صنعت")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? PartyIndustryId { get; set; }
		[DisplayName("صنعت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PartyIndustry? PartyIndustry { get; set; }

		[DisplayName("نوع پروپوزال درخواستی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProposalRequestEnum ProposalRequest { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProposalStatusEnum Status { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(500)]
		public string? Comment { get; set; }


		[DisplayName("نام مناقصه")]
		[DisplayInfo(null, true, type: SystemType.String)]
 
		public string? TenderName { get; set; }


		[DisplayName("شناسه مدیر فروش")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? SalesManagerId { get; set; }

		[DisplayName("مدیر فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? SalesManager { get; set; }

		[DisplayName("تاریخ شروع میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? StartMiladiDate { get; set; }

		[DisplayName("تاریخ شروع شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? StartShamsiDate { get; set; }

		[DisplayName("تاریخ مناقصه میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? TenderMiladiDate { get; set; }

		[DisplayName("تاریخ مناقصه شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? TenderShamsiDate { get; set; }

		[DisplayName("ارسال به متره")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsSendToMetre { get; set; } = false;
		[DisplayName("شناسه سرپرست فروش")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? SalesSupervisorId { get; set; }
		[DisplayName("سرپرست فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? SalesSupervisor { get; set; }

		[DisplayName("شناسه نوع مدرک درخواستی")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? VpisTypeId { get; set; }

		[DisplayName(" نوع مدرک درخواستی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public VpisType? VpisType { get; set; }


		[DisplayName("برای بخش مهندسی محصول است")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsForProductEngineeringDepartment { get; set; } = false;

		[DisplayName("دارای بسته عمومی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasGeneralPackage { get; set; } = false;
		[DisplayName("بازنگری")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int Revision { get; set; }
		[DisplayName("توضیحات بازنگری")]
		[DisplayInfo(null, true, type: SystemType.String) ]
 
		public string RevisionDescription { get; set; }
		[DisplayName("تجهیزات")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]


		public List<ProposalEquipment> ProposalEquipments { get; set; } = new();

		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<ProposalAttachment> ProposalAttachment { get; set; } = new();

		[DisplayName("مدارک")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<ProposalDocument> ProposalDocuments { get; set; } = new();

	}
	[Display(Name = "تجهیزات پروپوزال")]
	[Table("ProposalEquipment", Schema = "Epms")]
	public class ProposalEquipment:BaseEntity
	{
		public long ProposalId { get; set; }
		public Proposal Proposal { get; set; }

		[DisplayName("نام تجهیز")]
		[DisplayInfo(null, true, type: SystemType.String )]
		public string Name { get; set; }
		[DisplayName("مدل تجهیز")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string Model { get; set; }
		[DisplayName("نام تامین کننده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string SupplierName { get; set; }
		[DisplayName("استاندارد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public string Standard { get; set; }
		[DisplayName("تعداد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int Mount { get; set; }
		[DisplayName("مشخصات فنی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string TechnicalSpecifications { get; set; }
		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string Comment { get; set; }
	
	}
	[Display(Name = "پیوست پروپوزال")]
	[Table("ProposalAttachment", Schema = "Epms")]
	public class ProposalAttachment : BaseEntity
	{
		public long ProposalId { get; set; }
		public Proposal Proposal { get; set; }

		[DisplayName("بازنگری")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int Revision { get; set; } = 0;
		[DisplayName("ارسال شده به مهندسی پروژه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]

		public bool IsSendToProjectAttachments { get; set; } = false;

		[DisplayName("شناسه فایل")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long AttachmentId { get; set; }
		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public FileEntity Attachment { get; set; }
	}


}
