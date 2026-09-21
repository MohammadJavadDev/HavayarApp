/*
  Seed_SaleMissionSalary_DataProfilesAndRoles.sql
  نمایه حق ماموریت پرسنل مطابق گرید HTS صفحه Sale_Personel_MissionSalary (141)
  + نقش و انتساب کاربر/گروه از Vw_Permission و Gnr_UserGroupMember.
  Idempotent. Requires [TMS] → TotalSystem.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sale-missionsalary';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.sale.missionsalary';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sale.MissionSalary';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    PRINT N'=== [1] Roles 200061 Manage / 200062 View ===';
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.MissionSalary.Manage')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200061, N'Sale.MissionSalary.Manage', N'فروش - حق ماموریت پرسنل - مدیریت',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Sale.MissionSalary.Manage';
    END
    ELSE PRINT N'  EXISTS Sale.MissionSalary.Manage';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.MissionSalary.View')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200062, N'Sale.MissionSalary.View', N'فروش - حق ماموریت پرسنل - مشاهده',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Sale.MissionSalary.View';
    END
    ELSE PRINT N'  EXISTS Sale.MissionSalary.View';

    PRINT N'=== [2] Data Profile ===';
    DECLARE @PName NVARCHAR(100) = N'AfterSales_MissionSalary_List';
    DECLARE @PCols NVARCHAR(MAX) = N'[{"TableName":"Sale.MissionSalary","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hcm.Personel","ColumnName":"tExpert_Name","DisplayName":"نام و نام خانوادگی","Alliance":"tExpert_Name","Address":"COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(ISNULL([hp].[Name], N''''), N'' '', ISNULL([hp].[Family], N'''')))), N''''), [t3].[FullName], [t2].[NameFa], [t2].[Username])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.MissionSalary","ColumnName":"t1_BreakfastFee","DisplayName":"هزینه صبحانه","Alliance":"t1_BreakfastFee","Address":"[t1].[BreakfastFee]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.MissionSalary","ColumnName":"t1_LunchFee","DisplayName":"هزینه نهار","Alliance":"t1_LunchFee","Address":"[t1].[LunchFee]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.MissionSalary","ColumnName":"t1_DinnerFee","DisplayName":"هزینه شام","Alliance":"t1_DinnerFee","Address":"[t1].[DinnerFee]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.MissionSalary","ColumnName":"t1_NightRight","DisplayName":"حق شب","Alliance":"t1_NightRight","Address":"[t1].[NightRight]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.MissionSalary","ColumnName":"t1_WorkOvertimeFee","DisplayName":"مبلغ اضافه کاری","Alliance":"t1_WorkOvertimeFee","Address":"[t1].[WorkOvertimeFee]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.MissionSalary","ColumnName":"t1_MissionRight","DisplayName":"حق ماموریت","Alliance":"t1_MissionRight","Address":"[t1].[MissionRight]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.MissionSalary","ColumnName":"t1_MissionHolidayRight","DisplayName":"حق تعطیل کاری","Alliance":"t1_MissionHolidayRight","Address":"[t1].[MissionHolidayRight]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @PSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    COALESCE(
        NULLIF(LTRIM(RTRIM(CONCAT(ISNULL([hp].[Name], N''''), N'' '', ISNULL([hp].[Family], N'''')))), N''''),
        [t3].[FullName],
        [t2].[NameFa],
        [t2].[Username]
    ) AS [tExpert_Name],
    [t1].[BreakfastFee] AS [t1_BreakfastFee],
    [t1].[LunchFee] AS [t1_LunchFee],
    [t1].[DinnerFee] AS [t1_DinnerFee],
    [t1].[NightRight] AS [t1_NightRight],
    [t1].[WorkOvertimeFee] AS [t1_WorkOvertimeFee],
    [t1].[MissionRight] AS [t1_MissionRight],
    [t1].[MissionHolidayRight] AS [t1_MissionHolidayRight]
FROM [Sale].[MissionSalary] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [hp] ON [t1].[PersonelId] = [hp].[Id]
LEFT JOIN [Gnr].[Party] AS [t3] ON [hp].[PartyId] = [t3].[Id]
LEFT JOIN [system].[User] AS [t2] ON [t1].[ExpertId] = [t2].[Id]';
    DECLARE @PAct NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @PBtn NVARCHAR(MAX) = N'[{"id":"as_edit_missionsalary","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sale/MissionSalary/Edit?id='' + id, true, ''ویرایش'');\n}"}]';
    DECLARE @PQueryJson NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);
    DECLARE @PId BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = N'حق ماموریت پرسنل', QueryJson = @PQueryJson, ColumnsJson = @PCols,
            EntityFullName = @EntityFullName, Mode = 1, Type = 1,
            ActionOptions = @PAct, CustomActionButtonsJson = @PBtn, DiagramJson = N'{}',
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @PName;
        SELECT @PId = Id FROM system.SavedQuery WHERE Name = @PName;
        PRINT N'  UPDATED profile Id=' + CAST(@PId AS nvarchar(20));
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@PName, N'حق ماموریت پرسنل', @PQueryJson, @PCols, N'{}', @PBtn, NULL,
             1, 1, @EntityFullName, @PAct,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @PId = SCOPE_IDENTITY();
        PRINT N'  CREATED profile Id=' + CAST(@PId AS nvarchar(20));
    END

    PRINT N'=== [3] Controller RoleAccess ===';
    IF OBJECT_ID('tempdb..#MsAction') IS NOT NULL DROP TABLE #MsAction;
    CREATE TABLE #MsAction (RoleName NVARCHAR(200), Path NVARCHAR(300), ActionAccessType INT, ActionAccessItemType INT);

    INSERT INTO #MsAction (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sale/missionsalary/list',      1, 1),
        (N'/panel/sale/missionsalary/fetchdata', 2, 2),
        (N'/panel/sale/missionsalary/edit',      1, 5)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'Sale.MissionSalary.Manage'), (N'Sale.MissionSalary.View')) r(RoleName);

    INSERT INTO #MsAction (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT N'Sale.MissionSalary.Manage', a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sale/missionsalary/new',            1, 4),
        (N'/panel/sale/missionsalary/save',           2, 3),
        (N'/panel/sale/missionsalary/add',            2, 4),
        (N'/panel/sale/missionsalary/update',         2, 5),
        (N'/panel/sale/missionsalary/delete',         2, 6),
        (N'/panel/sale/missionsalary/exporttoexcel',  2, 0)
    ) a(Path, AType, IType);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType, @EntityPascal, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #MsAction a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [4] DataProfile RoleAccess ===';
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT N'dataProfile_' + CAST(@PId AS nvarchar(20)), 3, 7, @EntityPascal, N'حق ماموریت پرسنل', NULL, @PId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'Sale.MissionSalary.Manage', N'Sale.MissionSalary.View', N'Sale.AfterSales', N'ShowAllMenus')
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.ActionAccessType = 3 AND ra.RowId = @PId AND ra.RoleId = r.Id
      );
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [5] Menu AccessRoleIds (merge, keep existing) ===';
    DECLARE @MenuId BIGINT;
    SELECT TOP 1 @MenuId = Id FROM system.SystemMenu WHERE Name = N'AfterSalesServiceSystem';
    IF @MenuId IS NOT NULL
    BEGIN
        DECLARE @AccessRoles NVARCHAR(MAX), @AccessRoleIds NVARCHAR(MAX);
        SELECT @AccessRoles = AccessRoles, @AccessRoleIds = AccessRoleIds FROM system.SystemMenu WHERE Id = @MenuId;
        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName nvarchar(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT j.value FROM OPENJSON(ISNULL(NULLIF(LTRIM(RTRIM(@AccessRoles)), N''), N'[]')) j
        WHERE j.value IS NOT NULL AND LTRIM(RTRIM(j.value)) <> N'';
        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT r.Name
        FROM OPENJSON(ISNULL(NULLIF(LTRIM(RTRIM(@AccessRoleIds)), N''), N'[]')) j
        INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS bigint)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles m WHERE m.RoleName = r.Name);
        INSERT INTO #MenuRoles (RoleName)
        SELECT v.RoleName FROM (VALUES
            (N'Sale.MissionSalary.Manage'), (N'Sale.MissionSalary.View'),
            (N'Sale.AfterSales'), (N'ShowAllMenus')
        ) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles m WHERE m.RoleName = v.RoleName);
        SELECT @AccessRoles = N'[' + STUFF((SELECT N',"' + RoleName + N'"' FROM #MenuRoles ORDER BY RoleName FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        SELECT @AccessRoleIds = N'[' + STUFF((
            SELECT N',' + CAST(r.Id AS nvarchar(20))
            FROM #MenuRoles m INNER JOIN system.Role r ON r.Name = m.RoleName
            ORDER BY r.Id FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        UPDATE system.SystemMenu
        SET AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
        PRINT N'  Menu updated Id=' + CAST(@MenuId AS nvarchar(20));
    END
    ELSE PRINT N'  WARN menu AfterSalesServiceSystem not found';

    PRINT N'=== [6] Assign HTS users (direct + group) page 141 ===';
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, Source NVARCHAR(80) NOT NULL);
    IF OBJECT_ID('tempdb..#HtsGrant') IS NOT NULL DROP TABLE #HtsGrant;
    CREATE TABLE #HtsGrant (Permission_ID INT, User_FK INT, AdName NVARCHAR(200), HtsUsername NVARCHAR(200), Source NVARCHAR(80));

    INSERT INTO #HtsGrant (Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT p.Permission_ID, p.User_FK,
           NULLIF(LTRIM(RTRIM(p.ActiveDirectoryUsername)), N''),
           NULLIF(LTRIM(RTRIM(hu.Username)), N''),
           CASE WHEN p.UserGroup_ID IS NULL THEN N'direct' ELSE N'group:' + CAST(p.UserGroup_ID AS nvarchar(20)) END
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
    WHERE p.System_ID = 14 AND p.Page_ID = 141 AND p.IsActive = 1 AND p.User_FK IS NOT NULL;

    INSERT INTO #HtsGrant (Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT DISTINCT gp.Permission_ID, u.User_ID,
           NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
           NULLIF(LTRIM(RTRIM(u.Username)), N''),
           N'group-member:' + CAST(gp.UserGroup_ID AS nvarchar(20))
    FROM (
        SELECT DISTINCT Permission_ID, UserGroup_ID
        FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
        WHERE System_ID = 14 AND Page_ID = 141 AND IsActive = 1 AND UserGroup_ID IS NOT NULL
    ) gp
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = gp.UserGroup_ID
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    WHERE u.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant x WHERE x.Permission_ID = gp.Permission_ID AND x.User_FK = u.User_ID
      );

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT x.Username, x.RoleName, x.Source
    FROM (
        SELECT COALESCE(g.AdName, g.HtsUsername) AS Username, N'Sale.MissionSalary.Manage' AS RoleName, g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID = 2 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL
        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.MissionSalary.View', g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID IN (3, 7) AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM #HtsGrant f
              WHERE f.Permission_ID = 2
                AND LOWER(COALESCE(f.AdName, f.HtsUsername)) = LOWER(COALESCE(g.AdName, g.HtsUsername))
          )
    ) x WHERE x.Username IS NOT NULL;

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT hu.Username, m.RoleName, m.Source + N'+htsUser'
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON LOWER(hu.ActiveDirectoryUsername) = LOWER(m.Username)
    WHERE hu.Username IS NOT NULL AND LOWER(hu.Username) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x WHERE LOWER(x.Username) = LOWER(hu.Username) AND x.RoleName = m.RoleName
      );

    ;WITH d AS (
        SELECT Username, RoleName, ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleName ORDER BY Username) AS rn
        FROM #UserRoleMap
    )
    DELETE FROM d WHERE rn > 1;

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0;
    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName;
        SELECT TOP 1 @MapUserId = Id FROM system.[User] WHERE LOWER(Username) = LOWER(@MapUsername);
        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF @MapRoleId IS NULL
            PRINT N'  MISSING ROLE: ' + @MapRoleName;
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u CROSS APPLY OPENJSON(u.RoleIds) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
        )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + @MapRoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @MapRoleName + N'"]')
                END,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END
        SET @MapUserId = NULL; SET @MapRoleId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    END
    CLOSE map_cur; DEALLOCATE map_cur;
    PRINT N'  Assigned: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already: ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing: ' + CAST(@Missing AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SaleMissionSalary_DataProfilesAndRoles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT r.Name, r.Id,
       (SELECT COUNT(*) FROM system.RoleAccess ra WHERE ra.RoleId=r.Id AND ra.Path LIKE N'/panel/sale/missionsalary%') AS Acc,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds,N'[]')) j WHERE TRY_CAST(j.value AS bigint)=r.Id) AS Users
FROM system.Role r WHERE r.Name LIKE N'Sale.MissionSalary%' ORDER BY r.Name;

SELECT u.Username, r.Name AS RoleName
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds,N'[]')) j
INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS bigint)
WHERE r.Name LIKE N'Sale.MissionSalary%'
ORDER BY r.Name, u.Username;
