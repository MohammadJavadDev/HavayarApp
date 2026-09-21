/*
================================================================================
Update_ProductionOrderDelay_ExcludeFromCycle_Button.sql
================================================================================
دکمه سفارشی «خارج کردن از سیکل تاخیر» برای نمایه‌های:
  vw_All_Need_POD
  vw_All_POD

با انتخاب ردیف و کلیک دکمه، فیلد Sale.ProductionOrder.DelayNotCalculated
(پروژه‌ای / خروج از سیکل تاخیرات) برابر ۱ می‌شود و جدول رفرش می‌گردد.

Idempotent: دکمه قبلی با dataActionName = excludeFromDelayCycle حذف و دوباره اضافه می‌شود.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'update-pod-exclude-from-cycle';

    DECLARE @ActionScript NVARCHAR(MAX) = N'function(ctx) {
              if (!ctx.selectedRow) {
                  toastr.warning(''لطفاً یک ردیف انتخاب کنید'');
                  return;
              }

              var productionOrderId = ctx.primaryKeyValue;
              var orderNumber = ctx.selectedRow.productionOrderNumber || ctx.selectedRow.ProductionOrderNumber || '''';

              Swal.fire({
                  title: ''خارج کردن از سیکل تاخیر'',
                  text: ''با این کار فیلد پروژه‌ای (خروج از سیکل تأخیرات) برای سفارش ساخت '' + orderNumber + '' تیک می‌خورد و سفارش از لیست تاخیر خارج می‌شود.'',
                  icon: ''warning'',
                  showCancelButton: true,
                  confirmButtonText: ''بله، خارج شود'',
                  cancelButtonText: ''انصراف''
              }).then(function (result) {
                  if (!result.isConfirmed) return;
                  var $btn = ctx.$btn.block();
                  post(''/Panel/ProductionOrderDelay/ExcludeFromDelayCycle?productionOrderId='' + encodeURIComponent(productionOrderId), null, function (r) {
                      $btn.block(false);
                      if (!r.isSuccess) {
                          toastr.error(r.message || ''خطا در خارج کردن از سیکل تاخیر'', ''خطا'');
                          return;
                      }
                      toastr.success(''سفارش ساخت از سیکل تاخیر خارج شد'');
                      ctx.draw();
                  });
              });
          }';

    DECLARE @BtnJson NVARCHAR(MAX) = (
        SELECT
            N'id_excludeFromDelayCycle' AS id,
            N'خارج کردن از سیکل تاخیر' AS title,
            N'excludeFromDelayCycle' AS dataActionName,
            N'btn-color-warning' AS colorClass,
            N'ki-outline ki-exit-right' AS iconClass,
            CAST(1 AS bit) AS requiresSelection,
            CAST(0 AS bit) AS requiresMultiSelection,
            CAST(0 AS bit) AS useHtml,
            N'' AS html,
            @ActionScript AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    DECLARE @ProfileName NVARCHAR(100);
    DECLARE @Id BIGINT;
    DECLARE @Buttons NVARCHAR(MAX);
    DECLARE @Filtered NVARCHAR(MAX);
    DECLARE @Names TABLE (Name NVARCHAR(100));
    INSERT INTO @Names (Name) VALUES (N'vw_All_Need_POD'), (N'vw_All_POD');

    DECLARE name_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @Names;
    OPEN name_cursor;
    FETCH NEXT FROM name_cursor INTO @ProfileName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Id = NULL;
        SET @Buttons = NULL;
        SET @Filtered = NULL;

        SELECT @Id = Id, @Buttons = CustomActionButtonsJson
        FROM system.SavedQuery
        WHERE Name = @ProfileName;

        IF @Id IS NULL
            THROW 51020, N'نمایه داده یافت نشد.', 1;

        IF @Buttons IS NULL OR ISJSON(@Buttons) <> 1 OR LTRIM(RTRIM(@Buttons)) = N''
            SET @Buttons = N'[]';

        SELECT @Filtered = STRING_AGG(CAST([value] AS NVARCHAR(MAX)), N',')
        FROM OPENJSON(@Buttons)
        WHERE JSON_VALUE([value], '$.dataActionName') <> N'excludeFromDelayCycle'
          AND ISNULL(JSON_VALUE([value], '$.id'), N'') <> N'id_excludeFromDelayCycle';

        IF @Filtered IS NULL OR LEN(@Filtered) = 0
            SET @Buttons = N'[]';
        ELSE
            SET @Buttons = N'[' + @Filtered + N']';

        SET @Buttons = JSON_MODIFY(@Buttons, 'append $', JSON_QUERY(@BtnJson));

        UPDATE system.SavedQuery
        SET CustomActionButtonsJson = @Buttons,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @Id;

        FETCH NEXT FROM name_cursor INTO @ProfileName;
    END

    CLOSE name_cursor;
    DEALLOCATE name_cursor;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
