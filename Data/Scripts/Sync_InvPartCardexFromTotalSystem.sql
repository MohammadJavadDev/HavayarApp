-- ============================================================
-- Copy of Inv.Stock / Inv.PartCardex / Inv.VchHeader from HTS (TotalSystem, linked server [TMS])
-- Run on database HavayarApp.  Idempotent (safe to re-run).
--
-- Mapping keys:
--   Stock      : Inv_Stock.Hamkaran_Stock_FK        -> Inv.Stock.HamkaranId   (Inv_Stock.Stock_ID -> HtsId)
--   Part       : Inv_Part.Hamkaran_Part_FK          -> Inv.Part.HamkaranId
--   DL         : Acc_DL.Hamkaran_Acc_DL_FK (= DL code) -> FIN.DL.Code
--   VchType    : Inv_VchType.Hamkaran_InvVchType_FK -> Inv.VchType.HamkaranId
--   Creator/Confirmer: Gnr_ManCompany.Hamkaran_ManCompany_FK -> Gnr.Party.HamkaranId
--   PartCardex : Inv_PartCardex.VchItemId           -> Inv.PartCardex.HamkaranId (Inv_PartCardex.Id -> HtsId)
--   VchHeader  : Inv_VchHeader.VchHeaderId          -> Inv.VchHeader.HamkaranId (Inv_VchHeader.Id -> HtsId)
--
-- Years copied: PartCardex all (102..105); VchHeader all (Acc_Year 21..23).
-- VchHeader.IsNotified: rows older than 7 days are forced to 1 so no stale e-mails are sent.
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRANSACTION;

------------------------------------------------------------
-- 1) Stock
------------------------------------------------------------
INSERT INTO Inv.Stock (Title, HamkaranId, HtsId, IsActive, CreatedOnMiladiDateTime)
SELECT ISNULL(s.Stock_Title, N''), s.Hamkaran_Stock_FK, s.Stock_ID, 1, SYSDATETIME()
FROM [TMS].[TotalSystem].[dbo].[Inv_Stock] s
WHERE NOT EXISTS (SELECT 1 FROM Inv.Stock n WHERE n.HamkaranId = s.Hamkaran_Stock_FK);

UPDATE n SET n.HtsId = s.Stock_ID
FROM Inv.Stock n
JOIN [TMS].[TotalSystem].[dbo].[Inv_Stock] s ON s.Hamkaran_Stock_FK = n.HamkaranId
WHERE n.HtsId = 0;

------------------------------------------------------------
-- Staging: pull remote data once into temp tables (much faster than joining over the linked server)
------------------------------------------------------------
IF OBJECT_ID('tempdb..#PC') IS NOT NULL DROP TABLE #PC;
SELECT x.Id, x.VchItemId, x.VchHeaderId, x.[Year], RTRIM(x.VchType) AS VchType, x.VchNum, x.PartId, x.StockId,
       x.VchDate, x.DlId, x.Qty, x.VQtyRatio, RTRIM(x.[Status]) AS [Status], x.ItemDescription, x.IsPermanentVoucher
INTO #PC
FROM (SELECT p.*, ROW_NUMBER() OVER (PARTITION BY p.VchItemId ORDER BY p.Id) AS rn
      FROM [TMS].[TotalSystem].[dbo].[Inv_PartCardex] p) x
WHERE x.rn = 1;   -- 41 VchItemId values are duplicated in HTS (two Inv_Part rows share one Hamkaran_Part_FK, stock 88); keep lowest Id
CREATE UNIQUE CLUSTERED INDEX IX_PC ON #PC(VchItemId);

IF OBJECT_ID('tempdb..#HPart') IS NOT NULL DROP TABLE #HPart;
SELECT Part_ID, Hamkaran_Part_FK INTO #HPart FROM [TMS].[TotalSystem].[dbo].[Inv_Part];
CREATE UNIQUE CLUSTERED INDEX IX_HPart ON #HPart(Part_ID);

IF OBJECT_ID('tempdb..#HDL') IS NOT NULL DROP TABLE #HDL;
SELECT Acc_DL_ID, Hamkaran_Acc_DL_FK INTO #HDL FROM [TMS].[TotalSystem].[dbo].[Acc_DL];
CREATE UNIQUE CLUSTERED INDEX IX_HDL ON #HDL(Acc_DL_ID);

IF OBJECT_ID('tempdb..#NPart') IS NOT NULL DROP TABLE #NPart;
SELECT HamkaranId, MIN(Id) AS Id INTO #NPart FROM Inv.Part WHERE HamkaranId IS NOT NULL GROUP BY HamkaranId;
CREATE UNIQUE CLUSTERED INDEX IX_NPart ON #NPart(HamkaranId);

IF OBJECT_ID('tempdb..#NDL') IS NOT NULL DROP TABLE #NDL;
-- HTS Acc_DL.Hamkaran_Acc_DL_FK holds the Rahkaran DL *Code* (= CounterpartDLCode), so map by FIN.DL.Code
SELECT TRY_CAST(Code AS bigint) AS HamkaranId, MIN(Id) AS Id INTO #NDL FROM FIN.DL WHERE TRY_CAST(Code AS bigint) IS NOT NULL GROUP BY TRY_CAST(Code AS bigint);
CREATE UNIQUE CLUSTERED INDEX IX_NDL ON #NDL(HamkaranId);

IF OBJECT_ID('tempdb..#NParty') IS NOT NULL DROP TABLE #NParty;
SELECT HamkaranId, MIN(Id) AS Id INTO #NParty FROM Gnr.Party WHERE HamkaranId IS NOT NULL GROUP BY HamkaranId;
CREATE UNIQUE CLUSTERED INDEX IX_NParty ON #NParty(HamkaranId);

IF OBJECT_ID('tempdb..#Dates') IS NOT NULL DROP TABLE #Dates;
SELECT d.VchDate, CAST(dbo.fn_MiladiToShamsi(d.VchDate) AS nvarchar(10)) AS ShamsiDate
INTO #Dates FROM (SELECT DISTINCT VchDate FROM #PC) d;
CREATE UNIQUE CLUSTERED INDEX IX_Dates ON #Dates(VchDate);

IF OBJECT_ID('tempdb..#HStock') IS NOT NULL DROP TABLE #HStock;
SELECT Stock_ID, Hamkaran_Stock_FK INTO #HStock FROM [TMS].[TotalSystem].[dbo].[Inv_Stock];
CREATE UNIQUE CLUSTERED INDEX IX_HStock ON #HStock(Stock_ID);

------------------------------------------------------------
-- 2) PartCardex (HamkaranId = VchItemId, HtsId = old Id)
------------------------------------------------------------
INSERT INTO Inv.PartCardex
    (HamkaranId, VchHeaderHamkaranId, [Year], VchType, VchNum, PartId, StockId,
     VchDateMiladiDate, VchDateShamsiDate, DlId, Qty, VQtyRatio, [Status], ItemDescription,
     IsPermanentVoucher, HtsId, IsActive, CreatedOnMiladiDateTime)
SELECT c.VchItemId, c.VchHeaderId, c.[Year], c.VchType, c.VchNum, np.Id, ns.Id,
       c.VchDate, dt.ShamsiDate, nd.Id, c.Qty, c.VQtyRatio, c.[Status], c.ItemDescription,
       c.IsPermanentVoucher, c.Id, 1, SYSDATETIME()
FROM #PC c
JOIN #HPart p ON p.Part_ID = c.PartId
JOIN #NPart np ON np.HamkaranId = p.Hamkaran_Part_FK
JOIN #HStock s ON s.Stock_ID = c.StockId
JOIN Inv.Stock ns ON ns.HamkaranId = s.Hamkaran_Stock_FK
JOIN #Dates dt ON dt.VchDate = c.VchDate
LEFT JOIN #HDL od ON od.Acc_DL_ID = c.DlId
LEFT JOIN #NDL nd ON nd.HamkaranId = od.Hamkaran_Acc_DL_FK
WHERE NOT EXISTS (SELECT 1 FROM Inv.PartCardex n WHERE n.HamkaranId = c.VchItemId);

UPDATE n SET n.HtsId = c.Id
FROM Inv.PartCardex n JOIN #PC c ON c.VchItemId = n.HamkaranId
WHERE n.HtsId = 0;

------------------------------------------------------------
-- 3) VchHeader (HamkaranId = VchHeaderId, HtsId = old Id)
------------------------------------------------------------
IF OBJECT_ID('tempdb..#VH') IS NOT NULL DROP TABLE #VH;
SELECT x.* INTO #VH
FROM (SELECT h.*, ROW_NUMBER() OVER (PARTITION BY h.VchHeaderId ORDER BY h.Id) AS rn
      FROM [TMS].[TotalSystem].[dbo].[Inv_VchHeader] h) x
WHERE x.rn = 1;   -- HTS holds 2860 duplicated VchHeaderId values (confirmer join in the HTS import); keep lowest Id
CREATE UNIQUE CLUSTERED INDEX IX_VH ON #VH(VchHeaderId);

IF OBJECT_ID('tempdb..#HYear') IS NOT NULL DROP TABLE #HYear;
SELECT Year_ID, Year_Title INTO #HYear FROM [TMS].[TotalSystem].[dbo].[Acc_Year];
IF OBJECT_ID('tempdb..#HVT') IS NOT NULL DROP TABLE #HVT;
SELECT VchType_ID, Hamkaran_InvVchType_FK INTO #HVT FROM [TMS].[TotalSystem].[dbo].[Inv_VchType];
IF OBJECT_ID('tempdb..#HMan') IS NOT NULL DROP TABLE #HMan;
SELECT ManCompany_ID, Hamkaran_ManCompany_FK INTO #HMan FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany];
CREATE UNIQUE CLUSTERED INDEX IX_HMan ON #HMan(ManCompany_ID);

INSERT INTO Inv.VchHeader
    (HamkaranId, [Year], VchTypeId, VchNum, VchDateMiladiDate, VchDateShamsiDate, DlId, StockId,
     CreatorId, ConfirmerId, [Status], VchDescription, OpositStockName, OpositStockCode,
     BaseVchType, BaseVchDescription, Comment, IsNotified, HtsId, IsActive, CreatedOnMiladiDateTime)
SELECT h.VchHeaderId, y.Year_Title, nt.Id, h.VchNum, CAST(h.VchDate AS date), h.VchDateInText, nd.Id, ns.Id,
       cr.Id, cf.Id, h.[Status], h.VchDescription, h.OpositStockName, h.OpositStockCode,
       h.BaseVchType, h.BaseVchDescription, h.Comment,
       CASE WHEN h.VchDate < DATEADD(DAY, -7, CAST(GETDATE() AS date)) THEN 1 ELSE h.IsNotified END,
       h.Id, 1, SYSDATETIME()
FROM #VH h
JOIN #HYear y ON y.Year_ID = h.YearId
JOIN #HVT t ON t.VchType_ID = h.VchTypeId
JOIN Inv.VchType nt ON nt.HamkaranId = t.Hamkaran_InvVchType_FK
JOIN #HStock s ON s.Stock_ID = h.StockId
JOIN Inv.Stock ns ON ns.HamkaranId = s.Hamkaran_Stock_FK
LEFT JOIN #HDL od ON od.Acc_DL_ID = h.DlId
LEFT JOIN #NDL nd ON nd.HamkaranId = od.Hamkaran_Acc_DL_FK
LEFT JOIN #HMan mc ON mc.ManCompany_ID = h.CreatorId
LEFT JOIN #NParty cr ON cr.HamkaranId = mc.Hamkaran_ManCompany_FK
LEFT JOIN #HMan mf ON mf.ManCompany_ID = h.ConfirmerId
LEFT JOIN #NParty cf ON cf.HamkaranId = mf.Hamkaran_ManCompany_FK
WHERE NOT EXISTS (SELECT 1 FROM Inv.VchHeader n WHERE n.HamkaranId = h.VchHeaderId);
------------------------------------------------------------
-- 4) Sale.OrderPoint.PartCardexId: link legacy order points (HtsPartCardexId) to the copied cardex rows (by HtsId)
------------------------------------------------------------
UPDATE op SET op.PartCardexId = pc.Id
FROM Sale.OrderPoint op
JOIN Inv.PartCardex pc ON pc.HtsId = op.HtsPartCardexId
WHERE op.PartCardexId IS NULL AND op.HtsPartCardexId <> 0;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @m nvarchar(2000) = ERROR_MESSAGE();
    RAISERROR(@m, 16, 1);
END CATCH;

------------------------------------------------------------
-- Verification (compare with HTS)
------------------------------------------------------------
SELECT N'Stock' AS T, (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Inv_Stock]) AS Hts, (SELECT COUNT(*) FROM Inv.Stock) AS Havayar
UNION ALL SELECT N'PartCardex', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Inv_PartCardex]), (SELECT COUNT(*) FROM Inv.PartCardex)
UNION ALL SELECT N'VchHeader', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Inv_VchHeader]), (SELECT COUNT(*) FROM Inv.VchHeader);

SELECT N'PartCardex VQtyRatio sum' AS T,
       (SELECT SUM(VQtyRatio) FROM [TMS].[TotalSystem].[dbo].[Inv_PartCardex]) AS Hts,
       (SELECT SUM(VQtyRatio) FROM Inv.PartCardex) AS Havayar;

SELECT N'OrderPoint rows resolvable by HtsId' AS T,
       COUNT(*) AS Total, SUM(CASE WHEN pc.Id IS NOT NULL THEN 1 ELSE 0 END) AS Resolved
FROM Sale.OrderPoint op LEFT JOIN Inv.PartCardex pc ON pc.Id = op.PartCardexId;
