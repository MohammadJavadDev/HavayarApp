-- ============================================================================
-- Migrate Inv.PartSparePart from TotalSystem (HTS) via linked server [TMS]
-- Source: dbo.Inv_PartSparePart (~519 rows)
-- Target: Inv.PartSparePart (was EMPTY -> "Spare parts" button on
--         ProductionOrderItem/Edit showed nothing)
-- Part mapping: Inv.Part.HtsId = Inv_Part.Part_ID, fallback Inv.Part.Code = Part_Code
-- Idempotent: inserts only missing (PartId, SparePartId) pairs; never deletes.
-- Usage: @DryRun = 1 (counts only) | @DryRun = 0 (insert)
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;
DECLARE @SeedUser NVARCHAR(150) = N'migrate-inv-partsparepart-hts';
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] missing. Run CreateLinkedServer_TMS.sql first.', 16, 1);
    RETURN;
END

BEGIN TRY
    /* --- Part map (HtsId first, Code fallback; one row per HTS part) --- */
    IF OBJECT_ID('tempdb..#PartMap') IS NOT NULL DROP TABLE #PartMap;
    SELECT CAST(op.Part_ID AS BIGINT) AS HtsPartId, MIN(p.Id) AS PartId
    INTO #PartMap
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part] op
    INNER JOIN Inv.Part p ON p.HtsId = op.Part_ID AND p.HtsId <> 0
    GROUP BY op.Part_ID;

    INSERT INTO #PartMap (HtsPartId, PartId)
    SELECT CAST(op.Part_ID AS BIGINT), MIN(p.Id)
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part] op
    INNER JOIN Inv.Part p ON p.Code = op.Part_Code AND p.Code IS NOT NULL AND p.Code <> N''
    WHERE NOT EXISTS (SELECT 1 FROM #PartMap m WHERE m.HtsPartId = op.Part_ID)
    GROUP BY op.Part_ID;

    CREATE UNIQUE CLUSTERED INDEX IX_PartMap ON #PartMap(HtsPartId);

    /* --- Source rows --- */
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        CAST(s.Id AS BIGINT)                        AS HtsId,
        CAST(s.PartId AS BIGINT)                    AS HtsPartId,
        CAST(s.SparePartId AS BIGINT)               AS HtsSparePartId,
        CAST(s.WorkingHours AS INT)                 AS WorkingHours,
        CAST(s.Comment AS NVARCHAR(MAX))            AS Description,
        CAST(s.CreatedDate AS DATETIME2)            AS CreatedDate,
        CAST(s.CreatedDateInText AS NVARCHAR(30))   AS CreatedDateInText,
        CAST(NULL AS BIGINT)                        AS NewPartId,
        CAST(NULL AS BIGINT)                        AS NewSparePartId
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Inv_PartSparePart] s;

    UPDATE x SET NewPartId = m.PartId
    FROM #Src x INNER JOIN #PartMap m ON m.HtsPartId = x.HtsPartId;
    UPDATE x SET NewSparePartId = m.PartId
    FROM #Src x INNER JOIN #PartMap m ON m.HtsPartId = x.HtsSparePartId;

    DECLARE @Total INT = (SELECT COUNT(*) FROM #Src);
    DECLARE @Unmapped INT = (SELECT COUNT(*) FROM #Src WHERE NewPartId IS NULL OR NewSparePartId IS NULL);
    DECLARE @Existing INT = (SELECT COUNT(*) FROM Inv.PartSparePart);
    PRINT N'Source rows: ' + CAST(@Total AS NVARCHAR(20))
        + N', unmapped: ' + CAST(@Unmapped AS NVARCHAR(20))
        + N', existing target rows: ' + CAST(@Existing AS NVARCHAR(20));

    IF @DryRun = 1
    BEGIN
        PRINT N'DryRun=1 - no writes.';
        RETURN;
    END

    BEGIN TRANSACTION;

    INSERT INTO Inv.PartSparePart
        (PartId, SparePartId, WorkingHours, Description,
         CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    SELECT
        x.NewPartId, x.NewSparePartId, x.WorkingHours, x.Description,
        @SeedUser, ISNULL(x.CreatedDate, @Now), ISNULL(x.CreatedDateInText, @NowShamsi), 1
    FROM #Src x
    WHERE x.NewPartId IS NOT NULL AND x.NewSparePartId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Inv.PartSparePart t
                      WHERE t.PartId = x.NewPartId AND t.SparePartId = x.NewSparePartId)
    ORDER BY x.HtsId;
    PRINT N'Inserted PartSparePart: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    SELECT N'PartSparePart' AS T, COUNT(*) AS Cnt FROM Inv.PartSparePart;

    PRINT N'=== DONE Migrate_InvPartSparePart_FromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
