using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Gnr
{
	[Table("Gnr_User")]
	public class Hts_Gnr_User
	{
		[Key]
		public short User_ID { get; set; }

		public short? Personel_FK { get; set; }

		[StringLength(160)]
		public string? Username { get; set; }

		[StringLength(1024)]
		public string? FullName { get; set; }
	}
}
