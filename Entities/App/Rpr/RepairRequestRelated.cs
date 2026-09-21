using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
	[Display(Name = "ایستگاه تعمیر")]
	[Table("Station", Schema = "Rpr")]
	public class RepairStation : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(128)]
		public string Title { get; set; } = "";

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class RepairStationConfiguration : IEntityTypeConfiguration<RepairStation>
	{
		public void Configure(EntityTypeBuilder<RepairStation> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_Station_HtsId");
		}
	}

	[Display(Name = "پیمانکار تعمیر")]
	[Table("Contractor", Schema = "Rpr")]
	public class RepairContractor : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(255)]
		public string Title { get; set; } = "";

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class RepairContractorConfiguration : IEntityTypeConfiguration<RepairContractor>
	{
		public void Configure(EntityTypeBuilder<RepairContractor> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_Contractor_HtsId");
		}
	}

	[Display(Name = "پیمانکار درخواست تعمیر")]
	[Table("RepairRequestContractor", Schema = "Rpr")]
	public class RepairRequestContractor : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual RepairRequest? RepairRequest { get; set; }
		public long? RepairRequestId { get; set; }

		[DisplayName("پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual RepairContractor? Contractor { get; set; }
		public long? ContractorId { get; set; }

		[DisplayName("عنوان پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(255)]
		public string? ContractorTitle { get; set; }

		[DisplayName("مبلغ تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? Cost { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("عنوان کالا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? PartTitle { get; set; }

		[DisplayName("تاریخ ارسال قطعه میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? SendPartMiladiDate { get; set; }

		[DisplayName("تاریخ ارسال قطعه شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? SendPartShamsiDate { get; set; }

		[DisplayName("تاریخ تایید میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? AcceptMiladiDate { get; set; }

		[DisplayName("تاریخ تایید شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? AcceptShamsiDate { get; set; }

		[DisplayName("تاریخ توافقی میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? AgreementMiladiDate { get; set; }

		[DisplayName("تاریخ توافقی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? AgreementShamsiDate { get; set; }

		[DisplayName("تاریخ برگشت میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ReturnMiladiDate { get; set; }

		[DisplayName("تاریخ برگشت شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ReturnShamsiDate { get; set; }

		[DisplayName("تاریخ اعلام هزینه میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? CostAnnouncementMiladiDate { get; set; }

		[DisplayName("تاریخ اعلام هزینه شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? CostAnnouncementShamsiDate { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }
	}

	public class RepairRequestContractorConfiguration : IEntityTypeConfiguration<RepairRequestContractor>
	{
		public void Configure(EntityTypeBuilder<RepairRequestContractor> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequestContractor_HtsId");
		}
	}

	[Display(Name = "قطعه داغی درخواست تعمیر")]
	[Table("RepairRequestUsedPart", Schema = "Rpr")]
	public class RepairRequestUsedPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual RepairRequest? RepairRequest { get; set; }
		public long? RepairRequestId { get; set; }

		[DisplayName("تفصیل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual DL? Dl { get; set; }
		public long? DlId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("واحد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual PartUnit? PartUnit { get; set; }
		public long? PartUnitId { get; set; }

		[DisplayName("قطعه آسیب‌دیده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool DamagedPart { get; set; }

		[DisplayName("نام انبار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? StockName { get; set; }

		[DisplayName("نام واحد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? UnitName { get; set; }

		[DisplayName("مقدار مصرف")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal UsedMount { get; set; }

		[DisplayName("مقدار برگشتی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal ReturnMount { get; set; }

		[DisplayName("قیمت خرید")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? BuyPrice { get; set; }

		[DisplayName("قیمت فروش")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? SalePrice { get; set; }

		[DisplayName("قیمت کل")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? TotalPrice { get; set; }

		[DisplayName("قیمت کل خرید")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? TotalBuyPrice { get; set; }

		[DisplayName("تاریخ درخواست از انبار میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? RequestFromStockMiladiDate { get; set; }

		[DisplayName("تاریخ درخواست از انبار شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(50)]
		public string? RequestFromStockShamsiDate { get; set; }

		[DisplayName("نسخه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Revision { get; set; }

		[DisplayName("شناسه قلم سند انبار همکاران")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HtsHamkaranInvVchItemId { get; set; }
	}

	public class RepairRequestUsedPartConfiguration : IEntityTypeConfiguration<RepairRequestUsedPart>
	{
		public void Configure(EntityTypeBuilder<RepairRequestUsedPart> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequestUsedPart_HtsId");
		}
	}
}
