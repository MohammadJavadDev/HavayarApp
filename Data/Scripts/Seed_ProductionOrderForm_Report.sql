/*
فرم چاپی «سفارش ساخت» — معادل DevExpress قدیمی Pln_ProductionOrderReport (چاپ روی صفحه سفارش ساخت).
SavedQuery دو نتیجه دارد: dtMaster (سربرگ) و dtDetail (اقلام آخرین نسخه).
قالب Stimulsoft جداگانه در Data/Seed/SaleReports/ProductionOrderForm.json است و با اسکریپت محتوا روی ReportBuilderReport.Content می‌نشیند.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-production-order-form';
    DECLARE @Name NVARCHAR(200) = N'ProductionOrderForm';
    DECLARE @Title NVARCHAR(200) = N'سفارش ساخت';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    po.Id AS ProductionOrderId,
    COALESCE(CONVERT(nvarchar(50), po.ProductionOrderNumber), po.Number) AS ProductionOrder_Number,
    LEFT(ISNULL(po.CreatedOnShamsiDateTime, N''''), 10) AS CreatedDate,
    custParty.FullName AS Customer_Title,
    ind.Title AS CustomerIndustryHeader,
    po.CustomerIndustry AS CustomerIndustry,
    po.EndUser AS EndUser,
    COALESCE(dl.Code, CONVERT(nvarchar(50), po.ProjectDlCode)) AS ProjectDl,
    dl.Title AS ProjectDlTitle,
    br.Title AS SaleDepartment,
    COALESCE(NULLIF(LTRIM(RTRIM(se.NameFa)), N''''), se.Name) AS Sale_Personel,
    agencyParty.FullName AS SalesAgency,
    dbo.fn_MiladiToShamsi(po.MetreDate) AS MetreDate,
    po.MetreNumber AS MetreNumber,
    ct.Number AS ContractNumber,
    CAST(N'''' AS nvarchar(50)) AS RequirementsAdvise_Number,
    CASE po.DeliveryType
        WHEN 88 THEN N''پارت به پارت''
        WHEN 89 THEN N''تحویل کل سفارش''
        ELSE N''''
    END AS DeliverType,
    CAST(N'''' AS nvarchar(50)) AS ProjectType,
    CASE WHEN po.InstallationIsByHy = 1 THEN N''هوایار'' ELSE N'''' END AS InstallationType,
    city.Name AS InstallationCity,
    CAST(CASE WHEN po.IsNeedProjectManager = 1 THEN 1 ELSE 0 END AS bit) AS IsNeedProjectManager,
    CAST(CASE WHEN po.IsNeedInspectionBeforePacking = 1 THEN 1 ELSE 0 END AS bit) AS IsNeedInspectionBeforePacking,
    CAST(CASE WHEN po.CompressorHouseIsReady = 1 THEN 1 ELSE 0 END AS bit) AS CompressorHouseIsReady,
    CAST(CASE WHEN po.HasGA = 1 THEN 1 ELSE 0 END AS bit) AS HasGA,
    po.SalesComment AS SalesComment,
    CASE po.CentrifugeSetupType
        WHEN 1899 THEN N''تابلو راه اندازی | Star-Delta''
        WHEN 1900 THEN N''تابلو راه اندازی | DOL''
        WHEN 1901 THEN N''تابلو راه اندازی | Soft Starter''
        WHEN 1902 THEN N''تابلو راه اندازی ندارد''
        ELSE N''''
    END AS SetupMethod,
    CAST(CASE WHEN po.CentrifugeHasCompressor = 1 THEN 1 ELSE 0 END AS bit) AS CentrifugeHasCompressor,
    CASE WHEN po.CentrifugeCompressorCount IS NULL THEN N''''
         ELSE CONVERT(nvarchar(50), CONVERT(decimal(18, 2), po.CentrifugeCompressorCount))
    END AS CentrifugeCount,
    CASE WHEN po.CentrifugeElectromotorVoltage IS NULL THEN N''''
         ELSE CONVERT(nvarchar(50), CONVERT(decimal(18, 2), po.CentrifugeElectromotorVoltage))
    END AS ElectromotorVoltage
FROM Sale.ProductionOrder AS po
LEFT JOIN SLS.Contract AS ct ON ct.Id = po.ContractId
LEFT JOIN SLS.Customer AS cu ON cu.Id = ct.CustomerId
LEFT JOIN Gnr.Party AS custParty ON custParty.Id = cu.PartyId
LEFT JOIN Gnr.PartyIndustry AS ind ON ind.Id = cu.IndustryId
LEFT JOIN FIN.DL AS dl ON dl.Id = po.ProjectDlId
LEFT JOIN Sale.Branch AS br ON br.Id = po.BranchId
LEFT JOIN [system].[User] AS se ON se.Id = po.SalesExpertId
LEFT JOIN SLS.Customer AS agency ON agency.Id = po.SalesAgencyId
LEFT JOIN Gnr.Party AS agencyParty ON agencyParty.Id = agency.PartyId
LEFT JOIN Gnr.Region AS city ON city.Id = po.InstallationCityId
WHERE po.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @productionOrderId))), N''''));

SELECT
    i.Id AS ProductionOrderItemId,
    COALESCE(p.Code, i.PartCode) AS Part_Code,
    p.Name AS Part_Name,
    i.Capacity AS Capacity,
    CONVERT(nvarchar(50), i.OutputPressureBar) AS PressureWorking,
    CONVERT(nvarchar(50), i.BarometricPressure) AS BarometricPressure,
    CONVERT(nvarchar(50), i.PurityPercentage) AS PurityPercentage,
    CONVERT(nvarchar(50), i.MaximumTemperature) AS MaximumTemperature,
    CONVERT(nvarchar(50), i.RelativeHumidity) AS RelativeHumidity,
    CONVERT(nvarchar(50), i.Amount) AS Mount,
    dbo.fn_MiladiToShamsi(i.AgreedDeliverDate) AS AgreedDeliverDate,
    CAST(CASE WHEN i.HasInspection = 1 THEN 1 ELSE 0 END AS bit) AS HasInspection
FROM Sale.ProductionOrderItem AS i
LEFT JOIN Inv.Part AS p ON p.Id = i.PartId
WHERE i.ProductionOrderId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @productionOrderId))), N''''))
  AND i.IsDeleted = 0
  AND i.IsLatestVersion = 1
ORDER BY i.Id;';
    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":null,"CustomQuery":null,"Parameters":[{"Id":"productionOrderId","Name":"productionOrderId","DisplayName":"شناسه سفارش ساخت","DefaultValue":"","DataType":"bigint"}],"Selects":[{"Index":0,"Name":"dtMaster","Title":"dtMaster"},{"Index":1,"Name":"dtDetail","Title":"dtDetail"}]}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName":null,"ColumnName":"ProductionOrderId","DisplayName":"شناسه","Alliance":"ProductionOrderId","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ProductionOrder_Number","DisplayName":"شماره سفارش ساخت","Alliance":"ProductionOrder_Number","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"CreatedDate","DisplayName":"تاریخ","Alliance":"CreatedDate","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Customer_Title","DisplayName":"مشتری","Alliance":"Customer_Title","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"CustomerIndustryHeader","DisplayName":"نوع صنعت","Alliance":"CustomerIndustryHeader","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"CustomerIndustry","DisplayName":"ریز صنعت","Alliance":"CustomerIndustry","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"EndUser","DisplayName":"End-User","Alliance":"EndUser","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ProjectDl","DisplayName":"کد تفضیل پروژه","Alliance":"ProjectDl","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ProjectDlTitle","DisplayName":"تفضیل پروژه","Alliance":"ProjectDlTitle","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"SaleDepartment","DisplayName":"واحد فروش","Alliance":"SaleDepartment","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Sale_Personel","DisplayName":"کارشناس فروش","Alliance":"Sale_Personel","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"SalesAgency","DisplayName":"نمایندگی فروش","Alliance":"SalesAgency","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"MetreDate","DisplayName":"تاریخ متره","Alliance":"MetreDate","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"MetreNumber","DisplayName":"عطف به متره برآورد شماره","Alliance":"MetreNumber","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ContractNumber","DisplayName":"قرارداد/پیش فاکتور","Alliance":"ContractNumber","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"RequirementsAdvise_Number","DisplayName":"اعلام نیازمندی","Alliance":"RequirementsAdvise_Number","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"DeliverType","DisplayName":"نحوه تحویل","Alliance":"DeliverType","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ProjectType","DisplayName":"نوع پروژه","Alliance":"ProjectType","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"InstallationType","DisplayName":"نوع/متولی نصب","Alliance":"InstallationType","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"InstallationCity","DisplayName":"محل نصب","Alliance":"InstallationCity","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"IsNeedProjectManager","DisplayName":"نیاز به مدیر پروژه","Alliance":"IsNeedProjectManager","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"IsNeedInspectionBeforePacking","DisplayName":"بازرسی قبل از بسته بندی","Alliance":"IsNeedInspectionBeforePacking","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"CompressorHouseIsReady","DisplayName":"کمپرسورخانه آماده","Alliance":"CompressorHouseIsReady","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"HasGA","DisplayName":"GA","Alliance":"HasGA","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"SalesComment","DisplayName":"توضیحات فروش","Alliance":"SalesComment","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"SetupMethod","DisplayName":"نحوه راه اندازی","Alliance":"SetupMethod","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"CentrifugeHasCompressor","DisplayName":"مشتری از قبل دارد","Alliance":"CentrifugeHasCompressor","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"CentrifugeCount","DisplayName":"تعداد","Alliance":"CentrifugeCount","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ElectromotorVoltage","DisplayName":"ولتاژ الکتروموتور","Alliance":"ElectromotorVoltage","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ProductionOrderItemId","DisplayName":"شناسه قلم","Alliance":"ProductionOrderItemId","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Part_Code","DisplayName":"کد تجهیز","Alliance":"Part_Code","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Part_Name","DisplayName":"عنوان","Alliance":"Part_Name","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Capacity","DisplayName":"ظرفیت","Alliance":"Capacity","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PressureWorking","DisplayName":"فشار","Alliance":"PressureWorking","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"BarometricPressure","DisplayName":"فشار بارومتریک","Alliance":"BarometricPressure","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PurityPercentage","DisplayName":"درصد خلوص","Alliance":"PurityPercentage","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"MaximumTemperature","DisplayName":"دمای بیشینه","Alliance":"MaximumTemperature","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"RelativeHumidity","DisplayName":"رطوبت","Alliance":"RelativeHumidity","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Mount","DisplayName":"تعداد","Alliance":"Mount","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"AgreedDeliverDate","DisplayName":"تاریخ تحویل توافقی","Alliance":"AgreedDeliverDate","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"HasInspection","DisplayName":"بازرسی حین ساخت","Alliance":"HasInspection","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null}]';
    DECLARE @PrintBtn NVARCHAR(MAX) = N'[{"id":"po_print_form","title":"چاپ سفارش ساخت","dataActionName":"printProductionOrder","colorClass":"btn-color-primary","iconClass":"ki-printer ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/System/ReportBuilder/ViewReportByName/ProductionOrderForm?productionOrderId={\\''values\\'':[\\'''' + id + ''\\''],\\''condition\\'':\\''=\\''}&autoRun=True'');\n}"}]';

    DECLARE @SavedQueryId BIGINT;

    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            EntityFullName = NULL,
            Mode = 1,
            Type = 0,
            ActionOptions = N'',
            CustomActionButtonsJson = N'[]',
            EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}',
            DiagramJson = N'[]',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name;
        SELECT @SavedQueryId = Id FROM [system].[SavedQuery] WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
        (
            Name, Title, QueryJson, ColumnsJson, DiagramJson,
            CustomActionButtonsJson, EventScriptsJson,
            Mode, Type, EntityFullName, ActionOptions,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        VALUES
        (
            @Name, @Title, @QueryJson, @ColumnsJson, N'[]',
            N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
            1, 0, NULL, N'',
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @SavedQueryId = SCOPE_IDENTITY();
    END;

    IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @Name)
    BEGIN
        UPDATE dbo.ReportBuilderReport
        SET Title = @Title,
            BaseQuery = @CustomQuery,
            SavedQueryId = @SavedQueryId,
            Type = 1,
            ObjectType = N'SavedQuery',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.ReportBuilderReport
        (
            Name, Title, ObjectType, SavedQueryId, Type, Content, BaseQuery,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
            ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive
        )
        VALUES
        (
            @Name, @Title, N'SavedQuery', @SavedQueryId, 1, NULL, @CustomQuery,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi, 1
        );
    END;

    UPDATE [system].[SavedQuery]
    SET CustomActionButtonsJson = @PrintBtn,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (N'PO_All', N'PO_Related', N'productionorder_listinfo')
      AND EntityFullName = N'entities.app.sale.productionorder';

    COMMIT TRANSACTION;
    PRINT N'ProductionOrderForm SavedQueryId=' + CONVERT(nvarchar(20), @SavedQueryId);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
