/*
================================================================================
Seed_ProductionOrderItemBom_RolesAndAccess.sql
================================================================================
نقش‌ها و دسترسی BOM اقلام سفارش ساخت (معادل صفحه HTS 508 / Pln_ProductionOrderItemBom
با عنوان «Bom اقلام سفارش ساخت»):

  1) Sale.ProductionOrderItemBom  / Title: Bom اقلام سفارش ساخت
     — باز/ذخیره/حذف/اکسل BOM (فقط این نقش یا ادمین)
     — اعضا: گروه HTS 451 + کاربران مستقیم New/Edit/Delete صفحه 508

  2) تکمیل اطلاعات bom اقلام سفارش ساخت صنایع (Name=Title)
     — فقط فیلدهای جدید BOM: ProductionStep, NumberSupplied, Status
     — اعضا: سپیده آزاد بخش، علی اکبر خلیلی، رسول کیان

حذف مسیرهای BOM از Industries / AcceptIndustrial تا فقط نقش جدید (و ادمین) بتواند BOM کند.

Idempotent. UTF-8 with BOM required. Apply:
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i thisfile.sql
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-bom-roles';
DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItem';

DECLARE @BomRoleId BIGINT = 200066;
DECLARE @BomRoleName NVARCHAR(200) = N'Sale.ProductionOrderItemBom';
DECLARE @BomRoleTitle NVARCHAR(200) = N'Bom اقلام سفارش ساخت';

DECLARE @NewFieldsRoleId BIGINT = 200067;
DECLARE @NewFieldsRoleName NVARCHAR(200) = N'تکمیل اطلاعات bom اقلام سفارش ساخت صنایع';
DECLARE @NewFieldsRoleTitle NVARCHAR(200) = N'تکمیل اطلاعات bom اقلام سفارش ساخت صنایع';

BEGIN TRY
    BEGIN TRANSACTION;

    -----------------------------------------------------------------------------
    -- 1) Roles
    -----------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @BomRoleName)
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @BomRoleId)
            SELECT @BomRoleId = ISNULL(MAX(Id), 200000) + 1 FROM system.Role WHERE Id BETWEEN 200000 AND 299999;

        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role
            (Id, Name, Title, IsActive,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
             ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
        VALUES
            (@BomRoleId, @BomRoleName, @BomRoleTitle, 1,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  Created role: ' + @BomRoleName + N' id=' + CAST(@BomRoleId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        SELECT @BomRoleId = Id FROM system.Role WHERE Name = @BomRoleName;
        UPDATE system.Role
        SET Title = @BomRoleTitle, IsActive = 1,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @BomRoleId;
        PRINT N'  Reused role: ' + @BomRoleName + N' id=' + CAST(@BomRoleId AS NVARCHAR(20));
    END

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @NewFieldsRoleName)
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @NewFieldsRoleId)
            SELECT @NewFieldsRoleId = ISNULL(MAX(Id), 200000) + 1 FROM system.Role WHERE Id BETWEEN 200000 AND 299999;

        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role
            (Id, Name, Title, IsActive,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
             ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
        VALUES
            (@NewFieldsRoleId, @NewFieldsRoleName, @NewFieldsRoleTitle, 1,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  Created role: ' + @NewFieldsRoleName + N' id=' + CAST(@NewFieldsRoleId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        SELECT @NewFieldsRoleId = Id FROM system.Role WHERE Name = @NewFieldsRoleName;
        UPDATE system.Role
        SET Title = @NewFieldsRoleTitle, IsActive = 1,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @NewFieldsRoleId;
        PRINT N'  Reused role: ' + @NewFieldsRoleName + N' id=' + CAST(@NewFieldsRoleId AS NVARCHAR(20));
    END

    -----------------------------------------------------------------------------
    -- 2) Remove BOM paths from Industries / AcceptIndustrial
    -----------------------------------------------------------------------------
    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE r.Name IN (N'Industries', N'Sale.ProductionOrderItem.AcceptIndustrial')
      AND (
            ra.Path LIKE N'/panel/productionorderitem/%bom%'
         OR ra.Path LIKE N'/panel/productionorderitem/savebom%'
         OR ra.Path LIKE N'/panel/productionorderitem/deletebom%'
         OR ra.Path LIKE N'/panel/productionorderitem/importbom%'
         OR ra.Path LIKE N'/panel/productionorderitem/getbom%'
         OR ra.Path LIKE N'/panel/productionorderitem/downloadbom%'
      );
    PRINT N'  Removed BOM RoleAccess from Industries/AcceptIndustrial: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    -- 3) RoleAccess for BOM role (full BOM ops) + new-fields role (open+save)
    -----------------------------------------------------------------------------
    DECLARE @AccessRows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, RoleId BIGINT, DisplayName NVARCHAR(200));
    INSERT INTO @AccessRows (Path, AccessType, ItemType, RoleId, DisplayName) VALUES
        (N'/panel/productionorderitem/bomlistby',              1, 1000, @BomRoleId,       N'لیست BOM قلم سفارش ساخت'),
        (N'/panel/productionorderitem/getbomlistbyparentid',   2, 2,    @BomRoleId,       N'دریافت لیست BOM'),
        (N'/panel/productionorderitem/downloadbomexceltemplate', 2, 2,  @BomRoleId,       N'دانلود نمونه اکسل BOM'),
        (N'/panel/productionorderitem/savebom',                2, 3,    @BomRoleId,       N'ذخیره BOM'),
        (N'/panel/productionorderitem/importbomfromexcel',     2, 1000, @BomRoleId,       N'افزودن BOM از اکسل'),
        (N'/panel/productionorderitem/deletebom',              2, 6,    @BomRoleId,       N'حذف BOM'),
        -- نقش تکمیل فیلدهای جدید: باز کردن لیست + ذخیره (محدودیت فیلد در سرور)
        (N'/panel/productionorderitem/bomlistby',              1, 1000, @NewFieldsRoleId, N'لیست BOM قلم سفارش ساخت'),
        (N'/panel/productionorderitem/getbomlistbyparentid',   2, 2,    @NewFieldsRoleId, N'دریافت لیست BOM'),
        (N'/panel/productionorderitem/savebom',                2, 3,    @NewFieldsRoleId, N'ذخیره BOM');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT x.Path, x.AccessType, x.ItemType, @Entity, x.DisplayName, NULL, NULL, x.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @AccessRows x
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = x.RoleId AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType);
    PRINT N'  Inserted BOM RoleAccess rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    -- 4) Assign BOM role to matched HTS group-451 / New-Edit-Delete users
    -----------------------------------------------------------------------------
    DECLARE @BomUsers TABLE (Username NVARCHAR(200) NOT NULL);
    INSERT INTO @BomUsers (Username) VALUES
        (N'abdolmaleki.h'),(N'ahrarnejad.h'),(N'azami.sa'),(N'bagheri.m'),(N'bamimohammadi.gh'),
        (N'beiki.n'),(N'dehghan.r'),(N'eezi.a'),(N'erfani.m'),(N'falsafi.y'),
        (N'farokhi.m'),(N'ferasat.r'),(N'givehchi.h'),(N'hasani.m'),(N'javadian.p'),
        (N'kadkhodaei.v'),(N'kashani.sh'),(N'khaleghinejad.a'),(N'khodakarami.f'),(N'kondori.v'),
        (N'maranaki.m'),(N'mirlohi.a'),(N'mohammadi.a'),(N'moradi.p'),(N'mousavi.al'),
        (N'neysari.n'),(N'noroozi.s'),(N'norouzi.m'),(N'raman.f'),(N'raoufi.a'),
        (N'sabermanesh.s'),(N'saeedi.m'),(N'salahi.m'),(N'samii.m'),(N'shaghaghi.m'),
        (N'tabatabaei.z');

    DECLARE @AssignedBom INT = 0, @SkipBom INT = 0;
    DECLARE @Uid BIGINT, @Uname NVARCHAR(200), @RolesJson NVARCHAR(MAX), @RoleIdsJson NVARCHAR(MAX);
    DECLARE @MatchCnt INT;

    DECLARE bom_cur CURSOR LOCAL FAST_FORWARD FOR SELECT Username FROM @BomUsers;
    OPEN bom_cur;
    FETCH NEXT FROM bom_cur INTO @Uname;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MatchCnt = COUNT(*) FROM system.[User] u WHERE LOWER(u.Username) = LOWER(@Uname) AND u.IsActive = 1;
        IF @MatchCnt <> 1
        BEGIN
            SET @SkipBom += 1;
            PRINT N'  SKIP BOM user (match count=' + CAST(@MatchCnt AS NVARCHAR(10)) + N'): ' + @Uname;
        END
        ELSE
        BEGIN
            SELECT TOP 1 @Uid = Id, @RolesJson = ISNULL(Roles, N'[]'), @RoleIdsJson = ISNULL(RoleIds, N'[]')
            FROM system.[User] WHERE LOWER(Username) = LOWER(@Uname) AND IsActive = 1;

            IF ISJSON(@RolesJson) = 0 SET @RolesJson = N'[]';
            IF ISJSON(@RoleIdsJson) = 0 SET @RoleIdsJson = N'[]';

            IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RoleIdsJson) j WHERE TRY_CAST(j.value AS BIGINT) = @BomRoleId)
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RolesJson) WHERE LOWER([value]) = LOWER(@BomRoleName))
                    SET @RolesJson = JSON_MODIFY(@RolesJson, N'append $', @BomRoleName);
                SET @RoleIdsJson = JSON_MODIFY(@RoleIdsJson, N'append $', @BomRoleId);
                UPDATE system.[User]
                SET Roles = @RolesJson, RoleIds = @RoleIdsJson,
                    ModifiedById = 1, ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Id = @Uid;
                SET @AssignedBom += 1;
                PRINT N'  +BOM role for: ' + @Uname;
            END
        END
        FETCH NEXT FROM bom_cur INTO @Uname;
    END
    CLOSE bom_cur; DEALLOCATE bom_cur;
    PRINT N'  BOM role assigned=' + CAST(@AssignedBom AS NVARCHAR(20)) + N' skipped=' + CAST(@SkipBom AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    -- 5) Assign new-fields role to three named people
    -----------------------------------------------------------------------------
    DECLARE @Named TABLE (WantNameFa NVARCHAR(200) NOT NULL);
    INSERT INTO @Named (WantNameFa) VALUES
        (N'سپیده آزاد بخش'),
        (N'علی اکبر خلیلی'),
        (N'رسول کیان');

    DECLARE @AssignedNew INT = 0, @SkipNew INT = 0;
    DECLARE @Want NVARCHAR(200);

    DECLARE nf_cur CURSOR LOCAL FAST_FORWARD FOR SELECT WantNameFa FROM @Named;
    OPEN nf_cur;
    FETCH NEXT FROM nf_cur INTO @Want;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MatchCnt = COUNT(*)
        FROM system.[User] u
        WHERE u.IsActive = 1
          AND (
                REPLACE(REPLACE(u.NameFa, N' ', N''), N'‌', N'') = REPLACE(REPLACE(@Want, N' ', N''), N'‌', N'')
             OR u.NameFa = @Want
          );

        IF @MatchCnt <> 1
        BEGIN
            SET @SkipNew += 1;
            PRINT N'  SKIP new-fields user (match count=' + CAST(@MatchCnt AS NVARCHAR(10)) + N'): ' + @Want;
        END
        ELSE
        BEGIN
            SELECT TOP 1 @Uid = Id, @Uname = Username, @RolesJson = ISNULL(Roles, N'[]'), @RoleIdsJson = ISNULL(RoleIds, N'[]')
            FROM system.[User] u
            WHERE u.IsActive = 1
              AND (
                    REPLACE(REPLACE(u.NameFa, N' ', N''), N'‌', N'') = REPLACE(REPLACE(@Want, N' ', N''), N'‌', N'')
                 OR u.NameFa = @Want
              );

            IF ISJSON(@RolesJson) = 0 SET @RolesJson = N'[]';
            IF ISJSON(@RoleIdsJson) = 0 SET @RoleIdsJson = N'[]';

            IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RoleIdsJson) j WHERE TRY_CAST(j.value AS BIGINT) = @NewFieldsRoleId)
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RolesJson) WHERE [value] = @NewFieldsRoleName)
                    SET @RolesJson = JSON_MODIFY(@RolesJson, N'append $', @NewFieldsRoleName);
                SET @RoleIdsJson = JSON_MODIFY(@RoleIdsJson, N'append $', @NewFieldsRoleId);
                UPDATE system.[User]
                SET Roles = @RolesJson, RoleIds = @RoleIdsJson,
                    ModifiedById = 1, ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Id = @Uid;
                SET @AssignedNew += 1;
                PRINT N'  +new-fields role for: ' + @Uname + N' (' + @Want + N')';
            END
            ELSE
                PRINT N'  already has new-fields role: ' + @Uname;
        END
        FETCH NEXT FROM nf_cur INTO @Want;
    END
    CLOSE nf_cur; DEALLOCATE nf_cur;
    PRINT N'  New-fields role assigned=' + CAST(@AssignedNew AS NVARCHAR(20)) + N' skipped=' + CAST(@SkipNew AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_ProductionOrderItemBom_RolesAndAccess ===';

    -- verify
    SELECT r.Id, r.Name, r.Title,
           (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds,N'[]')) j
            WHERE TRY_CAST(j.value AS BIGINT) = r.Id AND u.IsActive = 1) AS ActiveMembers
    FROM system.Role r
    WHERE r.Id IN (@BomRoleId, @NewFieldsRoleId);

    SELECT ra.Path, ra.ActionAccessType, ra.RoleId, r.Name
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.RoleId IN (@BomRoleId, @NewFieldsRoleId)
       OR (r.Name IN (N'Industries', N'Sale.ProductionOrderItem.AcceptIndustrial')
           AND (ra.Path LIKE N'%bom%' OR ra.Path LIKE N'%Bom%'))
    ORDER BY r.Name, ra.Path;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
