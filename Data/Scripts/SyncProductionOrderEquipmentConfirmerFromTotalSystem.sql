-- ============================================
-- مهاجرت داده Pln_ProductionOrderEquipmentConfirmer
-- از TotalSystem (Linked Server: TMS) به HavayarApp (Pln.ProductionOrderEquipmentConfirmer)
--
-- زمینه: HTS این جدول را با LookupId (نوع دستگاه/Equipment, LookupType_FK=52) نگه‌داری
-- می‌کند. هر LookupId می‌تواند **چند ردیف هم‌زمان** داشته باشد (رابطه یک‌به‌چند، نه
-- یک‌به‌یک) - یعنی یک نوع دستگاه می‌تواند چند تاییدکننده مکانیک و/یا چند تاییدکننده برق
-- به‌طور هم‌زمان داشته باشد. نسخه قبلی این اسکریپت به‌اشتباه فقط آخرین ردیف هر LookupId را
-- مهاجرت می‌داد (فرض غلط یک‌به‌یک)؛ این نسخه **تمام** ردیف‌ها را بدون حذف/دیدوپ منتقل
-- می‌کند - دقیقاً هم‌تعداد با HTS.
--
-- نگاشت نوع دستگاه: Gnr_Lookup.Lookup_ID (LookupType_FK=52) == عدد Enum
-- Entities.App.Sale.Enums.ProductionOrderItemDeviceTypeEnum به‌صورت مستقیم و یک‌به‌یک
-- (تایید شد: 194=ScrewCompressor ... 2455=Unknown) - بدون نیاز به جدول نگاشت جداگانه.
--
-- نگاشت کاربر: Gnr_User.Username -> system.[User].Username
--
-- قبل از اجرا:
--   1) روی دیتابیس HavayarApp اجرا شود
--   2) Linked Server [TMS] به TotalSystem در دسترس باشد (نگاه کن به CreateLinkedServer_TMS.sql)
--   3) جدول Pln.ProductionOrderEquipmentConfirmer باید ساختار جدید را داشته باشد
--      (DeviceType/MechanicalUserId/ElectricalUserId) - نگاه کن به
--      FixProductionOrderEquipmentConfirmer.sql
--   4) کنترلر/UI دیگر محدودیت «یک ردیف به‌ازای هر DeviceType» را اعمال نمی‌کند
--      (ProductionOrderEquipmentConfirmerController) - چند ردیف برای یک نوع دستگاه مجاز است
--
-- این اسکریپت داده مقصد را کامل جایگزین می‌کند (DELETE + INSERT کامل، بدون دیدوپ) و
-- idempotent است (اجرای مجدد نتیجه یکسان می‌دهد).
-- ============================================

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'DeviceType') IS NULL
        THROW 51000, N'ستون DeviceType یافت نشد؛ ابتدا FixProductionOrderEquipmentConfirmer.sql را اجرا کنید.', 1;

    /* ─────────────────────────────────────────────
       0) User map (Old User_ID -> New User.Id)
       ───────────────────────────────────────────── */
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;

    SELECT
        CAST(ou.User_ID AS INT)  AS OldUserId,
        ou.Username              AS OldUsername,
        nu.Id                    AS NewUserId,
        nu.Name                  AS NewUserName
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON nu.Username = ou.Username;

    /* ─────────────────────────────────────────────
       1) تمام ردیف‌های منبع (بدون دیدوپ - هر LookupId می‌تواند چند ردیف داشته باشد)
       ───────────────────────────────────────────── */
    IF OBJECT_ID('tempdb..#SourceConfirmer') IS NOT NULL DROP TABLE #SourceConfirmer;

    SELECT
        c.Id          AS OldId,
        c.LookupId,
        c.MechanicalUserId,
        c.ElectricalUserId,
        c.Comment
    INTO #SourceConfirmer
    FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderEquipmentConfirmer] c;

    /* ─────────────────────────────────────────────
       2) جایگزینی کامل داده مقصد
       ───────────────────────────────────────────── */
    DELETE FROM Pln.ProductionOrderEquipmentConfirmer;

    INSERT INTO Pln.ProductionOrderEquipmentConfirmer
    (
        DeviceType,
        MechanicalUserId,
        ElectricalUserId,
        Comment,
        CreatedById,
        CreatedByName,
        CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime,
        ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        c.LookupId                                         AS DeviceType,
        umMech.NewUserId                                    AS MechanicalUserId,
        umElec.NewUserId                                    AS ElectricalUserId,
        c.Comment                                           AS Comment,
        1                                                   AS CreatedById,
        N'migration-hts'                                    AS CreatedByName,
        GETDATE()                                           AS CreatedOnMiladiDateTime,
        CONVERT(NVARCHAR(30), GETDATE(), 120)               AS CreatedOnShamsiDateTime,
        GETDATE()                                           AS ModifiedDateMiladiDateTime,
        CONVERT(NVARCHAR(30), GETDATE(), 120)               AS ModifiedDateShamsiDateTime,
        1                                                   AS IsActive
    FROM #SourceConfirmer c
    LEFT JOIN #UserMap umMech ON umMech.OldUserId = c.MechanicalUserId
    LEFT JOIN #UserMap umElec ON umElec.OldUserId = c.ElectricalUserId
    -- فقط LookupIdهایی که در حال حاضر هم به‌عنوان DeviceType در HavayarApp شناخته‌شده‌اند
    WHERE EXISTS (
        SELECT 1
        FROM (VALUES
            (194),(195),(2064),(2065),(2066),(2067),(2068),(2069),(2211),(2212),
            (2232),(2235),(2255),(2403),(2404),(2405),(2406),(2407),(2409),(2410),(2455)
        ) AS validDeviceTypes(v)
        WHERE validDeviceTypes.v = c.LookupId
    );

    DECLARE @InsertedCount INT = @@ROWCOUNT;
    DECLARE @SourceCount INT = (SELECT COUNT(*) FROM #SourceConfirmer);

    COMMIT TRANSACTION;

    PRINT N'مهاجرت Pln.ProductionOrderEquipmentConfirmer با موفقیت انجام شد. تعداد ردیف منبع (HTS): '
        + CAST(@SourceCount AS NVARCHAR(20)) + N' - تعداد ردیف مهاجرت‌شده: ' + CAST(@InsertedCount AS NVARCHAR(20));

    /* ─────────────────────────────────────────────
       گزارش‌ها
       ───────────────────────────────────────────── */

    -- ردیف‌های نهایی مهاجرت‌شده (ممکن است چند ردیف برای یک DeviceType باشد - این طبیعی است)
    SELECT
        r.Id,
        r.DeviceType,
        mu.Username AS MechanicalUsername,
        eu.Username AS ElectricalUsername,
        r.Comment
    FROM Pln.ProductionOrderEquipmentConfirmer r
    LEFT JOIN system.[User] mu ON mu.Id = r.MechanicalUserId
    LEFT JOIN system.[User] eu ON eu.Id = r.ElectricalUserId
    ORDER BY r.DeviceType, r.Id;

    -- تعداد تاییدکننده به‌ازای هر DeviceType (برای دیدن نوع دستگاه‌هایی که چند تاییدکننده دارند)
    SELECT
        r.DeviceType,
        COUNT(*) AS ConfirmerRowCount,
        COUNT(DISTINCT r.MechanicalUserId) AS DistinctMechanicalUsers,
        COUNT(DISTINCT r.ElectricalUserId) AS DistinctElectricalUsers
    FROM Pln.ProductionOrderEquipmentConfirmer r
    GROUP BY r.DeviceType
    ORDER BY ConfirmerRowCount DESC;

    -- LookupIdهایی که در HTS نگاشت داشتند ولی به‌عنوان DeviceType در HavayarApp شناخته‌نشدند
    SELECT DISTINCT c.LookupId AS UnmappedDeviceType, c.Comment
    FROM #SourceConfirmer c
    WHERE NOT EXISTS (
        SELECT 1
        FROM (VALUES
            (194),(195),(2064),(2065),(2066),(2067),(2068),(2069),(2211),(2212),
            (2232),(2235),(2255),(2403),(2404),(2405),(2406),(2407),(2409),(2410),(2455)
        ) AS validDeviceTypes(v)
        WHERE validDeviceTypes.v = c.LookupId
    );

    -- کاربرانی که در HTS به‌عنوان تاییدکننده ثبت شده‌اند ولی در HavayarApp (بر اساس Username) یافت نشدند
    SELECT DISTINCT
        ou.User_ID AS OldUserId,
        ou.Username AS OldUsername,
        ou.FullName
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    WHERE ou.User_ID IN (
        SELECT MechanicalUserId FROM #SourceConfirmer WHERE MechanicalUserId IS NOT NULL
        UNION
        SELECT ElectricalUserId FROM #SourceConfirmer WHERE ElectricalUserId IS NOT NULL
    )
    AND NOT EXISTS (SELECT 1 FROM #UserMap um WHERE um.OldUserId = ou.User_ID);

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
