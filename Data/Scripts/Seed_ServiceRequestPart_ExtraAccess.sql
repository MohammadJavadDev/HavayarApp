/*
  Seed_ServiceRequestPart_ExtraAccess.sql
  دسترسی واکشی اقلام سند، محصولات مرتبط، سال/مرکز فروش و مشاهده قیمت.
  Encoding: UTF-8 with BOM. Apply:
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_ServiceRequestPart_ExtraAccess.sql"
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-servicerequestpart-extra-access';

    IF OBJECT_ID('tempdb..#SrPartExtra') IS NOT NULL DROP TABLE #SrPartExtra;
    CREATE TABLE #SrPartExtra (
        RoleName NVARCHAR(200) NOT NULL,
        Path NVARCHAR(300) NOT NULL,
        ActionAccessType INT NOT NULL,
        ActionAccessItemType INT NOT NULL
    );

    INSERT INTO #SrPartExtra (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sale/servicerequestpart/fetchpieces', 2, 1000),
        (N'/panel/sale/servicerequestpart/getpiecesdata', 2, 1000),
        (N'/panel/sale/servicerequestpart/getrelateddetails', 2, 1000),
        (N'/panel/sale/servicerequestpart/getformlookups', 2, 1000)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'Sale.ServiceRequest.Manage'),
        (N'Sale.ServiceRequest.View'),
        (N'Sale.ServiceRequest.ShowAll'),
        (N'Sale.AfterSales'),
        (N'ShowAllMenus')
    ) r(RoleName);

    INSERT INTO #SrPartExtra (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, N'/panel/sale/servicerequestpart/showprice', 1, 1000
    FROM (VALUES
        (N'Sale.ServiceRequest.Manage'),
        (N'Sale.AfterSales'),
        (N'ShowAllMenus')
    ) r(RoleName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType,
        N'Entities.App.Sale.ServiceRequestPart', NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #SrPartExtra a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );

    PRINT N'Inserted ServiceRequestPart extra RoleAccess: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
