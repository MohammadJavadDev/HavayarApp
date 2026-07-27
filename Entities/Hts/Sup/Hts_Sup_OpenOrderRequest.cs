using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Sup
{
	[Table("Sup_OpenOrderRequest")]
	public class Hts_Sup_OpenOrderRequest
	{
		[Key]
		public int OpenOrderRequest_ID { get; set; }

		public long PurchaseRequestItemId { get; set; }

		public long? OrderRowId { get; set; }

		public bool IsDeleted { get; set; }
	}
}
