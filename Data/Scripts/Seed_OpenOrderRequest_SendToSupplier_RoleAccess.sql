/*
================================================================================
Seed_OpenOrderRequest_SendToSupplier_RoleAccess.sql  (WP1)
================================================================================
دسترسی اکشن‌های ارسال به پیمانکار / تامین‌کننده منتخب روی OpenOrderRequestController.

  HTS 76·SendEmail(27)  → /sendtosupplier, /sendtosupplierpartial
      نقش: ConfigManage، SupplyAndPurchase
  HTS AddComponyMansToRequests (صفحه 76)
      → /setselectedsupplier, /setselectedsupplierpartial
      نقش: ConfigManage، Industrial، Industries، SupplyAndPurchase

Idempotent. RoleId از system.Role.Name. پس از اجرا WebApp را ری‌استارت کنید.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-oor-sendtosupplier-wp1';
DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sup.OpenOrderRequest';
DECLARE @Base NVARCHAR(100) = N'/panel/sup/openorderrequest';

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Rows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, RoleName NVARCHAR(200));

    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName)
    SELECT a.Path, 2, 1000, r.RoleName
    FROM (VALUES
        (@Base + N'/sendtosupplier'),
        (@Base + N'/sendtosupplierpartial')
    ) a(Path)
    CROSS JOIN (VALUES
        (N'Sup.OpenOrderRequest.ConfigManage'),
        (N'SupplyAndPurchase')
    ) r(RoleName);

    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName)
    SELECT a.Path, 1, 1000, r.RoleName
    FROM (VALUES (@Base + N'/sendtosupplierpartial')) a(Path)
    CROSS JOIN (VALUES
        (N'Sup.OpenOrderRequest.ConfigManage'),
        (N'SupplyAndPurchase')
    ) r(RoleName);

    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName)
    SELECT a.Path, 2, 1000, r.RoleName
    FROM (VALUES
        (@Base + N'/setselectedsupplier'),
        (@Base + N'/setselectedsupplierpartial')
    ) a(Path)
    CROSS JOIN (VALUES
        (N'Sup.OpenOrderRequest.ConfigManage'),
        (N'Sup.OpenOrderRequest.Industrial'),
        (N'Industries'),
        (N'SupplyAndPurchase')
    ) r(RoleName);

    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName)
    SELECT a.Path, 1, 1000, r.RoleName
    FROM (VALUES (@Base + N'/setselectedsupplierpartial')) a(Path)
    CROSS JOIN (VALUES
        (N'Sup.OpenOrderRequest.ConfigManage'),
        (N'Sup.OpenOrderRequest.Industrial'),
        (N'Industries'),
        (N'SupplyAndPurchase')
    ) r(RoleName);

    SELECT DISTINCT x.RoleName AS MissingRole
    FROM @Rows x
    WHERE NOT EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = x.RoleName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT x.Path, x.AccessType, x.ItemType, @Entity, N'', NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Rows x
    INNER JOIN system.Role r ON r.Name = x.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType);
    PRINT N'  Inserted RoleAccess rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_OpenOrderRequest_SendToSupplier_RoleAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path LIKE N'/panel/sup/openorderrequest/sendtosupplier%'
   OR ra.Path LIKE N'/panel/sup/openorderrequest/setselectedsupplier%'
ORDER BY ra.Path, r.Name;
