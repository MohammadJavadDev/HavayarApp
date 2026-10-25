/*
  Row colors match HTS changeRowColor and paint the row-number cell, not HaveAttachment.
  Page 74: lightgreen, violet, orangered, deepskyblue on the # column (later rule wins).
  Orange (sales confirmation) is on the checkbox column when that cell is in the row.
  Config and history: lightgreen, violet, orangered only.
  Supply window matches the old datetime compare: supply date is 1 to 3 days after today.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @OldWindow nvarchar(300) = N'@TodayDate BETWEEN DATEADD(DAY, -3, CAST([t1].[SupplyMiladiDate] AS date)) AND CAST([t1].[SupplyMiladiDate] AS date)';
DECLARE @NewWindow nvarchar(300) = N'CAST([t1].[SupplyMiladiDate] AS date) > @TodayDate AND CAST([t1].[SupplyMiladiDate] AS date) <= DATEADD(DAY, 3, @TodayDate)';

UPDATE q
SET QueryJson = REPLACE(q.QueryJson, @OldWindow, @NewWindow)
FROM system.SavedQuery AS q
WHERE q.EntityFullName LIKE N'%OpenOrderRequest%'
  AND q.Name <> N'OpenOrderRequestEmailToSupplier_List'
  AND CHARINDEX(@OldWindow, q.QueryJson) > 0;

DECLARE @WindowLeft int = (
    SELECT COUNT(*)
    FROM system.SavedQuery AS q
    WHERE q.EntityFullName LIKE N'%OpenOrderRequest%'
      AND q.Name <> N'OpenOrderRequestEmailToSupplier_List'
      AND CHARINDEX(@OldWindow, q.QueryJson) > 0
);
IF @WindowLeft <> 0
    THROW 51051, N'Supply window text was not replaced on every profile.', 1;

DECLARE @Full nvarchar(max) = N'function(ctx) {
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

DECLARE @Basic nvarchar(max) = N'function(ctx) {
    var d = ctx.rowData || {};
    var yes = function (v) { return v === true || v === 1 || v === "true" || v === "بله"; };
    var $cells = ctx.$row.children("td");
    var $select = $cells.filter(".dt-row-select-col");
    var $rowNo = $select.length ? $select.next("td") : $cells.eq(0);
    $rowNo.css("background-color", "");
    if (yes(d.tEml_EmailSendToSupplier)) { $rowNo.css("background-color", "lightgreen"); }
    if (yes(d.t1_Changed)) { $rowNo.css("background-color", "violet"); }
    if (yes(d.t1_IsStop)) { $rowNo.css("background-color", "orangered"); }
}';

UPDATE q
SET EventScriptsJson = JSON_MODIFY(
        q.EventScriptsJson,
        N'$.onRowAdded',
        CASE
            WHEN q.Name IN (N'vw_openRequestConfig', N'vw_openOrderRequestPurchaseCompleted') THEN @Basic
            ELSE @Full
        END),
    ModifiedDateMiladiDateTime = GETDATE()
FROM system.SavedQuery AS q
WHERE q.EntityFullName LIKE N'%OpenOrderRequest%'
  AND q.Name <> N'OpenOrderRequestEmailToSupplier_List';

IF EXISTS (
    SELECT 1
    FROM system.SavedQuery AS q
    WHERE q.EntityFullName LIKE N'%OpenOrderRequest%'
      AND q.Name <> N'OpenOrderRequestEmailToSupplier_List'
      AND (
            ISJSON(q.EventScriptsJson) <> 1
            OR ISJSON(q.QueryJson) <> 1
            OR CHARINDEX(CHAR(13), q.QueryJson) > 0
            OR CHARINDEX(N'.eq(1)', q.EventScriptsJson) > 0
            OR CHARINDEX(N'.eq(2)', q.EventScriptsJson) > 0
            OR CHARINDEX(N'dt-row-select-col', q.EventScriptsJson) = 0
          )
)
    THROW 51052, N'Row color update verification failed.', 1;

IF EXISTS (
    SELECT 1
    FROM system.SavedQuery AS q
    WHERE q.Name = N'openorderrequest_listinfonew'
      AND CHARINDEX(N'lightgreen', q.EventScriptsJson) = 0
)
    THROW 51053, N'Engineering profile is missing the supplier-email color.', 1;

PRINT N'Row colors updated.';
