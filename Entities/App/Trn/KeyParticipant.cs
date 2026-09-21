using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "فرد کلیدی شرکت")]
	[Table("KeyParticipant", Schema = "Trn")]
	public class KeyParticipant : BaseEntity
	{
		public long CompanyId { get; set; }
		public virtual Company? Company { get; set; }

		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? FirstName { get; set; }

		[DisplayName("نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? LastName { get; set; }

		[DisplayName("سمت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Position { get; set; }

		[DisplayName("تلفن همراه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? Mobile { get; set; }

		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Email { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Description { get; set; }
	}
}
