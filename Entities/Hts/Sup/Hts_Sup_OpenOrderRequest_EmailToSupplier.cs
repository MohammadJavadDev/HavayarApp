using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Sup
{
	[Table("Sup_OpenOrderRequest_EmailToSupplier")]
	public class Hts_Sup_OpenOrderRequest_EmailToSupplier
	{
		[Key]
		public int OpenOrderRequest_EmailToSupplier_ID { get; set; }

		public int OpenOrderRequest_FK { get; set; }

		public int Company_FK { get; set; }

		public int? Attachment_FK { get; set; }

		public int? PartAttachmentId { get; set; }

		public int? EdmsDocumentAttachmentId { get; set; }
	}
}
