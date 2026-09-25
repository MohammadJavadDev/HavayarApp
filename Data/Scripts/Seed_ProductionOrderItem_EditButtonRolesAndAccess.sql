/*
================================================================================
Seed_ProductionOrderItem_EditButtonRolesAndAccess.sql
================================================================================
نقش‌ها و دسترسی دکمه‌های Edit اقلام سفارش ساخت (معادل HTS صفحه 214):

  1) Sale.ProductionOrderItem.Production  — HTS ProductionPermission / گروه 361
  2) Sale.ProductionOrderItem.Qc          — HTS QcPermission / گروه 362
  3) Sale.ProductionOrderItem.SellTeam    — HTS گروه 596 (تغییر وضعیت روی ۳۱۶۷)

  RoleAccess:
  - SaveComment برای نقش‌های تغییر وضعیت
  - DoObsolete برای Sale.ProductionOrder.Obsolete

اعضای Item 3 (AcceptProjectManager / Inquirer / Ministry / Obsolete / ViewActive / History)
در این اسکریپت پر نمی‌شوند — در HTS عضو ندارند یا نگاشت یکتا ندارند.

Idempotent. UTF-8 with BOM required. Apply:
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i thisfile.sql
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-edit-buttons';
DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItem';
DECLARE @PoEntity NVARCHAR(200) = N'Entities.App.Sale.ProductionOrder';

DECLARE @ProdRoleId BIGINT = 200068;
DECLARE @QcRoleId BIGINT = 200069;
DECLARE @SellRoleId BIGINT = 200070;
DECLARE @ProdRoleName NVARCHAR(200) = N'Sale.ProductionOrderItem.Production';
DECLARE @QcRoleName NVARCHAR(200) = N'Sale.ProductionOrderItem.Qc';
DECLARE @SellRoleName NVARCHAR(200) = N'Sale.ProductionOrderItem.SellTeam';
DECLARE @ObsoleteRoleName NVARCHAR(200) = N'Sale.ProductionOrder.Obsolete';

BEGIN TRY
    BEGIN TRANSACTION;

    -----------------------------------------------------------------------------
    -- 1) Roles
    -----------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @ProdRoleName)
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @ProdRoleId)
            SELECT @ProdRoleId = ISNULL(MAX(Id), 200000) + 1 FROM system.Role WHERE Id BETWEEN 200000 AND 299999;
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, IsActive, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
        VALUES (@ProdRoleId, @ProdRoleName, N'فروش - اقلام سفارش ساخت - تولید (تغییر وضعیت)', 1, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  Created role: ' + @ProdRoleName;
    END
    ELSE
        SELECT @ProdRoleId = Id FROM system.Role WHERE Name = @ProdRoleName;

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @QcRoleName)
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @QcRoleId)
            SELECT @QcRoleId = ISNULL(MAX(Id), 200000) + 1 FROM system.Role WHERE Id BETWEEN 200000 AND 299999;
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, IsActive, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
        VALUES (@QcRoleId, @QcRoleName, N'فروش - اقلام سفارش ساخت - کیفیت (تغییر وضعیت)', 1, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  Created role: ' + @QcRoleName;
    END
    ELSE
        SELECT @QcRoleId = Id FROM system.Role WHERE Name = @QcRoleName;

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @SellRoleName)
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @SellRoleId)
            SELECT @SellRoleId = ISNULL(MAX(Id), 200000) + 1 FROM system.Role WHERE Id BETWEEN 200000 AND 299999;
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, IsActive, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
        VALUES (@SellRoleId, @SellRoleName, N'فروش - اقلام سفارش ساخت - تیم فروش (توقف بازدید کارفرما)', 1, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  Created role: ' + @SellRoleName;
    END
    ELSE
        SELECT @SellRoleId = Id FROM system.Role WHERE Name = @SellRoleName;

    -----------------------------------------------------------------------------
    -- 2) RoleAccess: SaveComment + DoObsolete
    -----------------------------------------------------------------------------
    DECLARE @AccessRows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, RoleName NVARCHAR(200), EntityName NVARCHAR(200));
    INSERT INTO @AccessRows (Path, AccessType, ItemType, RoleName, EntityName) VALUES
        (N'/panel/productionorderitem/savecomment', 2, 0, @ProdRoleName, @Entity),
        (N'/panel/productionorderitem/savecomment', 2, 0, @QcRoleName, @Entity),
        (N'/panel/productionorderitem/savecomment', 2, 0, @SellRoleName, @Entity),
        (N'/panel/productionorderitem/savecomment', 2, 0, N'Sale.ProductionOrderItem.SupplyCommitteeBoss', @Entity),
        (N'/panel/productionorderitem/savecomment', 2, 0, N'Sale.ProductionOrderItem.Inquirer', @Entity),
        (N'/panel/productionorderitem/savecomment', 2, 0, N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer', @Entity),
        (N'/panel/productionorder/doobsolete/{id}', 2, 1000, @ObsoleteRoleName, @PoEntity);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT x.Path, x.AccessType, x.ItemType, x.EntityName, N'', NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @AccessRows x
    INNER JOIN system.Role r ON r.Name = x.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType);
    PRINT N'  Inserted RoleAccess: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    -- 3) Assign users from HTS groups 361 / 362 / 596 (unique username match only)
    -----------------------------------------------------------------------------
    DECLARE @ProdUsers TABLE (Username NVARCHAR(200));
    INSERT INTO @ProdUsers (Username) VALUES
        (N'Azami.sa'), (N'Eftekhari.a'), (N'estaki.a'), (N'golestaneh.a'), (N'karami.h'),
        (N'Khoshdel.f'), (N'Momenirad.s'), (N'pt'), (N'Sadeghpour.j'), (N'Sardashti.s'), (N'sohrabi.z');

    DECLARE @QcUsers TABLE (Username NVARCHAR(200));
    INSERT INTO @QcUsers (Username) VALUES
        (N'Azami.sa'), (N'badrkhani.s'), (N'Bahmandar.mo'), (N'gavan.a'),
        (N'qci'), (N'Sadeghpour.j'), (N'salim.sh'), (N'Sardashti.s');

    DECLARE @SellUsers TABLE (Username NVARCHAR(200));
    INSERT INTO @SellUsers (Username) VALUES
        (N'Abbasi.a'), (N'Aghighi.p'), (N'Ahadi.s'), (N'Ahmadvand.z'), (N'Ahrarnejad.h'),
        (N'Alizadeh.p'), (N'Azari.sh'), (N'Bosaghzadeh.e'), (N'erfani.k'), (N'esfahani.b'),
        (N'esmaillian.r'), (N'Farahmand.m'), (N'Fekri.f'), (N'givehchi.h'), (N'habibi.m'),
        (N'habibollah.m'), (N'karimi.ma'), (N'Kazemzadeh.h'), (N'khalaj.m'), (N'Khorshidi.m'),
        (N'Khoshdel.f'), (N'kiazadeh.s'), (N'MalekMohammadi.m'), (N'maranaki.m'), (N'masoudi.l'),
        (N'mohammadi.r'), (N'mohammadi.s'), (N'mohammadian.b'), (N'Momenirad.s'), (N'nezafati.a'),
        (N'Nikoosefat.m'), (N'noroozi.s'), (N'Rajabi.m'), (N'Rajablou.a'), (N'Ramezani.sh'),
        (N'Rostampoor.a'), (N'Samarzadeh.r'), (N'sharifi.mo'), (N'Shomali.m'), (N'taheran.f'),
        (N'taheran.fa'), (N'Tahmasebizadeh.sh'), (N'toosi.f'), (N'yaghyaei.m'), (N'yaltaghian.f');

    DECLARE @AssignRoleId BIGINT, @AssignRoleName NVARCHAR(200), @Uname NVARCHAR(200), @Uid BIGINT;
    DECLARE @Assigned INT = 0, @Ambiguous INT = 0, @Missing INT = 0, @Skipped INT = 0;
    DECLARE @MatchCount INT;

    DECLARE assign_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT @ProdRoleId, @ProdRoleName, Username FROM @ProdUsers
        UNION ALL
        SELECT @QcRoleId, @QcRoleName, Username FROM @QcUsers
        UNION ALL
        SELECT @SellRoleId, @SellRoleName, Username FROM @SellUsers;

    OPEN assign_cursor;
    FETCH NEXT FROM assign_cursor INTO @AssignRoleId, @AssignRoleName, @Uname;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MatchCount = (SELECT COUNT(*) FROM system.[User] u WHERE u.IsActive = 1 AND LOWER(u.Username) = LOWER(@Uname));
        IF @MatchCount = 0
        BEGIN
            SET @Missing = @Missing + 1;
            PRINT N'  MISSING user for ' + @AssignRoleName + N': ' + @Uname;
        END
        ELSE IF @MatchCount > 1
        BEGIN
            SET @Ambiguous = @Ambiguous + 1;
            PRINT N'  AMBIGUOUS user for ' + @AssignRoleName + N': ' + @Uname;
        END
        ELSE
        BEGIN
            SELECT @Uid = u.Id FROM system.[User] u WHERE u.IsActive = 1 AND LOWER(u.Username) = LOWER(@Uname);
            IF EXISTS (
                SELECT 1 FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE u.Id = @Uid AND TRY_CAST(j.[value] AS BIGINT) = @AssignRoleId)
            BEGIN
                SET @Skipped = @Skipped + 1;
            END
            ELSE
            BEGIN
                UPDATE system.[User]
                SET RoleIds = CASE
                        WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                            THEN N'[' + CAST(@AssignRoleId AS NVARCHAR(20)) + N']'
                        ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@AssignRoleId AS NVARCHAR(20)) + N']')
                    END,
                    Roles = CASE
                        WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.[value] = @AssignRoleName) THEN Roles
                        WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                            THEN N'["' + @AssignRoleName + N'"]'
                        ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @AssignRoleName + N'"]')
                    END,
                    ModifiedById = 1, ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Id = @Uid;
                SET @Assigned = @Assigned + 1;
                PRINT N'  +' + @AssignRoleName + N' → ' + @Uname;
            END
        END
        FETCH NEXT FROM assign_cursor INTO @AssignRoleId, @AssignRoleName, @Uname;
    END
    CLOSE assign_cursor;
    DEALLOCATE assign_cursor;

    PRINT N'  Assigned=' + CAST(@Assigned AS NVARCHAR(20))
        + N' skipped=' + CAST(@Skipped AS NVARCHAR(20))
        + N' missing=' + CAST(@Missing AS NVARCHAR(20))
        + N' ambiguous=' + CAST(@Ambiguous AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'DONE Seed_ProductionOrderItem_EditButtonRolesAndAccess';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
