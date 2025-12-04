using Common.Attributes;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Gnr
{

	[Display(Name = "صنعت شخص/شرکت")]
	[Table("PartyIndustry", Schema = "Gnr")]
	public class PartyIndustry:BaseEntity
	{

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(500)]
		public string Title { get; set; }

		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.Long )]
 
		public long Code { get; set; }

		[DisplayName("شناسه والد")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ParentId { get; set; }

		[DisplayName("والد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PartyIndustry? Parent { get; set; }
		
	}


}
