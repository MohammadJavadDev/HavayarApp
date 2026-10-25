/*
Patch_ProjectUtilizedMaterial_AlignHts_2026-10-07.sql
همسان‌سازی Sale.ProjectUtilizedMaterial با TotalSystem.dbo.Sale_ProjectUtilizedMaterial.
سال 105 (راهکاران) ردیف HtsId=0 و ردیف جاافتاده داشت. کالاهای HtsId=0 با HamkaranId وصل می‌شوند.
Encoding: UTF-8 with BOM
  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Patch_ProjectUtilizedMaterial_AlignHts_2026-10-07.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'TMS linked server not found.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'patch-pum-align-20261007';

UPDATE p
SET p.HtsId = ip.Part_ID,
    p.ModifiedById = 1,
    p.ModifiedByName = @Seed,
    p.ModifiedDateMiladiDateTime = @Now,
    p.ModifiedDateShamsiDateTime = @NowShamsi
FROM Inv.Part p
INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] ip ON ip.Hamkaran_Part_FK = p.HamkaranId
WHERE p.HtsId = 0
  AND ip.Part_ID IN (997366, 997367, 997369, 997370, 997371, 997384, 997385, 997386)
  AND NOT EXISTS (
      SELECT 1 FROM Inv.Part x WHERE x.HtsId = ip.Part_ID AND x.Id <> p.Id);
PRINT N'Parts HtsId stamped: ' + CAST(@@ROWCOUNT AS nvarchar(20));

DECLARE @Years TABLE (Y INT NOT NULL PRIMARY KEY);
-- فقط سال جاری راهکاران. سال‌های قبل در مقایسه نمونه (مقدار/جایگزین/تاریخ) اختلاف نداشتند
-- و به‌روزرسانی سراسری‌شان با جاب همزمان deadlock می‌ساخت.
INSERT INTO @Years (Y) VALUES (105);

DECLARE @Y INT;
DECLARE @Ins INT = 0, @Upd INT = 0, @Link INT = 0, @Skip INT = 0;
DECLARE @YearIns INT, @YearUpd INT, @YearLink INT, @YearSkip INT;

DECLARE yc CURSOR LOCAL FAST_FORWARD FOR SELECT Y FROM @Years ORDER BY Y;
OPEN yc;
FETCH NEXT FROM yc INTO @Y;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    IF OBJECT_ID('tempdb..#Dl') IS NOT NULL DROP TABLE #Dl;
    IF OBJECT_ID('tempdb..#Mapped') IS NOT NULL DROP TABLE #Mapped;
    IF OBJECT_ID('tempdb..#Zeros') IS NOT NULL DROP TABLE #Zeros;

    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        s.VchItemId,
        s.VchHdrId,
        s.DlId AS HtsDlId,
        s.[Year],
        s.VchNum,
        s.VchDate,
        s.VchDateInText,
        s.VchTypeId,
        s.Qty,
        s.PartId AS HtsPartId,
        s.ReplacedPartId AS HtsReplacedPartId,
        s.ReplacedQty,
        s.Comment
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Sale_ProjectUtilizedMaterial] s
    WHERE s.[Year] = @Y;

    ;WITH DlPick AS (
        SELECT
            ad.Acc_DL_ID AS HtsDlId,
            dl.Id AS DlId,
            CAST(ISNULL(dl.TypeId, 0) AS TINYINT) AS DlTypeId,
            ROW_NUMBER() OVER (PARTITION BY ad.Acc_DL_ID ORDER BY dl.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
        INNER JOIN (SELECT DISTINCT HtsDlId FROM #Src) s ON s.HtsDlId = ad.Acc_DL_ID
        INNER JOIN FIN.DL dl ON TRY_CONVERT(INT, LTRIM(RTRIM(dl.Code))) = ad.Hamkaran_Acc_DL_FK
    )
    SELECT HtsDlId, DlId, DlTypeId
    INTO #Dl
    FROM DlPick
    WHERE rn = 1;

    SELECT
        s.HtsId, s.VchItemId, s.VchHdrId, s.[Year], s.VchNum, s.VchDate, s.VchDateInText,
        s.VchTypeId, s.Qty, s.HtsReplacedPartId, s.ReplacedQty, s.Comment,
        p.Id AS PartId,
        d.DlId,
        d.DlTypeId,
        CASE
            WHEN ISNULL(s.HtsReplacedPartId, 0) = 0 THEN NULL
            ELSE rp.Id
        END AS ReplacedPartId
    INTO #Mapped
    FROM #Src s
    INNER JOIN Inv.Part p ON p.HtsId = s.HtsPartId
    INNER JOIN #Dl d ON d.HtsDlId = s.HtsDlId
    LEFT JOIN Inv.Part rp ON rp.HtsId = s.HtsReplacedPartId AND ISNULL(s.HtsReplacedPartId, 0) <> 0;

    CREATE UNIQUE CLUSTERED INDEX IX_Mapped_HtsId ON #Mapped(HtsId);
    CREATE NONCLUSTERED INDEX IX_Mapped_Vch ON #Mapped(VchItemId, [Year]);

    SET @YearSkip = (
        SELECT COUNT(*)
        FROM #Src s
        WHERE NOT EXISTS (SELECT 1 FROM #Mapped m WHERE m.HtsId = s.HtsId));

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
        t.ReplacedPartId = CASE
            WHEN ISNULL(m.HtsReplacedPartId, 0) = 0 THEN NULL
            WHEN m.ReplacedPartId IS NOT NULL THEN m.ReplacedPartId
            ELSE t.ReplacedPartId
        END,
        t.ReplacedQty = ISNULL(m.ReplacedQty, 0),
        t.Comment = m.Comment,
        t.DLTypeId = m.DlTypeId,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.ProjectUtilizedMaterial t
    INNER JOIN #Mapped m ON m.HtsId = t.HtsId
    WHERE t.VchItemId <> m.VchItemId
       OR t.VchHdrId <> m.VchHdrId
       OR ISNULL(t.DLId, 0) <> ISNULL(m.DlId, 0)
       OR t.[Year] <> m.[Year]
       OR ISNULL(t.VchNum, -1) <> ISNULL(m.VchNum, -1)
       OR CONVERT(datetime, ISNULL(t.VchMiladiDate, '19000101')) <> CONVERT(datetime, ISNULL(m.VchDate, '19000101'))
       OR ISNULL(t.VchDateShamsiDate, N'') <> ISNULL(m.VchDateInText, N'')
       OR ISNULL(t.VchTypeId, 0) <> ISNULL(m.VchTypeId, 0)
       OR t.Qty <> m.Qty
       OR ISNULL(t.PartId, 0) <> ISNULL(m.PartId, 0)
       OR (
            ISNULL(m.HtsReplacedPartId, 0) = 0 AND t.ReplacedPartId IS NOT NULL
          )
       OR (
            m.ReplacedPartId IS NOT NULL AND ISNULL(t.ReplacedPartId, 0) <> m.ReplacedPartId
          )
       OR t.ReplacedQty <> ISNULL(m.ReplacedQty, 0)
       OR ISNULL(t.Comment, N'') <> ISNULL(m.Comment, N'')
       OR t.DLTypeId <> m.DlTypeId;
    SET @YearUpd = @@ROWCOUNT;

    SELECT t.Id
    INTO #Zeros
    FROM Sale.ProjectUtilizedMaterial t
    WHERE t.HtsId = 0 AND t.[Year] = @Y;

    UPDATE t
    SET
        t.HtsId = m.HtsId,
        t.VchHdrId = m.VchHdrId,
        t.DLId = m.DlId,
        t.VchNum = m.VchNum,
        t.VchMiladiDate = m.VchDate,
        t.VchDateShamsiDate = m.VchDateInText,
        t.VchTypeId = m.VchTypeId,
        t.Qty = m.Qty,
        t.PartId = m.PartId,
        t.ReplacedPartId = CASE
            WHEN ISNULL(m.HtsReplacedPartId, 0) = 0 THEN NULL
            WHEN m.ReplacedPartId IS NOT NULL THEN m.ReplacedPartId
            ELSE t.ReplacedPartId
        END,
        t.ReplacedQty = ISNULL(m.ReplacedQty, 0),
        t.Comment = m.Comment,
        t.DLTypeId = m.DlTypeId,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM #Zeros z
    INNER JOIN Sale.ProjectUtilizedMaterial t ON t.Id = z.Id
    INNER JOIN #Mapped m ON m.VchItemId = t.VchItemId AND m.[Year] = t.[Year]
    WHERE NOT EXISTS (
          SELECT 1 FROM Sale.ProjectUtilizedMaterial x
          WHERE x.HtsId = m.HtsId AND x.Id <> t.Id);
    SET @YearLink = @@ROWCOUNT;

    INSERT INTO Sale.ProjectUtilizedMaterial (
        DLId, [Year], VchNum, VchMiladiDate, VchDateShamsiDate, Qty, PartId,
        DLTypeId, Comment, HtsId, ReplacedPartId, ReplacedQty, VchHdrId, VchItemId, VchTypeId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive)
    SELECT
        m.DlId, m.[Year], m.VchNum, m.VchDate, m.VchDateInText, m.Qty, m.PartId,
        m.DlTypeId, m.Comment, m.HtsId,
        CASE WHEN ISNULL(m.HtsReplacedPartId, 0) = 0 THEN NULL ELSE m.ReplacedPartId END,
        ISNULL(m.ReplacedQty, 0),
        m.VchHdrId, m.VchItemId, m.VchTypeId,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi,
        1
    FROM #Mapped m
    WHERE NOT EXISTS (SELECT 1 FROM Sale.ProjectUtilizedMaterial t WHERE t.HtsId = m.HtsId);
    SET @YearIns = @@ROWCOUNT;

    SET @Ins += @YearIns;
    SET @Upd += @YearUpd;
    SET @Link += @YearLink;
    SET @Skip += @YearSkip;

    IF @YearIns > 0 OR @YearUpd > 0 OR @YearLink > 0 OR @YearSkip > 0
        PRINT N'Year ' + CAST(@Y AS nvarchar(10))
            + N' ins=' + CAST(@YearIns AS nvarchar(20))
            + N' upd=' + CAST(@YearUpd AS nvarchar(20))
            + N' link=' + CAST(@YearLink AS nvarchar(20))
            + N' skip=' + CAST(@YearSkip AS nvarchar(20));

    FETCH NEXT FROM yc INTO @Y;
END
CLOSE yc;
DEALLOCATE yc;

PRINT N'=== DONE align materials ins=' + CAST(@Ins AS nvarchar(20))
    + N' upd=' + CAST(@Upd AS nvarchar(20))
    + N' link=' + CAST(@Link AS nvarchar(20))
    + N' skip=' + CAST(@Skip AS nvarchar(20));

SELECT
    (SELECT COUNT(*) FROM Sale.ProjectUtilizedMaterial) AS NewCnt,
    (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Sale_ProjectUtilizedMaterial]) AS OldCnt,
    (SELECT COUNT(*) FROM Sale.ProjectUtilizedMaterial WHERE HtsId = 0) AS HtsIdZero,
    (SELECT COUNT(*)
     FROM [TMS].[TotalSystem].[dbo].[Sale_ProjectUtilizedMaterial] o
     WHERE NOT EXISTS (SELECT 1 FROM Sale.ProjectUtilizedMaterial n WHERE n.HtsId = o.Id)) AS InOldNotNew,
    @Ins AS InsertedCnt,
    @Upd AS UpdatedCnt,
    @Link AS LinkedCnt,
    @Skip AS SkippedCnt;
