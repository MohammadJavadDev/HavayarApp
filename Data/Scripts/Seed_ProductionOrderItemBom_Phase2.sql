/*
================================================================================
Seed_ProductionOrderItemBom_Phase2.sql
================================================================================
دسترسی و اعلان BOM اقلام سفارش ساخت پس از بازطراحی «جدول اصلی = آخرین نسخه + جدول تاریخچه»:

  1) RoleAccess برای /panel/productionorderitem/addbomfromproductformula روی نقش Sale.ProductionOrderItemBom
     (کاربران مهندسی دارای ویرایش BOM). بدون این ردیف، دکمهٔ «افزودن BOM از روی فرمول محصول» برای غیرادمین ۴۰۳ می‌دهد.
  2) نقش Sale.ProductionOrderItemBom.View برای اعضای فعال گروه‌های HTS ۴۵۲ (کارشناسان عمومی Bom) و ۳۵۰ (مشاهده کلی HTS)
     که در سیستم جدید هیچ نقش BOM ندارند. تطبیق با نام کاربری. نیاز به Linked Server [TMS].
  3) دو گروه اعلان شعبه‌ای (معادل AddEmailOfRelatedMembers در HTS):
       Sale.ProductionOrderItemBom.SalesBranch            noroozi.s, givehchi.h, rostampoor.a
       Sale.ProductionOrderItemBom.TurboCompressorBranch  پرسنل دارای ایمیل واحد ۱۴۷ و ۲۶۶ HTS (تطبیق با ایمیل)
  4) پر کردن Email خالی اعضای گروه‌های اعلان از system.User.

Idempotent. UTF-8 with BOM required. Apply:
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_ProductionOrderItemBom_Phase2.sql"
بعد از اجرا WebApp را ری‌استارت کنید (کش نقش‌ها).
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-bom-phase2';
DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItem';

DECLARE @ManageRoleName NVARCHAR(200) = N'Sale.ProductionOrderItemBom';
DECLARE @NewFieldsRoleName NVARCHAR(200) = N'تکمیل اطلاعات bom اقلام سفارش ساخت صنایع';
DECLARE @ViewRoleName NVARCHAR(200) = N'Sale.ProductionOrderItemBom.View';

DECLARE @ManageRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = @ManageRoleName);
DECLARE @NewFieldsRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = @NewFieldsRoleName);
DECLARE @ViewRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = @ViewRoleName);

IF @ManageRoleId IS NULL OR @NewFieldsRoleId IS NULL OR @ViewRoleId IS NULL
    THROW 51101, N'One of the BOM roles (manage / new-fields / view) was not found.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    THROW 51102, N'Linked server [TMS] not found.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    /* -----------------------------------------------------------------------
       1) RoleAccess: افزودن BOM از روی فرمول محصول (ActionAccessType=2 Api، ItemType=4 Create)
       ----------------------------------------------------------------------- */
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'/panel/productionorderitem/addbomfromproductformula', 2, 4, @Entity, N'افزودن BOM از روی فرمول محصول', NULL, NULL, @ManageRoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = @ManageRoleId
          AND ra.Path = N'/panel/productionorderitem/addbomfromproductformula'
          AND ra.ActionAccessType = 2);
    PRINT N'RoleAccess (addbomfromproductformula) inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* -----------------------------------------------------------------------
       2) نقش مشاهده برای اعضای فعال گروه‌های ۴۵۲ و ۳۵۰ HTS
       ----------------------------------------------------------------------- */
    IF OBJECT_ID(N'tempdb..#ViewTargets') IS NOT NULL DROP TABLE #ViewTargets;
    SELECT DISTINCT LOWER(hu.ActiveDirectoryUsername) AS Username
    INTO #ViewTargets
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = m.User_FK
    WHERE m.UserGroup_FK IN (452, 350)
      AND hu.IsActive = 1
      AND hu.ActiveDirectoryUsername IS NOT NULL;

    UPDATE u
    SET Roles = JSON_MODIFY(CASE WHEN ISJSON(u.Roles) = 1 THEN u.Roles ELSE N'[]' END, N'append $', @ViewRoleName),
        RoleIds = JSON_MODIFY(CASE WHEN ISJSON(u.RoleIds) = 1 THEN u.RoleIds ELSE N'[]' END, N'append $', @ViewRoleId),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    FROM system.[User] u
    INNER JOIN #ViewTargets t ON t.Username = LOWER(u.Username)
    WHERE u.IsActive = 1
      AND NOT EXISTS (
            SELECT 1
            FROM OPENJSON(CASE WHEN ISJSON(u.RoleIds) = 1 THEN u.RoleIds ELSE N'[]' END) j
            WHERE TRY_CAST(j.value AS BIGINT) IN (@ManageRoleId, @NewFieldsRoleId, @ViewRoleId));
    PRINT N'View role assigned to users: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* -----------------------------------------------------------------------
       3) گروه‌های اعلان شعبه‌ای
       ----------------------------------------------------------------------- */
    DECLARE @Groups TABLE (Code NVARCHAR(100), DisplayName NVARCHAR(200), Description NVARCHAR(1500));
    INSERT INTO @Groups (Code, DisplayName, Description) VALUES
     (N'Sale.ProductionOrderItemBom.SalesBranch',
      N'سفارش ساخت - BOM - شعب فروش ویژه',
      N'CC اعلان BOM برای سفارش‌های شعب فروش با شناسهٔ HTS برابر ۱۲، ۱۵، ۳۹، ۴۹، ۵۰، ۵۱ (معادل AddEmailOfRelatedMembers در HTS)'),
     (N'Sale.ProductionOrderItemBom.TurboCompressorBranch',
      N'سفارش ساخت - BOM - شعب توربو و کمپرسور مهندسی',
      N'CC اعلان BOM برای سفارش‌های شعبهٔ توربو ماشین و کمپرسورهای مهندسی (۴۱ و ۴۲) — پرسنل واحد برنامه‌ریزی پروژه در HTS');

    INSERT INTO system.NotificationGroup
        (Code, DisplayName, Description,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT g.Code, g.DisplayName, g.Description, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Groups g
    WHERE NOT EXISTS (SELECT 1 FROM system.NotificationGroup n WHERE n.Code = g.Code);
    PRINT N'NotificationGroup rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    DECLARE @SalesBranchGroupId BIGINT = (SELECT Id FROM system.NotificationGroup WHERE Code = N'Sale.ProductionOrderItemBom.SalesBranch');
    DECLARE @TurboGroupId BIGINT = (SELECT Id FROM system.NotificationGroup WHERE Code = N'Sale.ProductionOrderItemBom.TurboCompressorBranch');

    IF OBJECT_ID(N'tempdb..#GroupMembers') IS NOT NULL DROP TABLE #GroupMembers;
    CREATE TABLE #GroupMembers (GroupId BIGINT NOT NULL, UserId BIGINT NOT NULL);

    -- شعب فروش ویژه: سه نفر ثابت HTS
    INSERT INTO #GroupMembers (GroupId, UserId)
    SELECT @SalesBranchGroupId, u.Id
    FROM system.[User] u
    WHERE u.IsActive = 1
      AND LOWER(u.Username) IN (N'noroozi.s', N'givehchi.h', N'rostampoor.a');

    -- توربو و کمپرسور مهندسی: پرسنل دارای ایمیل واحد ۱۴۷ و ۲۶۶ HTS، تطبیق با ایمیل
    INSERT INTO #GroupMembers (GroupId, UserId)
    SELECT DISTINCT @TurboGroupId, u.Id
    FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] p
    INNER JOIN system.[User] u ON LOWER(u.Email) = LOWER(LTRIM(RTRIM(p.PrsEmail)))
    WHERE p.OrgUnit_FK IN (147, 266)
      AND p.PrsEmail IS NOT NULL
      AND LTRIM(RTRIM(p.PrsEmail)) <> N''
      AND u.IsActive = 1;

    INSERT INTO system.NotificationGroupMember
        (NotificationGroupId, UserId, Email, FullName,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT gm.GroupId, gm.UserId, ISNULL(u.Email, N''), COALESCE(NULLIF(u.NameFa, N''), u.Name),
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #GroupMembers gm
    INNER JOIN system.[User] u ON u.Id = gm.UserId
    WHERE NOT EXISTS (
        SELECT 1 FROM system.NotificationGroupMember m
        WHERE m.NotificationGroupId = gm.GroupId AND m.UserId = gm.UserId);
    PRINT N'NotificationGroupMember rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* -----------------------------------------------------------------------
       4) Email خالی اعضای گروه‌های اعلان
       ----------------------------------------------------------------------- */
    UPDATE m
    SET m.Email = u.Email,
        m.ModifiedById = 1,
        m.ModifiedByName = @SeedUser,
        m.ModifiedDateMiladiDateTime = @Now,
        m.ModifiedDateShamsiDateTime = @NowShamsi
    FROM system.NotificationGroupMember m
    INNER JOIN system.[User] u ON u.Id = m.UserId
    WHERE (m.Email IS NULL OR LTRIM(RTRIM(m.Email)) = N'')
      AND u.Email IS NOT NULL
      AND LTRIM(RTRIM(u.Email)) <> N'';
    PRINT N'Empty member emails filled: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_ProductionOrderItemBom_Phase2 ===';

    -- verify
    SELECT r.Name AS Role,
           (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE TRY_CAST(j.value AS BIGINT) = r.Id AND u.IsActive = 1) AS ActiveMembers
    FROM system.Role r
    WHERE r.Id IN (@ManageRoleId, @NewFieldsRoleId, @ViewRoleId);

    SELECT g.Code, COUNT(m.Id) AS Members, SUM(CASE WHEN m.Email IS NULL OR LTRIM(RTRIM(m.Email)) = N'' THEN 1 ELSE 0 END) AS EmptyEmail
    FROM system.NotificationGroup g
    LEFT JOIN system.NotificationGroupMember m ON m.NotificationGroupId = g.Id
    WHERE g.Code LIKE N'Sale.ProductionOrderItemBom%'
    GROUP BY g.Code
    ORDER BY g.Code;

    SELECT ra.Path, ra.ActionAccessType, ra.ActionAccessItemType, r.Name AS Role
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.Path = N'/panel/productionorderitem/addbomfromproductformula';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
