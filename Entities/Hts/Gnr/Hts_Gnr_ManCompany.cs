using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Gnr
{
	[Table("Gnr_ManCompany")]
	public class Hts_Gnr_ManCompany
	{
		[Key]
		public int ManCompany_ID { get; set; }

		public int Hamkaran_ManCompany_FK { get; set; }

		[StringLength(256)]
		public string? CompanyName { get; set; }

		[StringLength(512)]
		public string? FullName { get; set; }
	}
}
