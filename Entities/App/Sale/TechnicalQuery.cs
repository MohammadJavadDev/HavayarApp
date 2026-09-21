using Common.Attributes;
using Entities.App.Gnr;
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
	[Display(Name = "پرسش فنی")]
	[Table("TechnicalQuery", Schema = "Sale")]
	public class TechnicalQuery : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("وضعیت کارتابل")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? CartableStatusId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("سریال")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Serial { get; set; }

		[DisplayName("استان")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Region? Province { get; set; }
		public long? ProvinceId { get; set; }

		[DisplayName("شرح مشکل فنی")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(512)]
		public string? TechnicalProblemDescription { get; set; }

		[DisplayName("تاریخ نصب میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? InstallationMiladiDate { get; set; }

		[DisplayName("تاریخ نصب شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? InstallationShamsiDate { get; set; }

		[DisplayName("اولین بروز مشکل میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? FirstEmergingProblemMiladiDate { get; set; }

		[DisplayName("اولین بروز مشکل شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? FirstEmergingProblemShamsiDate { get; set; }

		[DisplayName("آخرین بروز مشکل میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? LastEmergingProblemMiladiDate { get; set; }

		[DisplayName("آخرین بروز مشکل شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? LastEmergingProblemShamsiDate { get; set; }

		[DisplayName("تعداد بروز مشکل")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? NumberOfEmergedProblem { get; set; }

		[DisplayName("در کارتابل تایید")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool InApproveQueue { get; set; }
	}

	public class TechnicalQueryConfiguration : IEntityTypeConfiguration<TechnicalQuery>
	{
		public void Configure(EntityTypeBuilder<TechnicalQuery> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_TechnicalQuery_HtsId");
		}
	}

	[Display(Name = "یادداشت پرسش فنی")]
	[Table("TechnicalQueryComment", Schema = "Sale")]
	public class TechnicalQueryComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? TechnicalQueryId { get; set; }
		public virtual TechnicalQuery? TechnicalQuery { get; set; }

		[DisplayName("متن")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		[DisplayName("کاربر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual User? User { get; set; }
		public long? UserId { get; set; }
	}
}
