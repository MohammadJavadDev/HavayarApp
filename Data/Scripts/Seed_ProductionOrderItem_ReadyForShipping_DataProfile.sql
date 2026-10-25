/*
  Seed_ProductionOrderItem_ReadyForShipping_DataProfile.sql
  نمایه POI_All_ReadyForShipping (Id زنده=123) مطابق گزارش HTS
  Vw_Pln_ProductionOrderItem_ReadyToSend

  فیلتر: آخرین نسخه، حذف‌نشده، Status=579، ProductionStep=221
  و تعداد اقلام ناتمام سفارش = تعداد اقلام آماده ارسال همان سفارش.

  ستون‌ها و عناوین از ویو گزارش + CaptionsLibrary.fa-IR
  + InWarehouseTimeInDay و Bar (ExtractBarValue).

  Idempotent: upsert فقط بر اساس Name (نه EntityFullName — چند نمایه POI وجود دارد).
  Apply: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i این فایل
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-readyforsend';

    DECLARE @Name NVARCHAR(200) = N'POI_All_ReadyForShipping';
    DECLARE @Title NVARCHAR(200) = N'آماده ارسال اقلام سفارش ساخت';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.sale.productionorderitem';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItem';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[ProductionOrderNumber] AS [t2_ProductionOrderNumber],
    [t1].[Serial] AS [t1_Serial],
    [t7].[Code] AS [t7_Code],
    [t7].[Name] AS [t7_Name],
    [t1].[Amount] AS [t1_Amount],
    [bar].[Bar] AS [tBar_Bar],
    [t1].[PartModel] AS [t1_PartModel],
    [t8].[Title] AS [t8_Title],
    [t4].[Code] AS [t4_Code],
    [t5].[FullName] AS [t5_FullName],
    [t1].[CheckStatus] AS [t1_CheckStatus],
    [t1].[Status] AS [t1_Status],
    [t1].[ProductionStep] AS [t1_ProductionStep],
    [t1].[SerialType] AS [t1_SerialType],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[SendToIndustrialShamsiDate] AS [t1_SendToIndustrialShamsiDate],
    [t1].[AgreedDeliverDate] AS [t1_AgreedDeliverDate],
    [t1].[AgreedDeliverShamsiDate] AS [t1_AgreedDeliverShamsiDate],
    [t1].[StandardDeliveryShamsiDate] AS [t1_StandardDeliveryShamsiDate],
    [t1].[ProductionStartShamsiDate] AS [t1_ProductionStartShamsiDate],
    [t1].[ProductionEndShamsiDate] AS [t1_ProductionEndShamsiDate],
    [t1].[PlanningConsideration] AS [t1_PlanningConsideration],
    [t1].[IsLatestVersion] AS [t1_IsLatestVersion],
    [t1].[PreparationShamsiDate] AS [t1_PreparationShamsiDate],
    [t6].[Name] AS [t6_Name],
    [t11].[Name] AS [t11_Name],
    [t1].[EngineeringConsideration] AS [t1_EngineeringConsideration],
    [t1].[SalesConsideration] AS [t1_SalesConsideration],
    DATEDIFF(DAY, [t1].[PreparationMiladiDate], GETDATE()) AS [tWh_InWarehouseTimeInDay]
FROM [Sale].[ProductionOrderItem] AS [t1]
LEFT JOIN [Sale].[ProductionOrder] AS [t2]
    ON [t1].[ProductionOrderId] = [t2].[Id]
LEFT JOIN [Edms].[Project] AS [t11]
    ON [t11].[HtsId] = [t2].[EdmsProject]
   AND [t11].[HtsId] <> 0
LEFT JOIN [SLS].[Contract] AS [t3]
    ON [t2].[ContractId] = [t3].[Id]
LEFT JOIN [SLS].[Customer] AS [t4]
    ON [t3].[CustomerId] = [t4].[Id]
LEFT JOIN [Gnr].[Party] AS [t5]
    ON [t5].[Id] = [t4].[PartyId]
LEFT JOIN [system].[User] AS [t6]
    ON [t6].[Id] = [t2].[SalesExpertId]
LEFT JOIN [Inv].[Part] AS [t7]
    ON [t1].[PartId] = [t7].[Id]
LEFT JOIN [Sale].[Branch] AS [t8]
    ON [t2].[BranchId] = [t8].[Id]
OUTER APPLY (
    SELECT CASE
        WHEN PATINDEX(N''%bar%'', LOWER(ISNULL([t7].[Name], N''''))) = 0 THEN CAST(NULL AS int)
        ELSE (
            SELECT TRY_CAST(REVERSE(SUBSTRING(
                rev,
                PATINDEX(N''%[0-9]%'', rev + N''x''),
                PATINDEX(N''%[^0-9]%'', STUFF(rev, 1, PATINDEX(N''%[0-9]%'', rev + N''x'') - 1, N'''') + N''x'') - 1
            )) AS int)
            FROM (SELECT REVERSE(LEFT(LOWER([t7].[Name]), PATINDEX(N''%bar%'', LOWER([t7].[Name] + N''bar'')) - 1)) AS rev) d
        )
    END AS [Bar]
) AS [bar]
WHERE
    [t1].[IsDeleted] = 0
    AND [t1].[IsLatestVersion] = 1
    AND [t1].[Status] = 579
    AND [t1].[ProductionStep] = 221
    AND (
        SELECT COUNT(*)
        FROM [Sale].[ProductionOrderItem] AS [sibA]
        WHERE [sibA].[ProductionOrderId] = [t1].[ProductionOrderId]
          AND [sibA].[IsDeleted] = 0
          AND [sibA].[IsLatestVersion] = 1
          AND [sibA].[Status] = 579
    ) = (
        SELECT COUNT(*)
        FROM [Sale].[ProductionOrderItem] AS [sibB]
        WHERE [sibB].[ProductionOrderId] = [t1].[ProductionOrderId]
          AND [sibB].[IsDeleted] = 0
          AND [sibB].[IsLatestVersion] = 1
          AND [sibB].[ProductionStep] = 221
    )';
    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":[{"Index":0,"Name":"Select1","Title":"Select1"}]}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName":null,"ColumnName":"custom_id_lx6hqfp5w","DisplayName":"-","Alliance":"custom_id_lx6hqfp5w","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row, meta) {\n    var value = data ?? '''';\n    return `<i class=\"fa fa-plus-circle text-primary\"></i>`.replace(/{value}/g, value).replace(/{row\\.([^}]+)}/g, function(m, f){ return row[f] ?? row[f.charAt(0).toUpperCase()+f.slice(1)] ?? ''''; });\n}","IsCustom":true,"CustomColType":"html","HtmlTemplate":"<i class=\"fa fa-plus-circle text-primary\"></i>","BtnConfig":null,"InputConfig":null,"Width":50,"Filterable":false,"Sortable":false,"ClassName":"dt-control text-center"},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":1,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrder","ColumnName":"t2_ProductionOrderNumber","DisplayName":"شماره سفارش ساخت","Alliance":"t2_ProductionOrderNumber","Address":"[t2].[ProductionOrderNumber]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":75,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_Serial","DisplayName":"سریال","Alliance":"t1_Serial","Address":"[t1].[Serial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t7_Code","DisplayName":"کد محصول","Alliance":"t7_Code","Address":"[t7].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t7_Name","DisplayName":"نام کالا","Alliance":"t7_Name","Address":"[t7].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":320,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_Amount","DisplayName":"تعداد/مقدار","Alliance":"t1_Amount","Address":"[t1].[Amount]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"tBar_Bar","DisplayName":"بار","Alliance":"tBar_Bar","Address":"[bar].[Bar]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_PartModel","DisplayName":"مدل کالا","Alliance":"t1_PartModel","Address":"[t1].[PartModel]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Branch","ColumnName":"t8_Title","DisplayName":"دپارتمان فروش","Alliance":"t8_Title","Address":"[t8].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.Customer","ColumnName":"t4_Code","DisplayName":"کد مشتری","Alliance":"t4_Code","Address":"[t4].[Code]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Party","ColumnName":"t5_FullName","DisplayName":"مشتری","Alliance":"t5_FullName","Address":"[t5].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_CheckStatus","DisplayName":"وضعیت خرید","Alliance":"t1_CheckStatus","Address":"[t1].[CheckStatus]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderItemCheckStatusEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_Status","DisplayName":"وضعیت","Alliance":"t1_Status","Address":"[t1].[Status]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderItemStatusEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_ProductionStep","DisplayName":"مرحله ساخت","Alliance":"t1_ProductionStep","Address":"[t1].[ProductionStep]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderItemProductionStepEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_SerialType","DisplayName":"نوع سریال","Alliance":"t1_SerialType","Address":"[t1].[SerialType]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderItemSerialTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد قلم","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_SendToIndustrialShamsiDate","DisplayName":"تاریخ ارسال به صنایع","Alliance":"t1_SendToIndustrialShamsiDate","Address":"[t1].[SendToIndustrialShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_AgreedDeliverDate","DisplayName":"تاریخ تحویل توافقی","Alliance":"t1_AgreedDeliverDate","Address":"[t1].[AgreedDeliverDate]","SystemTypeName":"Date","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":3,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_AgreedDeliverShamsiDate","DisplayName":"تاریخ تحویل توافقی شمسی","Alliance":"t1_AgreedDeliverShamsiDate","Address":"[t1].[AgreedDeliverShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_StandardDeliveryShamsiDate","DisplayName":"تاریخ استاندارد تحویل","Alliance":"t1_StandardDeliveryShamsiDate","Address":"[t1].[StandardDeliveryShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_ProductionStartShamsiDate","DisplayName":"تاریخ شروع تولید","Alliance":"t1_ProductionStartShamsiDate","Address":"[t1].[ProductionStartShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_ProductionEndShamsiDate","DisplayName":"تاریخ پایان تولید","Alliance":"t1_ProductionEndShamsiDate","Address":"[t1].[ProductionEndShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_PlanningConsideration","DisplayName":"ملاحظات صنایع","Alliance":"t1_PlanningConsideration","Address":"[t1].[PlanningConsideration]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_IsLatestVersion","DisplayName":"نسخه آخر","Alliance":"t1_IsLatestVersion","Address":"[t1].[IsLatestVersion]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data === true || data === ''بله'' || data === ''TRUE'' || data === 1 || data === ''1'') return ''TRUE''; return ''FALSE''; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_PreparationShamsiDate","DisplayName":"تاریخ آماده سازی","Alliance":"t1_PreparationShamsiDate","Address":"[t1].[PreparationShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"system.User","ColumnName":"t6_Name","DisplayName":"کارشناس فروش","Alliance":"t6_Name","Address":"[t6].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":125,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Edms.Project","ColumnName":"t11_Name","DisplayName":"نام پروژه مهندسی","Alliance":"t11_Name","Address":"[t11].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_EngineeringConsideration","DisplayName":"ملاحظات مهندسی","Alliance":"t1_EngineeringConsideration","Address":"[t1].[EngineeringConsideration]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t1_SalesConsideration","DisplayName":"ملاحظات فروش","Alliance":"t1_SalesConsideration","Address":"[t1].[SalesConsideration]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"tWh_InWarehouseTimeInDay","DisplayName":"مدت ماندگاری در انبار (روز)","Alliance":"tWh_InWarehouseTimeInDay","Address":"DATEDIFF(DAY, [t1].[PreparationMiladiDate], GETDATE())","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow": "", "onRowAdded": "function(ctx) {\r\n    var data = ctx.rowData;\r\n    var $row = ctx.$row;\r\n\r\n     $row.off(''click'', ''td.dt-control'').on(''click'', ''td.dt-control'', async function () {\r\n        const tr = $(this).closest(''tr'');\r\n        const table = $(this).closest(''table'').DataTable();\r\n        const row = table.row(tr);\r\n\r\n         if (row.child.isShown()) {\r\n            row.child.hide();\r\n            tr.removeClass(''shown'');\r\n            $(this).html(''<i class=\"fa fa-plus-circle text-primary\"></i>'');\r\n            return;\r\n        }\r\n\r\n         const productionOrderItemId = ctx.primaryKeyValue;\r\n        if (!productionOrderItemId) {\r\n            console.error(''primaryKeyValue is not defined!'');\r\n            return;\r\n        }\r\n\r\n        const url = `/Panel/ProductionOrderItem/ProductionOrderItemCommentListPartial?productionOrderItemId=${productionOrderItemId}`;\r\n\r\n        try {\r\n             let htmlContent = await get(url);\r\n\r\n             const $html = $(htmlContent);\r\n            $html.find(''table'').css({ width: ''fit-content'' });\r\n\r\n             row.child($html).show();\r\n            tr.addClass(''shown'');\r\n            $(this).html(''<i class=\"fa fa-minus-circle text-danger\"></i>'');\r\n        } catch (error) {\r\n            console.error(''خطا در بارگذاری محتوای child:'', error);\r\n         }\r\n    });\r\n}"}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51600, N'ColumnsJson / QueryJson / EventScriptsJson معتبر نیست.', 1;

    DECLARE @ProfileId BIGINT;
    SELECT @ProfileId = Id FROM [system].[SavedQuery] WHERE Name = @Name;

    IF @ProfileId IS NOT NULL
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            EntityFullName = @EntityFullName,
            Mode = 1,
            [Type] = 1,
            ActionOptions = @ActionOptions,
            CustomActionButtonsJson = @CustomButtons,
            EventScriptsJson = @EventScripts,
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Id = @ProfileId;
        PRINT N'SavedQuery updated: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
        (
            Name, Title, QueryJson, ColumnsJson, DiagramJson,
            CustomActionButtonsJson, EventScriptsJson,
            Mode, [Type], EntityFullName, ActionOptions,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        VALUES
        (
            @Name, @Title, @QueryJson, @ColumnsJson, N'{}',
            @CustomButtons, @EventScripts,
            1, 1, @EntityFullName, @ActionOptions,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @ProfileId = SCOPE_IDENTITY();
        PRINT N'SavedQuery inserted: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.RowId = @ProfileId
      AND ra.ActionAccessType = 3
      AND ra.EntityName = @EntityPascal;

    INSERT INTO [system].[RoleAccess] (Path, ActionAccessType, RoleId, RowId, EntityName)
    SELECT N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)), 3, r.Id, @ProfileId, @EntityPascal
    FROM [system].[Role] r
    WHERE r.Name IN (N'Sale.ProductionOrderItem.ReadyForShipping', N'ShowAllMenus');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT Id, Name, Title, EntityFullName
FROM [system].[SavedQuery]
WHERE Name = N'POI_All_ReadyForShipping';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'POI_All_ReadyForShipping';
