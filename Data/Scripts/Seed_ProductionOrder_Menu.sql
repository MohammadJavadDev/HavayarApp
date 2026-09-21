/*
================================================================================
Seed_ProductionOrder_Menu.sql
================================================================================
منوی سفارش ساخت و اقلام سفارش ساخت مطابق مسیرهای زنده HTS:
  HTS: سیستم فروش (MainSale) → عملیات → «سفارشات ساخت»          (صفحه 463)
  HTS: سیستم برنامه‌ریزی → عملیات/گزارشات → «اقلام سفارش ساخت»    (صفحه 214)

نگاشت به منوهای پنل (system.SystemMenu):
  - Sell        (فروش)            → برگ «سفارش ساخت»  /panel/productionorder/list  (اگر نبود اضافه می‌شود)
                                  → برگ «اقلام سفارش ساخت» /panel/productionorderitem/list (اگر نبود اضافه می‌شود)
  - Industries  (صنایع/برنامه‌ریزی) → گروه «برنامه ریزی» باید هر دو برگ را داشته باشد (اگر نبود اضافه می‌شود)

Idempotent روی Path (lowercase). ساختار فعلی منوها دست نمی‌خورد؛ فقط برگ‌های غایب افزوده می‌شوند.
Restart لازم نیست (منو از جدول خوانده می‌شود).
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-productionorder-menu';

DECLARE @PoPath  NVARCHAR(200) = N'/panel/productionorder/list';
DECLARE @PoiPath NVARCHAR(200) = N'/panel/productionorderitem/list';

DECLARE @PoLeaf NVARCHAR(MAX) = N'{"text":"سفارش ساخت","icon":"ki-cube-3 ki-outline","iconColor":"#1fe57f","path":"/panel/productionorder/list","a_attr":{"href":"/panel/productionorder/list"},"data":{"iconColor":"#1fe57f","path":"/panel/productionorder/list"},"children":[]}';
DECLARE @PoiLeaf NVARCHAR(MAX) = N'{"text":"اقلام سفارش ساخت","icon":"ki-colors-square ki-outline","iconColor":"#d78819","path":"/panel/productionorderitem/list","a_attr":{"href":"/panel/productionorderitem/list"},"data":{"iconColor":"#d78819","path":"/panel/productionorderitem/list"},"children":[]}';

BEGIN TRY
    BEGIN TRANSACTION;

    -----------------------------------------------------------------------------
    -- 1) منوی فروش (Sell): برگ‌های سفارش ساخت / اقلام سفارش ساخت در ریشه منو
    -----------------------------------------------------------------------------
    DECLARE @SellContent NVARCHAR(MAX);
    SELECT @SellContent = Content FROM system.SystemMenu WHERE Name = N'Sell';

    IF @SellContent IS NULL
        PRINT N'WARN: منوی Sell (فروش) یافت نشد؛ برگ سفارش ساخت به آن اضافه نشد';
    ELSE
    BEGIN
        IF ISJSON(@SellContent) = 0
            THROW 51100, N'Content منوی Sell JSON معتبر نیست', 1;

        IF CHARINDEX(N'"path":"' + @PoPath + N'"', @SellContent) = 0
        BEGIN
            SET @SellContent = JSON_MODIFY(@SellContent, N'append $', JSON_QUERY(@PoLeaf));
            PRINT N'  Sell: برگ «سفارش ساخت» اضافه شد';
        END
        ELSE
            PRINT N'  Sell: برگ «سفارش ساخت» از قبل موجود است';

        IF CHARINDEX(N'"path":"' + @PoiPath + N'"', @SellContent) = 0
        BEGIN
            SET @SellContent = JSON_MODIFY(@SellContent, N'append $', JSON_QUERY(@PoiLeaf));
            PRINT N'  Sell: برگ «اقلام سفارش ساخت» اضافه شد';
        END
        ELSE
            PRINT N'  Sell: برگ «اقلام سفارش ساخت» از قبل موجود است';

        UPDATE system.SystemMenu
        SET Content = @SellContent,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'Sell';
    END

    -----------------------------------------------------------------------------
    -- 2) منوی صنایع (Industries): گروه «برنامه ریزی» → هر دو برگ
    -----------------------------------------------------------------------------
    DECLARE @IndContent NVARCHAR(MAX);
    SELECT @IndContent = Content FROM system.SystemMenu WHERE Name = N'Industries';

    IF @IndContent IS NULL
        PRINT N'WARN: منوی Industries (صنایع) یافت نشد؛ برگ‌های برنامه‌ریزی اضافه نشدند';
    ELSE
    BEGIN
        IF ISJSON(@IndContent) = 0
            THROW 51101, N'Content منوی Industries JSON معتبر نیست', 1;

        -- ایندکس گروه «برنامه ریزی» در آرایه ریشه
        DECLARE @PlanningIdx INT;
        SELECT TOP 1 @PlanningIdx = CAST([key] AS INT)
        FROM OPENJSON(@IndContent)
        WHERE JSON_VALUE([value], '$.text') IN (N'برنامه ریزی', N'برنامه‌ریزی')
        ORDER BY CAST([key] AS INT);

        IF @PlanningIdx IS NULL
        BEGIN
            -- گروه وجود ندارد: گروه جدید با هر دو برگ به انتهای منو
            DECLARE @PlanningGroup NVARCHAR(MAX) = N'{"text":"برنامه ریزی","icon":"ki-colors-square ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[' + @PoLeaf + N',' + @PoiLeaf + N']}';
            SET @IndContent = JSON_MODIFY(@IndContent, N'append $', JSON_QUERY(@PlanningGroup));
            PRINT N'  Industries: گروه «برنامه ریزی» با دو برگ ایجاد شد';
        END
        ELSE
        BEGIN
            DECLARE @ChildrenPath NVARCHAR(50) = N'$[' + CAST(@PlanningIdx AS NVARCHAR(10)) + N'].children';
            DECLARE @Children NVARCHAR(MAX) = JSON_QUERY(@IndContent, @ChildrenPath);
            IF @Children IS NULL
                SET @IndContent = JSON_MODIFY(@IndContent, @ChildrenPath, JSON_QUERY(N'[]'));

            IF CHARINDEX(N'"path":"' + @PoPath + N'"', ISNULL(@Children, N'')) = 0
            BEGIN
                SET @IndContent = JSON_MODIFY(@IndContent, N'append ' + @ChildrenPath, JSON_QUERY(@PoLeaf));
                PRINT N'  Industries/برنامه ریزی: برگ «سفارش ساخت» اضافه شد';
            END
            ELSE
                PRINT N'  Industries/برنامه ریزی: برگ «سفارش ساخت» از قبل موجود است';

            IF CHARINDEX(N'"path":"' + @PoiPath + N'"', ISNULL(@Children, N'')) = 0
            BEGIN
                SET @IndContent = JSON_MODIFY(@IndContent, N'append ' + @ChildrenPath, JSON_QUERY(@PoiLeaf));
                PRINT N'  Industries/برنامه ریزی: برگ «اقلام سفارش ساخت» اضافه شد';
            END
            ELSE
                PRINT N'  Industries/برنامه ریزی: برگ «اقلام سفارش ساخت» از قبل موجود است';
        END

        UPDATE system.SystemMenu
        SET Content = @IndContent,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'Industries';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_ProductionOrder_Menu ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-- تایید نتیجه
SELECT Name, Title,
       CASE WHEN CHARINDEX(N'/panel/productionorder/list', Content) > 0 THEN 1 ELSE 0 END AS HasProductionOrder,
       CASE WHEN CHARINDEX(N'/panel/productionorderitem/list', Content) > 0 THEN 1 ELSE 0 END AS HasProductionOrderItem
FROM system.SystemMenu
WHERE Name IN (N'Sell', N'Industries', N'Production');
