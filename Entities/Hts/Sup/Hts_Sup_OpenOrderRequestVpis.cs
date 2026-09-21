using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Sup
{
	[Table("Sup_OpenOrderRequestVpis")]
	public class Hts_Sup_OpenOrderRequestVpis
	{
		[Key]
		public int Id { get; set; }

		public int OpenOrderRequestId { get; set; }

		public int ProjectId { get; set; }

		public int ProjectVpisId { get; set; }

		public int? DocumentId { get; set; }

		public byte? RevisionNumber { get; set; }

		public bool IsLatest { get; set; }

		[StringLength(50)]
		public string? Comment { get; set; }
	}
}
