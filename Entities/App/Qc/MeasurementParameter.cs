using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Qc
{
	[Display(Name = "گروه بندی پارامترهای اندازه گیری")]
	[Table("MeasurementParameter", Schema = "Qc")]
	public class MeasurementParameter : BaseEntity
	{
		[DisplayName("گروه بندی پارامترهای اندازه گیری")]
		[MaxLength(2048)]
		[DisplayInfo("Title", true, type: SystemType.String, required: true, showInRelationData: true, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string Title { get; set; }


	}
}
