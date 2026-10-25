using Common.Utilities;
using Data.Services.Pln;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.App.Rpr;
using Entities.App.Rpr.Enums;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.Sup;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Data.Services.Dashboard;

/// <summary>
/// داشبورد نبض هیئت‌مدیره — مانده‌های لحظه‌ای + درصد تحویل به‌موقع از سرویس موجود.
/// اعداد هر بار از دیتابیس خوانده می‌شوند و در کد ثابت نیستند.
/// </summary>
public sealed class BoardPulseDashboardService(
	ApplicationDbContext db,
	ITimelyDeliveryReportService timelyDeliveryReportService) : IBoardPulseDashboardService
{
	public async Task<BoardPulseDashboardResponse> GetReportAsync(CancellationToken cancellationToken = default)
	{
		var now = DateTime.Now;
		var response = new BoardPulseDashboardResponse
		{
			AsOfShamsiDateTime = now.ToShamsiDateTime(),
			SnapshotLabel = "وضعیت همین لحظه"
		};

		var openProductionCount = await db.Set<ProductionOrderItem>().AsNoTracking()
			.CountAsync(i =>
				i.IsLatestVersion &&
				!i.IsDeleted &&
				i.Status == ProductionOrderItemStatusEnum.NotCompleted,
				cancellationToken);

		var openPurchase = await db.Set<OpenOrderRequest>().AsNoTracking()
			.CountAsync(o => !o.IsDeleted, cancellationToken);

		var stoppedPurchase = await db.Set<OpenOrderRequest>().AsNoTracking()
			.CountAsync(o => !o.IsDeleted && o.IsStop, cancellationToken);

		var edmsInProcessCount = await db.Set<Project>().AsNoTracking()
			.CountAsync(p => p.Status == ProjectStatusEnum.InProcess, cancellationToken);

		var openRepairsCount = await db.Set<RepairRequest>().AsNoTracking()
			.CountAsync(r =>
				r.Status == null ||
				(r.Status != RepairRequestStatusEnum.Completed &&
				 r.Status != RepairRequestStatusEnum.Unrepairable &&
				 r.Status != RepairRequestStatusEnum.CustomerRejected &&
				 r.Status != RepairRequestStatusEnum.UnrealItem),
				cancellationToken);

		var waitingDispatchCount = await db.Set<ServiceRequest>().AsNoTracking()
			.CountAsync(s => s.Unreal == false && s.IsWaitingToSend == true, cancellationToken);

		var activePurchase = openPurchase - stoppedPurchase;

		response.OpenProductionItems = new BoardPulseMetricDto
		{
			Title = "اقلام ساخت باز",
			Definition = "آخرین نسخهٔ اقلام سفارش ساخت حذف‌نشده با وضعیت تکمیل‌نشده.",
			Count = openProductionCount
		};

		response.OpenPurchaseRequests = new BoardPulsePurchaseMetricDto
		{
			Title = "درخواست خرید باز",
			Definition = "درخواست‌های خرید حذف‌نشده؛ متوقف زیرمجموعهٔ همین جمعیت باز است.",
			Count = openPurchase,
			StoppedCount = stoppedPurchase
		};

		response.EdmsProjectsInProcess = new BoardPulseMetricDto
		{
			Title = "پروژه‌های مهندسی در جریان",
			Definition = "پروژه‌های مهندسی با وضعیت در جریان.",
			Count = edmsInProcessCount
		};

		response.OpenRepairs = new BoardPulseMetricDto
		{
			Title = "تعمیرات باز",
			Definition = "درخواست تعمیر با وضعیت خالی یا غیر از تکمیل‌شده، غیرقابل‌تعمیر، عدم‌تایید مشتری و آیتم صوری.",
			Count = openRepairsCount
		};

		response.ServiceRequestsWaitingToDispatch = new BoardPulseMetricDto
		{
			Title = "پشتیبانی منتظر اعزام",
			Definition = "درخواست‌های پشتیبانی واقعی که پرچم منتظر اعزام دارند؛ کل ردیف‌ها به‌عنوان مانده نمایش داده نمی‌شود.",
			Count = waitingDispatchCount
		};

		response.OpenPurchaseSlices =
		[
			new BoardPulseSliceDto { Label = "فعال", Count = activePurchase },
			new BoardPulseSliceDto { Label = "متوقف", Count = stoppedPurchase }
		];

		var timely = await timelyDeliveryReportService.GetReportAsync(
			new TimelyDeliveryReportFilter(),
			cancellationToken);

		response.OnTimeDelivery = new BoardPulseOnTimeDeliveryDto
		{
			Title = "درصد تحویل به‌موقع",
			Definition = "درصد تحویل به‌موقع از همان سرویس گزارش برنامه‌ریزی، با فیلتر پیش‌فرض آن گزارش.",
			OnTimeDeliveryPercentage = timely.OnTimeDeliveryPercentage,
			FilterSentence = BuildTimelyDeliveryDefaultFilterSentence()
		};

		return response;
	}

	/// <summary>
	/// جملهٔ فیلتر مطابق ResolveDateRange سرویس تحویل به‌موقع وقتی فیلتر خالی است:
	/// از ابتدای سال شمسی جاری تا امروز.
	/// </summary>
	private static string BuildTimelyDeliveryDefaultFilterSentence()
	{
		var calendar = new PersianCalendar();
		var today = DateTime.Today;
		var from = calendar.ToDateTime(calendar.GetYear(today), 1, 1, 0, 0, 0, 0).Date;
		return $"بازه پیش‌فرض گزارش تحویل به‌موقع: از ابتدای سال شمسی جاری تا امروز ({from.ToShamsiDate()} تا {today.ToShamsiDate()})";
	}
}
