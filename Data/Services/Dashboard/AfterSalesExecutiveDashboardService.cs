using Common.Utilities;
using Entities.App.Cng;
using Entities.App.Cng.Enums;
using Entities.App.Rpr;
using Entities.App.Rpr.Enums;
using Entities.App.Sale;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Data.Services.Dashboard;

/// <summary>
/// داشبورد مدیریتی خدمات پس از فروش — فقط مانده‌های قابل‌اتکا و روند ثبت (نه باز بودن درخواست پشتیبانی).
/// </summary>
public class AfterSalesExecutiveDashboardService(ApplicationDbContext db) : IAfterSalesExecutiveDashboardService
{
	private const string WaitingDefinition =
		"درخواست پشتیبانی غیرصوری که پرچم «در انتظار اعزام» روشن است.";

	private const string OpenRepairsDefinition =
		"درخواست تعمیر با وضعیت خالی یا خارج از تکمیل‌شده، غیرقابل‌تعمیر، عدم‌تایید مشتری و آیتم صوری.";

	private const string OpenCngDefinition =
		"تعمیرات جایگاه CNG با وضعیت خالی یا خارج از تکمیل‌شده و غیرقابل‌تعمیر.";

	private const string RegisteredTrendDefinition =
		"تعداد درخواست پشتیبانی غیرصوری ثبت‌شده در بازه، به تفکیک ماه شمسی تاریخ ایجاد — عنوان «ثبت‌شده در بازه»، نه باز.";

	public async Task<AfterSalesExecutiveDashboardResponse> GetDataAsync(
		AfterSalesExecutiveDashboardFilter filter,
		CancellationToken cancellationToken = default)
	{
		filter ??= new AfterSalesExecutiveDashboardFilter();
		var now = DateTime.Now;
		var response = new AfterSalesExecutiveDashboardResponse
		{
			ComputedAtShamsiDateTime = now.ToShamsiDateTime(),
			StockLabel = "وضعیت همین لحظه",
			WaitingToDispatchDefinition = WaitingDefinition,
			OpenRepairsDefinition = OpenRepairsDefinition,
			OpenCngRepairsDefinition = OpenCngDefinition,
			RegisteredTrendDefinition = RegisteredTrendDefinition
		};

		response.WaitingToDispatchCount = await db.Set<ServiceRequest>()
			.AsNoTracking()
			.CountAsync(x => x.Unreal == false && x.IsWaitingToSend == true, cancellationToken);

		var openRepairStatuses = await db.Set<RepairRequest>()
			.AsNoTracking()
			.Where(x => x.Status == null
				|| (x.Status != RepairRequestStatusEnum.Completed
					&& x.Status != RepairRequestStatusEnum.Unrepairable
					&& x.Status != RepairRequestStatusEnum.CustomerRejected
					&& x.Status != RepairRequestStatusEnum.UnrealItem))
			.GroupBy(x => x.Status)
			.Select(g => new { Status = g.Key, Count = g.Count() })
			.ToListAsync(cancellationToken);

		response.OpenRepairsByStatus = openRepairStatuses
			.Select(x => new AfterSalesNamedCount
			{
				Name = x.Status.HasValue ? x.Status.Value.ToDisplay() : "نامشخص",
				Count = x.Count
			})
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Name)
			.ToList();
		response.OpenRepairsCount = response.OpenRepairsByStatus.Sum(x => x.Count);

		var cngTotal = await db.Set<Repairs>().AsNoTracking().CountAsync(cancellationToken);
		var cngWithStatus = await db.Set<Repairs>().AsNoTracking()
			.CountAsync(x => x.Status != null, cancellationToken);
		var cngStatusCoverage = cngTotal == 0 ? 0d : (double)cngWithStatus / cngTotal;

		// Status روی موجودیت required است و در عمل برای اکثر قریب‌به‌اتفاق ردیف‌ها پر است.
		if (cngTotal > 0 && cngStatusCoverage >= 0.9)
		{
			response.IncludeCngStatusChart = true;

			var openCngStatuses = await db.Set<Repairs>()
				.AsNoTracking()
				.Where(x => x.Status == null
					|| (x.Status != RepairsStatusEnum.Finished
						&& x.Status != RepairsStatusEnum.Unrepairable))
				.GroupBy(x => x.Status)
				.Select(g => new { Status = g.Key, Count = g.Count() })
				.ToListAsync(cancellationToken);

			response.OpenCngRepairsByStatus = openCngStatuses
				.Select(x => new AfterSalesNamedCount
				{
					Name = x.Status.HasValue ? x.Status.Value.ToDisplay() : "نامشخص",
					Count = x.Count
				})
				.OrderByDescending(x => x.Count)
				.ThenBy(x => x.Name)
				.ToList();
			response.OpenCngRepairsCount = response.OpenCngRepairsByStatus.Sum(x => x.Count);
		}
		else
		{
			response.IncludeCngStatusChart = false;
			response.CngOmittedReason = cngTotal == 0
				? "ردیفی برای تعمیرات جایگاه CNG نیست."
				: $"وضعیت فقط برای {cngWithStatus:N0} از {cngTotal:N0} ردیف پر است؛ نمودار CNG حذف شد.";
		}

		await FillRegisteredTrendAsync(response, filter, cancellationToken);

		return response;
	}

	private async Task FillRegisteredTrendAsync(
		AfterSalesExecutiveDashboardResponse response,
		AfterSalesExecutiveDashboardFilter filter,
		CancellationToken cancellationToken)
	{
		var (fromDate, toDate) = ResolveDateRange(filter);

		// CreatedOnMiladiDateTime برای همهٔ درخواست‌های غیرصوری پر است — روند قابل‌اتکا است.
		var createdDates = await db.Set<ServiceRequest>()
			.AsNoTracking()
			.Where(x => x.Unreal == false
				&& x.CreatedOnMiladiDateTime != null
				&& x.CreatedOnMiladiDateTime >= fromDate
				&& x.CreatedOnMiladiDateTime < toDate.AddDays(1))
			.Select(x => x.CreatedOnMiladiDateTime!.Value)
			.ToListAsync(cancellationToken);

		response.IncludeRegisteredTrend = true;
		response.TrendFromDateShamsi = fromDate.ToShamsiDate();
		response.TrendToDateShamsi = toDate.ToShamsiDate();

		var calendar = new PersianCalendar();
		response.RegisteredByShamsiMonth = createdDates
			.GroupBy(d => $"{calendar.GetYear(d)}/{calendar.GetMonth(d):D2}")
			.Select(g => new AfterSalesNamedCount { Name = g.Key, Count = g.Count() })
			.OrderBy(x => x.Name)
			.ToList();
	}

	private static (DateTime FromDate, DateTime ToDate) ResolveDateRange(AfterSalesExecutiveDashboardFilter filter)
	{
		var fromDate = ParseShamsiDate(filter.FromDateShamsi) ?? GetFirstDayOfCurrentShamsiYear();
		var toDate = ParseShamsiDate(filter.ToDateShamsi) ?? DateTime.Today;
		if (toDate < fromDate)
			(toDate, fromDate) = (fromDate, toDate);
		return (fromDate.Date, toDate.Date);
	}

	private static DateTime? ParseShamsiDate(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		var datePart = NormalizeDigits(value.Trim())
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.FirstOrDefault();

		if (string.IsNullOrWhiteSpace(datePart))
			return null;

		var parts = datePart.Split(['/', '-'], StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length != 3
			|| !int.TryParse(parts[0], out var year)
			|| !int.TryParse(parts[1], out var month)
			|| !int.TryParse(parts[2], out var day))
			return null;

		try
		{
			return year < 1700
				? new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0).Date
				: new DateTime(year, month, day).Date;
		}
		catch (ArgumentOutOfRangeException)
		{
			return null;
		}
	}

	private static string NormalizeDigits(string value)
	{
		return value
			.Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
			.Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
			.Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
			.Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
	}

	private static DateTime GetFirstDayOfCurrentShamsiYear()
	{
		var calendar = new PersianCalendar();
		return calendar.ToDateTime(calendar.GetYear(DateTime.Today), 1, 1, 0, 0, 0, 0).Date;
	}
}
