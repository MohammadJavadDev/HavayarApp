/*
چاپ برچسب و شناسنامه درخواست تعمیر.
ViewReportByName فقط وقتی گزارش در dbo.ReportBuilderReport باشد صفحه را باز می‌کند؛
نبودن این دو نام همان خطای 404 است.
قالب Stimulsoft در Data/Seed/RprReports است و با Update_RepairRequest_Report_Content.ps1 روی Content می‌نشیند.
اعمال: sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-repair-request-reports';

    DECLARE @Name_RepairRequestLabel NVARCHAR(200) = N'RepairRequestLabel';
    DECLARE @Title_RepairRequestLabel NVARCHAR(200) = N'برچسب تعمیر';
    DECLARE @Base_RepairRequestLabel NVARCHAR(MAX) = N'SELECT
    r.Id,
    r.IdNumber AS IDNumber,
    r.EntryShamsiDate AS EntryDate_Shamsi,
    r.ExitShamsiDate AS ExitDate_Shamsi,
    r.WarehouseDeliveryShamsiDate AS DeliveryDate_Warehouse_Shamsi,
    COALESCE(NULLIF(LTRIM(RTRIM(supParty.FullName)), N''''), sup.NameFa, N'''') AS Zone_SupervisorTitle,
    COALESCE(NULLIF(LTRIM(RTRIM(custParty.FullName)), N''''), custParty.CompanyName, N'''') AS Customer_Title,
    COALESCE(NULLIF(LTRIM(RTRIM(r.CustomerAddressManual)), N''''), ca.Address, N'''') AS CustomerAddressManual,
    r.Serial,
    LTRIM(RTRIM(ISNULL(p.Code, N'''') + CASE WHEN p.Code IS NOT NULL AND p.Name IS NOT NULL THEN N'' '' ELSE N'''' END + ISNULL(p.Name, N''''))) AS Product,
    r.FailureReason,
    CASE WHEN r.IsGuarantee = 1 THEN N''گارانتی'' ELSE N''وارانتی'' END AS GuaranteeTitle,
    CASE WHEN r.AgreedShamsiDate IS NULL OR LTRIM(RTRIM(r.AgreedShamsiDate)) = N'''' THEN N''تعریف نشده'' ELSE r.AgreedShamsiDate END AS AgreedDateInText,
    CASE WHEN r.Agreed2ShamsiDate IS NULL OR LTRIM(RTRIM(r.Agreed2ShamsiDate)) = N'''' THEN N''تعریف نشده'' ELSE r.Agreed2ShamsiDate END AS AgreedDate2InText,
    CASE WHEN r.Agreed3ShamsiDate IS NULL OR LTRIM(RTRIM(r.Agreed3ShamsiDate)) = N'''' THEN N''تعریف نشده'' ELSE r.Agreed3ShamsiDate END AS AgreedDate3InText,
    CASE r.DestinationAfterRepair
        WHEN 293 THEN N''مشتری''
        WHEN 294 THEN N''نمایندگی''
        WHEN 295 THEN N''هوایار (داغی)''
        WHEN 296 THEN N''هوایار (برگشت امانی)''
        WHEN 2700 THEN N''هوایار''
        ELSE N''''
    END AS DestinationAfterRepair,
    CASE r.Status
        WHEN 301 THEN N''1-در حال انجام''
        WHEN 302 THEN N''2-توقف''
        WHEN 304 THEN N''3-غیرقابل تعمیر''
        WHEN 305 THEN N''4-تکمیل شده''
        WHEN 1535 THEN N''5-ارسال به پیمانکار''
        WHEN 1536 THEN N''6-در انتظار تایید مشتری''
        WHEN 1537 THEN N''7-در انتظار خرید قطعه''
        WHEN 1774 THEN N''8-در حال تهیه PreCheck''
        WHEN 1871 THEN N''9-در انتظار تست''
        WHEN 2092 THEN N''10-عدم تایید مشتری''
        WHEN 2093 THEN N''11-تحویل به انبار''
        WHEN 2236 THEN N''12-آیتم صوری''
        WHEN 2413 THEN N''13-در انتظار تعمیر''
        WHEN 3169 THEN N''14-در انتظار بازدید مشتری''
        WHEN 3175 THEN N''15-توقف-تعیین تکلیف مشتری''
        WHEN 3176 THEN N''16-توقف-خرید قطعه''
        ELSE N''''
    END AS RepairRequestStatus
FROM Rpr.RepairRequest AS r
LEFT JOIN Inv.Part AS p ON p.Id = r.ProductId
LEFT JOIN SLS.Customer AS cust ON cust.Id = r.CustomerId
LEFT JOIN Gnr.Party AS custParty ON custParty.Id = cust.PartyId
LEFT JOIN SLS.CustomerAddress AS ca ON ca.Id = r.CustomerAddressId
LEFT JOIN [system].[User] AS sup ON sup.Id = r.ZoneSupervisorId
LEFT JOIN Gnr.Party AS supParty ON supParty.Id = sup.PartyId
WHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''));';
    DECLARE @QueryJson_RepairRequestLabel NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":null,"CustomQuery":"SELECT\n    r.Id,\n    r.IdNumber AS IDNumber,\n    r.EntryShamsiDate AS EntryDate_Shamsi,\n    r.ExitShamsiDate AS ExitDate_Shamsi,\n    r.WarehouseDeliveryShamsiDate AS DeliveryDate_Warehouse_Shamsi,\n    COALESCE(NULLIF(LTRIM(RTRIM(supParty.FullName)), N''''), sup.NameFa, N'''') AS Zone_SupervisorTitle,\n    COALESCE(NULLIF(LTRIM(RTRIM(custParty.FullName)), N''''), custParty.CompanyName, N'''') AS Customer_Title,\n    COALESCE(NULLIF(LTRIM(RTRIM(r.CustomerAddressManual)), N''''), ca.Address, N'''') AS CustomerAddressManual,\n    r.Serial,\n    LTRIM(RTRIM(ISNULL(p.Code, N'''') + CASE WHEN p.Code IS NOT NULL AND p.Name IS NOT NULL THEN N'' '' ELSE N'''' END + ISNULL(p.Name, N''''))) AS Product,\n    r.FailureReason,\n    CASE WHEN r.IsGuarantee = 1 THEN N''گارانتی'' ELSE N''وارانتی'' END AS GuaranteeTitle,\n    CASE WHEN r.AgreedShamsiDate IS NULL OR LTRIM(RTRIM(r.AgreedShamsiDate)) = N'''' THEN N''تعریف نشده'' ELSE r.AgreedShamsiDate END AS AgreedDateInText,\n    CASE WHEN r.Agreed2ShamsiDate IS NULL OR LTRIM(RTRIM(r.Agreed2ShamsiDate)) = N'''' THEN N''تعریف نشده'' ELSE r.Agreed2ShamsiDate END AS AgreedDate2InText,\n    CASE WHEN r.Agreed3ShamsiDate IS NULL OR LTRIM(RTRIM(r.Agreed3ShamsiDate)) = N'''' THEN N''تعریف نشده'' ELSE r.Agreed3ShamsiDate END AS AgreedDate3InText,\n    CASE r.DestinationAfterRepair\n        WHEN 293 THEN N''مشتری''\n        WHEN 294 THEN N''نمایندگی''\n        WHEN 295 THEN N''هوایار (داغی)''\n        WHEN 296 THEN N''هوایار (برگشت امانی)''\n        WHEN 2700 THEN N''هوایار''\n        ELSE N''''\n    END AS DestinationAfterRepair,\n    CASE r.Status\n        WHEN 301 THEN N''1-در حال انجام''\n        WHEN 302 THEN N''2-توقف''\n        WHEN 304 THEN N''3-غیرقابل تعمیر''\n        WHEN 305 THEN N''4-تکمیل شده''\n        WHEN 1535 THEN N''5-ارسال به پیمانکار''\n        WHEN 1536 THEN N''6-در انتظار تایید مشتری''\n        WHEN 1537 THEN N''7-در انتظار خرید قطعه''\n        WHEN 1774 THEN N''8-در حال تهیه PreCheck''\n        WHEN 1871 THEN N''9-در انتظار تست''\n        WHEN 2092 THEN N''10-عدم تایید مشتری''\n        WHEN 2093 THEN N''11-تحویل به انبار''\n        WHEN 2236 THEN N''12-آیتم صوری''\n        WHEN 2413 THEN N''13-در انتظار تعمیر''\n        WHEN 3169 THEN N''14-در انتظار بازدید مشتری''\n        WHEN 3175 THEN N''15-توقف-تعیین تکلیف مشتری''\n        WHEN 3176 THEN N''16-توقف-خرید قطعه''\n        ELSE N''''\n    END AS RepairRequestStatus\nFROM Rpr.RepairRequest AS r\nLEFT JOIN Inv.Part AS p ON p.Id = r.ProductId\nLEFT JOIN SLS.Customer AS cust ON cust.Id = r.CustomerId\nLEFT JOIN Gnr.Party AS custParty ON custParty.Id = cust.PartyId\nLEFT JOIN SLS.CustomerAddress AS ca ON ca.Id = r.CustomerAddressId\nLEFT JOIN [system].[User] AS sup ON sup.Id = r.ZoneSupervisorId\nLEFT JOIN Gnr.Party AS supParty ON supParty.Id = sup.PartyId\nWHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''));","Parameters":[{"Id":"repairRequestId","Name":"repairRequestId","DisplayName":"شناسه درخواست تعمیر","DefaultValue":"","DataType":"bigint"}],"Selects":[{"Index":0,"Name":"Report","Title":"Report"}]}';
    DECLARE @ColumnsJson_RepairRequestLabel NVARCHAR(MAX) = N'[{"TableName":null,"ColumnName":"Id","DisplayName":"شناسه","Alliance":"Id","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"IDNumber","DisplayName":"شماره شناسنامه","Alliance":"IDNumber","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"EntryDate_Shamsi","DisplayName":"تاریخ ورود","Alliance":"EntryDate_Shamsi","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ExitDate_Shamsi","DisplayName":"تاریخ خروج","Alliance":"ExitDate_Shamsi","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"DeliveryDate_Warehouse_Shamsi","DisplayName":"تحویل انبار","Alliance":"DeliveryDate_Warehouse_Shamsi","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Zone_SupervisorTitle","DisplayName":"مسئول منطقه","Alliance":"Zone_SupervisorTitle","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Customer_Title","DisplayName":"مشتری","Alliance":"Customer_Title","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"CustomerAddressManual","DisplayName":"سایت مشتری","Alliance":"CustomerAddressManual","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Serial","DisplayName":"سریال","Alliance":"Serial","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Product","DisplayName":"تجهیز","Alliance":"Product","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"FailureReason","DisplayName":"علت خرابی","Alliance":"FailureReason","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"GuaranteeTitle","DisplayName":"گارانتی / وارانتی","Alliance":"GuaranteeTitle","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"AgreedDateInText","DisplayName":"تاریخ توافقی","Alliance":"AgreedDateInText","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"AgreedDate2InText","DisplayName":"تاریخ توافقی ۲","Alliance":"AgreedDate2InText","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"AgreedDate3InText","DisplayName":"تاریخ توافقی ۳","Alliance":"AgreedDate3InText","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"DestinationAfterRepair","DisplayName":"مقصد پس از تعمیر","Alliance":"DestinationAfterRepair","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"RepairRequestStatus","DisplayName":"وضعیت","Alliance":"RepairRequestStatus","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null}]';
    DECLARE @SavedQueryId_RepairRequestLabel BIGINT;

    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name_RepairRequestLabel)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title_RepairRequestLabel,
            QueryJson = @QueryJson_RepairRequestLabel,
            ColumnsJson = @ColumnsJson_RepairRequestLabel,
            EntityFullName = N'entities.app.rpr.repairrequest',
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
        WHERE Name = @Name_RepairRequestLabel;
        SELECT @SavedQueryId_RepairRequestLabel = Id FROM [system].[SavedQuery] WHERE Name = @Name_RepairRequestLabel;
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
            @Name_RepairRequestLabel, @Title_RepairRequestLabel, @QueryJson_RepairRequestLabel, @ColumnsJson_RepairRequestLabel, N'[]',
            N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
            1, 0, N'entities.app.rpr.repairrequest', N'',
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @SavedQueryId_RepairRequestLabel = SCOPE_IDENTITY();
    END;

    IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @Name_RepairRequestLabel)
    BEGIN
        UPDATE dbo.ReportBuilderReport
        SET Title = @Title_RepairRequestLabel,
            BaseQuery = @Base_RepairRequestLabel,
            SavedQueryId = @SavedQueryId_RepairRequestLabel,
            Type = 1,
            ObjectType = N'Table',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name_RepairRequestLabel;
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
            @Name_RepairRequestLabel, @Title_RepairRequestLabel, N'Table', @SavedQueryId_RepairRequestLabel, 1, NULL,
            @Base_RepairRequestLabel,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi, 1
        );
    END;

    DECLARE @Name_RepairRequestID NVARCHAR(200) = N'RepairRequestID';
    DECLARE @Title_RepairRequestID NVARCHAR(200) = N'شناسنامه درخواست تعمیر';
    DECLARE @Base_RepairRequestID NVARCHAR(MAX) = N'SELECT
    r.Id,
    r.IdNumber AS IDNumber,
    r.WarehouseDeliveryShamsiDate AS DeliveryDate_Warehouse_Shamsi,
    r.EntryShamsiDate AS EntryDate_Shamsi,
    p.Name AS Part_Name,
    p.Code AS Part_Code,
    r.Serial,
    r.ExitShamsiDate AS ExitDate_Shamsi,
    z.Title AS Agency,
    STUFF((
        SELECT N'' ، '' + c.ContractorTitle
        FROM Rpr.RepairRequestContractor AS c
        WHERE c.RepairRequestId = r.Id AND NULLIF(LTRIM(RTRIM(c.ContractorTitle)), N'''') IS NOT NULL
        FOR XML PATH(''''), TYPE
    ).value(''.'', ''nvarchar(max)''), 1, 3, N'''') AS VendorName,
    COALESCE(NULLIF(LTRIM(RTRIM(custParty.FullName)), N''''), custParty.CompanyName, N'''') AS Customer_Title,
    COALESCE(NULLIF(LTRIM(RTRIM(r.DestinationAddress)), N''''), NULLIF(LTRIM(RTRIM(r.CustomerAddressManual)), N''''), ca.Address, N'''') AS DestinationAddress,
    r.CustomerPhone AS Customer_Phone,
    r.IsGuarantee,
    CAST(CASE WHEN r.IsGuarantee = 0 THEN 1 ELSE 0 END AS bit) AS ISWarranty,
    CASE r.Status
        WHEN 301 THEN N''1-در حال انجام''
        WHEN 302 THEN N''2-توقف''
        WHEN 304 THEN N''3-غیرقابل تعمیر''
        WHEN 305 THEN N''4-تکمیل شده''
        WHEN 1535 THEN N''5-ارسال به پیمانکار''
        WHEN 1536 THEN N''6-در انتظار تایید مشتری''
        WHEN 1537 THEN N''7-در انتظار خرید قطعه''
        WHEN 1774 THEN N''8-در حال تهیه PreCheck''
        WHEN 1871 THEN N''9-در انتظار تست''
        WHEN 2092 THEN N''10-عدم تایید مشتری''
        WHEN 2093 THEN N''11-تحویل به انبار''
        WHEN 2236 THEN N''12-آیتم صوری''
        WHEN 2413 THEN N''13-در انتظار تعمیر''
        WHEN 3169 THEN N''14-در انتظار بازدید مشتری''
        WHEN 3175 THEN N''15-توقف-تعیین تکلیف مشتری''
        WHEN 3176 THEN N''16-توقف-خرید قطعه''
        ELSE N''''
    END AS RepairRequestStatus,
    CASE r.DestinationAfterRepair
        WHEN 293 THEN N''مشتری''
        WHEN 294 THEN N''نمایندگی''
        WHEN 295 THEN N''هوایار (داغی)''
        WHEN 296 THEN N''هوایار (برگشت امانی)''
        WHEN 2700 THEN N''هوایار''
        ELSE N''''
    END AS DestinationAfterRepair,
    r.FailureReason,
    r.CustomerNeedShamsiDate AS CustomerNeedDate_Shamsi,
    r.RepresentationReception,
    r.Comment AS Operations,
    r.DeliveringRecipient,
    LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(r.RepairDescription, N''''), N''*'', N'' ''), N''  '', N'' ''), N''  '', N'' ''))) AS RepairDescription,
    r.LoanFormNumber AS LoanForm_Number,
    r.DeliveryFormNumber AS DeliveryForm_Number,
    r.PreFactorNumber AS PreFactor_Number,
    ISNULL(pt.TotalBuy, 0) AS TotalBuy,
    ISNULL(pt.TotalSale, 0) AS TotalSale,
    ISNULL(pt.Total, 0) AS Total,
    ISNULL((
        SELECT SUM(c.Cost)
        FROM Rpr.RepairRequestContractor AS c
        WHERE c.RepairRequestId = r.Id
    ), 0) AS VendoringCost
FROM Rpr.RepairRequest AS r
LEFT JOIN Inv.Part AS p ON p.Id = r.ProductId
LEFT JOIN SLS.Customer AS cust ON cust.Id = r.CustomerId
LEFT JOIN Gnr.Party AS custParty ON custParty.Id = cust.PartyId
LEFT JOIN SLS.CustomerAddress AS ca ON ca.Id = r.CustomerAddressId
LEFT JOIN Crm.Zone AS z ON z.Id = r.ZoneId
OUTER APPLY (
    SELECT SUM(g.BuyPrice) AS TotalBuy, SUM(g.SalePrice) AS TotalSale, SUM(g.TotalPrice) AS Total
    FROM (
        SELECT
            MAX(x.BuyPrice) AS BuyPrice,
            x.SalePrice AS SalePrice,
            CAST(ISNULL(x.SalePrice, 0) * (SUM(ISNULL(x.UsedMount, 0)) - SUM(ISNULL(x.ReturnMount, 0))) AS decimal(18, 2)) AS TotalPrice
        FROM Rpr.RepairRequestPart AS x
        INNER JOIN Inv.Part AS xp ON xp.Id = x.PartId
        LEFT JOIN Inv.PartUnit AS xpu ON xpu.Id = COALESCE(x.PartUnitId, xp.UnitId)
        WHERE x.RepairRequestId = r.Id
        GROUP BY xp.Code, xp.Name, xp.Number, ISNULL(NULLIF(LTRIM(RTRIM(x.UnitName)), N''''), xpu.Title), x.RepairRequestId, x.DamagedPart, x.SalePrice
        HAVING (SUM(ISNULL(x.UsedMount, 0)) - SUM(ISNULL(x.ReturnMount, 0))) <> 0
    ) AS g
) AS pt
WHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''));

SELECT
    MAX(rp.Id) AS Id,
    p.Code AS Part_Code,
    p.Name AS Part_Name,
    p.Number AS Part_Number,
    ISNULL(NULLIF(LTRIM(RTRIM(rp.UnitName)), N''''), pu.Title) AS PartUnit_Title,
    CAST(MAX(CASE WHEN rp.DamagedPart = 1 THEN 1 ELSE 0 END) AS bit) AS DamagedPart,
    MAX(NULLIF(rp.HtsHamkaranInvVchItemId, 0)) AS Hamkaran_InvVchItm_FK,
    MAX(rp.BuyPrice) AS BuyPrice,
    rp.RepairRequestId,
    CAST(SUM(ISNULL(rp.UsedMount, 0)) - SUM(ISNULL(rp.ReturnMount, 0)) AS decimal(18, 3)) AS Tafazol,
    rp.SalePrice AS SalePrice,
    CAST(ISNULL(rp.SalePrice, 0) * (SUM(ISNULL(rp.UsedMount, 0)) - SUM(ISNULL(rp.ReturnMount, 0))) AS decimal(18, 2)) AS TotalPrice
FROM Rpr.RepairRequestPart AS rp
INNER JOIN Inv.Part AS p ON p.Id = rp.PartId
LEFT JOIN Inv.PartUnit AS pu ON pu.Id = COALESCE(rp.PartUnitId, p.UnitId)
WHERE rp.RepairRequestId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''))
GROUP BY p.Code, p.Name, p.Number, ISNULL(NULLIF(LTRIM(RTRIM(rp.UnitName)), N''''), pu.Title), rp.RepairRequestId, rp.DamagedPart, rp.SalePrice
HAVING (SUM(ISNULL(rp.UsedMount, 0)) - SUM(ISNULL(rp.ReturnMount, 0))) <> 0;

SELECT
    d.Id,
    d.ExpertId,
    d.PrsName,
    d.PrsFamily,
    d.WorkDate,
    d.StartTime,
    d.EndTime,
    d.ManHourPrice,
    d.WorkingHour,
    d.PriceManInHour,
    d.Comment,
    CAST(d.PersonMinutes / 60 AS nvarchar(10)) + N'':'' + RIGHT(N''0'' + CAST(d.PersonMinutes % 60 AS nvarchar(10)), 2) AS PersonHours,
    d.PersonPrice,
    ROW_NUMBER() OVER (ORDER BY d.PrsFamily, d.PrsName, d.ExpertId, d.Id) AS RowNo,
    RIGHT(N''0000000'' + CAST(DENSE_RANK() OVER (ORDER BY d.PrsFamily, d.PrsName, d.ExpertId) AS nvarchar(7)), 7) AS GroupKey
FROM (
    SELECT
        mh.Id,
        mh.ExpertId,
        ISNULL(prs.Name, N'''') AS PrsName,
        ISNULL(prs.Family, N'''') AS PrsFamily,
        mh.WorkShamsiDate AS WorkDate,
        mh.StartTime,
        mh.EndTime,
        CAST(ISNULL(sal.DailySalary, 0) / 8 AS bigint) AS ManHourPrice,
        CASE
            WHEN m.Minutes IS NULL THEN N''''
            ELSE RIGHT(N''0'' + CAST(m.Minutes / 60 AS nvarchar(10)), 2) + N'':'' + RIGHT(N''0'' + CAST(m.Minutes % 60 AS nvarchar(10)), 2)
        END AS WorkingHour,
        CAST((ISNULL(sal.DailySalary, 0) / 8) * (ISNULL(m.Minutes, 0) / 60) AS bigint) AS PriceManInHour,
        mh.Comment,
        SUM(ISNULL(m.Minutes, 0)) OVER (PARTITION BY mh.ExpertId) AS PersonMinutes,
        SUM(CAST((ISNULL(sal.DailySalary, 0) / 8) * (ISNULL(m.Minutes, 0) / 60) AS bigint)) OVER (PARTITION BY mh.ExpertId) AS PersonPrice
    FROM Rpr.RepairRequestManHour AS mh
    LEFT JOIN Hcm.Personel AS prs ON prs.Id = mh.ExpertId
    OUTER APPLY (
        SELECT TOP 1 ms.DailySalary
        FROM Sale.MissionSalary AS ms
        WHERE ms.PersonelId = mh.ExpertId
        ORDER BY ms.Id DESC
    ) AS sal
    OUTER APPLY (
        SELECT CASE
            WHEN TRY_CONVERT(time, mh.StartTime) IS NULL OR TRY_CONVERT(time, mh.EndTime) IS NULL THEN NULL
            ELSE DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime))
        END AS Minutes
    ) AS m
    WHERE mh.RepairRequestId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''))
) AS d
ORDER BY d.PrsFamily, d.PrsName, d.ExpertId, d.Id;

SELECT
    ISNULL(
        CAST(SUM(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime))) / 60 AS nvarchar(10))
        + N'':'' +
        RIGHT(N''0'' + CAST(SUM(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime))) % 60 AS nvarchar(10)), 2),
        N''0:00'') AS SumWorkingHour,
    CAST(ISNULL(SUM(
        (ISNULL(sal.DailySalary, 0) / 8) * (
            CASE
                WHEN TRY_CONVERT(time, mh.StartTime) IS NULL OR TRY_CONVERT(time, mh.EndTime) IS NULL THEN 0
                ELSE DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60
            END
        )
    ), 0) AS bigint) AS SumOfMissionPrice
FROM Rpr.RepairRequestManHour AS mh
OUTER APPLY (
    SELECT TOP 1 ms.DailySalary
    FROM Sale.MissionSalary AS ms
    WHERE ms.PersonelId = mh.ExpertId
    ORDER BY ms.Id DESC
) AS sal
WHERE mh.RepairRequestId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''));

SELECT
    r.Id,
    CAST(ISNULL(SUM(
        (ISNULL(sal.DailySalary, 0) / 8) * (
            CASE
                WHEN TRY_CONVERT(time, mh.StartTime) IS NULL OR TRY_CONVERT(time, mh.EndTime) IS NULL THEN 0
                ELSE DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60
            END
        )
    ), 0) AS bigint) AS PriceManInHour
FROM Rpr.RepairRequest AS r
LEFT JOIN Rpr.RepairRequestManHour AS mh ON mh.RepairRequestId = r.Id
OUTER APPLY (
    SELECT TOP 1 ms.DailySalary
    FROM Sale.MissionSalary AS ms
    WHERE ms.PersonelId = mh.ExpertId
    ORDER BY ms.Id DESC
) AS sal
WHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''))
GROUP BY r.Id;';
    DECLARE @QueryJson_RepairRequestID NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":null,"CustomQuery":"SELECT\n    r.Id,\n    r.IdNumber AS IDNumber,\n    r.WarehouseDeliveryShamsiDate AS DeliveryDate_Warehouse_Shamsi,\n    r.EntryShamsiDate AS EntryDate_Shamsi,\n    p.Name AS Part_Name,\n    p.Code AS Part_Code,\n    r.Serial,\n    r.ExitShamsiDate AS ExitDate_Shamsi,\n    z.Title AS Agency,\n    STUFF((\n        SELECT N'' ، '' + c.ContractorTitle\n        FROM Rpr.RepairRequestContractor AS c\n        WHERE c.RepairRequestId = r.Id AND NULLIF(LTRIM(RTRIM(c.ContractorTitle)), N'''') IS NOT NULL\n        FOR XML PATH(''''), TYPE\n    ).value(''.'', ''nvarchar(max)''), 1, 3, N'''') AS VendorName,\n    COALESCE(NULLIF(LTRIM(RTRIM(custParty.FullName)), N''''), custParty.CompanyName, N'''') AS Customer_Title,\n    COALESCE(NULLIF(LTRIM(RTRIM(r.DestinationAddress)), N''''), NULLIF(LTRIM(RTRIM(r.CustomerAddressManual)), N''''), ca.Address, N'''') AS DestinationAddress,\n    r.CustomerPhone AS Customer_Phone,\n    r.IsGuarantee,\n    CAST(CASE WHEN r.IsGuarantee = 0 THEN 1 ELSE 0 END AS bit) AS ISWarranty,\n    CASE r.Status\n        WHEN 301 THEN N''1-در حال انجام''\n        WHEN 302 THEN N''2-توقف''\n        WHEN 304 THEN N''3-غیرقابل تعمیر''\n        WHEN 305 THEN N''4-تکمیل شده''\n        WHEN 1535 THEN N''5-ارسال به پیمانکار''\n        WHEN 1536 THEN N''6-در انتظار تایید مشتری''\n        WHEN 1537 THEN N''7-در انتظار خرید قطعه''\n        WHEN 1774 THEN N''8-در حال تهیه PreCheck''\n        WHEN 1871 THEN N''9-در انتظار تست''\n        WHEN 2092 THEN N''10-عدم تایید مشتری''\n        WHEN 2093 THEN N''11-تحویل به انبار''\n        WHEN 2236 THEN N''12-آیتم صوری''\n        WHEN 2413 THEN N''13-در انتظار تعمیر''\n        WHEN 3169 THEN N''14-در انتظار بازدید مشتری''\n        WHEN 3175 THEN N''15-توقف-تعیین تکلیف مشتری''\n        WHEN 3176 THEN N''16-توقف-خرید قطعه''\n        ELSE N''''\n    END AS RepairRequestStatus,\n    CASE r.DestinationAfterRepair\n        WHEN 293 THEN N''مشتری''\n        WHEN 294 THEN N''نمایندگی''\n        WHEN 295 THEN N''هوایار (داغی)''\n        WHEN 296 THEN N''هوایار (برگشت امانی)''\n        WHEN 2700 THEN N''هوایار''\n        ELSE N''''\n    END AS DestinationAfterRepair,\n    r.FailureReason,\n    r.CustomerNeedShamsiDate AS CustomerNeedDate_Shamsi,\n    r.RepresentationReception,\n    r.Comment AS Operations,\n    r.DeliveringRecipient,\n    LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(r.RepairDescription, N''''), N''*'', N'' ''), N''  '', N'' ''), N''  '', N'' ''))) AS RepairDescription,\n    r.LoanFormNumber AS LoanForm_Number,\n    r.DeliveryFormNumber AS DeliveryForm_Number,\n    r.PreFactorNumber AS PreFactor_Number,\n    ISNULL(pt.TotalBuy, 0) AS TotalBuy,\n    ISNULL(pt.TotalSale, 0) AS TotalSale,\n    ISNULL(pt.Total, 0) AS Total,\n    ISNULL((\n        SELECT SUM(c.Cost)\n        FROM Rpr.RepairRequestContractor AS c\n        WHERE c.RepairRequestId = r.Id\n    ), 0) AS VendoringCost\nFROM Rpr.RepairRequest AS r\nLEFT JOIN Inv.Part AS p ON p.Id = r.ProductId\nLEFT JOIN SLS.Customer AS cust ON cust.Id = r.CustomerId\nLEFT JOIN Gnr.Party AS custParty ON custParty.Id = cust.PartyId\nLEFT JOIN SLS.CustomerAddress AS ca ON ca.Id = r.CustomerAddressId\nLEFT JOIN Crm.Zone AS z ON z.Id = r.ZoneId\nOUTER APPLY (\n    SELECT SUM(g.BuyPrice) AS TotalBuy, SUM(g.SalePrice) AS TotalSale, SUM(g.TotalPrice) AS Total\n    FROM (\n        SELECT\n            MAX(x.BuyPrice) AS BuyPrice,\n            x.SalePrice AS SalePrice,\n            CAST(ISNULL(x.SalePrice, 0) * (SUM(ISNULL(x.UsedMount, 0)) - SUM(ISNULL(x.ReturnMount, 0))) AS decimal(18, 2)) AS TotalPrice\n        FROM Rpr.RepairRequestPart AS x\n        INNER JOIN Inv.Part AS xp ON xp.Id = x.PartId\n        LEFT JOIN Inv.PartUnit AS xpu ON xpu.Id = COALESCE(x.PartUnitId, xp.UnitId)\n        WHERE x.RepairRequestId = r.Id\n        GROUP BY xp.Code, xp.Name, xp.Number, ISNULL(NULLIF(LTRIM(RTRIM(x.UnitName)), N''''), xpu.Title), x.RepairRequestId, x.DamagedPart, x.SalePrice\n        HAVING (SUM(ISNULL(x.UsedMount, 0)) - SUM(ISNULL(x.ReturnMount, 0))) <> 0\n    ) AS g\n) AS pt\nWHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''));\n\nSELECT\n    MAX(rp.Id) AS Id,\n    p.Code AS Part_Code,\n    p.Name AS Part_Name,\n    p.Number AS Part_Number,\n    ISNULL(NULLIF(LTRIM(RTRIM(rp.UnitName)), N''''), pu.Title) AS PartUnit_Title,\n    CAST(MAX(CASE WHEN rp.DamagedPart = 1 THEN 1 ELSE 0 END) AS bit) AS DamagedPart,\n    MAX(NULLIF(rp.HtsHamkaranInvVchItemId, 0)) AS Hamkaran_InvVchItm_FK,\n    MAX(rp.BuyPrice) AS BuyPrice,\n    rp.RepairRequestId,\n    CAST(SUM(ISNULL(rp.UsedMount, 0)) - SUM(ISNULL(rp.ReturnMount, 0)) AS decimal(18, 3)) AS Tafazol,\n    rp.SalePrice AS SalePrice,\n    CAST(ISNULL(rp.SalePrice, 0) * (SUM(ISNULL(rp.UsedMount, 0)) - SUM(ISNULL(rp.ReturnMount, 0))) AS decimal(18, 2)) AS TotalPrice\nFROM Rpr.RepairRequestPart AS rp\nINNER JOIN Inv.Part AS p ON p.Id = rp.PartId\nLEFT JOIN Inv.PartUnit AS pu ON pu.Id = COALESCE(rp.PartUnitId, p.UnitId)\nWHERE rp.RepairRequestId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''))\nGROUP BY p.Code, p.Name, p.Number, ISNULL(NULLIF(LTRIM(RTRIM(rp.UnitName)), N''''), pu.Title), rp.RepairRequestId, rp.DamagedPart, rp.SalePrice\nHAVING (SUM(ISNULL(rp.UsedMount, 0)) - SUM(ISNULL(rp.ReturnMount, 0))) <> 0;\n\nSELECT\n    d.Id,\n    d.ExpertId,\n    d.PrsName,\n    d.PrsFamily,\n    d.WorkDate,\n    d.StartTime,\n    d.EndTime,\n    d.ManHourPrice,\n    d.WorkingHour,\n    d.PriceManInHour,\n    d.Comment,\n    CAST(d.PersonMinutes / 60 AS nvarchar(10)) + N'':'' + RIGHT(N''0'' + CAST(d.PersonMinutes % 60 AS nvarchar(10)), 2) AS PersonHours,\n    d.PersonPrice,\n    ROW_NUMBER() OVER (ORDER BY d.PrsFamily, d.PrsName, d.ExpertId, d.Id) AS RowNo,\n    RIGHT(N''0000000'' + CAST(DENSE_RANK() OVER (ORDER BY d.PrsFamily, d.PrsName, d.ExpertId) AS nvarchar(7)), 7) AS GroupKey\nFROM (\n    SELECT\n        mh.Id,\n        mh.ExpertId,\n        ISNULL(prs.Name, N'''') AS PrsName,\n        ISNULL(prs.Family, N'''') AS PrsFamily,\n        mh.WorkShamsiDate AS WorkDate,\n        mh.StartTime,\n        mh.EndTime,\n        CAST(ISNULL(sal.DailySalary, 0) / 8 AS bigint) AS ManHourPrice,\n        CASE\n            WHEN m.Minutes IS NULL THEN N''''\n            ELSE RIGHT(N''0'' + CAST(m.Minutes / 60 AS nvarchar(10)), 2) + N'':'' + RIGHT(N''0'' + CAST(m.Minutes % 60 AS nvarchar(10)), 2)\n        END AS WorkingHour,\n        CAST((ISNULL(sal.DailySalary, 0) / 8) * (ISNULL(m.Minutes, 0) / 60) AS bigint) AS PriceManInHour,\n        mh.Comment,\n        SUM(ISNULL(m.Minutes, 0)) OVER (PARTITION BY mh.ExpertId) AS PersonMinutes,\n        SUM(CAST((ISNULL(sal.DailySalary, 0) / 8) * (ISNULL(m.Minutes, 0) / 60) AS bigint)) OVER (PARTITION BY mh.ExpertId) AS PersonPrice\n    FROM Rpr.RepairRequestManHour AS mh\n    LEFT JOIN Hcm.Personel AS prs ON prs.Id = mh.ExpertId\n    OUTER APPLY (\n        SELECT TOP 1 ms.DailySalary\n        FROM Sale.MissionSalary AS ms\n        WHERE ms.PersonelId = mh.ExpertId\n        ORDER BY ms.Id DESC\n    ) AS sal\n    OUTER APPLY (\n        SELECT CASE\n            WHEN TRY_CONVERT(time, mh.StartTime) IS NULL OR TRY_CONVERT(time, mh.EndTime) IS NULL THEN NULL\n            ELSE DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime))\n        END AS Minutes\n    ) AS m\n    WHERE mh.RepairRequestId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''))\n) AS d\nORDER BY d.PrsFamily, d.PrsName, d.ExpertId, d.Id;\n\nSELECT\n    ISNULL(\n        CAST(SUM(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime))) / 60 AS nvarchar(10))\n        + N'':'' +\n        RIGHT(N''0'' + CAST(SUM(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime))) % 60 AS nvarchar(10)), 2),\n        N''0:00'') AS SumWorkingHour,\n    CAST(ISNULL(SUM(\n        (ISNULL(sal.DailySalary, 0) / 8) * (\n            CASE\n                WHEN TRY_CONVERT(time, mh.StartTime) IS NULL OR TRY_CONVERT(time, mh.EndTime) IS NULL THEN 0\n                ELSE DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60\n            END\n        )\n    ), 0) AS bigint) AS SumOfMissionPrice\nFROM Rpr.RepairRequestManHour AS mh\nOUTER APPLY (\n    SELECT TOP 1 ms.DailySalary\n    FROM Sale.MissionSalary AS ms\n    WHERE ms.PersonelId = mh.ExpertId\n    ORDER BY ms.Id DESC\n) AS sal\nWHERE mh.RepairRequestId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''));\n\nSELECT\n    r.Id,\n    CAST(ISNULL(SUM(\n        (ISNULL(sal.DailySalary, 0) / 8) * (\n            CASE\n                WHEN TRY_CONVERT(time, mh.StartTime) IS NULL OR TRY_CONVERT(time, mh.EndTime) IS NULL THEN 0\n                ELSE DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60\n            END\n        )\n    ), 0) AS bigint) AS PriceManInHour\nFROM Rpr.RepairRequest AS r\nLEFT JOIN Rpr.RepairRequestManHour AS mh ON mh.RepairRequestId = r.Id\nOUTER APPLY (\n    SELECT TOP 1 ms.DailySalary\n    FROM Sale.MissionSalary AS ms\n    WHERE ms.PersonelId = mh.ExpertId\n    ORDER BY ms.Id DESC\n) AS sal\nWHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''''))\nGROUP BY r.Id;","Parameters":[{"Id":"repairRequestId","Name":"repairRequestId","DisplayName":"شناسه درخواست تعمیر","DefaultValue":"","DataType":"bigint"}],"Selects":[{"Index":0,"Name":"RepairRequest","Title":"RepairRequest"},{"Index":1,"Name":"RepairRequestPart","Title":"RepairRequestPart"},{"Index":2,"Name":"RepairRequestManHour","Title":"RepairRequestManHour"},{"Index":3,"Name":"RepairRequestManHourMaster","Title":"RepairRequestManHourMaster"},{"Index":4,"Name":"RepairRequestTotalManHourPrice","Title":"RepairRequestTotalManHourPrice"}]}';
    DECLARE @ColumnsJson_RepairRequestID NVARCHAR(MAX) = N'[{"TableName":null,"ColumnName":"Id","DisplayName":"شناسه","Alliance":"Id","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"IDNumber","DisplayName":"شماره شناسنامه","Alliance":"IDNumber","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"DeliveryDate_Warehouse_Shamsi","DisplayName":"تحویل انبار","Alliance":"DeliveryDate_Warehouse_Shamsi","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"EntryDate_Shamsi","DisplayName":"تاریخ ورود","Alliance":"EntryDate_Shamsi","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Part_Name","DisplayName":"نام کالا","Alliance":"Part_Name","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Part_Code","DisplayName":"کد کالا","Alliance":"Part_Code","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Serial","DisplayName":"سریال","Alliance":"Serial","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ExitDate_Shamsi","DisplayName":"تاریخ ارسال","Alliance":"ExitDate_Shamsi","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Agency","DisplayName":"منطقه","Alliance":"Agency","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"VendorName","DisplayName":"پیمانکار","Alliance":"VendorName","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Customer_Title","DisplayName":"مشتری","Alliance":"Customer_Title","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"DestinationAddress","DisplayName":"آدرس مقصد","Alliance":"DestinationAddress","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Customer_Phone","DisplayName":"تلفن","Alliance":"Customer_Phone","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"IsGuarantee","DisplayName":"گارانتی","Alliance":"IsGuarantee","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ISWarranty","DisplayName":"وارانتی","Alliance":"ISWarranty","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"RepairRequestStatus","DisplayName":"وضعیت","Alliance":"RepairRequestStatus","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"DestinationAfterRepair","DisplayName":"مقصد","Alliance":"DestinationAfterRepair","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"FailureReason","DisplayName":"علت خرابی","Alliance":"FailureReason","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"CustomerNeedDate_Shamsi","DisplayName":"تاریخ نیاز مشتری","Alliance":"CustomerNeedDate_Shamsi","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"RepresentationReception","DisplayName":"دریافت از","Alliance":"RepresentationReception","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Operations","DisplayName":"عملیات","Alliance":"Operations","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"DeliveringRecipient","DisplayName":"تحویل گیرنده","Alliance":"DeliveringRecipient","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"RepairDescription","DisplayName":"شرح قطعه","Alliance":"RepairDescription","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"LoanForm_Number","DisplayName":"شماره فرم امانی","Alliance":"LoanForm_Number","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"DeliveryForm_Number","DisplayName":"شماره فرم تحویل","Alliance":"DeliveryForm_Number","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PreFactor_Number","DisplayName":"شماره پیش فاکتور","Alliance":"PreFactor_Number","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"TotalBuy","DisplayName":"جمع خرید","Alliance":"TotalBuy","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"TotalSale","DisplayName":"جمع فروش","Alliance":"TotalSale","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Total","DisplayName":"جمع","Alliance":"Total","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"VendoringCost","DisplayName":"هزینه پیمانکار","Alliance":"VendoringCost","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Id","DisplayName":"شناسه قطعه","Alliance":"Id","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Part_Code","DisplayName":"کد کالا","Alliance":"Part_Code","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Part_Name","DisplayName":"نام کالا","Alliance":"Part_Name","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Part_Number","DisplayName":"شماره کالا","Alliance":"Part_Number","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PartUnit_Title","DisplayName":"واحد","Alliance":"PartUnit_Title","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"DamagedPart","DisplayName":"داغی","Alliance":"DamagedPart","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Hamkaran_InvVchItm_FK","DisplayName":"شماره درخواست","Alliance":"Hamkaran_InvVchItm_FK","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"BuyPrice","DisplayName":"فی خرید","Alliance":"BuyPrice","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"RepairRequestId","DisplayName":"شناسه درخواست","Alliance":"RepairRequestId","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Tafazol","DisplayName":"تعداد","Alliance":"Tafazol","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"SalePrice","DisplayName":"فی فروش","Alliance":"SalePrice","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"TotalPrice","DisplayName":"مبلغ","Alliance":"TotalPrice","Address":null,"SystemTypeName":null,"SelectIndex":1,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Id","DisplayName":"شناسه","Alliance":"Id","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ExpertId","DisplayName":"شناسه تعمیرکار","Alliance":"ExpertId","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PrsName","DisplayName":"نام","Alliance":"PrsName","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PrsFamily","DisplayName":"نام خانوادگی","Alliance":"PrsFamily","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"WorkDate","DisplayName":"تاریخ کار","Alliance":"WorkDate","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"StartTime","DisplayName":"شروع","Alliance":"StartTime","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"EndTime","DisplayName":"پایان","Alliance":"EndTime","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"ManHourPrice","DisplayName":"نرخ","Alliance":"ManHourPrice","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"WorkingHour","DisplayName":"کارکرد","Alliance":"WorkingHour","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PriceManInHour","DisplayName":"هزینه","Alliance":"PriceManInHour","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Comment","DisplayName":"توضیحات","Alliance":"Comment","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PersonHours","DisplayName":"جمع کارکرد تعمیرکار","Alliance":"PersonHours","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PersonPrice","DisplayName":"جمع هزینه تعمیرکار","Alliance":"PersonPrice","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"RowNo","DisplayName":"ردیف","Alliance":"RowNo","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"GroupKey","DisplayName":"کلید گروه","Alliance":"GroupKey","Address":null,"SystemTypeName":null,"SelectIndex":2,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"SumWorkingHour","DisplayName":"جمع نفرساعت","Alliance":"SumWorkingHour","Address":null,"SystemTypeName":null,"SelectIndex":3,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"SumOfMissionPrice","DisplayName":"جمع هزینه نفرساعت","Alliance":"SumOfMissionPrice","Address":null,"SystemTypeName":null,"SelectIndex":3,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"Id","DisplayName":"شناسه","Alliance":"Id","Address":null,"SystemTypeName":null,"SelectIndex":4,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},{"TableName":null,"ColumnName":"PriceManInHour","DisplayName":"هزینه نفرساعت","Alliance":"PriceManInHour","Address":null,"SystemTypeName":null,"SelectIndex":4,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null}]';
    DECLARE @SavedQueryId_RepairRequestID BIGINT;

    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name_RepairRequestID)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title_RepairRequestID,
            QueryJson = @QueryJson_RepairRequestID,
            ColumnsJson = @ColumnsJson_RepairRequestID,
            EntityFullName = N'entities.app.rpr.repairrequest',
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
        WHERE Name = @Name_RepairRequestID;
        SELECT @SavedQueryId_RepairRequestID = Id FROM [system].[SavedQuery] WHERE Name = @Name_RepairRequestID;
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
            @Name_RepairRequestID, @Title_RepairRequestID, @QueryJson_RepairRequestID, @ColumnsJson_RepairRequestID, N'[]',
            N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
            1, 0, N'entities.app.rpr.repairrequest', N'',
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @SavedQueryId_RepairRequestID = SCOPE_IDENTITY();
    END;

    IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @Name_RepairRequestID)
    BEGIN
        UPDATE dbo.ReportBuilderReport
        SET Title = @Title_RepairRequestID,
            BaseQuery = @Base_RepairRequestID,
            SavedQueryId = @SavedQueryId_RepairRequestID,
            Type = 1,
            ObjectType = N'Table',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name_RepairRequestID;
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
            @Name_RepairRequestID, @Title_RepairRequestID, N'Table', @SavedQueryId_RepairRequestID, 1, NULL,
            @Base_RepairRequestID,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi, 1
        );
    END;

SELECT
    (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'RepairRequestLabel') AS LabelSavedQueryId,
    (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'RepairRequestID') AS IdSavedQueryId,
    (SELECT Title FROM dbo.ReportBuilderReport WHERE Name = N'RepairRequestLabel') AS LabelTitle,
    (SELECT Title FROM dbo.ReportBuilderReport WHERE Name = N'RepairRequestID') AS IdTitle;
