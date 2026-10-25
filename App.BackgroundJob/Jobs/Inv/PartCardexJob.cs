using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Inv;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using Services.ProductionOrderServices;
using System.Globalization;
using System.Net;
using System.Text;

namespace App.BackgroundJob.Jobs.Inv
{
	/// <summary>
	/// معادل HTS <c>HtsTaskService.InvSystemTask</c> / <c>ModulesTask.RunInvSystemTasks</c>:
	///   1) WriteData.Import_InvPartCardex(year)        → Inv.PartCardex   (شاخه Rahkaran)
	///   2) WriteData.Import_InvVchHeader(years)        → Inv.VchHeader    (شاخه Rahkaran)
	///   3) InvVchHeaderService.SendNewItemsNotifications()  → Notification (ایمیل)
	///   4) WriteData.UpdateSaleOrderPoint()            → Sale.OrderPoint
	/// ترتیب اجرا دقیقاً مثل HTS است.
	/// </summary>
	public class PartCardexJob(ApplicationDbContext dbContext, IUnitOfWork unitOfWork)
	{
		private static readonly int[] NotifyStockHtsIds = { 10, 29, 32, 34, 45 };

		// Inv_VchType.VchType_ID (= Inv.VchType.Id) of the document types HTS notifies about
		private static readonly long[] NotifyVchTypeIds = { 2, 3, 4, 5, 6, 7, 9, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 27, 46, 41, 25 };

		// معادل GetReceivers(SystemPage.Nothing) / GetCcCReceivers در HTS
		private static readonly string[] NotifyReceivers =
		{
			"Golestaneh.m", "Zafarkia.m", "Shahpordeli.s", "Yaltaghian.f", "Sarmadi.p", "Gholamifard.m", "Azadbakhsh.s",
			"Moradmand.a", "Mohammadi.ma", "Azadbakhsh.sh", "Salim.sh", "Gavan.a", "Mokhtari.a", "Vatanchi.a", "Ahsani.e",
			"Tafakor.s", "Shokouhmandi.b", "kaboli.f", "Ashrafi.m", "Abdollahi.a", "Sharifi.b", "Saemian.r", "bagheri.h",
			"Razaghmanesh.z", "sohrabi.za"
		};
		private static readonly string[] NotifyCc = { "Kargar.m", "Fazeli.e" };
		private const string EmailSuffix = "@havayar.com";
		private const string NotificationTitle = "اعلان صدور سند انبار";

		[JobHandler("همگام‌سازی کاردکس کالا، اسناد انبار، اعلان سند جدید و نقطه سفارش (معادل InvSystemTask)")]
		public async Task RunInvSystemTasks(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var now = DateTime.Now;
			if (now.Hour < 6 || now.Hour > 21)
			{
				await jobLogger?.LogInfoAsync("خارج از بازه ۶ تا ۲۱ — اجرا نشد (مطابق HTS)", cn);
				return;
			}

			dbContext.Database.SetCommandTimeout(0);

			var hamkaranYear = HamkaranYear(DateTime.Now);
			var minYear = hamkaranYear - 1;

			var cardex = await ImportPartCardex(minYear, cn);
			await jobLogger?.LogInfoAsync($"کاردکس کالا همگام شد (از سال {minYear}): جدید/تغییر/حذف = {cardex}", cn);

			var headers = await ImportVchHeader(minYear, cn);
			await jobLogger?.LogInfoAsync($"هدر اسناد انبار همگام شد (از سال {minYear}): جدید/تغییر/حذف = {headers}", cn);

			var notified = await SendNewItemsNotifications(jobLogger, cn);
			await jobLogger?.LogInfoAsync($"اعلان اسناد جدید انبار: {notified} سند", cn);

			var orderPoints = await UpdateSaleOrderPoint(cn);
			await jobLogger?.LogInfoAsync($"نقطه سفارش جدید: {orderPoints}", cn);
		}

		/// <summary>سال مالی راهکاران = سال شمسی − 1300 (۱۴۰۵ → ۱۰۵)</summary>
		private static int HamkaranYear(DateTime date) => new PersianCalendar().GetYear(date) - 1300;

		// ───────────────────────── 1) PartCardex ─────────────────────────
		private async Task<string> ImportPartCardex(int minYear, CancellationToken cn)
		{
			var sql = $@"
DECLARE @MinYear SMALLINT = {minYear};
DECLARE @Ins INT = 0, @Upd INT = 0, @Del INT = 0;

IF OBJECT_ID('tempdb..#PartCardexTemp') IS NOT NULL DROP TABLE #PartCardexTemp;

SELECT InventoryVoucherItemID AS VchItmId,
       InventoryVoucherID AS VchHdrRef,
       CAST(State AS nvarchar(5)) COLLATE DATABASE_DEFAULT AS [Status],
       StoreRef AS StockRef,
       FiscalYearRef AS [Year],
       CAST([Date] AS date) AS VchDate,
       CounterpartDLCode AS DlRef,
       TypeOfEffectOnStock,
       XValue AS VQtyRatio,
       PartRef,
       RTRIM(VchType) COLLATE DATABASE_DEFAULT AS VchType,
       Quantity AS Qty,
       TRY_CAST(ItemVchNumber AS int) AS VchNum,
       CAST(ItemDescription AS nvarchar(4000)) COLLATE DATABASE_DEFAULT AS ItmDesc
INTO #PartCardexTemp
FROM ERPS.Erps.LGS3.vwCardexPart2
WHERE FiscalYearRef >= @MinYear;

CREATE CLUSTERED INDEX IX_PCT ON #PartCardexTemp (VchItmId);

IF OBJECT_ID('tempdb..#Dl') IS NOT NULL DROP TABLE #Dl;
SELECT TRY_CAST(Code AS bigint) AS Code, MIN(Id) AS Id INTO #Dl FROM FIN.DL WHERE TRY_CAST(Code AS bigint) IS NOT NULL GROUP BY TRY_CAST(Code AS bigint);
CREATE UNIQUE CLUSTERED INDEX IX_Dl ON #Dl (Code);

IF OBJECT_ID('tempdb..#Dates') IS NOT NULL DROP TABLE #Dates;
SELECT d.VchDate, CAST(dbo.fn_MiladiToShamsi(d.VchDate) AS nvarchar(10)) AS ShamsiDate
INTO #Dates FROM (SELECT DISTINCT VchDate FROM #PartCardexTemp) d;
CREATE UNIQUE CLUSTERED INDEX IX_Dates ON #Dates (VchDate);

-- افزودن موارد جدید
INSERT INTO Inv.PartCardex
    (HamkaranId, VchHeaderHamkaranId, [Year], VchType, VchNum, PartId, StockId, VchDateMiladiDate, VchDateShamsiDate,
     DlId, Qty, VQtyRatio, [Status], ItemDescription, IsPermanentVoucher, HtsId, IsActive, CreatedOnMiladiDateTime)
SELECT t.VchItmId, t.VchHdrRef, t.[Year], t.VchType, ISNULL(t.VchNum, 0), part.Id, stock.Id, t.VchDate, dt.ShamsiDate,
       dl.Id, t.Qty, t.VQtyRatio, t.[Status], t.ItmDesc, CASE WHEN t.TypeOfEffectOnStock = 1 THEN 1 ELSE 0 END, 0, 1, SYSDATETIME()
FROM #PartCardexTemp t
JOIN Inv.Part part ON part.HamkaranId = t.PartRef
JOIN Inv.Stock stock ON stock.HamkaranId = t.StockRef
JOIN #Dates dt ON dt.VchDate = t.VchDate
LEFT JOIN #Dl dl ON dl.Code = t.DlRef
WHERE NOT EXISTS (SELECT 1 FROM Inv.PartCardex c WHERE c.HamkaranId = t.VchItmId);
SET @Ins = @@ROWCOUNT;

-- آپدیت تغییرات
UPDATE c SET
    c.[Year] = t.[Year], c.PartId = part.Id, c.StockId = stock.Id, c.VchType = t.VchType, c.VchNum = ISNULL(t.VchNum, 0),
    c.VchDateMiladiDate = t.VchDate, c.VchDateShamsiDate = dt.ShamsiDate, c.DlId = dl.Id, c.Qty = t.Qty,
    c.[Status] = t.[Status], c.VQtyRatio = t.VQtyRatio, c.ItemDescription = t.ItmDesc,
    c.IsPermanentVoucher = CASE WHEN t.TypeOfEffectOnStock = 1 THEN 1 ELSE 0 END,
    c.VchHeaderHamkaranId = t.VchHdrRef,
    c.ModifiedDateMiladiDateTime = SYSDATETIME()
FROM Inv.PartCardex c
JOIN #PartCardexTemp t ON t.VchItmId = c.HamkaranId
JOIN Inv.Part part ON part.HamkaranId = t.PartRef
JOIN Inv.Stock stock ON stock.HamkaranId = t.StockRef
JOIN #Dates dt ON dt.VchDate = t.VchDate
LEFT JOIN #Dl dl ON dl.Code = t.DlRef
WHERE c.[Year] <> t.[Year]
   OR c.PartId <> part.Id
   OR c.StockId <> stock.Id
   OR c.VchType <> t.VchType
   OR c.VchNum <> ISNULL(t.VchNum, 0)
   OR c.VchDateMiladiDate <> t.VchDate
   OR ISNULL(c.DlId, 0) <> ISNULL(dl.Id, 0)
   OR c.Qty <> t.Qty
   OR c.VQtyRatio <> t.VQtyRatio
   OR c.[Status] <> t.[Status]
   OR ISNULL(c.ItemDescription, N'') <> ISNULL(t.ItmDesc, N'')
   OR c.IsPermanentVoucher <> CASE WHEN t.TypeOfEffectOnStock = 1 THEN 1 ELSE 0 END;
SET @Upd = @@ROWCOUNT;

-- حذف موارد ناموجود (فقط سال‌های همگام‌شده؛ ردیف‌هایی که به نقطه سفارش وصل‌اند حذف نمی‌شوند)
DELETE c
FROM Inv.PartCardex c
LEFT JOIN #PartCardexTemp t ON t.VchItmId = c.HamkaranId
WHERE c.[Year] >= @MinYear AND t.VchItmId IS NULL
  AND NOT EXISTS (SELECT 1 FROM Sale.OrderPoint op WHERE op.PartCardexId = c.Id);
SET @Del = @@ROWCOUNT;

SELECT CAST(@Ins AS nvarchar(20)) + N'/' + CAST(@Upd AS nvarchar(20)) + N'/' + CAST(@Del AS nvarchar(20)) AS Value;";

			return await ScalarAsync(sql, cn);
		}

		// ───────────────────────── 2) VchHeader ─────────────────────────
		private async Task<string> ImportVchHeader(int minYear, CancellationToken cn)
		{
			var sql = $@"
DECLARE @MinYear SMALLINT = {minYear};
DECLARE @Ins INT = 0, @Upd INT = 0, @Del INT = 0;

IF OBJECT_ID('tempdb..#VchTemp') IS NOT NULL DROP TABLE #VchTemp;

SELECT x.* INTO #VchTemp FROM (
    SELECT H.InventoryVoucherID AS VchHdrID,
           H.FiscalYearRef AS [Year],
           DS.Code AS VchType,
           H.Number AS VchNum,
           CAST(H.[Date] AS date) AS VchDate,
           H.CounterpartDLCode AS DLREF,
           H.StoreRef AS StockRef,
           CreatorParty.PartyID AS Creator,
           Confirmer.ConfirmerId AS Confirmer,
           CASE WHEN H.State = 3 THEN N'ابطال شـده'
                WHEN H.State = 1 THEN N'ثبت اولـيه'
                WHEN H.State = 2 THEN N'تاييد شـده' END COLLATE DATABASE_DEFAULT AS StatusTitle,
           CAST(BaseVoucherSpecification.Code AS nvarchar(50)) COLLATE DATABASE_DEFAULT AS BaseVchType,
           CAST(H.[Description] AS nvarchar(2048)) COLLATE DATABASE_DEFAULT AS VchDesc,
           CAST(OS.[Name] AS nvarchar(128)) COLLATE DATABASE_DEFAULT AS OpositStockName,
           CAST(OS.Code AS nvarchar(128)) COLLATE DATABASE_DEFAULT AS OpositStockCode,
           CAST(BaseVoucherSpecification.[Name] AS nvarchar(2048)) COLLATE DATABASE_DEFAULT AS BaseVchDesc,
           ROW_NUMBER() OVER (PARTITION BY H.InventoryVoucherID ORDER BY Confirmer.ConfirmerId) AS rn
    FROM ERPS.Erps.LGS3.InventoryVoucher AS H
    JOIN ERPS.Erps.LGS3.Store AS S ON H.StoreRef = S.StoreID
    JOIN ERPS.Erps.LGS3.InventoryVoucherSpecification AS DS ON H.InventoryVoucherSpecificationRef = DS.InventoryVoucherSpecificationID
    LEFT JOIN ERPS.Erps.LGS3.Store AS OS ON H.CounterpartStoreRef = OS.StoreID
    LEFT JOIN ERPS.Erps.SYS3.[User] AS Crs1 ON Crs1.UserID = H.Creator
    LEFT JOIN ERPS.Erps.GNR3.Party AS CreatorParty ON CreatorParty.PartyID = Crs1.PartyRef
    LEFT JOIN ERPS.Erps.LGS3.InventoryVoucher AS BaseVoucher ON BaseVoucher.InventoryVoucherID = H.ReferenceRef
    LEFT JOIN ERPS.Erps.LGS3.InventoryVoucherSpecification AS BaseVoucherSpecification
           ON BaseVoucherSpecification.InventoryVoucherSpecificationID = BaseVoucher.InventoryVoucherSpecificationRef
    LEFT JOIN (
        SELECT StateHistory.RecordID, StateUserParty.PartyID AS ConfirmerId
        FROM ERPS.Erps.SYS3.StateHistory AS StateHistory
        JOIN ERPS.Erps.SYS3.[User] AS StateUser ON StateUser.UserID = StateHistory.ChangingUserID
        JOIN ERPS.Erps.GNR3.Party AS StateUserParty ON StateUserParty.PartyID = StateUser.PartyRef
        WHERE StateHistory.EntityCode = 674 AND StateHistory.TargetState = 2
    ) AS Confirmer ON Confirmer.RecordID = H.InventoryVoucherID
    WHERE DS.Code IN (01, 02, 03, 04, 05, 07, 08, 09, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 22, 23, 24, 51, 52, 53, 55,
                      57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 9901, 9951, 9908, 9964, 9910,
                      9959, 99, 75, 25)
      AND H.FiscalYearRef >= @MinYear
      AND H.Creator IS NOT NULL
) x WHERE x.rn = 1;

CREATE UNIQUE CLUSTERED INDEX IX_VT ON #VchTemp (VchHdrID);

IF OBJECT_ID('tempdb..#Dl') IS NOT NULL DROP TABLE #Dl;
SELECT TRY_CAST(Code AS bigint) AS Code, MIN(Id) AS Id INTO #Dl FROM FIN.DL WHERE TRY_CAST(Code AS bigint) IS NOT NULL GROUP BY TRY_CAST(Code AS bigint);
CREATE UNIQUE CLUSTERED INDEX IX_Dl ON #Dl (Code);

IF OBJECT_ID('tempdb..#Party') IS NOT NULL DROP TABLE #Party;
SELECT HamkaranId, MIN(Id) AS Id INTO #Party FROM Gnr.Party WHERE HamkaranId IS NOT NULL GROUP BY HamkaranId;
CREATE UNIQUE CLUSTERED INDEX IX_Party ON #Party (HamkaranId);

IF OBJECT_ID('tempdb..#Dates') IS NOT NULL DROP TABLE #Dates;
SELECT d.VchDate, CAST(dbo.fn_MiladiToShamsi(d.VchDate) AS nvarchar(10)) AS ShamsiDate
INTO #Dates FROM (SELECT DISTINCT VchDate FROM #VchTemp) d;
CREATE UNIQUE CLUSTERED INDEX IX_Dates ON #Dates (VchDate);

-- افزودن موارد جدید
INSERT INTO Inv.VchHeader
    (HamkaranId, [Year], VchTypeId, VchNum, VchDateMiladiDate, VchDateShamsiDate, DlId, StockId, CreatorId, ConfirmerId,
     [Status], VchDescription, OpositStockName, OpositStockCode, BaseVchType, BaseVchDescription, Comment,
     IsNotified, HtsId, IsActive, CreatedOnMiladiDateTime)
SELECT h.VchHdrID, h.[Year], vt.Id, h.VchNum, h.VchDate, dt.ShamsiDate, dl.Id, stock.Id, cr.Id, cf.Id,
       h.StatusTitle, h.VchDesc, h.OpositStockName, h.OpositStockCode, h.BaseVchType, h.BaseVchDesc, NULL,
       0, 0, 1, SYSDATETIME()
FROM #VchTemp h
JOIN Inv.VchType vt ON vt.HamkaranId = h.VchType
JOIN Inv.Stock stock ON stock.HamkaranId = h.StockRef
JOIN #Dates dt ON dt.VchDate = h.VchDate
LEFT JOIN #Dl dl ON dl.Code = h.DLREF
LEFT JOIN #Party cr ON cr.HamkaranId = h.Creator
LEFT JOIN #Party cf ON cf.HamkaranId = h.Confirmer
WHERE NOT EXISTS (SELECT 1 FROM Inv.VchHeader v WHERE v.HamkaranId = h.VchHdrID);
SET @Ins = @@ROWCOUNT;

-- بروزرسانی تغییرات
UPDATE v SET
    v.[Year] = h.[Year], v.VchTypeId = vt.Id, v.VchNum = h.VchNum, v.VchDateMiladiDate = h.VchDate, v.VchDateShamsiDate = dt.ShamsiDate,
    v.DlId = dl.Id, v.StockId = stock.Id, v.CreatorId = cr.Id, v.ConfirmerId = cf.Id, v.[Status] = h.StatusTitle,
    v.VchDescription = h.VchDesc, v.OpositStockName = h.OpositStockName, v.OpositStockCode = h.OpositStockCode,
    v.BaseVchType = h.BaseVchType, v.BaseVchDescription = h.BaseVchDesc,
    v.ModifiedDateMiladiDateTime = SYSDATETIME()
FROM Inv.VchHeader v
JOIN #VchTemp h ON h.VchHdrID = v.HamkaranId
JOIN Inv.VchType vt ON vt.HamkaranId = h.VchType
JOIN Inv.Stock stock ON stock.HamkaranId = h.StockRef
JOIN #Dates dt ON dt.VchDate = h.VchDate
LEFT JOIN #Dl dl ON dl.Code = h.DLREF
LEFT JOIN #Party cr ON cr.HamkaranId = h.Creator
LEFT JOIN #Party cf ON cf.HamkaranId = h.Confirmer
WHERE v.[Year] <> h.[Year]
   OR v.VchTypeId <> vt.Id
   OR v.VchNum <> h.VchNum
   OR v.VchDateMiladiDate <> h.VchDate
   OR ISNULL(v.DlId, 0) <> ISNULL(dl.Id, 0)
   OR v.StockId <> stock.Id
   OR ISNULL(v.CreatorId, 0) <> ISNULL(cr.Id, 0)
   OR ISNULL(v.ConfirmerId, 0) <> ISNULL(cf.Id, 0)
   OR ISNULL(v.[Status], N'') <> ISNULL(h.StatusTitle, N'')
   OR ISNULL(v.VchDescription, N'') <> ISNULL(h.VchDesc, N'')
   OR ISNULL(v.OpositStockName, N'') <> ISNULL(h.OpositStockName, N'')
   OR ISNULL(v.OpositStockCode, N'') <> ISNULL(h.OpositStockCode, N'')
   OR ISNULL(v.BaseVchType, N'') <> ISNULL(h.BaseVchType, N'')
   OR ISNULL(v.BaseVchDescription, N'') <> ISNULL(h.BaseVchDesc, N'');
SET @Upd = @@ROWCOUNT;

-- حذف موارد ناموجود (فقط سال‌های همگام‌شده — در HTS حذف بدون فیلتر سال بود و به‌خاطر FK بی‌صدا شکست می‌خورد)
DELETE v
FROM Inv.VchHeader v
LEFT JOIN #VchTemp h ON h.VchHdrID = v.HamkaranId
WHERE v.[Year] >= @MinYear AND h.VchHdrID IS NULL;
SET @Del = @@ROWCOUNT;

SELECT CAST(@Ins AS nvarchar(20)) + N'/' + CAST(@Upd AS nvarchar(20)) + N'/' + CAST(@Del AS nvarchar(20)) AS Value;";

			return await ScalarAsync(sql, cn);
		}

		// ─────────────────── 3) اعلان سند جدید (SendNewItemsNotifications) ───────────────────
		private async Task<int> SendNewItemsNotifications(IJobLogger? jobLogger, CancellationToken cn)
		{
			var since = DateTime.Now.AddDays(-7).Date;
			var headers = await dbContext.Set<VchHeader>()
				.Where(h => !h.IsNotified
					&& h.Stock != null && NotifyStockHtsIds.Contains(h.Stock.HtsId)
					&& h.VchDateMiladiDate >= since
					&& NotifyVchTypeIds.Contains(h.VchTypeId))
				.Include(h => h.Stock)
				.Include(h => h.Dl)
				.Include(h => h.Creator)
				.ToListAsync(cn);

			if (headers.Count == 0)
				return 0;

			var items = await LoadVchItems(headers.Select(h => h.HamkaranId), cn);
			if (items.Count == 0)
				return 0;

			var enc = (string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
			var content = new StringBuilder();
			content.Append("احتراماً اسناد جدیدی با مشخصات ذیل صادر گردید");
			content.Append("<br /><br />");
			content.Append("<ul>");

			foreach (var h in headers)
			{
				content.Append($"<li>انبار : <strong> {enc(h.Stock?.Title)} </strong></li>");
				content.Append($"<li>شماره سند : <strong> {h.VchNum} </strong></li>");
				content.Append($"<li>تفضیل : <strong> {enc(h.Dl?.Title)} | {enc(h.Dl?.Code)}</strong></li>");
				content.Append($"<li>وضعیت : <strong> {enc(h.Status)} </strong></li>");
				content.Append($"<li>ایجاد کننده : <strong> {enc(h.Creator?.FullName)} </strong></li>");
				content.Append("<br /><strong>اقلام :</strong><br />");

				foreach (var item in items.Where(i => i.VchHdrRef == h.HamkaranId))
				{
					content.Append($"<li>کد قطعه : <strong> {enc(item.PartCode)} </strong></li>");
					content.Append($"<li>عنوان قطعه : <strong> {enc(item.PartName)} </strong></li>");
					content.Append($"<li>تعداد : <strong> {item.Qty} </strong></li>");
					content.Append($"<li>توضیحات : <strong> {enc(item.Description)} </strong></li>");
					content.Append("<br />");
				}

				content.Append("<br /><br />");
				h.IsNotified = true;
			}

			content.Append("</ul>");
			var bodyHtml = ProductionOrderItemNotificationHelper.BuildEmailShell(content.ToString());

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = NotificationTitle,
				Body = bodyHtml,
				OwnerId = 1,
				ViewPath = "/Panel/Inv/PartCardex/List",
				IsRead = false,
				IsSend = false,
				ToEmails = NotifyReceivers.Select(u => u + EmailSuffix).ToList(),
				CcEmails = NotifyCc.Select(u => u + EmailSuffix).ToList()
			}, cn);

			await dbContext.SaveChangesAsync(cn);
			await unitOfWork.SaveChangesAsync(cn);
			return headers.Count;
		}

		private async Task<List<VchItemRow>> LoadVchItems(IEnumerable<long> headerIds, CancellationToken cn)
		{
			var ids = string.Join(",", headerIds.Distinct());
			var sql = $@"
SELECT VchItem.InventoryVoucherRef AS VchHdrRef,
       CAST(Part.Code AS nvarchar(256)) COLLATE DATABASE_DEFAULT AS PartCode,
       CAST(Part.[Name] AS nvarchar(1024)) COLLATE DATABASE_DEFAULT AS PartName,
       CAST(VchItem.Quantity AS float) AS Qty,
       CAST(VchItem.[Description] AS nvarchar(max)) COLLATE DATABASE_DEFAULT AS [Description]
FROM ERPS.Erps.LGS3.InventoryVoucherItem VchItem
JOIN ERPS.Erps.LGS3.Part Part ON VchItem.PartRef = Part.PartID
WHERE VchItem.InventoryVoucherRef IN ({ids})
ORDER BY VchItem.InventoryVoucherRef, VchItem.InventoryVoucherItemID";
			return await dbContext.Database.SqlQueryRaw<VchItemRow>(sql).ToListAsync(cn);
		}

		private sealed class VchItemRow
		{
			public long VchHdrRef { get; set; }
			public string? PartCode { get; set; }
			public string? PartName { get; set; }
			public double Qty { get; set; }
			public string? Description { get; set; }
		}

		// ─────────────── 4) UpdateSaleOrderPoint ───────────────
		private async Task<int> UpdateSaleOrderPoint(CancellationToken cn)
		{
			// HTS: StockId IN (1,4,17) (Inv_Stock.Stock_ID) و VchDate >= 2020-06-06 و بدون Sale_OrderPoint
			// IsExternal در HTS از Vw_Bom_PartLastPriceSimple می‌آمد؛ معادل جدید Part.[Foreign] است.
			const string sql = @"
INSERT INTO Sale.OrderPoint
    (HtsId, HtsPartCardexId, PartCardexId, AlternativePartId, IsIgnore, IsExternal, Comment,
     CreatedById, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
SELECT 0, 0, c.Id, NULL, 0, ISNULL(p.[Foreign], 0), LEFT(c.ItemDescription, 1024),
       1, CAST(c.VchDateMiladiDate AS datetime2), c.VchDateShamsiDate, 1
FROM Inv.PartCardex c
JOIN Inv.Stock s ON s.Id = c.StockId
JOIN Inv.Part p ON p.Id = c.PartId
WHERE s.HtsId IN (1, 4, 17)
  AND c.VchDateMiladiDate >= '2020-06-06'
  AND NOT EXISTS (SELECT 1 FROM Sale.OrderPoint op WHERE op.PartCardexId = c.Id);
SELECT CAST(@@ROWCOUNT AS nvarchar(20)) AS Value;";
			return int.Parse(await ScalarAsync(sql, cn), CultureInfo.InvariantCulture);
		}

		// ─────────────── helpers ───────────────
		private async Task<string> ScalarAsync(string sql, CancellationToken cn)
		{
			var rows = await dbContext.Database.SqlQueryRaw<ScalarRow>(sql).ToListAsync(cn);
			return rows.FirstOrDefault()?.Value ?? "0";
		}

		private sealed class ScalarRow
		{
			public string? Value { get; set; }
		}
	}
}
