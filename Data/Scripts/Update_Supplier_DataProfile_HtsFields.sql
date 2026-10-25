-- ============================================================================
-- نمایه supplier_listinfo (SavedQuery زنده، معمولاً Id = 38)
-- سرستون‌ها = عنوان‌های صفحه تامین‌کنندگان HTS (_CompanyManagement)
-- و ستون‌های عنوان شرکت / همراه / کد تفصیل که در نمایه قبلی نبودند.
--
-- فقط UPDATE. اگر نمایه پیدا نشد هیچ ردیف جدیدی ساخته نمی‌شود.
-- ActionOptions / RoleAccess / دکمه‌های سفارشی دست نخورده می‌مانند.
-- این فایل باید UTF-8 with BOM باشد و با sqlcmd -f 65001 اجرا شود.
-- بعد از اجرا: کش Redis دیتابیس DataTableProfileCacheDb پاک شود یا WebApp ری‌استارت شود.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-supplier-list-fa';
    DECLARE @Name NVARCHAR(200) = N'supplier_listinfo';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.gnr.supplier';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[CompanyName] AS [t2_CompanyName],
    [t2].[FullName] AS [t2_FullName],
    [t2].[EconomicCode] AS [t2_EconomicCode],
    [t2].[Mobile] AS [t2_Mobile],
    [t1].[DlCode] AS [t1_DlCode],
    [t1].[PhoneNumber] AS [t1_PhoneNumber],
    [t1].[Fax] AS [t1_Fax],
    [t1].[PostalCode] AS [t1_PostalCode],
    [t1].[Email] AS [t1_Email],
    [t1].[Grade] AS [t1_Grade],
    [t1].[ServicesAndProducts] AS [t1_ServicesAndProducts],
    [t1].[RelatedPersonName] AS [t1_RelatedPersonName],
    [t1].[Address] AS [t1_Address],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Gnr].[Supplier] AS [t1]
LEFT JOIN [Gnr].[Party] AS [t2]
    ON [t1].[PartyId] = [t2].[Id]';

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Gnr.Supplier","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"t2_CompanyName","DisplayName":"عنوان شرکت","Alliance":"t2_CompanyName","Address":"[t2].[CompanyName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"t2_FullName","DisplayName":"نام و نام خانوادگی","Alliance":"t2_FullName","Address":"[t2].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"t2_EconomicCode","DisplayName":"کد اقتصادی","Alliance":"t2_EconomicCode","Address":"[t2].[EconomicCode]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"t2_Mobile","DisplayName":"همراه","Alliance":"t2_Mobile","Address":"[t2].[Mobile]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_DlCode","DisplayName":"کد تفصیل","Alliance":"t1_DlCode","Address":"[t1].[DlCode]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_PhoneNumber","DisplayName":"شماره تماس","Alliance":"t1_PhoneNumber","Address":"[t1].[PhoneNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_Fax","DisplayName":"فکس","Alliance":"t1_Fax","Address":"[t1].[Fax]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_PostalCode","DisplayName":"کد پستی","Alliance":"t1_PostalCode","Address":"[t1].[PostalCode]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_Email","DisplayName":"ایمیل","Alliance":"t1_Email","Address":"[t1].[Email]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_Grade","DisplayName":"گرید","Alliance":"t1_Grade","Address":"[t1].[Grade]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_ServicesAndProducts","DisplayName":"خدمات / محصولات","Alliance":"t1_ServicesAndProducts","Address":"[t1].[ServicesAndProducts]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_RelatedPersonName","DisplayName":"شخص رابط","Alliance":"t1_RelatedPersonName","Address":"[t1].[RelatedPersonName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_Address","DisplayName":"آدرس","Alliance":"t1_Address","Address":"[t1].[Address]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":280,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Supplier","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""}
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
        Mode = 1,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = @ProfileId;

    IF NOT EXISTS (
        SELECT 1
        FROM [system].[SavedQuery] q
        CROSS APPLY OPENJSON(q.ColumnsJson) j
        WHERE q.Id = @ProfileId
          AND JSON_VALUE(j.value, '$.DisplayName') = N'شناسه'
    )
        THROW 51601, N'DisplayName شناسه بعد از به‌روزرسانی درست نیست. فایل را با UTF-8 BOM و sqlcmd -f 65001 اجرا کنید.', 1;

    COMMIT TRANSACTION;
    PRINT N'SavedQuery updated: Id=' + CAST(@ProfileId AS NVARCHAR(20));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'supplier_listinfo'
ORDER BY CAST(j.[key] AS int);
