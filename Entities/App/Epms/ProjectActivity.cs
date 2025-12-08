using Common.Attributes;
using Entities.App.Edms.Enums;
using Entities.App.Epms.Enums;
using Entities.App.Hrm;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Epms
{
	[Display(Name = "فعالیت های پروپوزال")]
	[Table("ProjectActivity", Schema = "Epms")]
	public class ProposalActivity:BaseEntity
	{
		[DisplayName("پروپوزال")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true )]
 
		public Proposal Proposal { get; set; }
		 
		[DisplayName("شناسه پروپوزال")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long ProposalId { get; set; }

		[DisplayName("نام پروژه")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData:true)]
		[MaxLength(4000)]
		public string? ProjectName { get; set; }

		[DisplayName("واحد ذینفع")]
		[DisplayInfo(null, true, type: SystemType.Entity )]

		public OrgUnit? BeneficiaryUnit { get; set; }
		[DisplayName("شناسه واحد ذینفع")]
		[DisplayInfo(null, true, type: SystemType.Long )]
		public long? BeneficiaryUnitId { get; set; }

		[DisplayName("نوع فعالیت")]
		[DisplayInfo(null, true, type: SystemType.Select , required:true)]
		public ProposalActivityActivityTypeEnum Type { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Comment { get; set; }

		[DisplayName("تاریخ میلادی فعالیت")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? MiladiDate { get; set; }
		[DisplayName("تاریخ شمسی فعالیت")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ShamsiDate { get; set; }

		[DisplayName("طول/مدت (دقیقه)")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long Duration { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProposalActivityStatusEnum Status { get; set; }

		[DisplayName("کامنت فعالیت های پروپوزال")]
		[DisplayInfo(null, true, type: SystemType.ListEntity )]
		public List<ProposalActivityComment> ProposalActivityComments { get; set; } = new();

	}
	[Display(Name = "کامنت فعالیت های پروپوزال")]
	[Table("ProposalActivityComment", Schema = "Epms")]

	public class ProposalActivityComment:BaseEntity
	{

		[DisplayName("فعالیت  پروپوزال")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public ProposalActivity ProposalActivity { get; set; }
		[DisplayName("شناسه فعالیت پروپوزال")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long ProposalActivityId { get; set; }

		[DisplayName("وضعیت سند")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public DocumentStatusEnums Status{ get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string Comment { get; set; }
	}

}
