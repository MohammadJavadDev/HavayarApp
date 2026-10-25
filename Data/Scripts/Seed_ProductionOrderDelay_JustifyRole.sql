/*
Seed_ProductionOrderDelay_JustifyRole.sql
نقش توجیه تاخیر سفارش ساخت.
دسترسی صفحه: لیست موجود تاخیرات و ذخیره توجیه.
نمایه داده و دکمه مودال در Seed_ProductionOrderDelay_Justify_DataProfile.sql است.
Encoding: UTF-8 with BOM
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-productionorderdelay-justify';
DECLARE @RoleName NVARCHAR(200) = N'Pln.ProductionOrderDelay.Justify';
DECLARE @RoleTitle NVARCHAR(200) = N'توجیه تاخیر سفارش ساخت';

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RoleName)
BEGIN
    INSERT INTO system.Role
        (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
         CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (@RoleName, @RoleTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
END

DECLARE @RoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = @RoleName);

IF OBJECT_ID(N'tempdb..#Map') IS NOT NULL DROP TABLE #Map;
CREATE TABLE #Map (RoleName NVARCHAR(200) NOT NULL, Path NVARCHAR(300) NOT NULL, AType INT NOT NULL, IType INT NOT NULL);

INSERT INTO #Map (RoleName, Path, AType, IType)
SELECT r.RoleName, a.Path, a.AType, a.IType
FROM (VALUES
    (N'/panel/productionorderdelay/list', 1, 1),
    (N'/panel/productionorderdelay/savejustification', 2, 3)
) a(Path, AType, IType)
CROSS JOIN (VALUES
    (@RoleName)
) r(RoleName);

DELETE ra
FROM system.RoleAccess ra
INNER JOIN system.Role role ON role.Id = ra.RoleId
WHERE ra.Path IN (
        N'/panel/productionorderdelay/cartable',
        N'/panel/productionorderdelay/justify',
        N'/panel/productionorderdelay/list',
        N'/panel/productionorderdelay/savejustification'
    )
  AND role.Name = @RoleName;

INSERT INTO system.RoleAccess
    (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT DISTINCT m.Path, m.AType, m.IType,
    N'Entities.App.Pln.ProductionOrderDelay',
    NULL, NULL, NULL, role.Id,
    1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM #Map m
INNER JOIN system.Role role ON role.Name = m.RoleName;

DECLARE @Leaf NVARCHAR(400) = N',{"text":"توجیه تاخیر سفارش ساخت","icon":"ki-message-text ki-outline","iconColor":"#F9A8D4","path":"/panel/productionorderdelay/cartable","a_attr":{"href":"/panel/productionorderdelay/cartable"},"data":{"iconColor":"#F9A8D4","path":"/panel/productionorderdelay/cartable"},"children":[]}';

UPDATE system.SystemMenu
SET Content = REPLACE(Content, @Leaf, N''),
    ModifiedById = 1,
    ModifiedByName = @SeedUser,
    ModifiedDateMiladiDateTime = @Now,
    ModifiedDateShamsiDateTime = @NowShamsi
WHERE Name IN (N'AllMenus', N'Industries')
  AND CHARINDEX(@Leaf, Content) > 0;

DELETE ra
FROM system.RoleAccess ra
INNER JOIN system.Role role ON role.Id = ra.RoleId
WHERE ra.Path IN (N'/panel/productionorderdelay/cartable', N'/panel/productionorderdelay/justify')
  AND role.Name IN (@RoleName, N'ShowAllMenus');

DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'AllMenus');
IF @MenuId IS NOT NULL AND @RoleId IS NOT NULL
BEGIN
    IF OBJECT_ID(N'tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
    CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
    INSERT INTO #MenuRoles (RoleName)
    SELECT DISTINCT LTRIM(RTRIM(j.value))
    FROM system.SystemMenu m
    CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
    WHERE m.Id = @MenuId AND LTRIM(RTRIM(j.value)) <> N'';

    IF NOT EXISTS (SELECT 1 FROM #MenuRoles WHERE RoleName = @RoleName)
        INSERT INTO #MenuRoles (RoleName) VALUES (@RoleName);

    DECLARE @AccessRoles NVARCHAR(MAX);
    DECLARE @AccessRoleIds NVARCHAR(MAX);
    SELECT @AccessRoles = N'[' + STUFF((
        SELECT N',"' + STRING_ESCAPE(RoleName, 'json') + N'"'
        FROM #MenuRoles
        ORDER BY RoleName
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
    SELECT @AccessRoleIds = N'[' + STUFF((
        SELECT N',' + CAST(r.Id AS nvarchar(20))
        FROM #MenuRoles m
        INNER JOIN system.Role r ON r.Name = m.RoleName
        ORDER BY r.Id
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

    UPDATE system.SystemMenu
    SET AccessRoles = @AccessRoles,
        AccessRoleIds = @AccessRoleIds,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = @MenuId;
END

COMMIT TRANSACTION;

SELECT r.Name, ra.Path
FROM system.Role r
INNER JOIN system.RoleAccess ra ON ra.RoleId = r.Id
WHERE r.Name = @RoleName
ORDER BY ra.Path;
