
IF OBJECT_ID(N'Cng.Vw_Repairs', N'V') IS NOT NULL
    DROP VIEW Cng.Vw_Repairs;
GO
CREATE VIEW Cng.Vw_Repairs
AS
SELECT
    r.Id,
    LTRIM(RTRIM(ISNULL(p.Code, N'') + CASE WHEN p.Code IS NOT NULL AND p.Name IS NOT NULL THEN N' - ' ELSE N'' END + ISNULL(p.Name, N''))) AS Product,
    p.Code AS Part_Code,
    p.Name AS Part_Name,
    r.CustomerId AS Customer_FK,
    r.Count,
    r.SendMethod AS SendMethodId,
    CASE r.SendMethod
        WHEN 868 THEN N'پیش کرایه'
        WHEN 869 THEN N'پس کرایه'
        WHEN 870 THEN N'تحویل کارشناس'
        WHEN 871 THEN N'تحویل نماینده مشتری'
        ELSE N''
    END AS SendMethodTitle,
    COALESCE(custParty.CompanyName, custParty.FullName, N'') AS Customer_Title,
    r.DlId AS DlId,
    r.EntryMiladiDate AS EntryDate,
    r.EntryShamsiDate AS EntryDate_Shamsi,
    r.DestinationAfterRepair AS DestinationAfterRepair_FK,
    r.DestinationAddress,
    r.CustomerPhone AS Customer_Phone,
    r.Comment,
    r.PreFactorNumber AS PreFactor_Number,
    r.DeliveryFormNumber AS DeliveryForm_Number,
    r.LoanFormNumber AS LoanForm_Number,
    r.CreatedById AS CreatedUserId,
    r.CreatedOnMiladiDateTime AS CreatedDate,
    r.CreatedOnShamsiDateTime AS CreatedDateInText,
    cust.Code AS Customer_Code,
    NULL AS Acc_DLTitle,
    CASE r.DestinationAfterRepair
        WHEN 293 THEN N'مشتری'
        WHEN 294 THEN N'نمایندگی'
        WHEN 295 THEN N'هوایار (داغی)'
        WHEN 296 THEN N'هوایار (برگشت امانی)'
        WHEN 2700 THEN N'هوایار'
        ELSE N''
    END AS DestinationAfterRepair,
    LTRIM(RTRIM(ISNULL(p.Code, N'') + N' ' + ISNULL(p.Name, N''))) AS AllProductName,
    NULL AS ProjectCode,
    NULL AS ProjectTitle,
    r.CreatedByName AS CreatedUser_Title,
    CASE r.Status
        WHEN 872 THEN N'در حال اجرا'
        WHEN 873 THEN N'غیرقابل تعمیر'
        WHEN 874 THEN N'نزد CNC'
        WHEN 875 THEN N'تکمیل شده'
        ELSE N''
    END AS RepairRequestStatus,
    r.WarehouseExitMiladiDate AS WarehouseExitDate,
    r.WarehouseExitShamsiDate AS WarehouseExitDateInText,
    r.WarehouseExitShamsiDate AS ExitDate_Shamsi,
    r.WarehouseDeliveryMiladiDate AS DeliveryDate_Warehouse,
    r.WarehouseDeliveryShamsiDate AS DeliveryDate_Warehouse_Shamsi,
    r.IsGuarantee,
    CASE WHEN r.IsGuarantee = 1 THEN N'گارانتی' ELSE N'وارانتی' END AS GuaranteeTitle,
    r.NeedForSendToEmployer,
    r.ConfirmerId AS ConfirmerUserId,
    r.ConfirmShamsiDate AS Confirm_Date,
    r.ConfirmTime AS Confirm_Time,
    r.Status AS StatusId,
    r.FailureReason,
    r.PartDescriptionBelonging,
    r.ProductId AS Product_FK,
    r.IdNumber AS IDNumber,
    r.Serial,
    r.AgencyId,
    COALESCE(agencyParty.CompanyName, agencyParty.FullName, N'') AS Agency,
    agency.Code AS AgencyCode,
    r.HasVendor,
    r.VendorName,
    r.VendoringCost
FROM Cng.Repairs AS r
LEFT JOIN Inv.Part AS p ON p.Id = r.ProductId
LEFT JOIN SLS.Customer AS cust ON cust.Id = r.CustomerId
LEFT JOIN Gnr.Party AS custParty ON custParty.Id = cust.PartyId
LEFT JOIN SLS.Customer AS agency ON agency.Id = r.AgencyId
LEFT JOIN Gnr.Party AS agencyParty ON agencyParty.Id = agency.PartyId;
GO

-- ============================================
-- CNG Repairs print reports (ReportBuilder / Stimulsoft)
-- Keys: CngRepairsLabel, CngRepairsLoanReturn, CngRepairsID
-- View: Cng.Vw_Repairs (HTS Vw_Cng_Repairs equivalent)
-- Idempotent. Apply with Seed_CngRepairs_Reports.ps1 (UTF-8 BOM).
-- sqlcmd without -f 65001 corrupts Persian.
-- ============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-repairs-reports';

-- ===== CngRepairsLabel =====
DECLARE @Name_CngRepairsLabel NVARCHAR(200) = N'CngRepairsLabel';
DECLARE @Title_CngRepairsLabel NVARCHAR(200) = N'برچسب تعمیرات CNG';
DECLARE @CustomQuery_CngRepairsLabel NVARCHAR(MAX) = N'SELECT
    v.Id,
    v.IDNumber,
    v.DeliveryDate_Warehouse_Shamsi,
    v.ExitDate_Shamsi,
    v.Customer_Title,
    v.Serial,
    v.Product,
    v.FailureReason,
    v.GuaranteeTitle,
    v.Part_Code,
    v.Part_Name,
    v.Count,
    v.Agency,
    v.EntryDate_Shamsi,
    v.SendMethodTitle,
    v.IsGuarantee
FROM Cng.Vw_Repairs AS v
WHERE v.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), COALESCE(@repairsId, @Id)))), N''''))';
DECLARE @QueryJson_CngRepairsLabel NVARCHAR(MAX) = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": null, "CustomQuery": null, "Parameters": [{"Id": "Id", "Name": "Id", "DisplayName": "شناسه تعمیر", "DefaultValue": "", "DataType": "int"}], "Selects": [{"Index": 0, "Name": "Report", "Title": "Report"}]}';
SET @QueryJson_CngRepairsLabel = JSON_MODIFY(@QueryJson_CngRepairsLabel, '$.CustomQuery', @CustomQuery_CngRepairsLabel);
DECLARE @ColumnsJson_CngRepairsLabel NVARCHAR(MAX) = N'[{"TableName": null, "ColumnName": "Id", "DisplayName": "شناسه", "Alliance": "Id", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": true, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "IDNumber", "DisplayName": "شماره شناسنامه", "Alliance": "IDNumber", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "DeliveryDate_Warehouse_Shamsi", "DisplayName": "تاریخ تحویل", "Alliance": "DeliveryDate_Warehouse_Shamsi", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "ExitDate_Shamsi", "DisplayName": "تاریخ خروج", "Alliance": "ExitDate_Shamsi", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Customer_Title", "DisplayName": "مشتری", "Alliance": "Customer_Title", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Serial", "DisplayName": "سریال", "Alliance": "Serial", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Product", "DisplayName": "تجهیز", "Alliance": "Product", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "FailureReason", "DisplayName": "علت خرابی", "Alliance": "FailureReason", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "GuaranteeTitle", "DisplayName": "گارانتی/وارانتی", "Alliance": "GuaranteeTitle", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Part_Code", "DisplayName": "کد کالا", "Alliance": "Part_Code", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Part_Name", "DisplayName": "نام کالا", "Alliance": "Part_Name", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Count", "DisplayName": "تعداد", "Alliance": "Count", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Agency", "DisplayName": "نمایندگی", "Alliance": "Agency", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "EntryDate_Shamsi", "DisplayName": "تاریخ ورود", "Alliance": "EntryDate_Shamsi", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "SendMethodTitle", "DisplayName": "نحوه ارسال", "Alliance": "SendMethodTitle", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "IsGuarantee", "DisplayName": "گارانتی", "Alliance": "IsGuarantee", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SystemType": 1, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}]';
DECLARE @SavedQueryId_CngRepairsLabel BIGINT;

IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name_CngRepairsLabel)
BEGIN
    UPDATE [system].[SavedQuery]
    SET Title = @Title_CngRepairsLabel,
        QueryJson = @QueryJson_CngRepairsLabel,
        ColumnsJson = @ColumnsJson_CngRepairsLabel,
        EntityFullName = N'entities.app.cng.repairs',
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
    WHERE Name = @Name_CngRepairsLabel;
    SELECT @SavedQueryId_CngRepairsLabel = Id FROM [system].[SavedQuery] WHERE Name = @Name_CngRepairsLabel;
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
        @Name_CngRepairsLabel, @Title_CngRepairsLabel, @QueryJson_CngRepairsLabel, @ColumnsJson_CngRepairsLabel, N'[]',
        N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
        1, 0, N'entities.app.cng.repairs', N'',
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    );
    SET @SavedQueryId_CngRepairsLabel = SCOPE_IDENTITY();
END;

IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @Name_CngRepairsLabel)
BEGIN
    UPDATE dbo.ReportBuilderReport
    SET Title = @Title_CngRepairsLabel,
        BaseQuery = @CustomQuery_CngRepairsLabel,
        SavedQueryId = @SavedQueryId_CngRepairsLabel,
        Type = 1,
        ObjectType = N'Table',
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi,
        IsActive = 1
    WHERE Name = @Name_CngRepairsLabel;
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
        @Name_CngRepairsLabel, @Title_CngRepairsLabel, N'Table', @SavedQueryId_CngRepairsLabel, 1, NULL, @CustomQuery_CngRepairsLabel,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi, 1
    );
END;


-- ===== CngRepairsLoanReturn =====
DECLARE @Name_CngRepairsLoanReturn NVARCHAR(200) = N'CngRepairsLoanReturn';
DECLARE @Title_CngRepairsLoanReturn NVARCHAR(200) = N'برگشت از امانت تعمیرات CNG';
DECLARE @CustomQuery_CngRepairsLoanReturn NVARCHAR(MAX) = N'SELECT
    v.Id,
    v.IDNumber,
    N''CNG/9/'' + ISNULL(CONVERT(nvarchar(50), v.IDNumber), N'''') AS FormNumber,
    v.SendMethodTitle,
    v.DestinationAddress,
    v.Customer_Phone,
    v.EntryDate_Shamsi
FROM Cng.Vw_Repairs AS v
WHERE v.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), COALESCE(@repairsId, @Id)))), N''''));

SELECT
    MAX(rp.Id) AS Id,
    p.Code AS Part_Code,
    p.Name AS Part_Name,
    ISNULL(rp.UnitName, pu.Title) AS PartUnit_Title,
    SUM(rp.UsedMount) - SUM(rp.ReturnMount) AS Qty,
    CAST(N'''' AS nvarchar(200)) AS Description,
    rp.RepairsId
FROM Cng.RepairsPart AS rp
INNER JOIN Inv.Part AS p ON p.Id = rp.PartId
LEFT JOIN Inv.PartUnit AS pu ON pu.Id = rp.PartUnitId
WHERE rp.RepairsId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), COALESCE(@repairsId, @Id)))), N''''))
GROUP BY
    p.Code, p.Name, ISNULL(rp.UnitName, pu.Title), rp.RepairsId
HAVING (SUM(rp.UsedMount) - SUM(rp.ReturnMount)) <> 0;';
DECLARE @QueryJson_CngRepairsLoanReturn NVARCHAR(MAX) = N'{"Tables": [],"Relations": [],"Filters": [],"CustomConditions": null,"CustomQuery": null,"Parameters": [{"Id": "Id","Name": "Id","DisplayName": "شناسه تعمیر","DefaultValue": "","DataType": "int"}],"Selects": [{"Index": 0,"Name": "Report","Title": "Report"},{"Index": 1,"Name": "Parts","Title": "Parts"}]}';
SET @QueryJson_CngRepairsLoanReturn = JSON_MODIFY(@QueryJson_CngRepairsLoanReturn, '$.CustomQuery', @CustomQuery_CngRepairsLoanReturn);
DECLARE @ColumnsJson_CngRepairsLoanReturn NVARCHAR(MAX) = N'[{"TableName": null,"ColumnName": "Id","DisplayName": "شناسه","Alliance": "Id","Address": null,"SystemTypeName": null,"SelectIndex": 0,"Visible": false,"PrimaryKey": true,"SystemType": 6,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "IDNumber","DisplayName": "شماره شناسنامه","Alliance": "IDNumber","Address": null,"SystemTypeName": null,"SelectIndex": 0,"Visible": true,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "FormNumber","DisplayName": "شماره فرم","Alliance": "FormNumber","Address": null,"SystemTypeName": null,"SelectIndex": 0,"Visible": true,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "SendMethodTitle","DisplayName": "نحوه ارسال","Alliance": "SendMethodTitle","Address": null,"SystemTypeName": null,"SelectIndex": 0,"Visible": true,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "DestinationAddress","DisplayName": "آدرس مقصد","Alliance": "DestinationAddress","Address": null,"SystemTypeName": null,"SelectIndex": 0,"Visible": false,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "Customer_Phone","DisplayName": "تلفن","Alliance": "Customer_Phone","Address": null,"SystemTypeName": null,"SelectIndex": 0,"Visible": false,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "EntryDate_Shamsi","DisplayName": "تاریخ ورود","Alliance": "EntryDate_Shamsi","Address": null,"SystemTypeName": null,"SelectIndex": 0,"Visible": false,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "Id","DisplayName": "شناسه قطعه","Alliance": "Id","Address": null,"SystemTypeName": null,"SelectIndex": 1,"Visible": false,"PrimaryKey": true,"SystemType": 6,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "Part_Code","DisplayName": "کد کالا","Alliance": "Part_Code","Address": null,"SystemTypeName": null,"SelectIndex": 1,"Visible": true,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "Part_Name","DisplayName": "شرح کالا","Alliance": "Part_Name","Address": null,"SystemTypeName": null,"SelectIndex": 1,"Visible": true,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "PartUnit_Title","DisplayName": "واحد","Alliance": "PartUnit_Title","Address": null,"SystemTypeName": null,"SelectIndex": 1,"Visible": true,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "Qty","DisplayName": "مقدار","Alliance": "Qty","Address": null,"SystemTypeName": null,"SelectIndex": 1,"Visible": true,"PrimaryKey": false,"SystemType": 7,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "Description","DisplayName": "توضیحات","Alliance": "Description","Address": null,"SystemTypeName": null,"SelectIndex": 1,"Visible": true,"PrimaryKey": false,"SystemType": 0,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null},{"TableName": null,"ColumnName": "RepairsId","DisplayName": "شناسه تعمیر","Alliance": "RepairsId","Address": null,"SystemTypeName": null,"SelectIndex": 1,"Visible": false,"PrimaryKey": false,"SystemType": 6,"SortDirection": null,"SortOrder": null,"GroupBy": false,"Options": [],"OptionSetting": null,"Aggregate": null,"Render": null,"IsCustom": false,"CustomColType": null,"HtmlTemplate": null,"BtnConfig": null,"InputConfig": null,"Width": 120,"Filterable": true,"Sortable": true,"ClassName": null}]';
DECLARE @SavedQueryId_CngRepairsLoanReturn BIGINT;

IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name_CngRepairsLoanReturn)
BEGIN
    UPDATE [system].[SavedQuery]
    SET Title = @Title_CngRepairsLoanReturn,
        QueryJson = @QueryJson_CngRepairsLoanReturn,
        ColumnsJson = @ColumnsJson_CngRepairsLoanReturn,
        EntityFullName = N'entities.app.cng.repairs',
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
    WHERE Name = @Name_CngRepairsLoanReturn;
    SELECT @SavedQueryId_CngRepairsLoanReturn = Id FROM [system].[SavedQuery] WHERE Name = @Name_CngRepairsLoanReturn;
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
        @Name_CngRepairsLoanReturn, @Title_CngRepairsLoanReturn, @QueryJson_CngRepairsLoanReturn, @ColumnsJson_CngRepairsLoanReturn, N'[]',
        N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
        1, 0, N'entities.app.cng.repairs', N'',
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    );
    SET @SavedQueryId_CngRepairsLoanReturn = SCOPE_IDENTITY();
END;

IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @Name_CngRepairsLoanReturn)
BEGIN
    UPDATE dbo.ReportBuilderReport
    SET Title = @Title_CngRepairsLoanReturn,
        BaseQuery = @CustomQuery_CngRepairsLoanReturn,
        SavedQueryId = @SavedQueryId_CngRepairsLoanReturn,
        Type = 1,
        ObjectType = N'Table',
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi,
        IsActive = 1
    WHERE Name = @Name_CngRepairsLoanReturn;
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
        @Name_CngRepairsLoanReturn, @Title_CngRepairsLoanReturn, N'Table', @SavedQueryId_CngRepairsLoanReturn, 1, NULL, @CustomQuery_CngRepairsLoanReturn,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi, 1
    );
END;


-- ===== CngRepairsID =====
DECLARE @Name_CngRepairsID NVARCHAR(200) = N'CngRepairsID';
DECLARE @Title_CngRepairsID NVARCHAR(200) = N'شناسنامه تعمیرات CNG';
DECLARE @CustomQuery_CngRepairsID NVARCHAR(MAX) = N'SELECT
    v.Id,
    v.IDNumber,
    v.DeliveryDate_Warehouse_Shamsi,
    v.EntryDate_Shamsi,
    v.Part_Name,
    v.Part_Code,
    v.Count,
    v.Serial,
    v.Agency,
    v.VendorName,
    v.Customer_Title,
    v.DestinationAddress,
    v.Customer_Phone,
    v.IsGuarantee,
    CAST(CASE WHEN v.IsGuarantee = 0 THEN 1 ELSE 0 END AS bit) AS ISWarranty,
    v.RepairRequestStatus,
    v.DestinationAfterRepair,
    v.FailureReason,
    v.VendoringCost,
    SUM(rp.BuyPrice) AS TotalBuy,
    SUM(rp.SalePrice) AS TotalSale,
    SUM(rp.TotalPrice) AS Total
FROM Cng.Vw_Repairs AS v
LEFT JOIN Cng.RepairsPart AS rp ON rp.RepairsId = v.Id
WHERE v.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @Id))), N''''))
GROUP BY
    v.Id, v.IDNumber, v.DeliveryDate_Warehouse_Shamsi, v.EntryDate_Shamsi,
    v.Part_Name, v.Part_Code, v.Count, v.Serial, v.Agency, v.VendorName,
    v.Customer_Title, v.DestinationAddress, v.Customer_Phone, v.IsGuarantee,
    v.RepairRequestStatus, v.DestinationAfterRepair, v.FailureReason, v.VendoringCost;

SELECT
    MAX(rp.Id) AS Id,
    p.Code AS Part_Code,
    p.Name AS Part_Name,
    p.Number AS Part_Number,
    ISNULL(rp.UnitName, pu.Title) AS PartUnit_Title,
    rp.DamagedPart,
    MAX(rp.BuyPrice) AS BuyPrice,
    rp.RepairsId,
    SUM(rp.UsedMount) - SUM(rp.ReturnMount) AS Tafazol,
    rp.SalePrice,
    rp.TotalPrice
FROM Cng.RepairsPart AS rp
INNER JOIN Inv.Part AS p ON p.Id = rp.PartId
LEFT JOIN Inv.PartUnit AS pu ON pu.Id = rp.PartUnitId
WHERE rp.RepairsId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @Id))), N''''))
GROUP BY
    p.Code, p.Name, p.Number, ISNULL(rp.UnitName, pu.Title),
    rp.DamagedPart, rp.RepairsId, rp.SalePrice, rp.TotalPrice
HAVING (SUM(rp.UsedMount) - SUM(rp.ReturnMount)) <> 0;

SELECT
    r.Id,
    pe.Name AS PrsName,
    pe.Family AS PrsFamily,
    mh.WorkDate,
    mh.StartTime,
    mh.EndTime,
    mh.ManHourPrice,
    RIGHT(N''0'' + CAST(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60 AS nvarchar(10)), 2)
      + N'':'' +
    RIGHT(N''0'' + CAST(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) % 60 AS nvarchar(10)), 2)
      AS WorkingHour,
    (DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60) * mh.ManHourPrice AS PriceManInHour,
    mh.Comment
FROM Cng.Repairs AS r
INNER JOIN Cng.RepairsManHours AS mh ON mh.RepairsId = r.Id
INNER JOIN Hcm.Personel AS pe ON pe.Id = mh.PersonelId
WHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @Id))), N''''));

DECLARE @hours INT;
DECLARE @mins INT;
DECLARE @working TABLE (WorkingHour nvarchar(10));
INSERT INTO @working(WorkingHour)
SELECT
    RIGHT(N''0'' + CAST(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60 AS nvarchar(10)), 2)
      + N'':'' +
    RIGHT(N''0'' + CAST(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) % 60 AS nvarchar(10)), 2)
FROM Cng.RepairsManHours AS mh
WHERE mh.RepairsId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @Id))), N''''));

SELECT @hours = ISNULL(SUM(DATEDIFF(HOUR, ''00:00'', WorkingHour)), 0)
    + ((ISNULL(SUM(DATEDIFF(MINUTE, ''00:00'', WorkingHour)), 0) - (ISNULL(SUM(DATEDIFF(HOUR, ''00:00'', WorkingHour)), 0) * 60)) / 60)
FROM @working;
SELECT @mins = (ISNULL(SUM(DATEDIFF(SECOND, ''00:00'', WorkingHour)), 0) - (ISNULL(@hours, 0) * 60 * 60)) / 60
FROM @working;
SELECT CAST(ISNULL(@hours, 0) AS nvarchar(4)) + N'':'' + RIGHT(N''0'' + CAST(ISNULL(@mins, 0) AS nvarchar(2)), 2) AS SumWorkingHour;

SELECT
    r.Id,
    SUM((DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60) * mh.ManHourPrice) AS PriceManInHour
FROM Cng.Repairs AS r
INNER JOIN Cng.RepairsManHours AS mh ON mh.RepairsId = r.Id
WHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @Id))), N''''))
GROUP BY r.Id;';
DECLARE @QueryJson_CngRepairsID NVARCHAR(MAX) = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": null, "CustomQuery": null, "Parameters": [{"Id": "Id", "Name": "Id", "DisplayName": "شناسه تعمیر", "DefaultValue": "", "DataType": "int"}], "Selects": [{"Index": 0, "Name": "CngRepairs", "Title": "CngRepairs"}, {"Index": 1, "Name": "CngRepairsPart", "Title": "CngRepairsPart"}, {"Index": 2, "Name": "CngRepairsManHour", "Title": "CngRepairsManHour"}, {"Index": 3, "Name": "CngRepairsManHourMaster", "Title": "CngRepairsManHourMaster"}, {"Index": 4, "Name": "CngRepairsTotalManHourPrice", "Title": "CngRepairsTotalManHourPrice"}]}';
SET @QueryJson_CngRepairsID = JSON_MODIFY(@QueryJson_CngRepairsID, '$.CustomQuery', @CustomQuery_CngRepairsID);
DECLARE @ColumnsJson_CngRepairsID NVARCHAR(MAX) = N'[{"TableName": null, "ColumnName": "Id", "DisplayName": "شناسه", "Alliance": "Id", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": true, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "IDNumber", "DisplayName": "شماره شناسنامه", "Alliance": "IDNumber", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "DeliveryDate_Warehouse_Shamsi", "DisplayName": "تحویل انبار", "Alliance": "DeliveryDate_Warehouse_Shamsi", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "EntryDate_Shamsi", "DisplayName": "ورود", "Alliance": "EntryDate_Shamsi", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Part_Name", "DisplayName": "نام کالا", "Alliance": "Part_Name", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Part_Code", "DisplayName": "کد کالا", "Alliance": "Part_Code", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Count", "DisplayName": "تعداد", "Alliance": "Count", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Serial", "DisplayName": "سریال", "Alliance": "Serial", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Agency", "DisplayName": "نمایندگی", "Alliance": "Agency", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "VendorName", "DisplayName": "فروشنده", "Alliance": "VendorName", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Customer_Title", "DisplayName": "مشتری", "Alliance": "Customer_Title", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "DestinationAddress", "DisplayName": "آدرس مقصد", "Alliance": "DestinationAddress", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Customer_Phone", "DisplayName": "تلفن", "Alliance": "Customer_Phone", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "IsGuarantee", "DisplayName": "گارانتی", "Alliance": "IsGuarantee", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 1, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "ISWarranty", "DisplayName": "وارانتی", "Alliance": "ISWarranty", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 1, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "RepairRequestStatus", "DisplayName": "وضعیت", "Alliance": "RepairRequestStatus", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "DestinationAfterRepair", "DisplayName": "مقصد", "Alliance": "DestinationAfterRepair", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "FailureReason", "DisplayName": "علت خرابی", "Alliance": "FailureReason", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "VendoringCost", "DisplayName": "هزینه برون‌سپاری", "Alliance": "VendoringCost", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "TotalBuy", "DisplayName": "جمع خرید", "Alliance": "TotalBuy", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "TotalSale", "DisplayName": "جمع فروش", "Alliance": "TotalSale", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Total", "DisplayName": "جمع", "Alliance": "Total", "Address": null, "SystemTypeName": null, "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Id", "DisplayName": "شناسه قطعه", "Alliance": "Id", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": false, "PrimaryKey": true, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Part_Code", "DisplayName": "کد قطعه", "Alliance": "Part_Code", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Part_Name", "DisplayName": "نام قطعه", "Alliance": "Part_Name", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Part_Number", "DisplayName": "شماره قطعه", "Alliance": "Part_Number", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": false, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "PartUnit_Title", "DisplayName": "واحد", "Alliance": "PartUnit_Title", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "DamagedPart", "DisplayName": "آسیب‌دیده", "Alliance": "DamagedPart", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": true, "PrimaryKey": false, "SystemType": 1, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "BuyPrice", "DisplayName": "خرید", "Alliance": "BuyPrice", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": true, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "RepairsId", "DisplayName": "شناسه تعمیر", "Alliance": "RepairsId", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": false, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Tafazol", "DisplayName": "مقدار", "Alliance": "Tafazol", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": true, "PrimaryKey": false, "SystemType": 7, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "SalePrice", "DisplayName": "فروش", "Alliance": "SalePrice", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": true, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "TotalPrice", "DisplayName": "جمع", "Alliance": "TotalPrice", "Address": null, "SystemTypeName": null, "SelectIndex": 1, "Visible": true, "PrimaryKey": false, "SystemType": 7, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Id", "DisplayName": "شناسه", "Alliance": "Id", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": false, "PrimaryKey": true, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "PrsName", "DisplayName": "نام", "Alliance": "PrsName", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "PrsFamily", "DisplayName": "نام خانوادگی", "Alliance": "PrsFamily", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "WorkDate", "DisplayName": "تاریخ کار", "Alliance": "WorkDate", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "StartTime", "DisplayName": "شروع", "Alliance": "StartTime", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "EndTime", "DisplayName": "پایان", "Alliance": "EndTime", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "ManHourPrice", "DisplayName": "نرخ", "Alliance": "ManHourPrice", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": true, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "WorkingHour", "DisplayName": "مدت", "Alliance": "WorkingHour", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "PriceManInHour", "DisplayName": "هزینه", "Alliance": "PriceManInHour", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": true, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Comment", "DisplayName": "توضیحات", "Alliance": "Comment", "Address": null, "SystemTypeName": null, "SelectIndex": 2, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "SumWorkingHour", "DisplayName": "جمع نفرساعت", "Alliance": "SumWorkingHour", "Address": null, "SystemTypeName": null, "SelectIndex": 3, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "Id", "DisplayName": "شناسه", "Alliance": "Id", "Address": null, "SystemTypeName": null, "SelectIndex": 4, "Visible": false, "PrimaryKey": true, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}, {"TableName": null, "ColumnName": "PriceManInHour", "DisplayName": "هزینه نفرساعت", "Alliance": "PriceManInHour", "Address": null, "SystemTypeName": null, "SelectIndex": 4, "Visible": true, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": null}]';
DECLARE @SavedQueryId_CngRepairsID BIGINT;

IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name_CngRepairsID)
BEGIN
    UPDATE [system].[SavedQuery]
    SET Title = @Title_CngRepairsID,
        QueryJson = @QueryJson_CngRepairsID,
        ColumnsJson = @ColumnsJson_CngRepairsID,
        EntityFullName = N'entities.app.cng.repairs',
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
    WHERE Name = @Name_CngRepairsID;
    SELECT @SavedQueryId_CngRepairsID = Id FROM [system].[SavedQuery] WHERE Name = @Name_CngRepairsID;
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
        @Name_CngRepairsID, @Title_CngRepairsID, @QueryJson_CngRepairsID, @ColumnsJson_CngRepairsID, N'[]',
        N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
        1, 0, N'entities.app.cng.repairs', N'',
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    );
    SET @SavedQueryId_CngRepairsID = SCOPE_IDENTITY();
END;

IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @Name_CngRepairsID)
BEGIN
    UPDATE dbo.ReportBuilderReport
    SET Title = @Title_CngRepairsID,
        BaseQuery = @CustomQuery_CngRepairsID,
        SavedQueryId = @SavedQueryId_CngRepairsID,
        Type = 1,
        ObjectType = N'Table',
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi,
        IsActive = 1
    WHERE Name = @Name_CngRepairsID;
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
        @Name_CngRepairsID, @Title_CngRepairsID, N'Table', @SavedQueryId_CngRepairsID, 1, NULL, @CustomQuery_CngRepairsID,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi, 1
    );
END;

SELECT
    (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'CngRepairsLabel') AS LabelSavedQueryId,
    (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'CngRepairsLoanReturn') AS LoanSavedQueryId,
    (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'CngRepairsID') AS IdSavedQueryId,
    (SELECT COUNT(*) FROM dbo.ReportBuilderReport WHERE Name IN (N'CngRepairsLabel', N'CngRepairsLoanReturn', N'CngRepairsID')) AS ReportCount,
    (SELECT TOP 1 Title FROM dbo.ReportBuilderReport WHERE Name = N'CngRepairsLoanReturn') AS LoanTitleSample;
