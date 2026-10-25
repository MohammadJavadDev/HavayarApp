/*
  گزارش قطعات مصرفی در محصولات — HTS Gnr_Report 7 (Sale_RequestPart) صفحه ۱۸۹.
  نمایه روی صفحه گزارشات با EntityFullName = AfterSalesReports.
  ارزش افزوده در داده قدیمی همیشه صفر است و ستون ندارد.
  برای قطعه بدون قلم حواله و بدون قیمت واحد، مبلغ کل خالی می‌ماند؛ سیستم قبلی آخرین قیمت خرید را می‌گذاشت.
  UTF-8 BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-sale-requestpart';
    DECLARE @ProfileName NVARCHAR(100) = N'AfterSales_Sale_RequestPart';
    DECLARE @ProfileTitle NVARCHAR(200) = N'گزارش قطعات مصرفی در محصولات';
    DECLARE @EntityName NVARCHAR(200) = N'AfterSalesReports';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/sale/aftersalesreport/list';
    DECLARE @Sql NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [tSr].[Id] AS [tSr_Id],
    CASE [tOrd].[HtsStockId]
        WHEN 1 THEN N''انبار خدمات HY''
        WHEN 4 THEN N''انبار مواد و قطعات هوایار''
        WHEN 6 THEN N''انبار محصولات''
        WHEN 7 THEN N''انبار سامسونگ''
        WHEN 11 THEN N''انبار تعمیراتی قطعات''
        WHEN 17 THEN N''انبار خدمات CNG''
        WHEN 27 THEN N''انبار مواد و قطعات''
        WHEN 29 THEN N''انبار قابل تعمیر محصول''
        WHEN 30 THEN N''انبار تعمیراتی محصول''
        ELSE CONVERT(nvarchar(20), [tOrd].[HtsStockId])
    END AS [tStock_Title],
    CASE COALESCE([tPartOrd].[VoucherTypeStatus], [tOrd].[VoucherTypeStatus])
        WHEN 97 THEN N''نقدی'' WHEN 98 THEN N''هدیه'' WHEN 99 THEN N''تخفیفی''
        WHEN 3172 THEN N''برگشتی به سال قبل 9%'' WHEN 3173 THEN N''اعتباری'' WHEN 3174 THEN N''امانی''
        ELSE CONVERT(nvarchar(20), COALESCE([tPartOrd].[VoucherTypeStatus], [tOrd].[VoucherTypeStatus]))
    END AS [tVchType_Title],
    CONVERT(bigint, CASE WHEN [t1].[OrderDetailId] IS NOT NULL THEN [tPartOrd].[OrderNumber] ELSE [t1].[ProjectVchNum] END) AS [tVchNo],
    CASE WHEN [t1].[OrderDetailId] IS NOT NULL THEN [tPartOrd].[VchDateShamsiDate] ELSE [t1].[ProjectVchShamsiDate] END AS [tVchDate_Shamsi],
    [tCc].[Title] AS [tCc_Title],
    [tCust].[Code] AS [tCust_Code],
    [tParty].[FullName] AS [tParty_FullName],
    COALESCE([tProd].[Code], [tOth].[Code]) AS [tProduct_Code],
    COALESCE([tProd].[Name], [tOth].[Name]) AS [tProduct_Name],
    COALESCE([tSer].[Serial], [tDet].[OtherPartSerial]) AS [tSerial],
    [tSer].[Model] AS [tModel],
    [tSer].[ProductionOrderNumber] AS [tProductionOrder_Number],
    COALESCE([tPartOd].[Code], [tPartPrj].[Code]) AS [tPart_Code],
    COALESCE([tPartOd].[Name], [tPartPrj].[Name]) AS [tPart_Name],
    COALESCE([tUnit].[Title], [tPartUnit].[Title]) AS [tUnit_Title],
    [t1].[Mount] AS [t1_Mount],
    [t1].[UnitPrice] AS [t1_UnitPrice],
    [tPod].[IncPrice] AS [tIncPrice],
    [tPod].[DecPrice] AS [tDecPrice],
    CASE
        WHEN [t1].[OrderDetailId] IS NOT NULL AND [t1].[ServiceType] = 150
            THEN ISNULL([tPod].[Price], 0) + ISNULL([tPod].[IncPrice], 0) - ISNULL([tPod].[DecPrice], 0)
        WHEN [t1].[UnitPrice] IS NOT NULL
            THEN CONVERT(bigint, [t1].[UnitPrice] * [t1].[Mount])
        ELSE NULL
    END AS [tTotal_Price],
    [t1].[ReturnLicence] AS [t1_ReturnLicence],
    [t1].[ReturnDamagedPart] AS [t1_ReturnDamagedPart],
    [tRep].[Code] AS [tRep_Code],
    [tRep].[Name] AS [tRep_Name],
    CASE [t1].[ServiceType]
        WHEN 148 THEN N''مصرف پروژه'' WHEN 149 THEN N''گارانتی'' WHEN 150 THEN N''وارانتی''
        WHEN 151 THEN N''گارانتی مشروط'' WHEN 157 THEN N''تخفیفی'' WHEN 185 THEN N''مصرف پروژه گارانتی''
        ELSE CONVERT(nvarchar(20), [t1].[ServiceType])
    END AS [tServiceType_Title],
    [tSr].[Comment] AS [tSr_Comment],
    [t1].[Comment] AS [t1_Comment],
    [tProv].[Name] AS [tProv_Name],
    [tZone].[Title] AS [tZone_Title],
    [t1].[DamagedPartDescription] AS [t1_DamagedPartDescription],
    [t1].[DamagedParts] AS [t1_DamagedParts],
    [t1].[HasFailure] AS [t1_HasFailure],
    [t1].[FailureParts] AS [t1_FailureParts],
    [t1].[FailureDescription] AS [t1_FailureDescription],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Sale].[ServiceRequestPart] AS [t1]
LEFT JOIN [Sale].[ServiceRequestDetail] AS [tDet] ON [tDet].[Id] = [t1].[ServiceRequestDetailId]
LEFT JOIN [Sale].[ServiceRequest] AS [tSr] ON [tSr].[Id] = [tDet].[ServiceRequestId]
LEFT JOIN [SLS].[Customer] AS [tCust] ON [tCust].[Id] = [tSr].[CustomerId]
LEFT JOIN [Gnr].[Party] AS [tParty] ON [tParty].[Id] = [tCust].[PartyId]
LEFT JOIN [SLS].[CustomerAddress] AS [tAddr] ON [tAddr].[Id] = [tSr].[CustomerAddressId]
LEFT JOIN [Gnr].[Region] AS [tProv] ON [tProv].[Id] = COALESCE([tCust].[ProvinceId], [tAddr].[ProvinceId])
LEFT JOIN [Crm].[Zone] AS [tZone] ON [tZone].[Id] = [tAddr].[ZoneId]
LEFT JOIN [Gnr].[CostCenter] AS [tCc] ON [tCc].[Id] = [t1].[CostCenterId]
LEFT JOIN [Sale].[OrderDetail] AS [tPod] ON [tPod].[Id] = [t1].[OrderDetailId]
LEFT JOIN [Sale].[Order] AS [tPartOrd] ON [tPartOrd].[Id] = [tPod].[Sale_OrderId]
LEFT JOIN [Inv].[Part] AS [tPartOd] ON [tPartOd].[Id] = [tPod].[PartId]
LEFT JOIN [Inv].[Part] AS [tPartPrj] ON [tPartPrj].[HtsId] = [t1].[HtsProjectVchPartId] AND [t1].[HtsProjectVchPartId] IS NOT NULL AND [tPartPrj].[HtsId] <> 0
LEFT JOIN [Sale].[OrderDetail] AS [tDod] ON [tDod].[Id] = [tDet].[OrderDetailId]
LEFT JOIN [Sale].[Order] AS [tOrd] ON [tOrd].[Id] = [tDod].[Sale_OrderId]
LEFT JOIN [Inv].[Part] AS [tProd] ON [tProd].[Id] = [tDod].[PartId]
LEFT JOIN [Inv].[Part] AS [tOth] ON [tOth].[Id] = [tDet].[OtherPartId]
LEFT JOIN [Sale].[OrderDetailSerial] AS [tSer] ON [tSer].[Id] = [tDet].[OrderDetailSerialId]
LEFT JOIN [Inv].[Part] AS [tRep] ON [tRep].[Id] = [t1].[ReplacePartId]
LEFT JOIN [Inv].[PartUnit] AS [tUnit] ON [tUnit].[Id] = [t1].[PartUnitId]
LEFT JOIN [Inv].[PartUnit] AS [tPartUnit] ON [tPartUnit].[Id] = COALESCE([tPartOd].[UnitId], [tPartPrj].[UnitId])';
    DECLARE @Columns NVARCHAR(MAX) = N'[{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequest","ColumnName":"tSr_Id","DisplayName":"شماره اندیکاتور","Alliance":"tSr_Id","Address":"[tSr].[Id]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"tStock_Title","DisplayName":"انبار","Alliance":"tStock_Title","Address":"CASE [tOrd].[HtsStockId]\n        WHEN 1 THEN N''انبار خدمات HY''\n        WHEN 4 THEN N''انبار مواد و قطعات هوایار''\n        WHEN 6 THEN N''انبار محصولات''\n        WHEN 7 THEN N''انبار سامسونگ''\n        WHEN 11 THEN N''انبار تعمیراتی قطعات''\n        WHEN 17 THEN N''انبار خدمات CNG''\n        WHEN 27 THEN N''انبار مواد و قطعات''\n        WHEN 29 THEN N''انبار قابل تعمیر محصول''\n        WHEN 30 THEN N''انبار تعمیراتی محصول''\n        ELSE CONVERT(nvarchar(20), [tOrd].[HtsStockId])\n    END","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"tVchType_Title","DisplayName":"نوع حواله","Alliance":"tVchType_Title","Address":"CASE COALESCE([tPartOrd].[VoucherTypeStatus], [tOrd].[VoucherTypeStatus])\n        WHEN 97 THEN N''نقدی'' WHEN 98 THEN N''هدیه'' WHEN 99 THEN N''تخفیفی''\n        WHEN 3172 THEN N''برگشتی به سال قبل 9%'' WHEN 3173 THEN N''اعتباری'' WHEN 3174 THEN N''امانی''\n        ELSE CONVERT(nvarchar(20), COALESCE([tPartOrd].[VoucherTypeStatus], [tOrd].[VoucherTypeStatus]))\n    END","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"tVchNo","DisplayName":"شماره حواله","Alliance":"tVchNo","Address":"CONVERT(bigint, CASE WHEN [t1].[OrderDetailId] IS NOT NULL THEN [tPartOrd].[OrderNumber] ELSE [t1].[ProjectVchNum] END)","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"tVchDate_Shamsi","DisplayName":"تاریخ حواله","Alliance":"tVchDate_Shamsi","Address":"CASE WHEN [t1].[OrderDetailId] IS NOT NULL THEN [tPartOrd].[VchDateShamsiDate] ELSE [t1].[ProjectVchShamsiDate] END","SystemTypeName":"DateShamsi","SystemType":5,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.CostCenter","ColumnName":"tCc_Title","DisplayName":"مرکز هزینه","Alliance":"tCc_Title","Address":"[tCc].[Title]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.Customer","ColumnName":"tCust_Code","DisplayName":"کد مشتری","Alliance":"tCust_Code","Address":"[tCust].[Code]","SystemTypeName":"Int","SystemType":7,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Party","ColumnName":"tParty_FullName","DisplayName":"مشتری","Alliance":"tParty_FullName","Address":"[tParty].[FullName]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"tProduct_Code","DisplayName":"کد محصول","Alliance":"tProduct_Code","Address":"COALESCE([tProd].[Code], [tOth].[Code])","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"tProduct_Name","DisplayName":"نام محصول","Alliance":"tProduct_Name","Address":"COALESCE([tProd].[Name], [tOth].[Name])","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetailSerial","ColumnName":"tSerial","DisplayName":"سریال","Alliance":"tSerial","Address":"COALESCE([tSer].[Serial], [tDet].[OtherPartSerial])","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetailSerial","ColumnName":"tModel","DisplayName":"مدل","Alliance":"tModel","Address":"[tSer].[Model]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetailSerial","ColumnName":"tProductionOrder_Number","DisplayName":"شماره سفارش ساخت","Alliance":"tProductionOrder_Number","Address":"[tSer].[ProductionOrderNumber]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"tPart_Code","DisplayName":"کد قطعه","Alliance":"tPart_Code","Address":"COALESCE([tPartOd].[Code], [tPartPrj].[Code])","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"tPart_Name","DisplayName":"نام قطعه","Alliance":"tPart_Name","Address":"COALESCE([tPartOd].[Name], [tPartPrj].[Name])","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.PartUnit","ColumnName":"tUnit_Title","DisplayName":"واحد","Alliance":"tUnit_Title","Address":"COALESCE([tUnit].[Title], [tPartUnit].[Title])","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_Mount","DisplayName":"مقدار","Alliance":"t1_Mount","Address":"[t1].[Mount]","SystemTypeName":"Decimal","SystemType":14,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_UnitPrice","DisplayName":"قیمت واحد","Alliance":"t1_UnitPrice","Address":"[t1].[UnitPrice]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"tIncPrice","DisplayName":"اضافات","Alliance":"tIncPrice","Address":"[tPod].[IncPrice]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"tDecPrice","DisplayName":"کسور","Alliance":"tDecPrice","Address":"[tPod].[DecPrice]","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"tTotal_Price","DisplayName":"مبلغ کل","Alliance":"tTotal_Price","Address":"CASE\n        WHEN [t1].[OrderDetailId] IS NOT NULL AND [t1].[ServiceType] = 150\n            THEN ISNULL([tPod].[Price], 0) + ISNULL([tPod].[IncPrice], 0) - ISNULL([tPod].[DecPrice], 0)\n        WHEN [t1].[UnitPrice] IS NOT NULL\n            THEN CONVERT(bigint, [t1].[UnitPrice] * [t1].[Mount])\n        ELSE NULL\n    END","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_ReturnLicence","DisplayName":"مجوز برگشت","Alliance":"t1_ReturnLicence","Address":"[t1].[ReturnLicence]","SystemTypeName":"Boolean","SystemType":1,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_ReturnDamagedPart","DisplayName":"داغی دارد","Alliance":"t1_ReturnDamagedPart","Address":"[t1].[ReturnDamagedPart]","SystemTypeName":"Boolean","SystemType":1,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"tRep_Code","DisplayName":"کد قطعه جایگزین","Alliance":"tRep_Code","Address":"[tRep].[Code]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"tRep_Name","DisplayName":"نام قطعه جایگزین","Alliance":"tRep_Name","Address":"[tRep].[Name]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"tServiceType_Title","DisplayName":"نوع خدمت","Alliance":"tServiceType_Title","Address":"CASE [t1].[ServiceType]\n        WHEN 148 THEN N''مصرف پروژه'' WHEN 149 THEN N''گارانتی'' WHEN 150 THEN N''وارانتی''\n        WHEN 151 THEN N''گارانتی مشروط'' WHEN 157 THEN N''تخفیفی'' WHEN 185 THEN N''مصرف پروژه گارانتی''\n        ELSE CONVERT(nvarchar(20), [t1].[ServiceType])\n    END","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequest","ColumnName":"tSr_Comment","DisplayName":"توضیحات درخواست","Alliance":"tSr_Comment","Address":"[tSr].[Comment]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_Comment","DisplayName":"توضیحات قطعه","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Region","ColumnName":"tProv_Name","DisplayName":"استان","Alliance":"tProv_Name","Address":"[tProv].[Name]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.Zone","ColumnName":"tZone_Title","DisplayName":"منطقه","Alliance":"tZone_Title","Address":"[tZone].[Title]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_DamagedPartDescription","DisplayName":"توضیح داغی","Alliance":"t1_DamagedPartDescription","Address":"[t1].[DamagedPartDescription]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_DamagedParts","DisplayName":"قطعات داغی","Alliance":"t1_DamagedParts","Address":"[t1].[DamagedParts]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_HasFailure","DisplayName":"خرابی دارد","Alliance":"t1_HasFailure","Address":"[t1].[HasFailure]","SystemTypeName":"Boolean","SystemType":1,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_FailureParts","DisplayName":"قطعات معیوب","Alliance":"t1_FailureParts","Address":"[t1].[FailureParts]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_FailureDescription","DisplayName":"شرح خرابی","Alliance":"t1_FailureDescription","Address":"[t1].[FailureDescription]","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ServiceRequestPart","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SystemType":4,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @Actions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @QueryJson NVARCHAR(MAX) =
        N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"'
        + REPLACE(REPLACE(REPLACE(@Sql, NCHAR(13), N''), NCHAR(10), N' '), N'"', N'\"')
        + N'","Parameters":[],"Selects":null}';

    IF OBJECT_ID('tempdb..#HtsGroup') IS NOT NULL DROP TABLE #HtsGroup;
    CREATE TABLE #HtsGroup (HtsGroupId INT NOT NULL PRIMARY KEY, Title NVARCHAR(200) NOT NULL, RoleId BIGINT NULL, RoleName NVARCHAR(200) NULL);
    INSERT INTO #HtsGroup (HtsGroupId, Title) VALUES
        (32, N'مدیر سیستم فروش'),
        (48, N'کارشناسان خدمات پس از فروش'),
        (61, N'دسترسی مشاهده-سیستم خدمات پس از فروش'),
        (69, N'مدیر سیستم خدمات پس ازفروش'),
        (102, N'نمایندگان فروش'),
        (190, N'کارشناسان گزارشات عمومی خدمات پس از فروش'),
        (350, N'مشاهده کلی HTS');

    UPDATE h SET RoleId = r.Id, RoleName = r.Name
    FROM #HtsGroup h INNER JOIN system.Role r ON r.Name = h.Title;

    DECLARE @Gid INT, @Title NVARCHAR(200), @WantedId BIGINT, @RoleName NVARCHAR(200), @RoleId BIGINT;
    DECLARE g_cur CURSOR LOCAL FAST_FORWARD FOR SELECT HtsGroupId, Title FROM #HtsGroup WHERE RoleId IS NULL;
    OPEN g_cur;
    FETCH NEXT FROM g_cur INTO @Gid, @Title;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @WantedId = 260000 + @Gid;
        SET @RoleName = @Title;
        IF EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = @RoleName AND r.Id <> @WantedId)
            SET @RoleName = @Title + N' (' + CAST(@Gid AS nvarchar(20)) + N')';
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @WantedId)
            UPDATE system.Role SET Name = @RoleName, Title = @Title, IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi WHERE Id = @WantedId;
        ELSE
        BEGIN
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@WantedId, @RoleName, @Title, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
        END
        UPDATE #HtsGroup SET RoleId = @WantedId, RoleName = @RoleName WHERE HtsGroupId = @Gid;
        FETCH NEXT FROM g_cur INTO @Gid, @Title;
    END
    CLOSE g_cur; DEALLOCATE g_cur;

    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, RoleId BIGINT NOT NULL);

    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), h.RoleName, h.RoleId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    INNER JOIN #HtsGroup h ON h.HtsGroupId = m.UserGroup_FK
    WHERE u.IsActive = 1 AND h.RoleId IS NOT NULL
      AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), h.RoleName, h.RoleId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau ON pau.PageAction_FK = pa.PageAction_ID
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pau.User_FK
    INNER JOIN #HtsGroup h ON h.HtsGroupId = 61
    WHERE pa.Page_FK = 189 AND u.IsActive = 1 AND h.RoleId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] gm WHERE gm.User_FK = u.User_ID AND gm.UserGroup_FK IN (32,48,61,69,102,190,350))
      AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE LOWER(x.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N''))));

    ;WITH d AS (
        SELECT Username, RoleId, ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleId ORDER BY Username) AS rn FROM #UserRoleMap
    ) DELETE FROM d WHERE rn > 1;

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName, RoleId FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName, @MapRoleId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MapUserId = NULL;
        SELECT TOP 1 @MapUserId = Id FROM system.[User] WHERE LOWER(Username) = LOWER(@MapUsername);
        IF @MapUserId IS NOT NULL AND NOT (
            ISJSON(ISNULL((SELECT RoleIds FROM system.[User] WHERE Id = @MapUserId), N'[]')) = 1
            AND EXISTS (SELECT 1 FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId)
        )
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]') OR ISJSON(RoleIds) = 0
                    THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']') END,
                Roles = CASE WHEN ISJSON(ISNULL(Roles, N'[]')) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]') OR ISJSON(Roles) = 0
                    THEN N'["' + REPLACE(@MapRoleName, N'"', N'') + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + REPLACE(@MapRoleName, N'"', N'') + N'"]') END,
                ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
        END
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName, @MapRoleId;
    END
    CLOSE map_cur; DEALLOCATE map_cur;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
        UPDATE system.SavedQuery
        SET Title = @ProfileTitle, EntityFullName = @EntityName, Mode = 1, [Type] = 1,
            QueryJson = @QueryJson, ColumnsJson = @Columns, ActionOptions = @Actions,
            CustomActionButtonsJson = N'[]', EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}', DiagramJson = N'{}',
            IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @ProfileName;
    ELSE
        INSERT INTO system.SavedQuery
            (Name, Title, EntityFullName, Mode, [Type], QueryJson, ColumnsJson, ActionOptions, CustomActionButtonsJson, EventScriptsJson, DiagramJson,
             CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@ProfileName, @ProfileTitle, @EntityName, 1, 1, @QueryJson, @Columns, @Actions, N'[]', N'{"onSelectedRow":"","onRowAdded":""}', N'{}',
             1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

    DECLARE @ProfileId BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @ProfileName);
    DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @ProfileId;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)), 3, 7, @EntityName, @ProfileTitle, NULL, @ProfileId, h.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #HtsGroup h WHERE h.RoleId IS NOT NULL;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, NULL, N'لیست گزارشات', NULL, NULL, h.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #HtsGroup h
    WHERE h.RoleId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM system.RoleAccess x WHERE x.RoleId = h.RoleId AND x.Path = @PagePath AND x.ActionAccessType = 1);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

SELECT q.Id, q.Name, q.Title, q.EntityFullName, ISJSON(q.QueryJson) AS QueryOk, ISJSON(q.ColumnsJson) AS ColOk
FROM system.SavedQuery q WHERE q.Name = N'AfterSales_Sale_RequestPart';

SELECT r.Name
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
WHERE q.Name = N'AfterSales_Sale_RequestPart' AND ra.ActionAccessType = 3;
