/*
================================================================================
Seed_ProductionOrderItemBom_ViewRole.sql
================================================================================
نقش مشاهده BOM اقلام سفارش ساخت:

  Sale.ProductionOrderItemBom.View  / Title: مشاهده Bom اقلام سفارش ساخت
  — دکمه «Bom محصول» و باز کردن لیست
  — بدون افزودن، ویرایش، حذف، اکسل ورودی و دانلود نمونه

Name نقش باید دقیقاً Sale.ProductionOrderItemBom.View بماند؛ کد HasRole همین رشته را
از Roles کاربر می‌خواند (فرم کاربر Name را ذخیره می‌کند، نه Title).

Idempotent. UTF-8 with BOM required. Apply:
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i thisfile.sql
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-bom-view';
DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItem';

DECLARE @ViewRoleId BIGINT = 200071;
DECLARE @ViewRoleName NVARCHAR(200) = N'Sale.ProductionOrderItemBom.View';
DECLARE @ViewRoleTitle NVARCHAR(200) = N'مشاهده Bom اقلام سفارش ساخت';

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @ViewRoleName)
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @ViewRoleId)
            SELECT @ViewRoleId = ISNULL(MAX(Id), 200000) + 1 FROM system.Role WHERE Id BETWEEN 200000 AND 299999;

        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role
            (Id, Name, Title, IsActive,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
             ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
        VALUES
            (@ViewRoleId, @ViewRoleName, @ViewRoleTitle, 1,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  Created role: ' + @ViewRoleName + N' id=' + CAST(@ViewRoleId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        SELECT @ViewRoleId = Id FROM system.Role WHERE Name = @ViewRoleName;
        UPDATE system.Role
        SET Title = @ViewRoleTitle, IsActive = 1,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @ViewRoleId;
        PRINT N'  Reused role: ' + @ViewRoleName + N' id=' + CAST(@ViewRoleId AS NVARCHAR(20));
    END

    DECLARE @AccessRows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, DisplayName NVARCHAR(200));
    INSERT INTO @AccessRows (Path, AccessType, ItemType, DisplayName) VALUES
        (N'/panel/productionorderitem/bomlistby',            1, 1000, N'لیست BOM قلم سفارش ساخت'),
        (N'/panel/productionorderitem/getbomlistbyparentid', 2, 2,    N'دریافت لیست BOM');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT x.Path, x.AccessType, x.ItemType, @Entity, x.DisplayName, NULL, NULL, @ViewRoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @AccessRows x
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = @ViewRoleId AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType);
    PRINT N'  Inserted view RoleAccess rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_ProductionOrderItemBom_ViewRole ===';

    SELECT r.Id, r.Name, r.Title, r.IsActive
    FROM system.Role r
    WHERE r.Name = @ViewRoleName;

    SELECT ra.Path, ra.DisplayName, ra.ActionAccessType, ra.ActionAccessItemType, ra.IsActive
    FROM system.RoleAccess ra
    WHERE ra.RoleId = @ViewRoleId
    ORDER BY ra.Path;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
