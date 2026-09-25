using Common.Attributes;
using Entities.App.Gnr;
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
	/// <summary>درخواست خودرو — معادل HTS <c>Srv_CarRequest</c>.</summary>
	[Display(Name = "درخواست خودرو")]
	[Table("CarRequest", Schema = "Srv")]
	public class CarRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public CarRequestTypeEnum TypeId { get; set; }

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

		[DisplayName("مسافر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PassengerId))]
		public virtual Personel? Passenger { get; set; }
		public long? PassengerId { get; set; }

		[DisplayName("تعداد نفرات")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? CountPeople { get; set; }

		[DisplayName("مرکز هزینه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(CostCenterId))]
		public virtual CostCenter? CostCenter { get; set; }
		public long? CostCenterId { get; set; }

		[DisplayName("راننده داخلی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(DriverId))]
		public virtual Personel? Driver { get; set; }
		public long? DriverId { get; set; }

		[DisplayName("راننده خارجی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public OutdoorCarServiceEnum? OutdoorCarServiceId { get; set; }

		[DisplayName("مقصد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? Destination { get; set; }

		[DisplayName("کامنت آخر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? LastComment { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Description { get; set; }

		/// <summary>
		/// وضعیت عددی HTS. مقدار 0 تاریخی مجاز است؛ برای برچسب‌های شناخته‌شده از <see cref="CarRequestStatusEnum"/> استفاده کنید
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

		[DisplayName("محل سرویس دهی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CarRequestBranchEnum? BranchId { get; set; }

		[DisplayName("تاریخ ایجاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? CreatedDateInText { get; set; }

		public virtual ICollection<CarRequestComment>? Comments { get; set; }
		public virtual ICollection<CarRequestAttachment>? Attachments { get; set; }
	}

	public class CarRequestConfiguration : IEntityTypeConfiguration<CarRequest>
	{
		public void Configure(EntityTypeBuilder<CarRequest> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_CarRequest_HtsId");

			builder.Property(x => x.WaitingTime).HasPrecision(18, 1);

			builder.HasOne(x => x.Passenger)
				.WithMany()
				.HasForeignKey(x => x.PassengerId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Driver)
				.WithMany()
				.HasForeignKey(x => x.DriverId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.CostCenter)
				.WithMany()
				.HasForeignKey(x => x.CostCenterId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
