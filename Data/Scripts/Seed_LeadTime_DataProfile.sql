-- ============================================================================
-- WP6 (D5/D6/D17) — نمایه داده «زمان در راه» (Pln.LeadTime) مطابق گرید صفحه 414 HTS
--
-- ستون‌های HTS (_LeadTime.cshtml / SuppliesLeadTimeModel):
--   کد کالا | نام کالا | دسته خرید | زمان در راه (روز) | تامین کننده | ایجاد کننده | تاریخ ایجاد | کامنت
-- + ستون جدید «زمان در راه غیرروتین (روز)» (D17 — Pln.LeadTime.NonRoutineLeadTimeInDay)
--
-- دکمه سفارشی «ورود از اکسل» → صفحه ورود اطلاعات (/panel/importData/list) — تعریف ورود در
--   Seed_LeadTime_ImportDefinition.sql (ابتدا آن را اجرا کنید تا در لیست انتخاب شود).
--
-- Idempotent:
--   * upsert بر اساس Name = 'leadtime_listinfo'
--   * اگر Name وجود نداشت ولی قبلاً یک نمایه DataProfile برای entities.app.pln.leadtime ساخته شده
--     (مثلاً از Query Designer)، همان ردیف UPDATE می‌شود (به‌جای ایجاد نمایه تکراری).
--   * RoleAccess با join روی system.Role.Name (بدون RoleId ثابت)
--
-- پیش‌نیاز: ستون NonRoutineLeadTimeInDay روی Pln.LeadTime
--   (migration AddLeadTimeNonRoutineLeadTime یا Add_LeadTime_NonRoutineLeadTimeInDay.sql)
-- بعد از اجرا: ری‌استارت WebApp / پاک‌کردن DataTableProfileCacheDb (Redis)
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH('Pln.LeadTime', 'NonRoutineLeadTimeInDay') IS NULL
BEGIN
    RAISERROR(N'ستون Pln.LeadTime.NonRoutineLeadTimeInDay وجود ندارد. ابتدا migration AddLeadTimeNonRoutineLeadTime یا Add_LeadTime_NonRoutineLeadTimeInDay.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-wp6-leadtime-dataprofile';

    DECLARE @Name NVARCHAR(200) = N'leadtime_listinfo';
    DECLARE @Title NVARCHAR(200) = N'زمان در راه';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.pln.leadtime';       -- SavedQuery.EntityFullName (lowercase)
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Pln.LeadTime';         -- RoleAccess.EntityName (Pascal)

    -- HTS 414: New / Edit / Delete برای «کارشناسان زمان در راه»، Read برای «مشاهده» → CRUD استاندارد + اکسل
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    ---------------------------------------------------------------------------
    -- CustomQuery (t1 = Pln.LeadTime, t2 = Inv.Part, t3 = Sup.BuyCategory)
    -- دسته خرید مثل HTS از روی کالا (Inv.Part.BuyCategoryId) خوانده می‌شود.
    ---------------------------------------------------------------------------
    DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t3].[Title] AS [t3_Title],
    [t1].[LeadTimeDay] AS [t1_LeadTimeDay],
    [t1].[NonRoutineLeadTimeInDay] AS [t1_NonRoutineLeadTimeInDay],
    [t1].[Supplier] AS [t1_Supplier],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[Comment] AS [t1_Comment]
FROM [Pln].[LeadTime] AS [t1]
LEFT JOIN [Inv].[Part] AS [t2]
    ON [t1].[PartId] = [t2].[Id]
LEFT JOIN [Sup].[BuyCategory] AS [t3]
    ON [t2].[BuyCategoryId] = [t3].[Id]';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    ---------------------------------------------------------------------------
    -- ColumnsJson — همه فیلدهای QueryColumn (havayar-dataprofile-savedquery.mdc §3)
    ---------------------------------------------------------------------------
    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Pln.LeadTime","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":1,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"Code","DisplayName":"کد کالا","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"Name","DisplayName":"نام کالا","Alliance":"t2_Name","Address":"[t2].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":350,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sup.BuyCategory","ColumnName":"Title","DisplayName":"دسته خرید","Alliance":"t3_Title","Address":"[t3].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.LeadTime","ColumnName":"LeadTimeDay","DisplayName":"زمان در راه (روز)","Alliance":"t1_LeadTimeDay","Address":"[t1].[LeadTimeDay]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.LeadTime","ColumnName":"NonRoutineLeadTimeInDay","DisplayName":"زمان در راه غیرروتین (روز)","Alliance":"t1_NonRoutineLeadTimeInDay","Address":"[t1].[NonRoutineLeadTimeInDay]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.LeadTime","ColumnName":"Supplier","DisplayName":"تامین کننده","Alliance":"t1_Supplier","Address":"[t1].[Supplier]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Pln.Enums.LeadTimeSupplierEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.LeadTime","ColumnName":"CreatedByName","DisplayName":"ایجاد کننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.LeadTime","ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.LeadTime","ColumnName":"Comment","DisplayName":"کامنت","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1
        THROW 51600, N'ColumnsJson / QueryJson معتبر نیست.', 1;

    ---------------------------------------------------------------------------
    -- دکمه سفارشی «ورود از اکسل» (معادل دکمه ImportExcel صفحه 414 HTS)
    -- صفحه ورود اطلاعات عمومی است؛ کاربر تعریف «ورود اطلاعات زمان در راه (اکسل)» را انتخاب می‌کند.
    ---------------------------------------------------------------------------
    DECLARE @ImportScript NVARCHAR(MAX) = N'function(ctx) {
    appController.addPage(''/panel/importData/list'', true, ''ورود اطلاعات زمان در راه'');
}';

    DECLARE @CustomButtons NVARCHAR(MAX) = N'[' + (
        SELECT
            N'id_leadTimeImportExcel'      AS id,
            N'ورود از اکسل'                AS title,
            N'leadTimeImportExcel'         AS dataActionName,
            N'btn-color-success'           AS colorClass,
            N'ki-outline ki-file-up'       AS iconClass,
            CAST(0 AS BIT)                 AS requiresSelection,
            CAST(0 AS BIT)                 AS requiresMultiSelection,
            CAST(0 AS BIT)                 AS useHtml,
            N''                            AS html,
            @ImportScript                  AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) + N']';

    ---------------------------------------------------------------------------
    -- Upsert نمایه: اول بر اساس Name، در غیر این صورت نمایه موجود همین موجودیت
    ---------------------------------------------------------------------------
    DECLARE @ProfileId BIGINT;

    SELECT TOP (1) @ProfileId = Id
    FROM [system].[SavedQuery]
    WHERE Name = @Name
       OR ([Type] = 1 AND LOWER(EntityFullName) = @EntityFullName)
    ORDER BY CASE WHEN Name = @Name THEN 0 ELSE 1 END, Id;

    IF @ProfileId IS NOT NULL
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Name = @Name,
            Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            EntityFullName = @EntityFullName,
            Mode = 1,
            [Type] = 1,
            ActionOptions = @ActionOptions,
            CustomActionButtonsJson = @CustomButtons,
            EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}',
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Id = @ProfileId;

        PRINT N'SavedQuery updated: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
        (
            Name, Title, QueryJson, ColumnsJson, DiagramJson,
            CustomActionButtonsJson, EventScriptsJson,
            Mode, [Type], EntityFullName, ActionOptions,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        VALUES
        (
            @Name, @Title, @QueryJson, @ColumnsJson, N'{}',
            @CustomButtons, N'{"onSelectedRow":"","onRowAdded":""}',
            1, 1, @EntityFullName, @ActionOptions,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );

        SET @ProfileId = SCOPE_IDENTITY();
        PRINT N'SavedQuery inserted: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END;

    ---------------------------------------------------------------------------
    -- RoleAccess نمایه (ActionAccessType=3 DataProfile, ItemType=7)
    -- HTS: گروه 291 «کارشناسان زمان در راه» (New/Edit/Delete) + 524 «مشاهده زمان در راه» (Read)
    -- معادل موجود در جدید: نقش‌های تامین و خرید + منوی تامین + نمایش همه منوها
    -- (نقش مستقل «مشاهده زمان در راه» و نگاشت اعضای گروه 291/524 → WP4 / D38)
    ---------------------------------------------------------------------------
    DECLARE @Path NVARCHAR(100) = N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20));

    DECLARE @Roles TABLE (RoleName NVARCHAR(200));
    INSERT INTO @Roles (RoleName) VALUES
        (N'SupplyAndPurchase'),              -- 100017 تامین و خرید (HTS FullAccess)
        (N'Sup.OpenOrderRequest.Supply'),    -- 100010 تدارکات (درخواست‌های باز)
        (N'SupplierMenu'),                   -- 400008 منو تامین و خرید
        (N'ShowAllMenus');                   -- 200052 نمایش همه منوها

    -- فقط ردیف‌های غایب اضافه می‌شوند (دسترسی‌هایی که ادمین دستی به این نمایه داده حفظ می‌شود)
    INSERT INTO [system].[RoleAccess]
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId,
        CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        @Path, 3, 7, @EntityPascal, NULL,
        NULL, @ProfileId, r.Id,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM [system].[Role] r
    INNER JOIN @Roles x ON x.RoleName = r.Name
    WHERE NOT EXISTS (
        SELECT 1 FROM [system].[RoleAccess] ra
        WHERE ra.Path = @Path AND ra.RoleId = r.Id AND ra.ActionAccessType = 3);

    PRINT N'RoleAccess rows added for profile: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    SELECT x.RoleName AS MissingRole
    FROM @Roles x
    WHERE NOT EXISTS (SELECT 1 FROM [system].[Role] r WHERE r.Name = x.RoleName);

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_LeadTime_DataProfile ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

-- بازبینی
SELECT sq.Id, sq.Name, sq.Title, sq.EntityFullName, sq.[Type], sq.Mode, sq.IsActive,
       LEN(sq.ColumnsJson) AS ColsLen, LEN(sq.CustomActionButtonsJson) AS BtnLen
FROM [system].[SavedQuery] sq
WHERE sq.Name = N'leadtime_listinfo';

SELECT ra.Path, ra.ActionAccessType, ra.ActionAccessItemType, ra.EntityName, ra.RowId, r.Id AS RoleId, r.Name AS RoleName, r.Title AS RoleTitle
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
INNER JOIN [system].[SavedQuery] sq ON sq.Id = ra.RowId AND ra.ActionAccessType = 3
WHERE sq.Name = N'leadtime_listinfo'
ORDER BY r.Name;
