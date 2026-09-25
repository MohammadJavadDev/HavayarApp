/*
================================================================================
Seed_CngSystem_Menu.sql
================================================================================
منوی «سیستم توزیع سوخت (CNG)» — اطلاعات پایه + عملیات (قیمت قطعات + تعمیرات + درخواست کالا + گزارش کار + فاکتور + قرارداد تعمیر و نگهداشت).

Idempotent روی system.SystemMenu.Name = N'CngSystem'.
On UPDATE فقط Content/Title به‌روز می‌شود؛ AccessRoles/AccessRoleIds دست نخورده می‌مانند.
UTF-8 with BOM. sqlcmd -f 65001
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-system-menu';

DECLARE @MenuName NVARCHAR(150) = N'CngSystem';
DECLARE @MenuTitle NVARCHAR(150) = N'سیستم توزیع سوخت (CNG)';

DECLARE @MenuContent NVARCHAR(MAX) = N'[{"text": "اطلاعات پایه", "icon": "ki-abstract-26 ki-outline", "iconColor": "#1fc4e5", "path": "", "a_attr": {"href": ""}, "data": {"iconColor": "#1fc4e5", "path": ""}, "children": [{"text": "پارامترهای فنی", "icon": "ki-setting-2 ki-outline", "iconColor": "#1fc4e5", "path": "/panel/cng/failureinfo/list", "a_attr": {"href": "/panel/cng/failureinfo/list"}, "data": {"iconColor": "#1fc4e5", "path": "/panel/cng/failureinfo/list"}, "children": []}, {"text": "اطلاعات مشتری/جایگاه", "icon": "ki-geolocation ki-outline", "iconColor": "#50dcae", "path": "/panel/cng/stationinfo/list", "a_attr": {"href": "/panel/cng/stationinfo/list"}, "data": {"iconColor": "#50dcae", "path": "/panel/cng/stationinfo/list"}, "children": []}]}, {"text": "عملیات", "icon": "ki-wrench ki-outline", "iconColor": "#d78819", "path": "", "a_attr": {"href": ""}, "data": {"iconColor": "#d78819", "path": ""}, "children": [{"text": "مدیریت قیمت قطعات", "icon": "ki-dollar ki-outline", "iconColor": "#d78819", "path": "/panel/cng/partprice/list", "a_attr": {"href": "/panel/cng/partprice/list"}, "data": {"iconColor": "#d78819", "path": "/panel/cng/partprice/list"}, "children": []}, {"text": "تعمیرات", "icon": "ki-setting-4 ki-outline", "iconColor": "#d78819", "path": "/panel/cng/repairs/list", "a_attr": {"href": "/panel/cng/repairs/list"}, "data": {"iconColor": "#d78819", "path": "/panel/cng/repairs/list"}, "children": []}, {"text": "درخواست کالا", "icon": "ki-parcel ki-outline", "iconColor": "#d78819", "path": "/panel/cng/partrequest/list", "a_attr": {"href": "/panel/cng/partrequest/list"}, "data": {"iconColor": "#d78819", "path": "/panel/cng/partrequest/list"}, "children": []}, {"text": "گزارش کار", "icon": "ki-document ki-outline", "iconColor": "#d78819", "path": "/panel/cng/workreport/list", "a_attr": {"href": "/panel/cng/workreport/list"}, "data": {"iconColor": "#d78819", "path": "/panel/cng/workreport/list"}, "children": []}, {"text": "فاکتور", "icon": "ki-bill ki-outline", "iconColor": "#d78819", "path": "/panel/cng/invoice/list", "a_attr": {"href": "/panel/cng/invoice/list"}, "data": {"iconColor": "#d78819", "path": "/panel/cng/invoice/list"}, "children": []}, {"text": "قرارداد تعمیر و نگهداشت", "icon": "ki-calendar-tick ki-outline", "iconColor": "#d78819", "path": "/panel/cng/maintenancecontract/list", "a_attr": {"href": "/panel/cng/maintenancecontract/list"}, "data": {"iconColor": "#d78819", "path": "/panel/cng/maintenancecontract/list"}, "children": []}]}, {"text": "گزارشات", "icon": "ki-chart-simple-3 ki-outline", "iconColor": "#e5ca1f", "path": "", "a_attr": {"href": ""}, "data": {"iconColor": "#e5ca1f", "path": ""}, "children": []}]';;

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
    PRINT N'=== DONE Seed_CngSystem_Menu (اطلاعات پایه + عملیات قیمت قطعات + تعمیرات + درخواست کالا + گزارش کار + فاکتور + قرارداد تعمیر و نگهداشت) ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
