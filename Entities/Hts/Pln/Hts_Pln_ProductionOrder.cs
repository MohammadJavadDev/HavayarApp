using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Pln
{
	[Table("Pln_ProductionOrder")]
	public class Hts_Pln_ProductionOrder
	{
		[Key]
		public int Id { get; set; }

		public byte Revision { get; set; }

		public bool IsLatestVersion { get; set; }

		public int Number { get; set; }

		public long? RahkaranId { get; set; }

		public bool IsDisableForTimelyDeliveryReport { get; set; }

		[StringLength(1024)]
		public string? DisableForTimelyDeliveryReportComment { get; set; }
	}
}
