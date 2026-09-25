using Common.Attributes;
using Entities.App.Hcm;
using Entities.App.Srv.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>درخواست پیک — معادل HTS <c>Srv_AmbassadorRequest</c>.</summary>
	[Display(Name = "درخواست پیک")]
	[Table("AmbassadorRequest", Schema = "Srv")]
	public class AmbassadorRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public AmbassadorRequestTypeEnum TypeId { get; set; }

		[DisplayName("نوع کار")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public AmbassadorRequestJobTypeEnum JobTypeId { get; set; }

		[DisplayName("تاریخ نیاز")]
		[DisplayInfo(null, true, type: SystemType.Date, required: true)]
		[Column(TypeName = "date")]
		public DateTime NeedDate { get; set; }

		[DisplayName("ساعت نیاز")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(5)]
		public string? NeedTime { get; set; }

		[DisplayName("تاریخ نیاز")]
		[DisplayInfo(null, false, type: SystemType.String, required: true)]
		[MaxLength(20)]
		public string? NeedDateInText { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? PackageDescription { get; set; }

		[DisplayName("پیک داخلی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(AmbassadorId))]
		public virtual Personel? Ambassador { get; set; }
		public long? AmbassadorId { get; set; }

		[DisplayName("پیک خارج از سازمان")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public OutdoorAmbassadorServiceEnum? OutdoorAmbassadorServiceId { get; set; }

		[DisplayName("مقصد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Destination { get; set; }

		[DisplayName("کامنت آخر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? LastComment { get; set; }

		/// <summary>
		/// وضعیت عددی HTS. مقدارهای تاریخی مجاز است؛ برای برچسب‌های شناخته‌شده از <see cref="AmbassadorRequestStatusEnum"/> استفاده کنید
		/// ولی این ستون را به آن enum نگاشت نکنید.
		/// </summary>
		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public short StatusId { get; set; }

		[DisplayName("مدت زمان درخواستی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		[Column(TypeName = "decimal(18,1)")]
		public decimal? WaitingTime { get; set; }

		[DisplayName("زمان انجام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? DoneDateTimeInText { get; set; }

		[DisplayName("تاریخ ایجاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? CreatedDateInText { get; set; }

		public virtual ICollection<AmbassadorRequestComment>? Comments { get; set; }
		public virtual ICollection<AmbassadorRequestAttachment>? Attachments { get; set; }
	}

	public class AmbassadorRequestConfiguration : IEntityTypeConfiguration<AmbassadorRequest>
	{
		public void Configure(EntityTypeBuilder<AmbassadorRequest> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_AmbassadorRequest_HtsId");

			builder.Property(x => x.WaitingTime).HasPrecision(18, 1);

			builder.HasOne(x => x.Ambassador)
				.WithMany()
				.HasForeignKey(x => x.AmbassadorId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
