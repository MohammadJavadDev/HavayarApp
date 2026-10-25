-- ============================================================================
-- کپی فیلدهای تماس تامین‌کننده (پیمانکار، نوع COMP) از HTS
--   [TMS].TotalSystem.dbo.Gnr_ManCompany → Gnr.Supplier / Gnr.Party
--
-- صفحه قدیمی: SupplayersSystem/_CompanyManagement (ManCompany_Type = COMP)
--   Email, Phone, Fax, PostBox, DlRef, Address, Grade,
--   ServicesAndProducts, RelatedPersonName, Mobile, CompanyName
--
-- نگاشت (اولین تطبیق برنده؛ یک تامین‌کننده به ازای هر Party):
--   1) Gnr.Party.HamkaranId = Gnr_ManCompany.Hamkaran_ManCompany_FK
--   2) Gnr.Party.HamkaranId = Gnr_ManCompany.ManCompany_ID
--   3) Gnr.Party.Id         = Gnr_ManCompany.ManCompany_ID
-- چند ردیف HTS برای یک Supplier: ردیفی که ایمیل دارد، سپس جدیدترین UpdatedDate.
--
-- فقط فیلد خالی پر می‌شود (@OverwriteExisting = 0).
-- اگر ایمیل HTS نبود و ایمیل Party پر بود، همان روی Supplier.Email می‌نشیند
-- تا فرم ویرایش و «ارسال به پیمانکار» همان ایمیل گرید را ببینند.
-- همراه فقط وقتی روی Party می‌نشیند که بعد از trim حداکثر ۲۰ نویسه باشد.
--
-- اجرا:
--   1) @DryRun = 1 (پیش‌فرض): شمارش — بدون تغییر
--   2) @DryRun = 0 : یک تراکنش
-- پیش‌نیاز: Linked Server [TMS]
-- این فایل باید UTF-8 with BOM باشد و با sqlcmd -f 65001 اجرا شود.
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
   OR COL_LENGTH('Gnr.Supplier', 'Email') IS NULL
   OR COL_LENGTH('Gnr.Supplier', 'Address') IS NULL
BEGIN
    RAISERROR(N'ستون‌های تماس روی Gnr.Supplier نیستند.', 16, 1);
    RETURN;
END

BEGIN TRY
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    IF OBJECT_ID('tempdb..#PartyByHamkaran') IS NOT NULL DROP TABLE #PartyByHamkaran;
    IF OBJECT_ID('tempdb..#SupplierByParty') IS NOT NULL DROP TABLE #SupplierByParty;
    IF OBJECT_ID('tempdb..#Final') IS NOT NULL DROP TABLE #Final;

    SELECT
        CAST(mc.ManCompany_ID AS BIGINT)              AS HtsId,
        CAST(mc.Hamkaran_ManCompany_FK AS BIGINT)     AS HamkaranId,
        CAST(mc.ManCompany_Type AS NVARCHAR(100))     AS ManCompanyType,
        CAST(mc.CompanyName AS NVARCHAR(256))         AS CompanyName,
        CAST(mc.FullName AS NVARCHAR(512))            AS FullName,
        CAST(mc.Email AS NVARCHAR(512))               AS Email,
        CAST(mc.Phone AS NVARCHAR(256))               AS Phone,
        CAST(mc.Fax AS NVARCHAR(256))                 AS Fax,
        CAST(mc.PostBox AS NVARCHAR(128))             AS PostBox,
        CAST(mc.DlRef AS NVARCHAR(80))                AS DlRef,
        CAST(mc.Mobile AS NVARCHAR(128))              AS Mobile,
        CAST(mc.Grade AS NVARCHAR(50))                AS Grade,
        CAST(mc.ServicesAndProducts AS NVARCHAR(256)) AS ServicesAndProducts,
        CAST(mc.RelatedPersonName AS NVARCHAR(128))   AS RelatedPersonName,
        CAST(mc.Address AS NVARCHAR(1024))            AS Address,
        CAST(mc.UpdatedDate AS DATETIME2)             AS UpdatedDate,
        CAST(NULL AS BIGINT)                          AS NewSupplierId,
        CAST(NULL AS BIGINT)                          AS NewPartyId,
        CAST(NULL AS NVARCHAR(100))                   AS MatchBy
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
    WHERE mc.ManCompany_Type = N'COMP';

    CREATE UNIQUE CLUSTERED INDEX IX_Src ON #Src(HtsId);

    DECLARE @SrcRows INT = (SELECT COUNT(*) FROM #Src);
    PRINT N'HTS COMP rows: ' + CAST(@SrcRows AS NVARCHAR(20));

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

    ;WITH RankedSup AS
    (
        SELECT s.Id, s.PartyId,
               ROW_NUMBER() OVER (PARTITION BY s.PartyId
                                  ORDER BY CASE WHEN s.IsActive = 1 THEN 0 ELSE 1 END, s.Id) AS Rn
        FROM Gnr.Supplier s
    )
    SELECT Id, PartyId INTO #SupplierByParty FROM RankedSup WHERE Rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_SupplierByParty ON #SupplierByParty(PartyId);

    UPDATE s
    SET NewPartyId = p.Id,
        NewSupplierId = sup.Id,
        MatchBy = N'HamkaranId'
    FROM #Src s
    INNER JOIN #PartyByHamkaran p ON p.HamkaranId = s.HamkaranId
    INNER JOIN #SupplierByParty sup ON sup.PartyId = p.Id
    WHERE s.NewSupplierId IS NULL
      AND s.HamkaranId IS NOT NULL AND s.HamkaranId <> 0;

    /* HamkaranId = ManCompany_ID فقط وقتی نام همان شرکت باشد.
       شناسه Party با شناسه HTS یکی نیست و نباید بدون نام تطبیق داده شود. */
    UPDATE s
    SET NewPartyId = py.Id,
        NewSupplierId = sup.Id,
        MatchBy = N'HamkaranId=ManCompany_ID'
    FROM #Src s
    INNER JOIN #PartyByHamkaran p ON p.HamkaranId = s.HtsId
    INNER JOIN Gnr.Party py ON py.Id = p.Id
    INNER JOIN #SupplierByParty sup ON sup.PartyId = p.Id
    WHERE s.NewSupplierId IS NULL
      AND (
            LTRIM(RTRIM(py.FullName)) = LTRIM(RTRIM(s.CompanyName))
         OR LTRIM(RTRIM(ISNULL(py.CompanyName, N''))) = LTRIM(RTRIM(s.CompanyName))
         OR LTRIM(RTRIM(py.FullName)) = LTRIM(RTRIM(s.FullName))
      );

    ;WITH RankedSrc AS
    (
        SELECT s.*,
               ROW_NUMBER() OVER (
                   PARTITION BY s.NewSupplierId
                   ORDER BY CASE WHEN NULLIF(LTRIM(RTRIM(s.Email)), N'') IS NOT NULL THEN 0 ELSE 1 END,
                            s.UpdatedDate DESC,
                            s.HtsId DESC) AS Rn
        FROM #Src s
        WHERE s.NewSupplierId IS NOT NULL
    )
    SELECT
        r.HtsId, r.HamkaranId, r.ManCompanyType, r.FullName,
        NULLIF(LTRIM(RTRIM(r.CompanyName)), N'')          AS CompanyName,
        NULLIF(LTRIM(RTRIM(r.Email)), N'')                AS Email,
        NULLIF(LTRIM(RTRIM(r.Phone)), N'')                AS Phone,
        NULLIF(LTRIM(RTRIM(r.Fax)), N'')                  AS Fax,
        NULLIF(LTRIM(RTRIM(r.PostBox)), N'')              AS PostBox,
        NULLIF(LTRIM(RTRIM(r.DlRef)), N'')                AS DlRef,
        NULLIF(LTRIM(RTRIM(r.Mobile)), N'')               AS Mobile,
        NULLIF(LTRIM(RTRIM(r.Grade)), N'')                AS Grade,
        NULLIF(LTRIM(RTRIM(r.ServicesAndProducts)), N'')  AS ServicesAndProducts,
        NULLIF(LTRIM(RTRIM(r.RelatedPersonName)), N'')    AS RelatedPersonName,
        NULLIF(LTRIM(RTRIM(r.Address)), N'')              AS Address,
        r.UpdatedDate, r.NewSupplierId, r.NewPartyId, r.MatchBy
    INTO #Final
    FROM RankedSrc r
    WHERE r.Rn = 1;

    DECLARE @Matched INT = (SELECT COUNT(*) FROM #Src WHERE NewSupplierId IS NOT NULL);
    DECLARE @Unmatched INT = (SELECT COUNT(*) FROM #Src WHERE NewSupplierId IS NULL);
    DECLARE @UnmatchedEmail INT = (
        SELECT COUNT(*) FROM #Src
        WHERE NewSupplierId IS NULL AND NULLIF(LTRIM(RTRIM(Email)), N'') IS NOT NULL);
    DECLARE @EmailFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE f.Email IS NOT NULL
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Email)), N'') IS NULL));
    DECLARE @PhoneFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE f.Phone IS NOT NULL
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.PhoneNumber)), N'') IS NULL));
    DECLARE @FaxFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE f.Fax IS NOT NULL
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Fax)), N'') IS NULL));
    DECLARE @PostalFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE f.PostBox IS NOT NULL
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.PostalCode)), N'') IS NULL));
    DECLARE @DlFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE f.DlRef IS NOT NULL
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.DlCode)), N'') IS NULL));
    DECLARE @AddressFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE f.Address IS NOT NULL
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Address)), N'') IS NULL));
    DECLARE @GradeFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE f.Grade IS NOT NULL
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Grade)), N'') IS NULL));
    DECLARE @ServicesFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE f.ServicesAndProducts IS NOT NULL
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.ServicesAndProducts)), N'') IS NULL));
    DECLARE @RelatedFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
        WHERE f.RelatedPersonName IS NOT NULL
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.RelatedPersonName)), N'') IS NULL));
    DECLARE @MobileFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Party p ON p.Id = f.NewPartyId
        WHERE f.Mobile IS NOT NULL
          AND LEN(f.Mobile) <= 20
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(p.Mobile)), N'') IS NULL));
    DECLARE @CompanyFill INT = (
        SELECT COUNT(*)
        FROM #Final f
        INNER JOIN Gnr.Party p ON p.Id = f.NewPartyId
        WHERE f.CompanyName IS NOT NULL
          AND LEN(f.CompanyName) <= 100
          AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(p.CompanyName)), N'') IS NULL));
    DECLARE @PartyEmailFallback INT = (
        SELECT COUNT(*)
        FROM Gnr.Supplier e
        INNER JOIN Gnr.Party p ON p.Id = e.PartyId
        WHERE NULLIF(LTRIM(RTRIM(e.Email)), N'') IS NULL
          AND NULLIF(LTRIM(RTRIM(p.Email)), N'') IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM #Final f
                WHERE f.NewSupplierId = e.Id
                  AND f.Email IS NOT NULL
          ));

    PRINT N'--- Summary (' + CASE WHEN @DryRun = 1 THEN N'DRY RUN' ELSE N'APPLY' END + N') ---';
    PRINT N'Source COMP rows            : ' + CAST(@SrcRows AS NVARCHAR(20));
    PRINT N'Matched                      : ' + CAST(@Matched AS NVARCHAR(20));
    PRINT N'Unmatched                    : ' + CAST(@Unmatched AS NVARCHAR(20));
    PRINT N'Unmatched with email         : ' + CAST(@UnmatchedEmail AS NVARCHAR(20));
    PRINT N'Email to fill                : ' + CAST(@EmailFill AS NVARCHAR(20));
    PRINT N'Phone to fill                : ' + CAST(@PhoneFill AS NVARCHAR(20));
    PRINT N'Fax to fill                  : ' + CAST(@FaxFill AS NVARCHAR(20));
    PRINT N'Postal to fill               : ' + CAST(@PostalFill AS NVARCHAR(20));
    PRINT N'Dl to fill                   : ' + CAST(@DlFill AS NVARCHAR(20));
    PRINT N'Address to fill              : ' + CAST(@AddressFill AS NVARCHAR(20));
    PRINT N'Grade to fill                : ' + CAST(@GradeFill AS NVARCHAR(20));
    PRINT N'Services to fill             : ' + CAST(@ServicesFill AS NVARCHAR(20));
    PRINT N'Related person to fill       : ' + CAST(@RelatedFill AS NVARCHAR(20));
    PRINT N'Party mobile to fill         : ' + CAST(@MobileFill AS NVARCHAR(20));
    PRINT N'Party company name to fill   : ' + CAST(@CompanyFill AS NVARCHAR(20));
    PRINT N'Party email fallback         : ' + CAST(@PartyEmailFallback AS NVARCHAR(20));

    SELECT TOP (30)
        N'EMAIL' AS Action,
        f.MatchBy, f.HtsId, f.NewSupplierId, f.CompanyName, f.Email
    FROM #Final f
    INNER JOIN Gnr.Supplier e ON e.Id = f.NewSupplierId
    WHERE f.Email IS NOT NULL
      AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Email)), N'') IS NULL)
    ORDER BY f.CompanyName;

    IF @DryRun = 1
    BEGIN
        PRINT N'DRY RUN — هیچ تغییری اعمال نشد. برای اجرای واقعی @DryRun = 0 بگذارید.';
        RETURN;
    END

    BEGIN TRANSACTION;

    UPDATE e
    SET e.Email = CASE
            WHEN f.Email IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Email)), N'') IS NULL)
                THEN LEFT(f.Email, 200) ELSE e.Email END,
        e.PhoneNumber = CASE
            WHEN f.Phone IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.PhoneNumber)), N'') IS NULL)
                THEN LEFT(f.Phone, 200) ELSE e.PhoneNumber END,
        e.Fax = CASE
            WHEN f.Fax IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Fax)), N'') IS NULL)
                THEN LEFT(f.Fax, 200) ELSE e.Fax END,
        e.PostalCode = CASE
            WHEN f.PostBox IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.PostalCode)), N'') IS NULL)
                THEN LEFT(f.PostBox, 200) ELSE e.PostalCode END,
        e.DlCode = CASE
            WHEN f.DlRef IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.DlCode)), N'') IS NULL)
                THEN LEFT(f.DlRef, 200) ELSE e.DlCode END,
        e.Address = CASE
            WHEN f.Address IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Address)), N'') IS NULL)
                THEN LEFT(f.Address, 2000) ELSE e.Address END,
        e.Grade = CASE
            WHEN f.Grade IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Grade)), N'') IS NULL)
                THEN LEFT(f.Grade, 50) ELSE e.Grade END,
        e.ServicesAndProducts = CASE
            WHEN f.ServicesAndProducts IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.ServicesAndProducts)), N'') IS NULL)
                THEN LEFT(f.ServicesAndProducts, 256) ELSE e.ServicesAndProducts END,
        e.RelatedPersonName = CASE
            WHEN f.RelatedPersonName IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.RelatedPersonName)), N'') IS NULL)
                THEN LEFT(f.RelatedPersonName, 128) ELSE e.RelatedPersonName END,
        e.ModifiedById = 1,
        e.ModifiedByName = @SeedUser,
        e.ModifiedDateMiladiDateTime = @Now,
        e.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Gnr.Supplier e
    INNER JOIN #Final f ON f.NewSupplierId = e.Id
    WHERE (f.Email IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Email)), N'') IS NULL))
       OR (f.Phone IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.PhoneNumber)), N'') IS NULL))
       OR (f.Fax IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Fax)), N'') IS NULL))
       OR (f.PostBox IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.PostalCode)), N'') IS NULL))
       OR (f.DlRef IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.DlCode)), N'') IS NULL))
       OR (f.Address IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Address)), N'') IS NULL))
       OR (f.Grade IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.Grade)), N'') IS NULL))
       OR (f.ServicesAndProducts IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.ServicesAndProducts)), N'') IS NULL))
       OR (f.RelatedPersonName IS NOT NULL AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(e.RelatedPersonName)), N'') IS NULL));

    DECLARE @UpdatedSupplier INT = @@ROWCOUNT;

    UPDATE e
    SET e.Email = LEFT(LTRIM(RTRIM(p.Email)), 200),
        e.ModifiedById = 1,
        e.ModifiedByName = @SeedUser,
        e.ModifiedDateMiladiDateTime = @Now,
        e.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Gnr.Supplier e
    INNER JOIN Gnr.Party p ON p.Id = e.PartyId
    WHERE NULLIF(LTRIM(RTRIM(e.Email)), N'') IS NULL
      AND NULLIF(LTRIM(RTRIM(p.Email)), N'') IS NOT NULL;

    DECLARE @UpdatedFromParty INT = @@ROWCOUNT;

    /* ایمیل فعلی کد ملی / تلفن / آدرس است و HTS ایمیل واقعی همان شرکت را دارد */
    UPDATE e
    SET e.Email = LEFT(LTRIM(RTRIM(f.Email)), 200),
        e.ModifiedById = 1,
        e.ModifiedByName = @SeedUser,
        e.ModifiedDateMiladiDateTime = @Now,
        e.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Gnr.Supplier e
    INNER JOIN #Final f ON f.NewSupplierId = e.Id
    INNER JOIN Gnr.Party p ON p.Id = e.PartyId
    WHERE f.Email LIKE N'%@%'
      AND CHARINDEX(N'@', ISNULL(e.Email, N'')) = 0
      AND (
            LTRIM(RTRIM(p.FullName)) = LTRIM(RTRIM(f.CompanyName))
         OR LTRIM(RTRIM(ISNULL(p.CompanyName, N''))) = LTRIM(RTRIM(f.CompanyName))
      );

    DECLARE @ReplacedNonEmail INT = @@ROWCOUNT;

    UPDATE p
    SET p.Mobile = CASE
            WHEN f.Mobile IS NOT NULL AND LEN(f.Mobile) <= 20
                 AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(p.Mobile)), N'') IS NULL)
                THEN f.Mobile ELSE p.Mobile END,
        p.CompanyName = CASE
            WHEN f.CompanyName IS NOT NULL AND LEN(f.CompanyName) <= 100
                 AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(p.CompanyName)), N'') IS NULL)
                THEN f.CompanyName ELSE p.CompanyName END,
        p.ModifiedById = 1,
        p.ModifiedByName = @SeedUser,
        p.ModifiedDateMiladiDateTime = @Now,
        p.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Gnr.Party p
    INNER JOIN #Final f ON f.NewPartyId = p.Id
    WHERE f.MatchBy = N'HamkaranId'
      AND (
            (f.Mobile IS NOT NULL AND LEN(f.Mobile) <= 20
             AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(p.Mobile)), N'') IS NULL))
         OR (f.CompanyName IS NOT NULL AND LEN(f.CompanyName) <= 100
             AND (@OverwriteExisting = 1 OR NULLIF(LTRIM(RTRIM(p.CompanyName)), N'') IS NULL))
      );

    DECLARE @UpdatedParty INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    PRINT N'--- Applied ---';
    PRINT N'Updated suppliers from HTS : ' + CAST(@UpdatedSupplier AS NVARCHAR(20));
    PRINT N'Emails copied from Party   : ' + CAST(@UpdatedFromParty AS NVARCHAR(20));
    PRINT N'Non-email replaced         : ' + CAST(@ReplacedNonEmail AS NVARCHAR(20));
    PRINT N'Updated parties            : ' + CAST(@UpdatedParty AS NVARCHAR(20));
    PRINT N'=== DONE Migrate_Supplier_HtsFields_FromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
