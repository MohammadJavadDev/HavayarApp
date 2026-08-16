using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Edms
{
	[Table("Edms_Project_Progress")]
	public class Hts_Edms_Project_Progress
	{
		[Key]
		public int Project_Progress_ID { get; set; }

		public int Project_FK { get; set; }

		public byte Document_Status_FK { get; set; }

		public byte Progress_Percentage { get; set; }
	}
}
