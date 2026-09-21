using Common.Attributes;
using Entities.App.Crm;
using Entities.App.Inv;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "کاردکس قطعه نمایندگی")]
	[Table("AgencyPartCardex", Schema = "Sale")]
	public class AgencyPartCardex : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نمایندگی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Customer? Agency { get; set; }
		public long? AgencyId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal Amount { get; set; }

		[DisplayName("شناسه سند انبار HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsInvVoucherId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class AgencyPartCardexConfiguration : IEntityTypeConfiguration<AgencyPartCardex>
	{
		public void Configure(EntityTypeBuilder<AgencyPartCardex> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_AgencyPartCardex_HtsId");
		}
	}

	[Display(Name = "کارتابل نمایندگی")]
	[Table("AgencyCartable", Schema = "Sale")]
	public class AgencyCartable : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مسئول")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(ResponsibleId))]
		public virtual User? Responsible { get; set; }
		public long? ResponsibleId { get; set; }

		[DisplayName("مسئول پشتیبانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(SupportResponsibleId))]
		public virtual User? SupportResponsible { get; set; }
		public long? SupportResponsibleId { get; set; }

		[DisplayName("تاریخ درخواست میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? RequestMiladiDate { get; set; }

		[DisplayName("تاریخ درخواست شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? RequestShamsiDate { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("عنوان مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? CustomerTitle { get; set; }

		[DisplayName("منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Zone? Zone { get; set; }
		public long? ZoneId { get; set; }

		[DisplayName("شرح درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? RequestComment { get; set; }

		[DisplayName("گارانتی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasGuarantee { get; set; }

		[DisplayName("انجام شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsDone { get; set; }

		[DisplayName("درخواست پشتیبانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual ServiceRequest? ServiceRequest { get; set; }
		public long? ServiceRequestId { get; set; }
	}

	public class AgencyCartableConfiguration : IEntityTypeConfiguration<AgencyCartable>
	{
		public void Configure(EntityTypeBuilder<AgencyCartable> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_AgencyCartable_HtsId");
		}
	}
}
