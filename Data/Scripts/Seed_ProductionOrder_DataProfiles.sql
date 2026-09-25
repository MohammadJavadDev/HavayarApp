/*
================================================================================
Seed_ProductionOrder_DataProfiles.sql
================================================================================
نمایه‌های داده (Data Profile) سربرگ سفارش ساخت (Sale.ProductionOrder) مطابق گرید زنده HTS صفحه 463
(ProductionOrderController.GetGridData / _ProductionOrder.cshtml):
  آخرین وضعیت، شماره سفارش، نسخه، قرارداد، مشتری، تفصیل پروژه، پروژه مهندسی، کاربر نهایی، شعبه،
  کارشناس/مدیر فروش، مدیر پروژه، شهر نصب، محل تحویل، متره، نوع بسته‌بندی/محصول/تحویل، پرچم‌ها، تاریخ‌ها، توضیحات.

نمایه‌ها:
  PO_All                — همه سفارش‌های ساخت                 → Sale.ProductionOrder.ShowAll
  PO_Related            — مرتبط با من (ایجادکننده / کارشناس / مدیر فروش / جایگزین / معرف / مدیر پروژه = کاربر جاری)
                          معادل فیلتر غیر-ShowAll در HTS GetGridData:191 → Sale.ProductionOrder.ViewRelated (نقش جدید 200036)
  (کارتابل تایید مالی ساخته نمی‌شود: طبق تصمیم 2026-09-17 تایید مالی فقط در راهکاران است — مثل HTS.)

پیش‌نیاز: Add_ProductionOrder_DeliveryLocation_EngineeringAttachment.sql (اگر اجرا نشده باشد ستون «محل تحویل» NULL نمایش داده می‌شود).
Idempotent — Upsert روی Name. نمایه دستی قبلی (productionorder_listinfo, Id=36) دست‌نخورده می‌ماند.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-po-dataprofiles';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.sale.productionorder';
    DECLARE @EntityFullNamePascal NVARCHAR(200) = N'Entities.App.Sale.ProductionOrder';

    -----------------------------------------------------------------------------
    PRINT N'=== [1/4] Roles ===';
    -----------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrder.ViewRelated')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
        VALUES (200036, N'Sale.ProductionOrder.ViewRelated', N'فروش - سفارش ساخت - مشاهده مرتبط', 1, @SeedUser, 1, @SeedUser, @Now, @Now, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200036 Sale.ProductionOrder.ViewRelated';
    END
    ELSE
        PRINT N'  EXISTS Role Sale.ProductionOrder.ViewRelated';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrder.ShowAll')
        THROW 51200, N'Role Sale.ProductionOrder.ShowAll یافت نشد؛ ابتدا Seed_ProductionOrderItem_DataProfiles.sql را اجرا کنید.', 1;

    -----------------------------------------------------------------------------
    PRINT N'=== [2/4] Build CustomQuery / ColumnsJson ===';
    -----------------------------------------------------------------------------
    DECLARE @DeliveryLocationExpr NVARCHAR(200) =
        CASE WHEN COL_LENGTH('Sale.ProductionOrder', 'DeliveryLocation') IS NULL
             THEN N'CAST(NULL AS NVARCHAR(128))' ELSE N'[t1].[DeliveryLocation]' END;

    DECLARE @BaseSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[State] AS [t1_State],
    [t1].[ProductionOrderNumber] AS [t1_ProductionOrderNumber],
    [t1].[Number] AS [t1_Number],
    [t1].[Revision] AS [t1_Revision],
    [t3].[Number] AS [t3_Number],
    [t5].[FullName] AS [t5_FullName],
    [t1].[CustomerIndustry] AS [t1_CustomerIndustry],
    [t6].[Code] AS [t6_Code],
    [t6].[Title] AS [t6_Title],
    [t1].[EdmsProject] AS [t1_EdmsProject],
    [t1].[EndUser] AS [t1_EndUser],
    [t7].[Title] AS [t7_Title],
    [t8].[Name] AS [t8_Name],
    [t9].[Name] AS [t9_Name],
    [t13].[Name] AS [t13_Name],
    [t10].[Name] AS [t10_Name],
    [t11].[Name] AS [t11_Name],
    ' + @DeliveryLocationExpr + N' AS [t1_DeliveryLocation],
    [t1].[MetreNumber] AS [t1_MetreNumber],
    [t1].[MetreDate] AS [t1_MetreDate],
    [t1].[ProjectStartDate] AS [t1_ProjectStartDate],
    [t1].[AgreedDeliveryDate] AS [t1_AgreedDeliveryDate],
    [t1].[PackingType] AS [t1_PackingType],
    [t1].[ProductType] AS [t1_ProductType],
    [t1].[DeliveryType] AS [t1_DeliveryType],
    [t1].[IsNeedProjectManager] AS [t1_IsNeedProjectManager],
    [t1].[IsNeedInspectionBeforePacking] AS [t1_IsNeedInspectionBeforePacking],
    [t1].[HasContractor] AS [t1_HasContractor],
    [t1].[HasGA] AS [t1_HasGA],
    [t1].[CompressorHouseIsReady] AS [t1_CompressorHouseIsReady],
    [t1].[InstallationIsByHy] AS [t1_InstallationIsByHy],
    [t1].[HasFinancialGuarantee] AS [t1_HasFinancialGuarantee],
    [t1].[DelayNotCalculated] AS [t1_DelayNotCalculated],
    [t1].[SalesComment] AS [t1_SalesComment],
    [t1].[Comment] AS [t1_Comment],
    [t12].[Name] AS [t12_Name],
    [t1].[FinancialConfirmedOnShamsiDate] AS [t1_FinancialConfirmedOnShamsiDate],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime]
FROM [Sale].[ProductionOrder] AS [t1]
LEFT JOIN [SLS].[Contract] AS [t3] ON [t1].[ContractId] = [t3].[Id]
LEFT JOIN [SLS].[Customer] AS [t4] ON [t3].[CustomerId] = [t4].[Id]
LEFT JOIN [Gnr].[Party] AS [t5] ON [t4].[PartyId] = [t5].[Id]
LEFT JOIN [FIN].[DL] AS [t6] ON [t1].[ProjectDlId] = [t6].[Id]
LEFT JOIN [Sale].[Branch] AS [t7] ON [t1].[BranchId] = [t7].[Id]
LEFT JOIN [system].[User] AS [t8] ON [t1].[SalesExpertId] = [t8].[Id]
LEFT JOIN [system].[User] AS [t9] ON [t1].[SalesManagerId] = [t9].[Id]
LEFT JOIN [system].[User] AS [t13] ON [t1].[AlternativeExpertId] = [t13].[Id]
LEFT JOIN [system].[User] AS [t10] ON [t1].[ProjectManagerId] = [t10].[Id]
LEFT JOIN [Gnr].[Region] AS [t11] ON [t1].[InstallationCityId] = [t11].[Id]
LEFT JOIN [system].[User] AS [t12] ON [t1].[FinancialConfirmedById] = [t12].[Id]
WHERE [t1].[IsActive] = 1';

    -- WHERE اختصاصی نمایه «مرتبط با من» (به انتهای @BaseSelect اضافه می‌شود)
    DECLARE @WhereRelated NVARCHAR(MAX) = N'
    AND (
        [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)
        OR [t1].[SalesExpertId] = TRY_CAST(@CurrentUserId AS BIGINT)
        OR [t1].[SalesManagerId] = TRY_CAST(@CurrentUserId AS BIGINT)
        OR [t1].[AlternativeExpertId] = TRY_CAST(@CurrentUserId AS BIGINT)
        OR [t1].[IntroducerExpertId] = TRY_CAST(@CurrentUserId AS BIGINT)
        OR [t1].[ProjectManagerId] = TRY_CAST(@CurrentUserId AS BIGINT)
    )';

    DECLARE @QueryJsonTemplate NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":null,"CustomQuery":null,"Parameters":[],"Selects":[{"Index":0,"Name":"Select1","Title":"Select1"}]}';

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Sale.ProductionOrder","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"State","DisplayName":"آخرین وضعیت","Alliance":"t1_State","Address":"[t1].[State]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderStateEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"ProductionOrderNumber","DisplayName":"شماره سفارش ساخت","Alliance":"t1_ProductionOrderNumber","Address":"[t1].[ProductionOrderNumber]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"Number","DisplayName":"شماره (متنی)","Alliance":"t1_Number","Address":"[t1].[Number]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"Revision","DisplayName":"نسخه","Alliance":"t1_Revision","Address":"[t1].[Revision]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"SLS.Contract","ColumnName":"Number","DisplayName":"قرارداد","Alliance":"t3_Number","Address":"[t3].[Number]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"FullName","DisplayName":"مشتری","Alliance":"t5_FullName","Address":"[t5].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"CustomerIndustry","DisplayName":"صنعت مشتری","Alliance":"t1_CustomerIndustry","Address":"[t1].[CustomerIndustry]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"FIN.DL","ColumnName":"Code","DisplayName":"کد تفصیل پروژه","Alliance":"t6_Code","Address":"[t6].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"FIN.DL","ColumnName":"Title","DisplayName":"تفصیل پروژه","Alliance":"t6_Title","Address":"[t6].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"EdmsProject","DisplayName":"پروژه مهندسی","Alliance":"t1_EdmsProject","Address":"[t1].[EdmsProject]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"EndUser","DisplayName":"کاربر نهایی","Alliance":"t1_EndUser","Address":"[t1].[EndUser]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.Branch","ColumnName":"Title","DisplayName":"شعبه فروش","Alliance":"t7_Title","Address":"[t7].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"Name","DisplayName":"کارشناس فروش","Alliance":"t8_Name","Address":"[t8].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"Name","DisplayName":"مدیر فروش","Alliance":"t9_Name","Address":"[t9].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"Name","DisplayName":"کارشناس جایگزین","Alliance":"t13_Name","Address":"[t13].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"Name","DisplayName":"مدیر پروژه","Alliance":"t10_Name","Address":"[t10].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Region","ColumnName":"Name","DisplayName":"شهر نصب","Alliance":"t11_Name","Address":"[t11].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"DeliveryLocation","DisplayName":"محل تحویل","Alliance":"t1_DeliveryLocation","Address":"[t1].[DeliveryLocation]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"MetreNumber","DisplayName":"شماره متره","Alliance":"t1_MetreNumber","Address":"[t1].[MetreNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"MetreDate","DisplayName":"تاریخ متره","Alliance":"t1_MetreDate","Address":"[t1].[MetreDate]","SystemTypeName":"Date","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":3,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"ProjectStartDate","DisplayName":"تاریخ شروع پروژه","Alliance":"t1_ProjectStartDate","Address":"[t1].[ProjectStartDate]","SystemTypeName":"Date","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":3,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"AgreedDeliveryDate","DisplayName":"تاریخ تحویل توافقی","Alliance":"t1_AgreedDeliveryDate","Address":"[t1].[AgreedDeliveryDate]","SystemTypeName":"Date","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":3,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"PackingType","DisplayName":"نوع بسته‌بندی","Alliance":"t1_PackingType","Address":"[t1].[PackingType]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"ProductType","DisplayName":"نوع محصول","Alliance":"t1_ProductType","Address":"[t1].[ProductType]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"DeliveryType","DisplayName":"نوع تحویل","Alliance":"t1_DeliveryType","Address":"[t1].[DeliveryType]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"IsNeedProjectManager","DisplayName":"نیاز به مدیر پروژه","Alliance":"t1_IsNeedProjectManager","Address":"[t1].[IsNeedProjectManager]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"IsNeedInspectionBeforePacking","DisplayName":"بازرسی قبل از بسته‌بندی","Alliance":"t1_IsNeedInspectionBeforePacking","Address":"[t1].[IsNeedInspectionBeforePacking]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"HasContractor","DisplayName":"پیمانکار","Alliance":"t1_HasContractor","Address":"[t1].[HasContractor]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"HasGA","DisplayName":"GA","Alliance":"t1_HasGA","Address":"[t1].[HasGA]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"CompressorHouseIsReady","DisplayName":"کمپرسورخانه آماده","Alliance":"t1_CompressorHouseIsReady","Address":"[t1].[CompressorHouseIsReady]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"InstallationIsByHy","DisplayName":"نصب بعهده هوایار","Alliance":"t1_InstallationIsByHy","Address":"[t1].[InstallationIsByHy]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"HasFinancialGuarantee","DisplayName":"تعهدات مالی","Alliance":"t1_HasFinancialGuarantee","Address":"[t1].[HasFinancialGuarantee]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"DelayNotCalculated","DisplayName":"پروژه‌ای (بدون تاخیر)","Alliance":"t1_DelayNotCalculated","Address":"[t1].[DelayNotCalculated]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"SalesComment","DisplayName":"توضیحات فروش","Alliance":"t1_SalesComment","Address":"[t1].[SalesComment]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"Name","DisplayName":"تایید کننده مالی","Alliance":"t12_Name","Address":"[t12].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"FinancialConfirmedOnShamsiDate","DisplayName":"تاریخ تایید مالی","Alliance":"t1_FinancialConfirmedOnShamsiDate","Address":"[t1].[FinancialConfirmedOnShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"CreatedByName","DisplayName":"ایجاد کننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"ModifiedDateShamsiDateTime","DisplayName":"تاریخ ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';

    -----------------------------------------------------------------------------
    PRINT N'=== [3/4] Upsert SavedQuery profiles ===';
    -----------------------------------------------------------------------------
    DECLARE @Profiles TABLE (Seq INT, Name NVARCHAR(100), Title NVARCHAR(200), CustomQuery NVARCHAR(MAX), SavedQueryId BIGINT NULL);
    INSERT INTO @Profiles (Seq, Name, Title, CustomQuery) VALUES
        (1, N'PO_All',     N'سفارش ساخت — همه',          @BaseSelect),
        (2, N'PO_Related', N'سفارش ساخت — مرتبط با من',   @BaseSelect + @WhereRelated);

    DECLARE @Seq INT = 1, @Name NVARCHAR(100), @Title NVARCHAR(200), @CustomQuery NVARCHAR(MAX), @QueryJson NVARCHAR(MAX), @SqId BIGINT;
    WHILE @Seq <= 2
    BEGIN
        SELECT @Name = Name, @Title = Title, @CustomQuery = CustomQuery FROM @Profiles WHERE Seq = @Seq;
        SET @QueryJson = JSON_MODIFY(@QueryJsonTemplate, '$.CustomQuery', @CustomQuery);

        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
        BEGIN
            UPDATE system.SavedQuery
            SET Title = @Title,
                QueryJson = @QueryJson,
                ColumnsJson = @ColumnsJson,
                DiagramJson = N'[]',
                EntityFullName = @EntityFullName,
                Mode = 1,
                Type = 1,
                ActionOptions = @ActionOptions,
                -- دکمه چاپ در Seed_ProductionOrderForm_Report.sql است؛ اینجا بازنویسی نمی‌شود
                EventScriptsJson = @EventScripts,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi,
                IsActive = 1
            WHERE Name = @Name;
            SELECT @SqId = Id FROM system.SavedQuery WHERE Name = @Name;
            PRINT N'  UPDATED ' + @Name + N' Id=' + CAST(@SqId AS NVARCHAR(20));
        END
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
                (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
                 Mode, Type, EntityFullName, ActionOptions,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@Name, @Title, @QueryJson, @ColumnsJson, N'[]', N'[]', @EventScripts,
                 1, 1, @EntityFullName, @ActionOptions,
                 1, 1, @SeedUser, @SeedUser,
                 @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET @SqId = SCOPE_IDENTITY();
            PRINT N'  CREATED ' + @Name + N' Id=' + CAST(@SqId AS NVARCHAR(20));
        END

        UPDATE @Profiles SET SavedQueryId = @SqId WHERE Seq = @Seq;
        SET @Seq += 1;
    END

    -----------------------------------------------------------------------------
    PRINT N'=== [4/4] RoleAccess (ActionAccessType=3 DataProfile) ===';
    -----------------------------------------------------------------------------
    DECLARE @RoleMap TABLE (ProfileName NVARCHAR(100), RoleName NVARCHAR(200));
    INSERT INTO @RoleMap (ProfileName, RoleName) VALUES
        (N'PO_All',     N'Sale.ProductionOrder.ShowAll'),
        (N'PO_Related', N'Sale.ProductionOrder.ViewRelated');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(p.SavedQueryId AS NVARCHAR(20)), 3, 7, @EntityFullNamePascal, p.Title, NULL, p.SavedQueryId, r.Id,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @RoleMap m
    INNER JOIN @Profiles p ON p.Name = m.ProfileName
    INNER JOIN system.Role r ON r.Name = m.RoleName          -- نام نقش ممکن است تکراری باشد (200020/200033) → هر دو
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id AND ra.ActionAccessType = 3 AND ra.RowId = p.SavedQueryId);
    PRINT N'  RoleAccess rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'--- DONE Seed_ProductionOrder_DataProfiles ---';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT Id, Name, Title, Mode, Type FROM system.SavedQuery WHERE Name IN (N'PO_All', N'PO_Related');
