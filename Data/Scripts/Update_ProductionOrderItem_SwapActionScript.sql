SET NOCOUNT ON;
SET XACT_ABORT ON;

-- دکمه «جابه جایی رکورد» روی نمایه‌های اقلام سفارش ساخت
-- منطق مودال در List.cshtml: openProductionOrderItemSwapModal

DECLARE @ActionScript NVARCHAR(MAX) = N'function(ctx) {
  if (typeof openProductionOrderItemSwapModal === ''function'') {
    openProductionOrderItemSwapModal(ctx);
    return;
  }
  toastr.error(''تابع جابجایی رکورد بارگذاری نشده است'');
}';

UPDATE q
SET q.CustomActionButtonsJson = JSON_MODIFY(q.CustomActionButtonsJson, N'$[0].actionScript', @ActionScript)
FROM system.SavedQuery q
WHERE q.CustomActionButtonsJson LIKE N'%changeRow%'
  AND q.CustomActionButtonsJson LIKE N'%id_23wur52hk%'
  AND (
        q.EntityFullName LIKE N'%productionorderitem%'
        OR q.Name LIKE N'POI_%'
        OR q.Name = N'productionorderitem_listinfonew'
      );

PRINT CONCAT(N'Updated swap ActionScript on ', @@ROWCOUNT, N' SavedQuery row(s).');
