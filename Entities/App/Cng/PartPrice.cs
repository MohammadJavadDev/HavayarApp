using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>مدیریت قیمت قطعات CNG — معادل HTS <c>Sale_CngPartPrice</c>.</summary>
	[Display(Name = "مدیریت قیمت قطعات")]
	[Table("PartPrice", Schema = "Cng")]
	public class PartPrice : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PartId))]
		public virtual Part? Part { get; set; }
		public long PartId { get; set; }

		[DisplayName("قیمت")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long Price { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		[DisplayName("توضیحات (شخصی)")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Description { get; set; }

		[DisplayName("تاریخ بروزرسانی میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? UpdatedDate { get; set; }

		[DisplayName("تاریخ بروزرسانی")]
		[MaxLength(10)]
		[DisplayInfo("UpdatedDate", true, type: SystemType.DateShamsi)]
		public string? UpdatedDateShamsi { get; set; }

		[DisplayName("داخلی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsInternal { get; set; }

		[DisplayName("خارجی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsExternal { get; set; }
	}

	public class PartPriceConfiguration : IEntityTypeConfiguration<PartPrice>
	{
		public void Configure(EntityTypeBuilder<PartPrice> builder)
		{
			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_PartPrice_HtsId");
		}
	}
}
