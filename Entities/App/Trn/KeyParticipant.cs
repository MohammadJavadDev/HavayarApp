using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "مدیران و افراد کلیدی")]
	[Table("KeyParticipant", Schema = "Trn")]
	public class KeyParticipant : BaseEntity
	{
		public long CompanyId { get; set; }
		public virtual Company? Company { get; set; }

		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? FirstName { get; set; }

		[DisplayName("نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? LastName { get; set; }

		[DisplayName("سمت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? Position { get; set; }

		[DisplayName("تلفن مستقیم")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? DirectPhoneNumber { get; set; }

		[DisplayName("شماره تماس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? PhoneNumber { get; set; }

		[DisplayName("شماره موبایل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? MobileNumber { get; set; }

		[DisplayName("فکس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? FaxNumber { get; set; }

		[DisplayName("تاییدیه فکس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? ConfirmedFaxNumber { get; set; }

		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Email { get; set; }
	}
}
