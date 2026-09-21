/*
================================================================================
Seed_ProductionOrderItem_WorkflowRoleAccess.sql
================================================================================
دسترسی اکشن‌های گردش‌کار اقلام سفارش ساخت (system.RoleAccess با ActionAccessType=2 Api / ActionAccessItemType=1000 Custom)
مطابق نگاشت مجوز↔وضعیت HTS (PermissionType 137/138/139 روی صفحه 214):

  Sale.ProductionOrderItem.SupplyCommitteeBoss        (Pln_SupplyCommitteeBoss 137) → committeedecide/{id}
  Sale.ProductionOrderItem.Inquirer                   (Pln_Inquirer 138)          → sendtosupplycommittee/{id}
  Sale.ProductionOrderItem.MinistryOfIndustryInquirer (Pln_MinistryOfIndustryInquirer 139) → sendtosupplycommittee/{id}
                                                      + دسترسی صفحه (list/edit/new) که برای این نقش ثبت نشده بود

اکشن‌های بدون [ActionDisplayName] (ProductionOrderItemInquiryPartial, SparePartListPartial, *Partial) در کاتالوگ دسترسی
نیستند و برای هر کاربر لاگین‌شده مجازند — ردیف لازم ندارند.

پاک‌سازی: ردیف‌های اکشن‌های حذف‌شده (تصمیم 2026-09-17) که دیگر endpoint ندارند.

Idempotent. Path ها lowercase و با قالب route (مثل ردیف‌های Role UI) ذخیره می‌شوند.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-workflow-roleaccess';
DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItem';

BEGIN TRY
    BEGIN TRANSACTION;

    -----------------------------------------------------------------------------
    -- 1) حذف ردیف‌های اکشن‌های حذف‌شده
    -----------------------------------------------------------------------------
    DELETE FROM system.RoleAccess
    WHERE ActionAccessType <> 3
      AND Path IN (
        N'/panel/productionorderitem/acceptindustrial/{id}',
        N'/panel/productionorderitem/sendtoinquiry/{id}',
        N'/panel/productionorderitem/sendtoministryreview/{id}',
        N'/panel/productionorderitem/committeeaccept/{id}',
        N'/panel/productionorderitem/committeereject/{id}',
        N'/panel/productionorder/dofinancialconfirm/{id}');
    PRINT N'  Removed stale RoleAccess rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    -- 2) ردیف‌های لازم
    -----------------------------------------------------------------------------
    DECLARE @Rows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, RoleName NVARCHAR(200));
    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName) VALUES
        -- رئیس کمیته تامین: تصمیم کمیته (۲۲۰۲/۱۹۰۹ → ۱۹۰۴/۱۹۰۵/۱۹۰۶/۱۹۰۸)
        (N'/panel/productionorderitem/committeedecide/{id}',        2, 1000, N'Sale.ProductionOrderItem.SupplyCommitteeBoss'),
        -- استعلام‌گر (۱۹۰۶) و استعلام‌گر وزارت (۱۹۰۸): بازگشت نتیجه به کمیته (۱۹۰۹)
        (N'/panel/productionorderitem/sendtosupplycommittee/{id}',  2, 1000, N'Sale.ProductionOrderItem.Inquirer'),
        (N'/panel/productionorderitem/sendtosupplycommittee/{id}',  2, 1000, N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer'),
        -- دسترسی صفحه برای استعلام‌گر وزارت (سایر نقش‌های کارتابل از قبل داشتند)
        (N'/panel/productionorderitem/list',                        1, 1,    N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer'),
        (N'/panel/productionorderitem/edit',                        1, 5,    N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer'),
        (N'/panel/productionorderitem/new',                         1, 4,    N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT x.Path, x.AccessType, x.ItemType, @Entity, N'', NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Rows x
    INNER JOIN system.Role r ON r.Name = x.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType);
    PRINT N'  Inserted RoleAccess rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_ProductionOrderItem_WorkflowRoleAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT ra.Path, ra.ActionAccessType, ra.ActionAccessItemType, r.Name AS RoleName
FROM system.RoleAccess ra JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType <> 3
  AND r.Name IN (N'Sale.ProductionOrderItem.SupplyCommitteeBoss', N'Sale.ProductionOrderItem.Inquirer', N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer')
ORDER BY r.Name, ra.Path;
