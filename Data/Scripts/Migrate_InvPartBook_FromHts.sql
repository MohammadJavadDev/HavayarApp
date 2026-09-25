-- ============================================================================
-- Migrate Inv Part Book from TotalSystem via linked server [TMS]
-- Source: Inv_PartBookSection (~80), Inv_Part_PartBookSection (~2785), Inv_Part_CustomBom (~1009)
-- Pictures: companion Python script Migrate_InvPartBook_Pictures.py
-- Usage: @DryRun = 1 (counts) | @DryRun = 0 (reload)
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 0;
DECLARE @SeedUser NVARCHAR(150) = N'migrate-inv-partbook-hts';
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] missing. Run CreateLinkedServer_TMS.sql first.', 16, 1);
    RETURN;
END

IF OBJECT_ID(N'Inv.PartBookSection', N'U') IS NULL
   OR OBJECT_ID(N'Inv.PartPartBookSection', N'U') IS NULL
   OR OBJECT_ID(N'Inv.PartCustomBom', N'U') IS NULL
BEGIN
    RAISERROR(N'Target Inv PartBook tables missing. Apply migration AddInvPartBook first.', 16, 1);
    RETURN;
END

BEGIN TRY
    /* ─── Part map ─── */
    IF OBJECT_ID('tempdb..#PartMap') IS NOT NULL DROP TABLE #PartMap;
    SELECT CAST(op.Part_ID AS BIGINT) AS HtsPartId, p.Id AS PartId, CAST(N'HtsId' AS NVARCHAR(20)) AS MatchBy
    INTO #PartMap
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part] op
    INNER JOIN Inv.Part p ON p.HtsId = op.Part_ID AND p.HtsId <> 0;

    INSERT INTO #PartMap (HtsPartId, PartId, MatchBy)
    SELECT CAST(op.Part_ID AS BIGINT), p.Id, N'Code'
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part] op
    INNER JOIN Inv.Part p ON p.Code = op.Part_Code AND p.Code IS NOT NULL AND p.Code <> N''
    WHERE NOT EXISTS (SELECT 1 FROM #PartMap m WHERE m.HtsPartId = op.Part_ID);

    CREATE UNIQUE CLUSTERED INDEX IX_PartMap ON #PartMap(HtsPartId);
    DECLARE @Cnt INT = (SELECT COUNT(*) FROM #PartMap);
    PRINT N'Part map rows: ' + CAST(@Cnt AS NVARCHAR(20));

    /* ─── Source sections ─── */
    IF OBJECT_ID('tempdb..#SrcSection') IS NOT NULL DROP TABLE #SrcSection;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        CAST(s.Title AS NVARCHAR(255)) AS Title,
        CAST(s.[Order] AS INT) AS [Order],
        CAST(s.PictureTitle AS NVARCHAR(255)) AS PictureTitle,
        CAST(s.IsForCustomBom AS BIT) AS IsForCustomBom,
        CAST(s.Comment AS NVARCHAR(2048)) AS Comment
    INTO #SrcSection
    FROM [TMS].[TotalSystem].[dbo].[Inv_PartBookSection] s;

    SET @Cnt = (SELECT COUNT(*) FROM #SrcSection);
    PRINT N'Sections source: ' + CAST(@Cnt AS NVARCHAR(20));

    /* ─── Source links ─── */
    IF OBJECT_ID('tempdb..#SrcLink') IS NOT NULL DROP TABLE #SrcLink;
    SELECT
        CAST(l.Id AS BIGINT) AS HtsId,
        CAST(l.PartId AS BIGINT) AS HtsPartId,
        CAST(l.PartBookSectionId AS BIGINT) AS HtsSectionId,
        CAST(l.[Order] AS INT) AS [Order],
        CAST(NULL AS BIGINT) AS NewPartId,
        CAST(NULL AS BIGINT) AS NewSectionId
    INTO #SrcLink
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part_PartBookSection] l;

    UPDATE l SET NewPartId = m.PartId
    FROM #SrcLink l INNER JOIN #PartMap m ON m.HtsPartId = l.HtsPartId;

    SET @Cnt = (SELECT COUNT(*) FROM #SrcLink);
    DECLARE @Cnt2 INT = (SELECT COUNT(*) FROM #SrcLink WHERE NewPartId IS NULL);
    PRINT N'Links source: ' + CAST(@Cnt AS NVARCHAR(20))
        + N', unmapped product: ' + CAST(@Cnt2 AS NVARCHAR(20));

    /* ─── Source custom BOM ─── */
    IF OBJECT_ID('tempdb..#SrcCustom') IS NOT NULL DROP TABLE #SrcCustom;
    SELECT
        CAST(c.Id AS BIGINT) AS HtsId,
        CAST(c.ProductId AS BIGINT) AS HtsProductId,
        CAST(c.PartId AS BIGINT) AS HtsPartId,
        CAST(c.UsingRate AS DECIMAL(18,4)) AS UsingRate,
        CAST(c.[Order] AS INT) AS [Order],
        CAST(c.IsDisabled AS BIT) AS IsDisabled,
        CAST(c.Comment AS NVARCHAR(2048)) AS Comment,
        CAST(c.CreatedDate AS DATETIME2) AS CreatedDate,
        CAST(c.CreatedDateInText AS NVARCHAR(30)) AS CreatedDateInText,
        CAST(NULL AS BIGINT) AS NewProductId,
        CAST(NULL AS BIGINT) AS NewPartId
    INTO #SrcCustom
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part_CustomBom] c;

    UPDATE c SET NewProductId = m.PartId
    FROM #SrcCustom c INNER JOIN #PartMap m ON m.HtsPartId = c.HtsProductId;
    UPDATE c SET NewPartId = m.PartId
    FROM #SrcCustom c INNER JOIN #PartMap m ON m.HtsPartId = c.HtsPartId;

    SET @Cnt = (SELECT COUNT(*) FROM #SrcCustom);
    SET @Cnt2 = (SELECT COUNT(*) FROM #SrcCustom WHERE NewProductId IS NULL);
    DECLARE @Cnt3 INT = (SELECT COUNT(*) FROM #SrcCustom WHERE NewPartId IS NULL);
    PRINT N'CustomBom source: ' + CAST(@Cnt AS NVARCHAR(20))
        + N', unmapped product: ' + CAST(@Cnt2 AS NVARCHAR(20))
        + N', unmapped part: ' + CAST(@Cnt3 AS NVARCHAR(20));

    /* ─── PartBookSection denormalized titles from HTS ─── */
    IF OBJECT_ID('tempdb..#SrcPartTitle') IS NOT NULL DROP TABLE #SrcPartTitle;
    SELECT CAST(op.Part_ID AS BIGINT) AS HtsPartId,
           CAST(op.PartBookSection AS NVARCHAR(2048)) AS PartBookSection,
           m.PartId AS NewPartId
    INTO #SrcPartTitle
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part] op
    INNER JOIN #PartMap m ON m.HtsPartId = op.Part_ID
    WHERE op.PartBookSection IS NOT NULL AND LTRIM(RTRIM(op.PartBookSection)) <> N'';

    SET @Cnt = (SELECT COUNT(*) FROM #SrcPartTitle);
    PRINT N'Part titles to sync: ' + CAST(@Cnt AS NVARCHAR(20));

    IF @DryRun = 1
    BEGIN
        PRINT N'DryRun=1 — no writes.';
        RETURN;
    END

    BEGIN TRANSACTION;

    DELETE FROM Inv.PartCustomBom;
    DELETE FROM Inv.PartPartBookSection;
    UPDATE Inv.PartBookSection SET PictureId = NULL;
    DELETE FROM Inv.PartBookSection;
    UPDATE Inv.Part SET PartBookSection = NULL WHERE PartBookSection IS NOT NULL;

    INSERT INTO Inv.PartBookSection
        (HtsId, Title, [Order], PictureTitle, IsForCustomBom, Comment,
         CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.Title, s.[Order], s.PictureTitle, s.IsForCustomBom, s.Comment,
        @SeedUser, @Now, @NowShamsi, 1
    FROM #SrcSection s
    ORDER BY s.HtsId;
    PRINT N'Inserted PartBookSection: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    UPDATE l SET NewSectionId = s.Id
    FROM #SrcLink l
    INNER JOIN Inv.PartBookSection s ON s.HtsId = l.HtsSectionId;

    INSERT INTO Inv.PartPartBookSection
        (HtsId, PartId, PartBookSectionId, [Order],
         CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    SELECT
        l.HtsId, l.NewPartId, l.NewSectionId, l.[Order],
        @SeedUser, @Now, @NowShamsi, 1
    FROM #SrcLink l
    WHERE l.NewPartId IS NOT NULL AND l.NewSectionId IS NOT NULL;
    PRINT N'Inserted PartPartBookSection: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Inv.PartCustomBom
        (HtsId, ProductId, PartId, UsingRate, [Order], IsDisabled, Comment,
         CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    SELECT
        c.HtsId, c.NewProductId, c.NewPartId, c.UsingRate, c.[Order], c.IsDisabled, c.Comment,
        @SeedUser,
        ISNULL(c.CreatedDate, @Now),
        ISNULL(c.CreatedDateInText, @NowShamsi),
        1
    FROM #SrcCustom c
    WHERE c.NewProductId IS NOT NULL AND c.NewPartId IS NOT NULL;
    PRINT N'Inserted PartCustomBom: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    UPDATE p SET PartBookSection = t.PartBookSection
    FROM Inv.Part p
    INNER JOIN #SrcPartTitle t ON t.NewPartId = p.Id;
    PRINT N'Updated Part.PartBookSection titles: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    SELECT N'PartBookSection' AS T, COUNT(*) AS Cnt FROM Inv.PartBookSection
    UNION ALL SELECT N'PartPartBookSection', COUNT(*) FROM Inv.PartPartBookSection
    UNION ALL SELECT N'PartCustomBom', COUNT(*) FROM Inv.PartCustomBom
    UNION ALL SELECT N'PartWithTitle', COUNT(*) FROM Inv.Part WHERE PartBookSection IS NOT NULL AND PartBookSection <> N'';

    PRINT N'=== DONE Migrate_InvPartBook_FromHts (run Migrate_InvPartBook_Pictures.py next) ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
