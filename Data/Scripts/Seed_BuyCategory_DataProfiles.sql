/*
================================================================================
Seed_BuyCategory_DataProfiles.sql  (WP5 / D10)
================================================================================
نمایه داده (system.SavedQuery, Type=1 DataProfile, Mode=1 Query) برای صفحه والد «دسته های خرید»
  /panel/sup/buycategory/list  ↔  HTS صفحه 70 (_BuyCategoryManagement)
ستون‌ها مطابق گرید HTS: عنوان دسته خرید، نوع، متولی خرید، Lead time روتین/غیرروتین
  + تعداد اقلام، کد راهکاران، آخرین به‌روزرسانی (= آخرین همگام‌سازی جاب)

تصمیم Q2 = (a) (08-Decisions-2026-09-17): راهکاران مرجع کامل؛ دسته، اقلام، متولی خرید و lead time در این سامانه **فقط‌خواندنی**.
  → ActionOptions: new / edit / delete غیرفعال؛ فقط خروجی اکسل + دکمه‌های سفارشی «مشاهده دسته» و «اقلام دسته های خرید».
  → اکشن‌های نوشتن کنترلر (save/add/update/delete/new) به هیچ نقشی داده نمی‌شوند و در کد همیشه isSuccess=false برمی‌گردانند.

دسترسی:
  [3] RoleAccess نمایه (ActionAccessType=3) برای همان نقش‌هایی که امروز /panel/sup/buycategoryitem/list دارند
      (SupplyAndPurchase 100017، ShowAllMenus 200052 — به‌صورت دینامیک از RoleAccess خوانده می‌شود + نام نقش‌ها به‌عنوان fallback)
  [4] RoleAccess صفحه برای مسیرهای کنترلر BuyCategoryController: list / fetchdata / exporttoexcel / edit (= صفحه «مشاهده» فقط‌خواندنی)
      + پاک‌سازی ردیف‌های نوشتن (save/update/add/new/delete) اگر از نسخهٴ قبلی این اسکریپت مانده باشند
  [5] نمایه موجود اقلام (buycategoryitem_listinfo) هم به new/edit/delete=false قفل می‌شود

ColumnName = Alliance (حالت Query). Idempotent — safe to re-run.
بعد از اجرا: Restart WebApp یا پاک‌کردن Redis DataTableProfileCacheDb (کش نمایه‌ها).
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-buycategory-dataprofiles';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.sup.buycategory';
    DECLARE @EntityFullNamePascal NVARCHAR(200) = N'Entities.App.Sup.BuyCategory';
    DECLARE @ProfileName NVARCHAR(100) = N'BuyCategory_List';
    DECLARE @ProfileTitle NVARCHAR(200) = N'دسته های خرید';

    -----------------------------------------------------------------------------
    PRINT N'=== [1/5] Build CustomQuery / ColumnsJson / ActionOptions ===';
    -----------------------------------------------------------------------------

    DECLARE @BaseSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[Title] AS [t1_Title],
    [t1].[Type] AS [t1_Type],
    [t2].[Name] AS [t2_Name],
    [t1].[RoutineLeadTimeInDay] AS [t1_RoutineLeadTimeInDay],
    [t1].[NonRoutineLeadTimeInDay] AS [t1_NonRoutineLeadTimeInDay],
    ISNULL([tCnt].[ItemCount], 0) AS [tCnt_ItemCount],
    [t1].[HamkaranId] AS [t1_HamkaranId],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime]
FROM [Sup].[BuyCategory] AS [t1]
LEFT JOIN [system].[User] AS [t2] ON [t1].[PurchaseResponsibleId] = [t2].[Id]
LEFT JOIN (
    SELECT [BuyCategoryId], COUNT(*) AS [ItemCount]
    FROM [Sup].[BuyCategoryItem]
    GROUP BY [BuyCategoryId]
) AS [tCnt] ON [tCnt].[BuyCategoryId] = [t1].[Id]';

    DECLARE @Columns NVARCHAR(MAX) = N'[{"TableName":"Sup.BuyCategory","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sup.BuyCategory","ColumnName":"t1_Title","DisplayName":"عنوان دسته خرید","Alliance":"t1_Title","Address":"[t1].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sup.BuyCategory","ColumnName":"t1_Type","DisplayName":"نوع","Alliance":"t1_Type","Address":"[t1].[Type]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sup.Enums.BuyCategoryTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"system.User","ColumnName":"t2_Name","DisplayName":"متولی خرید","Alliance":"t2_Name","Address":"[t2].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sup.BuyCategory","ColumnName":"t1_RoutineLeadTimeInDay","DisplayName":"زمان تامین روتین (روز)","Alliance":"t1_RoutineLeadTimeInDay","Address":"[t1].[RoutineLeadTimeInDay]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sup.BuyCategory","ColumnName":"t1_NonRoutineLeadTimeInDay","DisplayName":"زمان تامین غیرروتین (روز)","Alliance":"t1_NonRoutineLeadTimeInDay","Address":"[t1].[NonRoutineLeadTimeInDay]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sup.BuyCategoryItem","ColumnName":"tCnt_ItemCount","DisplayName":"تعداد اقلام","Alliance":"tCnt_ItemCount","Address":"ISNULL([tCnt].[ItemCount], 0)","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sup.BuyCategory","ColumnName":"t1_HamkaranId","DisplayName":"کد راهکاران","Alliance":"t1_HamkaranId","Address":"[t1].[HamkaranId]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sup.BuyCategory","ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"آخرین به‌روزرسانی / همگام‌سازی","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""}]';

    -- Q2 = (a): فقط‌خواندنی — new / edit / delete غیرفعال (HTS صفحه 70 New/Edit/Delete داشت؛ مرجع اکنون راهکاران است)
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    -- دکمه‌های سفارشی:
    --   «مشاهده دسته»          → صفحه فقط‌خواندنی /panel/sup/buycategory/edit?id=<id> (نیاز به انتخاب ردیف)
    --   «اقلام دسته های خرید»  → لیست اقلام (صفحه 72) — بدون نیاز به انتخاب ردیف
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[{"id":"id_bc_view","title":"مشاهده دسته","dataActionName":"buyCategoryView","colorClass":"btn-color-info","iconClass":"ki-eye ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx?.primaryKeyValue\n        || ctx?.primaryKeyValues?.[0]\n        || ctx?.selectedRow?.t1_Id;\n\n    if (!id) {\n        toastr.error(''لطفاً یک دسته خرید را انتخاب کنید'');\n        return;\n    }\n\n    appController.addPage(''/panel/sup/buycategory/edit?id='' + id, true, ''مشاهده دسته خرید'');\n}"},{"id":"id_bc_items","title":"اقلام دسته های خرید","dataActionName":"buyCategoryItems","colorClass":"btn-color-primary","iconClass":"ki-category ki-outline","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    appController.addPage(''/panel/sup/buycategoryitem/list'', true, ''اقلام دسته های خرید'');\n}"}]';

    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';

    DECLARE @QueryJson NVARCHAR(MAX) = JSON_MODIFY(N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}', '$.CustomQuery', @BaseSelect);

    -----------------------------------------------------------------------------
    PRINT N'=== [2/5] Upsert SavedQuery DataProfile ===';
    -----------------------------------------------------------------------------
    DECLARE @ProfileId BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @ProfileTitle,
            QueryJson = @QueryJson,
            ColumnsJson = @Columns,
            EntityFullName = @EntityFullName,
            Mode = 1,
            Type = 1,
            ActionOptions = @ActionOptions,
            CustomActionButtonsJson = @CustomButtons,
            EventScriptsJson = @EventScripts,
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @ProfileName;

        SELECT @ProfileId = Id FROM system.SavedQuery WHERE Name = @ProfileName;
        PRINT N'  UPDATED SavedQuery Name=' + @ProfileName + N' Id=' + CAST(@ProfileId AS nvarchar(20));
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
        (
            Name, Title, QueryJson, ColumnsJson, DiagramJson,
            CustomActionButtonsJson, EventScriptsJson,
            Mode, Type, EntityFullName, ActionOptions,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        VALUES
        (
            @ProfileName, @ProfileTitle, @QueryJson, @Columns, N'{}',
            @CustomButtons, @EventScripts,
            1, 1, @EntityFullName, @ActionOptions,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @ProfileId = SCOPE_IDENTITY();
        PRINT N'  CREATED SavedQuery Name=' + @ProfileName + N' Id=' + CAST(@ProfileId AS nvarchar(20));
    END

    -----------------------------------------------------------------------------
    PRINT N'=== [3/5] Rebuild DataProfile RoleAccess (roles that already see /panel/sup/buycategoryitem/list) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#BcRoles') IS NOT NULL DROP TABLE #BcRoles;
    CREATE TABLE #BcRoles (RoleId BIGINT NOT NULL PRIMARY KEY, RoleName NVARCHAR(200) NULL);

    -- (a) دینامیک: هر نقشی که امروز لیست اقلام دسته را می‌بیند
    INSERT INTO #BcRoles (RoleId, RoleName)
    SELECT DISTINCT ra.RoleId, r.Name
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.Path = N'/panel/sup/buycategoryitem/list'
      AND ra.ActionAccessType <> 3;

    -- (b) fallback به نام نقش (اگر در این محیط ردیف RoleAccess نداشتند)
    INSERT INTO #BcRoles (RoleId, RoleName)
    SELECT r.Id, r.Name
    FROM system.Role r
    WHERE r.Name IN (N'SupplyAndPurchase', N'ShowAllMenus')
      AND NOT EXISTS (SELECT 1 FROM #BcRoles b WHERE b.RoleId = r.Id);

    PRINT N'  Target roles: ' + ISNULL((SELECT STRING_AGG(CAST(RoleId AS nvarchar(20)) + N':' + ISNULL(RoleName, N'?'), N', ') FROM #BcRoles), N'(none)');

    DELETE FROM system.RoleAccess
    WHERE ActionAccessType = 3 AND RowId = @ProfileId;
    PRINT N'  Cleared prior DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO system.RoleAccess
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)),
        3,
        NULL,
        @EntityFullNamePascal,
        @ProfileTitle,
        NULL,
        @ProfileId,
        b.RoleId,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM #BcRoles b;
    PRINT N'  Inserted DataProfile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4/5] Page RoleAccess for BuyCategoryController paths (read-only set) ===';
    -----------------------------------------------------------------------------
    -- ActionAccessType: 1 View · 2 Api      ActionAccessItemType: 0 Show · 1 List · 2 FetchData · 3 Save · 4 Create · 5 Update · 6 Delete
    -- شکل ردیف‌ها عین ردیف‌های موجود /panel/sup/buycategoryitem/* (N04): list|1|1، fetchdata|2|2، exporttoexcel|2|0
    -- Q2 = (a): هیچ مسیر نوشتنی (save/update/add/new/delete) برای دسته یا اقلام داده نمی‌شود؛ ردیف‌های احتمالی قبلی پاک می‌شوند.
    DELETE FROM system.RoleAccess
    WHERE ActionAccessType <> 3
      AND Path IN (
        N'/panel/sup/buycategory/save',     N'/panel/sup/buycategory/update',     N'/panel/sup/buycategory/add',
        N'/panel/sup/buycategory/new',      N'/panel/sup/buycategory/delete',
        N'/panel/sup/buycategoryitem/save', N'/panel/sup/buycategoryitem/update', N'/panel/sup/buycategoryitem/add',
        N'/panel/sup/buycategoryitem/new',  N'/panel/sup/buycategoryitem/edit',   N'/panel/sup/buycategoryitem/delete');
    PRINT N'  Removed write-path RoleAccess rows (Q2=a): ' + CAST(@@ROWCOUNT AS nvarchar(20));

    DECLARE @Paths TABLE (Path NVARCHAR(400) NOT NULL, AccessType INT NOT NULL, ItemType INT NOT NULL);
    INSERT INTO @Paths (Path, AccessType, ItemType) VALUES
        (N'/panel/sup/buycategory/list',          1, 1),
        (N'/panel/sup/buycategory/fetchdata',     2, 2),
        (N'/panel/sup/buycategory/exporttoexcel', 2, 0),
        (N'/panel/sup/buycategory/edit',          1, 5);   -- صفحه «مشاهده» فقط‌خواندنی (Edit action = view)

    INSERT INTO system.RoleAccess
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        p.Path, p.AccessType, p.ItemType, @EntityFullNamePascal, N'',
        NULL, NULL, b.RoleId,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM @Paths p
    CROSS JOIN #BcRoles b
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = b.RoleId AND ra.Path = p.Path AND ra.ActionAccessType = p.AccessType);
    PRINT N'  Inserted page RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [5/5] Lock existing items profile (buycategoryitem_listinfo) to view-only ===';
    -----------------------------------------------------------------------------
    UPDATE system.SavedQuery
    SET ActionOptions = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]',
        Title = CASE WHEN Title = N'لیست اطلاعات' THEN N'اقلام دسته های خرید' ELSE Title END,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE EntityFullName = N'entities.app.sup.buycategoryitem'
      AND Type = 1;
    PRINT N'  Items profiles locked (new/edit/delete=false): ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_BuyCategory_DataProfiles committed ===';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR: Seed_BuyCategory_DataProfiles rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

-- Verification
SELECT Id, Name, Title, EntityFullName, Mode, Type, IsActive, ActionOptions
FROM system.SavedQuery
WHERE Name = N'BuyCategory_List'
   OR (EntityFullName = N'entities.app.sup.buycategoryitem' AND Type = 1);

SELECT ra.Path, ra.ActionAccessType, ra.ActionAccessItemType, ra.RowId, ra.DisplayName, r.Name AS RoleName
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.EntityName = N'Entities.App.Sup.BuyCategory'
ORDER BY ra.ActionAccessType, ra.Path, r.Name;
