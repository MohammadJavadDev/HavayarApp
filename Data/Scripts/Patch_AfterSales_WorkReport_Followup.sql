SET NOCOUNT ON;

UPDATE system.SavedQuery
SET ColumnsJson = REPLACE(
        ColumnsJson,
        N'"ColumnName":"ServiceRequestDetailId","DisplayName":"ServiceRequestDetailId"',
        N'"ColumnName":"ServiceRequestDetailId","DisplayName":"جزئیات درخواست"')
WHERE Name IN (N'AfterSales_WorkReport_List', N'AfterSales_ServiceRequestPart_List')
  AND ColumnsJson LIKE N'%"DisplayName":"ServiceRequestDetailId"%';

PRINT N'SavedQuery DisplayName rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-workreport-followup';

INSERT INTO system.RoleAccess
    (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT N'/panel/sale/workreport/exporttoexcel', 2, 0, N'Entities.App.Sale.WorkReport',
       NULL, NULL, NULL, r.Id,
       1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM system.Role r
WHERE r.Name IN (N'Sale.ServiceRequest.Manage', N'Sale.ServiceRequest.View', N'Sale.ServiceRequest.ShowAll')
  AND NOT EXISTS (
      SELECT 1 FROM system.RoleAccess x
      WHERE x.RoleId = r.Id
        AND x.Path = N'/panel/sale/workreport/exporttoexcel'
        AND x.ActionAccessType = 2
  );
PRINT N'exporttoexcel RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

INSERT INTO system.RoleAccess
    (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT
    N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
    3, 7, q.EntityFullName, q.Title, NULL, q.Id, r.Id,
    1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM system.SavedQuery q
CROSS JOIN system.Role r
WHERE q.Name IN (N'AfterSales_WorkReport_List', N'AfterSales_ServiceRequestPart_List')
  AND r.Name IN (
        N'Sale.ServiceRequest.Manage',
        N'Sale.ServiceRequest.View',
        N'Sale.ServiceRequest.ShowAll')
  AND NOT EXISTS (
      SELECT 1 FROM system.RoleAccess x
      WHERE x.ActionAccessType = 3 AND x.RowId = q.Id AND x.RoleId = r.Id
  );
PRINT N'catalog profile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

DELETE FROM system.RoleAccess
WHERE Path = N'/panel/sale/workreport/getsamesreports';
PRINT N'typo path deleted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

SELECT q.Name, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name IN (N'AfterSales_WorkReport_List', N'AfterSales_ServiceRequestPart_List')
  AND JSON_VALUE(j.value, '$.ColumnName') = N'ServiceRequestDetailId';
