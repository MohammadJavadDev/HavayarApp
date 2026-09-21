using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "نماینده / بازاریاب")]
	[Table("Agent", Schema = "Trn")]
	public class Agent : BaseEntity
	{
		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? FirstName { get; set; }

		[DisplayName("نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? LastName { get; set; }

		[DisplayName("منطقه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Zone { get; set; }

		[DisplayName("تلفن همراه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? Mobile { get; set; }

		[DisplayName("تلفن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? Phone { get; set; }

		[DisplayName("آدرس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Address { get; set; }
	}
}
