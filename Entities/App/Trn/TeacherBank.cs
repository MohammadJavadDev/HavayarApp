using Common.Attributes;
using Entities.App.Trn.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "بانک مدرسین")]
	[Table("TeacherBank", Schema = "Trn")]
	public class TeacherBank : BaseEntity
	{
		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? FirstName { get; set; }

		[DisplayName("نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? LastName { get; set; }

		[DisplayName("تحصیلات")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public EducationEnum? Education { get; set; }

		[DisplayName("رشته تحصیلی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? FieldOfStudy { get; set; }

		[DisplayName("تلفن همراه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? Mobile { get; set; }

		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Email { get; set; }

		[DisplayName("کد ملی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? NationalCode { get; set; }

		[DisplayName("هوایاری هست یا خیر")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsHavayarPerson { get; set; }

		[DisplayName("حق الزحمه (روزانه)")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? DailyWages { get; set; }

		[DisplayName("آدرس")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(512)]
		public string? Address { get; set; }

		[DisplayName("زمینه تدریس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? TeachingField { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Description { get; set; }

		public virtual ICollection<TeacherBankAttachment> Attachments { get; set; } = new List<TeacherBankAttachment>();
	}
}
