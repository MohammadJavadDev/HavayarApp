using Data;
using Data.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Services.Cng
{
	/// <summary>
	/// معادل HTS WriteData.Import_Cng_Repairing_UsedPart + ReadData_Hamkaran.GetAll_Cng_Repairing_UsedPart (Rahkaran).
	/// فقط ردیف‌های جدید با HamkaranInvVchItmFk درج می‌شوند (بدون به‌روزرسانی).
	/// </summary>
	public class CngRepairsUsedPartImporter(ApplicationDbContext db) : ICngRepairsUsedPartImporter
	{
		public async Task<int> ImportAsync(CancellationToken cancellationToken = default)
		{
			db.Database.SetCommandTimeout(0);

			if (!await LinkedServerExistsAsync("ERPS", cancellationToken)
				|| !await LinkedServerExistsAsync("TMS", cancellationToken))
				return 0;

			var sql = """
				SET NOCOUNT ON;

				IF OBJECT_ID('tempdb..#HtsPartLastPriceTemp') IS NOT NULL DROP TABLE #HtsPartLastPriceTemp;
				IF OBJECT_ID('tempdb..#RepairDlTemp') IS NOT NULL DROP TABLE #RepairDlTemp;
				IF OBJECT_ID('tempdb..#HamkaranSrc') IS NOT NULL DROP TABLE #HamkaranSrc;

				DECLARE @Now DATETIME2 = SYSDATETIME();
				DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
				DECLARE @Seed NVARCHAR(80) = N'job-cng-repairs-usedpart';
				DECLARE @Inserted INT = 0;

				SELECT lastPrice.Hamkaran_Part_FK,
				       lastPrice.UnitPrice AS BuyPrice,
				       lastPrice.CreatedDate AS LastBuyPriceDate_Shamsi
				INTO #HtsPartLastPriceTemp
				FROM [TMS].[TotalSystem].[dbo].[Inv_Part] ip
				LEFT JOIN [TMS].[TotalSystem].[dbo].[Vw_Bom_PartLastPrice] lastPrice ON lastPrice.Part_FK = ip.Part_ID;

				SELECT r.Id AS RepairsId,
				       d.Id AS DlId,
				       TRY_CONVERT(INT, LTRIM(RTRIM(d.Code))) AS HamkaranDlCode
				INTO #RepairDlTemp
				FROM Cng.Repairs r
				INNER JOIN FIN.DL d ON d.Id = r.DlId
				WHERE TRY_CONVERT(INT, LTRIM(RTRIM(d.Code))) IS NOT NULL;

				;WITH Src AS (
					SELECT vchItm.InventoryVoucherItemID AS VchItmID,
					       TRY_CONVERT(INT, LTRIM(RTRIM(CAST(vchItm.CounterpartDLCode AS NVARCHAR(32))))) AS DlRef,
					       vchItm.PartRef,
					       CASE WHEN vchItm.InventoryVoucherSpecificationRef = 53 THEN vchItm.Quantity ELSE 0 END AS Qty,
					       CASE WHEN vchItm.InventoryVoucherSpecificationRef = 7 THEN vchItm.Quantity ELSE 0 END AS ReturnQty,
					       part.MajorUnitRef AS MunitRef,
					       vchItm.StoreRef AS StockRef,
					       stock.[Name] AS StockName,
					       unit.[Name] AS UnitName,
					       lp.BuyPrice,
					       lp.LastBuyPriceDate_Shamsi,
					       rd.RepairsId,
					       rd.DlId,
					       ROW_NUMBER() OVER (
					           PARTITION BY vchItm.InventoryVoucherItemID
					           ORDER BY rd.RepairsId) AS rn
					FROM [ERPS].[Erps].[LGS3].[InventoryVoucherItem] AS vchItm
					LEFT JOIN [ERPS].[Erps].[LGS3].[Store] stock ON stock.StoreID = vchItm.StoreRef
					LEFT JOIN [ERPS].[Erps].[LGS3].[Part] part ON vchItm.PartRef = part.PartID
					LEFT JOIN [ERPS].[Erps].[LGS3].[PartUnit] unit
						ON unit.PartUnitID = part.MajorUnitRef
					INNER JOIN #RepairDlTemp rd
						ON rd.HamkaranDlCode = TRY_CONVERT(INT, LTRIM(RTRIM(CAST(vchItm.CounterpartDLCode AS NVARCHAR(32)))))
					LEFT JOIN #HtsPartLastPriceTemp lp ON lp.Hamkaran_Part_FK = vchItm.PartRef
					WHERE vchItm.InventoryVoucherSpecificationRef IN (7, 53, 22)
					  AND vchItm.CounterpartDLCode IS NOT NULL
				)
				SELECT VchItmID, DlRef, PartRef, Qty, ReturnQty, MunitRef, StockRef, StockName, UnitName,
				       BuyPrice, LastBuyPriceDate_Shamsi, RepairsId, DlId
				INTO #HamkaranSrc
				FROM Src
				WHERE rn = 1;

				INSERT INTO Cng.RepairsPart (
					HtsId, RepairsId, DlId, HamkaranDlFk, PartId, PartUnitId, HamkaranInvVchItmFk,
					DamagedPart, DamagedPartDeliveryFormNumber, StockRef, StockName, UnitName,
					UsedMount, ReturnMount, BuyPrice, LastBuyPriceDateShamsi, SalePrice,
					CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
					ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
					IsActive)
				SELECT
					0,
					s.RepairsId,
					s.DlId,
					s.DlRef,
					p.Id,
					COALESCE(pu.Id, p.UnitId),
					s.VchItmID,
					1,
					NULL,
					s.StockRef,
					s.StockName,
					s.UnitName,
					CAST(ISNULL(s.Qty, 0) AS DECIMAL(18, 2)),
					CAST(ISNULL(s.ReturnQty, 0) AS DECIMAL(18, 2)),
					TRY_CONVERT(BIGINT, s.BuyPrice),
					LEFT(NULLIF(LTRIM(RTRIM(CAST(s.LastBuyPriceDate_Shamsi AS NVARCHAR(80)))), N''), 80),
					NULL,
					1, @Seed, @Now, @NowShamsi,
					1, @Seed, @Now, @NowShamsi,
					1
				FROM #HamkaranSrc s
				INNER JOIN Inv.Part p ON p.HamkaranId = s.PartRef
				LEFT JOIN Inv.PartUnit pu ON pu.HamkaranId = s.MunitRef
				WHERE NOT EXISTS (
					SELECT 1 FROM Cng.RepairsPart existing
					WHERE existing.HamkaranInvVchItmFk = s.VchItmID
					  AND existing.HamkaranInvVchItmFk IS NOT NULL);

				SET @Inserted = @@ROWCOUNT;
				SELECT @Inserted AS [Value];
				""";

			var rows = await db.Database.SqlQueryRaw<int>(sql).ToListAsync(cancellationToken);
			return rows.FirstOrDefault();
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
