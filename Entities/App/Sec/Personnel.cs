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
	/// <summary>پرسنل دبیرخانه — معادل HTS <c>Sec_Personnel</c>. فیلد <c>SendToEveryOne</c> با <see cref="ReciversGroupId"/> جایگزین شده است.</summary>
	[Display(Name = "پرسنل دبیرخانه")]
	[Table("Personnel", Schema = "Sec")]
	public class Personnel : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرسنل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PersonelId))]
		public Personel? Personel { get; set; }

		/// <summary>ممکن است null باشد وقتی ردیف HTS متناظر در Hcm.Personel ندارد.</summary>
		public long? PersonelId { get; set; }

		[DisplayName("نام نمایشی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(160)]
		public string? NameDisplay { get; set; }

		[DisplayName("نام‌خانوادگی نمایشی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(400)]
		public string? FamilyDisplay { get; set; }

		/// <summary>تاریخ تولد شمسی به‌صورت رشته (مثلاً 1370/01/15) — مثل HTS.</summary>
		[DisplayName("تاریخ تولد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(80)]
		public string? BirthDate { get; set; }

		/// <summary>تاریخ شروع به کار شمسی به‌صورت رشته — مثل HTS.</summary>
		[DisplayName("تاریخ شروع به کار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(80)]
		public string? EmploymentDate { get; set; }

		[DisplayName("تصویر تولد")]
		[DisplayInfo(null, false, type: SystemType.File)]
		public byte[]? Picture { get; set; }

		[DisplayName("تصویر سالروز حضور")]
		[DisplayInfo(null, false, type: SystemType.File)]
		public byte[]? SecondPicture { get; set; }

		[DisplayName("اجبار ارسال")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? ForceToSend { get; set; }

		[DisplayName("گیرنده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ReciversGroupId))]
		public ReciversGroup? ReciversGroup { get; set; }

		public long? ReciversGroupId { get; set; }

		[NotMapped]
		public string? PictureBase64 { get; set; }

		[NotMapped]
		public bool RemovePicture { get; set; }

		[NotMapped]
		public string? SecondPictureBase64 { get; set; }

		[NotMapped]
		public bool RemoveSecondPicture { get; set; }

		[NotMapped]
		public bool HasPicture => Picture != null && Picture.Length > 0;

		[NotMapped]
		public bool HasSecondPicture => SecondPicture != null && SecondPicture.Length > 0;
	}

	public class PersonnelConfiguration : IEntityTypeConfiguration<Personnel>
	{
		public void Configure(EntityTypeBuilder<Personnel> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sec_Personnel_HtsId");
			builder.HasIndex(x => x.PersonelId).HasDatabaseName("IX_Sec_Personnel_PersonelId");
			builder.Ignore(x => x.PictureBase64);
			builder.Ignore(x => x.RemovePicture);
			builder.Ignore(x => x.SecondPictureBase64);
			builder.Ignore(x => x.RemoveSecondPicture);
			builder.Ignore(x => x.HasPicture);
			builder.Ignore(x => x.HasSecondPicture);
		}
	}
}
