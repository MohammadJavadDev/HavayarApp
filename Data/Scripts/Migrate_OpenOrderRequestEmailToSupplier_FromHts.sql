/*
================================================================================
Migrate_OpenOrderRequestEmailToSupplier_FromHts.sql  (WP1 / D1 / Q1-a)
================================================================================
کپی یک‌باره ~37,952 ردیف HTS TotalSystem.dbo.Sup_OpenOrderRequest_EmailToSupplier
به Sup.OpenOrderRequestEmailToSupplier. Upsert با HtsId.

منبع: Linked Server [TMS]
  OpenOrderRequest_EmailToSupplier_ID, OpenOrderRequest_FK, Company_FK,
  CreatedUser_FK, SendDate (شمسی ۱۰)، SendTime (۵)، Comment, SendingCount
  (HTS موضوع/بدنه ندارد)

نگاشت:
  درخواست: OpenOrderRequest_FK → HTS Sup_OpenOrderRequest
           سپس همان سه اولویت SyncOpenOrderRequestFromTotalSystem
           (OrderRowId+PurchaseRequestItemId / PR+تاریخ / فقط PR)
           OpenOrderRequest.HtsId وجود ندارد.
  پیمانکار: Company_FK → Gnr_ManCompany.Hamkaran_ManCompany_FK → Party.HamkaranId → Supplier
  کاربر:    Gnr_User.Username → system.User.Username
            fallback LOWER(ActiveDirectoryUsername)=LOWER(Username)
            (system.User.HtsId وجود ندارد)

تاریخ: SendShamsiDateTime = SendDate + ' ' + SendTime
       SendMiladiDateTime = dbo.fn_shmasiToMiladi در صورت وجود، وگرنه NULL

اجرا:
  1) @DryRun = 1 (پیش‌فرض): شمارش + پیش‌نمایش + بی‌تطبیق‌ها — بدون نوشتن
  2) @DryRun = 0: یک تراکنش واقعی

پیش‌نیاز: TMS، جدول هدف، و درخواست‌های باز قبلاً همگام شده باشند.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @DryRun BIT = 1;
DECLARE @SeedUser NVARCHAR(150) = N'migrate-emailtosupplier-hts';
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @HasShamsiFn BIT = CASE WHEN OBJECT_ID(N'dbo.fn_shmasiToMiladi', N'FN') IS NOT NULL
                                  OR OBJECT_ID(N'dbo.fn_shmasiToMiladi', N'FS') IS NOT NULL
                                  OR OBJECT_ID(N'dbo.fn_shmasiToMiladi') IS NOT NULL
                             THEN 1 ELSE 0 END;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] وجود ندارد. ابتدا Data/Scripts/CreateLinkedServer_TMS.sql را اجرا کنید.', 16, 1);
    RETURN;
END

IF OBJECT_ID(N'Sup.OpenOrderRequestEmailToSupplier', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Sup.OpenOrderRequestEmailToSupplier وجود ندارد.', 16, 1);
    RETURN;
END

BEGIN TRY
    IF OBJECT_ID('tempdb..#Ets') IS NOT NULL DROP TABLE #Ets;
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    IF OBJECT_ID('tempdb..#SupplierMap') IS NOT NULL DROP TABLE #SupplierMap;
    IF OBJECT_ID('tempdb..#IdMap') IS NOT NULL DROP TABLE #IdMap;
    IF OBJECT_ID('tempdb..#ReqSrc') IS NOT NULL DROP TABLE #ReqSrc;
    IF OBJECT_ID('tempdb..#Final') IS NOT NULL DROP TABLE #Final;

    SELECT
        CAST(e.OpenOrderRequest_EmailToSupplier_ID AS BIGINT) AS HtsId,
        CAST(e.OpenOrderRequest_FK AS BIGINT) AS OldRequestId,
        CAST(e.Company_FK AS INT) AS OldCompanyId,
        CAST(e.CreatedUser_FK AS INT) AS OldUserId,
        CAST(e.SendDate AS NVARCHAR(10)) AS SendDate,
        CAST(e.SendTime AS NVARCHAR(5)) AS SendTime,
        CAST(e.Comment AS NVARCHAR(2048)) AS Comment,
        CAST(e.SendingCount AS DECIMAL(18, 2)) AS SendingCount
    INTO #Ets
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_EmailToSupplier] e;

    CREATE UNIQUE CLUSTERED INDEX IX_Ets ON #Ets(HtsId);
    DECLARE @EtsRows INT = (SELECT COUNT(*) FROM #Ets);
    PRINT N'HTS EmailToSupplier rows: ' + CAST(@EtsRows AS NVARCHAR(20));

    /* کاربر */
    SELECT
        CAST(ou.User_ID AS INT) AS OldUserId,
        nu.Id AS NewUserId,
        nu.Name AS NewUserName
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu ON nu.Username = ou.Username;

    INSERT INTO #UserMap (OldUserId, NewUserId, NewUserName)
    SELECT CAST(ou.User_ID AS INT), nu.Id, nu.Name
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON ou.ActiveDirectoryUsername IS NOT NULL
       AND LOWER(nu.Username) = LOWER(ou.ActiveDirectoryUsername)
    WHERE NOT EXISTS (SELECT 1 FROM #UserMap m WHERE m.OldUserId = CAST(ou.User_ID AS INT));

    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap(OldUserId);

    /* پیمانکار */
    SELECT
        mc.ManCompany_ID AS OldManCompanyId,
        s.Id AS NewSupplierId
    INTO #SupplierMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
    INNER JOIN Gnr.Party py ON py.HamkaranId = mc.Hamkaran_ManCompany_FK
    INNER JOIN Gnr.Supplier s ON s.PartyId = py.Id
    WHERE mc.Hamkaran_ManCompany_FK > 0;

    CREATE UNIQUE CLUSTERED INDEX IX_SupplierMap ON #SupplierMap(OldManCompanyId);

    /* درخواست‌های HTS مورد نیاز */
    SELECT
        o.OpenOrderRequest_ID AS OldId,
        o.OrderRowId,
        o.PurchaseRequestItemId,
        o.OrderDateInEurope AS PurchaseRequestMiladiDate
    INTO #ReqSrc
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest] o
    WHERE EXISTS (SELECT 1 FROM #Ets e WHERE e.OldRequestId = o.OpenOrderRequest_ID);

    CREATE UNIQUE CLUSTERED INDEX IX_ReqSrc ON #ReqSrc(OldId);

    CREATE TABLE #IdMap
    (
        OldId BIGINT NOT NULL PRIMARY KEY,
        NewId BIGINT NOT NULL,
        MatchBy NVARCHAR(40) NOT NULL
    );

    INSERT INTO #IdMap (OldId, NewId, MatchBy)
    SELECT s.OldId, n.Id, N'1:OrderItemID'
    FROM #ReqSrc s
    INNER JOIN Sup.OpenOrderRequest n
        ON s.OrderRowId IS NOT NULL
       AND n.OrderRowId = s.OrderRowId
       AND n.PurchaseRequestItemId = s.PurchaseRequestItemId;

    INSERT INTO #IdMap (OldId, NewId, MatchBy)
    SELECT s.OldId, n.Id, N'2:PRItem+NullOrder+Date'
    FROM #ReqSrc s
    INNER JOIN Sup.OpenOrderRequest n
        ON s.OrderRowId IS NOT NULL
       AND n.OrderRowId IS NULL
       AND n.PurchaseRequestItemId = s.PurchaseRequestItemId
       AND ISNULL(n.PurchaseRequestMiladiDate, '19000101') = ISNULL(s.PurchaseRequestMiladiDate, '19000101')
    WHERE NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.OldId = s.OldId)
      AND NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.NewId = n.Id);

    INSERT INTO #IdMap (OldId, NewId, MatchBy)
    SELECT s.OldId, n.Id, N'3:PurchaseRequestItemID'
    FROM #ReqSrc s
    INNER JOIN Sup.OpenOrderRequest n
        ON s.OrderRowId IS NULL
       AND n.OrderRowId IS NULL
       AND n.PurchaseRequestItemId = s.PurchaseRequestItemId
    WHERE NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.OldId = s.OldId)
      AND NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.NewId = n.Id);

    DECLARE @UserMapRows INT = (SELECT COUNT(*) FROM #UserMap);
    DECLARE @SupplierMapRows INT = (SELECT COUNT(*) FROM #SupplierMap);
    DECLARE @RequestMapRows INT = (SELECT COUNT(*) FROM #IdMap);
    DECLARE @ReqSrcRows INT = (SELECT COUNT(*) FROM #ReqSrc);
    PRINT CONCAT(N'UserMap=', @UserMapRows,
                 N' | SupplierMap=', @SupplierMapRows,
                 N' | RequestMap=', @RequestMapRows,
                 N' / HTS requests used=', @ReqSrcRows);

    SELECT
        e.HtsId,
        m.NewId AS OpenOrderRequestId,
        sm.NewSupplierId AS SupplierId,
        um.NewUserId AS SenderUserId,
        um.NewUserName AS SenderUserName,
        LTRIM(RTRIM(ISNULL(e.SendDate, N'') + CASE WHEN e.SendTime IS NULL OR e.SendTime = N'' THEN N'' ELSE N' ' + e.SendTime END)) AS SendShamsiDateTime,
        CAST(NULL AS DATETIME2) AS SendMiladiDateTime,
        e.Comment,
        e.SendingCount,
        CASE WHEN m.NewId IS NULL THEN N'UnmatchedRequest'
             WHEN sm.NewSupplierId IS NULL THEN N'UnmatchedSupplier'
             ELSE N'OK' END AS MapStatus
    INTO #Final
    FROM #Ets e
    LEFT JOIN #IdMap m ON m.OldId = e.OldRequestId
    LEFT JOIN #SupplierMap sm ON sm.OldManCompanyId = e.OldCompanyId
    LEFT JOIN #UserMap um ON um.OldUserId = e.OldUserId;

    IF @HasShamsiFn = 1
    BEGIN
        UPDATE #Final
        SET SendMiladiDateTime = TRY_CONVERT(DATETIME2, dbo.fn_shmasiToMiladi(LEFT(SendShamsiDateTime, 10)))
        WHERE SendShamsiDateTime IS NOT NULL AND LEN(SendShamsiDateTime) >= 10;
    END

    DECLARE @Ok INT = (SELECT COUNT(*) FROM #Final WHERE MapStatus = N'OK');
    DECLARE @BadReq INT = (SELECT COUNT(*) FROM #Final WHERE MapStatus = N'UnmatchedRequest');
    DECLARE @BadSup INT = (SELECT COUNT(*) FROM #Final WHERE MapStatus = N'UnmatchedSupplier');
    DECLARE @WouldInsert INT = (
        SELECT COUNT(*) FROM #Final f
        WHERE f.MapStatus = N'OK'
          AND NOT EXISTS (SELECT 1 FROM Sup.OpenOrderRequestEmailToSupplier t WHERE t.HtsId = f.HtsId));
    DECLARE @WouldUpdate INT = (
        SELECT COUNT(*) FROM #Final f
        INNER JOIN Sup.OpenOrderRequestEmailToSupplier t ON t.HtsId = f.HtsId
        WHERE f.MapStatus = N'OK');

    PRINT CONCAT(N'Mappable=', @Ok, N' | UnmatchedRequest=', @BadReq,
                 N' | UnmatchedSupplier=', @BadSup,
                 N' | WouldInsert=', @WouldInsert, N' | WouldUpdate=', @WouldUpdate,
                 N' | ShamsiFn=', @HasShamsiFn);

    SELECT TOP (50) f.HtsId, f.MapStatus, e.OldRequestId, e.OldCompanyId, e.OldUserId
    FROM #Final f
    INNER JOIN #Ets e ON e.HtsId = f.HtsId
    WHERE f.MapStatus <> N'OK'
    ORDER BY f.HtsId;

    SELECT TOP (20) f.HtsId, f.OpenOrderRequestId, f.SupplierId, f.SenderUserId, f.SendShamsiDateTime, f.SendingCount
    FROM #Final f
    WHERE f.MapStatus = N'OK'
    ORDER BY f.HtsId DESC;

    IF @DryRun = 1
    BEGIN
        PRINT N'DRY RUN — هیچ ردیفی نوشته نشد. برای اجرا @DryRun=0 بگذارید.';
        RETURN;
    END

    BEGIN TRANSACTION;

    UPDATE t
    SET t.OpenOrderRequestId = f.OpenOrderRequestId,
        t.SupplierId = f.SupplierId,
        t.SenderUserId = f.SenderUserId,
        t.SendShamsiDateTime = NULLIF(f.SendShamsiDateTime, N''),
        t.SendMiladiDateTime = f.SendMiladiDateTime,
        t.Comment = f.Comment,
        t.SendingCount = f.SendingCount,
        t.ModifiedById = 1,
        t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sup.OpenOrderRequestEmailToSupplier t
    INNER JOIN #Final f ON f.HtsId = t.HtsId
    WHERE f.MapStatus = N'OK';
    PRINT N'Updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Sup.OpenOrderRequestEmailToSupplier
        (OpenOrderRequestId, SupplierId, SendMiladiDateTime, SendShamsiDateTime, SenderUserId,
         ToEmail, Subject, Body, Comment, SendingCount, HtsId,
         CreatedById, CreatedByName, ModifiedById, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        f.OpenOrderRequestId, f.SupplierId, f.SendMiladiDateTime, NULLIF(f.SendShamsiDateTime, N''), f.SenderUserId,
        NULL, NULL, NULL, f.Comment, f.SendingCount, f.HtsId,
        ISNULL(f.SenderUserId, 1), ISNULL(f.SenderUserName, @SeedUser), 1, @SeedUser,
        ISNULL(f.SendMiladiDateTime, @Now), ISNULL(NULLIF(f.SendShamsiDateTime, N''), @NowShamsi), @Now, @NowShamsi, 1
    FROM #Final f
    WHERE f.MapStatus = N'OK'
      AND NOT EXISTS (SELECT 1 FROM Sup.OpenOrderRequestEmailToSupplier t WHERE t.HtsId = f.HtsId);
    PRINT N'Inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Migrate_OpenOrderRequestEmailToSupplier_FromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT COUNT(*) AS LocalRows, COUNT(HtsId) AS WithHtsId
FROM Sup.OpenOrderRequestEmailToSupplier;
