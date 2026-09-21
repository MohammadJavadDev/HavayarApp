/*
  Seed_Engineering_HtsLeftover_DirectAndFixes.sql
  باقی‌ماندهٔ دسترسی مهندسی بعد از Seed_Engineering_HtsGroups_RolesAndUsers:
    1) گروه ۳۶۳ — فقط ExportToExcel روی لیست کنترل مدارک
    2) گروه ۴۸۲ تکوین (perm 171) — نمایه آرشیو کلیه پروژه‌ها + list/fetch، بدون CRUD
    3) ۱۰ کاربر اعطای مستقیم HTS (بدون گروه) → نقش گروه نزدیک یا نقش کوچک Eng.Direct
  تغییر نمی‌دهد: SystemMenu، نقش eng، DCC، زیرگروه‌های درخواست تغییر قطعه ۲۹۹، ۴۲ نقش خالی.
  Idempotent. Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-eng-hts-leftover';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] Group 363: document control Excel export only ===';
    -----------------------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1 FROM system.RoleAccess
        WHERE RoleId = 260363
          AND Path = N'/panel/edms/document/exporttoexcel'
          AND ActionAccessType = 2
    )
    BEGIN
        INSERT INTO system.RoleAccess
            (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (N'/panel/edms/document/exporttoexcel', 2, 0, N'Entities.App.Edms.Document', N'خروجی اکسل', NULL, NULL, 260363,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        PRINT N'  Inserted 260363 /panel/edms/document/exporttoexcel';
    END
    ELSE
        PRINT N'  260363 exporttoexcel already present';

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Group 482 Takvin: archive Data Profile + list path (no CRUD) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#TakvinAccess') IS NOT NULL DROP TABLE #TakvinAccess;
    CREATE TABLE #TakvinAccess
    (
        Path                 NVARCHAR(300) NOT NULL,
        ActionAccessType     INT           NOT NULL,
        ActionAccessItemType INT           NOT NULL,
        EntityName           NVARCHAR(200) NULL,
        DisplayName          NVARCHAR(200) NULL,
        RowId                BIGINT        NULL
    );

    INSERT INTO #TakvinAccess (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    VALUES
        (N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'لیست اطلاعات', NULL),
        (N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'دریافت داده', NULL);

    INSERT INTO #TakvinAccess (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q
    WHERE q.Name = N'vw_DocumentAllProjectInfo';

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, a.DisplayName, NULL, a.RowId, 260482,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #TakvinAccess a
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = 260482
          AND x.Path = a.Path
          AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted 260482 archive/list RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [3] Small per-grant-set roles for direct HTS users ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#DirectRole') IS NOT NULL DROP TABLE #DirectRole;
    CREATE TABLE #DirectRole
    (
        RoleId BIGINT        NOT NULL PRIMARY KEY,
        Name   NVARCHAR(200) NOT NULL,
        Title  NVARCHAR(200) NOT NULL
    );
    INSERT INTO #DirectRole (RoleId, Name, Title) VALUES
        (269001, N'اعطا مستقیم - آرشیو شخصی اسناد', N'اعطا مستقیم - آرشیو شخصی اسناد'),
        (269002, N'اعطا مستقیم - مشاهده شناسنامه کمپرسور', N'اعطا مستقیم - مشاهده شناسنامه کمپرسور'),
        (269003, N'اعطا مستقیم - مشاهده محصول', N'اعطا مستقیم - مشاهده محصول'),
        (269004, N'اعطا مستقیم - مشاهده BOM و آرشیو', N'اعطا مستقیم - مشاهده BOM و آرشیو'),
        (269005, N'اعطا مستقیم - محصول کامل و مقایسه قیمت', N'اعطا مستقیم - محصول کامل و مقایسه قیمت'),
        (269006, N'اعطا مستقیم - مقایسه قیمت محصولات', N'اعطا مستقیم - مقایسه قیمت محصولات');

    DECLARE @DrId BIGINT, @DrName NVARCHAR(200), @DrTitle NVARCHAR(200);
    DECLARE @CreatedDirect INT = 0, @ExistsDirect INT = 0;
    DECLARE dr_cur CURSOR LOCAL FAST_FORWARD FOR SELECT RoleId, Name, Title FROM #DirectRole;
    OPEN dr_cur;
    FETCH NEXT FROM dr_cur INTO @DrId, @DrName, @DrTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @DrId)
        BEGIN
            UPDATE system.Role
            SET Name = @DrName, Title = @DrTitle, IsActive = 1,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @DrId;
            SET @ExistsDirect += 1;
        END
        ELSE
        BEGIN
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role
                (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
                 CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@DrId, @DrName, @DrTitle, 1, @SeedUser, 1, @SeedUser,
                 @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
            SET @CreatedDirect += 1;
        END
        FETCH NEXT FROM dr_cur INTO @DrId, @DrName, @DrTitle;
    END
    CLOSE dr_cur;
    DEALLOCATE dr_cur;
    PRINT N'  Direct roles created: ' + CAST(@CreatedDirect AS nvarchar(20));
    PRINT N'  Direct roles already existed: ' + CAST(@ExistsDirect AS nvarchar(20));

    IF OBJECT_ID('tempdb..#DirectAccess') IS NOT NULL DROP TABLE #DirectAccess;
    CREATE TABLE #DirectAccess
    (
        RoleId               BIGINT         NOT NULL,
        Path                 NVARCHAR(300)  NOT NULL,
        ActionAccessType     INT            NOT NULL,
        ActionAccessItemType INT            NOT NULL,
        EntityName           NVARCHAR(200)  NULL,
        DisplayName          NVARCHAR(200)  NULL,
        RowId                BIGINT         NULL
    );

    -- 269001 personal archive read (HTS 233)
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269001, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'لیست اطلاعات', NULL),
        (269001, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'ویرایش اطلاعات', NULL),
        (269001, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'دریافت داده', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269001, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q WHERE q.Name = N'vw_DocumentMyArchive';

    -- 269002 compressor identity read (HTS 109 Read; skip ShowSalePrice 182)
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269002, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'لیست اطلاعات', NULL),
        (269002, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'ویرایش اطلاعات', NULL),
        (269002, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'دریافت داده', NULL),
        (269002, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'خروجی اکسل', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269002, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q WHERE q.Name IN (N'partextrainfo_listinfo', N'vw_PartextrainfoOilInject');

    -- 269003 product formul view read (HTS 107+108 Read, no price)
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269003, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'لیست اطلاعات', NULL),
        (269003, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'ویرایش اطلاعات', NULL),
        (269003, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'دریافت داده', NULL),
        (269003, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'خروجی اکسل', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269003, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q WHERE q.Name IN (N'productformul_listinfo', N'vw_productformulwithBom');

    -- 269004 Sadeghpour: 100/106/107/108/109/232/541 Read
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269004, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'لیست اطلاعات', NULL),
        (269004, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'ویرایش اطلاعات', NULL),
        (269004, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'دریافت داده', NULL),
        (269004, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'خروجی اکسل', NULL),
        (269004, N'/panel/inv/part/list', 1, 1, N'Entities.App.Inv.Part', N'لیست اطلاعات', NULL),
        (269004, N'/panel/inv/part/edit', 1, 5, N'Entities.App.Inv.Part', N'ویرایش اطلاعات', NULL),
        (269004, N'/panel/inv/part/fetchdata', 2, 2, N'Entities.App.Inv.Part', N'دریافت داده', NULL),
        (269004, N'/panel/inv/part/exporttoexcel', 2, 0, N'Entities.App.Inv.Part', N'خروجی اکسل', NULL),
        (269004, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'لیست اطلاعات', NULL),
        (269004, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'ویرایش اطلاعات', NULL),
        (269004, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'دریافت داده', NULL),
        (269004, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'خروجی اکسل', NULL),
        (269004, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'لیست اطلاعات', NULL),
        (269004, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'ویرایش اطلاعات', NULL),
        (269004, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'دریافت داده', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269004, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q
    WHERE q.Name IN (
        N'productformul_listinfo', N'vw_productformulwithBom', N'part_listinfo',
        N'partextrainfo_listinfo', N'vw_PartextrainfoOilInject', N'vw_PartextrainfoOilFree',
        N'vw_DocumentAllProjectInfo'
    );

    -- 269005 toosi: 107 FullAccess + 116 Read (no ShowPrice profile)
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269005, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'لیست اطلاعات', NULL),
        (269005, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'ویرایش اطلاعات', NULL),
        (269005, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'دریافت داده', NULL),
        (269005, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'خروجی اکسل', NULL),
        (269005, N'/panel/bom/productformul/new', 1, 4, N'Entities.App.Bom.ProductFormul', N'درج اطلاعات', NULL),
        (269005, N'/panel/bom/productformul/add', 2, 4, N'Entities.App.Bom.ProductFormul', N'درج اطلاعات', NULL),
        (269005, N'/panel/bom/productformul/save', 2, 3, N'Entities.App.Bom.ProductFormul', N'ذخیره', NULL),
        (269005, N'/panel/bom/productformul/update', 2, 5, N'Entities.App.Bom.ProductFormul', N'بروزرسانی', NULL),
        (269005, N'/panel/bom/productformul/delete', 2, 6, N'Entities.App.Bom.ProductFormul', N'حذف', NULL),
        (269005, N'/panel/bom/productpricecompare/list', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'لیست اطلاعات', NULL),
        (269005, N'/panel/bom/productpricecompare/fetchdata', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'دریافت داده', NULL),
        (269005, N'/panel/bom/productpricecompare/details', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'جزئیات', NULL),
        (269005, N'/panel/bom/productpricecompare/availabledates', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'تاریخ‌ها', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269005, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q WHERE q.Name IN (N'productformul_listinfo', N'vw_productformulwithBom');

    -- 269006 Zaeim: 116 FullAccess, no ShowPrice
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269006, N'/panel/bom/productpricecompare/list', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'لیست اطلاعات', NULL),
        (269006, N'/panel/bom/productpricecompare/fetchdata', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'دریافت داده', NULL),
        (269006, N'/panel/bom/productpricecompare/details', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'جزئیات', NULL),
        (269006, N'/panel/bom/productpricecompare/availabledates', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'تاریخ‌ها', NULL),
        (269006, N'/panel/bom/productpricecompare/sendemail', 2, 1000, N'Entities.App.Bom.ProductPriceCompare', N'ارسال ایمیل', NULL);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, a.DisplayName, NULL, a.RowId, a.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #DirectAccess a
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = a.RoleId AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted direct-role RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4] Assign leftover users (skip missing / inactive) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleId   BIGINT        NOT NULL
    );
    INSERT INTO #UserRoleMap (Username, RoleId) VALUES
        (N'Bostanpira.h', 269001),
        (N'Commissioning', 269001),
        (N'Shahini.a', 269001),
        (N'a', 269002),
        (N'Khalilnezhad.a', 269003),
        (N'Sadeghpour.j', 269004),
        (N'toosi.f', 269005),
        (N'Zaeim.s', 269006),
        (N'Kooravand.m', 260092);

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @MapIsActive bit;
    DECLARE @Assigned int = 0, @Missing int = 0, @Inactive int = 0, @SkippedAlready int = 0;

    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT m.Username, m.RoleId, r.Name
        FROM #UserRoleMap m
        INNER JOIN system.Role r ON r.Id = m.RoleId;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleId, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MapUserId = NULL;
        SET @MapIsActive = NULL;
        SELECT TOP 1 @MapUserId = Id, @MapIsActive = IsActive
        FROM system.[User]
        WHERE LOWER(Username) = LOWER(@MapUsername);

        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF ISNULL(@MapIsActive, 0) = 0
        BEGIN
            PRINT N'  INACTIVE USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
            SET @Inactive = @Inactive + 1;
        END
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
                        THEN N'["' + REPLACE(@MapRoleName, N'"', N'') + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + REPLACE(@MapRoleName, N'"', N'') + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END

        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleId, @MapRoleName;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    PRINT N'  User role assignments applied: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already had role (skipped): ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing users (logged): ' + CAST(@Missing AS nvarchar(20));
    PRINT N'  Inactive users (logged): ' + CAST(@Inactive AS nvarchar(20));
    PRINT N'  SKIPPED unimplemented page 246: kouchehbaghi.i (Edms_ProjectProgress_Report)';

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_Engineering_HtsLeftover_DirectAndFixes committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
