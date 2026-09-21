using Common.Utilities;
using Entities.App.Pln;
using Entities.App.Pln.Enums;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Data.Services.Pln;

/// <summary>
/// گزارش انحراف از تحویل به‌موقع سفارش‌های ساخت.
/// مبنای محاسبه تاخیر عیناً همان قاعده‌ای است که در ProductionOrderDelayController.CalculateOrderDelayRangeAsync
/// و نمایه‌های داده vw_All_Need_POD / vw_All_POD استفاده می‌شود: تاریخ مجاز سفارش برابر است با
/// بیشترین تاریخ بین تحویل توافقی و تحویل استاندارد همه اقلام.
/// سهم هر عامل توقف از روزهای تاخیر ثبت‌شده در Pln.ProductionOrderDelay به‌دست می‌آید.
/// </summary>
public class TimelyDeliveryReportService(ApplicationDbContext db) : ITimelyDeliveryReportService
{
	/// <summary>حداقل مهلت توافقی (مخرج درصد انحراف) به روز — قاعده موروثی از سیستم قدیم</summary>
	private const int MinimumAgreedWindowDays = 10;

	private static readonly DelayResponsibleEnum[] ExecutiveUnits =
	[
		DelayResponsibleEnum.IndustrialStops,
		DelayResponsibleEnum.EngineeringStops,
		DelayResponsibleEnum.ProcurementStops,
		DelayResponsibleEnum.ProductionStops,
		DelayResponsibleEnum.ProductionStopsImproperFeeding,
		DelayResponsibleEnum.TestingStops,
		DelayResponsibleEnum.WarehouseStops,
		DelayResponsibleEnum.UnforeseenExecutiveStops,
		DelayResponsibleEnum.UnidentifiedExecutiveStops
	];

	private static readonly DelayResponsibleEnum[] SalesUnits =
	[
		DelayResponsibleEnum.EngineeringCompressorStops,
		DelayResponsibleEnum.IndustrialSalesStops,
		DelayResponsibleEnum.IndustrialSalesStops1,
		DelayResponsibleEnum.IndustrialSalesStops2,
		DelayResponsibleEnum.IndustrialSalesStops3,
		DelayResponsibleEnum.IndustrialGasSalesStops,
		DelayResponsibleEnum.MedicalEquipmentSalesStops,
		DelayResponsibleEnum.TurboMachineSalesStops,
		DelayResponsibleEnum.CNGSalesStops,
		DelayResponsibleEnum.CompressedAirSalesStops,
		DelayResponsibleEnum.SalesStopsObsolete
	];

	private static readonly DelayResponsibleEnum[] OutOfExecutiveUnits =
	[
		.. SalesUnits,
		DelayResponsibleEnum.CommercialStops,
		DelayResponsibleEnum.FinancialStops
	];

	/// <summary>ترتیب نمایش واحدها در نمودار و جدول‌ها (مطابق گزارش سیستم قدیم)</summary>
	private static readonly DelayResponsibleEnum[] DisplayOrder = [.. ExecutiveUnits, .. OutOfExecutiveUnits];

	private sealed class ItemRow
	{
		public long OrderId { get; init; }
		public long ItemId { get; init; }
		public DateTime? PreparationMiladiDate { get; init; }
		public DateTime? AgreedDeliverDate { get; init; }
		public DateTime? StandardDeliveryMiladiDate { get; init; }
		public DateTime? SendToIndustrialMiladiDate { get; init; }
		public string? PartName { get; init; }
		public string? Serial { get; init; }
	}

	private sealed class OrderHeader
	{
		public long Id { get; init; }
		public long? ProductionOrderNumber { get; init; }
		public DateTime? CreatedOnMiladiDateTime { get; init; }
		public string? CustomerTitle { get; init; }
		public string? BranchTitle { get; init; }
		public string? SalesExpertName { get; init; }
	}

	public async Task<TimelyDeliveryReportResponse> GetReportAsync(
		TimelyDeliveryReportFilter filter,
		CancellationToken cancellationToken = default)
	{
		filter ??= new TimelyDeliveryReportFilter();
		var response = new TimelyDeliveryReportResponse();
		var today = DateTime.Today;

		var (fromDate, toDate) = ResolveDateRange(filter);

		var items = await LoadEligibleItemsAsync(filter, fromDate, toDate, includeDisabledOrders: false, cancellationToken);
		response.InactiveOrdersCount = await CountInactiveOrdersAsync(filter, fromDate, toDate, cancellationToken);

		if (items.Count == 0)
		{
			response.OrdersCount = response.InactiveOrdersCount;
			response.TimelyDeliveryOrdersCount = response.InactiveOrdersCount;
			response.Units = BuildUnits(new Dictionary<DelayResponsibleEnum, decimal>());
			response.OnTimeDeliveryPercentage = 100;
			return response;
		}

		var orderIds = items.Select(i => i.OrderId).Distinct().ToList();
		var headers = await LoadOrderHeadersAsync(orderIds, cancellationToken);
		var delaysByOrder = await LoadDelaysAsync(orderIds, cancellationToken);

		var orders = new List<TimelyDeliveryOrderDto>();
		var unitDeviationSums = new Dictionary<DelayResponsibleEnum, decimal>();
		var unidentifiedOrderNumbers = new List<string>();
		// کلید ماه شمسی «تاریخ مجاز تحویل سفارش» (بزرگ‌ترین تاریخ اقلام) — همان تاریخی که فیلتر بازه روی آن اعمال شده
		var trendKeys = new List<(string Month, bool IsDelayed)>();

		foreach (var group in items.GroupBy(i => i.OrderId))
		{
			if (!headers.TryGetValue(group.Key, out var header))
				continue;

			var orderDeliverDate = group
				.Select(i => GetItemDeliverDate(i.AgreedDeliverDate, i.StandardDeliveryMiladiDate))
				.Where(d => d.HasValue)
				.Select(d => d!.Value)
				.Select(d => (DateTime?)d)
				.DefaultIfEmpty(null)
				.Max();

			var itemDelays = group
				.Select(item =>
				{
					var deliverDate = GetItemDeliverDate(item.AgreedDeliverDate, item.StandardDeliveryMiladiDate);
					return new
					{
						item.ItemId,
						item.PartName,
						item.Serial,
						DeliverDate = deliverDate,
						PreparationDate = item.PreparationMiladiDate?.Date,
						DelayDays = CalcItemDelayDays(
							orderDeliverDate,
							item.PreparationMiladiDate?.Date,
							item.AgreedDeliverDate,
							item.StandardDeliveryMiladiDate,
							item.SendToIndustrialMiladiDate,
							today)
					};
				})
				.ToList();

			var maxDelayItem = itemDelays
				.OrderByDescending(i => i.DelayDays)
				.ThenBy(i => i.ItemId)
				.First();

			var maxDelayDays = maxDelayItem.DelayDays;
			var delays = delaysByOrder.TryGetValue(group.Key, out var orderDelays) ? orderDelays : [];

			var registeredByUnit = delays
				.GroupBy(d => d.DelayResponsible)
				.ToDictionary(g => g.Key, g => g.Sum(d => d.DelayDays ?? 0));

			var registeredDelayDays = registeredByUnit.Values.Sum();
			var unidentifiedDelayDays = Math.Max(0, maxDelayDays - registeredDelayDays);

			var agreedWindowDays = CalcAgreedWindowDays(
				orderDeliverDate,
				group.Min(i => i.SendToIndustrialMiladiDate),
				header.CreatedOnMiladiDateTime);

			var totalDeviation = agreedWindowDays <= 0 || maxDelayDays <= 0
				? 0m
				: maxDelayDays / (decimal)agreedWindowDays * 100m;

			var basis = registeredDelayDays + unidentifiedDelayDays;
			if (basis > 0 && totalDeviation > 0)
			{
				foreach (var (unit, days) in registeredByUnit)
				{
					if (days <= 0)
						continue;

					AddDeviation(unitDeviationSums, unit, days / (decimal)basis * totalDeviation);
				}

				if (unidentifiedDelayDays > 0)
				{
					AddDeviation(
						unitDeviationSums,
						DelayResponsibleEnum.UnidentifiedExecutiveStops,
						unidentifiedDelayDays / (decimal)basis * totalDeviation);
				}
			}

			if (unidentifiedDelayDays > 0)
				unidentifiedOrderNumbers.Add(header.ProductionOrderNumber?.ToString() ?? header.Id.ToString());

			// روند ماهانه بر مبنای تاریخ مجاز تحویل خودِ سفارش (بزرگ‌ترین تاریخ اقلام) ساخته می‌شود.
			if (orderDeliverDate.HasValue)
				trendKeys.Add((orderDeliverDate.Value.ToShamsiDate()[..7], totalDeviation > 0));

			orders.Add(new TimelyDeliveryOrderDto
			{
				ProductionOrderId = header.Id,
				ProductionOrderNumber = header.ProductionOrderNumber,
				CustomerTitle = header.CustomerTitle,
				BranchTitle = header.BranchTitle,
				SalesExpertName = header.SalesExpertName,
				DeliverDateShamsi = orderDeliverDate?.ToShamsiDate(),
				PreparationDateShamsi = maxDelayItem.PreparationDate?.ToShamsiDate(),
				PartName = maxDelayItem.PartName,
				Serial = maxDelayItem.Serial,
				MaxDelayDays = maxDelayDays,
				AgreedWindowDays = agreedWindowDays,
				TotalDeviationPercentage = Round(totalDeviation),
				RegisteredDelayDays = registeredDelayDays,
				UnregisteredDelayDays = unidentifiedDelayDays,
				MissingPreparationCount = itemDelays.Count(i => i.PreparationDate == null),
				TotalItemsCount = itemDelays.Count,
				Delays = [.. delays.Select(d => new TimelyDeliveryOrderDelayDto
				{
					Id = d.Id,
					DelayResponsible = d.DelayResponsible,
					DelayResponsibleTitle = d.DelayResponsible.ToDisplay(),
					DelayDays = d.DelayDays,
					FromDateShamsi = d.DelayStartShamsiDate ?? d.DelayStartMiladiDate?.ToShamsiDate(),
					ToDateShamsi = d.DelayEndShamsiDate ?? d.DelayEndMiladiDate?.ToShamsiDate(),
					DelayReason = d.DelayReason,
					Description = d.Description,
					CreatedByName = d.CreatedByName
				})]
			});
		}

		BuildResponse(response, orders, unitDeviationSums, unidentifiedOrderNumbers, trendKeys);
		return response;
	}

	public async Task<List<ProductionOrderLookupDto>> GetProductionOrderNumbersAsync(
		CancellationToken cancellationToken = default)
	{
		var rows = await db.Set<ProductionOrder>().AsNoTracking()
			.Where(po => po.IsActive == IsActiveEnum.Active
				&& !po.IsDisableForTimelyDeliveryReport
				&& !po.DelayNotCalculated
				&& po.ProjectManagerId == null
				&& po.ProductionOrderNumber != null
				&& po.Items.Any(poi => !poi.IsDeleted
					&& poi.IsLatestVersion
					&& poi.Status != ProductionOrderItemStatusEnum.Invalid
					&& poi.SendToIndustrialMiladiDate != null))
			.OrderByDescending(po => po.ProductionOrderNumber)
			.Select(po => new
			{
				Id = po.Id!.Value,
				po.ProductionOrderNumber,
				CustomerTitle = po.Contract != null && po.Contract.Customer != null && po.Contract.Customer.Party != null
					? po.Contract.Customer.Party.FullName
					: null
			})
			.ToListAsync(cancellationToken);

		return [.. rows.Select(r => new ProductionOrderLookupDto
		{
			Id = r.Id,
			ProductionOrderNumber = r.ProductionOrderNumber,
			Text = string.IsNullOrWhiteSpace(r.CustomerTitle)
				? r.ProductionOrderNumber?.ToString() ?? string.Empty
				: $"{r.ProductionOrderNumber} - {r.CustomerTitle}"
		})];
	}

	#region Data loading

	private async Task<List<ItemRow>> LoadEligibleItemsAsync(
		TimelyDeliveryReportFilter filter,
		DateTime? fromDate,
		DateTime? toDate,
		bool includeDisabledOrders,
		CancellationToken cancellationToken)
	{
		var query = BuildItemQuery(filter, includeDisabledOrders);

		var rows = await query
			.Select(poi => new ItemRow
			{
				OrderId = poi.ProductionOrderId!.Value,
				ItemId = poi.Id!.Value,
				PreparationMiladiDate = poi.PreparationMiladiDate,
				AgreedDeliverDate = poi.AgreedDeliverDate,
				StandardDeliveryMiladiDate = poi.StandardDeliveryMiladiDate,
				SendToIndustrialMiladiDate = poi.SendToIndustrialMiladiDate,
				PartName = poi.Part != null ? poi.Part.Name : null,
				Serial = poi.Serial
			})
			.ToListAsync(cancellationToken);

		// بازه تاریخ روی «تاریخ مجاز تحویل» سفارش اعمال می‌شود؛ چون این تاریخ از دو ستون
		// AgreedDeliverDate/StandardDeliveryMiladiDate ساخته می‌شود، فیلتر در حافظه انجام می‌شود.
		if (filter.ProductionOrderNumber.HasValue || (fromDate == null && toDate == null))
			return rows;

		var eligibleOrderIds = rows
			.GroupBy(r => r.OrderId)
			.Where(g =>
			{
				var maxDeliverDate = g
					.Select(i => GetItemDeliverDate(i.AgreedDeliverDate, i.StandardDeliveryMiladiDate))
					.Where(d => d.HasValue)
					.Select(d => d!.Value)
					.DefaultIfEmpty(DateTime.MinValue)
					.Max();

				if (maxDeliverDate == DateTime.MinValue)
					return false;
				if (fromDate.HasValue && maxDeliverDate < fromDate.Value)
					return false;
				if (toDate.HasValue && maxDeliverDate > toDate.Value)
					return false;

				return true;
			})
			.Select(g => g.Key)
			.ToHashSet();

		return [.. rows.Where(r => eligibleOrderIds.Contains(r.OrderId))];
	}

	private IQueryable<ProductionOrderItem> BuildItemQuery(
		TimelyDeliveryReportFilter filter,
		bool includeDisabledOrders)
	{
		var query = db.Set<ProductionOrderItem>().AsNoTracking()
			.Where(poi => !poi.IsDeleted
				&& poi.IsLatestVersion
				&& poi.Status != ProductionOrderItemStatusEnum.Invalid
				// تاریخ ارسال، ملاک قطعی ورود قلم به کارتابل صنایع است.
				&& poi.SendToIndustrialMiladiDate != null
				&& poi.ProductionOrderId != null
				&& poi.ProductionOrder!.IsActive == IsActiveEnum.Active
				// سفارش‌های پروژه‌ای (واقعی یا علامت‌گذاری‌شده) از سیکل تاخیرات خارج هستند.
				&& poi.ProductionOrder.ProjectManagerId == null
				&& !poi.ProductionOrder.DelayNotCalculated);

		query = includeDisabledOrders
			? query.Where(poi => poi.ProductionOrder!.IsDisableForTimelyDeliveryReport)
			: query.Where(poi => !poi.ProductionOrder!.IsDisableForTimelyDeliveryReport);

		if (filter.ProductionOrderNumber.HasValue)
			query = query.Where(poi => poi.ProductionOrder!.ProductionOrderNumber == filter.ProductionOrderNumber.Value);

		if (filter.BranchId.HasValue)
			query = query.Where(poi => poi.ProductionOrder!.BranchId == filter.BranchId.Value);

		if (filter.SalesExpertId.HasValue)
			query = query.Where(poi => poi.ProductionOrder!.SalesExpertId == filter.SalesExpertId.Value);

		return query;
	}

	private async Task<int> CountInactiveOrdersAsync(
		TimelyDeliveryReportFilter filter,
		DateTime? fromDate,
		DateTime? toDate,
		CancellationToken cancellationToken)
	{
		var rows = await LoadEligibleItemsAsync(filter, fromDate, toDate, includeDisabledOrders: true, cancellationToken);
		return rows.Select(r => r.OrderId).Distinct().Count();
	}

	private async Task<Dictionary<long, OrderHeader>> LoadOrderHeadersAsync(
		List<long> orderIds,
		CancellationToken cancellationToken)
	{
		var headers = await db.Set<ProductionOrder>().AsNoTracking()
			.Where(po => po.Id != null && orderIds.Contains(po.Id.Value))
			.Select(po => new OrderHeader
			{
				Id = po.Id!.Value,
				ProductionOrderNumber = po.ProductionOrderNumber,
				CreatedOnMiladiDateTime = po.CreatedOnMiladiDateTime,
				CustomerTitle = po.Contract != null && po.Contract.Customer != null && po.Contract.Customer.Party != null
					? po.Contract.Customer.Party.FullName
					: null,
				BranchTitle = po.Branch != null ? po.Branch.Title : null,
				SalesExpertName = po.SalesExpert != null ? po.SalesExpert.Name : null
			})
			.ToListAsync(cancellationToken);

		return headers.ToDictionary(h => h.Id);
	}

	private async Task<Dictionary<long, List<ProductionOrderDelay>>> LoadDelaysAsync(
		List<long> orderIds,
		CancellationToken cancellationToken)
	{
		var delays = await db.Set<ProductionOrderDelay>().AsNoTracking()
			.Where(pod => pod.ProductionOrderId != null
				&& orderIds.Contains(pod.ProductionOrderId.Value)
				&& pod.IsActive == IsActiveEnum.Active)
			.OrderBy(pod => pod.DelayStartMiladiDate)
			.ThenBy(pod => pod.Id)
			.ToListAsync(cancellationToken);

		return delays
			.GroupBy(d => d.ProductionOrderId!.Value)
			.ToDictionary(g => g.Key, g => g.ToList());
	}

	#endregion

	#region Delay calculation (هم‌ارز ProductionOrderDelayController.CalculateOrderDelayRangeAsync)

	private static DateTime? GetValidDeliverDate(DateTime? date)
	{
		if (date == null || date.Value.Year >= 9000)
			return null;
		return date.Value.Date;
	}

	private static DateTime? GetItemDeliverDate(DateTime? agreed, DateTime? standard)
	{
		var validAgreed = GetValidDeliverDate(agreed);
		var validStandard = GetValidDeliverDate(standard);
		if (validAgreed == null)
			return validStandard;
		if (validStandard == null)
			return validAgreed;
		return validAgreed.Value >= validStandard.Value ? validAgreed : validStandard;
	}

	private static int CalcItemDelayDays(
		DateTime? orderDeliverDate,
		DateTime? preparationDate,
		DateTime? agreedDeliverDate,
		DateTime? standardDeliverDate,
		DateTime? sendToIndustrialDate,
		DateTime todayDate)
	{
		if (orderDeliverDate == null)
			return 0;

		// وقتی زمان استاندارد وجود ندارد، فروش تاریخ مجاز را پیش از ورود قلم به
		// کارتابل صنایع ثبت کرده و صنایع نیز قلم را همان روز آماده کرده است، تاخیر صفر است.
		if (!GetValidDeliverDate(standardDeliverDate).HasValue &&
			agreedDeliverDate.HasValue &&
			sendToIndustrialDate.HasValue &&
			preparationDate.HasValue &&
			agreedDeliverDate.Value.Date < sendToIndustrialDate.Value.Date &&
			preparationDate.Value.Date == sendToIndustrialDate.Value.Date)
			return 0;

		if (preparationDate != null)
			return Math.Max(0, (int)(preparationDate.Value.Date - orderDeliverDate.Value.Date).TotalDays);

		if (todayDate > orderDeliverDate.Value.Date)
			return (int)(todayDate - orderDeliverDate.Value.Date).TotalDays;

		return 0;
	}

	/// <summary>
	/// مهلت توافقی سفارش (مخرج درصد انحراف): فاصله «ورود سفارش به سیکل تولید» تا «تاریخ مجاز تحویل».
	///
	/// گزارش قدیم این فاصله را از ProductionOrder.CreatedDate می‌گرفت، ولی در سیستم جدید
	/// CreatedOnMiladiDateTime زمانِ درج رکورد توسط جاب همگام‌سازی راهکاران است نه تاریخ واقعی صدور سفارش
	/// (۹۰۳ سفارش از ۹۷۹ سفارش همین یک تاریخ را دارند)، و استفاده از آن مخرج را برای اکثر سفارش‌ها منفی می‌کرد.
	/// بنابراین مبدأ، زودترین تاریخ ارسال اقلام به کارتابل صنایع است — همان ملاکی که قاعده جاری
	/// محاسبه تاخیر (ProductionOrderDelayController.CalculateOrderDelayRangeAsync) بر آن بنا شده است.
	///
	/// قاعده موروثی سیستم قدیم حفظ شده: اگر فاصله کمتر از ۱۰ روز شد، ۱۰ روز در نظر گرفته می‌شود.
	/// </summary>
	private static int CalcAgreedWindowDays(
		DateTime? deliverDate,
		DateTime? sendToIndustrialDate,
		DateTime? orderCreatedDate)
	{
		if (deliverDate == null)
			return MinimumAgreedWindowDays;

		var startDate = (sendToIndustrialDate ?? orderCreatedDate)?.Date;
		if (startDate == null)
			return MinimumAgreedWindowDays;

		var days = (int)(deliverDate.Value.Date - startDate.Value).TotalDays;

		if (days < -MinimumAgreedWindowDays)
			days = Math.Abs(days);

		return days < MinimumAgreedWindowDays ? MinimumAgreedWindowDays : days;
	}

	#endregion

	#region Aggregation

	private static void AddDeviation(
		Dictionary<DelayResponsibleEnum, decimal> sums,
		DelayResponsibleEnum unit,
		decimal value)
	{
		sums[unit] = sums.TryGetValue(unit, out var current) ? current + value : value;
	}

	private void BuildResponse(
		TimelyDeliveryReportResponse response,
		List<TimelyDeliveryOrderDto> orders,
		Dictionary<DelayResponsibleEnum, decimal> unitDeviationSums,
		List<string> unidentifiedOrderNumbers,
		List<(string Month, bool IsDelayed)> trendKeys)
	{
		var orderCount = orders.Count;
		var averages = orderCount == 0
			? []
			: unitDeviationSums.ToDictionary(kv => kv.Key, kv => kv.Value / orderCount);

		response.Units = BuildUnits(averages);

		response.TotalDeviationPercentage = orderCount == 0
			? 0
			: Round(orders.Sum(o => o.TotalDeviationPercentage) / orderCount);

		response.ExecutiveSumPercentage = Round(SumOf(averages, ExecutiveUnits));
		response.OutOfExecutiveSumPercentage = Round(SumOf(averages, OutOfExecutiveUnits));
		response.SalesUnitsSumPercentage = Round(SumOf(averages, SalesUnits));
		response.CommercialPercentage = Round(ValueOf(averages, DelayResponsibleEnum.CommercialStops));
		response.FinancialPercentage = Round(ValueOf(averages, DelayResponsibleEnum.FinancialStops));
		response.OnTimeDeliveryPercentage = Round(100m - response.TotalDeviationPercentage);

		response.DelayedOrdersCount = orders.Count(o => o.TotalDeviationPercentage > 0);
		response.OrdersCount = orderCount + response.InactiveOrdersCount;
		response.TimelyDeliveryOrdersCount = response.OrdersCount - response.DelayedOrdersCount;

		if (unidentifiedOrderNumbers.Count > 0)
			response.UnidentifiedDelayProductionOrders = string.Join("، ", unidentifiedOrderNumbers.Distinct());

		response.Orders = [.. orders
			// جدول و اکسل فقط سفارش‌های دارای تاخیر را نشان می‌دهند؛ مخرج میانگین‌ها همه سفارش‌های واجد شرایط است.
			.Where(o => o.MaxDelayDays > 0)
			.OrderByDescending(o => o.TotalDeviationPercentage)
			.ThenByDescending(o => o.MaxDelayDays)
			.ThenBy(o => o.ProductionOrderNumber)];

		response.UnregisteredOrders = [.. orders
			.Where(o => o.UnregisteredDelayDays > 0)
			.OrderByDescending(o => o.UnregisteredDelayDays)
			.Select(o => new TimelyDeliveryUnregisteredOrderDto
			{
				ProductionOrderId = o.ProductionOrderId,
				ProductionOrderNumber = o.ProductionOrderNumber,
				CustomerTitle = o.CustomerTitle,
				MaxDelayDays = o.MaxDelayDays,
				RegisteredDelayDays = o.RegisteredDelayDays,
				UnregisteredDelayDays = o.UnregisteredDelayDays
			})];

		response.MonthlyTrend = BuildMonthlyTrend(trendKeys);
	}

	private static List<TimelyDeliveryUnitDto> BuildUnits(Dictionary<DelayResponsibleEnum, decimal> averages)
	{
		return [.. DisplayOrder.Select(unit => new TimelyDeliveryUnitDto
		{
			Code = unit,
			Title = unit.ToDisplay(),
			Group = ExecutiveUnits.Contains(unit) ? DelayUnitGroup.Executive : DelayUnitGroup.OutOfExecutive,
			IsSalesUnit = SalesUnits.Contains(unit),
			Percentage = Round(ValueOf(averages, unit))
		})];
	}

	private static List<TimelyDeliveryMonthlyTrendDto> BuildMonthlyTrend(List<(string Month, bool IsDelayed)> trendKeys)
	{
		return [.. trendKeys
			.GroupBy(k => k.Month)
			.OrderBy(g => g.Key)
			.Select(g =>
			{
				var count = g.Count();
				var delayed = g.Count(k => k.IsDelayed);
				return new TimelyDeliveryMonthlyTrendDto
				{
					ShamsiMonth = g.Key,
					OrdersCount = count,
					DelayedOrdersCount = delayed,
					OnTimePercentage = count == 0 ? 0 : Round((count - delayed) / (decimal)count * 100m)
				};
			})];
	}

	private static decimal SumOf(Dictionary<DelayResponsibleEnum, decimal> averages, DelayResponsibleEnum[] units)
		=> units.Sum(unit => ValueOf(averages, unit));

	private static decimal ValueOf(Dictionary<DelayResponsibleEnum, decimal> averages, DelayResponsibleEnum unit)
		=> averages.TryGetValue(unit, out var value) ? value : 0m;

	private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

	#endregion

	#region Date range

	private static (DateTime? FromDate, DateTime? ToDate) ResolveDateRange(TimelyDeliveryReportFilter filter)
	{
		if (filter.ProductionOrderNumber.HasValue)
			return (null, null);

		var fromDate = ParseShamsiDate(filter.FromDateShamsi);
		var toDate = ParseShamsiDate(filter.ToDateShamsi);

		// پیش‌فرض مطابق گزارش قدیم: از ابتدای سال شمسی جاری تا امروز
		fromDate ??= GetFirstDayOfCurrentShamsiYear();
		toDate ??= DateTime.Today;

		return (fromDate.Value.Date, toDate.Value.Date);
	}

	/// <summary>
	/// تبدیل تاریخ شمسی ورودی کاربر به میلادی.
	/// از DateTimeExtensions.ToMiladiDate استفاده نمی‌شود چون آن متد ابتدا DateTime.TryParse را صدا می‌زند
	/// و رشته‌ای مثل «1405/01/01» را به‌عنوان سال میلادی ۱۴۰۵ می‌پذیرد (تاریخ نتیجه ~۶۰۰ سال عقب‌تر).
	/// همان قاعده ProductPriceCompareController.TryParseSourceDate: سال کمتر از ۱۷۰۰ یعنی شمسی.
	/// </summary>
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
			// تاریخ نامعتبر ورودی کاربر، به معنی «بدون فیلتر» تلقی می‌شود.
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

	#endregion
}
