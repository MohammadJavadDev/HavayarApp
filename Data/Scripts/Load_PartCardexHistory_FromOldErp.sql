-- ============================================================
-- One-time load of the OLD ERP (erp.newhavayar, frozen 2004-02-20 .. 2023-03-20) card index
-- into Inv.PartCardexHistory.   Run on database HavayarApp. Idempotent (skips existing OldErpItemId).
--
-- Source: linked server [TMS] -> its linked server ERP (HavayarApp DB has no ERP link of its own).
-- Legacy page filter kept: only items with Qty > 0 are stored.
-- Part mapping: old INV.Part.Serial == Inv.Part.HamkaranId (same assumption as HTS Get_ProductCardex).
--   Items whose part does not exist in Inv.Part are skipped (reported at the end).
--
-- RemainingCount: the HTS old-half subquery was not correlated to the row (one global constant).
-- Here it is the real running balance per (part, stock) ordered by date, then item id:
--   sum( Qty*Ratio )  for VchType BETWEEN '01' AND '49'
--   sum( -Qty*Ratio ) for VchType BETWEEN '51' AND '99'      (same string comparison as the HTS formula)
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('tempdb..#Old') IS NOT NULL DROP TABLE #Old;

SELECT *
INTO #Old
FROM OPENQUERY([TMS], '
SELECT I.VchItmID, I.PartRef, H.StockRef, S.StockName, IA.VchDesc AS VchTypeTitle, H.VchNum,
       CAST(H.VchDate AS date) AS VchDate, I.Qty, I.Ratio, I.VchType, D.Title AS DlTitle, I.ItmDesc AS Comment
FROM erp.newhavayar.INV.InvVchItm AS I
JOIN erp.newhavayar.INV.InvVchHdr AS H ON H.VchHdrID = I.VchHdrRef
JOIN erp.newhavayar.INV.IADocSpec AS IA ON IA.VchType = H.VchType
JOIN erp.newhavayar.INV.Stock AS S ON S.Serial = H.StockRef
LEFT JOIN erp.newhavayar.ACC.DL AS D ON D.AccNum = H.DLREF
');

CREATE CLUSTERED INDEX IX_Old ON #Old (PartRef, StockRef, VchDate, VchItmID);

IF OBJECT_ID('tempdb..#Bal') IS NOT NULL DROP TABLE #Bal;
SELECT o.VchItmID,
       SUM(CASE WHEN o.VchType BETWEEN '01' AND '49' THEN o.Qty * o.Ratio
                WHEN o.VchType BETWEEN '51' AND '99' THEN -o.Qty * o.Ratio
                ELSE 0 END)
           OVER (PARTITION BY o.PartRef, o.StockRef ORDER BY o.VchDate, o.VchItmID ROWS UNBOUNDED PRECEDING) AS Balance
INTO #Bal
FROM #Old o;
CREATE UNIQUE CLUSTERED INDEX IX_Bal ON #Bal (VchItmID);

IF OBJECT_ID('tempdb..#Dates') IS NOT NULL DROP TABLE #Dates;
SELECT d.VchDate, CAST(dbo.fn_MiladiToShamsi(d.VchDate) AS nvarchar(10)) AS ShamsiDate
INTO #Dates FROM (SELECT DISTINCT VchDate FROM #Old) d;
CREATE UNIQUE CLUSTERED INDEX IX_Dates ON #Dates (VchDate);

IF OBJECT_ID('tempdb..#NPart') IS NOT NULL DROP TABLE #NPart;
SELECT HamkaranId, MIN(Id) AS Id INTO #NPart FROM Inv.Part WHERE HamkaranId IS NOT NULL GROUP BY HamkaranId;
CREATE UNIQUE CLUSTERED INDEX IX_NPart ON #NPart (HamkaranId);

INSERT INTO Inv.PartCardexHistory
    (OldErpItemId, PartId, StockName, VchNum, VchTypeTitle, VchDateShamsiDate, VchDateMiladiDate,
     Qty, DlTitle, Comment, RemainingCount, IsActive, CreatedOnMiladiDateTime)
SELECT o.VchItmID, np.Id, o.StockName, o.VchNum, o.VchTypeTitle, dt.ShamsiDate, o.VchDate,
       o.Qty, o.DlTitle, o.Comment, ISNULL(b.Balance, 0), 1, SYSDATETIME()
FROM #Old o
JOIN #NPart np ON np.HamkaranId = o.PartRef
JOIN #Dates dt ON dt.VchDate = o.VchDate
JOIN #Bal b ON b.VchItmID = o.VchItmID
WHERE o.Qty > 0
  AND NOT EXISTS (SELECT 1 FROM Inv.PartCardexHistory h WHERE h.OldErpItemId = o.VchItmID);

-- Verification
SELECT N'old items Qty>0' AS T, COUNT(*) AS C FROM #Old WHERE Qty > 0
UNION ALL SELECT N'loaded', COUNT(*) FROM Inv.PartCardexHistory
UNION ALL SELECT N'skipped (part not in Inv.Part)', COUNT(*) FROM #Old o
          WHERE o.Qty > 0 AND NOT EXISTS (SELECT 1 FROM #NPart p WHERE p.HamkaranId = o.PartRef);
