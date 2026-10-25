/*
================================================================================
Seed_ProductionOrderItemBom_IndustrialRoleNameFix.sql
================================================================================
نقش ۲۰۰۶۷ یک گروه است: مشاهده BOM + اصلاح فیلدهای صنایع
(مرحله ساخت، تعداد تامین‌شده، وضعیت).

Name و Title هر دو باید «تکمیل اطلاعات bom اقلام سفارش ساخت صنایع» باشند.
Name کوتاه bom باعث می‌شود دکمه و میان‌افزار دسترسی با هم جور نباشند.

Idempotent. UTF-8 with BOM required. Apply:
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i thisfile.sql
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-bom-industrial-fix';
DECLARE @RoleId BIGINT = 200067;
DECLARE @RoleName NVARCHAR(200) = N'تکمیل اطلاعات bom اقلام سفارش ساخت صنایع';

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE system.Role
    SET Name = @RoleName,
        Title = @RoleName,
        IsActive = 1,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = @RoleId
      AND (Name <> @RoleName OR Title <> @RoleName OR IsActive <> 1);
    PRINT N'  Role name aligned: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    UPDATE u
    SET Roles = rebuilt.NewRoles,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    FROM system.[User] u
    CROSS APPLY (
        SELECT N'[' + STRING_AGG(N'"' + STRING_ESCAPE(token, 'json') + N'"', N',')
            WITHIN GROUP (ORDER BY ord) + N']' AS NewRoles
        FROM (
            SELECT MIN(TRY_CAST(j.[key] AS INT)) AS ord,
                   CASE WHEN j.value = N'bom' THEN @RoleName ELSE j.value END AS token
            FROM OPENJSON(u.Roles) j
            GROUP BY CASE WHEN j.value = N'bom' THEN @RoleName ELSE j.value END
        ) d
    ) rebuilt
    WHERE ISJSON(u.Roles) = 1
      AND ISJSON(u.RoleIds) = 1
      AND EXISTS (
            SELECT 1 FROM OPENJSON(u.RoleIds) i
            WHERE TRY_CAST(i.value AS BIGINT) = @RoleId)
      AND EXISTS (SELECT 1 FROM OPENJSON(u.Roles) x WHERE x.value = N'bom');
    PRINT N'  User role tokens repaired: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    UPDATE ra
    SET IsActive = 1,
        DisplayName = CASE ra.Path
            WHEN N'/panel/productionorderitem/bomlistby' THEN N'لیست BOM قلم سفارش ساخت'
            WHEN N'/panel/productionorderitem/getbomlistbyparentid' THEN N'دریافت لیست BOM'
            WHEN N'/panel/productionorderitem/savebom' THEN N'ذخیره BOM'
            ELSE ra.DisplayName
        END,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    FROM system.RoleAccess ra
    WHERE ra.RoleId = @RoleId
      AND ra.Path IN (
            N'/panel/productionorderitem/bomlistby',
            N'/panel/productionorderitem/getbomlistbyparentid',
            N'/panel/productionorderitem/savebom');
    PRINT N'  RoleAccess activated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    SELECT r.Id, r.Name, r.Title, r.IsActive
    FROM system.Role r
    WHERE r.Id = @RoleId;

    SELECT ra.Path, ra.DisplayName, ra.IsActive
    FROM system.RoleAccess ra
    WHERE ra.RoleId = @RoleId
    ORDER BY ra.Path;

    SELECT u.Username, j.value AS Token
    FROM system.[User] u
    CROSS APPLY OPENJSON(u.Roles) j
    WHERE ISJSON(u.RoleIds) = 1
      AND EXISTS (
            SELECT 1 FROM OPENJSON(u.RoleIds) i
            WHERE TRY_CAST(i.value AS BIGINT) = @RoleId)
      AND j.value IN (N'bom', @RoleName);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
