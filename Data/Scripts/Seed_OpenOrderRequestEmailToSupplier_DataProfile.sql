/*
================================================================================
Seed_OpenOrderRequestEmailToSupplier_DataProfile.sql  (WP1 / D1 / D26)
================================================================================
نمایه داده صفحه 545 HTS «پیشینه ارسال به پیمانکاران»
  /panel/sup/openorderrequestemailtosupplier/list

ستون‌ها مطابق گرید HTS (_OpenOrderRequestEmailToSupplier):
  شماره درخواست خرید، شماره سفارش، کد/نام/واحد کالا، نام شرکت‌ها،
  شرکت گیرنده سفارش، تعداد درخواست، تعداد ارسال، زمان ارسال، ساعت ارسال، متولی خرید.

دسترسی مستقل از «پیشینه درخواست‌ها» صفحه 77 (D26):
  نقش جدید 100015 Sup.OpenOrderRequest.SendHistoryView
    ← گروه HTS 525 «کارشناسان ( پیشینه ارسال به پیمانکاران )»
  + ConfigManage، Supply، SupplyAndPurchase، ShowAllMenus

ActionOptions: new/delete خاموش؛ edit = مشاهده فقط‌خواندنی؛ خروجی اکسل روشن.

پیش‌نیاز: جدول Sup.OpenOrderRequestEmailToSupplier
بعد از اجرا: Restart WebApp / پاک‌کردن Redis DataTableProfileCacheDb
            کاربران باید دوباره لاگین کنند (کش نقش)
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'Sup.OpenOrderRequestEmailToSupplier', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Sup.OpenOrderRequestEmailToSupplier وجود ندارد. ابتدا migration یا Add_OpenOrderRequestEmailToSupplier_Table.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-emailtosupplier-dataprofile';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.sup.openorderrequestemailtosupplier';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sup.OpenOrderRequestEmailToSupplier';
    DECLARE @ProfileName NVARCHAR(100) = N'OpenOrderRequestEmailToSupplier_List';
    DECLARE @ProfileTitle NVARCHAR(200) = N'پیشینه ارسال به پیمانکاران';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] نقش SendHistoryView (D26) ===';
    -----------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sup.OpenOrderRequest.SendHistoryView')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (100015, N'Sup.OpenOrderRequest.SendHistoryView', N'تامین و خرید - پیشینه ارسال به پیمانکاران - مشاهده',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=100015 Sup.OpenOrderRequest.SendHistoryView';
    END
    ELSE
        PRINT N'  EXISTS Role Sup.OpenOrderRequest.SendHistoryView';

    DECLARE @BaseSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[PurchaseRequestNumber] AS [t2_PurchaseRequestNumber],
    [t2].[OrderNo] AS [t2_OrderNo],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t6].[Title] AS [t6_Title],
    [tCo].[CompanyNames] AS [tCo_CompanyNames],
    [t5].[FullName] AS [t5_FullName],
    [t2].[RequiredQty] AS [t2_RequiredQty],
    [t1].[SendingCount] AS [t1_SendingCount],
    LEFT([t1].[SendShamsiDateTime], 10) AS [t1_SendDate],
    NULLIF(LTRIM(SUBSTRING([t1].[SendShamsiDateTime], 12, 8)), N'''') AS [t1_SendTime],
    COALESCE([t7].[NameFa], [t1].[CreatedByName]) AS [tSender_Name]
FROM [Sup].[OpenOrderRequestEmailToSupplier] AS [t1]
INNER JOIN [Sup].[OpenOrderRequest] AS [t2] ON [t1].[OpenOrderRequestId] = [t2].[Id]
LEFT JOIN [Inv].[Part] AS [t3] ON [t2].[PartId] = [t3].[Id]
LEFT JOIN [Inv].[PartUnit] AS [t6] ON [t3].[UnitId] = [t6].[Id]
LEFT JOIN [Gnr].[Supplier] AS [t4] ON [t1].[SupplierId] = [t4].[Id]
LEFT JOIN [Gnr].[Party] AS [t5] ON [t4].[PartyId] = [t5].[Id]
LEFT JOIN [system].[User] AS [t7] ON [t1].[SenderUserId] = [t7].[Id]
OUTER APPLY (
    SELECT STRING_AGG(CAST([p].[FullName] AS nvarchar(max)), N''، '') WITHIN GROUP (ORDER BY [p].[FullName]) AS [CompanyNames]
    FROM [Inv].[PartCompany] AS [pc]
    INNER JOIN [Gnr].[Supplier] AS [s] ON [s].[Id] = [pc].[SupplierId]
    LEFT JOIN [Gnr].[Party] AS [p] ON [p].[Id] = [s].[PartyId]
    WHERE [pc].[PartId] = [t2].[PartId]
) AS [tCo]';

    DECLARE @Columns NVARCHAR(MAX) = CAST(N'[' AS NVARCHAR(MAX)) +
N'{"TableName":"Sup.OpenOrderRequestEmailToSupplier","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":1,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Sup.OpenOrderRequest","ColumnName":"t2_PurchaseRequestNumber","DisplayName":"شماره درخواست خرید","Alliance":"t2_PurchaseRequestNumber","Address":"[t2].[PurchaseRequestNumber]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Sup.OpenOrderRequest","ColumnName":"t2_OrderNo","DisplayName":"شماره سفارش","Alliance":"t2_OrderNo","Address":"[t2].[OrderNo]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Inv.Part","ColumnName":"t3_Code","DisplayName":"کد کالا","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Inv.Part","ColumnName":"t3_Name","DisplayName":"نام کالا","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Inv.PartUnit","ColumnName":"t6_Title","DisplayName":"واحد کالا","Alliance":"t6_Title","Address":"[t6].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Inv.PartCompany","ColumnName":"tCo_CompanyNames","DisplayName":"نام شرکت ها","Alliance":"tCo_CompanyNames","Address":"[tCo].[CompanyNames]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Gnr.Party","ColumnName":"t5_FullName","DisplayName":"شرکت گیرنده سفارش","Alliance":"t5_FullName","Address":"[t5].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Sup.OpenOrderRequest","ColumnName":"t2_RequiredQty","DisplayName":"تعداد درخواست","Alliance":"t2_RequiredQty","Address":"[t2].[RequiredQty]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Sup.OpenOrderRequestEmailToSupplier","ColumnName":"t1_SendingCount","DisplayName":"تعداد ارسال","Alliance":"t1_SendingCount","Address":"[t1].[SendingCount]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Sup.OpenOrderRequestEmailToSupplier","ColumnName":"t1_SendDate","DisplayName":"زمان ارسال","Alliance":"t1_SendDate","Address":"LEFT([t1].[SendShamsiDateTime], 10)","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"Sup.OpenOrderRequestEmailToSupplier","ColumnName":"t1_SendTime","DisplayName":"ساعت ارسال","Alliance":"t1_SendTime","Address":"NULLIF(LTRIM(SUBSTRING([t1].[SendShamsiDateTime], 12, 8)), N'''')","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},' +
N'{"TableName":"system.User","ColumnName":"tSender_Name","DisplayName":"متولی خرید","Alliance":"tSender_Name","Address":"COALESCE([t7].[NameFa], [t1].[CreatedByName])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""}' +
N']';

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"مشاهده"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @QueryJson NVARCHAR(MAX) = JSON_MODIFY(N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}', '$.CustomQuery', @BaseSelect);

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Upsert SavedQuery ===';
    -----------------------------------------------------------------------------
    DECLARE @ProfileId BIGINT;
    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @ProfileTitle, QueryJson = @QueryJson, ColumnsJson = @Columns,
            EntityFullName = @EntityFullName, Mode = 1, Type = 1,
            ActionOptions = @ActionOptions, CustomActionButtonsJson = N'[]',
            EventScriptsJson = @EventScripts, DiagramJson = N'{}',
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @ProfileName;
        SELECT @ProfileId = Id FROM system.SavedQuery WHERE Name = @ProfileName;
        PRINT N'  UPDATED SavedQuery ' + @ProfileName;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@ProfileName, @ProfileTitle, @QueryJson, @Columns, N'{}', N'[]', @EventScripts,
             1, 1, @EntityFullName, @ActionOptions,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @ProfileId = SCOPE_IDENTITY();
        PRINT N'  CREATED SavedQuery ' + @ProfileName;
    END

    -----------------------------------------------------------------------------
    PRINT N'=== [3] RoleAccess نمایه + مسیر صفحه ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#EtsRoles') IS NOT NULL DROP TABLE #EtsRoles;
    CREATE TABLE #EtsRoles (RoleId BIGINT NOT NULL PRIMARY KEY, RoleName NVARCHAR(200) NULL);
    INSERT INTO #EtsRoles (RoleId, RoleName)
    SELECT r.Id, r.Name
    FROM system.Role r
    WHERE r.Name IN (
        N'Sup.OpenOrderRequest.SendHistoryView',
        N'Sup.OpenOrderRequest.ConfigManage',
        N'Sup.OpenOrderRequest.Supply',
        N'SupplyAndPurchase',
        N'ShowAllMenus');

    DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @ProfileId;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)), 3, NULL, @EntityPascal, @ProfileTitle, NULL, @ProfileId, b.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #EtsRoles b;

    DECLARE @Paths TABLE (Path NVARCHAR(400) NOT NULL, AccessType INT NOT NULL, ItemType INT NOT NULL);
    INSERT INTO @Paths (Path, AccessType, ItemType) VALUES
        (N'/panel/sup/openorderrequestemailtosupplier/list',          1, 1),
        (N'/panel/sup/openorderrequestemailtosupplier/fetchdata',     2, 2),
        (N'/panel/sup/openorderrequestemailtosupplier/exporttoexcel', 2, 0),
        (N'/panel/sup/openorderrequestemailtosupplier/edit',          1, 5);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT p.Path, p.AccessType, p.ItemType, @EntityPascal, @ProfileTitle, NULL, NULL, b.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Paths p
    CROSS JOIN #EtsRoles b
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = b.RoleId AND ra.Path = p.Path AND ra.ActionAccessType = p.AccessType);

    -----------------------------------------------------------------------------
    PRINT N'=== [4] اعضای گروه HTS 525 → SendHistoryView ===';
    -----------------------------------------------------------------------------
    DECLARE @SendHistoryRoleId BIGINT =
        (SELECT Id FROM system.Role WHERE Name = N'Sup.OpenOrderRequest.SendHistoryView');
    DECLARE @SendHistoryRoleName NVARCHAR(200) = N'Sup.OpenOrderRequest.SendHistoryView';
    DECLARE @Assigned INT = 0;

    DECLARE @Uid BIGINT;
    DECLARE mem_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT u.Id
        FROM system.[User] u
        WHERE LOWER(u.Username) IN (
            N'sharifi.b', N'sohrabi.za', N'ashrafi.m', N'zabihian.s', N'parhizkari.s')
          AND ISNULL(u.IsActive, 0) = 1
          AND NOT EXISTS (
              SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
              WHERE TRY_CAST(j.value AS BIGINT) = @SendHistoryRoleId);
    OPEN mem_cur;
    FETCH NEXT FROM mem_cur INTO @Uid;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        UPDATE system.[User]
        SET
            RoleIds = CASE
                WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                    THEN N'[' + CAST(@SendHistoryRoleId AS NVARCHAR(20)) + N']'
                ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@SendHistoryRoleId AS NVARCHAR(20)) + N']')
            END,
            Roles = CASE
                WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @SendHistoryRoleName) THEN Roles
                WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                    THEN N'["' + @SendHistoryRoleName + N'"]'
                ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @SendHistoryRoleName + N'"]')
            END,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @Uid;
        SET @Assigned = @Assigned + 1;
        FETCH NEXT FROM mem_cur INTO @Uid;
    END
    CLOSE mem_cur;
    DEALLOCATE mem_cur;
    PRINT N'  SendHistoryView members added: ' + CAST(@Assigned AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_OpenOrderRequestEmailToSupplier_DataProfile ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT q.Id, q.Name, q.Title, q.EntityFullName
FROM system.SavedQuery q WHERE q.Name = N'OpenOrderRequestEmailToSupplier_List';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'OpenOrderRequestEmailToSupplier_List';

SELECT r.Name, ra.Path, ra.ActionAccessType
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path LIKE N'/panel/sup/openorderrequestemailtosupplier%'
   OR (ra.ActionAccessType = 3 AND ra.Path LIKE N'dataProfile_%'
       AND EXISTS (SELECT 1 FROM system.SavedQuery q WHERE q.Id = ra.RowId AND q.Name = N'OpenOrderRequestEmailToSupplier_List'))
ORDER BY ra.Path, r.Name;

SELECT u.Username, u.NameFa
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS BIGINT)
WHERE r.Name = N'Sup.OpenOrderRequest.SendHistoryView'
ORDER BY u.Username;
