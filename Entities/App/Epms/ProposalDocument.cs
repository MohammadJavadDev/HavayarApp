using Common.Attributes;
using Entities.App.Epms.Enums;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Epms
{
	[Display(Name = "مدارک پروپوزال")]
	[Table("ProposalDocument", Schema = "Epms")]
	public class ProposalDocument:BaseEntity
	{
		[DisplayName("پروپوزال")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]

		public Proposal Proposal { get; set; }
		 
		public long ProposalId { get; set; }
		 
		public long AttachmentId { get; set; }
		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public FileEntity Attachment { get; set; }
		[DisplayName("بازنگری")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Revision { get; set; } = 0;

		[DisplayName("نفر ساعت به دقیقه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int ConsumedManHours { get; set; }

		[DisplayName("آخرین وضعیت کامنت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProposalDocumentStatusEnum LastCommentStatus { get; set; } = ProposalDocumentStatusEnum.Submit;

		[DisplayName("کامنت ها")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<ProposalDocumentComment> ProposalDocumentComments { get; set; } = new();
	}
	[Display(Name = "مدارک پروپوزال")]
	[Table("ProposalDocumentComment", Schema = "Epms")]
	public class ProposalDocumentComment:BaseEntity
	{
		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string Comment { get; set; }
		public long AttachmentId { get; set; }
		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public FileEntity Attachment { get; set; }
		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProposalDocumentStatusEnum Status { get; set; } =ProposalDocumentStatusEnum.Submit;
	}
}
