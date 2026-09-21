/*
System_FK=14 report SavedQueries filled from Havayar tables that HTS Gnr_Report System 14 actually reads.
Idempotent on SavedQuery.Name. See havayar-dataprofile-savedquery.mdc.
AfterSales_Sale_ServiceRequestPart (گزارش درخواست های پشتیبانی) is owned by
Seed_SaleServiceRequest_DataProfiles.sql — do not MERGE it here (would wipe ColumnsJson).
*/
SET NOCOUNT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-after-sales-reports';

DECLARE @ColId NVARCHAR(MAX) = N'[{"TableName":"","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6,"Visible":false,"PrimaryKey":true,"SortDirection":1,"SortOrder":0,"SelectIndex":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]';
DECLARE @Actions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

;WITH src AS (
    SELECT * FROM (VALUES
        (N'AfterSales_Sale_Mission', N'گزارش حق ماموریت پرسنل', N'entities.app.sale.mission',
         N'SELECT [t1].[Id] AS [t1_Id], [t1].[HokmNumber] AS [t1_HokmNumber], [t2].[Name] AS [t1_ExpertName], [t1].[DispatchShamsiDate] AS [t1_DispatchShamsiDate], [t1].[ReturnShamsiDate] AS [t1_ReturnShamsiDate], [t1].[HaveGuarantee] AS [t1_HaveGuarantee], [t1].[MissionRight] AS [t1_MissionRight], [t1].[BreakfastFee] AS [t1_BreakfastFee], [t1].[LunchFee] AS [t1_LunchFee], [t1].[DinnerFee] AS [t1_DinnerFee] FROM [Sale].[Mission] AS [t1] LEFT JOIN [system].[User] AS [t2] ON [t1].[ExpertId] = [t2].[Id]'),
        (N'AfterSales_Sale_Order', N'گزارش حواله های فروش', N'entities.app.sale.orderdetail',
         N'SELECT [t1].[Id] AS [t1_Id], [t1].[Seq] AS [t1_Seq], [t2].[Code] AS [t1_PartCode], [t2].[Name] AS [t1_PartName], [t1].[Qty] AS [t1_Qty] FROM [Sale].[OrderDetail] AS [t1] LEFT JOIN [Inv].[Part] AS [t2] ON [t1].[PartId] = [t2].[Id]'),
        (N'AfterSales_Sale_CustomerDebit', N'گزارش مانده حساب مشتری', N'entities.app.sale.collectionclaim',
         N'SELECT [t1].[Id] AS [t1_Id], [t1].[VchNumber] AS [t1_VchNumber], [t1].[DlTitle] AS [t1_DlTitle], [t1].[Debit] AS [t1_Debit], [t1].[Credit] AS [t1_Credit], [t3].[FullName] AS [t1_CustomerName] FROM [Sale].[CollectionClaim] AS [t1] LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[CustomerId] = [t2].[Id] LEFT JOIN [Gnr].[Party] AS [t3] ON [t2].[PartyId] = [t3].[Id]'),
        (N'AfterSales_Sale_RepairsReport', N'گزارش شناسنامه تعمیر', N'entities.app.rpr.repairrequest',
         N'SELECT [t1].[Id] AS [t1_Id], [t1].[IdNumber] AS [t1_IdNumber], [t1].[EntryShamsiDate] AS [t1_EntryShamsiDate], [t1].[Status] AS [t1_Status], [t1].[HasDelay] AS [t1_HasDelay], [t1].[HasContractor] AS [t1_HasContractor], [t3].[FullName] AS [t1_CustomerName], [t4].[Code] AS [t1_ProductCode] FROM [Rpr].[RepairRequest] AS [t1] LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[CustomerId] = [t2].[Id] LEFT JOIN [Gnr].[Party] AS [t3] ON [t2].[PartyId] = [t3].[Id] LEFT JOIN [Inv].[Part] AS [t4] ON [t1].[ProductId] = [t4].[Id]'),
        (N'AfterSales_Sale_EstimateCost', N'گزارش هزینه تقریبی', N'entities.app.rpr.estimatedcost',
         N'SELECT [t1].[Id] AS [t1_Id], [t1].[CustomerTitle] AS [t1_CustomerTitle], [t1].[ManHour] AS [t1_ManHour], [t1].[Comment] AS [t1_Comment], [t2].[Code] AS [t1_ProductCode] FROM [Rpr].[EstimatedCost] AS [t1] LEFT JOIN [Inv].[Part] AS [t2] ON [t1].[ProductId] = [t2].[Id]')
    ) v(Name, Title, EntityFullName, CustomQuery)
)
MERGE system.SavedQuery AS t
USING src AS s ON t.Name = s.Name
WHEN MATCHED THEN UPDATE SET
    Title = s.Title,
    EntityFullName = s.EntityFullName,
    QueryJson = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"' + REPLACE(s.CustomQuery, '"', '\"') + N'","Parameters":[],"Selects":null}',
    ActionOptions = @Actions,
    ModifiedByName = @SeedUser,
    ModifiedDateMiladiDateTime = @Now,
    ModifiedDateShamsiDateTime = @NowShamsi
WHEN NOT MATCHED THEN INSERT
    (Name, Title, EntityFullName, Mode, [Type], QueryJson, ColumnsJson, ActionOptions, CustomActionButtonsJson, DiagramJson,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES
    (s.Name, s.Title, s.EntityFullName, 1, 1,
     N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"' + REPLACE(s.CustomQuery, '"', '\"') + N'","Parameters":[],"Selects":null}',
     @ColId, @Actions, N'[]', N'{}',
     1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

PRINT N'Seeded AfterSales report Data Profiles from live Havayar tables';
