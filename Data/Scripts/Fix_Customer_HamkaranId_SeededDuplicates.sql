/*
  Fix_Customer_HamkaranId_SeededDuplicates.sql

  علت (۱۴۰۵/۰۶/۲۹ — ۲۰۲۶-۰۹-۱۸ ۱۷:۱۳):
    Fix_CustomerAddress_SyncFromHts.sql برای مشتریان درج‌شده
    Customer.HamkaranId را برابر Party.HamkaranId گذاشت (شناسه طرف‌حساب راهکاران)،
    نه CustomerID راهکاران. نتیجه: ۸۰۸ کلید تکراری / ۱۶۱۶ ردیف.
    نمونه کلید ۶۷۷: Id ۶۴۵ آرام (راهکاران واقعی) در برابر Id ۱۱۲۸۵ یزدگلریز (seed).

  قانون: SLS.Customer.HamkaranId باید شناسه مشتری راهکاران باشد.
    جاب‌های Customer / Contract / ProductionOrder / RequirmentAdvertise
    با JobLookup.ToUniqueMapAsync روی همین کلید نگاشت می‌کنند؛ تکرار کلید جاب را می‌شکند.
    SaleOrderJob با GroupBy.First همین کلید را برمی‌دارد و ممکن است سفارش را
    به ردیف seed بچسباند.

  این اسکریپت فقط همان ۸۰۸ ردیف seed را عوض می‌کند
    (CreatedByName = N'sync-customeraddress-hts-fix' و HamkaranId تکراری).
    ردیف واقعی/قدیمی (CreatedByName غیر sync) دست نمی‌خورد. حذف نمی‌شود.

  فرمول جدید: HtsId + ۱۰۰۰۰۰۰۰۰۰
    بررسی زنده ۲۰۲۶-۰۹-۲۰:
      - هر ۸۰۸ گروه دقیقاً ۱ seed + ۱ واقعی است
      - هر ۸۰۸ تا HtsId دارند (۱ تا ۲۸۲۰۰)؛ شناسه جدید از ۱۰۰۰۰۰۰۰۰۱ تا ۱۰۰۰۰۲۸۲۰۰
      - هیچ Customer.HamkaranId الان >= ۱۰۰۰۰۰۰۰۰۰ نیست (Max فعلی ۳۰۰۰۱۴۴۸۱)
      - شناسه جدید با هیچ HamkaranId موجود یا بین خود seedها برخورد ندارد
      - شبیه‌سازی بعد از UPDATE: ۰ کلید تکراری

    چرا CustomerID راهکاران (ERPS SLS3.Customer روی PartyRef) استفاده نشد:
      - ۷۸۴ ردیف دقیقاً یک CustomerID برای Party.HamkaranId دارند، ولی همان
        شناسه الان HamkaranId خواهر/برادر روی همان Party است → برخورد جدید
      - ۲۴ ردیف اصلاً مشتری راهکاران برای آن Party ندارند
      - ۰ ردیف CustomerID آزاد و بدون برخورد دارد
      - اگر شناسه آزاد بود هم خطرناک بود: CustomerJob آن ردیف HTS را
        به‌جای مشتری واقعی راهکاران به‌روز می‌کرد

    چرا Code استفاده نشد: حدود ۸۱۲ کد seed الان HamkaranId مشتری دیگری است.

  جداول وابسته (فقط FK به Customer.Id — با عوض شدن HamkaranId جابه‌جا نمی‌شوند):
    CustomerAddress ۲۵ ردیف / ۲۴ مشتری seed
    OrderDetailSerial ۴ | ServiceRequest ۷ | EstimatedCost ۱
    بقیه صفر: Contract, Order, OrderDetail, ProductionOrder.SalesAgencyId,
    RequirmentAdvertise.SalesRepresentativeId, CustomerRequest,
    CustomerAllowedGuarantee, AgencyCartable, CollectionClaim,
    EquipmentMonitoring, TechnicalQuery, ResponsibleZoneCustomer,
    RepairRequest, CustomerSatisfactionSurvey, AgencyPartCardex
    مشتری واقعی همان کلیدها: Address ۹۵۳، Contract ۱۴۵، Order ۲۱۵۷۵، ServiceRequest ۲۷۲۵
    (عمداً دست نخورده)

  تریگر روی SLS.Customer نیست. ایندکس یکتا روی HamkaranId نیست
    (یکتا فقط PK و HtsId). جاب‌ها یکتایی منطقی می‌خواهند.

  Idempotent: اگر HamkaranId دیگر تکراری نباشد یا از قبل HtsId+۱۰۰۰۰۰۰۰۰۰ باشد،
    صفر ردیف به‌روز می‌شود.

  Encoding: UTF-8 with BOM
  Apply:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Fix_Customer_HamkaranId_SeededDuplicates.sql"
  پیش‌فرض @Commit = 0 یعنی UPDATE داخل تراکنش و بعد ROLLBACK (خشک).
  برای اعمال زنده فقط همان متغیر را ۱ کنید.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Commit bit = 0; -- ۱ = COMMIT اعمال زنده؛ ۰ = پیش‌نمایش + ROLLBACK
DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'fix-customer-hamkaranid-dup';
DECLARE @Reserved BIGINT = 1000000000;
DECLARE @Updated INT = 0;

IF OBJECT_ID(N'tempdb..#Target') IS NOT NULL DROP TABLE #Target;
CREATE TABLE #Target
(
    CustomerId      BIGINT NOT NULL PRIMARY KEY,
    OldHamkaranId   BIGINT NOT NULL,
    NewHamkaranId   BIGINT NOT NULL,
    HtsId           BIGINT NOT NULL,
    PartyId         BIGINT NOT NULL,
    Code            INT    NULL,
    RealCustomerId  BIGINT NOT NULL
);

INSERT INTO #Target
    (CustomerId, OldHamkaranId, NewHamkaranId, HtsId, PartyId, Code, RealCustomerId)
SELECT
    s.Id,
    s.HamkaranId,
    s.HtsId + @Reserved,
    s.HtsId,
    s.PartyId,
    s.Code,
    r.Id
FROM SLS.Customer s
INNER JOIN SLS.Customer r
    ON r.HamkaranId = s.HamkaranId
   AND r.Id <> s.Id
   AND ISNULL(r.CreatedByName, N'') <> N'sync-customeraddress-hts-fix'
WHERE s.CreatedByName = N'sync-customeraddress-hts-fix'
  AND s.HamkaranId IS NOT NULL
  AND ISNULL(s.HtsId, 0) <> 0
  AND s.HamkaranId <> (s.HtsId + @Reserved);

DECLARE @TargetCnt INT = (SELECT COUNT(*) FROM #Target);
PRINT N'=== هدف ===';
PRINT N'  ردیف seed برای اصلاح: ' + CAST(@TargetCnt AS nvarchar(20));

PRINT N'=== پیش‌نمایش نمونه کلید ۶۷۷ ===';
SELECT
    t.CustomerId AS SeededId,
    t.Code AS SeededCode,
    t.OldHamkaranId,
    t.NewHamkaranId,
    t.HtsId,
    t.RealCustomerId,
    ps.FullName AS SeededName,
    pr.FullName AS RealName
FROM #Target t
INNER JOIN SLS.Customer s ON s.Id = t.CustomerId
INNER JOIN SLS.Customer r ON r.Id = t.RealCustomerId
LEFT JOIN Gnr.Party ps ON ps.Id = s.PartyId
LEFT JOIN Gnr.Party pr ON pr.Id = r.PartyId
WHERE t.OldHamkaranId = 677;

PRINT N'=== شمارش وابسته روی همان Idهای seed (باید بعد از UPDATE ثابت بماند) ===';
SELECT
    (SELECT COUNT(*) FROM #Target) AS TargetCnt,
    (SELECT COUNT(*) FROM SLS.CustomerAddress a INNER JOIN #Target t ON t.CustomerId = a.CustomerId) AS AddrOnSeeded,
    (SELECT COUNT(*) FROM SLS.Contract c INNER JOIN #Target t ON t.CustomerId = c.CustomerId) AS ContractOnSeeded,
    (SELECT COUNT(*) FROM Sale.[Order] o INNER JOIN #Target t ON t.CustomerId = o.CustomerId) AS OrderOnSeeded,
    (SELECT COUNT(*) FROM Sale.OrderDetail od INNER JOIN #Target t ON t.CustomerId = od.CustomerId) AS OrderDetailOnSeeded,
    (SELECT COUNT(*) FROM Sale.OrderDetailSerial s INNER JOIN #Target t ON t.CustomerId = s.CustomerId) AS SerialOnSeeded,
    (SELECT COUNT(*) FROM Sale.ProductionOrder po INNER JOIN #Target t ON t.CustomerId = po.SalesAgencyId) AS ProdAgencyOnSeeded,
    (SELECT COUNT(*) FROM Sale.RequirmentAdvertise ra INNER JOIN #Target t ON t.CustomerId = ra.SalesRepresentativeId) AS ReqAdvOnSeeded,
    (SELECT COUNT(*) FROM Sale.ServiceRequest sr INNER JOIN #Target t ON t.CustomerId = sr.CustomerId) AS SvcReqOnSeeded,
    (SELECT COUNT(*) FROM Sale.CustomerRequest cr INNER JOIN #Target t ON t.CustomerId = cr.CustomerId) AS CustReqOnSeeded,
    (SELECT COUNT(*) FROM Sale.CustomerAllowedGuarantee g INNER JOIN #Target t ON t.CustomerId = g.CustomerId) AS GuaranteeOnSeeded,
    (SELECT COUNT(*) FROM Rpr.RepairRequest rr INNER JOIN #Target t ON t.CustomerId = rr.CustomerId) AS RepairOnSeeded,
    (SELECT COUNT(*) FROM Rpr.EstimatedCost ec INNER JOIN #Target t ON t.CustomerId = ec.CustomerId) AS EstCostOnSeeded,
    (SELECT COUNT(*) FROM Crm.CustomerSatisfactionSurvey css INNER JOIN #Target t ON t.CustomerId = css.CustomerId) AS SurveyOnSeeded;

PRINT N'=== کنترل ایمنی شناسه جدید ===';
SELECT
    (SELECT COUNT(*) FROM #Target WHERE HtsId IS NULL OR HtsId = 0) AS MissingHtsId,
    (SELECT COUNT(*) FROM #Target t INNER JOIN SLS.Customer x ON x.Id <> t.CustomerId AND x.HamkaranId = t.NewHamkaranId) AS NewCollidesExisting,
    (SELECT COUNT(*) FROM #Target t INNER JOIN #Target t2 ON t2.CustomerId <> t.CustomerId AND t2.NewHamkaranId = t.NewHamkaranId) AS NewCollidesAmongTarget,
    (SELECT MIN(NewHamkaranId) FROM #Target) AS MinNew,
    (SELECT MAX(NewHamkaranId) FROM #Target) AS MaxNew;

IF EXISTS (SELECT 1 FROM #Target WHERE HtsId IS NULL OR HtsId = 0)
BEGIN
    RAISERROR(N'HtsId خالی روی ردیف هدف؛ اصلاح انجام نشد.', 16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT 1
    FROM #Target t
    INNER JOIN SLS.Customer x ON x.Id <> t.CustomerId AND x.HamkaranId = t.NewHamkaranId
)
BEGIN
    RAISERROR(N'شناسه جدید با HamkaranId موجود برخورد دارد؛ اصلاح انجام نشد.', 16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT 1
    FROM #Target t
    INNER JOIN #Target t2 ON t2.CustomerId <> t.CustomerId AND t2.NewHamkaranId = t.NewHamkaranId
)
BEGIN
    RAISERROR(N'شناسه جدید داخل خود هدف تکراری است؛ اصلاح انجام نشد.', 16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT 1
    FROM SLS.Customer c
    INNER JOIN #Target t ON t.CustomerId = c.Id
    WHERE ISNULL(c.CreatedByName, N'') <> N'sync-customeraddress-hts-fix'
)
BEGIN
    RAISERROR(N'هدف شامل ردیف غیر seed است؛ اصلاح انجام نشد.', 16, 1);
    RETURN;
END;

PRINT N'=== مشتری‌هایی که نمی‌توان ایمن عوض کرد (باید خالی باشد) ===';
SELECT t.CustomerId, t.OldHamkaranId, t.NewHamkaranId, N'برخورد شناسه جدید' AS Reason
FROM #Target t
INNER JOIN SLS.Customer x ON x.Id <> t.CustomerId AND x.HamkaranId = t.NewHamkaranId;

BEGIN TRANSACTION;
BEGIN TRY
    PRINT N'=== UPDATE فقط ردیف‌های seed ===';
    UPDATE c
    SET
        c.HamkaranId = t.NewHamkaranId,
        c.ModifiedById = 1,
        c.ModifiedByName = @SeedUser,
        c.ModifiedDateMiladiDateTime = @Now,
        c.ModifiedDateShamsiDateTime = @NowShamsi
    FROM SLS.Customer c
    INNER JOIN #Target t ON t.CustomerId = c.Id
    WHERE c.CreatedByName = N'sync-customeraddress-hts-fix'
      AND c.HamkaranId = t.OldHamkaranId
      AND c.HamkaranId <> t.NewHamkaranId;

    SET @Updated = @@ROWCOUNT;
    PRINT N'  ردیف به‌روزشده: ' + CAST(@Updated AS nvarchar(20));

    IF EXISTS (
        SELECT 1
        FROM SLS.Customer
        WHERE HamkaranId IS NOT NULL
        GROUP BY HamkaranId
        HAVING COUNT(*) > 1
    )
    BEGIN
        RAISERROR(N'بعد از UPDATE هنوز HamkaranId تکراری مانده؛ ROLLBACK.', 16, 1);
    END;

    PRINT N'=== AFTER (داخل تراکنش) ===';
    SELECT
        @Updated AS UpdatedRows,
        (SELECT COUNT(*) FROM SLS.Customer WHERE CreatedByName = N'sync-customeraddress-hts-fix' AND HamkaranId >= @Reserved) AS SeededInReservedRange,
        (SELECT COUNT(*) FROM (
            SELECT HamkaranId FROM SLS.Customer WHERE HamkaranId IS NOT NULL
            GROUP BY HamkaranId HAVING COUNT(*) > 1
        ) d) AS RemainingDupKeys,
        (SELECT COUNT(*) FROM SLS.CustomerAddress a INNER JOIN #Target t ON t.CustomerId = a.CustomerId) AS AddrOnSeededStill,
        (SELECT COUNT(*) FROM Sale.ServiceRequest sr INNER JOIN #Target t ON t.CustomerId = sr.CustomerId) AS SvcReqOnSeededStill,
        (SELECT COUNT(*) FROM Sale.OrderDetailSerial s INNER JOIN #Target t ON t.CustomerId = s.CustomerId) AS SerialOnSeededStill,
        (SELECT COUNT(*) FROM Rpr.EstimatedCost ec INNER JOIN #Target t ON t.CustomerId = ec.CustomerId) AS EstCostOnSeededStill;

    -- ردیف واقعی همان کلیدهای قدیمی باید همان HamkaranId قبلی را نگه دارد
    SELECT COUNT(*) AS RealRowsStillOldKey
    FROM SLS.Customer r
    INNER JOIN #Target t ON t.RealCustomerId = r.Id AND r.HamkaranId = t.OldHamkaranId;

    IF @Commit = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'=== COMMIT شد ===';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'=== ROLLBACK شد (Dry-run؛ برای اعمال @Commit را ۱ کنید) ===';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
    RETURN;
END CATCH;

PRINT N'=== DONE Fix_Customer_HamkaranId_SeededDuplicates ===';
