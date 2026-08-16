using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Inv
{
	[Table("Inv_Part_Company")]
	public class Hts_Inv_Part_Company
	{
		[Key]
		public int Id { get; set; }

		public long PartId { get; set; }

		public int CompanyId { get; set; }
	}
}
