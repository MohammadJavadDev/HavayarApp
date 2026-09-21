using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
	[Display(Name = "قطعه درخواست تعمیر")]
	[Table("RepairRequestPart", Schema = "Rpr")]
	public class RepairRequestPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual RepairRequest? RepairRequest { get; set; }
		public long? RepairRequestId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("واحد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual PartUnit? PartUnit { get; set; }
		public long? PartUnitId { get; set; }

		[DisplayName("تفصیل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual DL? Dl { get; set; }
		public long? DlId { get; set; }

		[DisplayName("داغی")]
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

		[DisplayName("تاریخ درخواست از انبار میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? RequestFromStockMiladiDate { get; set; }

		[DisplayName("تاریخ درخواست از انبار شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? RequestFromStockShamsiDate { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Description { get; set; }

		[DisplayName("توضیحات قطعه داغی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? ReplacementPartDescription { get; set; }

		[DisplayName("شناسه قلم درخواست قطعه HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HtsPartRequestItemId { get; set; }

		[DisplayName("شناسه قلم سند انبار همکاران")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int HtsHamkaranInvVchItemId { get; set; }
	}

	public class RepairRequestPartConfiguration : IEntityTypeConfiguration<RepairRequestPart>
	{
		public void Configure(EntityTypeBuilder<RepairRequestPart> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequestPart_HtsId");
		}
	}

	[Display(Name = "یادداشت درخواست تعمیر")]
	[Table("RepairRequestComment", Schema = "Rpr")]
	public class RepairRequestComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual RepairRequest? RepairRequest { get; set; }
		public long? RepairRequestId { get; set; }

		[DisplayName("متن")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		[DisplayName("هزینه")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? Cost { get; set; }
	}

	public class RepairRequestCommentConfiguration : IEntityTypeConfiguration<RepairRequestComment>
	{
		public void Configure(EntityTypeBuilder<RepairRequestComment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequestComment_HtsId");
		}
	}

	[Display(Name = "نفرساعت درخواست تعمیر")]
	[Table("RepairRequestManHour", Schema = "Rpr")]
	public class RepairRequestManHour : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual RepairRequest? RepairRequest { get; set; }
		public long? RepairRequestId { get; set; }

		[DisplayName("کارشناس")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual User? Expert { get; set; }
		public long? ExpertId { get; set; }

		[DisplayName("شناسه پرسنل HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsPersonelId { get; set; }

		[DisplayName("تاریخ کار میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? WorkMiladiDate { get; set; }

		[DisplayName("تاریخ کار شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? WorkShamsiDate { get; set; }

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

		[DisplayName("نرخ فروش نفرساعت")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ManHourSalePrice { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }
	}

	public class RepairRequestManHourConfiguration : IEntityTypeConfiguration<RepairRequestManHour>
	{
		public void Configure(EntityTypeBuilder<RepairRequestManHour> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequestManHour_HtsId");
		}
	}

	[Display(Name = "پیوست درخواست تعمیر")]
	[Table("RepairRequestAttachment", Schema = "Rpr")]
	public class RepairRequestAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست تعمیر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual RepairRequest? RepairRequest { get; set; }
		public long? RepairRequestId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Title { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class RepairRequestAttachmentConfiguration : IEntityTypeConfiguration<RepairRequestAttachment>
	{
		public void Configure(EntityTypeBuilder<RepairRequestAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequestAttachment_HtsId");
		}
	}

	[Display(Name = "شرح کار تعمیر")]
	[Table("RepairRequestWorkExplanation", Schema = "Rpr")]
	public class RepairRequestWorkExplanation : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(64)]
		public string Title { get; set; } = "";

		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(64)]
		public string? Code { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? Description { get; set; }
	}

	public class RepairRequestWorkExplanationConfiguration : IEntityTypeConfiguration<RepairRequestWorkExplanation>
	{
		public void Configure(EntityTypeBuilder<RepairRequestWorkExplanation> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequestWorkExplanation_HtsId");
		}
	}

	[Display(Name = "WBS نفرساعت تعمیر")]
	[Table("RepairRequestWbs", Schema = "Rpr")]
	public class RepairRequestWbs : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نفرساعت")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual RepairRequestManHour? RepairRequestManHour { get; set; }
		public long? RepairRequestManHourId { get; set; }

		[DisplayName("شرح کار")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual RepairRequestWorkExplanation? WorkExplanation { get; set; }
		public long? WorkExplanationId { get; set; }

		[DisplayName("نفرساعت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short ManHour { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Description { get; set; }
	}

	public class RepairRequestWbsConfiguration : IEntityTypeConfiguration<RepairRequestWbs>
	{
		public void Configure(EntityTypeBuilder<RepairRequestWbs> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequestWbs_HtsId");
		}
	}

	[Display(Name = "کسر قطعات تعمیر")]
	[Table("RepairRequestPartFraction", Schema = "Rpr")]
	public class RepairRequestPartFraction : BaseEntity
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
		[MaxLength(256)]
		public string? Description { get; set; }
	}

	public class RepairRequestPartFractionConfiguration : IEntityTypeConfiguration<RepairRequestPartFraction>
	{
		public void Configure(EntityTypeBuilder<RepairRequestPartFraction> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Rpr_RepairRequestPartFraction_HtsId");
		}
	}
}
