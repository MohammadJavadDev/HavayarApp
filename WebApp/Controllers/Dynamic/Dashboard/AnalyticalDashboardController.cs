using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.Sup;
using Entities.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebApp.ViewModels.Dashboard;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Dashboard/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("داشبوردهای تحلیلی")]
	public class AnalyticalDashboardController(IUnitOfWork unitOfWork, ApplicationDbContext db) : BaseController
	{
		private const string ViewFolder = @"\Views\Panel\Dashboard\AnalyticalDashboard\";
		private const string RoutePrefix = "/panel/dashboard/analyticaldashboard/";
		private const string SupplyRoleName = "Sup.OpenOrderRequest.Supply";
		private const string UnknownTitle = "نامشخص";

		#region Pages

		[HttpGet("[action]")]
		[ActionDisplayName("داشبوردهای تحلیلی", ActionAccessType.View, ActionAccessItemType.Dashbord)]
		public IActionResult Index()
			=> View(ViewFolder + "Index.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش محصولات آماده ارسال", ActionAccessType.View, ActionAccessItemType.Dashbord)]
		public IActionResult ReadyForShipping()
			=> View(ViewFolder + "ReadyForShipping.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش سفارشات در انتظار مهندسی", ActionAccessType.View, ActionAccessItemType.Dashbord)]
		public IActionResult AwaitingEngineering()
			=> View(ViewFolder + "AwaitingEngineering.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش خریدهای باز بدون تایید مهندسی", ActionAccessType.View, ActionAccessItemType.Dashbord)]
		public IActionResult UnapprovedPurchases()
			=> View(ViewFolder + "UnapprovedPurchases.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش خریدهای متوقف شده", ActionAccessType.View, ActionAccessItemType.Dashbord)]
		public IActionResult StoppedPurchases()
			=> View(ViewFolder + "StoppedPurchases.cshtml");

		#endregion

		#region Data

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت خلاصه داشبوردهای تحلیلی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetSummary(CancellationToken cn)
		{
			// فقط کارت گزارش‌هایی برگردانده می‌شود که کاربر به صفحه آن دسترسی دارد
			var items = new List<AnalyticalDashboardSummaryItem>();

			if (CanView(nameof(ReadyForShipping)))
				items.Add(Summarize(nameof(ReadyForShipping), (await GetReadyForShippingRowsAsync(null, null, cn)).Select(c => c.Days)));

			if (CanView(nameof(AwaitingEngineering)))
				items.Add(Summarize(nameof(AwaitingEngineering), (await GetAwaitingEngineeringRowsAsync(cn)).Select(c => c.Days)));

			if (CanView(nameof(UnapprovedPurchases)))
				items.Add(Summarize(nameof(UnapprovedPurchases), (await GetUnapprovedPurchasesRowsAsync(cn)).Select(c => c.Days)));

			if (CanView(nameof(StoppedPurchases)))
				items.Add(Summarize(nameof(StoppedPurchases), (await GetStoppedPurchasesRowsAsync(false, cn)).Select(c => c.Days)));

			return Ok(items);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات گزارش محصولات آماده ارسال", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetReadyForShippingData([FromBody] ReadyForShippingFilter? filter, CancellationToken cn)
		{
			var fromDate = ParseShamsiDate(filter?.FromDateShamsi);
			var toDate = ParseShamsiDate(filter?.ToDateShamsi);
			return Ok(await GetReadyForShippingRowsAsync(fromDate, toDate, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات گزارش سفارشات در انتظار مهندسی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetAwaitingEngineeringData(CancellationToken cn)
			=> Ok(await GetAwaitingEngineeringRowsAsync(cn));

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات گزارش خریدهای باز بدون تایید مهندسی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetUnapprovedPurchasesData(CancellationToken cn)
			=> Ok(await GetUnapprovedPurchasesRowsAsync(cn));

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات گزارش خریدهای متوقف شده", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetStoppedPurchasesData(CancellationToken cn)
			=> Ok(await GetStoppedPurchasesRowsAsync(true, cn));

		#endregion

		#region Queries

		/// <summary>
		/// منطق عیناً مطابق نمایه «آماده ارسال اقلام سفارش ساخت» (POI_All_ReadyForShipping):
		/// آخرین نسخه، حذف‌نشده، وضعیت 0، مرحله ساخت آماده ارسال؛ و تعداد اقلام وضعیت 0 سفارش
		/// برابر تعداد اقلام آماده ارسال همان سفارش باشد.
		/// </summary>
		private async Task<List<ProductionOrderItemDashboardRow>> GetReadyForShippingRowsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cn)
		{
			var items = unitOfWork.Repository<ProductionOrderItem>().TableNoTracking;

			var query = items.Where(c => !c.IsDeleted
				&& c.IsLatestVersion
				&& c.Status == ProductionOrderItemStatusEnum.NotCompleted
				&& c.ProductionStep == ProductionOrderItemProductionStepEnum.ReadyForShipping
				&& items.Count(s => s.ProductionOrderId == c.ProductionOrderId
					&& !s.IsDeleted
					&& s.IsLatestVersion
					&& s.Status == ProductionOrderItemStatusEnum.NotCompleted)
				== items.Count(s => s.ProductionOrderId == c.ProductionOrderId
					&& !s.IsDeleted
					&& s.IsLatestVersion
					&& s.ProductionStep == ProductionOrderItemProductionStepEnum.ReadyForShipping));

			if (fromDate.HasValue)
				query = query.Where(c => c.PreparationMiladiDate >= fromDate.Value);

			if (toDate.HasValue)
			{
				var toDateExclusive = toDate.Value.AddDays(1);
				query = query.Where(c => c.PreparationMiladiDate < toDateExclusive);
			}

			var rows = await query
				.Select(c => new
				{
					c.Id,
					BranchTitle = c.ProductionOrder.Branch.Title,
					c.ProductionOrder.ProductionOrderNumber,
					CustomerName = c.ProductionOrder.Contract.Customer.Party.FullName,
					PartCode = c.Part.Code,
					PartName = c.Part.Name,
					c.Amount,
					c.ProductionStep,
					c.SendToIndustrialMiladiDate,
					c.SendToIndustrialShamsiDate,
					c.PreparationMiladiDate,
					c.PreparationShamsiDate
				})
				.ToListAsync(cn);

			return rows
				.Select(c => new ProductionOrderItemDashboardRow
				{
					Id = c.Id ?? 0,
					BranchTitle = OrUnknown(c.BranchTitle),
					ProductionOrderNumber = c.ProductionOrderNumber,
					CustomerName = c.CustomerName,
					PartCode = c.PartCode,
					PartName = c.PartName,
					Amount = c.Amount,
					ProductionStepTitle = c.ProductionStep.ToDisplay(),
					SendToIndustrialShamsiDate = ToShamsi(c.SendToIndustrialMiladiDate, c.SendToIndustrialShamsiDate),
					PreparationShamsiDate = ToShamsi(c.PreparationMiladiDate, c.PreparationShamsiDate),
					Days = DaysUntilToday(c.PreparationMiladiDate, c.PreparationShamsiDate)
				})
				.ToList();
		}

		private async Task<List<ProductionOrderItemDashboardRow>> GetAwaitingEngineeringRowsAsync(CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.Where(c => !c.IsDeleted
					&& c.IsLatestVersion
					&& c.Status == ProductionOrderItemStatusEnum.NotCompleted
					&& c.ProductionStep == ProductionOrderItemProductionStepEnum.AwaitingEngineering)
				.Select(c => new
				{
					c.Id,
					BranchTitle = c.ProductionOrder.Branch.Title,
					c.ProductionOrder.ProductionOrderNumber,
					CustomerName = c.ProductionOrder.Contract.Customer.Party.FullName,
					PartCode = c.Part.Code,
					PartName = c.Part.Name,
					c.Amount,
					c.ProductionStep,
					c.SendToIndustrialMiladiDate,
					c.SendToIndustrialShamsiDate,
					c.AgreedDeliverDate
				})
				.ToListAsync(cn);

			return rows
				.Select(c => new ProductionOrderItemDashboardRow
				{
					Id = c.Id ?? 0,
					BranchTitle = OrUnknown(c.BranchTitle),
					ProductionOrderNumber = c.ProductionOrderNumber,
					CustomerName = c.CustomerName,
					PartCode = c.PartCode,
					PartName = c.PartName,
					Amount = c.Amount,
					ProductionStepTitle = c.ProductionStep.ToDisplay(),
					SendToIndustrialShamsiDate = ToShamsi(c.SendToIndustrialMiladiDate, c.SendToIndustrialShamsiDate),
					AgreedDeliverShamsiDate = ToShamsi(c.AgreedDeliverDate, null),
					Days = DaysUntilToday(c.SendToIndustrialMiladiDate, c.SendToIndustrialShamsiDate)
				})
				.ToList();
		}

		/// <summary>
		/// «همه فعال» عیناً مطابق نمایه ماژول درخواست‌های باز: IsDeleted = 0
		/// </summary>
		private async Task<List<OpenOrderRequestDashboardRow>> GetUnapprovedPurchasesRowsAsync(CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
				.Where(c => !c.IsDeleted && !c.EngineeringAccept)
				.Select(c => new
				{
					c.Id,
					c.PurchaseRequestNumber,
					c.PurchaseRequestMiladiDate,
					c.PurchaseRequestShamsiDate,
					PartCode = c.Part.Code,
					PartName = c.Part.Name,
					PartUnitTitle = c.Part.Unit.Title,
					c.RequiredQty,
					c.OrderItemComment,
					c.NeedDateMiladiDate,
					c.NeedDateShamsiDate,
					c.ProductionOrderNumber,
					BranchTitle = c.ProductionOrder.Branch.Title,
					SalesExpertName = c.SalesUnitSalesExpert.NameFa ?? c.SalesUnitSalesExpert.Name,
					DlTitle = c.Dl.Title
				})
				.ToListAsync(cn);

			return rows
				.Select(c => new OpenOrderRequestDashboardRow
				{
					Id = c.Id ?? 0,
					PurchaseRequestNumber = c.PurchaseRequestNumber,
					PurchaseRequestShamsiDate = ToShamsi(c.PurchaseRequestMiladiDate, c.PurchaseRequestShamsiDate),
					PartCode = c.PartCode,
					PartName = c.PartName,
					PartUnitTitle = c.PartUnitTitle,
					RequiredQty = c.RequiredQty,
					OrderItemComment = c.OrderItemComment,
					NeedShamsiDate = ToShamsi(c.NeedDateMiladiDate, c.NeedDateShamsiDate),
					ProductionOrderNumber = c.ProductionOrderNumber,
					BranchTitle = OrUnknown(c.BranchTitle),
					SalesExpertName = c.SalesExpertName,
					DlTitle = OrUnknown(c.DlTitle),
					Days = DaysUntilToday(c.PurchaseRequestMiladiDate, c.PurchaseRequestShamsiDate)
				})
				.ToList();
		}

		/// <param name="withSupplyComments">
		/// توضیحات تدارکات فقط برای جدول لازم است؛ خلاصه داشبورد بدون آن محاسبه می‌شود.
		/// </param>
		private async Task<List<OpenOrderRequestDashboardRow>> GetStoppedPurchasesRowsAsync(bool withSupplyComments, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
				.Where(c => !c.IsDeleted && c.IsStop)
				.Select(c => new
				{
					c.Id,
					c.PurchaseRequestNumber,
					c.PurchaseRequestMiladiDate,
					c.PurchaseRequestShamsiDate,
					PartCode = c.Part.Code,
					PartName = c.Part.Name,
					PartUnitTitle = c.Part.Unit.Title,
					c.RequiredQty,
					c.OrderItemComment,
					c.NeedDateMiladiDate,
					c.NeedDateShamsiDate,
					c.ProductionOrderNumber,
					BranchTitle = c.ProductionOrder.Branch.Title,
					SalesExpertName = c.SalesUnitSalesExpert.NameFa ?? c.SalesUnitSalesExpert.Name,
					DlTitle = c.Dl.Title,
					c.StopStatus,
					c.StopMiladiDate,
					c.StopShamsiDate
				})
				.ToListAsync(cn);

			var supplyComments = withSupplyComments
				? await GetLastSupplyCommentsAsync(rows.Select(c => c.Id ?? 0).ToList(), cn)
				: new Dictionary<long, (string? ShamsiDate, string? Comment)>();

			return rows
				.Select(c =>
				{
					supplyComments.TryGetValue(c.Id ?? 0, out var supplyComment);
					return new OpenOrderRequestDashboardRow
					{
						Id = c.Id ?? 0,
						PurchaseRequestNumber = c.PurchaseRequestNumber,
						PurchaseRequestShamsiDate = ToShamsi(c.PurchaseRequestMiladiDate, c.PurchaseRequestShamsiDate),
						PartCode = c.PartCode,
						PartName = c.PartName,
						PartUnitTitle = c.PartUnitTitle,
						RequiredQty = c.RequiredQty,
						OrderItemComment = c.OrderItemComment,
						NeedShamsiDate = ToShamsi(c.NeedDateMiladiDate, c.NeedDateShamsiDate),
						ProductionOrderNumber = c.ProductionOrderNumber,
						BranchTitle = OrUnknown(c.BranchTitle),
						SalesExpertName = c.SalesExpertName,
						DlTitle = OrUnknown(c.DlTitle),
						SupplyCommentShamsiDate = supplyComment.ShamsiDate,
						SupplyComment = supplyComment.Comment,
						StopStatusTitle = c.StopStatus?.ToDisplay(),
						Days = DaysUntilToday(c.StopMiladiDate, c.StopShamsiDate)
					};
				})
				.ToList();
		}

		/// <summary>
		/// «توضیحات تدارکات» عیناً مطابق ستون tCmtSup نمایه درخواست‌های باز:
		/// آخرین کامنت (بیشترین Id) که ثبت‌کننده آن نقش Sup.OpenOrderRequest.Supply دارد.
		/// RoleIds کاربر به‌صورت JSON ذخیره می‌شود و در LINQ قابل ترجمه نیست، پس نقش در حافظه بررسی می‌شود.
		/// </summary>
		private async Task<Dictionary<long, (string? ShamsiDate, string? Comment)>> GetLastSupplyCommentsAsync(List<long> openOrderRequestIds, CancellationToken cn)
		{
			var result = new Dictionary<long, (string? ShamsiDate, string? Comment)>();
			if (openOrderRequestIds.Count == 0)
				return result;

			var supplyRoleId = await unitOfWork.Repository<Role>().TableNoTracking
				.Where(c => c.Name == SupplyRoleName)
				.Select(c => c.Id)
				.FirstOrDefaultAsync(cn);

			if (!supplyRoleId.HasValue)
				return result;

			var comments = await unitOfWork.Repository<OpenOrderRequestComment>().TableNoTracking
				.Where(c => openOrderRequestIds.Contains(c.OpenOrderRequestId) && c.CreatedById != null)
				.Select(c => new
				{
					c.Id,
					c.OpenOrderRequestId,
					c.CreatedById,
					c.CommentValue,
					c.ShamsiDate,
					c.CreatedOnShamsiDateTime
				})
				.ToListAsync(cn);

			var creatorIds = comments.Select(c => c.CreatedById).Distinct().ToList();

			var supplyUserIds = (await db.Set<User>().AsNoTracking()
					.Where(c => creatorIds.Contains(c.Id))
					.Select(c => new { c.Id, c.RoleIds })
					.ToListAsync(cn))
				.Where(c => c.RoleIds.Contains(supplyRoleId.Value))
				.Select(c => c.Id)
				.ToHashSet();

			foreach (var group in comments.Where(c => supplyUserIds.Contains(c.CreatedById)).GroupBy(c => c.OpenOrderRequestId))
			{
				var last = group.OrderByDescending(c => c.Id).First();
				var shamsiDate = last.ShamsiDate
					?? (last.CreatedOnShamsiDateTime?.Length > 10 ? last.CreatedOnShamsiDateTime[..10] : last.CreatedOnShamsiDateTime);
				result[group.Key] = (shamsiDate, last.CommentValue);
			}

			return result;
		}

		#endregion

		#region Helpers

		private bool CanView(string actionName)
		{
			if (IsAdministrator)
				return true;

			var path = RoutePrefix + actionName;
			return sdk.CurrentUser?.RoleAccess?.Any(c => string.Equals(c.Path, path, StringComparison.OrdinalIgnoreCase)) ?? false;
		}

		private static AnalyticalDashboardSummaryItem Summarize(string key, IEnumerable<int?> days)
		{
			var allDays = days.ToList();
			var knownDays = allDays.Where(c => c.HasValue).Select(c => c!.Value).ToList();

			return new AnalyticalDashboardSummaryItem
			{
				Key = key,
				Count = allDays.Count,
				AverageDays = knownDays.Count > 0 ? Math.Round((decimal)knownDays.Average(), 1) : null,
				MaxDays = knownDays.Count > 0 ? knownDays.Max() : null
			};
		}

		/// <summary>
		/// معادل DATEDIFF(DAY, date, GETDATE()) در نمایه‌ها: اختلاف روز تقویمی تا امروز.
		/// رکوردهای منتقل‌شده از HTS گاهی فقط تاریخ شمسی دارند (مثلاً StopShamsiDate بدون StopMiladiDate)،
		/// پس اگر تاریخ میلادی خالی بود از تاریخ شمسی استفاده می‌شود.
		/// </summary>
		private static int? DaysUntilToday(DateTime? miladi, string? shamsi)
		{
			var date = miladi ?? ParseShamsiDate(shamsi);
			return date.HasValue ? (DateTime.Today - date.Value.Date).Days : null;
		}

		private static string? ToShamsi(DateTime? miladi, string? shamsi)
			=> miladi?.ToShamsiDate() ?? shamsi;

		private static string OrUnknown(string? value)
			=> string.IsNullOrWhiteSpace(value) ? UnknownTitle : value;

		/// <summary>
		/// DateTimeExtensions.ToMiladiDate استفاده نمی‌شود چون رشته «1405/01/01» را سال میلادی ۱۴۰۵ می‌پذیرد
		/// (همان دلیل TimelyDeliveryReportService.ParseShamsiDate).
		/// </summary>
		private static DateTime? ParseShamsiDate(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			var datePart = NormalizeDigits(value.Trim())
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.FirstOrDefault();

			var parts = datePart?.Split(['/', '-'], StringSplitOptions.RemoveEmptyEntries);
			if (parts is not { Length: 3 }
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

		#endregion
	}
}
