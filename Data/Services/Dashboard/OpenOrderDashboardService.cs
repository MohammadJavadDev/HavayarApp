using Common.Utilities;
using Entities.App.Sup;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.Dashboard;

/// <summary>
/// داشبورد درخواست‌های باز خرید.
/// جمعیت فقط ردیف‌های حذف‌نشده است؛ فیلد Status برای نمودار/گروه‌بندی استفاده نمی‌شود.
/// </summary>
public class OpenOrderDashboardService(ApplicationDbContext db) : IOpenOrderDashboardService
{
	private const int DetailCap = 50;

	public async Task<OpenOrderDashboardResponse> GetDashboardAsync(
		OpenOrderDashboardFilter? filter = null,
		CancellationToken cancellationToken = default)
	{
		_ = filter; // تاریخ روی شاخص‌های مانده اثر ندارد
		var today = DateTime.Today;
		var now = DateTime.Now;

		var openQuery = db.Set<OpenOrderRequest>()
			.AsNoTracking()
			.Where(x => !x.IsDeleted);

		var snapshot = await openQuery
			.Select(x => new OpenRowSnapshot
			{
				IsStop = x.IsStop,
				EngineeringAccept = x.EngineeringAccept,
				CreatedOnMiladiDateTime = x.CreatedOnMiladiDateTime,
				NeedDateMiladiDate = x.NeedDateMiladiDate
			})
			.ToListAsync(cancellationToken);

		var openTotal = snapshot.Count;
		var stopped = snapshot.Count(x => x.IsStop);
		var active = snapshot.Count(x => !x.IsStop);
		var engAccepted = snapshot.Count(x => x.EngineeringAccept);
		var engNotAccepted = snapshot.Count(x => !x.EngineeringAccept);

		var age0To30 = 0;
		var age31To90 = 0;
		var age91Plus = 0;
		var ageUnknown = 0;
		var needPastDue = 0;
		var needNotYet = 0;
		var needNoDate = 0;

		foreach (var row in snapshot)
		{
			if (row.CreatedOnMiladiDateTime is null)
			{
				ageUnknown++;
			}
			else
			{
				var ageDays = (today - row.CreatedOnMiladiDateTime.Value.Date).Days;
				if (ageDays <= 30)
					age0To30++;
				else if (ageDays <= 90)
					age31To90++;
				else
					age91Plus++;
			}

			if (row.NeedDateMiladiDate is null)
				needNoDate++;
			else if (row.NeedDateMiladiDate.Value.Date < today)
				needPastDue++;
			else
				needNotYet++;
		}

		var details = await openQuery
			.OrderByDescending(x => x.CreatedOnMiladiDateTime)
			.Take(DetailCap)
			.Select(x => new OpenOrderDashboardDetailRowDto
			{
				Id = x.Id ?? 0,
				PartTitle = x.Part != null ? x.Part.Name : null,
				PartCode = x.Part != null ? x.Part.Code : null,
				RequestedPersonel = x.RequestedPersonel,
				PurchaseRequestShamsiDate = x.PurchaseRequestShamsiDate,
				NeedDateShamsiDate = x.NeedDateShamsiDate,
				IsStop = x.IsStop,
				EngineeringAccept = x.EngineeringAccept
			})
			.ToListAsync(cancellationToken);

		return new OpenOrderDashboardResponse
		{
			StockLabel = "وضعیت همین لحظه",
			AsOfShamsiDateTime = now.ToShamsiDateTime(),
			PopulationFilterText = "جمعیت: درخواست‌های خرید حذف‌نشده",
			OpenTotal = Metric(
				"openTotal",
				"درخواست‌های باز خرید",
				"تعداد درخواست‌های خرید که حذف نشده‌اند.",
				openTotal),
			StopSplit =
			[
				Metric(
					"stopped",
					"متوقف",
					"درخواست خرید حذف‌نشده که متوقف است.",
					stopped),
				Metric(
					"active",
					"فعال",
					"درخواست خرید حذف‌نشده که متوقف نیست.",
					active)
			],
			EngineeringSplit =
			[
				Metric(
					"engineeringAccepted",
					"تایید مهندسی",
					"درخواست خرید حذف‌نشده با تایید مهندسی.",
					engAccepted),
				Metric(
					"engineeringNotAccepted",
					"بدون تایید مهندسی",
					"درخواست خرید حذف‌نشده بدون تایید مهندسی.",
					engNotAccepted)
			],
			AgeBuckets =
			[
				Metric(
					"age0To30",
					"۰ تا ۳۰ روز",
					"درخواست خرید حذف‌نشده با سن حداکثر ۳۰ روز از تاریخ ایجاد نسبت به امروز.",
					age0To30),
				Metric(
					"age31To90",
					"۳۱ تا ۹۰ روز",
					"درخواست خرید حذف‌نشده با سن ۳۱ تا ۹۰ روز از تاریخ ایجاد نسبت به امروز.",
					age31To90),
				Metric(
					"age91Plus",
					"۹۱ روز و بیشتر",
					"درخواست خرید حذف‌نشده با سن ۹۱ روز یا بیشتر از تاریخ ایجاد نسبت به امروز.",
					age91Plus),
				Metric(
					"ageUnknown",
					"نامشخص",
					"درخواست خرید حذف‌نشده که تاریخ ایجاد آن خالی است.",
					ageUnknown)
			],
			NeedDateBuckets =
			[
				Metric(
					"needPastDue",
					"نیاز گذشته",
					"درخواست خرید حذف‌نشده با تاریخ نیاز قبل از امروز.",
					needPastDue),
				Metric(
					"needNotYet",
					"نیاز نرسیده",
					"درخواست خرید حذف‌نشده با تاریخ نیاز امروز یا بعد از آن.",
					needNotYet),
				Metric(
					"needNoDate",
					"بدون تاریخ نیاز",
					"درخواست خرید حذف‌نشده که تاریخ نیاز آن خالی است.",
					needNoDate)
			],
			Details = details
		};
	}

	private static OpenOrderDashboardMetricDto Metric(string key, string title, string definition, long count) =>
		new()
		{
			Key = key,
			Title = title,
			Definition = definition,
			Count = count
		};

	private sealed class OpenRowSnapshot
	{
		public bool IsStop { get; init; }
		public bool EngineeringAccept { get; init; }
		public DateTime? CreatedOnMiladiDateTime { get; init; }
		public DateTime? NeedDateMiladiDate { get; init; }
	}
}
