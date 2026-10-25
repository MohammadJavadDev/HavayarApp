using System.Globalization;
using Data;
using Data.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Services.Rpr
{
	/// <summary>
	/// معادل HTS WriteData.Import_Rpr_RepairRequest_Part و Update_Rpr_RepairRequest_Part در حالت راهکاران.
	/// ردیف از درخواست کالا ساخته می‌شود و با کد تفصیل شناسنامه وصل می‌شود.
	/// </summary>
	public class RepairRequestPartImporter(ApplicationDbContext db) : IRepairRequestPartImporter
	{
		public async Task<int> ImportAsync(long? dlId = null, CancellationToken cancellationToken = default)
		{
			db.Database.SetCommandTimeout(0);

			if (!await LinkedServerExistsAsync("ERPS", cancellationToken)
				|| !await LinkedServerExistsAsync("TMS", cancellationToken))
				return 0;

			var minFiscalYear = MinHamkaranFiscalYear();
			var dlSql = dlId is > 0
				? dlId.Value.ToString(CultureInfo.InvariantCulture)
				: "NULL";

			var sql = $"""
				SET NOCOUNT ON;

				DECLARE @dlId BIGINT = {dlSql};
				DECLARE @minFiscalYear INT = {minFiscalYear.ToString(CultureInfo.InvariantCulture)};
				DECLARE @Now DATETIME2 = SYSDATETIME();
				DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
				DECLARE @Seed NVARCHAR(80) = N'job-rpr-repair-request-part';
				DECLARE @Inserted INT = 0;

				IF OBJECT_ID('tempdb..#RepairDl') IS NOT NULL DROP TABLE #RepairDl;
				IF OBJECT_ID('tempdb..#Req') IS NOT NULL DROP TABLE #Req;
				IF OBJECT_ID('tempdb..#Price') IS NOT NULL DROP TABLE #Price;
				IF OBJECT_ID('tempdb..#Matched') IS NOT NULL DROP TABLE #Matched;
				IF OBJECT_ID('tempdb..#Returns') IS NOT NULL DROP TABLE #Returns;

				DELETE n
				FROM Rpr.RepairRequestPart n
				INNER JOIN Rpr.RepairRequestPart old
					ON old.RepairRequestId = n.RepairRequestId
				   AND old.Id <> n.Id
				   AND old.HtsId <> 0
				INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_PartRequestItem] pri
					ON pri.Id = old.HtsPartRequestItemId
				   AND pri.RequestItemId = n.RahkaranPartRequestItemId
				WHERE n.HtsId = 0
				  AND n.CreatedByName = @Seed
				  AND n.RahkaranPartRequestItemId IS NOT NULL;

				;WITH Backfill AS (
					SELECT rp.Id,
					       pri.RequestItemId,
					       ROW_NUMBER() OVER (
					           PARTITION BY rp.RepairRequestId, pri.RequestItemId
					           ORDER BY rp.Id) AS rn
					FROM Rpr.RepairRequestPart rp
					INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_PartRequestItem] pri
						ON pri.Id = rp.HtsPartRequestItemId
					WHERE rp.RahkaranPartRequestItemId IS NULL
					  AND rp.HtsPartRequestItemId IS NOT NULL
					  AND pri.RequestItemId IS NOT NULL
				)
				UPDATE rp
				SET rp.RahkaranPartRequestItemId = b.RequestItemId
				FROM Rpr.RepairRequestPart rp
				INNER JOIN Backfill b ON b.Id = rp.Id
				WHERE b.rn = 1
				  AND NOT EXISTS (
					  SELECT 1
					  FROM Rpr.RepairRequestPart other
					  WHERE other.Id <> rp.Id
					    AND other.RepairRequestId = rp.RepairRequestId
					    AND other.RahkaranPartRequestItemId = b.RequestItemId);

				SELECT rr.Id AS RepairRequestId,
				       d.Id AS DlId,
				       TRY_CONVERT(BIGINT, LTRIM(RTRIM(d.Code))) AS DlCode
				INTO #RepairDl
				FROM Rpr.RepairRequest rr
				INNER JOIN FIN.DL d ON d.Id = rr.DlId
				WHERE TRY_CONVERT(BIGINT, LTRIM(RTRIM(d.Code))) IS NOT NULL
				  AND (@dlId IS NULL OR rr.DlId = @dlId);

				;WITH ReqRaw AS (
					SELECT pri.PartRequestItemID,
					       pr.DateOfNeed,
					       TRY_CONVERT(BIGINT, LTRIM(RTRIM(dl.Code))) AS DlCode,
					       p.Code AS PartCode,
					       p.PartID AS PartRef,
					       pri.Quantity,
					       LEFT(pri.Description, 1024) AS ItemComment,
					       LEFT(CAST(pr.Number AS NVARCHAR(64)), 64) AS RequestNo,
					       ROW_NUMBER() OVER (
					           PARTITION BY pri.PartRequestItemID
					           ORDER BY dl.Code) AS rn
					FROM [ERPS].[Erps].[LGS3].[PartRequestItem] pri
					INNER JOIN [ERPS].[Erps].[LGS3].[PartRequest] pr ON pr.PartRequestID = pri.PartRequestRef
					INNER JOIN [ERPS].[Erps].[LGS3].[Part] p ON p.PartID = pri.PartRef
					INNER JOIN [ERPS].[Erps].[FIN3].[DL] dl
						ON dl.ReferenceID = pr.CounterpartEntityRef
					   AND dl.EntityCode = pr.CounterpartEntityCode
					WHERE pr.FiscalYearRef >= @minFiscalYear
					  AND pr.DateOfNeed >= '2023-03-21'
					  AND TRY_CONVERT(BIGINT, LTRIM(RTRIM(dl.Code))) IS NOT NULL
					  AND TRY_CONVERT(BIGINT, LTRIM(RTRIM(dl.Code))) IN (SELECT DlCode FROM #RepairDl)
				)
				SELECT PartRequestItemID, DateOfNeed, DlCode, PartCode, PartRef, Quantity, ItemComment, RequestNo
				INTO #Req
				FROM ReqRaw
				WHERE rn = 1;

				SELECT ip.Hamkaran_Part_FK, MAX(lastPrice.UnitPrice) AS BuyPrice
				INTO #Price
				FROM [TMS].[TotalSystem].[dbo].[Inv_Part] ip
				INNER JOIN (SELECT DISTINCT PartRef FROM #Req) r ON r.PartRef = ip.Hamkaran_Part_FK
				LEFT JOIN [TMS].[TotalSystem].[dbo].[Vw_Bom_PartLastPrice] lastPrice ON lastPrice.Part_FK = ip.Part_ID
				GROUP BY ip.Hamkaran_Part_FK;

				;WITH MatchedRaw AS (
					SELECT rd.RepairRequestId,
					       rd.DlId,
					       q.PartRequestItemID,
					       q.DateOfNeed,
					       q.Quantity,
					       q.ItemComment,
					       q.RequestNo,
					       part.Id AS PartId,
					       part.UnitId AS PartUnitId,
					       LEFT(pu.Title, 1024) AS UnitName,
					       price.BuyPrice,
					       ROW_NUMBER() OVER (
					           PARTITION BY rd.RepairRequestId, q.PartRequestItemID
					           ORDER BY part.Id) AS rn
					FROM #Req q
					INNER JOIN #RepairDl rd ON rd.DlCode = q.DlCode
					CROSS APPLY (
						SELECT TOP (1) p.Id, p.UnitId
						FROM Inv.Part p
						WHERE LTRIM(RTRIM(p.Code)) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(q.PartCode)) COLLATE DATABASE_DEFAULT
						ORDER BY CASE WHEN p.HamkaranId = q.PartRef THEN 0 ELSE 1 END, p.Id
					) part
					LEFT JOIN Inv.PartUnit pu ON pu.Id = part.UnitId
					LEFT JOIN #Price price ON price.Hamkaran_Part_FK = q.PartRef
				)
				SELECT RepairRequestId, DlId, PartRequestItemID, DateOfNeed, Quantity, ItemComment, RequestNo,
				       PartId, PartUnitId, UnitName, BuyPrice
				INTO #Matched
				FROM MatchedRaw
				WHERE rn = 1;

				INSERT INTO Rpr.RepairRequestPart (
					HtsId, RepairRequestId, PartId, PartUnitId, DlId,
					DamagedPart, StockName, UnitName, UsedMount, ReturnMount,
					BuyPrice, SalePrice, RequestFromStockMiladiDate, RequestFromStockShamsiDate,
					Description, ReplacementPartDescription, HtsPartRequestItemId, RahkaranPartRequestItemId, HtsHamkaranInvVchItemId,
					RahkaranRequestNo, RahkaranDescription,
					CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
					ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
					IsActive)
				SELECT
					0,
					s.RepairRequestId,
					s.PartId,
					s.PartUnitId,
					s.DlId,
					0,
					NULL,
					s.UnitName,
					CAST(ISNULL(s.Quantity, 0) AS DECIMAL(18, 2)),
					0,
					TRY_CONVERT(BIGINT, s.BuyPrice),
					0,
					CAST(s.DateOfNeed AS DATE),
					LEFT(dbo.ToShamsiDate(CAST(s.DateOfNeed AS DATE), '/'), 10),
					s.ItemComment,
					NULL,
					NULL,
					s.PartRequestItemID,
					0,
					s.RequestNo,
					s.ItemComment,
					1, @Seed, @Now, @NowShamsi,
					1, @Seed, @Now, @NowShamsi,
					1
				FROM #Matched s
				WHERE NOT EXISTS (
					SELECT 1
					FROM Rpr.RepairRequestPart existing
					WHERE existing.RepairRequestId = s.RepairRequestId
					  AND existing.RahkaranPartRequestItemId = s.PartRequestItemID)
				  AND NOT EXISTS (
					SELECT 1
					FROM Rpr.RepairRequestPart existing
					INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_PartRequestItem] pri
						ON pri.Id = existing.HtsPartRequestItemId
					WHERE existing.RepairRequestId = s.RepairRequestId
					  AND pri.RequestItemId = s.PartRequestItemID);

				SET @Inserted = @@ROWCOUNT;

				UPDATE rp
				SET rp.PartId = s.PartId,
				    rp.PartUnitId = s.PartUnitId,
				    rp.UnitName = s.UnitName,
				    rp.UsedMount = CAST(ISNULL(s.Quantity, 0) AS DECIMAL(18, 2)),
				    rp.ModifiedById = 1,
				    rp.ModifiedByName = @Seed,
				    rp.ModifiedDateMiladiDateTime = @Now,
				    rp.ModifiedDateShamsiDateTime = @NowShamsi
				FROM Rpr.RepairRequestPart rp
				INNER JOIN #Matched s
					ON s.RepairRequestId = rp.RepairRequestId
				   AND s.PartRequestItemID = rp.RahkaranPartRequestItemId
				WHERE ISNULL(rp.PartId, 0) <> s.PartId
				   OR ISNULL(rp.PartUnitId, 0) <> ISNULL(s.PartUnitId, 0)
				   OR ISNULL(rp.UnitName, N'') COLLATE DATABASE_DEFAULT <> ISNULL(s.UnitName, N'') COLLATE DATABASE_DEFAULT
				   OR rp.UsedMount <> CAST(ISNULL(s.Quantity, 0) AS DECIMAL(18, 2));

				UPDATE rp
				SET rp.RahkaranRequestNo = LEFT(CAST(pr.Number AS NVARCHAR(64)), 64),
				    rp.RahkaranDescription = LEFT(pri.Description, 1024)
				FROM Rpr.RepairRequestPart rp
				INNER JOIN [ERPS].[Erps].[LGS3].[PartRequestItem] pri
					ON pri.PartRequestItemID = rp.RahkaranPartRequestItemId
				INNER JOIN [ERPS].[Erps].[LGS3].[PartRequest] pr ON pr.PartRequestID = pri.PartRequestRef
				WHERE rp.RahkaranPartRequestItemId IS NOT NULL
				  AND (rp.RahkaranRequestNo IS NULL AND rp.RahkaranDescription IS NULL);

				-- ردیف‌های قدیمی (HTS) که شناسه‌شان در راهکاران نیست: از TotalSystem
				UPDATE rp
				SET rp.RahkaranRequestNo = LEFT(CAST(t.RequestNo AS NVARCHAR(64)), 64),
				    rp.RahkaranDescription = LEFT(t.ItemComment, 1024)
				FROM Rpr.RepairRequestPart rp
				INNER JOIN (
					SELECT RequestItemId, MAX(RequestNo) AS RequestNo, MAX(ItemComment) AS ItemComment
					FROM [TMS].[TotalSystem].[dbo].[Inv_PartRequestItem]
					GROUP BY RequestItemId) t ON t.RequestItemId = rp.RahkaranPartRequestItemId
				WHERE rp.RahkaranRequestNo IS NULL AND rp.RahkaranDescription IS NULL;

				SELECT pri.PartRequestItemID,
				       SUM(CASE WHEN TRY_CONVERT(INT, spec.Code) = 7 THEN ret.Quantity ELSE 0 END) AS ReturnQty,
				       MAX(store.Name) AS StockName
				INTO #Returns
				FROM [ERPS].[Erps].[LGS3].[InventoryVoucherItem] ret
				INNER JOIN [ERPS].[Erps].[LGS3].[InventoryVoucherSpecification] spec
					ON spec.InventoryVoucherSpecificationID = ret.InventoryVoucherSpecificationRef
				INNER JOIN [ERPS].[Erps].[LGS3].[InventoryVoucherItem] baseItem
					ON baseItem.InventoryVoucherItemID = ret.ReturnableVoucherItemRef
				INNER JOIN [ERPS].[Erps].[LGS3].[PartRequestItem] pri
					ON pri.PartRequestItemID = baseItem.ReferenceRef
				LEFT JOIN [ERPS].[Erps].[LGS3].[Store] store ON store.StoreID = ret.StoreRef
				WHERE ret.[State] = 2
				  AND ret.InventoryVoucherSpecificationRef IN (7, 24)
				  AND ret.FiscalYearRef >= 102
				  AND pri.PartRequestItemID IN (
					  SELECT rp.RahkaranPartRequestItemId
					  FROM Rpr.RepairRequestPart rp
					  INNER JOIN #RepairDl rd ON rd.RepairRequestId = rp.RepairRequestId
					  WHERE rp.RahkaranPartRequestItemId IS NOT NULL)
				GROUP BY pri.PartRequestItemID;

				UPDATE rp
				SET rp.ReturnMount = CAST(ISNULL(r.ReturnQty, 0) AS DECIMAL(18, 2)),
				    rp.StockName = LEFT(r.StockName, 1024),
				    rp.ModifiedById = 1,
				    rp.ModifiedByName = @Seed,
				    rp.ModifiedDateMiladiDateTime = @Now,
				    rp.ModifiedDateShamsiDateTime = @NowShamsi
				FROM Rpr.RepairRequestPart rp
				INNER JOIN #RepairDl rd ON rd.RepairRequestId = rp.RepairRequestId
				INNER JOIN #Returns r ON r.PartRequestItemID = rp.RahkaranPartRequestItemId
				WHERE rp.ReturnMount <> CAST(ISNULL(r.ReturnQty, 0) AS DECIMAL(18, 2))
				   OR ISNULL(rp.StockName, N'') COLLATE DATABASE_DEFAULT <> ISNULL(LEFT(r.StockName, 1024), N'') COLLATE DATABASE_DEFAULT;

				SELECT @Inserted AS [Value];
				""";

			var rows = await db.Database.SqlQueryRaw<int>(sql).ToListAsync(cancellationToken);
			return rows.FirstOrDefault();
		}

		private static int MinHamkaranFiscalYear()
		{
			var persianYear = new PersianCalendar().GetYear(DateTime.Now) - 2;
			return persianYear <= 1399 ? persianYear % 100 : (persianYear - 1399) + 99;
		}

		private async Task<bool> LinkedServerExistsAsync(string name, CancellationToken cn)
		{
			var rows = await db.Database.SqlQueryRaw<int>(
					$"SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.servers WHERE name = N'{name}' AND is_linked = 1) THEN 1 ELSE 0 END AS [Value]")
				.ToListAsync(cn);
			return rows.FirstOrDefault() == 1;
		}
	}
}
