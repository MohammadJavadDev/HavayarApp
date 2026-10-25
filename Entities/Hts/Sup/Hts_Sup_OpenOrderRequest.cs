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

		public bool? Engineering_Accept { get; set; }

		public short? Engineering_Accept_User_FK { get; set; }

		[StringLength(10)]
		public string? Engineering_Accept_Date { get; set; }

		[StringLength(5)]
		public string? Engineering_Accept_Time { get; set; }

		public DateTime? EngineeringAcceptDateInEurope { get; set; }

		public bool IsAcceptedAutomaticallyByEngineering { get; set; }
	}
}
