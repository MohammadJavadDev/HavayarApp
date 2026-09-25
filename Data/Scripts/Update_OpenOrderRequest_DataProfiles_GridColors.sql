/*
================================================================================
Update_OpenOrderRequest_DataProfiles_GridColors.sql
================================================================================
فعال‌سازی کامل EventScriptsJson.onRowAdded روی نمایه‌های زنده درخواست باز.
ترتیب عین HTS changeRowColor در _OpenOrderRequestManagement:
  lightgreen (ایمیل پیمانکار) → violet (Changed) → orangered (IsStop)
  → سلول دوم orange (HasSalesUnitConfirmation) → deepskyblue (پنجره تامین)
قواعد بعدی همان سلول را بازنویسی می‌کنند.

نمایه کامل (۵ رنگ): 32, 72, 74, 102, 103, 141
نمایه پایه (سبز/بنفش/قرمز): 99, 73

TODO WP1 و خط سبز کامنت‌شده حذف می‌شوند.
UTF-8 with BOM؛ اعمال: sqlcmd -f 65001
پس از اجرا: Restart WebApp / پاک‌کردن Redis DataTableProfileCacheDb
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'wp1-grid-colors';

DECLARE @OnRowAddedFull NVARCHAR(MAX) = N'function(ctx) {
    var d = ctx.rowData || {};
    var yes = function (v) { return v === true || v === 1 || v === "true" || v === "بله"; };
    var $cells = ctx.$row.children("td");
    var $c1 = $cells.eq(1);
    var $c2 = $cells.eq(2);
    $c1.css("background-color", "");
    $c2.css("background-color", "");
    if (yes(d.tEml_EmailSendToSupplier)) { $c1.css("background-color", "lightgreen"); }
    if (yes(d.t1_Changed)) { $c1.css("background-color", "violet"); }
    if (yes(d.t1_IsStop)) { $c1.css("background-color", "orangered"); }
    if (yes(d.t1_HasSalesUnitConfirmation)) { $c2.css("background-color", "orange"); }
    if (yes(d.tClr_InSupplyWindow)) { $c1.css("background-color", "deepskyblue"); }
}';

DECLARE @OnRowAddedBasic NVARCHAR(MAX) = N'function(ctx) {
    var d = ctx.rowData || {};
    var yes = function (v) { return v === true || v === 1 || v === "true" || v === "بله"; };
    var $c1 = ctx.$row.children("td").eq(1);
    $c1.css("background-color", "");
    if (yes(d.tEml_EmailSendToSupplier)) { $c1.css("background-color", "lightgreen"); }
    if (yes(d.t1_Changed)) { $c1.css("background-color", "violet"); }
    if (yes(d.t1_IsStop)) { $c1.css("background-color", "orangered"); }
}';

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Id INT, @Name NVARCHAR(200), @Ev NVARCHAR(MAX), @OnSel NVARCHAR(MAX);
    DECLARE @OnRow NVARCHAR(MAX), @NewEv NVARCHAR(MAX);
    DECLARE @Updated INT = 0;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT q.Id, q.Name, q.EventScriptsJson
        FROM system.SavedQuery q
        WHERE q.Id IN (32, 72, 73, 74, 99, 102, 103, 141);

    OPEN cur;
    FETCH NEXT FROM cur INTO @Id, @Name, @Ev;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @Ev IS NULL OR ISJSON(@Ev) = 0
            SET @Ev = N'{"onSelectedRow":"","onRowAdded":""}';

        SET @OnSel = NULL;
        IF LEFT(LTRIM(@Ev), 1) = N'['
            SELECT @OnSel = JSON_VALUE([value], N'$.onSelectedRow') FROM OPENJSON(@Ev) WHERE [key] = N'0';
        ELSE
            SELECT @OnSel = CONVERT(NVARCHAR(MAX), [value]) FROM OPENJSON(@Ev) WHERE [key] = N'onSelectedRow';

        SET @OnSel = ISNULL(@OnSel, N'');

        SET @OnRow =
            CASE
                WHEN @Id IN (99, 73) THEN @OnRowAddedBasic
                ELSE @OnRowAddedFull
            END;

        SET @NewEv = (
            SELECT @OnSel AS onSelectedRow, @OnRow AS onRowAdded
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        UPDATE system.SavedQuery
        SET EventScriptsJson = @NewEv,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @Id;

        SET @Updated += 1;
        PRINT N'  onRowAdded: ' + @Name + N' (Id=' + CAST(@Id AS NVARCHAR(10)) + N')';

        FETCH NEXT FROM cur INTO @Id, @Name, @Ev;
    END
    CLOSE cur;
    DEALLOCATE cur;

    COMMIT TRANSACTION;
    PRINT N'=== DONE GridColors updates=' + CAST(@Updated AS NVARCHAR(10));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT q.Id, q.Name, q.Title,
       CASE WHEN q.EventScriptsJson LIKE N'%TODO WP1%' THEN 1 ELSE 0 END AS HasTodoWp1,
       CASE WHEN q.EventScriptsJson LIKE N'%// if (yes(d.tEml_EmailSendToSupplier)%' THEN 1 ELSE 0 END AS GreenCommented,
       CASE WHEN q.EventScriptsJson LIKE N'%lightgreen%'
                 AND q.EventScriptsJson NOT LIKE N'%// if (yes(d.tEml_EmailSendToSupplier)%' THEN 1 ELSE 0 END AS GreenLive,
       CASE WHEN q.EventScriptsJson LIKE N'%deepskyblue%' THEN 1 ELSE 0 END AS HasSupplyBlue,
       CASE WHEN q.Title LIKE N'%درخواست%' OR q.Title LIKE N'%تایید%' OR q.Title LIKE N'%پیشینه%'
                 OR q.Title LIKE N'%تنظیمات%' OR q.Title LIKE N'%مشاهده%' THEN 1 ELSE 0 END AS TitleOkPersian
FROM system.SavedQuery q
WHERE q.Id IN (32, 72, 73, 74, 99, 102, 103, 141)
ORDER BY q.Id;
