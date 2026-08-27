using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Edms
{
	[Table("Edms_Project_Vpis_Responsible")]
	public class Hts_Edms_Project_Vpis_Responsible
	{
		[Key]
		public int Project_Vpis_Responsible_ID { get; set; }

		public int Project_Vpis_FK { get; set; }

		public short Responsible_FK { get; set; }

		public bool? IsMain { get; set; }
	}
}
