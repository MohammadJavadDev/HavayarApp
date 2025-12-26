using Common.Attributes;
using Entities.App.Gnr.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Gnr
{
	[Display(Name = "شخص و شرکت")]
	[Table("Party", Schema = "Gnr")]
	public class Party : BaseEntity
	{
		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? FirstName { get; set; }


		[DisplayName("نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? LastName { get; set; }


		[DisplayName("نام کامل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(101)]
		public string? FullName { get; set; }


		[DisplayName("صفت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Alias { get; set; }


		[DisplayName("شناسه ملی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? NationalID { get; set; }


		[DisplayName("جنسیت")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PartyGenderEnum? Gender { get; set; }


		[DisplayName("ملیت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? Nationality { get; set; }


		[DisplayName("موبایل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? Mobile { get; set; }


		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Email { get; set; }


		[DisplayName("شماره شناسنامه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? IDNumber { get; set; }


		[DisplayName("سریال شناسنامه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? IDSerial { get; set; }


		[DisplayName("نام پدر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? FatherName { get; set; }


		[DisplayName("تاریخ تولد")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? BirthDate { get; set; }


		[DisplayName("کد اقتصادی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? EconomicCode { get; set; }


		[DisplayName("نام شرکت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? CompanyName { get; set; }


		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Number { get; set; }


		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public PartyTypeEnum Type { get; set; }


		[DisplayName("اسم به انگیلیسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? FirstNameInEnglish { get; set; }


		[DisplayName("نام خانوادگی به انگلیسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? LastNameInEnglish { get; set; }


		[DisplayName("نام شرکت به انگلیسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? CompanyNameInEnglish { get; set; }


		[DisplayName("تلفن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? Phone { get; set; }


		[DisplayName("نام کامل به انگیسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(101)]
		public string? FullNameInEnglish { get; set; }

		public long? HamkaranId { get; set; }

	}
}
