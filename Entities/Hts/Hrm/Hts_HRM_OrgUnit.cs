using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Hrm
{
	[Table("HRM_OrgUnit")]
	public class Hts_HRM_OrgUnit
	{
		[Key]
		public short OrgUnit_ID { get; set; }

		[StringLength(480)]
		public string? OrgUnit_Title { get; set; }

		public int? Hamkaran_Unit_FK { get; set; }
	}
}
