/*
================================================================================
Update_OpenOrderRequest_DataProfiles_EmailToSupplier.sql  (WP1 / D22 / D41)
================================================================================
جراحی روی نمایه‌های موجود درخواست باز — HtsParity را ویرایش نمی‌کند.

پیش‌نیاز:
  1) جدول Sup.OpenOrderRequestEmailToSupplier (migration یا Add_…_Table.sql)
  2) Update_OpenOrderRequest_DataProfiles_HtsParity.sql قبلاً اجرا شده باشد
     (placeholder CROSS APPLY [tEml] + TODO رنگ سبز + دکمه پرسنل روی تنظیمات)

کارها:
  الف) CROSS APPLY placeholder [tEml] → OUTER APPLY آخرین ارسال
       (SendMiladiDateTime DESC) روی همه نمایه‌های OpenOrderRequest که placeholder دارند
  ب) رنگ lightgreen ردیف ارسال‌شده از کامنت خارج می‌شود
  ج) دکمه چندانتخابی «ارسال به پیمانکار» فقط روی vw_openRequestConfig
     (آینه «افزودن درخواست کنندگان و ذینفعان»؛ نقش ConfigManage یا SupplyAndPurchase)

Idempotent. SQL را اجرا نکنید مگر پس از استقرار کد + جدول.
پس از اجرا: Restart WebApp / پاک‌کردن Redis DataTableProfileCacheDb
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'Sup.OpenOrderRequestEmailToSupplier', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Sup.OpenOrderRequestEmailToSupplier وجود ندارد. ابتدا migration یا Add_OpenOrderRequestEmailToSupplier_Table.sql را اجرا کنید.', 16, 1);
    RETURN;
END

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'wp1-emailtosupplier-profile-extras';

DECLARE @Placeholder NVARCHAR(MAX) = N'CROSS APPLY (
    SELECT CAST(0 AS bit) AS [EmailSendToSupplier],
           CAST(NULL AS nvarchar(500)) AS [CompanyNameEmailSended]
) AS [tEml]';

DECLARE @OuterApply NVARCHAR(MAX) = N'OUTER APPLY (
    SELECT TOP (1) CAST(1 AS bit) AS [EmailSendToSupplier], [ep].[FullName] AS [CompanyNameEmailSended]
    FROM [Sup].[OpenOrderRequestEmailToSupplier] AS [e]
    LEFT JOIN [Gnr].[Supplier] AS [es] ON [es].[Id] = [e].[SupplierId]
    LEFT JOIN [Gnr].[Party]    AS [ep] ON [ep].[Id] = [es].[PartyId]
    WHERE [e].[OpenOrderRequestId] = [t1].[Id]
    ORDER BY [e].[SendMiladiDateTime] DESC, [e].[Id] DESC
) AS [tEml]';

DECLARE @ColorCommented NVARCHAR(300) =
    N'// if (yes(d.tEml_EmailSendToSupplier)) { $c1.css("background-color", "lightgreen"); }';
DECLARE @ColorLive NVARCHAR(300) =
    N'if (yes(d.tEml_EmailSendToSupplier)) { $c1.css("background-color", "lightgreen"); }';

DECLARE @ScriptSendToSupplier NVARCHAR(MAX) = N'function(ctx) {
    var ids = ctx.multiSelectMode ? (ctx.primaryKeyValues || []) : (ctx.primaryKeyValue ? [ctx.primaryKeyValue] : []);
    if (!ids.length) {
        toastr.warning("لطفاً حداقل یک ردیف انتخاب کنید");
        return;
    }
    var rows = (ctx.selectedRows && ctx.selectedRows.length) ? ctx.selectedRows
        : (ctx.selectedRow ? [ctx.selectedRow] : []);
    var hasCompany = rows.some(function (d) {
        return d.tCo_IsHasCompany === true || d.tCo_IsHasCompany === 1 || d.tCo_IsHasCompany === "true" || d.tCo_IsHasCompany === "بله"
            || d.t1_ManCompanyId
            || (d.tCo_CompanyNames && String(d.tCo_CompanyNames).trim())
            || (d.t12_FullName && String(d.t12_FullName).trim());
    });
    if (!hasCompany) {
        toastr.warning("برای ارسال به پیمانکار، ردیف انتخاب‌شده باید شرکت/تامین‌کننده داشته باشد");
        return;
    }
    if (!AppSdk.hasRole("Sup.OpenOrderRequest.ConfigManage") && !AppSdk.hasRole("SupplyAndPurchase")) {
        toastr.error("شما دسترسی ارسال به پیمانکار را ندارید");
        return;
    }
    get("/Panel/Sup/OpenOrderRequest/SendToSupplierPartial?openOrderRequestId=" + ids[0], function (result) {
        $.confirm({
            title: "ارسال به پیمانکار",
            columnClass: "col-md-8",
            content: "<div style=\"overflow-x: hidden; overflow-y: auto; max-height: 70vh;\">" + result + "</div>",
            rtl: true,
            buttons: {
                send: {
                    text: "ارسال",
                    btnClass: "btn-success",
                    action: function () {
                        var $content = this.$content;
                        if (validateError($content)) { return false; }
                        var model = $content.dataBind();
                        model.OpenOrderRequestIds = ids;
                        var self = this;
                        var $btn = this.$$send.block();
                        post("/Panel/Sup/OpenOrderRequest/SendToSupplier", model, function (r) {
                            $btn.block(false);
                            if (!r.isSuccess) { toastr.error(r.message, "خطا"); return false; }
                            toastr.success((r.data && r.data.message) ? r.data.message : "ارسال به پیمانکار در صف اعلان ثبت شد");
                            self.close();
                            if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);
                            else ctx.draw();
                        });
                        return false;
                    }
                },
                cancel: { text: "انصراف", btnClass: "btn-secondary" }
            },
            onContentReady: function () {
                var $c = this.$content;
                setTimeout(function () {
                    $c.find(".entity-selector-wrapper").each(function () {
                        if (!$(this).data("readonly") && typeof $(this).entitySelector === "function")
                            $(this).entitySelector();
                    });
                }, 100);
            }
        });
    });
}';

DECLARE @OnSelectedSendToSupplier NVARCHAR(MAX) = N'function(ctx) {
    var $btn = (ctx.dataActionBtns && ctx.dataActionBtns.sendToSupplier)
        || (ctx.$toolbar && ctx.$toolbar.find("[data-action=\"sendToSupplier\"]"));
    if (!$btn || !$btn.length) return;
    var rows = (ctx.selectedRows && ctx.selectedRows.length) ? ctx.selectedRows
        : (ctx.selectedRow && !ctx.isDeselect ? [ctx.selectedRow] : []);
    if (!rows.length) {
        $btn.prop("disabled", true);
        return;
    }
    var hasCompany = rows.some(function (d) {
        return d.tCo_IsHasCompany === true || d.tCo_IsHasCompany === 1 || d.tCo_IsHasCompany === "true" || d.tCo_IsHasCompany === "بله"
            || d.t1_ManCompanyId
            || (d.tCo_CompanyNames && String(d.tCo_CompanyNames).trim())
            || (d.t12_FullName && String(d.t12_FullName).trim());
    });
    $btn.prop("disabled", !hasCompany);
}';

DECLARE @BtnSendToSupplier NVARCHAR(MAX) = (
    SELECT N'id_oor_sendToSupplier' AS id, N'ارسال به پیمانکار' AS title, N'sendToSupplier' AS dataActionName,
           N'btn-color-success' AS colorClass, N'fa fa-envelope' AS iconClass,
           CAST(0 AS bit) AS requiresSelection, CAST(1 AS bit) AS requiresMultiSelection,
           CAST(0 AS bit) AS useHtml, N'' AS html, @ScriptSendToSupplier AS actionScript
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Name NVARCHAR(200);
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Cq NVARCHAR(MAX);
    DECLARE @Ev NVARCHAR(MAX);
    DECLARE @OnRow NVARCHAR(MAX);
    DECLARE @Btns NVARCHAR(MAX);
    DECLARE @UpdatedQuery INT = 0;
    DECLARE @UpdatedColor INT = 0;
    DECLARE @Start INT;
    DECLARE @Mark INT;
    DECLARE @EndAlias INT;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT q.Name, q.QueryJson, q.EventScriptsJson, q.CustomActionButtonsJson
        FROM system.SavedQuery q
        WHERE q.Type = 1
          AND (
                q.EntityFullName LIKE N'%openorderrequest'
             OR q.EntityFullName LIKE N'%OpenOrderRequest'
          )
          AND q.Name <> N'OpenOrderRequestEmailToSupplier_List'
          AND q.Name NOT LIKE N'%EmailToSupplier%';

    OPEN cur;
    FETCH NEXT FROM cur INTO @Name, @Qj, @Ev, @Btns;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Cq = NULL;
        IF @Qj IS NOT NULL AND ISJSON(@Qj) = 1
            SELECT @Cq = CONVERT(NVARCHAR(MAX), [value]) FROM OPENJSON(@Qj) WHERE [key] = N'CustomQuery';

        IF @Cq IS NOT NULL
           AND CHARINDEX(N'OpenOrderRequestEmailToSupplier', @Cq) = 0
           AND CHARINDEX(N'CAST(0 AS bit) AS [EmailSendToSupplier]', @Cq) > 0
        BEGIN
            IF CHARINDEX(@Placeholder, @Cq) > 0
                SET @Cq = REPLACE(@Cq, @Placeholder, @OuterApply);
            ELSE
            BEGIN
                SET @Start = CHARINDEX(N'CROSS APPLY', @Cq);
                SET @Mark = CHARINDEX(N'CAST(0 AS bit) AS [EmailSendToSupplier]', @Cq);
                SET @EndAlias = CHARINDEX(N') AS [tEml]', @Cq, @Mark);
                IF @Start > 0 AND @Mark > @Start AND @EndAlias > @Mark
                    SET @Cq = STUFF(@Cq, @Start, (@EndAlias + LEN(N') AS [tEml]')) - @Start, @OuterApply);
            END

            SET @Qj = JSON_MODIFY(@Qj, N'$.CustomQuery', @Cq);
            UPDATE system.SavedQuery
            SET QueryJson = @Qj,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Name = @Name;
            SET @UpdatedQuery += 1;
            PRINT N'  QueryJson OUTER APPLY: ' + @Name;
        END
        ELSE IF @Cq IS NOT NULL AND CHARINDEX(N'OpenOrderRequestEmailToSupplier', @Cq) > 0
            PRINT N'  QueryJson already live: ' + @Name;
        ELSE IF @Cq IS NOT NULL
            PRINT N'  WARN: placeholder [tEml] در ' + @Name + N' نیست — ابتدا HtsParity را اجرا کنید';

        -- رنگ سبز
        SET @OnRow = NULL;
        IF @Ev IS NOT NULL AND ISJSON(@Ev) = 1
        BEGIN
            IF LEFT(LTRIM(@Ev), 1) = N'['
                SELECT @OnRow = JSON_VALUE([value], N'$.onRowAdded')
                FROM OPENJSON(@Ev) WHERE [key] = N'0';
            ELSE
                SELECT @OnRow = CONVERT(NVARCHAR(MAX), [value]) FROM OPENJSON(@Ev) WHERE [key] = N'onRowAdded';

            IF @OnRow IS NOT NULL AND CHARINDEX(@ColorCommented, @OnRow) > 0
            BEGIN
                SET @OnRow = REPLACE(@OnRow, @ColorCommented, @ColorLive);
                IF LEFT(LTRIM(@Ev), 1) = N'['
                    SET @Ev = JSON_MODIFY(@Ev, N'$[0].onRowAdded', @OnRow);
                ELSE
                    SET @Ev = JSON_MODIFY(@Ev, N'$.onRowAdded', @OnRow);

                UPDATE system.SavedQuery
                SET EventScriptsJson = @Ev,
                    ModifiedById = 1, ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Name = @Name;
                SET @UpdatedColor += 1;
                PRINT N'  onRowAdded lightgreen: ' + @Name;
            END
        END

        FETCH NEXT FROM cur INTO @Name, @Qj, @Ev, @Btns;
    END
    CLOSE cur;
    DEALLOCATE cur;

    -- دکمه فقط روی تنظیمات
    DECLARE @ConfigButtons NVARCHAR(MAX);
    SELECT @ConfigButtons = CustomActionButtonsJson
    FROM system.SavedQuery
    WHERE Name = N'vw_openRequestConfig';

    IF @ConfigButtons IS NULL
        PRINT N'WARN: نمایه vw_openRequestConfig یافت نشد';
    ELSE IF CHARINDEX(N'"dataActionName":"sendToSupplier"', @ConfigButtons) > 0
         OR CHARINDEX(N'id_oor_sendToSupplier', @ConfigButtons) > 0
        PRINT N'  دکمه ارسال به پیمانکار از قبل روی vw_openRequestConfig است';
    ELSE
    BEGIN
        IF @ConfigButtons IS NULL OR LTRIM(RTRIM(@ConfigButtons)) IN (N'', N'[]', N'null')
            SET @ConfigButtons = N'[' + @BtnSendToSupplier + N']';
        ELSE
        BEGIN
            SET @ConfigButtons = LTRIM(RTRIM(@ConfigButtons));
            IF RIGHT(@ConfigButtons, 1) = N']'
                SET @ConfigButtons = LEFT(@ConfigButtons, LEN(@ConfigButtons) - 1) + N',' + @BtnSendToSupplier + N']';
            ELSE
                SET @ConfigButtons = N'[' + @BtnSendToSupplier + N']';
        END

        IF ISJSON(@ConfigButtons) = 0
            THROW 51300, N'CustomActionButtonsJson پس از افزودن دکمه ارسال نامعتبر شد', 1;

        UPDATE system.SavedQuery
        SET CustomActionButtonsJson = @ConfigButtons,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'vw_openRequestConfig';
        PRINT N'  دکمه «ارسال به پیمانکار» به vw_openRequestConfig اضافه شد';
    END

    -- فعال‌شدن دکمه مثل صفحه 76: ردیف انتخاب‌شده + داشتن شرکت (IsHasCompany / ManCompany)
    DECLARE @ConfigEv NVARCHAR(MAX);
    SELECT @ConfigEv = EventScriptsJson FROM system.SavedQuery WHERE Name = N'vw_openRequestConfig';
    IF @ConfigEv IS NOT NULL AND ISJSON(@ConfigEv) = 1
    BEGIN
        IF LEFT(LTRIM(@ConfigEv), 1) = N'['
            SET @ConfigEv = JSON_MODIFY(@ConfigEv, N'$[0].onSelectedRow', @OnSelectedSendToSupplier);
        ELSE
            SET @ConfigEv = JSON_MODIFY(@ConfigEv, N'$.onSelectedRow', @OnSelectedSendToSupplier);

        UPDATE system.SavedQuery
        SET EventScriptsJson = @ConfigEv,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'vw_openRequestConfig';
        PRINT N'  onSelectedRow فعال‌سازی ارسال به پیمانکار روی vw_openRequestConfig تنظیم شد';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Update_OpenOrderRequest_DataProfiles_EmailToSupplier ===';
    PRINT N'  QueryJson updates=' + CAST(@UpdatedQuery AS NVARCHAR(10)) + N'  Color updates=' + CAST(@UpdatedColor AS NVARCHAR(10));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT q.Name, q.Title,
       CASE WHEN q.QueryJson LIKE N'%OpenOrderRequestEmailToSupplier%' THEN 1 ELSE 0 END AS HasLiveEmailApply,
       CASE WHEN q.EventScriptsJson LIKE N'%tEml_EmailSendToSupplier%'
                 AND q.EventScriptsJson NOT LIKE N'%// if (yes(d.tEml_EmailSendToSupplier)%' THEN 1 ELSE 0 END AS GreenLive,
       CASE WHEN q.CustomActionButtonsJson LIKE N'%sendToSupplier%' THEN 1 ELSE 0 END AS HasSendButton
FROM system.SavedQuery q
WHERE q.Type = 1
  AND (q.EntityFullName LIKE N'%openorderrequest' OR q.EntityFullName LIKE N'%OpenOrderRequest')
  AND q.Name NOT LIKE N'%EmailToSupplier%'
ORDER BY q.Name;
