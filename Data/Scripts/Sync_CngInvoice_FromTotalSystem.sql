/*
Sync_CngInvoice_FromTotalSystem.sql
همگام‌سازی Cng.Invoice + Cng.InvoiceItem از [TMS].[TotalSystem].
HtsId = CAST(old.Id AS BIGINT). Customer via SLS.Customer.HtsId؛ Part via Inv.Part.HtsId.
کاربر CreatedUserId / CreatedBy از #UserMap (Username / AD / Email).
ردیف‌های بدون نگاشت مشتری/کالا/کاربر گزارش و درج نمی‌شوند.
Idempotent MERGE روی HtsId. همه ردیف‌ها (داده جاری).
UTF-8 BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Cng.Invoice', N'U') IS NULL OR OBJECT_ID(N'Cng.InvoiceItem', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول Cng.Invoice / InvoiceItem وجود ندارند.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-cng-invoice';

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== User map ========== */
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, NewUserId, NewUserName
    INTO #UserMap
    FROM (
        SELECT
            CAST(ou.User_ID AS INT) AS OldUserId,
            nu.Id AS NewUserId,
            COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username) AS NewUserName,
            ROW_NUMBER() OVER (
                PARTITION BY ou.User_ID
                ORDER BY
                    CASE WHEN LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username))) THEN 0 ELSE 1 END,
                    nu.Id
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN system.[User] nu
            ON LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
            OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
            OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    /* ========== Headers ========== */
    PRINT N'=== Cng.Invoice ===';
    IF OBJECT_ID('tempdb..#HdrSrc') IS NOT NULL DROP TABLE #HdrSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        cust.Id AS CustomerId,
        NULLIF(LTRIM(RTRIM(s.InvoiceNumber)), N'') AS InvoiceNumber,
        NULLIF(LTRIM(RTRIM(s.InvoiceDate)), N'') AS InvoiceDate,
        umCreate.NewUserId AS CreatedUserId,
        CAST(s.CreatedDate AS datetime2) AS CreatedDate,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N''), @NowShamsi), 10) AS CreatedDateInText,
        umCreate.NewUserId AS CreatedById,
        umCreate.NewUserName AS CreatedByName,
        CAST(s.CreatedDate AS datetime2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        CASE WHEN cust.Id IS NULL THEN 1 ELSE 0 END AS SkipCustomer,
        CASE WHEN umCreate.NewUserId IS NULL THEN 1 ELSE 0 END AS SkipUser
    INTO #HdrSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_Invoice] s
    LEFT JOIN SLS.Customer cust ON cust.HtsId = CAST(s.CustomerId AS BIGINT) AND ISNULL(cust.HtsId, 0) <> 0
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = CAST(s.CreatedUserId AS INT);

    DECLARE @HdrCnt INT = (SELECT COUNT(*) FROM #HdrSrc);
    DECLARE @SkipCust INT = (SELECT COUNT(*) FROM #HdrSrc WHERE SkipCustomer = 1);
    DECLARE @SkipUser INT = (SELECT COUNT(*) FROM #HdrSrc WHERE SkipUser = 1);
    PRINT N'  Headers source: ' + CAST(@HdrCnt AS nvarchar(20));
    PRINT N'  Headers skipped (customer unmapped): ' + CAST(@SkipCust AS nvarchar(20));
    PRINT N'  Headers skipped (user unmapped): ' + CAST(@SkipUser AS nvarchar(20));

    IF @SkipCust > 0 OR @SkipUser > 0
    BEGIN
        PRINT N'  Sample skipped header HTS Ids:';
        SELECT TOP 50 HtsId, SkipCustomer, SkipUser
        FROM #HdrSrc
        WHERE SkipCustomer = 1 OR SkipUser = 1
        ORDER BY HtsId;
    END

    IF OBJECT_ID('tempdb..#HdrMapped') IS NOT NULL DROP TABLE #HdrMapped;
    SELECT * INTO #HdrMapped FROM #HdrSrc WHERE SkipCustomer = 0 AND SkipUser = 0;

    UPDATE t SET
        t.CustomerId = s.CustomerId,
        t.InvoiceNumber = s.InvoiceNumber,
        t.InvoiceDate = s.InvoiceDate,
        t.CreatedUserId = s.CreatedUserId,
        t.CreatedDate = s.CreatedDate,
        t.CreatedDateInText = s.CreatedDateInText,
        t.CreatedById = COALESCE(s.CreatedById, t.CreatedById),
        t.CreatedByName = COALESCE(s.CreatedByName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedOnMiladiDateTime, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(s.CreatedOnShamsiDateTime, t.CreatedOnShamsiDateTime),
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.Invoice t
    INNER JOIN #HdrMapped s ON s.HtsId = t.HtsId;
    PRINT N'  Invoice updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.Invoice (
        HtsId, CustomerId, InvoiceNumber, InvoiceDate,
        CreatedUserId, CreatedDate, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.CustomerId, s.InvoiceNumber, s.InvoiceDate,
        s.CreatedUserId, s.CreatedDate, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName,
        ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
        1, @Seed, @Now, @NowShamsi, 1
    FROM #HdrMapped s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.Invoice t WHERE t.HtsId = s.HtsId);
    PRINT N'  Invoice inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    /* ========== Items ========== */
    PRINT N'=== Cng.InvoiceItem ===';
    IF OBJECT_ID('tempdb..#ItemSrc') IS NOT NULL DROP TABLE #ItemSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        inv.Id AS InvoiceId,
        p.Id AS PartId,
        CAST(s.Quantity AS SMALLINT) AS Quantity,
        CAST(s.Price AS BIGINT) AS Price,
        umCreate.NewUserId AS CreatedUserId,
        CAST(s.CreatedDate AS datetime2) AS CreatedDate,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N''), @NowShamsi), 10) AS CreatedDateInText,
        umCreate.NewUserId AS CreatedById,
        umCreate.NewUserName AS CreatedByName,
        CAST(s.CreatedDate AS datetime2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        CASE WHEN inv.Id IS NULL THEN 1 ELSE 0 END AS SkipParent,
        CASE WHEN p.Id IS NULL THEN 1 ELSE 0 END AS SkipPart,
        CASE WHEN umCreate.NewUserId IS NULL THEN 1 ELSE 0 END AS SkipUser
    INTO #ItemSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_InvoiceItems] s
    LEFT JOIN Cng.Invoice inv ON inv.HtsId = CAST(s.InvoiceId AS BIGINT)
    LEFT JOIN Inv.Part p ON p.HtsId = CAST(s.PartId AS BIGINT) AND ISNULL(p.HtsId, 0) <> 0
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = CAST(s.CreatedUserId AS INT);

    DECLARE @ItemCnt INT = (SELECT COUNT(*) FROM #ItemSrc);
    DECLARE @SkipParent INT = (SELECT COUNT(*) FROM #ItemSrc WHERE SkipParent = 1);
    DECLARE @SkipPart INT = (SELECT COUNT(*) FROM #ItemSrc WHERE SkipPart = 1);
    DECLARE @SkipItemUser INT = (SELECT COUNT(*) FROM #ItemSrc WHERE SkipUser = 1);
    PRINT N'  Items source: ' + CAST(@ItemCnt AS nvarchar(20));
    PRINT N'  Items skipped (parent missing): ' + CAST(@SkipParent AS nvarchar(20));
    PRINT N'  Items skipped (part unmapped): ' + CAST(@SkipPart AS nvarchar(20));
    PRINT N'  Items skipped (user unmapped): ' + CAST(@SkipItemUser AS nvarchar(20));

    IF @SkipParent > 0 OR @SkipPart > 0 OR @SkipItemUser > 0
    BEGIN
        PRINT N'  Sample skipped item HTS Ids:';
        SELECT TOP 50 HtsId, SkipParent, SkipPart, SkipUser
        FROM #ItemSrc
        WHERE SkipParent = 1 OR SkipPart = 1 OR SkipUser = 1
        ORDER BY HtsId;
    END

    IF OBJECT_ID('tempdb..#ItemMapped') IS NOT NULL DROP TABLE #ItemMapped;
    SELECT * INTO #ItemMapped FROM #ItemSrc
    WHERE SkipParent = 0 AND SkipPart = 0 AND SkipUser = 0;

    UPDATE t SET
        t.InvoiceId = s.InvoiceId,
        t.PartId = s.PartId,
        t.Quantity = s.Quantity,
        t.Price = s.Price,
        t.CreatedUserId = s.CreatedUserId,
        t.CreatedDate = s.CreatedDate,
        t.CreatedDateInText = s.CreatedDateInText,
        t.CreatedById = COALESCE(s.CreatedById, t.CreatedById),
        t.CreatedByName = COALESCE(s.CreatedByName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedOnMiladiDateTime, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(s.CreatedOnShamsiDateTime, t.CreatedOnShamsiDateTime),
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.InvoiceItem t
    INNER JOIN #ItemMapped s ON s.HtsId = t.HtsId;
    PRINT N'  InvoiceItem updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.InvoiceItem (
        HtsId, InvoiceId, PartId, Quantity, Price,
        CreatedUserId, CreatedDate, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.InvoiceId, s.PartId, s.Quantity, s.Price,
        s.CreatedUserId, s.CreatedDate, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName,
        ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
        1, @Seed, @Now, @NowShamsi, 1
    FROM #ItemMapped s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.InvoiceItem t WHERE t.HtsId = s.HtsId);
    PRINT N'  InvoiceItem inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Sync_CngInvoice_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT N'Invoice' AS T, COUNT(*) AS Cnt FROM Cng.Invoice
UNION ALL SELECT N'InvoiceItem', COUNT(*) FROM Cng.InvoiceItem
UNION ALL SELECT N'HTS_Invoice', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_Invoice]
UNION ALL SELECT N'HTS_InvoiceItems', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_InvoiceItems];
