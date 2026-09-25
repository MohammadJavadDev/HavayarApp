using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hcm
{
	[Display(Name = "پرسنل")]
	[Table("Personel", Schema = "Hcm")]
	public class Personel : BaseEntity
	{
		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? Code { get; set; }


		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(40)]
		public string? Name { get; set; }


		[DisplayName("نام خانواده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? Family { get; set; }


		[DisplayName("نمایش نام خانواده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(400)]
		public string? FamilyDisplay { get; set; }


		[DisplayName("نام انگلیسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(160)]
		public string? NameEn { get; set; }


		[DisplayName("نام خانوادگی انگلیسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(400)]
		public string? FamilyEn { get; set; }


		[DisplayName("نام پدر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? FatherName { get; set; }


		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? Email { get; set; }


		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public OrgUnit? OrgUnit { get; set; }

		public long? OrgUnitId { get; set; }


		[DisplayName("موبایل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(400)]
		public string? Mobile { get; set; }


		[DisplayName("کد شعبه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? BranchCode { get; set; }


		[DisplayName("تاریخ تولد")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? BirthDate { get; set; }


		[DisplayName("تاریخ تولد واقعی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? RealyBirthDate { get; set; }


		[DisplayName("تاریخ استخدام")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? EmploymentDate { get; set; }


		[DisplayName("کد جنسیت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? GenderCode { get; set; }


		[DisplayName("شماره ملی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? NationalID { get; set; }


		[DisplayName("شماره بیمه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? InsuranceNo { get; set; }


		[DisplayName("شماره شناسنامه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? IDNumber { get; set; }


		[DisplayName("سال تجربه کاری خارج از هوایار")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? WorkExperienceOutOfHavayarYear { get; set; }


		[DisplayName("ماه‌های تجربه کاری خارج از هوایار")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? WorkExperienceOutOfHavayarMonth { get; set; }


		[DisplayName("رشته تحصیلی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? FieldOfStudy { get; set; }


		[DisplayName("مدرک تحصیلی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? DegreeOfEducation { get; set; }


		[DisplayName("شخص")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? Party { get; set; }

		public long? PartyId { get; set; }

		public long? HamkaranId { get; set; }

		[DisplayName("شغل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(JobId))]
		public virtual Job? Job { get; set; }

		public long? JobId { get; set; }

	}

	public class PersonelConfiguration : IEntityTypeConfiguration<Personel>
	{
		public void Configure(EntityTypeBuilder<Personel> builder)
		{
			builder.HasOne(x => x.Job)
				.WithMany()
				.HasForeignKey(x => x.JobId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
