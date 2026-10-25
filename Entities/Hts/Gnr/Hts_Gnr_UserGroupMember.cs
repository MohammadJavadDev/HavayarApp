using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Gnr
{
	[Table("Gnr_UserGroupMember")]
	public class Hts_Gnr_UserGroupMember
	{
		[Key]
		public int UserGroupMember_ID { get; set; }

		public short UserGroup_FK { get; set; }

		public short User_FK { get; set; }
	}
}
