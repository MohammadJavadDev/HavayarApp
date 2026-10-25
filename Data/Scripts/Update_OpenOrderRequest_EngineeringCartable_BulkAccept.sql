/*
  نمایه «منتظر تایید مهندسی» (openorderrequest_listinfonew):
    - انتخاب چندتایی پیش‌فرض فعال
    - دکمه «تایید مهندسی» برای تایید همزمان ردیف‌های انتخاب‌شده
    - RoleAccess مسیر EngineeringAcceptBulk برای همان نقشی که تایید تکی را دارد

  اعمال:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_OpenOrderRequest_EngineeringCartable_BulkAccept.sql"

  بعد از اعمال: ری‌استارت WebApp (اکشن جدید + کش نمایه داده).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @ScriptEngineeringAcceptBulk NVARCHAR(MAX) = N'function(ctx) {
    var ids = ctx.multiSelectMode ? (ctx.primaryKeyValues || []) : (ctx.primaryKeyValue ? [ctx.primaryKeyValue] : []);
    ids = ids.map(function (v) { return parseInt(v, 10); }).filter(function (n) { return !isNaN(n) && n > 0; });
    if (!ids.length) {
        toastr.warning("لطفاً حداقل یک ردیف انتخاب کنید");
        return;
    }
    if (typeof AppSdk !== "undefined" && !AppSdk.hasRole("Sup.OpenOrderRequest.EngineeringAccept")) {
        toastr.error("شما دسترسی تایید مهندسی را ندارید");
        return;
    }
    $.confirm({
        title: "تایید مهندسی",
        content: "آیا از تایید مهندسی " + ids.length + " درخواست انتخاب‌شده اطمینان دارید؟",
        rtl: true,
        buttons: {
            confirm: {
                text: "بله",
                btnClass: "btn-success",
                action: function () {
                    var box = this;
                    var $btn = this.$$confirm.block();
                    post("/Panel/Sup/OpenOrderRequest/EngineeringAcceptBulk", { ids: ids }, function (r) {
                        $btn.block(false);
                        if (!r || !r.isSuccess) {
                            toastr.error((r && r.message) || "خطا در تایید مهندسی", "خطا");
                            return false;
                        }
                        var data = r.data || {};
                        var failed = data.failed || [];
                        var accepted = data.acceptedCount || 0;
                        if (failed.length) {
                            var detail = failed.map(function (f) { return (f.id || "") + (f.message ? ": " + f.message : ""); }).join(" | ");
                            if (accepted > 0) toastr.warning(accepted + " مورد تایید شد. ناموفق: " + detail);
                            else toastr.error(detail || "هیچ درخواستی تایید نشد", "خطا");
                        } else {
                            toastr.success("تایید مهندسی " + accepted + " درخواست با موفقیت انجام شد");
                        }
                        box.close();
                        ctx.draw();
                    });
                    return false;
                }
            },
            cancel: { text: "انصراف", btnClass: "btn-secondary" }
        }
    });
}';

    DECLARE @BtnEngineeringAcceptBulk NVARCHAR(MAX) = (
        SELECT N'id_oor_engineeringAcceptBulk' AS id, N'تایید مهندسی' AS title, N'engineeringAcceptBulk' AS dataActionName,
               N'btn-color-success' AS colorClass, N'fa fa-check' AS iconClass,
               CAST(1 AS bit) AS requiresSelection, CAST(0 AS bit) AS requiresMultiSelection,
               CAST(0 AS bit) AS useHtml, N'' AS html, @ScriptEngineeringAcceptBulk AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    IF ISJSON(@BtnEngineeringAcceptBulk) <> 1
        THROW 51020, N'JSON دکمه تایید مهندسی نامعتبر است.', 1;

    DECLARE @Name NVARCHAR(100) = N'openorderrequest_listinfonew';
    DECLARE @CurrentButtons NVARCHAR(MAX) = (
        SELECT CustomActionButtonsJson FROM system.SavedQuery WHERE Name = @Name);

    IF @CurrentButtons IS NULL
        THROW 51021, N'نمایه openorderrequest_listinfonew پیدا نشد.', 1;

    DECLARE @Kept NVARCHAR(MAX) = (
        SELECT STRING_AGG(CAST(j.[value] AS NVARCHAR(MAX)), N',')
        FROM OPENJSON(CASE WHEN ISJSON(@CurrentButtons) = 1 THEN @CurrentButtons ELSE N'[]' END) AS j
        WHERE ISNULL(JSON_VALUE(j.[value], '$.dataActionName'), N'') <> N'engineeringAcceptBulk');

    DECLARE @Buttons NVARCHAR(MAX) = JSON_MODIFY(
        CASE WHEN @Kept IS NULL THEN N'[]' ELSE N'[' + @Kept + N']' END,
        'append $',
        JSON_QUERY(@BtnEngineeringAcceptBulk));

    IF ISJSON(@Buttons) <> 1
        THROW 51022, N'JSON دکمه‌های نمایه نامعتبر شد.', 1;

    DECLARE @Actions NVARCHAR(MAX) = (
        SELECT
            a.dataActionName,
            CASE WHEN a.dataActionName = N'defaultMultiSelect' THEN CAST(1 AS bit) ELSE a.enable END AS enable,
            a.title
        FROM system.SavedQuery q
        CROSS APPLY OPENJSON(q.ActionOptions) WITH (
            dataActionName NVARCHAR(100) '$.dataActionName',
            enable BIT '$.enable',
            title NVARCHAR(200) '$.title'
        ) AS a
        WHERE q.Name = @Name
        FOR JSON PATH);

    IF ISJSON(@Actions) <> 1
        OR NOT EXISTS (
            SELECT 1 FROM OPENJSON(@Actions)
            WHERE JSON_VALUE([value], '$.dataActionName') = N'defaultMultiSelect'
              AND JSON_VALUE([value], '$.enable') = N'true')
        THROW 51023, N'فعال‌سازی انتخاب چندتایی پیش‌فرض ناموفق بود.', 1;

    DECLARE @OnRowAddedEngineering NVARCHAR(MAX) = N'function(ctx) {
    var d = ctx.rowData || {};
    var yes = function (v) { return v === true || v === 1 || v === "true" || v === "بله"; };
    var $cells = ctx.$row.children("td");
    var $select = $cells.filter(".dt-row-select-col");
    var $rowNo = $select.length ? $select.next("td") : $cells.eq(0);
    $rowNo.css("background-color", "");
    $select.css("background-color", "");
    if (yes(d.t1_HasSalesUnitConfirmation)) {
        if ($select.length) $select.css("background-color", "orange");
        else $rowNo.css("background-color", "orange");
    }
    if (yes(d.tEml_EmailSendToSupplier)) { $rowNo.css("background-color", "lightgreen"); }
    if (yes(d.t1_Changed)) { $rowNo.css("background-color", "violet"); }
    if (yes(d.t1_IsStop)) { $rowNo.css("background-color", "orangered"); }
    if (yes(d.tClr_InSupplyWindow)) { $rowNo.css("background-color", "deepskyblue"); }
}';
    DECLARE @EventsEngineering NVARCHAR(MAX) = (
        SELECT N'' AS onSelectedRow, @OnRowAddedEngineering AS onRowAdded
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    IF ISJSON(@EventsEngineering) <> 1
        THROW 51024, N'JSON رنگ ردیف نامعتبر است.', 1;

    UPDATE system.SavedQuery
    SET ActionOptions = @Actions,
        CustomActionButtonsJson = @Buttons,
        EventScriptsJson = @EventsEngineering,
        ModifiedById = 1,
        ModifiedByName = N'seed-oor-engineering-bulk-accept',
        ModifiedDateMiladiDateTime = GETDATE(),
        ModifiedDateShamsiDateTime = CONVERT(NVARCHAR(30), GETDATE(), 120)
    WHERE Name = @Name;

    INSERT INTO system.RoleAccess
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId,
        CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        N'/panel/sup/openorderrequest/engineeringacceptbulk',
        src.ActionAccessType,
        src.ActionAccessItemType,
        src.EntityName,
        N'تایید مهندسی گروهی',
        src.EntityId,
        src.RowId,
        r.Id,
        1, 1, N'seed-oor-engineering-bulk-accept', N'seed-oor-engineering-bulk-accept',
        GETDATE(), CONVERT(NVARCHAR(30), GETDATE(), 120), GETDATE(), CONVERT(NVARCHAR(30), GETDATE(), 120),
        1
    FROM system.Role AS r
    INNER JOIN system.RoleAccess AS src
        ON src.RoleId = r.Id
       AND src.Path = N'/panel/sup/openorderrequest/engineeringaccept/{id}'
    WHERE r.Name = N'Sup.OpenOrderRequest.EngineeringAccept'
      AND NOT EXISTS (
          SELECT 1
          FROM system.RoleAccess AS x
          WHERE x.RoleId = r.Id
            AND x.Path = N'/panel/sup/openorderrequest/engineeringacceptbulk');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    JSON_VALUE(j.[value], '$.dataActionName') AS ActionName,
    JSON_VALUE(j.[value], '$.enable') AS Enabled,
    JSON_VALUE(j.[value], '$.title') AS Title
FROM system.SavedQuery AS q
CROSS APPLY OPENJSON(q.ActionOptions) AS j
WHERE q.Name = N'openorderrequest_listinfonew'
  AND JSON_VALUE(j.[value], '$.dataActionName') = N'defaultMultiSelect';

SELECT
    JSON_VALUE(b.[value], '$.title') AS ButtonTitle,
    JSON_VALUE(b.[value], '$.dataActionName') AS ButtonAction
FROM system.SavedQuery AS q
CROSS APPLY OPENJSON(q.CustomActionButtonsJson) AS b
WHERE q.Name = N'openorderrequest_listinfonew';

SELECT r.Name AS RoleName, ra.Path, ra.DisplayName
FROM system.RoleAccess AS ra
INNER JOIN system.Role AS r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/sup/openorderrequest/engineeringacceptbulk';
