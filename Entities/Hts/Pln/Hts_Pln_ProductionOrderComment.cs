using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Pln
{
	[Table("Pln_ProductionOrderComment")]
	public class Hts_Pln_ProductionOrderComment
	{
		[Key]
		public int Id { get; set; }

		public int ProductionOrderId { get; set; }

		public short StatusId { get; set; }

		public short CreatedUserId { get; set; }

		public DateTime CreatedDate { get; set; }

		[StringLength(16)]
		public string? CreatedDateInText { get; set; }

		[StringLength(2048)]
		public string? Comment { get; set; }

		/// <summary>Populated via FromSqlRaw join (PO.Number).</summary>
		public int? ProductionOrderNumber { get; set; }

		/// <summary>Populated via FromSqlRaw join (Gnr_User.Username).</summary>
		public string? CreatedUserUsername { get; set; }
	}
}
