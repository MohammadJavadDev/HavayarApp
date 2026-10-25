/*
  نمایه productionorderitemdeficit_listinfo:
    دکمه «حذف همه» — دسترسی در اکشن کنترلر برای گروه Pln.ProductionOrerItemDeficit.ImportExcell

  اعمال:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderItemDeficit_DeleteAllButton.sql"

  بعد از اعمال: ری‌استارت WebApp تا اکشن DeleteAll و کش نقش‌ها دیده شود.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @RoleName NVARCHAR(200) = N'Pln.ProductionOrerItemDeficit.ImportExcell';
    DECLARE @ProfileName NVARCHAR(200) = N'productionorderitemdeficit_listinfo';
    DECLARE @Path NVARCHAR(300) = N'/panel/pln/productionorderitemdeficit/deleteall';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RoleName)
        THROW 51030, N'نقش Pln.ProductionOrerItemDeficit.ImportExcell پیدا نشد.', 1;

    IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
        THROW 51031, N'نمایه productionorderitemdeficit_listinfo پیدا نشد.', 1;

    DECLARE @Script NVARCHAR(MAX) = N'function(ctx) {
    if (typeof AppSdk === "undefined" || !AppSdk.hasAnyRole("Pln.ProductionOrerItemDeficit.ImportExcell")) {
        toastr.error("شما دسترسی حذف همه را ندارید");
        return;
    }
    Swal.fire({
        title: "حذف همه",
        text: "تمامی اطلاعات این جدول حذف می‌شود. ادامه می‌دهید؟",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "حذف همه",
        cancelButtonText: "انصراف"
    }).then(function (result) {
        if (!result.isConfirmed) return;
        var $btn = ctx.$btn.block();
        post("/Panel/Pln/ProductionOrderItemDeficit/DeleteAll", {}, function (r) {
            $btn.block(false);
            if (!r || !r.isSuccess) {
                toastr.error((r && r.message) || "خطا در حذف اطلاعات", "خطا");
                return;
            }
            toastr.success("تمامی اطلاعات حذف شد");
            if (ctx.draw) ctx.draw();
        });
    });
}';

    DECLARE @Btn NVARCHAR(MAX) = (
        SELECT
            N'poi_deficit_delete_all' AS id,
            N'حذف همه' AS title,
            N'deleteAll' AS dataActionName,
            N'btn-color-danger' AS colorClass,
            N'bi bi-trash' AS iconClass,
            CAST(0 AS bit) AS requiresSelection,
            CAST(0 AS bit) AS requiresMultiSelection,
            CAST(0 AS bit) AS useHtml,
            N'' AS html,
            @Script AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    IF ISJSON(@Btn) <> 1
        THROW 51032, N'JSON دکمه حذف همه نامعتبر است.', 1;

    DECLARE @CurrentButtons NVARCHAR(MAX) = (
        SELECT CustomActionButtonsJson FROM system.SavedQuery WHERE Name = @ProfileName);

    DECLARE @Kept NVARCHAR(MAX) = (
        SELECT STRING_AGG(CAST(j.[value] AS NVARCHAR(MAX)), N',')
        FROM OPENJSON(CASE WHEN ISJSON(@CurrentButtons) = 1 THEN @CurrentButtons ELSE N'[]' END) AS j
        WHERE ISNULL(JSON_VALUE(j.[value], '$.dataActionName'), N'') <> N'deleteAll');

    DECLARE @Buttons NVARCHAR(MAX) = JSON_MODIFY(
        CASE WHEN @Kept IS NULL THEN N'[]' ELSE N'[' + @Kept + N']' END,
        'append $',
        JSON_QUERY(@Btn));

    IF ISJSON(@Buttons) <> 1
        THROW 51033, N'JSON دکمه‌های نمایه نامعتبر شد.', 1;

    UPDATE system.SavedQuery
    SET CustomActionButtonsJson = @Buttons,
        ModifiedById = 1,
        ModifiedByName = N'seed-poi-deficit-delete-all',
        ModifiedDateMiladiDateTime = GETDATE(),
        ModifiedDateShamsiDateTime = CONVERT(NVARCHAR(30), GETDATE(), 120)
    WHERE Name = @ProfileName;

    INSERT INTO system.RoleAccess
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId,
        CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        @Path,
        2,
        6,
        N'Entities.App.Pln.ProductionOrderItemDeficit',
        N'حذف همه',
        NULL,
        NULL,
        r.Id,
        1, 1, N'seed-poi-deficit-delete-all', N'seed-poi-deficit-delete-all',
        GETDATE(), CONVERT(NVARCHAR(30), GETDATE(), 120), GETDATE(), CONVERT(NVARCHAR(30), GETDATE(), 120),
        1
    FROM system.Role AS r
    WHERE r.Name = @RoleName
      AND NOT EXISTS (
          SELECT 1
          FROM system.RoleAccess AS x
          WHERE x.RoleId = r.Id
            AND x.Path = @Path);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    q.Name,
    JSON_VALUE(j.value, '$.dataActionName') AS dataActionName,
    JSON_VALUE(j.value, '$.title') AS title
FROM system.SavedQuery AS q
CROSS APPLY OPENJSON(q.CustomActionButtonsJson) AS j
WHERE q.Name = N'productionorderitemdeficit_listinfo';

SELECT r.Name AS RoleName, ra.Path, ra.DisplayName, ra.ActionAccessType, ra.ActionAccessItemType
FROM system.RoleAccess AS ra
INNER JOIN system.Role AS r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/pln/productionorderitemdeficit/deleteall';
