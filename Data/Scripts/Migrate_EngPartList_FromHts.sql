-- ============================================================================
-- Migrate Eng PartList (+ Inv.SimilarPart) from TotalSystem via linked server [TMS]
--
-- Source:
--   dbo.Eng_PartListProductSection (~921)
--   dbo.Eng_PartListProductBom (~18330)
--   dbo.Eng_PartListDlBom (~14504)
--   dbo.Inv_Similar_Part (~783)
--
-- Mappings:
--   Part:    Inv.Part.HtsId = Inv_Part.Part_ID (fallback Code = Part_Code)
--   DL:      TRY_CONVERT(INT, FIN.DL.Code) = Acc_DL.Hamkaran_Acc_DL_FK
--            (HTS DlId <= 0 → NULL in Eng.PartListDlBom)
--   Section: Eng.PartListProductSection.HtsId = source.Id
--   Picture: PicturePath copied as-is; FileEntity filled by companion script if file exists
--
-- Usage:
--   @DryRun = 1 (default) — counts only
--   @DryRun = 0 — truncate target tables (PartList* + SimilarPart) and reload
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 0;
DECLARE @SeedUser NVARCHAR(150) = N'migrate-eng-partlist-hts';
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] missing. Run CreateLinkedServer_TMS.sql first.', 16, 1);
    RETURN;
END

IF OBJECT_ID(N'Eng.PartListProductSection', N'U') IS NULL
   OR OBJECT_ID(N'Eng.PartListProductBom', N'U') IS NULL
   OR OBJECT_ID(N'Eng.PartListDlBom', N'U') IS NULL
   OR OBJECT_ID(N'Inv.SimilarPart', N'U') IS NULL
BEGIN
    RAISERROR(N'Target Eng/Inv PartList tables missing. Apply migration RebuildEngPartListAndSimilarPart first.', 16, 1);
    RETURN;
END

BEGIN TRY
    /* ─── DL map (Hts Acc_DL_ID → FIN.DL.Id) ─── */
    IF OBJECT_ID('tempdb..#DlMap') IS NOT NULL DROP TABLE #DlMap;
    SELECT ad.Acc_DL_ID AS HtsDlId, dl.Id AS DlId
    INTO #DlMap
    FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
    INNER JOIN FIN.DL dl ON TRY_CONVERT(INT, LTRIM(RTRIM(dl.Code))) = ad.Hamkaran_Acc_DL_FK;

    CREATE UNIQUE CLUSTERED INDEX IX_DlMap ON #DlMap(HtsDlId);

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

    DECLARE @PartMapCnt INT = (SELECT COUNT(*) FROM #PartMap);
    DECLARE @DlMapCnt INT = (SELECT COUNT(*) FROM #DlMap);
    PRINT N'Part map rows: ' + CAST(@PartMapCnt AS NVARCHAR(20));
    PRINT N'DL map rows: ' + CAST(@DlMapCnt AS NVARCHAR(20));

    /* ─── Source sections ─── */
    IF OBJECT_ID('tempdb..#SrcSection') IS NOT NULL DROP TABLE #SrcSection;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        CAST(s.ProductId AS BIGINT) AS HtsProductId,
        CAST(s.Title AS NVARCHAR(255)) AS Title,
        CAST(s.[Order] AS TINYINT) AS [Order],
        CAST(s.PicturePath AS NVARCHAR(1024)) AS PicturePath,
        CAST(s.IsCover AS BIT) AS IsCover,
        CAST(s.IsDisabled AS BIT) AS IsDisabled,
        CAST(s.Comment AS NVARCHAR(1024)) AS Comment,
        CAST(s.CreatedDate AS DATETIME2) AS CreatedDate,
        CAST(s.CreatedDateInText AS NVARCHAR(30)) AS CreatedDateInText,
        CAST(NULL AS BIGINT) AS NewProductId
    INTO #SrcSection
    FROM [TMS].[TotalSystem].[dbo].[Eng_PartListProductSection] s;

    UPDATE s SET NewProductId = m.PartId
    FROM #SrcSection s
    INNER JOIN #PartMap m ON m.HtsPartId = s.HtsProductId;

    DECLARE @SecTotal INT = (SELECT COUNT(*) FROM #SrcSection);
    DECLARE @SecUnmapped INT = (SELECT COUNT(*) FROM #SrcSection WHERE NewProductId IS NULL);
    PRINT N'Sections source: ' + CAST(@SecTotal AS NVARCHAR(20)) + N', unmapped product: ' + CAST(@SecUnmapped AS NVARCHAR(20));

    /* ─── Source BOM ─── */
    IF OBJECT_ID('tempdb..#SrcBom') IS NOT NULL DROP TABLE #SrcBom;
    SELECT
        CAST(b.Id AS BIGINT) AS HtsId,
        CAST(b.ProductSectionId AS BIGINT) AS HtsSectionId,
        CAST(b.ProductId AS BIGINT) AS HtsProductId,
        CAST(b.PartId AS BIGINT) AS HtsPartId,
        CAST(b.Qty AS DECIMAL(18,4)) AS Qty,
        CAST(b.[Order] AS SMALLINT) AS [Order],
        CAST(b.IsIgnored AS BIT) AS IsIgnored,
        CAST(b.Comment AS NVARCHAR(1024)) AS Comment,
        CAST(b.CreatedDate AS DATETIME2) AS CreatedDate,
        CAST(b.CreatedDateInText AS NVARCHAR(30)) AS CreatedDateInText,
        CAST(NULL AS BIGINT) AS NewProductId,
        CAST(NULL AS BIGINT) AS NewPartId
    INTO #SrcBom
    FROM [TMS].[TotalSystem].[dbo].[Eng_PartListProductBom] b;

    UPDATE b SET NewProductId = m.PartId FROM #SrcBom b INNER JOIN #PartMap m ON m.HtsPartId = b.HtsProductId;
    UPDATE b SET NewPartId = m.PartId FROM #SrcBom b INNER JOIN #PartMap m ON m.HtsPartId = b.HtsPartId;

    DECLARE @BomTotal INT = (SELECT COUNT(*) FROM #SrcBom);
    DECLARE @BomSkip INT = (SELECT COUNT(*) FROM #SrcBom WHERE NewProductId IS NULL OR NewPartId IS NULL);
    PRINT N'BOM source: ' + CAST(@BomTotal AS NVARCHAR(20)) + N', skip (unmapped part): ' + CAST(@BomSkip AS NVARCHAR(20));

    /* ─── Source DlBom ─── */
    IF OBJECT_ID('tempdb..#SrcDlBom') IS NOT NULL DROP TABLE #SrcDlBom;
    SELECT
        CAST(d.Id AS BIGINT) AS HtsId,
        CAST(d.DlId AS INT) AS HtsDlId,
        CAST(d.ProductId AS BIGINT) AS HtsProductId,
        CAST(d.ProductSectionId AS BIGINT) AS HtsSectionId,
        CAST(d.PartId AS BIGINT) AS HtsPartId,
        CAST(d.Qty AS DECIMAL(18,4)) AS Qty,
        CAST(d.[Order] AS SMALLINT) AS [Order],
        CAST(d.IsExistInOriginalBom AS BIT) AS IsExistInOriginalBom,
        CAST(d.IsIgnored AS BIT) AS IsIgnored,
        CAST(d.IsOnlyExistInOriginalBom AS BIT) AS IsOnlyExistInOriginalBom,
        CAST(d.IsReadFromSimilarPart AS BIT) AS IsReadFromSimilarPart,
        CAST(d.Comment AS NVARCHAR(1024)) AS Comment,
        CAST(d.CreatedDate AS DATETIME2) AS CreatedDate,
        CAST(d.CreatedDateInText AS NVARCHAR(30)) AS CreatedDateInText,
        CAST(NULL AS BIGINT) AS NewDlId,
        CAST(NULL AS BIGINT) AS NewProductId,
        CAST(NULL AS BIGINT) AS NewPartId
    INTO #SrcDlBom
    FROM [TMS].[TotalSystem].[dbo].[Eng_PartListDlBom] d;

    UPDATE d SET NewProductId = m.PartId FROM #SrcDlBom d INNER JOIN #PartMap m ON m.HtsPartId = d.HtsProductId;
    UPDATE d SET NewPartId = m.PartId FROM #SrcDlBom d INNER JOIN #PartMap m ON m.HtsPartId = d.HtsPartId;
    UPDATE d SET NewDlId = m.DlId
    FROM #SrcDlBom d
    INNER JOIN #DlMap m ON m.HtsDlId = d.HtsDlId
    WHERE d.HtsDlId > 0;

    DECLARE @DlTotal INT = (SELECT COUNT(*) FROM #SrcDlBom);
    DECLARE @DlSkipPart INT = (SELECT COUNT(*) FROM #SrcDlBom WHERE NewProductId IS NULL OR NewPartId IS NULL);
    DECLARE @DlSkipDl INT = (SELECT COUNT(*) FROM #SrcDlBom WHERE HtsDlId > 0 AND NewDlId IS NULL);
    DECLARE @DlZero INT = (SELECT COUNT(*) FROM #SrcDlBom WHERE HtsDlId <= 0);
    PRINT N'DlBom source: ' + CAST(@DlTotal AS NVARCHAR(20))
        + N', skip part: ' + CAST(@DlSkipPart AS NVARCHAR(20))
        + N', skip DL map: ' + CAST(@DlSkipDl AS NVARCHAR(20))
        + N', product-only (DlId<=0): ' + CAST(@DlZero AS NVARCHAR(20));

    /* ─── Source SimilarPart ─── */
    IF OBJECT_ID('tempdb..#SrcSimilar') IS NOT NULL DROP TABLE #SrcSimilar;
    SELECT
        CAST(sp.Similar_Part_ID AS BIGINT) AS HtsId,
        CAST(sp.BasePart_FK AS BIGINT) AS HtsBasePartId,
        CAST(sp.SimilarPart_FK AS BIGINT) AS HtsSimilarPartId,
        CAST(sp.Ratio AS DECIMAL(18,4)) AS Ratio,
        CAST(sp.Priority AS SMALLINT) AS Priority,
        CAST(sp.Hamkaran_SimilarPart_FK AS BIGINT) AS HamkaranSimilarPartId,
        CAST(sp.RahkaranId AS BIGINT) AS RahkaranId,
        CAST(NULL AS BIGINT) AS NewBasePartId,
        CAST(NULL AS BIGINT) AS NewSimilarPartId
    INTO #SrcSimilar
    FROM [TMS].[TotalSystem].[dbo].[Inv_Similar_Part] sp;

    UPDATE s SET NewBasePartId = m.PartId FROM #SrcSimilar s INNER JOIN #PartMap m ON m.HtsPartId = s.HtsBasePartId;
    UPDATE s SET NewSimilarPartId = m.PartId FROM #SrcSimilar s INNER JOIN #PartMap m ON m.HtsPartId = s.HtsSimilarPartId;

    DECLARE @SimTotal INT = (SELECT COUNT(*) FROM #SrcSimilar);
    DECLARE @SimSkip INT = (SELECT COUNT(*) FROM #SrcSimilar WHERE NewBasePartId IS NULL OR NewSimilarPartId IS NULL);
    PRINT N'SimilarPart source: ' + CAST(@SimTotal AS NVARCHAR(20)) + N', skip: ' + CAST(@SimSkip AS NVARCHAR(20));

    IF @DryRun = 1
    BEGIN
        PRINT N'DryRun=1 — no changes written.';
        RETURN;
    END

    BEGIN TRANSACTION;

    DELETE FROM Eng.PartListDlBom;
    DELETE FROM Eng.PartListProductBom;
    DELETE FROM Eng.PartListProductSection;
    DELETE FROM Inv.SimilarPart;

    /* Sections */
    INSERT INTO Eng.PartListProductSection
    (
        HtsId, ProductId, Title, [Order], PicturePath, IsCover, IsDisabled, Comment,
        CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
    )
    SELECT
        s.HtsId, s.NewProductId, s.Title, s.[Order], s.PicturePath, s.IsCover, s.IsDisabled, s.Comment,
        @SeedUser, s.CreatedDate, s.CreatedDateInText, 1
    FROM #SrcSection s
    WHERE s.NewProductId IS NOT NULL;

    DECLARE @InsSec INT = @@ROWCOUNT;
    PRINT N'Inserted sections: ' + CAST(@InsSec AS NVARCHAR(20));

    /* Section map HtsId → new Id */
    IF OBJECT_ID('tempdb..#SecMap') IS NOT NULL DROP TABLE #SecMap;
    SELECT HtsId, Id AS NewSectionId
    INTO #SecMap
    FROM Eng.PartListProductSection
    WHERE HtsId <> 0;
    CREATE UNIQUE CLUSTERED INDEX IX_SecMap ON #SecMap(HtsId);

    /* BOM */
    INSERT INTO Eng.PartListProductBom
    (
        HtsId, ProductSectionId, ProductId, PartId, Qty, [Order], IsIgnored, Comment,
        CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
    )
    SELECT
        b.HtsId,
        sm.NewSectionId,
        b.NewProductId,
        b.NewPartId,
        b.Qty,
        b.[Order],
        b.IsIgnored,
        b.Comment,
        @SeedUser, b.CreatedDate, b.CreatedDateInText, 1
    FROM #SrcBom b
    LEFT JOIN #SecMap sm ON sm.HtsId = b.HtsSectionId
    WHERE b.NewProductId IS NOT NULL AND b.NewPartId IS NOT NULL;

    DECLARE @InsBom INT = @@ROWCOUNT;
    PRINT N'Inserted BOM: ' + CAST(@InsBom AS NVARCHAR(20));

    /* DlBom — skip rows whose HtsDlId>0 but DL unmapped */
    INSERT INTO Eng.PartListDlBom
    (
        HtsId, DlId, ProductId, ProductSectionId, PartId, Qty, [Order],
        IsExistInOriginalBom, IsIgnored, IsOnlyExistInOriginalBom, IsReadFromSimilarPart, Comment,
        CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
    )
    SELECT
        d.HtsId,
        CASE WHEN d.HtsDlId <= 0 THEN NULL ELSE d.NewDlId END,
        d.NewProductId,
        sm.NewSectionId,
        d.NewPartId,
        d.Qty,
        d.[Order],
        d.IsExistInOriginalBom,
        d.IsIgnored,
        d.IsOnlyExistInOriginalBom,
        d.IsReadFromSimilarPart,
        d.Comment,
        @SeedUser, d.CreatedDate, d.CreatedDateInText, 1
    FROM #SrcDlBom d
    LEFT JOIN #SecMap sm ON sm.HtsId = d.HtsSectionId
    WHERE d.NewProductId IS NOT NULL
      AND d.NewPartId IS NOT NULL
      AND (d.HtsDlId <= 0 OR d.NewDlId IS NOT NULL);

    DECLARE @InsDl INT = @@ROWCOUNT;
    PRINT N'Inserted DlBom: ' + CAST(@InsDl AS NVARCHAR(20));

    /* SimilarPart */
    INSERT INTO Inv.SimilarPart
    (
        HtsId, BasePartId, SimilarPartId, Ratio, Priority, HamkaranSimilarPartId, RahkaranId,
        CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
    )
    SELECT
        s.HtsId, s.NewBasePartId, s.NewSimilarPartId, s.Ratio, s.Priority,
        s.HamkaranSimilarPartId, s.RahkaranId,
        @SeedUser, @Now, @NowShamsi, 1
    FROM #SrcSimilar s
    WHERE s.NewBasePartId IS NOT NULL AND s.NewSimilarPartId IS NOT NULL;

    DECLARE @InsSim INT = @@ROWCOUNT;
    PRINT N'Inserted SimilarPart: ' + CAST(@InsSim AS NVARCHAR(20));

    COMMIT TRANSACTION;

    PRINT N'=== DONE Migrate_EngPartList_FromHts ===';
    SELECT N'PartListProductSection' AS T, COUNT(*) AS Cnt FROM Eng.PartListProductSection
    UNION ALL SELECT N'PartListProductBom', COUNT(*) FROM Eng.PartListProductBom
    UNION ALL SELECT N'PartListDlBom', COUNT(*) FROM Eng.PartListDlBom
    UNION ALL SELECT N'SimilarPart', COUNT(*) FROM Inv.SimilarPart;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH
