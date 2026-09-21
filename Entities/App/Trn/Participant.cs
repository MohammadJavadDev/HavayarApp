using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Trn.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "مشتری حقیقی / شرکت‌کننده")]
	[Table("Participant", Schema = "Trn")]
	public class Participant : BaseEntity
	{
		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? FirstName { get; set; }

		[DisplayName("نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string? LastName { get; set; }

		[DisplayName("نام لاتین")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? FirstNameInLatin { get; set; }

		[DisplayName("نام خانوادگی لاتین")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? LastNameInLatin { get; set; }

		// Full name is a computed display value → derive in UI/query, NOT a stored column.

		[DisplayName("نام پدر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? FatherName { get; set; }

		[DisplayName("کد ملی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? NationalCode { get; set; }

		[DisplayName("شماره شناسنامه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? BirthCertificateNo { get; set; }

		[DisplayName("سال تولد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? BirthdayYear { get; set; }

		[DisplayName("محل صدور")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? IssuedPlace { get; set; }

		[DisplayName("جنسیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public GenderEnum? Gender { get; set; }

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

		[DisplayName("تلفن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? Phone { get; set; }

		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Email { get; set; }

		[DisplayName("کد پستی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? PostalCode { get; set; }

		[DisplayName("آدرس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Address { get; set; }

		[DisplayName("محل کار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Workplace { get; set; }

		[DisplayName("سمت شغلی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? JobPosition { get; set; }

		[DisplayName("شرکت (محل اشتغال)")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public long? CompanyId { get; set; }
		public virtual Company? Company { get; set; }


		public long? PhotoFileId { get; set; }
		[DisplayName("عکس")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public virtual FileEntity? PhotoFile { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Description { get; set; }

		// Optional link to the shared Party master (symmetric with Company.PartyId — per user decision).
		public long? PartyId { get; set; }
		public virtual Party? Party { get; set; }

		// Inverse navigations for the two enrollment tracks (Step 07) — used e.g. to limit
		// the CourseEvaluation respondent picker to participants enrolled in a course. No schema change.
		public virtual ICollection<CourseParticipant> CourseParticipants { get; set; } = new List<CourseParticipant>();
		public virtual ICollection<CourseCompanyParticipant> CourseCompanyParticipants { get; set; } = new List<CourseCompanyParticipant>();
	}
}
