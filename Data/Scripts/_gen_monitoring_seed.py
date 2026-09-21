# -*- coding: utf-8 -*-
"""One-off helper to emit Seed_AfterSales_OrderDetailSerial_Monitoring_DataProfiles.sql"""
from pathlib import Path
import json

OUT = Path(__file__).with_name("Seed_AfterSales_OrderDetailSerial_Monitoring_DataProfiles.sql")

def col(table, column, display, alliance, address, type_name, type_id, visible=True, pk=False, sort=None, width=120):
    return {
        "TableName": table,
        "ColumnName": column,
        "DisplayName": display,
        "Alliance": alliance,
        "Address": address,
        "SystemTypeName": type_name,
        "SelectIndex": 0,
        "Visible": visible,
        "PrimaryKey": pk,
        "SystemType": type_id,
        "SortDirection": sort,
        "SortOrder": None,
        "GroupBy": False,
        "Options": [],
        "OptionSetting": None,
        "Aggregate": None,
        "Render": None,
        "IsCustom": False,
        "CustomColType": None,
        "HtmlTemplate": None,
        "BtnConfig": None,
        "InputConfig": None,
        "Width": width,
        "Filterable": True,
        "Sortable": True,
        "ClassName": "",
    }

columns = [
    col("Sale.OrderDetailSerial", "Id", "شناسه", "t1_Id", "[t1].[Id]", "Long", 6, False, True, 1, 80),
    col("Sale.OrderDetailSerial", "IndustrialConfirm", "تایید صنایع", "t1_IndustrialConfirm", "[t1].[IndustrialConfirm]", "Boolean", 1, True, False, None, 80),
    col("SLS.Customer", "Code", "کد مشتری", "t5_Code", "[t5].[Code]", "Int", 7, True, False, None, 80),
    col("Gnr.Party", "FullName", "مشتری", "t6_FullName", "[t6].[FullName]", "String", 0, True, False, None, 180),
    col("Sale.Order", "OrderNumber", "شماره سند", "t3_OrderNumber", "[t3].[OrderNumber]", "Long", 6, True, False, None, 80),
    col("Sale.OrderDetail", "Seq", "ردیف", "t2_Seq", "[t2].[Seq]", "Int", 7, True, False, None, 70),
    col("Inv.Part", "Code", "کد کالا", "t4_Code", "[t4].[Code]", "String", 0, True, False, None, 100),
    col("Inv.Part", "Name", "عنوان کالا", "t4_Name", "[t4].[Name]", "String", 0, True, False, None, 220),
    col("Pln.Project", "Name", "نام پروژه", "t7_Name", "[t7].[Name]", "String", 0, True, False, None, 180),
    col("Sale.OrderDetailSerial", "Serial", "سریال", "t1_Serial", "[t1].[Serial]", "String", 0, True, False, None, 100),
    col("Sale.OrderDetailSerial", "Model", "مدل", "t1_Model", "[t1].[Model]", "String", 0, True, False, None, 100),
    col("Sale.OrderDetailSerial", "ProductionOrderNumber", "ش س ساخت", "t1_ProductionOrderNumber", "[t1].[ProductionOrderNumber]", "String", 0, True, False, None, 90),
    col("Sale.OrderDetailSerial", "Runtime", "کارکرد", "t1_Runtime", "[t1].[Runtime]", "Long", 6, True, False, None, 80),
    col("Sale.OrderDetailSerial", "HoursDaily", "ساعت کارکرد", "t1_HoursDaily", "[t1].[HoursDaily]", "String", 0, True, False, None, 90),
    col("Sale.OrderDetailSerial", "GuaranteeTime", "گارانتی ساعتی", "t1_GuaranteeTime", "[t1].[GuaranteeTime]", "Int", 7, True, False, None, 90),
    col("Sale.OrderDetailSerial", "GuaranteeSendDay", "گارانتی ارسال", "t1_GuaranteeSendDay", "[t1].[GuaranteeSendDay]", "Int", 7, True, False, None, 100),
    col("Sale.OrderDetailSerial", "GuaranteeLaunchDay", "گارانتی راه اندازی", "t1_GuaranteeLaunchDay", "[t1].[GuaranteeLaunchDay]", "Int", 7, True, False, None, 110),
    col("Sale.OrderDetailSerial", "HasInsurance", "بیمه", "t1_HasInsurance", "[t1].[HasInsurance]", "Boolean", 1, True, False, None, 70),
    col("Sale.OrderDetailSerial", "SendShamsiDate", "تاریخ ارسال", "t1_SendShamsiDate", "[t1].[SendShamsiDate]", "DateShamsi", 5, True, False, None, 100),
    col("Sale.OrderDetailSerial", "SendTime", "ساعت ارسال", "t1_SendTime", "[t1].[SendTime]", "String", 0, True, False, None, 80),
    col("Sale.OrderDetailSerial", "ExitFactoryShamsiDate", "ت سند خروج", "t1_ExitFactoryShamsiDate", "[t1].[ExitFactoryShamsiDate]", "String", 0, True, False, None, 100),
    col("Sale.OrderDetailSerial", "LaunchShamsiDate", "تاریخ راه اندازی", "t1_LaunchShamsiDate", "[t1].[LaunchShamsiDate]", "DateShamsi", 5, True, False, None, 110),
    col("Sale.OrderDetailSerial", "IndustrialComment", "توضیحات صنایع", "t1_IndustrialComment", "[t1].[IndustrialComment]", "String", 0, True, False, None, 180),
    col("Sale.OrderDetailSerial", "InvComment", "توضیحات انبار", "t1_InvComment", "[t1].[InvComment]", "String", 0, True, False, None, 180),
    col("Sale.OrderDetailSerial", "IsProductExited", "کالا خارج شده؟", "t1_IsProductExited",
        "CASE WHEN [t1].[FinalExitMiladiDate] IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END",
        "Boolean", 1, True, False, None, 110),
]

columns_json = json.dumps(columns, ensure_ascii=False, separators=(",", ":"))

base_select = """SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[IndustrialConfirm] AS [t1_IndustrialConfirm],
    [t5].[Code] AS [t5_Code],
    [t6].[FullName] AS [t6_FullName],
    [t3].[OrderNumber] AS [t3_OrderNumber],
    [t2].[Seq] AS [t2_Seq],
    [t4].[Code] AS [t4_Code],
    [t4].[Name] AS [t4_Name],
    [t7].[Name] AS [t7_Name],
    [t1].[Serial] AS [t1_Serial],
    [t1].[Model] AS [t1_Model],
    [t1].[ProductionOrderNumber] AS [t1_ProductionOrderNumber],
    [t1].[Runtime] AS [t1_Runtime],
    [t1].[HoursDaily] AS [t1_HoursDaily],
    [t1].[GuaranteeTime] AS [t1_GuaranteeTime],
    [t1].[GuaranteeSendDay] AS [t1_GuaranteeSendDay],
    [t1].[GuaranteeLaunchDay] AS [t1_GuaranteeLaunchDay],
    [t1].[HasInsurance] AS [t1_HasInsurance],
    [t1].[SendShamsiDate] AS [t1_SendShamsiDate],
    [t1].[SendTime] AS [t1_SendTime],
    [t1].[ExitFactoryShamsiDate] AS [t1_ExitFactoryShamsiDate],
    [t1].[LaunchShamsiDate] AS [t1_LaunchShamsiDate],
    [t1].[IndustrialComment] AS [t1_IndustrialComment],
    [t1].[InvComment] AS [t1_InvComment],
    CASE WHEN [t1].[FinalExitMiladiDate] IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS [t1_IsProductExited]
FROM [Sale].[OrderDetailSerial] AS [t1]
LEFT JOIN [Sale].[OrderDetail] AS [t2] ON [t1].[OrderDetailId] = [t2].[Id]
LEFT JOIN [Sale].[Order] AS [t3] ON [t2].[Sale_OrderId] = [t3].[Id]
LEFT JOIN [Inv].[Part] AS [t4] ON [t2].[PartId] = [t4].[Id]
LEFT JOIN [SLS].[Customer] AS [t5] ON [t3].[CustomerId] = [t5].[Id]
LEFT JOIN [Gnr].[Party] AS [t6] ON [t5].[PartyId] = [t6].[Id]
LEFT JOIN [Pln].[Project] AS [t7] ON [t1].[PlaningProjectId] = [t7].[Id]"""

industries = base_select + "\nWHERE [t1].[HtsId] > 3488"
warehouse = base_select + "\nWHERE [t1].[IndustrialConfirm] = 1"

action_options = json.dumps([
    {"dataActionName": "new", "enable": False, "title": "جدید"},
    {"dataActionName": "edit", "enable": True, "title": "ویرایش"},
    {"dataActionName": "delete", "enable": False, "title": "حذف"},
    {"dataActionName": "exportExcell", "enable": True, "title": "خروجی اکسل"},
], ensure_ascii=False, separators=(",", ":"))

edit_js = (
    "function(ctx) {\n"
    "    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && ctx.selectedRow.t1_Id));\n"
    "    if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }\n"
    "    appController.addPage('/Panel/Sale/OrderDetailSerial/MonitoringEdit?id=' + id, true, 'ویرایش مانیتورینگ');\n"
    "}"
)
confirm_js = (
    "function(ctx) {\n"
    "    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && ctx.selectedRow.t1_Id));\n"
    "    if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }\n"
    "    Swal.fire({ title: 'تایید / عدم تایید صنایع', text: 'آیا از تغییر وضعیت تایید صنایع مطمئن هستید؟', icon: 'warning', showCancelButton: true, confirmButtonText: 'بله', cancelButtonText: 'خیر' }).then(function(result) {\n"
    "        if (!result.isConfirmed) return;\n"
    "        post('/Panel/Sale/OrderDetailSerial/IndustrialConfirm?id=' + id, {}, function(r) {\n"
    "            if (!r || !r.isSuccess) { toastr.error((r && r.message) || 'خطا'); return; }\n"
    "            toastr.success('وضعیت تایید صنایع به‌روزرسانی شد');\n"
    "            if (ctx.draw) ctx.draw();\n"
    "        });\n"
    "    });\n"
    "}"
)
parts_js = (
    "function(ctx) {\n"
    "    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && ctx.selectedRow.t1_Id));\n"
    "    if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }\n"
    "    var $btn = ctx.$btn ? ctx.$btn : $('body');\n"
    "    $btn.block();\n"
    "    post('/Panel/Sale/OrderDetailSerial/GetProjectParts?id=' + id, {}, function(r) {\n"
    "        $btn.block(false);\n"
    "        if (!r || !r.isSuccess) { toastr.error((r && r.message) || 'خطا در دریافت اقلام پروژه'); return; }\n"
    "        var payload = r.data || {};\n"
    "        var rows = payload.items || [];\n"
    "        var missing = payload.connectionMissing;\n"
    "        var extra = payload.message || '';\n"
    "        var html = '<div style=\"max-height:420px;overflow:auto\"><table class=\"table table-sm table-striped table-bordered\"><thead><tr><th>انبار</th><th>تاریخ</th><th>شماره سند</th><th>کد کالا</th><th>عنوان کالا</th><th>تعداد</th></tr></thead><tbody>';\n"
    "        if (!rows.length) {\n"
    "            html += '<tr><td colspan=\"6\" class=\"text-center\">' + (missing ? (extra || 'اتصال راهکاران در دسترس نیست') : 'اقلامی یافت نشد') + '</td></tr>';\n"
    "        } else {\n"
    "            rows.forEach(function(p) {\n"
    "                html += '<tr><td>' + (p.stockName || '') + '</td><td>' + (p.vchDate || '') + '</td><td>' + (p.vchNum || '') + '</td><td>' + (p.partCode || '') + '</td><td>' + (p.partName || '') + '</td><td>' + (p.qty != null ? p.qty : '') + '</td></tr>';\n"
    "            });\n"
    "        }\n"
    "        html += '</tbody></table></div>';\n"
    "        $.confirm({ title: 'اقلام پروژه', content: html, rtl: true, columnClass: 'large', buttons: { close: { text: 'بستن' } } });\n"
    "    });\n"
    "}"
)

buttons = [
    {
        "id": "as_mon_edit",
        "title": "ویرایش",
        "dataActionName": "editRow",
        "colorClass": "btn-color-primary",
        "iconClass": "ki-pencil ki-outline",
        "requiresSelection": True,
        "requiresMultiSelection": False,
        "useHtml": False,
        "html": "",
        "actionScript": edit_js,
    },
    {
        "id": "as_mon_industrial",
        "title": "تایید / عدم تایید صنایع",
        "dataActionName": "industrialConfirm",
        "colorClass": "btn-color-success",
        "iconClass": "ki-check ki-outline",
        "requiresSelection": True,
        "requiresMultiSelection": False,
        "useHtml": False,
        "html": "",
        "actionScript": confirm_js,
    },
    {
        "id": "as_mon_parts",
        "title": "اقلام پروژه",
        "dataActionName": "projectParts",
        "colorClass": "btn-color-info",
        "iconClass": "ki-some-files ki-outline",
        "requiresSelection": True,
        "requiresMultiSelection": False,
        "useHtml": False,
        "html": "",
        "actionScript": parts_js,
    },
]
buttons_json = json.dumps(buttons, ensure_ascii=False, separators=(",", ":"))

def sql_n(s: str) -> str:
    return "N'" + s.replace("'", "''") + "'"

sql = f"""/*
Seed_AfterSales_OrderDetailSerial_Monitoring_DataProfiles.sql
دو نمایه مانیتورینگ فروش روی صفحه Monitoring (نه List / پیوست سریال).
Idempotent — upsert by Name + rebuild RoleAccess for these two profiles + monitoring actions.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-monitoring';
    DECLARE @EntityLower NVARCHAR(200) = N'entities.app.sale.orderdetailserial';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sale.OrderDetailSerial';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}}';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{{"onSelectedRow":"","onRowAdded":""}}';
    DECLARE @ActionOptions NVARCHAR(MAX) = {sql_n(action_options)};
    DECLARE @CustomButtons NVARCHAR(MAX) = {sql_n(buttons_json)};
    DECLARE @ColumnsJson NVARCHAR(MAX) = {sql_n(columns_json)};

    IF OBJECT_ID('tempdb..#MonProfiles') IS NOT NULL DROP TABLE #MonProfiles;
    CREATE TABLE #MonProfiles
    (
        Name NVARCHAR(150) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        SavedQueryId BIGINT NULL
    );

    INSERT INTO #MonProfiles (Name, Title, CustomQuery) VALUES
        (N'AfterSales_OrderDetailSerial_Monitoring_Industries', N'مانیتورینگ فروش - صنایع', {sql_n(industries)}),
        (N'AfterSales_OrderDetailSerial_Monitoring_Warehouse', N'مانیتورینگ فروش - انبار', {sql_n(warehouse)});

    DECLARE @PName NVARCHAR(150), @PTitle NVARCHAR(200), @PSelect NVARCHAR(MAX), @PQueryJson NVARCHAR(MAX), @PId BIGINT;
    DECLARE mon_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, CustomQuery FROM #MonProfiles;
    OPEN mon_cur;
    FETCH NEXT FROM mon_cur INTO @PName, @PTitle, @PSelect;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @PQueryJson = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);

        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
        BEGIN
            UPDATE system.SavedQuery
            SET Title = @PTitle,
                QueryJson = @PQueryJson,
                ColumnsJson = @ColumnsJson,
                EntityFullName = @EntityLower,
                Mode = 1,
                Type = 1,
                ActionOptions = @ActionOptions,
                CustomActionButtonsJson = @CustomButtons,
                EventScriptsJson = @EventScripts,
                DiagramJson = N'{{}}',
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi,
                IsActive = 1
            WHERE Name = @PName;
        END
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
                (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
                 ActionOptions, EntityFullName, Type, Mode,
                 CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
                 ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@PName, @PTitle, @PQueryJson, @ColumnsJson, N'{{}}', @CustomButtons, @EventScripts,
                 @ActionOptions, @EntityLower, 1, 1,
                 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
        END

        UPDATE #MonProfiles SET SavedQueryId = (SELECT Id FROM system.SavedQuery WHERE Name = @PName) WHERE Name = @PName;
        FETCH NEXT FROM mon_cur INTO @PName, @PTitle, @PSelect;
    END
    CLOSE mon_cur;
    DEALLOCATE mon_cur;

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN #MonProfiles p ON p.SavedQueryId = ra.RowId
    WHERE ra.ActionAccessType = 3;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(p.SavedQueryId AS nvarchar(20)),
        3, 7, @EntityPascal, p.Title, NULL, p.SavedQueryId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #MonProfiles p
    INNER JOIN system.Role r ON r.Name IN (
        N'Sale.AfterSales', N'ShowAllMenus',
        N'Sale.AfterSales.Monitoring.View',
        N'Sale.AfterSales.Monitoring.Edit',
        N'Sale.AfterSales.Monitoring.IndustrialConfirm',
        N'Sale.AfterSales.Monitoring.Exit',
        N'Sale.AfterSales.Monitoring.Inventory'
    );

    IF OBJECT_ID('tempdb..#MonActionMap') IS NOT NULL DROP TABLE #MonActionMap;
    CREATE TABLE #MonActionMap
    (
        RoleName NVARCHAR(200) NOT NULL,
        Path NVARCHAR(300) NOT NULL,
        ActionAccessType INT NOT NULL,
        ActionAccessItemType INT NOT NULL
    );

    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sale/orderdetailserial/monitoring', 1, 1),
        (N'/panel/sale/orderdetailserial/fetchdata', 2, 2)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'), (N'Sale.AfterSales'),
        (N'Sale.AfterSales.Monitoring.View'),
        (N'Sale.AfterSales.Monitoring.Edit'),
        (N'Sale.AfterSales.Monitoring.IndustrialConfirm'),
        (N'Sale.AfterSales.Monitoring.Exit'),
        (N'Sale.AfterSales.Monitoring.Inventory')
    ) r(RoleName);

    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sale/orderdetailserial/monitoringedit', 1, 5),
        (N'/panel/sale/orderdetailserial/monitoringsave', 2, 5),
        (N'/panel/sale/orderdetailserial/getprojectparts', 2, 1000),
        (N'/panel/sale/orderdetailserial/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'), (N'Sale.AfterSales'),
        (N'Sale.AfterSales.Monitoring.Edit'),
        (N'Sale.AfterSales.Monitoring.Exit'),
        (N'Sale.AfterSales.Monitoring.Inventory')
    ) r(RoleName);

    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, N'/panel/sale/orderdetailserial/getprojectparts', 2, 1000
    FROM (VALUES (N'Sale.AfterSales.Monitoring.IndustrialConfirm')) r(RoleName);

    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, N'/panel/sale/orderdetailserial/exporttoexcel', 2, 0
    FROM (VALUES (N'Sale.AfterSales.Monitoring.IndustrialConfirm')) r(RoleName);

    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, N'/panel/sale/orderdetailserial/industrialconfirm', 2, 1000
    FROM (VALUES (N'ShowAllMenus'), (N'Sale.AfterSales.Monitoring.IndustrialConfirm')) r(RoleName);

    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, N'/panel/sale/orderdetailserial/monitoringexitaccess', 1, 1000
    FROM (VALUES (N'ShowAllMenus'), (N'Sale.AfterSales.Monitoring.Exit')) r(RoleName);

    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, N'/panel/sale/orderdetailserial/monitoringinventoryaccess', 1, 1000
    FROM (VALUES (N'ShowAllMenus'), (N'Sale.AfterSales.Monitoring.Inventory')) r(RoleName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.ActionAccessType, a.ActionAccessItemType, @EntityPascal, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #MonActionMap a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE r.Name = N'Sale.AfterSales'
      AND ra.Path = N'/panel/sale/orderdetailserial/industrialconfirm';

    COMMIT TRANSACTION;
    PRINT N'=== DONE monitoring DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT Id, Name, Title, EntityFullName,
       CASE WHEN ISJSON(ColumnsJson) = 1 THEN 1 ELSE 0 END AS ColumnsValid,
       CASE WHEN ISJSON(CustomActionButtonsJson) = 1 THEN 1 ELSE 0 END AS ButtonsValid
FROM system.SavedQuery
WHERE Name IN (
    N'AfterSales_OrderDetailSerial_Monitoring_Industries',
    N'AfterSales_OrderDetailSerial_Monitoring_Warehouse',
    N'AfterSales_OrderDetailSerial_List'
);

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.RowId
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path LIKE N'dataProfile_%'
  AND ra.RowId IN (
      SELECT Id FROM system.SavedQuery
      WHERE Name IN (
          N'AfterSales_OrderDetailSerial_Monitoring_Industries',
          N'AfterSales_OrderDetailSerial_Monitoring_Warehouse'
      )
  )
ORDER BY r.Name, ra.Path;
"""

OUT.write_text(sql, encoding="utf-8-sig")
print(f"Wrote {OUT} ({OUT.stat().st_size} bytes, utf-8-sig)")

PATCH = Path(__file__).with_name("Patch_AfterSales_OrderDetailSerial_Monitoring_ColumnTitles.sql")
patch = f"""/*
Patch_AfterSales_OrderDetailSerial_Monitoring_ColumnTitles.sql
فقط ColumnsJson نمایه‌های مانیتورینگ فروش — عنوان ستون = کپشن HTS.
Apply: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i this file
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @ColumnsJson NVARCHAR(MAX) = {sql_n(columns_json)};

    UPDATE system.SavedQuery
    SET ColumnsJson = @ColumnsJson,
        ModifiedById = 1,
        ModifiedByName = N'seed-aftersales-monitoring-titles',
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (
        N'AfterSales_OrderDetailSerial_Monitoring_Industries',
        N'AfterSales_OrderDetailSerial_Monitoring_Warehouse'
    );

    IF @@ROWCOUNT <> 2
        THROW 50001, N'expected 2 monitoring SavedQuery rows', 1;

    COMMIT TRANSACTION;
    PRINT N'=== DONE monitoring column titles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT q.Name,
       JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name IN (
    N'AfterSales_OrderDetailSerial_Monitoring_Industries',
    N'AfterSales_OrderDetailSerial_Monitoring_Warehouse'
)
ORDER BY q.Name, CAST(j.[key] AS int);
"""
PATCH.write_text(patch, encoding="utf-8-sig")
print(f"Wrote {PATCH} ({PATCH.stat().st_size} bytes, utf-8-sig)")
