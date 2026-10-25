/*
Seed_Qc_MeasurementParameterItem_RolesAndAccess.sql
دسترسی پارامتر اندازه گیری روی همان نقش‌های صفحه HTS 298.
نقش جدید ساخته نمی‌شود و کاربر دوباره انتساب نمی‌گیرد.
  Qc.MeasurementParameter.Experts / SystemExperts : کامل
  مشاهده کلی HTS : list و fetchdata
  ShowAllMenus : مسیرهای صفحه بدون خروجی اکسل
UTF-8 BOM. Idempotent.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_MeasurementParameterItem_RolesAndAccess.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-measurementparameteritem-roles';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Qc.MeasurementParameter.Experts')
        OR NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Qc.MeasurementParameter.SystemExperts')
        OR NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'ShowAllMenus')
        OR NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'مشاهده کلی HTS')
        THROW 50041, N'Page 298 roles are missing. Run Seed_Qc_MeasurementParameter_RolesAndAccess.sql first.', 1;

    PRINT N'=== [1] Controller RoleAccess ===';
    IF OBJECT_ID('tempdb..#Map') IS NOT NULL DROP TABLE #Map;
    CREATE TABLE #Map (RoleName NVARCHAR(200) NOT NULL, Path NVARCHAR(300) NOT NULL, AType INT NOT NULL, IType INT NOT NULL);

    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/qc/measurementparameteritem/list', 1, 1),
        (N'/panel/qc/measurementparameteritem/fetchdata', 2, 2)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'), (N'مشاهده کلی HTS'),
        (N'Qc.MeasurementParameter.Experts'), (N'Qc.MeasurementParameter.SystemExperts')
    ) r(RoleName);

    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/qc/measurementparameteritem/new', 1, 4),
        (N'/panel/qc/measurementparameteritem/edit', 1, 5),
        (N'/panel/qc/measurementparameteritem/save', 2, 3),
        (N'/panel/qc/measurementparameteritem/add', 2, 4),
        (N'/panel/qc/measurementparameteritem/update', 2, 5),
        (N'/panel/qc/measurementparameteritem/delete', 2, 6)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'),
        (N'Qc.MeasurementParameter.Experts'), (N'Qc.MeasurementParameter.SystemExperts')
    ) r(RoleName);

    DELETE ra FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.Path LIKE N'/panel/qc/measurementparameteritem/%'
      AND r.Name IN (
            N'Qc.MeasurementParameter.Experts', N'Qc.MeasurementParameter.SystemExperts',
            N'ShowAllMenus', N'مشاهده کلی HTS'
      );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT m.Path, m.AType, m.IType,
        N'Entities.App.Qc.MeasurementParameterItem',
        NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #Map m
    INNER JOIN system.Role r ON r.Name = m.RoleName;
    PRINT N'  Controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    IF (SELECT COUNT(*) FROM system.RoleAccess ra
        INNER JOIN system.Role r ON r.Id = ra.RoleId
        WHERE ra.Path LIKE N'/panel/qc/measurementparameteritem/%'
          AND r.Name IN (
                N'Qc.MeasurementParameter.Experts', N'Qc.MeasurementParameter.SystemExperts',
                N'ShowAllMenus', N'مشاهده کلی HTS'
          )) <> 26
        THROW 50042, N'MeasurementParameterItem RoleAccess count is not 26.', 1;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_MeasurementParameterItem_RolesAndAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
