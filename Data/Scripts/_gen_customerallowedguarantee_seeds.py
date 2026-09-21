# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path(__file__).resolve().parent
MONEY_RENDER = (
    "function(data, type, row) { if (data == null || data === '') return ''; "
    "if (typeof MJUtil !== 'undefined' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }"
)


def esc(s: str) -> str:
    return s.replace("'", "''")


def col(
    table: str,
    column: str,
    display: str,
    alliance: str,
    address: str,
    st_name: str,
    st_num: int,
    visible: bool,
    pk: bool,
    width: int,
    render: str | None = None,
) -> str:
    vis = "true" if visible else "false"
    primary = "true" if pk else "false"
    sd = "1" if pk else "null"
    render_js = f'"{render}"' if render else "null"
    return (
        '{"TableName":"%s","ColumnName":"%s","DisplayName":"%s","Alliance":"%s","Address":"%s",'
        '"SystemTypeName":"%s","SelectIndex":0,"Visible":%s,"PrimaryKey":%s,"SystemType":%s,'
        '"SortDirection":%s,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,'
        '"Aggregate":null,"Render":%s,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,'
        '"BtnConfig":null,"InputConfig":null,"Width":%s,"Filterable":true,"Sortable":true,"ClassName":""}'
        % (table, column, display, alliance, address, st_name, vis, primary, st_num, sd, render_js, width)
    )


cols_json = "[\n" + ",\n".join([
    col("Sale.CustomerAllowedGuarantee", "Id", "شناسه", "t1_Id", "[t1].[Id]", "Long", 6, False, True, 80),
    col("Sale.CustomerAllowedGuarantee", "HtsId", "شناسه HTS", "t1_HtsId", "[t1].[HtsId]", "Long", 6, False, False, 80),
    col("SLS.Customer", "Code", "کد مشتری", "t2_Code", "[t2].[Code]", "Int", 7, True, False, 110),
    col("Gnr.Party", "FullName", "مشتری", "t3_FullName", "[t3].[FullName]", "String", 0, True, False, 200),
    col("Sale.CustomerAllowedGuarantee", "AllowedGuaranteePartAmount", "مقدار گارانتی مجاز قطعه",
        "t1_AllowedGuaranteePartAmount", "[t1].[AllowedGuaranteePartAmount]", "Long", 6, True, False, 160, MONEY_RENDER),
    col("Sale.CustomerAllowedGuarantee", "AllowedMissionGuaranteeAmount", "مقدار گارانتی مجاز ماموریت",
        "t1_AllowedMissionGuaranteeAmount", "[t1].[AllowedMissionGuaranteeAmount]", "Long", 6, True, False, 160, MONEY_RENDER),
    col("Sale.CustomerAllowedGuarantee", "ProductionOrderNumber", "شماره سفارش ساخت",
        "t1_ProductionOrderNumber", "[t1].[ProductionOrderNumber]", "Int", 7, True, False, 140),
    col("Sale.CustomerAllowedGuarantee", "CreatedByName", "ایجادکننده", "t1_CreatedByName",
        "[t1].[CreatedByName]", "String", 0, False, False, 140),
    col("Sale.CustomerAllowedGuarantee", "CreatedOnShamsiDateTime", "تاریخ ایجاد", "t1_CreatedOnShamsiDateTime",
        "[t1].[CreatedOnShamsiDateTime]", "DateTimeShamsi", 4, False, False, 140),
]) + "]"

query = """SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[HtsId] AS [t1_HtsId],
    [t2].[Code] AS [t2_Code],
    [t3].[FullName] AS [t3_FullName],
    [t1].[AllowedGuaranteePartAmount] AS [t1_AllowedGuaranteePartAmount],
    [t1].[AllowedMissionGuaranteeAmount] AS [t1_AllowedMissionGuaranteeAmount],
    [t1].[ProductionOrderNumber] AS [t1_ProductionOrderNumber],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Sale].[CustomerAllowedGuarantee] AS [t1]
LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[CustomerId] = [t2].[Id]
LEFT JOIN [Gnr].[Party] AS [t3] ON [t2].[PartyId] = [t3].[Id]"""

actions = (
    '[{"dataActionName":"new","enable":true,"title":"جدید"},'
    '{"dataActionName":"edit","enable":true,"title":"ویرایش"},'
    '{"dataActionName":"delete","enable":true,"title":"حذف"},'
    '{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]'
)
buttons = (
    '[{"id":"as_edit_customerallowedguarantee","title":"ویرایش","dataActionName":"editRow",'
    '"colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,'
    '"requiresMultiSelection":false,"useHtml":false,"html":"",'
    '"actionScript":"function(ctx) {\\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\\n    if (!id) { toastr.error(\'لطفاً یک ردیف را انتخاب کنید\'); return; }\\n    appController.addPage(\'/Panel/Sale/CustomerAllowedGuarantee/Edit?id=\' + id, true, \'ویرایش\');\\n}"}]'
)

profile_sql = f"""/*
  Seed_AfterSales_CustomerAllowedGuarantee_DataProfiles.sql
  نمایه AfterSales_CustomerAllowedGuarantee_List مطابق گرید HTS صفحه 206.
  Encoding: UTF-8 with BOM (sqlcmd -f 65001). Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-customerallowedguarantee-profile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}}';

    DECLARE @PName NVARCHAR(100) = N'AfterSales_CustomerAllowedGuarantee_List';
    DECLARE @PTitle NVARCHAR(200) = N'گارانتی مجاز مشتریان';
    DECLARE @PEnt NVARCHAR(200) = N'entities.app.sale.customerallowedguarantee';
    DECLARE @PPascal NVARCHAR(200) = N'Entities.App.Sale.CustomerAllowedGuarantee';
    DECLARE @PSelect NVARCHAR(MAX) = N'{esc(query)}';
    DECLARE @PCols NVARCHAR(MAX) = N'{esc(cols_json)}';
    DECLARE @PAct NVARCHAR(MAX) = N'{esc(actions)}';
    DECLARE @PBtn NVARCHAR(MAX) = N'{esc(buttons)}';
    DECLARE @PQueryJson NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);
    DECLARE @PId BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @PTitle,
            QueryJson = @PQueryJson,
            ColumnsJson = @PCols,
            EntityFullName = @PEnt,
            Mode = 1,
            Type = 1,
            ActionOptions = @PAct,
            CustomActionButtonsJson = @PBtn,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @PName;
        SELECT @PId = Id FROM system.SavedQuery WHERE Name = @PName;
        PRINT N'  UPDATED SavedQuery Id=' + CAST(@PId AS nvarchar(20));
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@PName, @PTitle, @PQueryJson, @PCols, N'{{}}', @PBtn, N'{{"onSelectedRow":"","onRowAdded":""}}',
             1, 1, @PEnt, @PAct,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @PId = SCOPE_IDENTITY();
        PRINT N'  CREATED SavedQuery Id=' + CAST(@PId AS nvarchar(20));
    END

    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @PId;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(@PId AS nvarchar(20)),
        3, 7, @PPascal, @PTitle, NULL, @PId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (
        N'Sale.AfterSales', N'ShowAllMenus',
        N'Sale.AfterSales.CustomerAllowedGuarantee.Manage',
        N'Sale.AfterSales.CustomerAllowedGuarantee.View'
    );
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_AfterSales_CustomerAllowedGuarantee_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'AfterSales_CustomerAllowedGuarantee_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'AfterSales_CustomerAllowedGuarantee_List';
"""

roles_sql = r"""/*
  Seed_AfterSales_CustomerAllowedGuarantee_RolesAndUsers.sql
  گارانتی مجاز مشتریان مطابق HTS صفحه 206 Sale_Customer_AllowedGuarantee:
    گروه 48 Full  + 69 Full + اعطای مستقیم Full → Manage
    گروه 300 View + 350 View → View
  اکسل در HTS روی این صفحه نیست؛ در Havayar فقط به Manage / AfterSales / ShowAllMenus داده شد.
  Idempotent. Requires linked server [TMS] → TotalSystem.
  Encoding: UTF-8 with BOM (sqlcmd -f 65001).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-customerallowedguarantee-roles';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sale.CustomerAllowedGuarantee';

    PRINT N'=== [1] Roles 500024 / 500025 ===';
    IF OBJECT_ID('tempdb..#CagRoles') IS NOT NULL DROP TABLE #CagRoles;
    CREATE TABLE #CagRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #CagRoles (WantId, Name, Title) VALUES
        (500024, N'Sale.AfterSales.CustomerAllowedGuarantee.Manage', N'گارانتی مجاز مشتریان - مدیریت (HTS 48/69)'),
        (500025, N'Sale.AfterSales.CustomerAllowedGuarantee.View', N'گارانتی مجاز مشتریان - مشاهده (HTS 300/350)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #CagRoles;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RName)
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Id = @Rid)
            BEGIN
                SET IDENTITY_INSERT system.Role ON;
                INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@Rid, @RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                SET IDENTITY_INSERT system.Role OFF;
                PRINT N'  CREATED ' + @RName;
            END
            ELSE
            BEGIN
                INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                PRINT N'  CREATED (new id) ' + @RName;
            END
        END
        ELSE PRINT N'  EXISTS ' + @RName;
        FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    END
    CLOSE rc; DEALLOCATE rc;

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.AfterSales')
        THROW 51021, N'نقش Sale.AfterSales یافت نشد.', 1;

    PRINT N'=== [2] RoleAccess ===';
    IF OBJECT_ID('tempdb..#CagActionMap') IS NOT NULL DROP TABLE #CagActionMap;
    CREATE TABLE #CagActionMap
    (
        RoleName NVARCHAR(200) NOT NULL,
        Path NVARCHAR(300) NOT NULL,
        ActionAccessType INT NOT NULL,
        ActionAccessItemType INT NOT NULL
    );

    INSERT INTO #CagActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sale/customerallowedguarantee/list', 1, 1),
        (N'/panel/sale/customerallowedguarantee/fetchdata', 2, 2)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'), (N'Sale.AfterSales'),
        (N'Sale.AfterSales.CustomerAllowedGuarantee.View'),
        (N'Sale.AfterSales.CustomerAllowedGuarantee.Manage')
    ) r(RoleName);

    INSERT INTO #CagActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sale/customerallowedguarantee/save', 2, 3),
        (N'/panel/sale/customerallowedguarantee/add', 2, 4),
        (N'/panel/sale/customerallowedguarantee/update', 2, 5),
        (N'/panel/sale/customerallowedguarantee/delete', 2, 6),
        (N'/panel/sale/customerallowedguarantee/edit', 1, 5),
        (N'/panel/sale/customerallowedguarantee/new', 1, 4),
        (N'/panel/sale/customerallowedguarantee/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'), (N'Sale.AfterSales'),
        (N'Sale.AfterSales.CustomerAllowedGuarantee.Manage')
    ) r(RoleName);

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.Path LIKE N'/panel/sale/customerallowedguarantee/%'
      AND r.Name IN (
            N'Sale.AfterSales.CustomerAllowedGuarantee.Manage',
            N'Sale.AfterSales.CustomerAllowedGuarantee.View',
            N'Sale.AfterSales', N'ShowAllMenus'
      );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType, @EntityPascal, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #CagActionMap a
    INNER JOIN system.Role r ON r.Name = a.RoleName;
    PRINT N'  Controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [3] Menu AccessRoleIds merge ===';
    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'AfterSalesServiceSystem');
    IF @MenuId IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT LTRIM(RTRIM(j.value))
        FROM system.SystemMenu m
        CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND LTRIM(RTRIM(j.value)) <> N'';

        INSERT INTO #MenuRoles (RoleName)
        SELECT v.RoleName
        FROM (VALUES
            (N'Sale.AfterSales.CustomerAllowedGuarantee.Manage'),
            (N'Sale.AfterSales.CustomerAllowedGuarantee.View')
        ) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles x WHERE x.RoleName = v.RoleName);

        DECLARE @AccessRoles NVARCHAR(MAX);
        DECLARE @AccessRoleIds NVARCHAR(MAX);
        SELECT @AccessRoles = N'[' + STUFF((
            SELECT N',"' + RoleName + N'"' FROM #MenuRoles ORDER BY RoleName
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        SELECT @AccessRoleIds = N'[' + STUFF((
            SELECT N',' + CAST(r.Id AS nvarchar(20))
            FROM #MenuRoles m INNER JOIN system.Role r ON r.Name = m.RoleName
            ORDER BY r.Id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

        UPDATE system.SystemMenu
        SET AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
        PRINT N'  Menu AccessRoleIds merged';
    END
    ELSE PRINT N'  WARN menu AfterSalesServiceSystem not found';

    PRINT N'=== [4] Assign HTS page 206 users ===';
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleName NVARCHAR(200) NOT NULL,
        Source   NVARCHAR(80)  NOT NULL
    );

    IF OBJECT_ID('tempdb..#HtsGrant') IS NOT NULL DROP TABLE #HtsGrant;
    CREATE TABLE #HtsGrant
    (
        Permission_ID  INT            NOT NULL,
        User_FK        INT            NOT NULL,
        AdName         NVARCHAR(200)  NULL,
        HtsUsername    NVARCHAR(200)  NULL,
        Source         NVARCHAR(80)   NOT NULL
    );

    INSERT INTO #HtsGrant (Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT DISTINCT
        pa.Permission_FK,
        u.User_ID,
        NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
        NULLIF(LTRIM(RTRIM(u.Username)), N''),
        N'group:' + CAST(pug.UserGroup_FK AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    WHERE pa.Page_FK = 206;

    INSERT INTO #HtsGrant (Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT
        pa.Permission_FK,
        u.User_ID,
        NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
        NULLIF(LTRIM(RTRIM(u.Username)), N''),
        N'direct'
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pau.PageAction_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pau.User_FK
    WHERE pa.Page_FK = 206
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant x
          WHERE x.Permission_ID = pa.Permission_FK AND x.User_FK = u.User_ID AND x.Source = N'direct'
      );

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT x.Username, x.RoleName, x.Source
    FROM (
        SELECT COALESCE(g.AdName, g.HtsUsername) AS Username,
               N'Sale.AfterSales.CustomerAllowedGuarantee.Manage' AS RoleName, g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID = 2 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername),
               N'Sale.AfterSales.CustomerAllowedGuarantee.View', g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID = 3 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL
    ) x
    WHERE x.Username IS NOT NULL;

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT hu.Username, m.RoleName, m.Source + N'+htsUser'
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu
        ON LOWER(hu.ActiveDirectoryUsername) = LOWER(m.Username)
    WHERE hu.Username IS NOT NULL
      AND LOWER(hu.Username) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(hu.Username) AND x.RoleName = m.RoleName
      );

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT hu.ActiveDirectoryUsername, m.RoleName, m.Source + N'+htsAd'
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu
        ON LOWER(hu.Username) = LOWER(m.Username)
    WHERE hu.ActiveDirectoryUsername IS NOT NULL
      AND LOWER(hu.ActiveDirectoryUsername) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(hu.ActiveDirectoryUsername) AND x.RoleName = m.RoleName
      );

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT a.HavayarName, src.RoleName, N'alias'
    FROM (VALUES
        (N'Ahadpour', N'Ahadpour.m'),
        (N'drghoroori', N'dr.h-ghoroori')
    ) a(HtsName, HavayarName)
    INNER JOIN #UserRoleMap src ON LOWER(src.Username) = LOWER(a.HtsName)
    WHERE NOT EXISTS (
        SELECT 1 FROM #UserRoleMap x
        WHERE LOWER(x.Username) = LOWER(a.HavayarName) AND x.RoleName = src.RoleName
    );

    ;WITH d AS (
        SELECT Username, RoleName, ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleName ORDER BY Username) AS rn
        FROM #UserRoleMap
    )
    DELETE FROM d WHERE rn > 1;

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0;

    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Username, RoleName FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName;
        SELECT TOP 1 @MapUserId = Id
        FROM system.[User] u
        WHERE LOWER(u.Username) = LOWER(@MapUsername)
           OR LOWER(REPLACE(ISNULL(u.Email, N''), N'@havayar.com', N'')) = LOWER(@MapUsername)
        ORDER BY CASE WHEN LOWER(u.Username) = LOWER(@MapUsername) THEN 0 ELSE 1 END;

        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF @MapRoleId IS NULL
            PRINT N'  MISSING ROLE (skip): ' + @MapRoleName;
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
        )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
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
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END

        SET @MapUserId = NULL;
        SET @MapRoleId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    PRINT N'  User role assignments applied: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already had role (skipped): ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing users (logged): ' + CAST(@Missing AS nvarchar(20));

    DECLARE @ViewRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales.CustomerAllowedGuarantee.View');
    DECLARE @ManageRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales.CustomerAllowedGuarantee.Manage');
    DECLARE @AfterSalesRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales');
    DECLARE @ViewStripped int = 0;

    IF @ViewRoleId IS NOT NULL AND @ManageRoleId IS NOT NULL AND @AfterSalesRoleId IS NOT NULL
    BEGIN
        DECLARE @StripUserId bigint, @StripRoleIds nvarchar(max), @StripRoles nvarchar(max);
        DECLARE strip_cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT u.Id, u.RoleIds, u.Roles
            FROM system.[User] u
            WHERE EXISTS (
                SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) = @ViewRoleId
            )
            AND EXISTS (
                SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) IN (@ManageRoleId, @AfterSalesRoleId)
            );
        OPEN strip_cur;
        FETCH NEXT FROM strip_cur INTO @StripUserId, @StripRoleIds, @StripRoles;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @StripRoleIds = N'[' + STUFF((
                SELECT N',' + j.value
                FROM OPENJSON(ISNULL(@StripRoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) <> @ViewRoleId
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
            IF @StripRoleIds IS NULL OR @StripRoleIds = N'[]'
                SET @StripRoleIds = N'[]';

            SELECT @StripRoles = N'[' + STUFF((
                SELECT N',"' + REPLACE(j.value, N'"', N'\"') + N'"'
                FROM OPENJSON(ISNULL(@StripRoles, N'[]')) j
                WHERE j.value <> N'Sale.AfterSales.CustomerAllowedGuarantee.View'
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
            IF @StripRoles IS NULL
                SET @StripRoles = N'[]';

            UPDATE system.[User]
            SET RoleIds = @StripRoleIds,
                Roles = @StripRoles,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @StripUserId;

            SET @ViewStripped = @ViewStripped + 1;
            FETCH NEXT FROM strip_cur INTO @StripUserId, @StripRoleIds, @StripRoles;
        END
        CLOSE strip_cur;
        DEALLOCATE strip_cur;
    END
    PRINT N'  Stripped View from Manage/AfterSales users: ' + CAST(@ViewStripped AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_AfterSales_CustomerAllowedGuarantee_RolesAndUsers ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT r.Id, r.Name, r.Title,
       (SELECT COUNT(*) FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id AND ra.Path LIKE N'/panel/sale/customerallowedguarantee/%') AS CagAccess,
       (SELECT COUNT(*) FROM system.[User] u
        CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
        WHERE TRY_CAST(j.value AS bigint) = r.Id) AS Users
FROM system.Role r
WHERE r.Name IN (
    N'Sale.AfterSales',
    N'Sale.AfterSales.CustomerAllowedGuarantee.Manage',
    N'Sale.AfterSales.CustomerAllowedGuarantee.View',
    N'ShowAllMenus'
)
ORDER BY r.Id;
"""

fix_sql = r"""/*
  Fix_CustomerAllowedGuarantee_CustomerId.sql
  Phase4 initially joined only SLS.Customer.HtsId = HTS Customer_FK.
  After Customer.HtsId widen, rematch; leftover rows use unique Customer.Code = Crm_Customer.Customer_Code.
  Does not invent customers or overwrite Customer.HtsId.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'fix-customerallowedguarantee-customerid';

    ;WITH UniqueCustomerCode AS (
        SELECT Code
        FROM SLS.Customer
        WHERE Code IS NOT NULL
        GROUP BY Code
        HAVING COUNT(*) = 1
    ),
    Map AS (
        SELECT
            g.Id,
            COALESCE(cHts.Id, cCode.Id) AS CustomerId
        FROM Sale.CustomerAllowedGuarantee g
        INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Customer_AllowedGuarantee] s
            ON s.Customer_AllowedGuarantee_ID = g.HtsId
        LEFT JOIN SLS.Customer cHts ON cHts.HtsId = s.Customer_FK AND cHts.HtsId <> 0
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] hc ON hc.Customer_ID = s.Customer_FK
        LEFT JOIN UniqueCustomerCode uc ON uc.Code = hc.Customer_Code
        LEFT JOIN SLS.Customer cCode ON cCode.Code = uc.Code
    )
    UPDATE g
    SET g.CustomerId = m.CustomerId,
        g.ModifiedById = 1,
        g.ModifiedByName = @SeedUser,
        g.ModifiedDateMiladiDateTime = @Now,
        g.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.CustomerAllowedGuarantee g
    INNER JOIN Map m ON m.Id = g.Id
    WHERE m.CustomerId IS NOT NULL
      AND (g.CustomerId IS NULL OR g.CustomerId <> m.CustomerId);
    PRINT N'  Remapped CustomerId rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Fix_CustomerAllowedGuarantee_CustomerId ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN CustomerId IS NULL THEN 1 ELSE 0 END) AS Unmapped,
    SUM(CASE WHEN CustomerId IS NOT NULL THEN 1 ELSE 0 END) AS Mapped
FROM Sale.CustomerAllowedGuarantee;
"""

(ROOT / "Seed_AfterSales_CustomerAllowedGuarantee_DataProfiles.sql").write_text(profile_sql, encoding="utf-8-sig")
(ROOT / "Seed_AfterSales_CustomerAllowedGuarantee_RolesAndUsers.sql").write_text(roles_sql, encoding="utf-8-sig")
(ROOT / "Fix_CustomerAllowedGuarantee_CustomerId.sql").write_text(fix_sql, encoding="utf-8-sig")
print("wrote 3 sql files")
