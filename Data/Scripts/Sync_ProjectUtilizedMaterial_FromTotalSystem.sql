/*
Sync_ProjectUtilizedMaterial_FromTotalSystem.sql
کات‌اور قابل‌اجرای مجدد Sale.ProjectUtilizedMaterial + Sale.ProjectUtilizedMaterialChange از [TMS].[TotalSystem].
نگاشت کالا: Inv.Part.HtsId = HTS.PartId
نگاشت تفصیل: TRY_CONVERT(INT, FIN.DL.Code) = Acc_DL.Hamkaran_Acc_DL_FK  (کد راهکاران، نه DLID)
نوع رسید همان VchTypeId عدد HTS است؛ جدول Inv.VchType ساخته نمی‌شود.

Encoding: UTF-8 with BOM
Apply:
  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Sync_ProjectUtilizedMaterial_FromTotalSystem.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-project-utilized-material';
DECLARE @Inserted INT = 0, @Updated INT = 0, @Skipped INT = 0, @YearInserted INT, @YearUpdated INT, @YearSkipped INT;
DECLARE @ChangeRows INT = 0;
DECLARE @Year INT;

IF OBJECT_ID(N'dbo._CutoverPumDlMap') IS NOT NULL DROP TABLE dbo._CutoverPumDlMap;
SELECT ad.Acc_DL_ID AS HtsDlId, dl.Id AS DlId, CAST(ISNULL(dl.TypeId, 0) AS TINYINT) AS DlTypeId
INTO dbo._CutoverPumDlMap
FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
INNER JOIN FIN.DL dl ON TRY_CONVERT(INT, LTRIM(RTRIM(dl.Code))) = ad.Hamkaran_Acc_DL_FK;
CREATE UNIQUE CLUSTERED INDEX IX_CutoverPumDlMap ON dbo._CutoverPumDlMap (HtsDlId);

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_DLId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
    ALTER INDEX IX_ProjectUtilizedMaterial_DLId ON Sale.ProjectUtilizedMaterial DISABLE;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_DLTypeId1' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
    ALTER INDEX IX_ProjectUtilizedMaterial_DLTypeId1 ON Sale.ProjectUtilizedMaterial DISABLE;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_PartId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
    ALTER INDEX IX_ProjectUtilizedMaterial_PartId ON Sale.ProjectUtilizedMaterial DISABLE;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectUtilizedMaterial_ReplacedPartId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
    ALTER INDEX IX_ProjectUtilizedMaterial_ReplacedPartId ON Sale.ProjectUtilizedMaterial DISABLE;

DECLARE year_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT [Year]
    FROM [TMS].[TotalSystem].[dbo].[Sale_ProjectUtilizedMaterial]
    ORDER BY [Year];

OPEN year_cursor;
FETCH NEXT FROM year_cursor INTO @Year;
WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRAN;

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

    SET @YearSkipped = (SELECT COUNT(*) FROM #PumSrc s WHERE NOT EXISTS (SELECT 1 FROM #PumMapped m WHERE m.HtsId = s.HtsId));

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
    SET @YearUpdated = @@ROWCOUNT;

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
    WHERE NOT EXISTS (SELECT 1 FROM Sale.ProjectUtilizedMaterial t WHERE t.HtsId = m.HtsId);
    SET @YearInserted = @@ROWCOUNT;

    COMMIT;

    SET @Inserted = @Inserted + @YearInserted;
    SET @Updated = @Updated + @YearUpdated;
    SET @Skipped = @Skipped + @YearSkipped;
    PRINT N'Year ' + CAST(@Year AS NVARCHAR(10)) + N': inserted=' + CAST(@YearInserted AS NVARCHAR(20))
        + N' updated=' + CAST(@YearUpdated AS NVARCHAR(20))
        + N' skipped=' + CAST(@YearSkipped AS NVARCHAR(20));

    FETCH NEXT FROM year_cursor INTO @Year;
END
CLOSE year_cursor;
DEALLOCATE year_cursor;

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
    t.ModifiedByName = N'sync-project-utilized-material-change',
    t.ModifiedDateMiladiDateTime = @Now,
    t.ModifiedDateShamsiDateTime = @NowShamsi
WHEN NOT MATCHED THEN INSERT (
    HtsId, ProjectUtilizedMaterialId, Comment,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
    IsActive)
VALUES (
    s.HtsId, s.ProjectUtilizedMaterialId, s.Comment,
    ISNULL(s.CreatedById, 1), ISNULL(s.CreatedByName, N'sync-project-utilized-material-change'),
    s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
    1, N'sync-project-utilized-material-change', @Now, @NowShamsi,
    1);
SET @ChangeRows = @@ROWCOUNT;

IF OBJECT_ID(N'dbo._CutoverPumDlMap') IS NOT NULL DROP TABLE dbo._CutoverPumDlMap;

PRINT N'=== DONE Sync_ProjectUtilizedMaterial_FromTotalSystem ===';
PRINT N'Materials inserted=' + CAST(@Inserted AS NVARCHAR(20))
    + N' updated=' + CAST(@Updated AS NVARCHAR(20))
    + N' skipped=' + CAST(@Skipped AS NVARCHAR(20))
    + N' changes=' + CAST(@ChangeRows AS NVARCHAR(20));

SELECT
    (SELECT COUNT(*) FROM Sale.ProjectUtilizedMaterial) AS MaterialCnt,
    (SELECT COUNT(*) FROM Sale.ProjectUtilizedMaterialChange) AS ChangeCnt,
    @Inserted AS InsertedCnt,
    @Updated AS UpdatedCnt,
    @Skipped AS SkippedCnt,
    @ChangeRows AS ChangeRows;
