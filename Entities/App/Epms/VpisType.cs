using Common.Attributes;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Epms
{
	[Display(Name = "نوع اسناد VPIS")]
	[Table("VpisType", Schema = "Epms")]
	public class VpisType:BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(500)]
		public string Title { get; set; }
	}
}
