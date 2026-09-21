/*
================================================================================
Seed_AfterSalesServiceSystem_Menu.sql
================================================================================
قسمت ۰ — گروه منوی خالی «سیستم خدمات پس از فروش».
برگ‌ها در اسکریپت‌های قسمت‌های ۱–۱۴ اضافه می‌شوند.

Idempotent روی system.SystemMenu.Name = N'AfterSalesServiceSystem'.
Restart WebApp بعد از اجرا لازم نیست (منو از جدول خوانده می‌شود).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-after-sales-menu';

DECLARE @MenuName NVARCHAR(150) = N'AfterSalesServiceSystem';
DECLARE @MenuTitle NVARCHAR(150) = N'سیستم خدمات پس از فروش';

-- Folder-only tree (no leaf paths yet). Paths filled by later part seeds.
DECLARE @MenuContent NVARCHAR(MAX) = N'[
  {"text":"مدیریت مناقصات","icon":"ki-briefcase ki-outline","iconColor":"#d78819","path":"","a_attr":{"href":""},"data":{"iconColor":"#d78819","path":""},"children":[]},
  {"text":"فروش","icon":"ki-shop ki-outline","iconColor":"#1fc4e5","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fc4e5","path":""},"children":[]},
  {"text":"خدمات پس از فروش","icon":"ki-shield-tick ki-outline","iconColor":"#50dcae","path":"","a_attr":{"href":""},"data":{"iconColor":"#50dcae","path":""},"children":[]},
  {"text":"گزارشات","icon":"ki-chart-simple-3 ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[]},
  {"text":"تعمیرات","icon":"ki-wrench ki-outline","iconColor":"#ed230c","path":"","a_attr":{"href":""},"data":{"iconColor":"#ed230c","path":""},"children":[]},
  {"text":"پرسش فنی","icon":"ki-questionnaire-tablet ki-outline","iconColor":"#1fe57f","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fe57f","path":""},"children":[]},
  {"text":"نمایندگی","icon":"ki-home ki-outline","iconColor":"#1fc4e5","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fc4e5","path":""},"children":[]},
  {"text":"نفت و گاز","icon":"ki-drop ki-outline","iconColor":"#d78819","path":"","a_attr":{"href":""},"data":{"iconColor":"#d78819","path":""},"children":[]},
  {"text":"مدیریت کاربران موبایل","icon":"ki-phone ki-outline","iconColor":"#50dcae","path":"","a_attr":{"href":""},"data":{"iconColor":"#50dcae","path":""},"children":[]},
  {"text":"مانیتورینگ تجهیزات","icon":"ki-technology-4 ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[]},
  {"text":"رضایت‌سنجی بعد از فروش","icon":"ki-like ki-outline","iconColor":"#1fe57f","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fe57f","path":""},"children":[]}
]';

DECLARE @IdShowAllMenus BIGINT = NULL;
SELECT TOP 1 @IdShowAllMenus = Id FROM system.Role WHERE Name = N'ShowAllMenus';

DECLARE @AccessRoles NVARCHAR(MAX) = N'["ShowAllMenus"]';
DECLARE @AccessRoleIds NVARCHAR(MAX) = N'[]';
IF @IdShowAllMenus IS NOT NULL
    SET @AccessRoleIds = N'[' + CAST(@IdShowAllMenus AS nvarchar(20)) + N']';

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM system.SystemMenu WHERE Name = @MenuName)
        UPDATE system.SystemMenu
        SET Title = @MenuTitle,
            Content = @MenuContent,
            AccessRoles = @AccessRoles,
            AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @MenuName;
    ELSE
        INSERT INTO system.SystemMenu
            (Title, Name, Content, AccessRoles, AccessRoleIds,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
             ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@MenuTitle, @MenuName, @MenuContent, @AccessRoles, @AccessRoleIds,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_AfterSalesServiceSystem_Menu (empty groups) ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
