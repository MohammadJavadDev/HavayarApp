/*
================================================================================
Seed_BuyCategory_Menu.sql  (WP5 / D10)
================================================================================
منوی «تامین و خرید» (system.SystemMenu Name='Supply', Id=3) مطابق نام‌گذاری HTS:
  HTS صفحه 70 «دسته های خرید»        → برگ جدید  /panel/sup/buycategory/list      (BuyCategoryController — والد)
  HTS صفحه 72 «اقلام دسته های خرید»  → برگ موجود /panel/sup/buycategoryitem/list  (تا امروز با عنوان اشتباه «دسته های خرید»)

عملیات (idempotent):
  1) برگ موجود با path=/panel/sup/buycategoryitem/list → text = «اقلام دسته های خرید»
  2) اگر برگ /panel/sup/buycategory/list وجود ندارد → درست قبل از برگ اقلام درج می‌شود (در ریشه منو)
     اگر برگ اقلام درون یک گروه (children) باشد، برگ جدید به همان children اضافه می‌شود.
  3) AccessRoleIds منو دست نمی‌خورد (200052 ShowAllMenus، 400008 SupplierMenu). دسترسی مسیر از Seed_BuyCategory_DataProfiles.sql می‌آید.

Restart لازم نیست (منو از جدول خوانده می‌شود).
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-buycategory-menu';

DECLARE @ParentPath NVARCHAR(200) = N'/panel/sup/buycategory/list';
DECLARE @ItemsPath  NVARCHAR(200) = N'/panel/sup/buycategoryitem/list';
DECLARE @ParentText NVARCHAR(200) = N'دسته های خرید';
DECLARE @ItemsText  NVARCHAR(200) = N'اقلام دسته های خرید';

DECLARE @ParentLeaf NVARCHAR(MAX) = N'{"text":"دسته های خرید","icon":"ki-category ki-outline","iconColor":"#ea603e","path":"/panel/sup/buycategory/list","a_attr":{"href":"/panel/sup/buycategory/list"},"data":{"iconColor":"#ea603e","path":"/panel/sup/buycategory/list"},"children":[]}';

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content
    FROM system.SystemMenu
    WHERE Name = N'Supply' OR Title = N'تامین و خرید'
    ORDER BY CASE WHEN Name = N'Supply' THEN 0 ELSE 1 END, Id;

    IF @MenuId IS NULL
    BEGIN
        PRINT N'WARN: منوی Supply (تامین و خرید) یافت نشد؛ کاری انجام نشد';
    END
    ELSE
    BEGIN
        IF @Content IS NULL OR LTRIM(RTRIM(@Content)) = N'' SET @Content = N'[]';
        IF ISJSON(@Content) = 0
            THROW 51200, N'Content منوی Supply JSON معتبر نیست', 1;

        -----------------------------------------------------------------------
        -- محل برگ اقلام: ریشه یا children سطح اول
        -----------------------------------------------------------------------
        DECLARE @ItemsRootIdx INT = NULL;       -- ایندکس برگ اقلام در ریشه
        DECLARE @ItemsGroupIdx INT = NULL;      -- ایندکس گروهی که برگ اقلام درون children آن است
        DECLARE @ItemsChildIdx INT = NULL;      -- ایندکس برگ اقلام درون children آن گروه

        SELECT TOP 1 @ItemsRootIdx = CAST([key] AS INT)
        FROM OPENJSON(@Content)
        WHERE JSON_VALUE([value], '$.path') = @ItemsPath
        ORDER BY CAST([key] AS INT);

        IF @ItemsRootIdx IS NULL
        BEGIN
            SELECT TOP 1 @ItemsGroupIdx = CAST(g.[key] AS INT), @ItemsChildIdx = CAST(c.[key] AS INT)
            FROM OPENJSON(@Content) g
            CROSS APPLY OPENJSON(g.[value], '$.children') c
            WHERE JSON_VALUE(c.[value], '$.path') = @ItemsPath
            ORDER BY CAST(g.[key] AS INT), CAST(c.[key] AS INT);
        END

        -----------------------------------------------------------------------
        -- 1) rename برگ اقلام → «اقلام دسته های خرید»
        -----------------------------------------------------------------------
        DECLARE @ItemsTextPath NVARCHAR(100) = NULL;
        IF @ItemsRootIdx IS NOT NULL
            SET @ItemsTextPath = N'$[' + CAST(@ItemsRootIdx AS NVARCHAR(10)) + N'].text';
        ELSE IF @ItemsGroupIdx IS NOT NULL
            SET @ItemsTextPath = N'$[' + CAST(@ItemsGroupIdx AS NVARCHAR(10)) + N'].children[' + CAST(@ItemsChildIdx AS NVARCHAR(10)) + N'].text';

        IF @ItemsTextPath IS NULL
            PRINT N'  WARN: برگ ' + @ItemsPath + N' در منو یافت نشد — rename انجام نشد';
        ELSE IF JSON_VALUE(@Content, @ItemsTextPath) = @ItemsText
            PRINT N'  برگ اقلام از قبل «' + @ItemsText + N'» است';
        ELSE
        BEGIN
            SET @Content = JSON_MODIFY(@Content, @ItemsTextPath, @ItemsText);
            PRINT N'  برگ ' + @ItemsPath + N' → «' + @ItemsText + N'» تغییر نام یافت';
        END

        -----------------------------------------------------------------------
        -- 2) برگ والد «دسته های خرید» → /panel/sup/buycategory/list
        -----------------------------------------------------------------------
        IF CHARINDEX(N'"path":"' + @ParentPath + N'"', @Content) > 0
        BEGIN
            PRINT N'  برگ «' + @ParentText + N'» (' + @ParentPath + N') از قبل موجود است';
        END
        ELSE IF @ItemsRootIdx IS NOT NULL
        BEGIN
            -- درج درست قبل از برگ اقلام در ریشه (بازسازی آرایه با حفظ ترتیب)
            SELECT @Content = N'[' + STRING_AGG(CAST(x.Item AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY x.Ord) + N']'
            FROM (
                SELECT CAST([key] AS INT) * 2 AS Ord, [value] AS Item FROM OPENJSON(@Content)
                UNION ALL
                SELECT @ItemsRootIdx * 2 - 1, @ParentLeaf
            ) x;
            PRINT N'  برگ «' + @ParentText + N'» قبل از برگ اقلام (ریشه، ایندکس ' + CAST(@ItemsRootIdx AS NVARCHAR(10)) + N') درج شد';
        END
        ELSE IF @ItemsGroupIdx IS NOT NULL
        BEGIN
            DECLARE @ChildrenPath NVARCHAR(50) = N'$[' + CAST(@ItemsGroupIdx AS NVARCHAR(10)) + N'].children';
            SET @Content = JSON_MODIFY(@Content, N'append ' + @ChildrenPath, JSON_QUERY(@ParentLeaf));
            PRINT N'  برگ «' + @ParentText + N'» به children گروه ایندکس ' + CAST(@ItemsGroupIdx AS NVARCHAR(10)) + N' اضافه شد';
        END
        ELSE
        BEGIN
            SET @Content = JSON_MODIFY(@Content, N'append $', JSON_QUERY(@ParentLeaf));
            PRINT N'  برگ «' + @ParentText + N'» به انتهای منو اضافه شد (برگ اقلام یافت نشد)';
        END

        IF ISJSON(@Content) = 0
            THROW 51201, N'Content تولیدشده JSON معتبر نیست — تغییری اعمال نشد', 1;

        UPDATE system.SystemMenu
        SET Content = @Content,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;

        PRINT N'  SystemMenu Id=' + CAST(@MenuId AS NVARCHAR(20)) + N' به‌روزرسانی شد';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_BuyCategory_Menu ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-- تایید نتیجه: برگ‌های مرتبط منوی Supply (ریشه + سطح اول)
SELECT m.Id, m.Name, m.Title, m.AccessRoleIds,
       CAST(r.[key] AS INT) AS RootIdx,
       JSON_VALUE(r.[value], '$.text') AS [Text],
       JSON_VALUE(r.[value], '$.path') AS [Path]
FROM system.SystemMenu m
CROSS APPLY OPENJSON(m.Content) r
WHERE (m.Name = N'Supply' OR m.Title = N'تامین و خرید')
  AND (JSON_VALUE(r.[value], '$.path') LIKE N'/panel/sup/buycategory%'
       OR EXISTS (SELECT 1 FROM OPENJSON(r.[value], '$.children') c WHERE JSON_VALUE(c.[value], '$.path') LIKE N'/panel/sup/buycategory%'))
ORDER BY m.Id, RootIdx;
