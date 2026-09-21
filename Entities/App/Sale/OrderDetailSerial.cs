using Common.Attributes;
using Entities.App.Pln;
using Entities.App.SLS;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Entities.App.Sale;

[Display(Name = "جزئیات سریال سفارش فروش")]
[Table("OrderDetailSerial", Schema = "Sale")]
public class OrderDetailSerial : BaseEntity
{

	[DisplayName("سفارش مرتبط")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public virtual OrderDetail OrderDetail { get; set; }
	public long? OrderDetailId { get; set; }


	[DisplayName("سفارش مرتبط")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public virtual PlaningProject PlaningProject { get; set; }
	public long? PlaningProjectId { get; set; }


	[DisplayName("مشتری")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public virtual Customer Customer { get; set; }
	public long? CustomerId { get; set; }


	[DisplayName("آدرس مشتری")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	[ForeignKey(nameof(CustomerAddressId))]
	public virtual CustomerAddress? CustomerAddress { get; set; }
	public long? CustomerAddressId { get; set; }

	[DisplayName("شناسه HTS")]
	[DisplayInfo(null, false, type: SystemType.Long)]
	public long HtsId { get; set; }


	[DisplayName("سریال")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? Serial { get; set; }


	[DisplayName("مدل")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? Model { get; set; }


	[DisplayName("ساعت روزانه")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? HoursDaily { get; set; }


	[DisplayName("زمان اجرا")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? Runtime { get; set; }


	[DisplayName("زمان گارانتی")]
	[DisplayInfo(null, true, type: SystemType.Int)]
	public int? GuaranteeTime { get; set; }


	[DisplayName("روز ارسال گارانتی")]
	[DisplayInfo(null, true, type: SystemType.Int)]
	public int? GuaranteeSendDay { get; set; }


	[DisplayName("روز راه اندازی گارانتی")]
	[DisplayInfo(null, true, type: SystemType.Int)]
	public int? GuaranteeLaunchDay { get; set; }


	[DisplayName("دارای بیمه")]
	[DisplayInfo(null, true, type: SystemType.Boolean)]
	public bool? HasInsurance { get; set; }


	[DisplayName("تاریخ ارسال میلادی")]
	[DisplayInfo(null, true, type: SystemType.Date)]
	public DateTime? SendMiladiDate { get; set; } = null;


	[DisplayName("تاریخ ارسال شمسی")]
	[DisplayInfo(null, true, type: SystemType.DateShamsi)]
	public string? SendShamsiDate { get; set; } = null;


	[DisplayName("ساعت ارسال")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? SendTime { get; set; }


	[DisplayName("تاریخ راه اندازی میلادی")]
	[DisplayInfo(null, true, type: SystemType.Date)]
	public DateTime? LaunchMiladiDate { get; set; } = null;

	[DisplayName("تاریخ راه اندازی شمسی")]
	[DisplayInfo(null, true, type: SystemType.DateShamsi)]
	public string? LaunchShamsiDate { get; set; } = null;


	[DisplayName("تاریخ  خروج از کارخانه میلادی")]
	[DisplayInfo(null, true, type: SystemType.Date)]
	public DateTime? ExitFactoryMiladiDate { get; set; } = null;


	[DisplayName("تاریخ  خروج از کارخانه شمسی")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? ExitFactoryShamsiDate { get; set; } = null;


	[DisplayName("ساعت خروج از کارخانه")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? ExitFactoryTime { get; set; } = null;


	[DisplayName("شماره سفارش تولید")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? ProductionOrderNumber { get; set; } = null;


	[DisplayName("توضیحات صنایع")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? IndustrialComment { get; set; } = null;

	[DisplayName("عمر مفید")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? LifeTime { get; set; }

	[DisplayName("توضیحات انبار")]
	[DisplayInfo(null, true, type: SystemType.String)]
	[MaxLength(4000)]
	public string? InvComment { get; set; }

	[DisplayName("تایید صنایع")]
	[DisplayInfo(null, true, type: SystemType.Boolean)]
	public bool? IndustrialConfirm { get; set; }

	[DisplayName("تاریخ خروج نهایی میلادی")]
	[DisplayInfo(null, true, type: SystemType.Date)]
	public DateTime? FinalExitMiladiDate { get; set; }

	[DisplayName("تاریخ خروج نهایی شمسی")]
	[DisplayInfo(null, true, type: SystemType.DateShamsi)]
	public string? FinalExitShamsiDate { get; set; }

	[DisplayName("ساعت خروج نهایی")]
	[DisplayInfo(null, true, type: SystemType.String)]
	[MaxLength(5)]
	public string? ExitTimeFinal { get; set; }

}

public class OrderDetailSerialConfiguration : IEntityTypeConfiguration<OrderDetailSerial>
{
	public void Configure(EntityTypeBuilder<OrderDetailSerial> builder)
	{
		builder.HasOne(x => x.CustomerAddress)
			.WithMany()
			.HasForeignKey(x => x.CustomerAddressId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(x => x.HtsId)
			.IsUnique()
			.HasFilter("[HtsId] <> CAST(0 AS bigint)")
			.HasDatabaseName("IX_Sale_OrderDetailSerial_HtsId");
	}
}
