using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sec
{
	/// <summary>گروه گیرندگان ایمیل دبیرخانه — معادل HTS <c>Sec_ReciversGroup</c>.</summary>
	[Display(Name = "گروه گیرندگان")]
	[Table("ReciversGroup", Schema = "Sec")]
	public class ReciversGroup : BaseEntity
	{
		[DisplayName("آدرس ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(160)]
		public string? ReciversEmails { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(160)]
		public string? ReciversTitle { get; set; }
	}
}
