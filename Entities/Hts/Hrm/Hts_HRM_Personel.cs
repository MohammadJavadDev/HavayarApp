using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Hrm
{
	[Table("HRM_Personel")]
	public class Hts_HRM_Personel
	{
		[Key]
		public short Personel_ID { get; set; }

		public int? Hamkaran_Personel_FK { get; set; }

		[StringLength(100)]
		public string? PrsName { get; set; }

		[StringLength(100)]
		public string? PrsFamily_Display { get; set; }
	}
}
