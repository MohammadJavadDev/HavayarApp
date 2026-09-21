-- ============================================================================
-- WP9 / D16 (Q5-a) — کپی یک‌باره فیلدهای HTS-only تامین‌کننده
--                    از [TMS].TotalSystem.dbo.Gnr_ManCompany → Gnr.Supplier
--
-- منبع: Linked Server [TMS] → Gnr_ManCompany
--   Grade (nvarchar 50), ServicesAndProducts (256), RelatedPersonName (128), Address (1024)
--
-- نگاشت تامین‌کننده (اولین تطبیق برنده؛ یک تامین‌کننده به ازای هر Party):
--   1) HamkaranId: Gnr.Party.HamkaranId = Gnr_ManCompany.Hamkaran_ManCompany_FK
--   2) HamkaranId: Gnr.Party.HamkaranId = Gnr_ManCompany.ManCompany_ID
--      (وقتی FK خالی است یا ردیف ۱ تطبیق نداد — همان الگوی شرکت‌ها)
--   3) HtsId:      Gnr.Party.Id        = Gnr_ManCompany.ManCompany_ID
-- Gnr.Supplier خودش HtsId/HamkaranId ندارد؛ تطبیق از طریق Party است.
-- چند ردیف HTS برای یک Supplier: نوع COMP، سپس جدیدترین UpdatedDate، سپس ManCompany_ID.
--
-- اجرا:
--   1) @DryRun = 1 (پیش‌فرض): شمارش + پیش‌نمایش UPDATE + فهرست بی‌تطبیق‌ها — بدون تغییر
--   2) بعد از بررسی: @DryRun = 0 → یک تراکنش
--   @OverwriteExisting = 0 (پیش‌فرض): فقط فیلد خالی/NULL پر می‌شود
-- پیش‌نیاز: Linked Server TMS، ستون‌های D16 (migration / Add_Supplier_HtsFields.sql)
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;                    -- 0 = اجرای واقعی
DECLARE @OverwriteExisting BIT = 0;         -- 1 = بازنویسی مقدار موجود
DECLARE @SeedUser NVARCHAR(150) = N'migrate-supplier-hts';
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] وجود ندارد. ابتدا Data/Scripts/CreateLinkedServer_TMS.sql را اجرا کنید.', 16, 1);
    RETURN;
END

IF COL_LENGTH('Gnr.Supplier', 'Grade') IS NULL
   OR COL_LENGTH('Gnr.Supplier', 'ServicesAndProducts') IS NULL
   OR COL_LENGTH('Gnr.Supplier', 'RelatedPersonName') IS NULL
   OR COL_LENGTH('Gnr.Supplier', 'Address') IS NULL
BEGIN
    RAISERROR(N'ستون‌های D16 روی Gnr.Supplier نیستند. ابتدا migration AddSupplierHtsFields یا Add_Supplier_HtsFields.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRY
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    IF OBJECT_ID('tempdb..#PartyByHamkaran') IS NOT NULL DROP TABLE #PartyByHamkaran;
    IF OBJECT_ID('tempdb..#SupplierByParty') IS NOT NULL DROP TABLE #SupplierByParty;
    IF OBJECT_ID('tempdb..#Final') IS NOT NULL DROP TABLE #Final;

    /* ─────────────────────────────────────────────
       0) منبع HTS (یک رفت‌وبرگشت به linked server)
       ───────────────────────────────────────────── */
    SELECT
        CAST(mc.ManCompany_ID AS BIGINT)              AS HtsId,
        CAST(mc.Hamkaran_ManCompany_FK AS BIGINT)     AS HamkaranId,
        CAST(mc.ManCompany_Type AS NVARCHAR(100))     AS ManCompanyType,
        CAST(mc.CompanyName AS NVARCHAR(256))         AS CompanyName,
        CAST(mc.FullName AS NVARCHAR(512))            AS FullName,
        CAST(mc.Grade AS NVARCHAR(50))                AS Grade,
        CAST(mc.ServicesAndProducts AS NVARCHAR(256)) AS ServicesAndProducts,
        CAST(mc.RelatedPersonName AS NVARCHAR(128))   AS RelatedPersonName,
        CAST(mc.Address AS NVARCHAR(1024))            AS Address,
        CAST(mc.UpdatedDate AS DATETIME2)             AS UpdatedDate,
        CAST(NULL AS BIGINT)                          AS NewSupplierId,
        CAST(NULL AS BIGINT)                          AS NewPartyId,
        CAST(NULL AS NVARCHAR(100))                   AS MatchBy
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc;

    CREATE UNIQUE CLUSTERED INDEX IX_Src ON #Src(HtsId);

    DECLARE @SrcRows INT = (SELECT COUNT(*) FROM #Src);
    PRINT N'HTS Gnr_ManCompany rows: ' + CAST(@SrcRows AS NVARCHAR(20));

    -- یک Party به ازای هر HamkaranId (فعال، سپس کوچک‌ترین Id)
    ;WITH RankedParty AS
    (
        SELECT p.Id, p.HamkaranId,
               ROW_NUMBER() OVER (PARTITION BY p.HamkaranId
                                  ORDER BY CASE WHEN p.IsActive = 1 THEN 0 ELSE 1 END, p.Id) AS Rn
        FROM Gnr.Party p
        WHERE p.HamkaranId IS NOT NULL AND p.HamkaranId <> 0
    )
    SELECT Id, HamkaranId INTO #PartyByHamkaran FROM RankedParty WHERE Rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_PartyByHamkaran ON #PartyByHamkaran(HamkaranId);

    -- یک Supplier به ازای هر Party (فعال، سپس کوچک‌ترین Id)
    ;WITH RankedSup AS
    (
        SELECT s.Id, s.PartyId,
               ROW_NUMBER() OVER (PARTITION BY s.PartyId
                                  ORDER BY CASE WHEN s.IsActive = 1 THEN 0 ELSE 1 END, s.Id) AS Rn
        FROM Gnr.Supplier s
    )
    SELECT Id, PartyId INTO #SupplierByParty FROM RankedSup WHERE Rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_SupplierByParty ON #SupplierByParty(PartyId);

    /* 1) HamkaranId = Hamkaran_ManCompany_FK */
    UPDATE s
    SET NewPartyId = p.Id,
        NewSupplierId = sup.Id,
        MatchBy = N'HamkaranId'
    FROM #Src s
    INNER JOIN #PartyByHamkaran p ON p.HamkaranId = s.HamkaranId
    INNER JOIN #SupplierByParty sup ON sup.PartyId = p.Id
    WHERE s.NewSupplierId IS NULL
      AND s.HamkaranId IS NOT NULL AND s.HamkaranId <> 0;

    /* 2) HamkaranId = ManCompany_ID */
    UPDATE s
    SET NewPartyId = p.Id,
        NewSupplierId = sup.Id,
        MatchBy = N'HamkaranId=ManCompany_ID'
    FROM #Src s
    INNER JOIN #PartyByHamkaran p ON p.HamkaranId = s.HtsId
    INNER JOIN #SupplierByParty sup ON sup.PartyId = p.Id
    WHERE s.NewSupplierId IS NULL;

    /* 3) HtsId: Party.Id = ManCompany_ID */
    UPDATE s
    SET NewPartyId = p.Id,
        NewSupplierId = sup.Id,
        MatchBy = N'HtsId'
    FROM #Src s
    INNER JOIN Gnr.Party p ON p.Id = s.HtsId
    INNER JOIN #SupplierByParty sup ON sup.PartyId = p.Id
    WHERE s.NewSupplierId IS NULL;

    /* یک ردیف HTS برنده برای هر Supplier */
    ;WITH RankedSrc AS
    (
        SELECT s.*,
               ROW_NUMBER() OVER (
                   PARTITION BY s.NewSupplierId
                   ORDER BY CASE WHEN s.ManCompanyType = N'COMP' THEN 0 ELSE 1 END,
                            s.UpdatedDate DESC,
                            s.HtsId DESC) AS Rn
        FROM #Src s
        WHERE s.NewSupplierId IS NOT NULL
    )
    SELECT
        r.HtsId, r.HamkaranId, r.ManCompanyType, r.CompanyName, r.FullName,
        NULLIF(LTRIM(RTRIM(r.Grade)), N'')                AS Grade,
        NULLIF(LTRIM(RTRIM(r.ServicesAndProducts)), N'')  AS ServicesAndProducts,
        NULLIF(LTRIM(RTRIM(r.RelatedPersonName)), N'')    AS RelatedPersonName,
        NULLIF(LTRIM(RTRIM(r.Address)), N'')              AS Address,
        r.UpdatedDate, r.NewSupplierId, r.NewPartyId, r.MatchBy
    INTO #Final
    FROM RankedSrc r
    WHERE r.Rn = 1;

    DECLARE @Matched INT     = (SELECT COUNT(*) FROM #Src WHERE NewSupplierId IS NOT NULL);
    DECLARE @Unmatched INT   = (SELECT COUNT(*) FROM #Src WHERE NewSupplierId IS NULL);
    DECLARE @SrcWithVal INT = (
        SELECT COUNT(*) FROM #Src
        WHERE NULLIF(LTRIM(RTRIM(Grade)), N'') IS NOT NULL
           OR NULLIF(LTRIM(RTRIM(ServicesAndProducts)), N'') IS NOT NULL
           OR NULLIF(LTRIM(RTRIM(RelatedPersonName)), N'') IS NOT NULL
           OR NULLIF(LTRIM(RTRIM(Address)), N'') IS NOT NULL);
    DECLARE @UnmatchedVal INT = (
        SELECT COUNT(*) FROM #Src
        WHERE NewSupplierId IS NULL
          AND (
                NULLIF(LTRIM(RTRIM(Grade)), N'') IS NOT NULL
             OR NULLIF(LTRIM(RTRIM(ServicesAndProducts)), N'') IS NOT NULL
             OR NULLIF(LTRIM(RTRIM(RelatedPersonName)), N'') IS NOT NULL
             OR NULLIF(LTRIM(RTRIM(Address)), N'') IS NOT NULL
          ));
    DECLARE @Collapsed INT   = @Matched - (SELECT COUNT(*) FROM #Final);
    DECLARE @ByHamkaran INT  = (SELECT COUNT(*) FROM #Src WHERE MatchBy = N'HamkaranId');
    DECLARE @ByManCoId INT   = (SELECT COUNT(*) FROM #Src WHERE MatchBy = N'HamkaranId=ManCompany_ID');
    DECLARE @ByHtsId INT     = (SELECT COUNT(*) FROM #Src WHERE MatchBy = N'HtsId');
    DECLARE @ToUpdate INT    = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE (f.Grade IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Grade)), N'') IS NULL))
           OR (f.ServicesAndProducts IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.ServicesAndProducts)), N'') IS NULL))
           OR (f.RelatedPersonName IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.RelatedPersonName)), N'') IS NULL))
           OR (f.Address IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Address)), N'') IS NULL))
    );
    DECLARE @NoValue INT = (
        SELECT COUNT(*) FROM #Final
        WHERE Grade IS NULL AND ServicesAndProducts IS NULL AND RelatedPersonName IS NULL AND Address IS NULL);

    PRINT N'--- Summary (' + CASE WHEN @DryRun = 1 THEN N'DRY RUN' ELSE N'APPLY' END + N') ---';
    PRINT N'Source rows                 : ' + CAST(@SrcRows AS NVARCHAR(20));
    PRINT N'Source with any of 4 fields : ' + CAST(@SrcWithVal AS NVARCHAR(20));
    PRINT N'Matched HTS → Supplier      : ' + CAST(@Matched AS NVARCHAR(20))
        + N'  (HamkaranFK=' + CAST(@ByHamkaran AS NVARCHAR(20))
        + N', Hamkaran=ManCompany_ID=' + CAST(@ByManCoId AS NVARCHAR(20))
        + N', HtsId=Party.Id=' + CAST(@ByHtsId AS NVARCHAR(20)) + N')';
    PRINT N'Unmatched HTS (all)         : ' + CAST(@Unmatched AS NVARCHAR(20));
    PRINT N'Unmatched with HTS-only val : ' + CAST(@UnmatchedVal AS NVARCHAR(20));
    PRINT N'Collapsed duplicates        : ' + CAST(@Collapsed AS NVARCHAR(20));
    PRINT N'Matched but all 4 empty     : ' + CAST(@NoValue AS NVARCHAR(20));
    PRINT N'Suppliers that would update : ' + CAST(@ToUpdate AS NVARCHAR(20));

    -- فهرست بی‌تطبیق‌هایی که حداقل یکی از چهار فیلد را دارند
    SELECT
        N'تامین‌کننده یافت نشد' AS Reason,
        s.HtsId, s.HamkaranId, s.ManCompanyType, s.CompanyName, s.FullName,
        s.Grade, s.ServicesAndProducts, s.RelatedPersonName, s.Address
    FROM #Src s
    WHERE s.NewSupplierId IS NULL
      AND (
            NULLIF(LTRIM(RTRIM(s.Grade)), N'') IS NOT NULL
         OR NULLIF(LTRIM(RTRIM(s.ServicesAndProducts)), N'') IS NOT NULL
         OR NULLIF(LTRIM(RTRIM(s.RelatedPersonName)), N'') IS NOT NULL
         OR NULLIF(LTRIM(RTRIM(s.Address)), N'') IS NOT NULL
      )
    ORDER BY s.ManCompanyType, s.CompanyName, s.HtsId;

    IF @DryRun = 1
    BEGIN
        SELECT
            N'UPDATE' AS Action,
            f.MatchBy, f.HtsId, f.HamkaranId, f.ManCompanyType,
            f.NewSupplierId, f.NewPartyId, p.FullName AS PartyFullName,
            f.Grade, e.Grade AS CurrentGrade,
            f.ServicesAndProducts, e.ServicesAndProducts AS CurrentServicesAndProducts,
            f.RelatedPersonName, e.RelatedPersonName AS CurrentRelatedPersonName,
            f.Address, e.Address AS CurrentAddress
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        LEFT JOIN Gnr.Party p ON p.Id = f.NewPartyId
        WHERE (f.Grade IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Grade)), N'') IS NULL))
           OR (f.ServicesAndProducts IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.ServicesAndProducts)), N'') IS NULL))
           OR (f.RelatedPersonName IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.RelatedPersonName)), N'') IS NULL))
           OR (f.Address IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Address)), N'') IS NULL))
        ORDER BY p.FullName, f.HtsId;

        PRINT N'DRY RUN — هیچ تغییری اعمال نشد. برای اجرای واقعی @DryRun = 0 بگذارید.';
        RETURN;
    END

    BEGIN TRANSACTION;

    UPDATE e
    SET e.Grade = CASE
            WHEN f.Grade IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Grade)), N'') IS NULL)
                THEN f.Grade ELSE e.Grade END,
        e.ServicesAndProducts = CASE
            WHEN f.ServicesAndProducts IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.ServicesAndProducts)), N'') IS NULL)
                THEN f.ServicesAndProducts ELSE e.ServicesAndProducts END,
        e.RelatedPersonName = CASE
            WHEN f.RelatedPersonName IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.RelatedPersonName)), N'') IS NULL)
                THEN f.RelatedPersonName ELSE e.RelatedPersonName END,
        e.Address = CASE
            WHEN f.Address IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Address)), N'') IS NULL)
                THEN f.Address ELSE e.Address END,
        e.ModifiedById = 1,
        e.ModifiedByName = @SeedUser,
        e.ModifiedDateMiladiDateTime = @Now,
        e.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Gnr.Supplier e
    INNER JOIN #Final f ON f.NewSupplierId = e.Id
    WHERE (f.Grade IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Grade)), N'') IS NULL))
       OR (f.ServicesAndProducts IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.ServicesAndProducts)), N'') IS NULL))
       OR (f.RelatedPersonName IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.RelatedPersonName)), N'') IS NULL))
       OR (f.Address IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Address)), N'') IS NULL));

    DECLARE @Updated INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    PRINT N'--- Applied ---';
    PRINT N'Updated suppliers: ' + CAST(@Updated AS NVARCHAR(20));
    PRINT N'=== DONE Migrate_Supplier_HtsFields_FromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
