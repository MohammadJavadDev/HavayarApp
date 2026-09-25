using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Gnr
{
	/// <summary>اعلام ورود قطعه — معادل HTS <c>Gnr_PartEntryNotify</c> (حداقل برای دکمه تعمیرات CNG).</summary>
	[Display(Name = "اعلام ورود قطعه")]
	[Table("PartEntryNotify", Schema = "Gnr")]
	public class PartEntryNotify : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PartId))]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		public decimal Mount { get; set; }

		[DisplayName("عنوان کالا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? PartTitle { get; set; }

		[DisplayName("دسته قطعه")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? PartCategoryId { get; set; }

		[DisplayName("گروه قطعه")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? PartGroupId { get; set; }

		[DisplayName("عنوان تامین‌کننده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? SupplierTitle { get; set; }

		[DisplayName("واحد مالک")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? PartOwnerUnitId { get; set; }

		[DisplayName("مالک قطعه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? PartOwner { get; set; }

		[DisplayName("تاریخ تقریبی ورود میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date, required: true)]
		public DateTime ApproximateEntryMiladiDate { get; set; }

		[DisplayName("تاریخ تقریبی ورود")]
		[MaxLength(10)]
		[DisplayInfo("ApproximateEntryMiladiDate", true, type: SystemType.DateShamsi)]
		public string ApproximateEntryShamsiDate { get; set; } = "";

		[DisplayName("تاریخ ورود میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? EntryMiladiDate { get; set; }

		[DisplayName("تاریخ ورود")]
		[MaxLength(10)]
		[DisplayInfo("EntryMiladiDate", true, type: SystemType.DateShamsi)]
		public string? EntryShamsiDate { get; set; }

		[DisplayName("وسیله حمل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? TransportationVehicle { get; set; }

		[DisplayName("گیرنده")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? PartReceiverId { get; set; }

		[DisplayName("شماره رسید موقت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? TemporaryReceiptNumber { get; set; }

		[DisplayName("دریافت شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsReceived { get; set; }

		[DisplayName("شناسه TQ فروش")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? SaleTqId { get; set; }

		[DisplayName("آخرین وضعیت")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? LastStatusId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class PartEntryNotifyConfiguration : IEntityTypeConfiguration<PartEntryNotify>
	{
		public void Configure(EntityTypeBuilder<PartEntryNotify> builder)
		{
			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Gnr_PartEntryNotify_HtsId");
		}
	}
}
