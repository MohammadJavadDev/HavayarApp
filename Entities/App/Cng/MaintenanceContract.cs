using Common.Attributes;
using Entities.App.Cng.Enums;
using Entities.App.SLS;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>قرارداد تعمیر و نگهداشت CNG — معادل HTS <c>Cng_MaintenanceContract</c>.</summary>
	[Display(Name = "قرارداد تعمیر و نگهداشت")]
	[Table("MaintenanceContract", Schema = "Cng")]
	public class MaintenanceContract : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نمایندگی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(AgencyId))]
		public virtual Customer? Agency { get; set; }
		public long AgencyId { get; set; }

		[DisplayName("جایگاه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(StationId))]
		public virtual StationInfo? Station { get; set; }
		public long? StationId { get; set; }

		[DisplayName("نام بهره‌بردار")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(StationBeneficiaryId))]
		public virtual Customer? StationBeneficiary { get; set; }
		public long? StationBeneficiaryId { get; set; }

		[DisplayName("تاریخ شروع قرارداد میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? StartDate { get; set; }

		[DisplayName("تاریخ شروع قرارداد")]
		[MaxLength(10)]
		[DisplayInfo("StartDate", true, type: SystemType.DateShamsi)]
		public string? StartDateInText { get; set; }

		[DisplayName("تاریخ اتمام قرارداد میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? EndDate { get; set; }

		[DisplayName("تاریخ اتمام قرارداد")]
		[MaxLength(10)]
		[DisplayInfo("EndDate", true, type: SystemType.DateShamsi)]
		public string? EndDateInText { get; set; }

		[DisplayName("تعداد بازدید در ماه")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public byte VisitCountInMonth { get; set; }

		[DisplayName("مدت قرارداد (ماه)")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public byte ContractDurationInMonth { get; set; }

		[DisplayName("قیمت ماهانه")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long MonthlyPrice { get; set; }

		[DisplayName("مجموع قیمت")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long TotalPrice { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public MaintenanceContractStatusEnum Status { get; set; }

		[DisplayName("فسخ شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsCanceled { get; set; }

		[DisplayName("توضیحات فسخ قرارداد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? CancelDescription { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class MaintenanceContractConfiguration : IEntityTypeConfiguration<MaintenanceContract>
	{
		public void Configure(EntityTypeBuilder<MaintenanceContract> builder)
		{
			builder.HasOne(x => x.Agency)
				.WithMany()
				.HasForeignKey(x => x.AgencyId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Station)
				.WithMany()
				.HasForeignKey(x => x.StationId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.StationBeneficiary)
				.WithMany()
				.HasForeignKey(x => x.StationBeneficiaryId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_MaintenanceContract_HtsId");
		}
	}
}
