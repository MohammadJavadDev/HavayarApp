using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Hts.Inv
{
	[Table("Inv_Part_Attachment_Permission")]
	public class Hts_Inv_Part_Attachment_Permission
    {

		[Key]
		public int Part_Attachment_Permission_ID { get; set; }

		public int Part_Attachment_FK { get; set; }

		public short OrgUnit_FK { get; set; }
	}
}
