using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sec
{
	/// <summary>نوع ایمیل دبیرخانه — معادل HTS <c>Sec_EmailType</c> (1=تولد، 2=سالروز حضور).</summary>
	[Display(Name = "نوع ایمیل")]
	[Table("EmailType", Schema = "Sec")]
	public class EmailType : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(160)]
		public string? EmailTypeTitle { get; set; }
	}
}
