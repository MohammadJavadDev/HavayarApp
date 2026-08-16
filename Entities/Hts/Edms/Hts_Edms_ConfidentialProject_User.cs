using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Edms
{
	[Table("Edms_ConfidentialProject_User")]
	public class Hts_Edms_ConfidentialProject_User
	{
		[Key]
		public int Id { get; set; }

		public int ProjectId { get; set; }

		public short UserId { get; set; }
	}
}
