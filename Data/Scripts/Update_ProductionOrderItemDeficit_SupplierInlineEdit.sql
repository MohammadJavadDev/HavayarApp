/*
  نمایه productionorderitemdeficit_listinfo:
    ستون‌های تامین‌کننده (تاریخ پیش بینی تامین، توضیح تامین کننده، متولی خرید) برای نقش‌های
    SupplierEdit / IndustriesEdit / ادمین به‌صورت INPUT نمایش داده می‌شوند (مقدار قبلی داخل INPUT).
    دکمه «ذخیره تغییرات تامین» فقط رکوردهایی را می‌فرستد که مقدارشان نسبت به مقدار اولیه تغییر کرده
    (فقط فیلدهای تغییرکرده) و اکشن SaveSupplierBatch فقط همان رکوردها/فیلدها را آپدیت می‌کند.

  اعمال:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderItemDeficit_SupplierInlineEdit.sql"

  بعد از اعمال: ری‌استارت WebApp (اکشن جدید SaveSupplierBatch + کش پروفایل).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @ProfileName NVARCHAR(200) = N'productionorderitemdeficit_listinfo';

    IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
        THROW 51040, N'نمایه productionorderitemdeficit_listinfo پیدا نشد.', 1;

    -- ───────── 1) Render ستون‌ها ─────────
    DECLARE @RenderTpl NVARCHAR(MAX) = N'function (data, type, row) {
    if (type !== "display") return data;
    var canEdit = typeof AppSdk !== "undefined" && AppSdk.hasAnyRole("Pln.ProductionOrerItemDeficit.SupplierEdit", "Pln.ProductionOrerItemDeficit.IndustriesEdit");
    var safe = (data == null ? "" : String(data)).replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/''/g, "&#39;").replace(/</g, "&lt;");
    if (!canEdit) return safe;
    var btn = {BTN};
    return "<div class=''input-group input-group-sm flex-nowrap''><input type=''text'' class=''form-control form-control-sm def-sup'' data-id=''" + row.t1_Id + "'' data-prop=''{PROP}'' data-original=''" + safe + "'' value=''" + safe + "'' maxlength=''{MAX}'' autocomplete=''off'' />" + btn + "</div>";
}';

    -- دکمه آیکن تقویم (فقط برای ستون تاریخ) به‌صورت literal جاوااسکریپت
    DECLARE @DateBtn NVARCHAR(MAX) = N'"<button type=''button'' class=''btn btn-sm btn-light px-3 def-date-btn'' tabindex=''-1'' title=''انتخاب تاریخ''><i class=''bi bi-calendar3''></i></button>"';

    DECLARE @Cols NVARCHAR(MAX) = (SELECT ColumnsJson FROM system.SavedQuery WHERE Name = @ProfileName);

    DECLARE @Targets TABLE (Alliance NVARCHAR(200), Prop NVARCHAR(100), MaxLen NVARCHAR(10));
    INSERT INTO @Targets VALUES
        (N't1_SupplierUnitForecastDateShamsiDateTime', N'supplierUnitForecastDate', N'10'),
        (N't1_SupplierDescription',                   N'supplierDescription',      N'1024'),
        (N't1_PurchaseExpert',                        N'purchaseExpert',           N'64');

    DECLARE @Alliance NVARCHAR(200), @Prop NVARCHAR(100), @MaxLen NVARCHAR(10), @Idx NVARCHAR(10), @Render NVARCHAR(MAX);
    DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Alliance, Prop, MaxLen FROM @Targets;
    OPEN c;
    FETCH NEXT FROM c INTO @Alliance, @Prop, @MaxLen;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Idx = (SELECT TOP 1 j.[key] FROM OPENJSON(@Cols) AS j WHERE JSON_VALUE(j.[value], '$.Alliance') = @Alliance);
        IF @Idx IS NULL
            THROW 51041, N'ستون تامین‌کننده در ColumnsJson پیدا نشد.', 1;

        SET @Render = REPLACE(REPLACE(REPLACE(@RenderTpl, N'{PROP}', @Prop), N'{MAX}', @MaxLen),
                              N'{BTN}', CASE WHEN @Prop = N'supplierUnitForecastDate' THEN @DateBtn ELSE N'""' END);
        SET @Cols = JSON_MODIFY(@Cols, '$[' + @Idx + '].Render', @Render);
        FETCH NEXT FROM c INTO @Alliance, @Prop, @MaxLen;
    END
    CLOSE c;
    DEALLOCATE c;

    IF ISJSON(@Cols) <> 1
        THROW 51042, N'ColumnsJson نامعتبر شد.', 1;

    -- ───────── 2) onRowAdded: فلگ تغییر (هایلایت ورودی‌های تغییرکرده + data-dirty روی ردیف) + تقویم شمسی ─────────
    DECLARE @OnRowAdded NVARCHAR(MAX) = N'function(ctx) {
    var $row = ctx.$row;
    var $inputs = $row.find("input.def-sup");
    if (!$inputs.length) return;

    var refresh = function () {
        var dirty = false;
        $inputs.each(function () {
            var changed = this.value !== this.getAttribute("data-original");
            jQuery(this).toggleClass("border-warning", changed);
            if (changed) dirty = true;
        });
        if (dirty) $row.attr("data-dirty", "1"); else $row.removeAttr("data-dirty");
    };

    $inputs.off(".defsup")
        .on("input.defsup change.defsup", refresh)
        .on("click.defsup", function (e) { e.stopPropagation(); });

    $inputs.filter("[data-prop=supplierUnitForecastDate]").each(function () {
        var $d = jQuery(this);
        if ($d.data("defPicker") || !jQuery.fn.persianDatepicker) return;
        var picker = $d.persianDatepicker({
            format: "YYYY/MM/DD",
            autoClose: true,
            initialValue: false,
            initialValueType: "persian",
            persianDigit: false,
            onSelect: function () { setTimeout(refresh, 0); }
        });
        $d.data("defPicker", true);
        // تقویم فقط با آیکن باز شود؛ کلیک روی خود INPUT فقط برای تایپ دستی است
        $d.off("focus click");

        $d.closest(".input-group").find(".def-date-btn").off(".defsup").on("click.defsup", function (e) {
            e.stopPropagation();
            picker.show();
            jQuery("body").off("click.defclose").on("click.defclose", function (ev) {
                var $t = jQuery(ev.target);
                if ($t.closest(picker.model.view.$container).length || $t.closest(".def-date-btn").length) return;
                picker.hide();
                jQuery("body").off("click.defclose");
            });
        });
    });

    refresh();
}';

    DECLARE @Events NVARCHAR(MAX) = (SELECT EventScriptsJson FROM system.SavedQuery WHERE Name = @ProfileName);
    IF ISJSON(@Events) <> 1 SET @Events = N'{"onSelectedRow":"","onRowAdded":""}';
    SET @Events = JSON_MODIFY(@Events, '$.onRowAdded', @OnRowAdded);

    -- ───────── 3) دکمه ذخیره ─────────
    DECLARE @SaveScript NVARCHAR(MAX) = N'function(ctx) {
    if (typeof AppSdk === "undefined" || !AppSdk.hasAnyRole("Pln.ProductionOrerItemDeficit.SupplierEdit", "Pln.ProductionOrerItemDeficit.IndustriesEdit")) {
        toastr.error("شما دسترسی ویرایش اطلاعات تامین‌کننده را ندارید");
        return;
    }

    var changes = {};
    jQuery(ctx.table.table().node()).find("input.def-sup").each(function () {
        if (this.value === this.getAttribute("data-original")) return;
        var id = this.getAttribute("data-id");
        if (!changes[id]) changes[id] = { id: Number(id) };
        changes[id][this.getAttribute("data-prop")] = this.value;
    });

    var items = Object.keys(changes).map(function (k) { return changes[k]; });
    if (!items.length) {
        toastr.info("تغییری برای ذخیره وجود ندارد");
        return;
    }

    var $btn = ctx.$btn.block();
    post("/Panel/Pln/ProductionOrderItemDeficit/SaveSupplierBatch", items, function (r) {
        $btn.block(false);
        if (!r || !r.isSuccess) {
            toastr.error((r && r.message) || "خطا در ذخیره سازی", "خطا");
            return;
        }
        toastr.success(items.length + " رکورد ذخیره شد");
        ctx.draw();
    });
}';

    DECLARE @Btn NVARCHAR(MAX) = (
        SELECT
            N'poi_deficit_save_supplier' AS id,
            N'ذخیره تغییرات تامین' AS title,
            N'saveSupplierBatch' AS dataActionName,
            N'btn-color-success' AS colorClass,
            N'bi bi-check2-all' AS iconClass,
            CAST(0 AS bit) AS requiresSelection,
            CAST(0 AS bit) AS requiresMultiSelection,
            CAST(0 AS bit) AS useHtml,
            N'' AS html,
            @SaveScript AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    IF ISJSON(@Btn) <> 1
        THROW 51043, N'JSON دکمه نامعتبر است.', 1;

    DECLARE @CurrentButtons NVARCHAR(MAX) = (SELECT CustomActionButtonsJson FROM system.SavedQuery WHERE Name = @ProfileName);

    DECLARE @Kept NVARCHAR(MAX) = (
        SELECT STRING_AGG(CAST(j.[value] AS NVARCHAR(MAX)), N',')
        FROM OPENJSON(CASE WHEN ISJSON(@CurrentButtons) = 1 THEN @CurrentButtons ELSE N'[]' END) AS j
        WHERE ISNULL(JSON_VALUE(j.[value], '$.dataActionName'), N'') <> N'saveSupplierBatch');

    DECLARE @Buttons NVARCHAR(MAX) = JSON_MODIFY(
        CASE WHEN @Kept IS NULL THEN N'[]' ELSE N'[' + @Kept + N']' END,
        'append $',
        JSON_QUERY(@Btn));

    IF ISJSON(@Buttons) <> 1
        THROW 51044, N'JSON دکمه‌های نمایه نامعتبر شد.', 1;

    UPDATE system.SavedQuery
    SET ColumnsJson = @Cols,
        EventScriptsJson = @Events,
        CustomActionButtonsJson = @Buttons,
        ModifiedById = 1,
        ModifiedByName = N'seed-poi-deficit-supplier-inline',
        ModifiedDateMiladiDateTime = GETDATE(),
        ModifiedDateShamsiDateTime = CONVERT(NVARCHAR(30), GETDATE(), 120)
    WHERE Name = @ProfileName;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- بررسی
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       CASE WHEN JSON_VALUE(j.value, '$.Render') LIKE N'%def-sup%' THEN 1 ELSE 0 END AS HasInputRender
FROM system.SavedQuery AS q
CROSS APPLY OPENJSON(q.ColumnsJson) AS j
WHERE q.Name = N'productionorderitemdeficit_listinfo'
  AND JSON_VALUE(j.value, '$.Alliance') IN (N't1_SupplierUnitForecastDateShamsiDateTime', N't1_SupplierDescription', N't1_PurchaseExpert');

SELECT JSON_VALUE(j.value, '$.dataActionName') AS dataActionName, JSON_VALUE(j.value, '$.title') AS title
FROM system.SavedQuery AS q
CROSS APPLY OPENJSON(q.CustomActionButtonsJson) AS j
WHERE q.Name = N'productionorderitemdeficit_listinfo';
