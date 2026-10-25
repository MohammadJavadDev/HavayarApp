/*
  Seed_ProductionOrder_SalesRole.sql
  نقش کارشناسان فروش سفارش ساخت، معادل گروه ۷۷ سیستم قدیم.
  RoleAccess: list / fetchdata / new / edit / add / update / save
  کاربران فعال Gnr_UserGroupMember.UserGroup_FK = 77
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @RoleName nvarchar(200) = N'Sale.ProductionOrder.Sales';
DECLARE @RoleTitle nvarchar(300) = N'فروش - سفارش ساخت - کارشناسان فروش';
DECLARE @RoleId bigint;

SELECT @RoleId = Id FROM system.Role WHERE Name = @RoleName;

IF @RoleId IS NULL
BEGIN
    SELECT @RoleId = ISNULL(MAX(Id), 200000) + 1
    FROM system.Role
    WHERE Id >= 200000 AND Id < 300000;

    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role
    (
        Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive
    )
    VALUES
    (
        @RoleId, @RoleName, @RoleTitle, 1, N'seed-po-sales', 1, N'seed-po-sales',
        GETDATE(), GETDATE(), 1
    );
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role ' + @RoleName + N' Id=' + CAST(@RoleId AS nvarchar(20));
END
ELSE
    PRINT N'  EXISTS Role ' + @RoleName + N' Id=' + CAST(@RoleId AS nvarchar(20));

DELETE FROM system.RoleAccess
WHERE RoleId = @RoleId
  AND Path IN (
      N'/panel/productionorder/list',
      N'/panel/productionorder/fetchdata',
      N'/panel/productionorder/new',
      N'/panel/productionorder/edit',
      N'/panel/productionorder/add',
      N'/panel/productionorder/update',
      N'/panel/productionorder/save'
  );

INSERT INTO system.RoleAccess
(
    Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
    EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive
)
VALUES
    (N'/panel/productionorder/list', 1, 1, N'Entities.App.Sale.ProductionOrder', N'لیست اطلاعات', NULL, NULL, @RoleId, 1, 1, N'seed-po-sales', N'seed-po-sales', GETDATE(), GETDATE(), 1),
    (N'/panel/productionorder/fetchdata', 2, 2, N'Entities.App.Sale.ProductionOrder', N'دریافت اطلاعات', NULL, NULL, @RoleId, 1, 1, N'seed-po-sales', N'seed-po-sales', GETDATE(), GETDATE(), 1),
    (N'/panel/productionorder/new', 1, 4, N'Entities.App.Sale.ProductionOrder', N'درج اطلاعات', NULL, NULL, @RoleId, 1, 1, N'seed-po-sales', N'seed-po-sales', GETDATE(), GETDATE(), 1),
    (N'/panel/productionorder/edit', 1, 5, N'Entities.App.Sale.ProductionOrder', N'ویرایش اطلاعات', NULL, NULL, @RoleId, 1, 1, N'seed-po-sales', N'seed-po-sales', GETDATE(), GETDATE(), 1),
    (N'/panel/productionorder/add', 2, 4, N'Entities.App.Sale.ProductionOrder', N'درج', NULL, NULL, @RoleId, 1, 1, N'seed-po-sales', N'seed-po-sales', GETDATE(), GETDATE(), 1),
    (N'/panel/productionorder/update', 2, 5, N'Entities.App.Sale.ProductionOrder', N'ویرایش', NULL, NULL, @RoleId, 1, 1, N'seed-po-sales', N'seed-po-sales', GETDATE(), GETDATE(), 1),
    (N'/panel/productionorder/save', 2, 3, N'Entities.App.Sale.ProductionOrder', N'ذخیره', NULL, NULL, @RoleId, 1, 1, N'seed-po-sales', N'seed-po-sales', GETDATE(), GETDATE(), 1);

PRINT N'  RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
CREATE TABLE #UserRoleMap (Username nvarchar(200) NOT NULL);
INSERT INTO #UserRoleMap (Username) VALUES
    (N'Abbasi.a'), (N'Aghighi.p'), (N'Ahadi.s'), (N'Ahmadvand.z'), (N'Ahrarnejad.h'),
    (N'Alizadeh.p'), (N'Azari.sh'), (N'Bosaghzadeh.e'), (N'erfani.k'), (N'esfahani.b'),
    (N'esmaillian.r'), (N'Farahmand.m'), (N'Fekri.f'), (N'givehchi.h'), (N'habibi.m'),
    (N'habibollah.m'), (N'karimi.ma'), (N'Kazemzadeh.h'), (N'khalaj.m'), (N'Khorshidi.m'),
    (N'Khoshdel.f'), (N'kiazadeh.s'), (N'MalekMohammadi.m'), (N'maranaki.m'), (N'masoudi.l'),
    (N'mohammadi.r'), (N'mohammadi.s'), (N'mohammadian.b'), (N'Momenirad.s'), (N'nezafati.a'),
    (N'Nikoosefat.m'), (N'noroozi.s'), (N'Rajabi.m'), (N'Rajablou.a'), (N'Ramezani.sh'),
    (N'Rostampoor.a'), (N'Samarzadeh.r'), (N'sharifi.mo'), (N'Shomali.m'), (N'taheran.f'),
    (N'taheran.fa'), (N'Tahmasebizadeh.sh'), (N'toosi.f'), (N'yaghyaei.m'), (N'yaltaghian.f');

DECLARE @MapUsername nvarchar(200), @MapUserId bigint;
DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0;

DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT Username FROM #UserRoleMap;
OPEN map_cur;
FETCH NEXT FROM map_cur INTO @MapUsername;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT TOP 1 @MapUserId = Id FROM system.[User] WHERE LOWER(Username) = LOWER(@MapUsername);

    IF @MapUserId IS NULL
    BEGIN
        PRINT N'  MISSING USER (skip): ' + @MapUsername;
        SET @Missing = @Missing + 1;
    END
    ELSE IF EXISTS (
        SELECT 1 FROM system.[User]
        WHERE Id = @MapUserId
          AND (ISJSON(RoleIds) <> 1 OR ISJSON(Roles) <> 1)
          AND NULLIF(LTRIM(RTRIM(RoleIds)), N'') IS NOT NULL
          AND RoleIds <> N'[]'
    )
    BEGIN
        PRINT N'  INVALID ROLE JSON (skip): ' + @MapUsername;
        SET @Missing = @Missing + 1;
    END
    ELSE IF ISJSON((SELECT RoleIds FROM system.[User] WHERE Id = @MapUserId)) = 1
        AND EXISTS (
        SELECT 1 FROM system.[User] u
        CROSS APPLY OPENJSON(u.RoleIds) j
        WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @RoleId
    )
        SET @SkippedAlready = @SkippedAlready + 1;
    ELSE
    BEGIN
        UPDATE system.[User]
        SET
            RoleIds = CASE
                WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                    THEN N'[' + CAST(@RoleId AS nvarchar(20)) + N']'
                ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS nvarchar(20)) + N']')
            END,
            Roles = CASE
                WHEN ISJSON(Roles) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(Roles) j WHERE j.value = @RoleName) THEN Roles
                WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                    THEN N'["' + @RoleName + N'"]'
                ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @RoleName + N'"]')
            END,
            ModifiedById = 1,
            ModifiedByName = N'seed-po-sales',
            ModifiedDateMiladiDateTime = GETDATE()
        WHERE Id = @MapUserId;
        SET @Assigned = @Assigned + 1;
    END

    SET @MapUserId = NULL;
    FETCH NEXT FROM map_cur INTO @MapUsername;
END
CLOSE map_cur;
DEALLOCATE map_cur;

PRINT N'  User role assignments applied: ' + CAST(@Assigned AS nvarchar(20));
PRINT N'  Already had role (skipped): ' + CAST(@SkippedAlready AS nvarchar(20));
PRINT N'  Missing users (logged): ' + CAST(@Missing AS nvarchar(20));

COMMIT TRANSACTION;
