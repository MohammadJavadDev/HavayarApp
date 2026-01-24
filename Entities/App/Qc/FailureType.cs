using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Qc
{
	[Display(Name = "نوع خرابی")]
	[Table("FailureType", Schema = "Qc")]
	public class FailureType : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo("Title", true, type: SystemType.String, required: true, showInRelationData: true, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string Title { get; set; }


	}
}
