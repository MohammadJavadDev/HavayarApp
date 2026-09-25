using Common.Attributes;
using Entities.App.Hcm;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>نفرساعت تعمیرات CNG — معادل HTS <c>Cng_RepairsManHours</c>.</summary>
	[Display(Name = "نفرساعت تعمیرات")]
	[Table("RepairsManHours", Schema = "Cng")]
	public class RepairsManHours : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(RepairsId))]
		public virtual Repairs? Repairs { get; set; }
		public long RepairsId { get; set; }

		[DisplayName("پرسنل")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PersonelId))]
		public virtual Personel? Personel { get; set; }
		public long PersonelId { get; set; }

		[DisplayName("تاریخ کار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? WorkDate { get; set; }

		[DisplayName("ساعت شروع")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? StartTime { get; set; }

		[DisplayName("ساعت پایان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? EndTime { get; set; }

		[DisplayName("نرخ نفرساعت")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ManHourPrice { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }
	}

	public class RepairsManHoursConfiguration : IEntityTypeConfiguration<RepairsManHours>
	{
		public void Configure(EntityTypeBuilder<RepairsManHours> builder)
		{
			builder.HasOne(x => x.Repairs)
				.WithMany()
				.HasForeignKey(x => x.RepairsId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Personel)
				.WithMany()
				.HasForeignKey(x => x.PersonelId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_RepairsManHours_HtsId");
		}
	}
}
