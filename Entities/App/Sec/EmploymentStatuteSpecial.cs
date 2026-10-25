using Common.Attributes;
using Entities.App.Hcm;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sec
{
	/// <summary>لیست استثنائات سابقه حضور — معادل HTS <c>HRM_EmplymentStatuteSpecialList</c>.</summary>
	[Display(Name = "لیست استثنائات سابقه حضور")]
	[Table("EmploymentStatuteSpecial", Schema = "Sec")]
	public class EmploymentStatuteSpecial : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرسنل")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PersonelId))]
		public Personel? Personel { get; set; }

		public long PersonelId { get; set; }

		[DisplayName("تاریخ میلادی استخدام واقعی")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? RealEmploymentMiladiDate { get; set; }

		[DisplayName("تاریخ استخدام واقعی")]
		[MaxLength(10)]
		[DisplayInfo(nameof(RealEmploymentMiladiDate), true, SystemType.DateShamsi)]
		public string? RealEmploymentShamsiDate { get; set; }

		[DisplayName("میزان افزایش سال")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public byte? AdditionalYear { get; set; }

		[DisplayName("میزان افزایش ماه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public byte? AdditionalMonth { get; set; }
	}

	public class EmploymentStatuteSpecialConfiguration : IEntityTypeConfiguration<EmploymentStatuteSpecial>
	{
		public void Configure(EntityTypeBuilder<EmploymentStatuteSpecial> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sec_EmploymentStatuteSpecial_HtsId");
			builder.HasIndex(x => x.PersonelId).HasDatabaseName("IX_Sec_EmploymentStatuteSpecial_PersonelId");
			builder.HasOne(x => x.Personel)
				.WithMany()
				.HasForeignKey(x => x.PersonelId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
