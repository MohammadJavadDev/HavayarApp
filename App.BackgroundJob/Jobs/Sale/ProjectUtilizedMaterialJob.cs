using Common.Attributes;
using Common.Utilities;
using Data;
using Entities.App.Sale;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Sale
{
	/// <summary>
	/// معادل HTS WriteData.Import_ProjectUtilizedMaterial
	/// (HtsTaskService.AfterSalesAndRepairSystemTask — سال جاری شمسی / Hamkaran)
	/// به‌همراه کات‌اور یک‌باره از TotalSystem.
	/// نوع رسید از [TMS]..Inv_VchType خوانده می‌شود (جدول Inv.VchType در هاویار ساخته نمی‌شود).
	/// </summary>
	public class ProjectUtilizedMaterialJob(ApplicationDbContext db)
	{
		[JobHandler("ورود اقلام مصرفی پروژه از راهکاران (سال جاری)")]
		public async Task ImportFromRahkaranCurrentYear(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			db.Database.SetCommandTimeout(0);
			var shamsiYear = DateTime.Now.GetShamsiYear();
			var hamkaranYear = shamsiYear.ToString().GetHamkaranYearNumber();
			await jobLogger?.LogInfoAsync(
				$"شروع ورود اقلام مصرفی پروژه از راهکاران — سال شمسی {shamsiYear} / Hamkaran {hamkaranYear}", cn);

			if (!await LinkedServerExistsAsync("ERPS", cn) || !await LinkedServerExistsAsync("TMS", cn))
			{
				await jobLogger?.LogWarningAsync("Linked Server [ERPS] یا [TMS] یافت نشد — ورود انجام نشد", 0, cn);
				return;
			}

			var before = await db.Set<ProjectUtilizedMaterial>().AsNoTracking().CountAsync(cn);
			var sql = $"""
				SET NOCOUNT ON;
				IF OBJECT_ID('tempdb..#VchTemp') IS NOT NULL DROP TABLE #VchTemp;
				IF OBJECT_ID('tempdb..#EditedData') IS NOT NULL DROP TABLE #EditedData;
				IF OBJECT_ID('tempdb..#NewData') IS NOT NULL DROP TABLE #NewData;

				DECLARE @Year SMALLINT = {hamkaranYear};
				DECLARE @Now DATETIME2 = SYSDATETIME();
				DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
				DECLARE @Seed NVARCHAR(80) = N'job-project-utilized-material';

				SELECT
					invInvVchItm.InventoryVoucherItemID AS VchItmID,
					invInvVchHdr.InventoryVoucherID AS VchHdrID,
					invInvVchHdr.FiscalYearRef AS [Year],
					invInvVchHdr.Number AS VchNum,
					AccDL.Code AS AccNum,
					VchType.Code AS VchType,
					invInvVchHdr.[Date] AS VchDate,
					invInvVchItm.Quantity AS Qty,
					invPart.PartID AS PartSerial
				INTO #VchTemp
				FROM [ERPS].[Erps].[lgs3].[InventoryVoucher] AS invInvVchHdr
				RIGHT JOIN [ERPS].[Erps].[lgs3].[InventoryVoucherItem] AS invInvVchItm
					ON invInvVchHdr.InventoryVoucherID = invInvVchItm.InventoryVoucherRef
				LEFT JOIN [ERPS].[Erps].[FIN3].[DL] AS AccDL
					ON invInvVchItm.CounterpartDLCode = AccDL.Code
				INNER JOIN [ERPS].[Erps].[lgs3].[Part] AS invPart
					ON invInvVchItm.PartRef = invPart.PartID
				JOIN [ERPS].[Erps].[LGS3].[InventoryVoucherSpecification] AS VchType
					ON VchType.InventoryVoucherSpecificationID = invInvVchHdr.InventoryVoucherSpecificationRef
				WHERE AccDL.Code IS NOT NULL
				  AND invInvVchHdr.FiscalYearRef >= @Year;

				SELECT v.*
				INTO #NewData
				FROM #VchTemp v
				LEFT JOIN Sale.ProjectUtilizedMaterial h ON h.VchItemId = v.VchItmID
				WHERE h.Id IS NULL;

				SELECT v.*
				INTO #EditedData
				FROM #VchTemp v
				JOIN Sale.ProjectUtilizedMaterial h ON h.VchItemId = v.VchItmID AND h.[Year] = v.[Year]
				JOIN Inv.Part hp ON hp.Id = h.PartId
				JOIN FIN.DL hd ON hd.Id = h.DLId
				JOIN [TMS].[TotalSystem].[dbo].[Inv_VchType] hv ON hv.VchType_ID = h.VchTypeId
				WHERE hv.Hamkaran_InvVchType_FK <> v.VchType
				   OR hp.HamkaranId <> v.PartSerial
				   OR TRY_CONVERT(INT, LTRIM(RTRIM(hd.Code))) <> TRY_CONVERT(INT, LTRIM(RTRIM(CAST(v.AccNum AS NVARCHAR(32)))))
				   OR h.Qty <> v.Qty
				   OR h.VchMiladiDate <> v.VchDate;

				DECLARE @Inserted INT = 0;
				DECLARE @Updated INT = 0;

				IF EXISTS (SELECT 1 FROM #NewData)
				BEGIN
					INSERT INTO Sale.ProjectUtilizedMaterial (
						VchItemId, VchHdrId, DLId, [Year], VchNum,
						VchMiladiDate, VchDateShamsiDate, VchTypeId, Qty, PartId,
						ReplacedPartId, ReplacedQty, Comment, HtsId, DLTypeId,
						CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
						ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
						IsActive)
					SELECT
						n.VchItmID,
						n.VchHdrID,
						d.Id,
						n.[Year],
						n.VchNum,
						n.VchDate,
						dbo.ToShamsiDate(CAST(n.VchDate AS DATE), '/'),
						CAST(vt.VchType_ID AS TINYINT),
						n.Qty,
						p.Id,
						NULL,
						0,
						NULL,
						0,
						CAST(ISNULL(d.TypeId, 0) AS TINYINT),
						1, @Seed, @Now, @NowShamsi,
						1, @Seed, @Now, @NowShamsi,
						1
					FROM #NewData n
					JOIN Inv.Part p ON p.HamkaranId = n.PartSerial
					JOIN FIN.DL d ON TRY_CONVERT(INT, LTRIM(RTRIM(d.Code))) = TRY_CONVERT(INT, LTRIM(RTRIM(CAST(n.AccNum AS NVARCHAR(32)))))
					JOIN [TMS].[TotalSystem].[dbo].[Inv_VchType] vt ON vt.Hamkaran_InvVchType_FK = n.VchType
					LEFT JOIN Sale.ProjectUtilizedMaterial t
						ON t.VchItemId = n.VchItmID AND t.[Year] = n.[Year] AND t.PartId = p.Id
					WHERE t.Id IS NULL;

					SET @Inserted = @@ROWCOUNT;
				END

				IF EXISTS (SELECT 1 FROM #EditedData)
				BEGIN
					UPDATE pum
					SET
						pum.PartId = p.Id,
						pum.DLId = d.Id,
						pum.VchTypeId = CAST(vt.VchType_ID AS TINYINT),
						pum.VchMiladiDate = e.VchDate,
						pum.VchDateShamsiDate = dbo.ToShamsiDate(CAST(e.VchDate AS DATE), '/'),
						pum.Qty = e.Qty,
						pum.DLTypeId = CAST(ISNULL(d.TypeId, 0) AS TINYINT),
						pum.ModifiedById = 1,
						pum.ModifiedByName = @Seed,
						pum.ModifiedDateMiladiDateTime = @Now,
						pum.ModifiedDateShamsiDateTime = @NowShamsi
					FROM Sale.ProjectUtilizedMaterial pum
					JOIN #EditedData e ON e.VchItmID = pum.VchItemId AND e.[Year] = pum.[Year]
					JOIN Inv.Part p ON p.HamkaranId = e.PartSerial
					JOIN FIN.DL d ON TRY_CONVERT(INT, LTRIM(RTRIM(d.Code))) = TRY_CONVERT(INT, LTRIM(RTRIM(CAST(e.AccNum AS NVARCHAR(32)))))
					JOIN [TMS].[TotalSystem].[dbo].[Inv_VchType] vt ON vt.Hamkaran_InvVchType_FK = e.VchType;

					SET @Updated = @@ROWCOUNT;
				END

				""";

			await db.Database.ExecuteSqlRawAsync(sql, cn);
			var after = await db.Set<ProjectUtilizedMaterial>().AsNoTracking().CountAsync(cn);
			await jobLogger?.LogInfoAsync(
				$"ورود سال جاری تمام شد — تعداد کل ردیف‌ها {after} (قبل {before})", cn);
		}

		[JobHandler("کات‌اور اقلام مصرفی پروژه و سوابق از HTS")]
		public async Task CutoverFromTotalSystem(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			db.Database.SetCommandTimeout(0);
			await jobLogger?.LogInfoAsync("شروع کات‌اور اقلام مصرفی پروژه از TotalSystem", cn);

			if (!await LinkedServerExistsAsync("TMS", cn))
			{
				await jobLogger?.LogWarningAsync("Linked Server [TMS] یافت نشد — کات‌اور انجام نشد", 0, cn);
				return;
			}

			await CreateDlMapTableAsync(cn);
			try
			{
				await DisableSecondaryIndexesAsync(cn);

				var years = await db.Database.SqlQueryRaw<int>(
						"SELECT DISTINCT CAST([Year] AS INT) AS [Value] FROM [TMS].[TotalSystem].[dbo].[Sale_ProjectUtilizedMaterial] ORDER BY [Value]")
					.ToListAsync(cn);

				var totalInserted = 0;
				var totalUpdated = 0;
				var totalSkipped = 0;

				foreach (var year in years)
				{
					cn.ThrowIfCancellationRequested();
					var stats = await RunYearCutoverAsync(year, cn);
					totalInserted += stats.InsertedCnt;
					totalUpdated += stats.UpdatedCnt;
					totalSkipped += stats.SkippedCnt;
					await jobLogger?.LogInfoAsync(
						$"سال {year}: درج {stats.InsertedCnt}، به‌روزرسانی {stats.UpdatedCnt}، بدون نگاشت {stats.SkippedCnt}", cn);
				}

				await RebuildSecondaryIndexesAsync(cn);
				var changes = await SyncHistoryFromTotalSystemAsync(cn);
				await jobLogger?.LogInfoAsync(
					$"کات‌اور تمام شد — مواد درج {totalInserted} / به‌روزرسانی {totalUpdated} / بدون نگاشت {totalSkipped}؛ سوابق {changes}", cn);
			}
			finally
			{
				await DropDlMapTableAsync(cn);
			}
		}

		private async Task<bool> LinkedServerExistsAsync(string name, CancellationToken cn)
		{
			var rows = await db.Database.SqlQueryRaw<int>(
					$"SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.servers WHERE name = N'{name}' AND is_linked = 1) THEN 1 ELSE 0 END AS [Value]")
				.ToListAsync(cn);
			return rows.FirstOrDefault() == 1;
		}

		private async Task CreateDlMapTableAsync(CancellationToken cn)
		{
			await db.Database.ExecuteSqlRawAsync("""
				IF OBJECT_ID(N'dbo._CutoverPumDlMap') IS NOT NULL DROP TABLE dbo._CutoverPumDlMap;
				SELECT ad.Acc_DL_ID AS HtsDlId, dl.Id AS DlId, CAST(ISNULL(dl.TypeId, 0) AS TINYINT) AS DlTypeId
				INTO dbo._CutoverPumDlMap
				FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
				INNER JOIN FIN.DL dl ON TRY_CONVERT(INT, LTRIM(RTRIM(dl.Code))) = ad.Hamkaran_Acc_DL_FK;
				CREATE UNIQUE CLUSTERED INDEX IX_CutoverPumDlMap ON dbo._CutoverPumDlMap (HtsDlId);
				""", cn);
		}

		private Task DropDlMapTableAsync(CancellationToken cn) =>
			db.Database.ExecuteSqlRawAsync("""
				IF OBJECT_ID(N'dbo._CutoverPumDlMap') IS NOT NULL DROP TABLE dbo._CutoverPumDlMap;
				""", cn);

		private Task DisableSecondaryIndexesAsync(CancellationToken cn) =>
			db.Database.ExecuteSqlRawAsync("""
				IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_DLId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
					ALTER INDEX IX_ProjectUtilizedMaterial_DLId ON Sale.ProjectUtilizedMaterial DISABLE;
				IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_DLTypeId1' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
					ALTER INDEX IX_ProjectUtilizedMaterial_DLTypeId1 ON Sale.ProjectUtilizedMaterial DISABLE;
				IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_PartId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
					ALTER INDEX IX_ProjectUtilizedMaterial_PartId ON Sale.ProjectUtilizedMaterial DISABLE;
				IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_ReplacedPartId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
					ALTER INDEX IX_ProjectUtilizedMaterial_ReplacedPartId ON Sale.ProjectUtilizedMaterial DISABLE;
				""", cn);

		private Task RebuildSecondaryIndexesAsync(CancellationToken cn) =>
			db.Database.ExecuteSqlRawAsync("""
				IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_DLId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
					ALTER INDEX IX_ProjectUtilizedMaterial_DLId ON Sale.ProjectUtilizedMaterial REBUILD;
				IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_DLTypeId1' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
					ALTER INDEX IX_ProjectUtilizedMaterial_DLTypeId1 ON Sale.ProjectUtilizedMaterial REBUILD;
				IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_PartId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
					ALTER INDEX IX_ProjectUtilizedMaterial_PartId ON Sale.ProjectUtilizedMaterial REBUILD;
				IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_ReplacedPartId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
					ALTER INDEX IX_ProjectUtilizedMaterial_ReplacedPartId ON Sale.ProjectUtilizedMaterial REBUILD;
				IF NOT EXISTS (
					SELECT 1 FROM sys.indexes
					WHERE name = N'IX_Sale_ProjectUtilizedMaterial_VchItemYear'
					  AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
				BEGIN
					CREATE NONCLUSTERED INDEX IX_Sale_ProjectUtilizedMaterial_VchItemYear
					ON Sale.ProjectUtilizedMaterial (VchItemId, [Year]);
				END
				""", cn);

		private async Task<CutoverYearRow> RunYearCutoverAsync(int year, CancellationToken cn)
		{
			var sql = $"""
				SET NOCOUNT ON;
				DECLARE @Year INT = {year};
				DECLARE @Now DATETIME2 = SYSDATETIME();
				DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
				DECLARE @Seed NVARCHAR(80) = N'sync-project-utilized-material';

				IF OBJECT_ID('tempdb..#PumSrc') IS NOT NULL DROP TABLE #PumSrc;
				SELECT
					CAST(s.Id AS BIGINT) AS HtsId,
					s.VchItemId, s.VchHdrId, s.DlId AS HtsDlId, s.[Year], s.VchNum,
					s.VchDate, s.VchDateInText, s.VchTypeId, s.Qty, s.PartId AS HtsPartId,
					s.ReplacedPartId AS HtsReplacedPartId, s.ReplacedQty, s.Comment
				INTO #PumSrc
				FROM [TMS].[TotalSystem].[dbo].[Sale_ProjectUtilizedMaterial] s
				WHERE s.[Year] = @Year;

				IF OBJECT_ID('tempdb..#PumMapped') IS NOT NULL DROP TABLE #PumMapped;
				SELECT
					s.*,
					p.Id AS PartId,
					d.DlId,
					d.DlTypeId,
					rp.Id AS ReplacedPartId
				INTO #PumMapped
				FROM #PumSrc s
				INNER JOIN Inv.Part p ON p.HtsId = s.HtsPartId
				INNER JOIN dbo._CutoverPumDlMap d ON d.HtsDlId = s.HtsDlId
				LEFT JOIN Inv.Part rp ON rp.HtsId = s.HtsReplacedPartId AND ISNULL(s.HtsReplacedPartId, 0) <> 0;

				DECLARE @Skipped INT = (SELECT COUNT(*) FROM #PumSrc s WHERE NOT EXISTS (SELECT 1 FROM #PumMapped m WHERE m.HtsId = s.HtsId));

				UPDATE t
				SET
					t.VchItemId = m.VchItemId,
					t.VchHdrId = m.VchHdrId,
					t.DLId = m.DlId,
					t.[Year] = m.[Year],
					t.VchNum = m.VchNum,
					t.VchMiladiDate = m.VchDate,
					t.VchDateShamsiDate = m.VchDateInText,
					t.VchTypeId = m.VchTypeId,
					t.Qty = m.Qty,
					t.PartId = m.PartId,
					t.ReplacedPartId = m.ReplacedPartId,
					t.ReplacedQty = ISNULL(m.ReplacedQty, 0),
					t.Comment = m.Comment,
					t.DLTypeId = m.DlTypeId,
					t.ModifiedById = 1,
					t.ModifiedByName = @Seed,
					t.ModifiedDateMiladiDateTime = @Now,
					t.ModifiedDateShamsiDateTime = @NowShamsi
				FROM Sale.ProjectUtilizedMaterial t
				INNER JOIN #PumMapped m ON m.HtsId = t.HtsId
				WHERE t.VchItemId <> m.VchItemId
				   OR t.VchHdrId <> m.VchHdrId
				   OR t.DLId <> m.DlId
				   OR t.[Year] <> m.[Year]
				   OR ISNULL(t.VchNum, -1) <> ISNULL(m.VchNum, -1)
				   OR ISNULL(t.VchMiladiDate, '19000101') <> ISNULL(m.VchDate, '19000101')
				   OR ISNULL(t.VchDateShamsiDate, N'') <> ISNULL(m.VchDateInText, N'')
				   OR ISNULL(t.VchTypeId, 0) <> ISNULL(m.VchTypeId, 0)
				   OR t.Qty <> m.Qty
				   OR t.PartId <> m.PartId
				   OR ISNULL(t.ReplacedPartId, 0) <> ISNULL(m.ReplacedPartId, 0)
				   OR t.ReplacedQty <> ISNULL(m.ReplacedQty, 0)
				   OR ISNULL(t.Comment, N'') <> ISNULL(m.Comment, N'')
				   OR t.DLTypeId <> m.DlTypeId;

				DECLARE @Updated INT = @@ROWCOUNT;

				INSERT INTO Sale.ProjectUtilizedMaterial (
					DLId, [Year], VchNum, VchMiladiDate, VchDateShamsiDate, Qty, PartId,
					DLTypeId, Comment, HtsId, ReplacedPartId, ReplacedQty, VchHdrId, VchItemId, VchTypeId,
					CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
					ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
					IsActive)
				SELECT
					m.DlId, m.[Year], m.VchNum, m.VchDate, m.VchDateInText, m.Qty, m.PartId,
					m.DlTypeId, m.Comment, m.HtsId, m.ReplacedPartId, ISNULL(m.ReplacedQty, 0),
					m.VchHdrId, m.VchItemId, m.VchTypeId,
					1, @Seed, @Now, @NowShamsi,
					1, @Seed, @Now, @NowShamsi,
					1
				FROM #PumMapped m
				WHERE NOT EXISTS (
					SELECT 1 FROM Sale.ProjectUtilizedMaterial t WHERE t.HtsId = m.HtsId);

				DECLARE @Inserted INT = @@ROWCOUNT;

				SELECT @Inserted AS InsertedCnt, @Updated AS UpdatedCnt, @Skipped AS SkippedCnt;
				""";

			var rows = await db.Database.SqlQueryRaw<CutoverYearRow>(sql).ToListAsync(cn);
			return rows.FirstOrDefault() ?? new CutoverYearRow();
		}

		private async Task<int> SyncHistoryFromTotalSystemAsync(CancellationToken cn)
		{
			var sql = """
				SET NOCOUNT ON;
				DECLARE @Now DATETIME2 = SYSDATETIME();
				DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
				DECLARE @Seed NVARCHAR(80) = N'sync-project-utilized-material-change';

				IF OBJECT_ID('tempdb..#PumUserMap') IS NOT NULL DROP TABLE #PumUserMap;
				;WITH UserCandidates AS (
					SELECT
						CAST(ou.User_ID AS BIGINT) AS OldUserId,
						nu.Id AS NewUserId,
						COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username) AS NewUserName,
						CASE
							WHEN LOWER(nu.Username) = LOWER(ou.Username) THEN 1
							WHEN NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
								 AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))) THEN 2
							WHEN NULLIF(LTRIM(RTRIM(ou.Username)), N'') IS NOT NULL
								 AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(ou.Username) THEN 3
							WHEN NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
								 AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))) THEN 4
						END AS MatchRank
					FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
					INNER JOIN [system].[User] nu
						ON LOWER(nu.Username) = LOWER(ou.Username)
						OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
							AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
						OR (NULLIF(LTRIM(RTRIM(ou.Username)), N'') IS NOT NULL
							AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(ou.Username))
						OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
							AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
				)
				SELECT OldUserId, NewUserId, NewUserName
				INTO #PumUserMap
				FROM (
					SELECT *, ROW_NUMBER() OVER (PARTITION BY OldUserId ORDER BY MatchRank, NewUserId) AS rn
					FROM UserCandidates
					WHERE MatchRank IS NOT NULL
				) x
				WHERE rn = 1;

				MERGE Sale.ProjectUtilizedMaterialChange AS t
				USING (
					SELECT
						CAST(c.Id AS BIGINT) AS HtsId,
						m.Id AS ProjectUtilizedMaterialId,
						c.Comment,
						um.NewUserId AS CreatedById,
						um.NewUserName AS CreatedByName,
						c.CreatedDate AS CreatedOnMiladiDateTime,
						c.CreatedDateInText AS CreatedOnShamsiDateTime
					FROM [TMS].[TotalSystem].[dbo].[Sale_ProjectUtilizedMaterialChanges] c
					INNER JOIN Sale.ProjectUtilizedMaterial m ON m.HtsId = c.ProjectUtilizedMaterialId
					LEFT JOIN #PumUserMap um ON um.OldUserId = c.CreatedUserId
				) s ON t.HtsId = s.HtsId
				WHEN MATCHED THEN UPDATE SET
					t.ProjectUtilizedMaterialId = s.ProjectUtilizedMaterialId,
					t.Comment = s.Comment,
					t.CreatedById = ISNULL(s.CreatedById, t.CreatedById),
					t.CreatedByName = ISNULL(s.CreatedByName, t.CreatedByName),
					t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
					t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
					t.ModifiedById = 1,
					t.ModifiedByName = @Seed,
					t.ModifiedDateMiladiDateTime = @Now,
					t.ModifiedDateShamsiDateTime = @NowShamsi
				WHEN NOT MATCHED THEN INSERT (
					HtsId, ProjectUtilizedMaterialId, Comment,
					CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
					ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
					IsActive)
				VALUES (
					s.HtsId, s.ProjectUtilizedMaterialId, s.Comment,
					ISNULL(s.CreatedById, 1), ISNULL(s.CreatedByName, @Seed), s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
					1, @Seed, @Now, @NowShamsi,
					1);

				SELECT @@ROWCOUNT AS [Value];
				""";

			var rows = await db.Database.SqlQueryRaw<int>(sql).ToListAsync(cn);
			return rows.FirstOrDefault();
		}

		private sealed class CutoverYearRow
		{
			public int InsertedCnt { get; set; }
			public int UpdatedCnt { get; set; }
			public int SkippedCnt { get; set; }
		}
	}
}
