using Common.Attributes;
using Entities.App.Sale;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Hrm
{
	[Display(Name = "واحد سازمانی")]
	[Table("OrgUnit", Schema = "Hrm")]
	public class OrgUnit:BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(500)]
		public string Title { get; set; }

		[DisplayName("شناسه والد")]
		[DisplayInfo(null, true, type: SystemType.Long)]
 
		public long? ParentId { get; set; }
		[DisplayName("شناسه توی همکاران")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long HamkaranUnitId { get; set; }
		[DisplayName("شناسه شعبه فروش")]
		[DisplayInfo(null, true, type: SystemType.Long)]

		public long? BranchId { get; set; }
		[DisplayName(" شعبه فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Branch? Branch { get; set; }
		[DisplayName("والد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public OrgUnit? Parent { get; set; }

	}
}
