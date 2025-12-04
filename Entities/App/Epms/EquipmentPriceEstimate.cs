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
	[Display(Name = "برآورد هزینه تجهیزات")]
	[Table("EquipmentPriceEstimate", Schema = "Epms")]
	public class EquipmentPriceEstimate:BaseEntity
	{
		[DisplayName("پروپوزال")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]

		public Proposal Proposal { get; set; }

		public long ProposalId { get; set; }

		public long PriceInquiryId { get; set; }
		[DisplayName("استعلام قیمت")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public FileEntity PriceInquiry { get; set; }

		public long BomId { get; set; }
		[DisplayName("Bom")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public FileEntity Bom { get; set; }

		[DisplayName("بازنگری")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Revision { get; set; } = 0;

		[DisplayName("نفر ساعت به دقیقه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int ConsumedManHours { get; set; }
		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string Comment { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public EquipmentPriceEstimateStatusEnum Status { get; set; }

	}
}
