/*
  OpenOrderRequest "SendToSupplier" button: pass all selected ids to SendToSupplierPartial
  so the confirm dialog lists exactly which rows will be e-mailed.
  ASCII only (no Persian) - safe with plain sqlcmd; use -f 65001 anyway.
  Idempotent: skips rows that already contain '&ids='.
*/
SET NOCOUNT ON;

DECLARE @Old NVARCHAR(MAX) = N'get("/Panel/Sup/OpenOrderRequest/SendToSupplierPartial?openOrderRequestId=" + ids[0], function (result) {';
DECLARE @New NVARCHAR(MAX) = N'get("/Panel/Sup/OpenOrderRequest/SendToSupplierPartial?openOrderRequestId=" + ids[0] + "&ids=" + ids.join(","), function (result) {';

DECLARE @OldEsc NVARCHAR(MAX) = STRING_ESCAPE(@Old, 'json');
DECLARE @NewEsc NVARCHAR(MAX) = STRING_ESCAPE(@New, 'json');
DECLARE @OldEscNoSlash NVARCHAR(MAX) = REPLACE(@OldEsc, N'\/', N'/');
DECLARE @NewEscNoSlash NVARCHAR(MAX) = REPLACE(@NewEsc, N'\/', N'/');

UPDATE q
SET CustomActionButtonsJson =
        REPLACE(REPLACE(q.CustomActionButtonsJson, @OldEsc, @NewEsc), @OldEscNoSlash, @NewEscNoSlash)
FROM system.SavedQuery q
WHERE q.CustomActionButtonsJson LIKE N'%SendToSupplierPartial%'
  AND q.CustomActionButtonsJson NOT LIKE N'%&ids=%';

PRINT CONCAT('Updated rows: ', @@ROWCOUNT);

SELECT q.Id, q.Name,
       CASE WHEN q.CustomActionButtonsJson LIKE N'%&ids=%' THEN 1 ELSE 0 END AS HasIdsParam
FROM system.SavedQuery q
WHERE q.CustomActionButtonsJson LIKE N'%SendToSupplierPartial%';
