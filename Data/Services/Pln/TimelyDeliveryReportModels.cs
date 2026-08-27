using Entities.App.Pln.Enums;

namespace Data.Services.Pln;

public class TimelyDeliveryReportFilter
{
	/// <summary>تاریخ شمسی شروع بازه (روی تاریخ مجاز تحویل سفارش اعمال می‌شود)</summary>
	public string? FromDateShamsi { get; set; }

	/// <summary>تاریخ شمسی پایان بازه (روی تاریخ مجاز تحویل سفارش اعمال می‌شود)</summary>
	public string? ToDateShamsi { get; set; }

	/// <summary>در صورت مقدار داشتن، گزارش فقط برای همین شماره سفارش ساخت محاسبه می‌شود و بازه تاریخ نادیده گرفته می‌شود</summary>
	public long? ProductionOrderNumber { get; set; }

	/// <summary>دپارتمان فروش (Sale.Branch)</summary>
	public long? BranchId { get; set; }

	/// <summary>کارشناس فروش</summary>
	public long? SalesExpertId { get; set; }
}

/// <summary>گروه سازمانی عامل توقف</summary>
public enum DelayUnitGroup
{
	/// <summary>معاونت اجرایی</summary>
	Executive = 0,

	/// <summary>خارج معاونت اجرایی</summary>
	OutOfExecutive = 1
}

public class TimelyDeliveryReportResponse
{
	/// <summary>سهم هر عامل توقف از انحراف تحویل به‌موقع (درصد، میانگین روی سفارش‌های واجد شرایط)</summary>
	public List<TimelyDeliveryUnitDto> Units { get; set; } = [];

	/// <summary>مجموع درصد انحراف از تحویل به‌موقع کل</summary>
	public decimal TotalDeviationPercentage { get; set; }

	/// <summary>مجموع درصد انحراف واحدهای معاونت اجرایی</summary>
	public decimal ExecutiveSumPercentage { get; set; }

	/// <summary>مجموع درصد انحراف واحدهای خارج معاونت اجرایی</summary>
	public decimal OutOfExecutiveSumPercentage { get; set; }

	/// <summary>مجموع درصد انحراف واحدهای فروش (زیرمجموعه خارج معاونت اجرایی)</summary>
	public decimal SalesUnitsSumPercentage { get; set; }

	/// <summary>درصد انحراف بازرگانی</summary>
	public decimal CommercialPercentage { get; set; }

	/// <summary>درصد انحراف مالی</summary>
	public decimal FinancialPercentage { get; set; }

	/// <summary>درصد تحویل به‌موقع (۱۰۰ منهای انحراف کل)</summary>
	public decimal OnTimeDeliveryPercentage { get; set; }

	public int OrdersCount { get; set; }
	public int TimelyDeliveryOrdersCount { get; set; }
	public int DelayedOrdersCount { get; set; }
	public int InactiveOrdersCount { get; set; }

	/// <summary>شماره سفارش‌هایی که تاخیر شناسایی‌نشده دارند (به‌صورت رشته، مطابق گزارش قدیم)</summary>
	public string? UnidentifiedDelayProductionOrders { get; set; }

	/// <summary>روند ماهانه تحویل به‌موقع بر اساس ماه شمسی تاریخ مجاز تحویل</summary>
	public List<TimelyDeliveryMonthlyTrendDto> MonthlyTrend { get; set; } = [];

	/// <summary>سفارش‌های واجد شرایط دارای تاخیر، با جزئیات تاخیرهای ثبت‌شده</summary>
	public List<TimelyDeliveryOrderDto> Orders { get; set; } = [];

	/// <summary>سفارش‌هایی که تاخیر محاسبه‌شده‌شان بیش از تاخیر ثبت‌شده است</summary>
	public List<TimelyDeliveryUnregisteredOrderDto> UnregisteredOrders { get; set; } = [];
}

public class TimelyDeliveryUnitDto
{
	public DelayResponsibleEnum Code { get; set; }
	public string Title { get; set; } = string.Empty;
	public DelayUnitGroup Group { get; set; }

	/// <summary>واحد فروش است (زیرمجموعه خارج معاونت اجرایی)</summary>
	public bool IsSalesUnit { get; set; }

	public decimal Percentage { get; set; }
}

public class TimelyDeliveryMonthlyTrendDto
{
	/// <summary>ماه شمسی به شکل ۱۴۰۴/۰۱</summary>
	public string ShamsiMonth { get; set; } = string.Empty;

	public int OrdersCount { get; set; }
	public int DelayedOrdersCount { get; set; }
	public decimal OnTimePercentage { get; set; }
}

public class TimelyDeliveryOrderDto
{
	public long ProductionOrderId { get; set; }
	public long? ProductionOrderNumber { get; set; }
	public string? CustomerTitle { get; set; }
	public string? BranchTitle { get; set; }
	public string? SalesExpertName { get; set; }

	/// <summary>تاریخ مجاز تحویل قلم دارای بیشترین تاخیر</summary>
	public string? DeliverDateShamsi { get; set; }

	/// <summary>تاریخ آماده‌سازی همان قلم</summary>
	public string? PreparationDateShamsi { get; set; }

	/// <summary>نام کالای قلم دارای بیشترین تاخیر</summary>
	public string? PartName { get; set; }

	/// <summary>سریال قلم دارای بیشترین تاخیر</summary>
	public string? Serial { get; set; }

	public int MaxDelayDays { get; set; }

	/// <summary>مهلت توافقی (مخرج محاسبه درصد) به روز</summary>
	public int AgreedWindowDays { get; set; }

	public decimal TotalDeviationPercentage { get; set; }
	public int RegisteredDelayDays { get; set; }
	public int UnregisteredDelayDays { get; set; }
	public int MissingPreparationCount { get; set; }
	public int TotalItemsCount { get; set; }

	public List<TimelyDeliveryOrderDelayDto> Delays { get; set; } = [];
}

public class TimelyDeliveryOrderDelayDto
{
	public long? Id { get; set; }
	public DelayResponsibleEnum DelayResponsible { get; set; }
	public string DelayResponsibleTitle { get; set; } = string.Empty;
	public int? DelayDays { get; set; }
	public string? FromDateShamsi { get; set; }
	public string? ToDateShamsi { get; set; }
	public string? DelayReason { get; set; }
	public string? Description { get; set; }
	public string? CreatedByName { get; set; }
}

public class TimelyDeliveryUnregisteredOrderDto
{
	public long ProductionOrderId { get; set; }
	public long? ProductionOrderNumber { get; set; }
	public string? CustomerTitle { get; set; }
	public int MaxDelayDays { get; set; }
	public int RegisteredDelayDays { get; set; }
	public int UnregisteredDelayDays { get; set; }
}

public class ProductionOrderLookupDto
{
	public long Id { get; set; }
	public long? ProductionOrderNumber { get; set; }
	public string Text { get; set; } = string.Empty;
}
