using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	[Display(Name = "قطعه")]
	[Table("Part", Schema = "Inv")]
	public class Part : BaseEntity
	{
		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		public string Name { get; set; }


		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Code { get; set; }


		[DisplayName("عنوان لاتین")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? LatinTitle { get; set; }


	}
}
