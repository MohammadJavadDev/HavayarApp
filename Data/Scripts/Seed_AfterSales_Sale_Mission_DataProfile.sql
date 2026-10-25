/*
  گزارش حق ماموریت پرسنل — HTS Gnr_Report 6 (Sale_Mission).
  روی صفحه کاتالوگ گزارشات (datatableprofile unicode = AfterSalesReports).
  دسترسی از گروه‌های HTS صفحه ۱۸۸: 32، 48، 61، 69، 350 و اعطای مستقیم فعال همان صفحه.
  فاکتور صبحانه/ناهار/شام/هتل و هزینه هتل از Sale_Mission منتقل می‌شوند. مجموع هزینه همان Total_Cost سیستم قبلی است و هزینه هتل را داخل خود ندارد.
  UTF-8 BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-sale-mission';
    DECLARE @ProfileName NVARCHAR(100) = N'AfterSales_Sale_Mission';
    DECLARE @ProfileTitle NVARCHAR(200) = N'گزارش حق ماموریت پرسنل';
    DECLARE @EntityName NVARCHAR(200) = N'AfterSalesReports';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/sale/aftersalesreport/list';
    DECLARE @Sql NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[Id] AS [tSr_Id],
    [t1].[HaveGuarantee] AS [t1_HaveGuarantee],
    [t3].[Code] AS [t3_Code],
    [t4].[FullName] AS [t4_FullName],
    [t5].[Title] AS [t5_Title],
    [t6].[Name] AS [t6_Name],
    [t1].[HokmNumber] AS [t1_HokmNumber],
    COALESCE(NULLIF(LTRIM(RTRIM(ISNULL([t7].[Name], N'''') + N'' '' + ISNULL([t7].[Family], N''''))), N''''), [t8].[Name]) AS [tExpert_Name],
    [t9].[Title] AS [t9_Title],
    STUFF((
        SELECT DISTINCT N''، '' + br.Title
        FROM [Sale].[ServiceRequestDetail] d
        INNER JOIN [Sale].[OrderDetail] od ON od.Id = d.OrderDetailId
        INNER JOIN [Sale].[Order] o ON o.Id = od.Sale_OrderId
        INNER JOIN [Sale].[Branch] br ON br.Id = o.BranchId
        WHERE d.ServiceRequestId = [t1].[ServiceRequestId] AND br.Title IS NOT NULL
        FOR XML PATH(''''), TYPE).value(''.'', ''nvarchar(max)''), 1, 2, N'''') AS [tBranch_Title],
    STUFF((
        SELECT DISTINCT N''، '' + CASE d.RequestType
            WHEN 1 THEN N''آموزش'' WHEN 2 THEN N''اورهال'' WHEN 3 THEN N''بازدید و مشاوره''
            WHEN 4 THEN N''تعمیرات گارانتی'' WHEN 5 THEN N''تعمیرات وارانتی''
            WHEN 6 THEN N''راه اندازی اولیه'' WHEN 7 THEN N''پایپینگ''
            WHEN 8 THEN N''سرویس دوره ای'' WHEN 9 THEN N''سایر موارد''
            WHEN 10 THEN N''OPI'' WHEN 11 THEN N''راه اندازی تجهیز تعمیری''
            WHEN 12 THEN N''پیش راه اندازی'' WHEN 13 THEN N''نصب''
            ELSE CAST(d.RequestType AS nvarchar(10)) END
        FROM [Sale].[ServiceRequestDetail] d
        WHERE d.ServiceRequestId = [t1].[ServiceRequestId]
        FOR XML PATH(''''), TYPE).value(''.'', ''nvarchar(max)''), 1, 2, N'''') AS [tReqType_Title],
    [t1].[DispatchShamsiDate] AS [t1_DispatchShamsiDate],
    [t1].[DispatchTime] AS [t1_DispatchTime],
    [t1].[ReturnShamsiDate] AS [t1_ReturnShamsiDate],
    [t1].[ReturnTime] AS [t1_ReturnTime],
    [t1].[WorkOvertimeHour] AS [t1_WorkOvertimeHour],
    [t1].[WorkOvertimeFee] AS [t1_WorkOvertimeFee],
    [t1].[HasFactorBreakfast] AS [t1_HasFactorBreakfast],
    [t1].[BreakfastFee] AS [t1_BreakfastFee],
    [t1].[HasFactorLunch] AS [t1_HasFactorLunch],
    [t1].[LunchFee] AS [t1_LunchFee],
    [t1].[HasFactorDinner] AS [t1_HasFactorDinner],
    [t1].[DinnerFee] AS [t1_DinnerFee],
    [t1].[HasFactorHotel] AS [t1_HasFactorHotel],
    [t1].[HotelCost] AS [t1_HotelCost],
    CASE
        WHEN [tTime].[Mins] IS NULL THEN NULL
        WHEN [tTime].[Mins] >= 60 THEN CAST([tTime].[Mins] / 60 AS nvarchar(20)) + N'':'' + RIGHT(N''00'' + CAST(ABS([tTime].[Mins] % 60) AS nvarchar(10)), 2)
        ELSE N''00:'' + RIGHT(N''00'' + CAST(ABS([tTime].[Mins] % 60) AS nvarchar(10)), 2)
    END AS [tMissionTime],
    ISNULL(CAST([t1].[BreakfastFee] AS bigint) * [t1].[Breakfast], 0)
        + ISNULL(CAST([t1].[LunchFee] AS bigint) * [t1].[Lunch], 0)
        + ISNULL(CAST([t1].[DinnerFee] AS bigint) * [t1].[Dinner], 0)
        + ISNULL(CAST([t1].[NightRight] AS bigint) * [t1].[NightRightFactor], 0)
        + CONVERT(bigint, ISNULL((CAST([t1].[WorkOvertimeFee] AS bigint) * [t1].[WorkOvertimeHour]) / 60, 0))
        + ISNULL(CAST([t1].[MissionRight] AS bigint) * [t1].[MissionRightFactor], 0)
        + ISNULL(CAST([t1].[MissionHolidayRight] AS bigint) * [t1].[MissionHolidayRightFactor], 0)
        + ISNULL([t1].[TransportationPersonal], 0)
        + ISNULL([t1].[TransportationDriving], 0)
        + ISNULL([t1].[OtherCost], 0) AS [tTotal_Cost],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Sale].[Mission] AS [t1]
LEFT JOIN [Sale].[ServiceRequest] AS [t2] ON [t1].[ServiceRequestId] = [t2].[Id]
LEFT JOIN [SLS].[Customer] AS [t3] ON [t2].[CustomerId] = [t3].[Id]
LEFT JOIN [Gnr].[Party] AS [t4] ON [t3].[PartyId] = [t4].[Id]
LEFT JOIN [SLS].[CustomerAddress] AS [t5] ON [t2].[CustomerAddressId] = [t5].[Id]
LEFT JOIN [Gnr].[Region] AS [t6] ON [t6].[Id] = COALESCE([t3].[ProvinceId], [t5].[ProvinceId])
LEFT JOIN [Hcm].[Personel] AS [t7] ON [t1].[PersonelId] = [t7].[Id]
LEFT JOIN [system].[User] AS [t8] ON [t1].[ExpertId] = [t8].[Id]
LEFT JOIN [Gnr].[CostCenter] AS [t9] ON [t1].[CostCenterId] = [t9].[Id]
CROSS APPLY (
    SELECT CASE
        WHEN [t1].[DispatchMiladiDate] IS NULL OR [t1].[ReturnMiladiDate] IS NULL THEN NULL
        ELSE
            DATEDIFF(MINUTE, CAST([t1].[DispatchMiladiDate] AS datetime2), CAST([t1].[ReturnMiladiDate] AS datetime2))
            + ISNULL(TRY_CONVERT(int, PARSENAME(REPLACE([t1].[ReturnTime], N'':'', N''.''), 2)), 0) * 60
            + ISNULL(TRY_CONVERT(int, PARSENAME(REPLACE([t1].[ReturnTime], N'':'', N''.''), 1)), 0)
            - ISNULL(TRY_CONVERT(int, PARSENAME(REPLACE([t1].[DispatchTime], N'':'', N''.''), 2)), 0) * 60
            - ISNULL(TRY_CONVERT(int, PARSENAME(REPLACE([t1].[DispatchTime], N'':'', N''.''), 1)), 0)
    END AS [Mins]
) AS [tTime]';
    DECLARE @Columns NVARCHAR(MAX) = N'[{"TableName":"Sale.Mission","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequest","ColumnName":"tSr_Id","DisplayName":"شماره اندیکاتور","Alliance":"tSr_Id","Address":"[t2].[Id]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_HaveGuarantee","DisplayName":"گارانتی","Alliance":"t1_HaveGuarantee","Address":"[t1].[HaveGuarantee]","SystemTypeName":"Boolean","SystemType":1,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.Customer","ColumnName":"t3_Code","DisplayName":"کد مشتری","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"Int","SystemType":7,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Party","ColumnName":"t4_FullName","DisplayName":"مشتری","Alliance":"t4_FullName","Address":"[t4].[FullName]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"t5_Title","DisplayName":"سایت مشتری","Alliance":"t5_Title","Address":"[t5].[Title]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Region","ColumnName":"t6_Name","DisplayName":"استان","Alliance":"t6_Name","Address":"[t6].[Name]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_HokmNumber","DisplayName":"شماره حکم","Alliance":"t1_HokmNumber","Address":"[t1].[HokmNumber]","SystemTypeName":"Int","SystemType":7,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hcm.Personel","ColumnName":"tExpert_Name","DisplayName":"کارشناسان ماموریت","Alliance":"tExpert_Name","Address":"COALESCE(NULLIF(LTRIM(RTRIM(ISNULL([t7].[Name], N'''') + N'' '' + ISNULL([t7].[Family], N''''))), N''''), [t8].[Name])","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.CostCenter","ColumnName":"t9_Title","DisplayName":"مرکز هزینه","Alliance":"t9_Title","Address":"[t9].[Title]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Branch","ColumnName":"tBranch_Title","DisplayName":"مرکز فروش","Alliance":"tBranch_Title","Address":"STUFF((\n        SELECT DISTINCT N''، '' + br.Title\n        FROM [Sale].[ServiceRequestDetail] d\n        INNER JOIN [Sale].[OrderDetail] od ON od.Id = d.OrderDetailId\n        INNER JOIN [Sale].[Order] o ON o.Id = od.Sale_OrderId\n        INNER JOIN [Sale].[Branch] br ON br.Id = o.BranchId\n        WHERE d.ServiceRequestId = [t1].[ServiceRequestId] AND br.Title IS NOT NULL\n        FOR XML PATH(''''), TYPE).value(''.'', ''nvarchar(max)''), 1, 2, N'''')","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestDetail","ColumnName":"tReqType_Title","DisplayName":"نوع درخواست","Alliance":"tReqType_Title","Address":"STUFF((\n        SELECT DISTINCT N''، '' + CASE d.RequestType\n            WHEN 1 THEN N''آموزش'' WHEN 2 THEN N''اورهال'' WHEN 3 THEN N''بازدید و مشاوره''\n            WHEN 4 THEN N''تعمیرات گارانتی'' WHEN 5 THEN N''تعمیرات وارانتی''\n            WHEN 6 THEN N''راه اندازی اولیه'' WHEN 7 THEN N''پایپینگ''\n            WHEN 8 THEN N''سرویس دوره ای'' WHEN 9 THEN N''سایر موارد''\n            WHEN 10 THEN N''OPI'' WHEN 11 THEN N''راه اندازی تجهیز تعمیری''\n            WHEN 12 THEN N''پیش راه اندازی'' WHEN 13 THEN N''نصب''\n            ELSE CAST(d.RequestType AS nvarchar(10)) END\n        FROM [Sale].[ServiceRequestDetail] d\n        WHERE d.ServiceRequestId = [t1].[ServiceRequestId]\n        FOR XML PATH(''''), TYPE).value(''.'', ''nvarchar(max)''), 1, 2, N'''')","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_DispatchShamsiDate","DisplayName":"تاریخ اعزام","Alliance":"t1_DispatchShamsiDate","Address":"[t1].[DispatchShamsiDate]","SystemTypeName":"DateShamsi","SystemType":5,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_DispatchTime","DisplayName":"ساعت اعزام","Alliance":"t1_DispatchTime","Address":"[t1].[DispatchTime]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_ReturnShamsiDate","DisplayName":"تاریخ برگشت","Alliance":"t1_ReturnShamsiDate","Address":"[t1].[ReturnShamsiDate]","SystemTypeName":"DateShamsi","SystemType":5,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_ReturnTime","DisplayName":"ساعت برگشت","Alliance":"t1_ReturnTime","Address":"[t1].[ReturnTime]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_WorkOvertimeHour","DisplayName":"ساعت اضافه کاری","Alliance":"t1_WorkOvertimeHour","Address":"[t1].[WorkOvertimeHour]","SystemTypeName":"Int","SystemType":7,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_WorkOvertimeFee","DisplayName":"مبلغ اضافه کاری","Alliance":"t1_WorkOvertimeFee","Address":"[t1].[WorkOvertimeFee]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_HasFactorBreakfast","DisplayName":"فاکتور صبحانه","Alliance":"t1_HasFactorBreakfast","Address":"[t1].[HasFactorBreakfast]","SystemTypeName":"Boolean","SystemType":1,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_BreakfastFee","DisplayName":"هزینه صبحانه","Alliance":"t1_BreakfastFee","Address":"[t1].[BreakfastFee]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_HasFactorLunch","DisplayName":"فاکتور ناهار","Alliance":"t1_HasFactorLunch","Address":"[t1].[HasFactorLunch]","SystemTypeName":"Boolean","SystemType":1,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_LunchFee","DisplayName":"هزینه نهار","Alliance":"t1_LunchFee","Address":"[t1].[LunchFee]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_HasFactorDinner","DisplayName":"فاکتور شام","Alliance":"t1_HasFactorDinner","Address":"[t1].[HasFactorDinner]","SystemTypeName":"Boolean","SystemType":1,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_DinnerFee","DisplayName":"هزینه شام","Alliance":"t1_DinnerFee","Address":"[t1].[DinnerFee]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_HasFactorHotel","DisplayName":"فاکتور هتل","Alliance":"t1_HasFactorHotel","Address":"[t1].[HasFactorHotel]","SystemTypeName":"Boolean","SystemType":1,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_HotelCost","DisplayName":"هزینه هتل","Alliance":"t1_HotelCost","Address":"[t1].[HotelCost]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"tMissionTime","DisplayName":"ساعت ماموریت","Alliance":"tMissionTime","Address":"CASE\n        WHEN [tTime].[Mins] IS NULL THEN NULL\n        WHEN [tTime].[Mins] >= 60 THEN CAST([tTime].[Mins] / 60 AS nvarchar(20)) + N'':'' + RIGHT(N''00'' + CAST(ABS([tTime].[Mins] % 60) AS nvarchar(10)), 2)\n        ELSE N''00:'' + RIGHT(N''00'' + CAST(ABS([tTime].[Mins] % 60) AS nvarchar(10)), 2)\n    END","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"tTotal_Cost","DisplayName":"مجموع هزینه","Alliance":"tTotal_Cost","Address":"ISNULL(CAST([t1].[BreakfastFee] AS bigint) * [t1].[Breakfast], 0)\n        + ISNULL(CAST([t1].[LunchFee] AS bigint) * [t1].[Lunch], 0)\n        + ISNULL(CAST([t1].[DinnerFee] AS bigint) * [t1].[Dinner], 0)\n        + ISNULL(CAST([t1].[NightRight] AS bigint) * [t1].[NightRightFactor], 0)\n        + CONVERT(bigint, ISNULL((CAST([t1].[WorkOvertimeFee] AS bigint) * [t1].[WorkOvertimeHour]) / 60, 0))\n        + ISNULL(CAST([t1].[MissionRight] AS bigint) * [t1].[MissionRightFactor], 0)\n        + ISNULL(CAST([t1].[MissionHolidayRight] AS bigint) * [t1].[MissionHolidayRightFactor], 0)\n        + ISNULL([t1].[TransportationPersonal], 0)\n        + ISNULL([t1].[TransportationDriving], 0)\n        + ISNULL([t1].[OtherCost], 0)","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Mission","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SystemType":4,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @Actions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @QueryJson NVARCHAR(MAX) =
        N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"'
        + REPLACE(REPLACE(REPLACE(@Sql, NCHAR(13), N''), NCHAR(10), N' '), N'"', N'\"')
        + N'","Parameters":[],"Selects":null}';

    IF OBJECT_ID('tempdb..#HtsGroup') IS NOT NULL DROP TABLE #HtsGroup;
    CREATE TABLE #HtsGroup
    (
        HtsGroupId INT NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        RoleId BIGINT NULL,
        RoleName NVARCHAR(200) NULL
    );
    INSERT INTO #HtsGroup (HtsGroupId, Title) VALUES
        (32, N'مدیر سیستم فروش'),
        (48, N'کارشناسان خدمات پس از فروش'),
        (61, N'دسترسی مشاهده-سیستم خدمات پس از فروش'),
        (69, N'مدیر سیستم خدمات پس ازفروش'),
        (350, N'مشاهده کلی HTS');

    UPDATE h
    SET RoleId = r.Id, RoleName = r.Name
    FROM #HtsGroup h
    INNER JOIN system.Role r ON r.Name = h.Title;

    DECLARE @Gid INT, @Title NVARCHAR(200), @WantedId BIGINT, @RoleName NVARCHAR(200), @RoleId BIGINT;
    DECLARE g_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT HtsGroupId, Title FROM #HtsGroup WHERE RoleId IS NULL;
    OPEN g_cur;
    FETCH NEXT FROM g_cur INTO @Gid, @Title;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @WantedId = 260000 + @Gid;
        SET @RoleName = @Title;
        IF EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = @RoleName AND r.Id <> @WantedId)
            SET @RoleName = @Title + N' (' + CAST(@Gid AS nvarchar(20)) + N')';

        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @WantedId)
        BEGIN
            UPDATE system.Role
            SET Name = @RoleName, Title = @Title, IsActive = 1,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @WantedId;
            SET @RoleId = @WantedId;
        END
        ELSE
        BEGIN
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role
                (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
                 CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@WantedId, @RoleName, @Title, 1, @SeedUser, 1, @SeedUser,
                 @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
            SET @RoleId = @WantedId;
        END

        UPDATE #HtsGroup SET RoleId = @RoleId, RoleName = @RoleName WHERE HtsGroupId = @Gid;
        SET @RoleId = NULL;
        FETCH NEXT FROM g_cur INTO @Gid, @Title;
    END
    CLOSE g_cur;
    DEALLOCATE g_cur;

    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleName NVARCHAR(200) NOT NULL,
        RoleId BIGINT NOT NULL
    );

    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT COALESCE(
            NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
            NULLIF(LTRIM(RTRIM(u.Username)), N'')),
           h.RoleName,
           h.RoleId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    INNER JOIN #HtsGroup h ON h.HtsGroupId = m.UserGroup_FK
    WHERE u.IsActive = 1
      AND h.RoleId IS NOT NULL
      AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT u.Username, m.RoleName, m.RoleId
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u
        ON LOWER(u.ActiveDirectoryUsername) = LOWER(m.Username)
    WHERE u.IsActive = 1
      AND u.Username IS NOT NULL
      AND LOWER(u.Username) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(u.Username) AND x.RoleId = m.RoleId
      );

    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT COALESCE(
            NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
            NULLIF(LTRIM(RTRIM(u.Username)), N'')),
           h.RoleName,
           h.RoleId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau ON pau.PageAction_FK = pa.PageAction_ID
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pau.User_FK
    INNER JOIN #HtsGroup h ON h.HtsGroupId = 61
    WHERE pa.Page_FK = 188
      AND u.IsActive = 1
      AND h.RoleId IS NOT NULL
      AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] gm
          WHERE gm.User_FK = u.User_ID
            AND gm.UserGroup_FK IN (32, 48, 61, 69, 350)
      )
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(COALESCE(
                NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
                NULLIF(LTRIM(RTRIM(u.Username)), N'')))
      );

    ;WITH d AS (
        SELECT Username, RoleId,
               ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleId ORDER BY Username) AS rn
        FROM #UserRoleMap
    )
    DELETE FROM d WHERE rn > 1;

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0;
    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Username, RoleName, RoleId FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName, @MapRoleId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MapUserId = NULL;
        SELECT TOP 1 @MapUserId = Id
        FROM system.[User]
        WHERE LOWER(Username) = LOWER(@MapUsername);

        IF @MapUserId IS NULL
            SET @Missing = @Missing + 1;
        ELSE IF ISJSON(ISNULL((SELECT RoleIds FROM system.[User] WHERE Id = @MapUserId), N'[]')) = 1
            AND EXISTS (
                SELECT 1 FROM system.[User] u
                CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
            )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]' OR ISJSON(RoleIds) = 0
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN ISJSON(ISNULL(Roles, N'[]')) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]' OR ISJSON(Roles) = 0
                        THEN N'["' + REPLACE(@MapRoleName, N'"', N'') + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + REPLACE(@MapRoleName, N'"', N'') + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END

        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName, @MapRoleId;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
        UPDATE system.SavedQuery
        SET Title = @ProfileTitle,
            EntityFullName = @EntityName,
            Mode = 1,
            [Type] = 1,
            QueryJson = @QueryJson,
            ColumnsJson = @Columns,
            ActionOptions = @Actions,
            CustomActionButtonsJson = N'[]',
            EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}',
            DiagramJson = N'{}',
            IsActive = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @ProfileName;
    ELSE
        INSERT INTO system.SavedQuery
            (Name, Title, EntityFullName, Mode, [Type], QueryJson, ColumnsJson, ActionOptions,
             CustomActionButtonsJson, EventScriptsJson, DiagramJson,
             CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
             ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@ProfileName, @ProfileTitle, @EntityName, 1, 1, @QueryJson, @Columns, @Actions,
             N'[]', N'{"onSelectedRow":"","onRowAdded":""}', N'{}',
             1, @SeedUser, @Now, @NowShamsi,
             1, @SeedUser, @Now, @NowShamsi, 1);

    DECLARE @ProfileId BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @ProfileName);

    DELETE FROM system.RoleAccess
    WHERE ActionAccessType = 3 AND RowId = @ProfileId;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)), 3, 7, @EntityName, @ProfileTitle, NULL, @ProfileId, h.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #HtsGroup h
    WHERE h.RoleId IS NOT NULL;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, NULL, N'لیست گزارشات', NULL, NULL, h.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #HtsGroup h
    WHERE h.RoleId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = h.RoleId AND x.Path = @PagePath AND x.ActionAccessType = 1
      );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

SELECT q.Id, q.Name, q.Title, q.EntityFullName, ISJSON(q.QueryJson) AS QueryJsonOk, ISJSON(q.ColumnsJson) AS ColumnsJsonOk
FROM system.SavedQuery q
WHERE q.Name = N'AfterSales_Sale_Mission';

SELECT v.HtsGroupId, v.Title, r.Id AS RoleId, r.Name AS RoleName
FROM (
    VALUES
        (32, N'مدیر سیستم فروش'),
        (48, N'کارشناسان خدمات پس از فروش'),
        (61, N'دسترسی مشاهده-سیستم خدمات پس از فروش'),
        (69, N'مدیر سیستم خدمات پس ازفروش'),
        (350, N'مشاهده کلی HTS')
) v(HtsGroupId, Title)
LEFT JOIN system.Role r ON r.Id = 260000 + v.HtsGroupId;

SELECT r.Name, ra.Path, ra.EntityName, ra.ActionAccessType
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
WHERE q.Name = N'AfterSales_Sale_Mission' AND ra.ActionAccessType = 3;
