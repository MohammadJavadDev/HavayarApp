-- ============================================================================
-- WP9 / D16 (Q5-a) — به‌روزرسانی نمایه موجود supplier_listinfo (SavedQuery Id زنده ≈ 38)
-- ستون‌های جدید: گرید، خدمات/محصولات، شخص مرتبط، آدرس کامل
--
-- فقط UPDATE. اگر نمایه پیدا نشد هیچ ردیف جدیدی ساخته نمی‌شود (تکراری ممنوع).
-- ActionOptions / RoleAccess / دکمه‌های سفارشی دست نخورده می‌مانند.
-- پیش‌نیاز: ستون‌های D16 روی Gnr.Supplier
-- بعد از اجرا: ری‌استارت WebApp / پاک‌کردن DataTableProfileCacheDb
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH('Gnr.Supplier', 'Grade') IS NULL
   OR COL_LENGTH('Gnr.Supplier', 'ServicesAndProducts') IS NULL
   OR COL_LENGTH('Gnr.Supplier', 'RelatedPersonName') IS NULL
   OR COL_LENGTH('Gnr.Supplier', 'Address') IS NULL
BEGIN
    RAISERROR(N'ستون‌های D16 روی Gnr.Supplier نیستند. ابتدا migration AddSupplierHtsFields یا Add_Supplier_HtsFields.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-wp9-supplier-htsfields';
    DECLARE @Name NVARCHAR(200) = N'supplier_listinfo';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.gnr.supplier';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[PostalCode] AS [t1_PostalCode],
    [t1].[PhoneNumber] AS [t1_PhoneNumber],
    [t1].[Fax] AS [t1_Fax],
    [t1].[Email] AS [t1_Email],
    [t1].[Grade] AS [t1_Grade],
    [t1].[ServicesAndProducts] AS [t1_ServicesAndProducts],
    [t1].[RelatedPersonName] AS [t1_RelatedPersonName],
    [t1].[Address] AS [t1_Address],
    [t2].[FullName] AS [t2_FullName],
    [t2].[EconomicCode] AS [t2_EconomicCode],
    [t2].[Phone] AS [t2_Phone]
FROM [Gnr].[Supplier] AS [t1]
LEFT JOIN [Gnr].[Party] AS [t2]
    ON [t1].[PartyId] = [t2].[Id]';

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Gnr.Supplier","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":1,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"FullName","DisplayName":"نام کامل","Alliance":"t2_FullName","Address":"[t2].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"EconomicCode","DisplayName":"کد اقتصادی","Alliance":"t2_EconomicCode","Address":"[t2].[EconomicCode]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"Phone","DisplayName":"تلفن","Alliance":"t2_Phone","Address":"[t2].[Phone]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"PhoneNumber","DisplayName":"شماره تماس","Alliance":"t1_PhoneNumber","Address":"[t1].[PhoneNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"Fax","DisplayName":"فکس","Alliance":"t1_Fax","Address":"[t1].[Fax]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"PostalCode","DisplayName":"کد پستی","Alliance":"t1_PostalCode","Address":"[t1].[PostalCode]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"Email","DisplayName":"ایمیل","Alliance":"t1_Email","Address":"[t1].[Email]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"Grade","DisplayName":"گرید","Alliance":"t1_Grade","Address":"[t1].[Grade]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"ServicesAndProducts","DisplayName":"خدمات/محصولات","Alliance":"t1_ServicesAndProducts","Address":"[t1].[ServicesAndProducts]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"RelatedPersonName","DisplayName":"شخص مرتبط","Alliance":"t1_RelatedPersonName","Address":"[t1].[RelatedPersonName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"Address","DisplayName":"آدرس کامل","Alliance":"t1_Address","Address":"[t1].[Address]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":280,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    IF ISJSON(@ColumnsJson) <> 1
        THROW 51600, N'ColumnsJson معتبر نیست.', 1;

    DECLARE @ProfileId BIGINT;
    SELECT TOP (1) @ProfileId = Id
    FROM [system].[SavedQuery]
    WHERE Name = @Name
       OR ([Type] = 1 AND LOWER(EntityFullName) = @EntityFullName)
    ORDER BY CASE WHEN Name = @Name THEN 0 ELSE 1 END, Id;

    IF @ProfileId IS NULL
    BEGIN
        PRINT N'WARNING: نمایه supplier_listinfo / entities.app.gnr.supplier یافت نشد — هیچ ردیفی درج نشد.';
        COMMIT TRANSACTION;
        RETURN;
    END

    DECLARE @QueryJson NVARCHAR(MAX);
    SELECT @QueryJson = QueryJson FROM [system].[SavedQuery] WHERE Id = @ProfileId;

    IF ISJSON(@QueryJson) = 1
        SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    ELSE
        SET @QueryJson = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';

    IF ISJSON(@QueryJson) = 1 AND JSON_VALUE(@QueryJson, '$.CustomQuery') IS NULL
        SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    UPDATE [system].[SavedQuery]
    SET QueryJson = @QueryJson,
        ColumnsJson = @ColumnsJson,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = @ProfileId;

    PRINT N'SavedQuery updated (columns only): Id=' + CAST(@ProfileId AS NVARCHAR(20)) + N' Name=' + @Name;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Update_Supplier_DataProfile_HtsFields ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT sq.Id, sq.Name, sq.Title, sq.EntityFullName, sq.[Type], sq.Mode, sq.IsActive,
       LEN(sq.ColumnsJson) AS ColsLen
FROM [system].[SavedQuery] sq
WHERE sq.Name = N'supplier_listinfo'
   OR (sq.[Type] = 1 AND LOWER(sq.EntityFullName) = N'entities.app.gnr.supplier');
