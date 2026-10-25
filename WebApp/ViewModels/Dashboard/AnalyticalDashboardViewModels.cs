namespace WebApp.ViewModels.Dashboard
{
	public class ReadyForShippingFilter
	{
		public string? FromDateShamsi { get; set; }
		public string? ToDateShamsi { get; set; }
	}

	/// <summary>
	/// ردیف مشترک گزارش‌های قلم سفارش ساخت (آماده ارسال / در انتظار مهندسی)
	/// </summary>
	public class ProductionOrderItemDashboardRow
	{
		public long Id { get; set; }
		public string BranchTitle { get; set; } = string.Empty;
		public long? ProductionOrderNumber { get; set; }
		public string? CustomerName { get; set; }
		public string? PartCode { get; set; }
		public string? PartName { get; set; }
		public decimal? Amount { get; set; }
		public string? ProductionStepTitle { get; set; }
		public string? SendToIndustrialShamsiDate { get; set; }

		// آماده ارسال: تاریخ آماده سازی | در انتظار مهندسی: تاریخ تحویل توافقی
		public string? PreparationShamsiDate { get; set; }
		public string? AgreedDeliverShamsiDate { get; set; }

		// آماده ارسال: ماندگاری در انبار | در انتظار مهندسی: تاخیر در بارگذاری BOM
		public int? Days { get; set; }
	}

	/// <summary>
	/// ردیف مشترک گزارش‌های درخواست باز (بدون تایید مهندسی / متوقف شده)
	/// </summary>
	public class OpenOrderRequestDashboardRow
	{
		public long Id { get; set; }
		public long PurchaseRequestNumber { get; set; }
		public string? PurchaseRequestShamsiDate { get; set; }
		public string? PartCode { get; set; }
		public string? PartName { get; set; }
		public string? PartUnitTitle { get; set; }
		public int RequiredQty { get; set; }
		public string? OrderItemComment { get; set; }
		public string? NeedShamsiDate { get; set; }
		public int? ProductionOrderNumber { get; set; }
		public string BranchTitle { get; set; } = string.Empty;
		public string? SalesExpertName { get; set; }
		public string DlTitle { get; set; } = string.Empty;

		// فقط گزارش خریدهای متوقف شده
		public string? SupplyCommentShamsiDate { get; set; }
		public string? SupplyComment { get; set; }
		public string? StopStatusTitle { get; set; }

		// بدون تایید مهندسی: تاخیر در تایید درخواست | متوقف شده: تاخیر در تعیین تکلیف توقف
		public int? Days { get; set; }
	}

	public class AnalyticalDashboardSummaryItem
	{
		public string Key { get; set; } = string.Empty;
		public int Count { get; set; }
		public decimal? AverageDays { get; set; }
		public int? MaxDays { get; set; }
	}
}
