/*
قالب چاپ خلاصه و اقدامات شکایات مشتری.
اعمال: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_CrmCustomerComplaint_Reports.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-crm-complaint';
    
    DECLARE @SumName NVARCHAR(200) = N'CustomerComplaintSummary';
    DECLARE @SumTitle NVARCHAR(200) = N'خلاصه شکایات مشتری';
    DECLARE @SumCustomQuery NVARCHAR(MAX) = N'SELECT N''1'' AS SortKey, N''تعداد شکایات'' AS Section, N''کل'' AS Title, N'''' AS Detail,
       COUNT(*) AS Number, CAST(100 AS decimal(10,2)) AS Percentage
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691
  AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
UNION ALL
SELECT N''2'', N''تعداد شکایات'', CASE c.CustomerType WHEN 107 THEN N''حقوقی'' WHEN 106 THEN N''حقیقی'' ELSE N''نامشخص'' END, N'''',
       COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.CustomerType
UNION ALL
SELECT N''3'', N''واحدهای درگیر'', ISNULL(NULLIF(c.OrganizationUnitInText, N''''), N''نامشخص''), N'''',
       COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.OrganizationUnitInText
UNION ALL
SELECT N''4'', N''موضوع شکایت'', ISNULL(s.Title, N''نامشخص''), ISNULL(p.Code, N''''),
       COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
LEFT JOIN Crm.ComplaintLookupItem s ON s.Id = c.ComplaintSubjectId
LEFT JOIN Inv.Part p ON p.Id = c.PartId
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY s.Title, p.Code
UNION ALL
SELECT N''5'', N''وضعیت شکایت'',
       CASE c.ComplaintStatus WHEN 1168 THEN N''دریافت شکایت'' WHEN 1169 THEN N''رسیدگی اولیه'' WHEN 1170 THEN N''تکمیل اطلاعات و رسیدگی نهایی''
            WHEN 1171 THEN N''اعلام نتیجه به مشتری'' WHEN 1172 THEN N''عدم جلب رضایت و بسته'' WHEN 1173 THEN N''جلب رضایت و بسته'' ELSE N''نامشخص'' END,
       N'''', COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.ComplaintStatus
UNION ALL
SELECT N''6'', N''وضعیت نهایی'',
       CASE c.FinalComplaintStatus WHEN 1182 THEN N''بسته شد'' WHEN 1183 THEN N''بسته نشد'' ELSE N''نامشخص'' END,
       N'''', COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.FinalComplaintStatus
UNION ALL
SELECT N''7'', N''نوع شکایت'',
       CASE c.ComplaintType WHEN 1531 THEN N''بحرانی'' WHEN 1532 THEN N''بسیار مهم'' WHEN 1533 THEN N''مهم'' WHEN 2739 THEN N''سایر'' ELSE N''نامشخص'' END,
       N'''', COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.ComplaintType
ORDER BY SortKey, Number DESC';
    DECLARE @SumQueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":null,"CustomQuery":"","Parameters":[{"Id":"fromDate","Name":"fromDate","DisplayName":"از تاریخ","DefaultValue":"","DataType":"nvarchar"},{"Id":"toDate","Name":"toDate","DisplayName":"تا تاریخ","DefaultValue":"","DataType":"nvarchar"}],"Selects":[{"Index":0,"Name":"dtRows","Title":"dtRows"}]}';
    SET @SumQueryJson = JSON_MODIFY(@SumQueryJson, '$.CustomQuery', @SumCustomQuery);
    DECLARE @SumColumnsJson NVARCHAR(MAX) = N'[]';
    DECLARE @SumContent NVARCHAR(MAX) = N'{"ReportVersion":"2024.3.2.0","ReportGuid":"c1a0b2d4-6e18-4a77-9f30-1b2c3d4e5f60","ReportName":"CustomerComplaintSummary","ReportAlias":"خلاصه شکایات مشتری","EngineVersion":"EngineV2","CalculationMode":"Interpretation","ReportUnit":"Centimeters","Dictionary":{"DataSources":{"0":{"Ident":"StiDataTableSource","Name":"dtRows","Alias":"dtRows","Key":"dtRows","Columns":{"0":{"Name":"Section","Index":-1,"NameInSource":"Section","Alias":"Section","Type":"System.String"},"1":{"Name":"Title","Index":-1,"NameInSource":"Title","Alias":"Title","Type":"System.String"},"2":{"Name":"Detail","Index":-1,"NameInSource":"Detail","Alias":"Detail","Type":"System.String"},"3":{"Name":"Number","Index":-1,"NameInSource":"Number","Alias":"Number","Type":"System.String"},"4":{"Name":"Percentage","Index":-1,"NameInSource":"Percentage","Alias":"Percentage","Type":"System.String"}},"NameInSource":"NewDataSet.dtRows"}}},"Pages":{"0":{"Ident":"StiPage","Name":"Page1","Guid":"c1a0b2d4-6e18-4a77-9f30-1b2c3d4e5f60","Border":";;2;;;;;solid:Black","Brush":"solid:","Components":{"0":{"Ident":"StiReportTitleBand","Name":"Title","ClientRectangle":"0,0,19.0,1.00","Components":{"0":{"Ident":"StiText","Name":"title","ClientRectangle":"0.00,0.00,19.00,1.00","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"خلاصه شکایات مشتری"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;14;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}}},"Brush":"solid:"},"1":{"Ident":"StiHeaderBand","Name":"Head","ClientRectangle":"0,1.10,19.0,0.70","Components":{"0":{"Ident":"StiText","Name":"h_Section","ClientRectangle":"0.00,0.00,4.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"بخش"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"1":{"Ident":"StiText","Name":"h_Title","ClientRectangle":"4.00,0.00,6.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"عنوان"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"2":{"Ident":"StiText","Name":"h_Detail","ClientRectangle":"10.00,0.00,4.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"جزئیات"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"3":{"Ident":"StiText","Name":"h_Number","ClientRectangle":"14.00,0.00,2.50,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"تعداد"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"4":{"Ident":"StiText","Name":"h_Percentage","ClientRectangle":"16.50,0.00,2.50,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"درصد"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}}},"Brush":"solid:230,230,230"},"2":{"Ident":"StiDataBand","Name":"Data","ClientRectangle":"0,1.90,19.0,0.70","Components":{"0":{"Ident":"StiText","Name":"d_Section","ClientRectangle":"0.00,0.00,4.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.Section}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"1":{"Ident":"StiText","Name":"d_Title","ClientRectangle":"4.00,0.00,6.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.Title}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"2":{"Ident":"StiText","Name":"d_Detail","ClientRectangle":"10.00,0.00,4.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.Detail}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"3":{"Ident":"StiText","Name":"d_Number","ClientRectangle":"14.00,0.00,2.50,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.Number}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"4":{"Ident":"StiText","Name":"d_Percentage","ClientRectangle":"16.50,0.00,2.50,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.Percentage}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}}},"DataSourceName":"dtRows","PrintIfEmpty":true,"CanGrow":true,"Brush":"solid:"}},"PaperSize":"A4","Orientation":"Landscape","PageWidth":29.7,"PageHeight":21.0,"RightToLeft":true,"Watermark":{"TextBrush":"solid:50,0,0,0"},"Margins":{"Left":1.0,"Right":1.0,"Top":0.8,"Bottom":0.8}}}}';
    DECLARE @SumSavedQueryId BIGINT;
    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @SumName)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @SumTitle, QueryJson = @SumQueryJson, ColumnsJson = @SumColumnsJson, EntityFullName = NULL,
            Mode = 1, Type = 0, ActionOptions = N'', CustomActionButtonsJson = N'[]',
            EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}', DiagramJson = N'[]',
            ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @SumName;
        SELECT @SumSavedQueryId = Id FROM [system].[SavedQuery] WHERE Name = @SumName;
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@SumName, @SumTitle, @SumQueryJson, @SumColumnsJson, N'[]', N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
                1, 0, NULL, N'', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @SumSavedQueryId = SCOPE_IDENTITY();
    END
    IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @SumName)
        UPDATE dbo.ReportBuilderReport
        SET Title = @SumTitle, BaseQuery = @SumCustomQuery, SavedQueryId = @SumSavedQueryId, Type = 1, ObjectType = N'SavedQuery',
            Content = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @SumContent ELSE Content END,
            ModifiedById = 1,
            ModifiedByName = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @SeedUser ELSE ModifiedByName END,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @SumName;
    ELSE
        INSERT INTO dbo.ReportBuilderReport
            (Name, Title, ObjectType, SavedQueryId, Type, Content, BaseQuery,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@SumName, @SumTitle, N'SavedQuery', @SumSavedQueryId, 1, @SumContent, @SumCustomQuery,
                1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
    DECLARE @SumReportId BIGINT = (SELECT Id FROM dbo.ReportBuilderReport WHERE Name = @SumName);
    
    IF NOT EXISTS (SELECT 1 FROM dbo.ReportBuilderReportItem WHERE ReportBuilderReportId = @SumReportId AND ColumnName = N'fromDate')
        INSERT INTO dbo.ReportBuilderReportItem
            (ColumnName, DataType, Title, Show, CanFilter, Required, ReportBuilderReportId, ReportBuilderReportSelectId,
             CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
        VALUES (N'fromDate', N'nvarchar', N'از تاریخ', 0, 0, 0, @SumReportId, NULL, 1, @SeedUser, @Now, @NowShamsi);
    IF NOT EXISTS (SELECT 1 FROM dbo.ReportBuilderReportItem WHERE ReportBuilderReportId = @SumReportId AND ColumnName = N'toDate')
        INSERT INTO dbo.ReportBuilderReportItem
            (ColumnName, DataType, Title, Show, CanFilter, Required, ReportBuilderReportId, ReportBuilderReportSelectId,
             CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
        VALUES (N'toDate', N'nvarchar', N'تا تاریخ', 0, 0, 0, @SumReportId, NULL, 1, @SeedUser, @Now, @NowShamsi);

    DECLARE @ActName NVARCHAR(200) = N'CustomerComplaintActions';
    DECLARE @ActTitle NVARCHAR(200) = N'چاپ اقدامات انجام شده';
    DECLARE @ActCustomQuery NVARCHAR(MAX) = N'SELECT c.SerialNumber, c.TrackingCode,
       ISNULL(party.FullName, c.CustomerTitle) AS CustomerName,
       a.ActionShamsiDate, a.ActionDescription, a.NextActionShamsiDate, a.NextActionDescription
FROM Crm.CustomerComplaint c
LEFT JOIN SLS.Customer cust ON cust.Id = c.CustomerId
LEFT JOIN Gnr.Party party ON party.Id = cust.PartyId
LEFT JOIN Crm.CustomerComplaintAction a ON a.ComplaintId = c.Id
WHERE c.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @complaintId))), N''''))
ORDER BY a.ActionMiladiDate, a.Id';
    DECLARE @ActQueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":null,"CustomQuery":"","Parameters":[{"Id":"complaintId","Name":"complaintId","DisplayName":"شناسه شکایت","DefaultValue":"","DataType":"nvarchar"}],"Selects":[{"Index":0,"Name":"dtRows","Title":"dtRows"}]}';
    SET @ActQueryJson = JSON_MODIFY(@ActQueryJson, '$.CustomQuery', @ActCustomQuery);
    DECLARE @ActColumnsJson NVARCHAR(MAX) = N'[]';
    DECLARE @ActContent NVARCHAR(MAX) = N'{"ReportVersion":"2024.3.2.0","ReportGuid":"d2b1c3e5-7f29-4b88-a041-2c3d4e5f6071","ReportName":"CustomerComplaintActions","ReportAlias":"اقدامات شکایت","EngineVersion":"EngineV2","CalculationMode":"Interpretation","ReportUnit":"Centimeters","Dictionary":{"DataSources":{"0":{"Ident":"StiDataTableSource","Name":"dtRows","Alias":"dtRows","Key":"dtRows","Columns":{"0":{"Name":"SerialNumber","Index":-1,"NameInSource":"SerialNumber","Alias":"SerialNumber","Type":"System.String"},"1":{"Name":"TrackingCode","Index":-1,"NameInSource":"TrackingCode","Alias":"TrackingCode","Type":"System.String"},"2":{"Name":"CustomerName","Index":-1,"NameInSource":"CustomerName","Alias":"CustomerName","Type":"System.String"},"3":{"Name":"ActionShamsiDate","Index":-1,"NameInSource":"ActionShamsiDate","Alias":"ActionShamsiDate","Type":"System.String"},"4":{"Name":"ActionDescription","Index":-1,"NameInSource":"ActionDescription","Alias":"ActionDescription","Type":"System.String"},"5":{"Name":"NextActionShamsiDate","Index":-1,"NameInSource":"NextActionShamsiDate","Alias":"NextActionShamsiDate","Type":"System.String"},"6":{"Name":"NextActionDescription","Index":-1,"NameInSource":"NextActionDescription","Alias":"NextActionDescription","Type":"System.String"}},"NameInSource":"NewDataSet.dtRows"}}},"Pages":{"0":{"Ident":"StiPage","Name":"Page1","Guid":"d2b1c3e5-7f29-4b88-a041-2c3d4e5f6071","Border":";;2;;;;;solid:Black","Brush":"solid:","Components":{"0":{"Ident":"StiReportTitleBand","Name":"Title","ClientRectangle":"0,0,19.0,1.00","Components":{"0":{"Ident":"StiText","Name":"title","ClientRectangle":"0.00,0.00,19.00,1.00","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"اقدامات شکایت"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;14;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}}},"Brush":"solid:"},"1":{"Ident":"StiHeaderBand","Name":"Head","ClientRectangle":"0,1.10,19.0,0.70","Components":{"0":{"Ident":"StiText","Name":"h_SerialNumber","ClientRectangle":"0.00,0.00,2.50,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"سریال"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"1":{"Ident":"StiText","Name":"h_TrackingCode","ClientRectangle":"2.50,0.00,2.20,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"رهگیری"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"2":{"Ident":"StiText","Name":"h_CustomerName","ClientRectangle":"4.70,0.00,4.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"مشتری"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"3":{"Ident":"StiText","Name":"h_ActionShamsiDate","ClientRectangle":"8.70,0.00,2.30,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"تاریخ"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"4":{"Ident":"StiText","Name":"h_ActionDescription","ClientRectangle":"11.00,0.00,4.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"شرح"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"5":{"Ident":"StiText","Name":"h_NextActionShamsiDate","ClientRectangle":"15.00,0.00,2.30,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"تاریخ بعدی"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"6":{"Ident":"StiText","Name":"h_NextActionDescription","ClientRectangle":"17.30,0.00,3.70,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"شرح بعدی"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}}},"Brush":"solid:230,230,230"},"2":{"Ident":"StiDataBand","Name":"Data","ClientRectangle":"0,1.90,19.0,0.70","Components":{"0":{"Ident":"StiText","Name":"d_SerialNumber","ClientRectangle":"0.00,0.00,2.50,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.SerialNumber}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"1":{"Ident":"StiText","Name":"d_TrackingCode","ClientRectangle":"2.50,0.00,2.20,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.TrackingCode}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"2":{"Ident":"StiText","Name":"d_CustomerName","ClientRectangle":"4.70,0.00,4.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.CustomerName}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"3":{"Ident":"StiText","Name":"d_ActionShamsiDate","ClientRectangle":"8.70,0.00,2.30,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.ActionShamsiDate}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"4":{"Ident":"StiText","Name":"d_ActionDescription","ClientRectangle":"11.00,0.00,4.00,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.ActionDescription}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"5":{"Ident":"StiText","Name":"d_NextActionShamsiDate","ClientRectangle":"15.00,0.00,2.30,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.NextActionShamsiDate}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}},"6":{"Ident":"StiText","Name":"d_NextActionDescription","ClientRectangle":"17.30,0.00,3.70,0.70","Interaction":{"Ident":"StiInteraction"},"Text":{"Value":"{dtRows.NextActionDescription}"},"HorAlignment":"Right","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}}},"DataSourceName":"dtRows","PrintIfEmpty":true,"CanGrow":true,"Brush":"solid:"}},"PaperSize":"A4","Orientation":"Landscape","PageWidth":29.7,"PageHeight":21.0,"RightToLeft":true,"Watermark":{"TextBrush":"solid:50,0,0,0"},"Margins":{"Left":1.0,"Right":1.0,"Top":0.8,"Bottom":0.8}}}}';
    DECLARE @ActSavedQueryId BIGINT;
    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @ActName)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @ActTitle, QueryJson = @ActQueryJson, ColumnsJson = @ActColumnsJson, EntityFullName = NULL,
            Mode = 1, Type = 0, ActionOptions = N'', CustomActionButtonsJson = N'[]',
            EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}', DiagramJson = N'[]',
            ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @ActName;
        SELECT @ActSavedQueryId = Id FROM [system].[SavedQuery] WHERE Name = @ActName;
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@ActName, @ActTitle, @ActQueryJson, @ActColumnsJson, N'[]', N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
                1, 0, NULL, N'', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @ActSavedQueryId = SCOPE_IDENTITY();
    END
    IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @ActName)
        UPDATE dbo.ReportBuilderReport
        SET Title = @ActTitle, BaseQuery = @ActCustomQuery, SavedQueryId = @ActSavedQueryId, Type = 1, ObjectType = N'SavedQuery',
            Content = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @ActContent ELSE Content END,
            ModifiedById = 1,
            ModifiedByName = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @SeedUser ELSE ModifiedByName END,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @ActName;
    ELSE
        INSERT INTO dbo.ReportBuilderReport
            (Name, Title, ObjectType, SavedQueryId, Type, Content, BaseQuery,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@ActName, @ActTitle, N'SavedQuery', @ActSavedQueryId, 1, @ActContent, @ActCustomQuery,
                1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
    DECLARE @ActReportId BIGINT = (SELECT Id FROM dbo.ReportBuilderReport WHERE Name = @ActName);
    
    IF NOT EXISTS (SELECT 1 FROM dbo.ReportBuilderReportItem WHERE ReportBuilderReportId = @ActReportId AND ColumnName = N'complaintId')
        INSERT INTO dbo.ReportBuilderReportItem
            (ColumnName, DataType, Title, Show, CanFilter, Required, ReportBuilderReportId, ReportBuilderReportSelectId,
             CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
        VALUES (N'complaintId', N'nvarchar', N'شناسه شکایت', 0, 0, 0, @ActReportId, NULL, 1, @SeedUser, @Now, @NowShamsi);

    COMMIT TRANSACTION;
    PRINT N'Customer complaint reports ready';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
