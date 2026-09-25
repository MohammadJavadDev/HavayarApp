/*
  Patch_ListDocumentProduct_PartBook_PrintButton.sql
  Append مشاهده دفترچه قطعات via JSON_MODIFY append.
  UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-ldp-partbook-print';
    DECLARE @Btn NVARCHAR(MAX) = N'{"id":"inv_partbook_print_ldp","title":"مشاهده دفترچه قطعات","dataActionName":"printPartBook","colorClass":"btn-color-info","iconClass":"ki-outline ki-book","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var row = ctx && (ctx.selectedRow || {});\n    var partId = row.PartId || row.partId;\n    if (!partId) { toastr.error(''محصول انتخاب نشده''); return; }\n    appController.addPage(''/Panel/Inv/PartBook/Print?partId='' + partId, true, ''چاپ دفترچه قطعات'');\n}"}';
    DECLARE @Updated INT = 0;

    IF ISJSON(@Btn) <> 1
        THROW 51219, N'Button JSON invalid', 1;

    DECLARE @Name NVARCHAR(200);
    DECLARE c CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name FROM system.SavedQuery
        WHERE Name IN (
            N'ListDocumentProduct_NoPrice',
            N'vw_ListDocumentProductPricenew',
            N'vw_ListDocumentProductPriceSale',
            N'vw_ListDocumentProductPrice',
            N'ListDocumentProduct_Bom_NoPrice',
            N'ListDocumentProduct_Bom_Buy',
            N'ListDocumentProduct_Bom_Sale',
            N'vw_ListDocumentProductPriceBom'
        );

    OPEN c;
    FETCH NEXT FROM c INTO @Name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @Json NVARCHAR(MAX) = (SELECT CustomActionButtonsJson FROM system.SavedQuery WHERE Name = @Name);
        IF @Json IS NULL OR LTRIM(RTRIM(@Json)) = N'' OR ISJSON(@Json) <> 1
            SET @Json = N'[]';

        IF @Json NOT LIKE N'%inv_partbook_print_ldp%'
        BEGIN
            SET @Json = JSON_MODIFY(@Json, 'append $', JSON_QUERY(@Btn));
            IF ISJSON(@Json) <> 1
                THROW 51220, N'Patched CustomActionButtonsJson invalid', 1;

            UPDATE system.SavedQuery
            SET CustomActionButtonsJson = @Json,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Name = @Name;
            SET @Updated = @Updated + 1;
        END
        FETCH NEXT FROM c INTO @Name;
    END
    CLOSE c; DEALLOCATE c;

    PRINT N'Profiles updated: ' + CAST(@Updated AS nvarchar(20));
    COMMIT TRANSACTION;
    PRINT N'=== DONE Patch_ListDocumentProduct_PartBook_PrintButton ===';

    SELECT Name,
           CASE WHEN CustomActionButtonsJson LIKE N'%inv_partbook_print_ldp%' THEN 1 ELSE 0 END AS HasBtn,
           CASE WHEN Title LIKE N'%Ø%' OR Title LIKE N'%Ù%' THEN N'MOJIBAKE' ELSE N'OK' END AS TitleCheck
    FROM system.SavedQuery
    WHERE Name IN (
            N'ListDocumentProduct_NoPrice',
            N'vw_ListDocumentProductPricenew',
            N'vw_ListDocumentProductPriceSale',
            N'vw_ListDocumentProductPrice',
            N'ListDocumentProduct_Bom_NoPrice',
            N'ListDocumentProduct_Bom_Buy',
            N'ListDocumentProduct_Bom_Sale',
            N'vw_ListDocumentProductPriceBom'
        );
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
