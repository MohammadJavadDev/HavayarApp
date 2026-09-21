/*
  Seed_AfterSales_CustomerRequest.sql
  HTS page 419 درخواست مشتریان: جدول + همگام‌سازی + نمایه + نقش View/Manage.
  CustomerAddress متن آزاد است (نه FK سایت). New از منوی داخلی خاموش (ثبت از فرم عمومی HTS).
  Encoding: UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-customerrequest';

    IF OBJECT_ID(N'Sale.CustomerRequest', N'U') IS NULL
    BEGIN
        CREATE TABLE Sale.CustomerRequest (
            Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            HtsId BIGINT NOT NULL CONSTRAINT DF_Sale_CustomerRequest_HtsId DEFAULT (0),
            OrganizationUnitId INT NOT NULL CONSTRAINT DF_Sale_CustomerRequest_Org DEFAULT (0),
            RequestTypeId INT NOT NULL CONSTRAINT DF_Sale_CustomerRequest_Type DEFAULT (0),
            RequestTypeTitle NVARCHAR(200) NULL,
            PartId BIGINT NULL,
            PartTitle NVARCHAR(128) NULL,
            RequestText NVARCHAR(1024) NULL,
            CustomerId BIGINT NULL,
            CustomerTitle NVARCHAR(128) NULL,
            CustomerAddress NVARCHAR(512) NULL,
            CustomerPhone NVARCHAR(11) NULL,
            AttachmentFileName NVARCHAR(800) NULL,
            AttachmentFile VARBINARY(MAX) NULL,
            StatusId INT NULL,
            ProvinceId BIGINT NULL,
            HasHavayarProduction BIT NULL,
            RelatedAgentName NVARCHAR(128) NULL,
            CreatedById BIGINT NULL,
            ModifiedById BIGINT NULL,
            CreatedByName NVARCHAR(150) NULL,
            ModifiedByName NVARCHAR(150) NULL,
            ModifiedDateMiladiDateTime DATETIME2 NULL,
            ModifiedDateShamsiDateTime NVARCHAR(30) NULL,
            CreatedOnMiladiDateTime DATETIME2 NULL,
            CreatedOnShamsiDateTime NVARCHAR(30) NULL,
            IsActive INT NULL
        );
        CREATE UNIQUE INDEX IX_Sale_CustomerRequest_HtsId ON Sale.CustomerRequest(HtsId) WHERE HtsId <> CAST(0 AS bigint);
    END

    IF OBJECT_ID(N'Sale.CustomerRequestComment', N'U') IS NULL
    BEGIN
        CREATE TABLE Sale.CustomerRequestComment (
            Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            HtsId BIGINT NOT NULL CONSTRAINT DF_Sale_CustomerRequestComment_HtsId DEFAULT (0),
            CustomerRequestId BIGINT NULL,
            StatusId INT NULL,
            Comment NVARCHAR(512) NULL,
            CreatedById BIGINT NULL,
            ModifiedById BIGINT NULL,
            CreatedByName NVARCHAR(150) NULL,
            ModifiedByName NVARCHAR(150) NULL,
            ModifiedDateMiladiDateTime DATETIME2 NULL,
            ModifiedDateShamsiDateTime NVARCHAR(30) NULL,
            CreatedOnMiladiDateTime DATETIME2 NULL,
            CreatedOnShamsiDateTime NVARCHAR(30) NULL,
            IsActive INT NULL
        );
        CREATE UNIQUE INDEX IX_Sale_CustomerRequestComment_HtsId ON Sale.CustomerRequestComment(HtsId) WHERE HtsId <> CAST(0 AS bigint);
    END

    MERGE Sale.CustomerRequest AS t
    USING (
        SELECT
            CAST(r.Id AS BIGINT) AS HtsId,
            CAST(r.OrganizationUnitId AS INT) AS OrganizationUnitId,
            CAST(r.RequestTypeId AS INT) AS RequestTypeId,
            NULLIF(LTRIM(RTRIM(lt.Lookup_Title_Fa)), N'') AS RequestTypeTitle,
            p.Id AS PartId,
            NULLIF(LTRIM(RTRIM(r.PartTitle)), N'') AS PartTitle,
            NULLIF(LTRIM(RTRIM(r.RequestText)), N'') AS RequestText,
            c.Id AS CustomerId,
            NULLIF(LTRIM(RTRIM(r.CustomerTitle)), N'') AS CustomerTitle,
            NULLIF(LTRIM(RTRIM(r.CustomerAddress)), N'') AS CustomerAddress,
            NULLIF(LTRIM(RTRIM(r.CustomerPhone)), N'') AS CustomerPhone,
            NULLIF(LTRIM(RTRIM(r.AttachmentFileName)), N'') AS AttachmentFileName,
            r.AttachmentFile,
            CAST(r.StatusId AS INT) AS StatusId,
            mapProv.Id AS ProvinceId,
            r.HasHavayarProduction,
            NULLIF(LTRIM(RTRIM(r.RelatedAgentName)), N'') AS RelatedAgentName,
            r.CreatedDate AS CreatedOnMiladiDateTime,
            NULLIF(LTRIM(RTRIM(r.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Sale_CustomerRequest] r
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Lookup] lt ON lt.Lookup_ID = r.RequestTypeId
        LEFT JOIN Inv.Part p ON p.HtsId = r.PartId AND ISNULL(p.HtsId, 0) <> 0
        LEFT JOIN SLS.Customer c ON c.HtsId = r.CustomerId AND ISNULL(c.HtsId, 0) <> 0
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Province] gp ON gp.Province_ID = r.ProvinceId
        OUTER APPLY (
            SELECT TOP 1 rg.Id
            FROM Gnr.Region rg
            WHERE rg.[Type] = 2 AND (
                (gp.Province_Title LIKE N'%تهران%' AND rg.Name = N'استان تهران')
                OR rg.Name = N'استان ' + gp.Province_Title
                OR rg.Name = gp.Province_Title
            )
            ORDER BY CASE WHEN rg.Name = N'استان ' + gp.Province_Title THEN 0 ELSE 1 END
        ) mapProv
    ) s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.OrganizationUnitId = s.OrganizationUnitId,
        t.RequestTypeId = s.RequestTypeId,
        t.RequestTypeTitle = s.RequestTypeTitle,
        t.PartId = s.PartId,
        t.PartTitle = s.PartTitle,
        t.RequestText = s.RequestText,
        t.CustomerId = s.CustomerId,
        t.CustomerTitle = s.CustomerTitle,
        t.CustomerAddress = s.CustomerAddress,
        t.CustomerPhone = s.CustomerPhone,
        t.AttachmentFileName = s.AttachmentFileName,
        t.AttachmentFile = s.AttachmentFile,
        t.StatusId = s.StatusId,
        t.ProvinceId = s.ProvinceId,
        t.HasHavayarProduction = s.HasHavayarProduction,
        t.RelatedAgentName = s.RelatedAgentName,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, OrganizationUnitId, RequestTypeId, RequestTypeTitle, PartId, PartTitle, RequestText,
         CustomerId, CustomerTitle, CustomerAddress, CustomerPhone, AttachmentFileName, AttachmentFile,
         StatusId, ProvinceId, HasHavayarProduction, RelatedAgentName,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.OrganizationUnitId, s.RequestTypeId, s.RequestTypeTitle, s.PartId, s.PartTitle, s.RequestText,
         s.CustomerId, s.CustomerTitle, s.CustomerAddress, s.CustomerPhone, s.AttachmentFileName, s.AttachmentFile,
         s.StatusId, s.ProvinceId, s.HasHavayarProduction, s.RelatedAgentName,
         1, @SeedUser, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
         1, @SeedUser, @Now, @NowShamsi, 1);

    MERGE Sale.CustomerRequestComment AS t
    USING (
        SELECT
            CAST(cm.Id AS BIGINT) AS HtsId,
            cr.Id AS CustomerRequestId,
            CAST(cm.StatusId AS INT) AS StatusId,
            NULLIF(LTRIM(RTRIM(cm.Comment)), N'') AS Comment,
            um.NewUserId AS CreatedById,
            um.Username AS CreatedByName,
            cm.CreatedDate AS CreatedOnMiladiDateTime,
            NULLIF(LTRIM(RTRIM(cm.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Sale_CustomerRequestComment] cm
        INNER JOIN Sale.CustomerRequest cr ON cr.HtsId = cm.CustomerRequestId
        LEFT JOIN (
            SELECT u.User_ID AS OldUserId, hu.Id AS NewUserId, hu.Username
            FROM [TMS].[TotalSystem].[dbo].[Gnr_User] u
            INNER JOIN system.[User] hu ON LOWER(hu.Username) = LOWER(u.Username)
        ) um ON um.OldUserId = cm.CreatedUserId
    ) s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.CustomerRequestId = s.CustomerRequestId, t.StatusId = s.StatusId, t.Comment = s.Comment,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, CustomerRequestId, StatusId, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.CustomerRequestId, s.StatusId, s.Comment,
         s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
         1, @SeedUser, @Now, @NowShamsi, 1);

    IF OBJECT_ID('tempdb..#CrRoles') IS NOT NULL DROP TABLE #CrRoles;
    CREATE TABLE #CrRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #CrRoles VALUES
        (500030, N'Sale.AfterSales.CustomerRequest.Manage', N'درخواست مشتریان - مدیریت (HTS 285)'),
        (500031, N'Sale.AfterSales.CustomerRequest.View', N'درخواست مشتریان - مشاهده (HTS 350/516)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #CrRoles;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RName)
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Id = @Rid)
            BEGIN
                SET IDENTITY_INSERT system.Role ON;
                INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@Rid, @RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                SET IDENTITY_INSERT system.Role OFF;
            END
            ELSE
                INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        END
        FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    END
    CLOSE rc; DEALLOCATE rc;

    DELETE ra FROM system.RoleAccess ra
    WHERE ra.Path LIKE N'/panel/sale/customerrequest/%'
       OR (ra.ActionAccessType = 3 AND ra.EntityName = N'Entities.App.Sale.CustomerRequest');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.AType, a.IType, N'Entities.App.Sale.CustomerRequest', NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
        (N'/panel/sale/customerrequest/list', 1, 1),
        (N'/panel/sale/customerrequest/fetchdata', 2, 2),
        (N'/panel/sale/customerrequest/exporttoexcel', 2, 0),
        (N'/panel/sale/customerrequest/edit', 1, 5),
        (N'/panel/sale/customerrequest/getcomments', 2, 2),
        (N'/panel/sale/customerrequest/downloadattachment', 1, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'Sale.AfterSales.CustomerRequest.View'), (N'Sale.AfterSales.CustomerRequest.Manage')) x(RoleName)
    INNER JOIN system.Role r ON r.Name = x.RoleName;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.AType, a.IType, N'Entities.App.Sale.CustomerRequest', NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
        (N'/panel/sale/customerrequest/addcomment', 2, 4)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'Sale.AfterSales.CustomerRequest.Manage')) x(RoleName)
    INNER JOIN system.Role r ON r.Name = x.RoleName;

    DECLARE @PName NVARCHAR(100) = N'AfterSales_CustomerRequest_List';
    DECLARE @PTitle NVARCHAR(200) = N'درخواست مشتریان';
    DECLARE @PEnt NVARCHAR(200) = N'entities.app.sale.customerrequest';
    DECLARE @PPascal NVARCHAR(200) = N'Entities.App.Sale.CustomerRequest';
    DECLARE @PSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[CustomerTitle] AS [t1_CustomerTitle],
    [t1].[CustomerPhone] AS [t1_CustomerPhone],
    [t1].[CustomerAddress] AS [t1_CustomerAddress],
    [t1].[RequestTypeTitle] AS [t1_RequestTypeTitle],
    [t2].[Name] AS [t2_Name],
    [t1].[PartTitle] AS [t1_PartTitle],
    [t1].[RequestText] AS [t1_RequestText],
    [t3].[Name] AS [t3_Name],
    [t1].[HasHavayarProduction] AS [t1_HasHavayarProduction],
    [t1].[AttachmentFileName] AS [t1_AttachmentFileName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Sale].[CustomerRequest] AS [t1]
LEFT JOIN [Inv].[Part] AS [t2] ON [t1].[PartId] = [t2].[Id]
LEFT JOIN [Gnr].[Region] AS [t3] ON [t1].[ProvinceId] = [t3].[Id]';
    DECLARE @PCols NVARCHAR(MAX) = N'[{"TableName":"Sale.CustomerRequest","ColumnName":"Id","DisplayName":"کد پیگیری","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CustomerRequest","ColumnName":"CustomerTitle","DisplayName":"عنوان مشتری","Alliance":"t1_CustomerTitle","Address":"[t1].[CustomerTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CustomerRequest","ColumnName":"CustomerPhone","DisplayName":"تلفن","Alliance":"t1_CustomerPhone","Address":"[t1].[CustomerPhone]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CustomerRequest","ColumnName":"CustomerAddress","DisplayName":"آدرس","Alliance":"t1_CustomerAddress","Address":"[t1].[CustomerAddress]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CustomerRequest","ColumnName":"RequestTypeTitle","DisplayName":"نوع درخواست","Alliance":"t1_RequestTypeTitle","Address":"[t1].[RequestTypeTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"Name","DisplayName":"کالا","Alliance":"t2_Name","Address":"[t2].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CustomerRequest","ColumnName":"PartTitle","DisplayName":"شرح کالا","Alliance":"t1_PartTitle","Address":"[t1].[PartTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CustomerRequest","ColumnName":"RequestText","DisplayName":"متن درخواست","Alliance":"t1_RequestText","Address":"[t1].[RequestText]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Region","ColumnName":"Name","DisplayName":"استان","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CustomerRequest","ColumnName":"HasHavayarProduction","DisplayName":"تولید هوایار","Alliance":"t1_HasHavayarProduction","Address":"[t1].[HasHavayarProduction]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CustomerRequest","ColumnName":"AttachmentFileName","DisplayName":"پیوست","Alliance":"t1_AttachmentFileName","Address":"[t1].[AttachmentFileName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CustomerRequest","ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @PAct NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @PBtn NVARCHAR(MAX) = N'[{"id":"as_edit_customerrequest","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sale/CustomerRequest/Edit?id='' + id, true, ''ویرایش'');\n}"}]';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    DECLARE @PQueryJson NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);
    DECLARE @PId BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title=@PTitle, QueryJson=@PQueryJson, ColumnsJson=@PCols, EntityFullName=@PEnt, Mode=1, Type=1,
            ActionOptions=@PAct, CustomActionButtonsJson=@PBtn, EventScriptsJson=N'{"onSelectedRow":"","onRowAdded":""}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@PName;
        SELECT @PId = Id FROM system.SavedQuery WHERE Name=@PName;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@PName, @PTitle, @PQueryJson, @PCols, N'{}', @PBtn, N'{"onSelectedRow":"","onRowAdded":""}',
             1, 1, @PEnt, @PAct, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @PId = SCOPE_IDENTITY();
    END

    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @PId;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@PId AS nvarchar(20)), 3, 7, @PPascal, @PTitle, NULL, @PId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'ShowAllMenus', N'Sale.AfterSales.CustomerRequest.Manage', N'Sale.AfterSales.CustomerRequest.View');

    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'AfterSalesServiceSystem');
    IF @MenuId IS NOT NULL
    BEGIN
        UPDATE system.SystemMenu
        SET Content = CASE
            WHEN Content LIKE N'%/panel/sale/customerrequest/list%' THEN Content
            ELSE REPLACE(Content,
                N'{"text":"سایت های مشتریان"',
                N'{"text":"درخواست مشتریان","icon":"ki-message-text ki-outline","iconColor":"#50dcae","path":"/panel/sale/customerrequest/list","a_attr":{"href":"/panel/sale/customerrequest/list"},"data":{"iconColor":"#50dcae","path":"/panel/sale/customerrequest/list"},"children":[]},{"text":"سایت های مشتریان"')
            END,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;

        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles SELECT DISTINCT LTRIM(RTRIM(j.value))
        FROM system.SystemMenu m CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND LTRIM(RTRIM(j.value)) <> N'';
        INSERT INTO #MenuRoles
        SELECT v.RoleName FROM (VALUES
            (N'Sale.AfterSales.CustomerRequest.Manage'),
            (N'Sale.AfterSales.CustomerRequest.View')
        ) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles x WHERE x.RoleName = v.RoleName);

        DECLARE @AccessRoles NVARCHAR(MAX);
        DECLARE @AccessRoleIds NVARCHAR(MAX);
        SELECT @AccessRoles = N'[' + STUFF((
            SELECT N',"' + RoleName + N'"' FROM #MenuRoles ORDER BY RoleName
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        SELECT @AccessRoleIds = N'[' + STUFF((
            SELECT N',' + CAST(r.Id AS nvarchar(20))
            FROM #MenuRoles m INNER JOIN system.Role r ON r.Name = m.RoleName
            ORDER BY r.Id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        UPDATE system.SystemMenu
        SET AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds
        WHERE Id = @MenuId;
    END

    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL);
    INSERT INTO #UserRoleMap
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username),
           CASE WHEN g.Permission_ID = 2 THEN N'Sale.AfterSales.CustomerRequest.Manage'
                ELSE N'Sale.AfterSales.CustomerRequest.View' END
    FROM (
        SELECT pa.Permission_FK AS Permission_ID, m.User_FK
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        WHERE pa.Page_FK = 419 AND pug.UserGroup_FK IN (285, 350, 516)
        UNION
        SELECT pa.Permission_FK, pau.User_FK
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pau.PageAction_FK
        WHERE pa.Page_FK = 419
    ) g
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = g.User_FK
    WHERE COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username) IS NOT NULL
      AND g.Permission_ID IN (2, 3);

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName;
        SELECT TOP 1 @MapUserId = Id FROM system.[User] u
        WHERE LOWER(u.Username) = LOWER(@MapUsername)
        ORDER BY u.Id;
        IF @MapUserId IS NOT NULL AND @MapRoleId IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
               WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
           )
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + @MapRoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @MapRoleName + N'"]')
                END
            WHERE Id = @MapUserId;
        END
        SET @MapUserId = NULL; SET @MapRoleId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    END
    CLOSE map_cur; DEALLOCATE map_cur;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_AfterSales_CustomerRequest ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT COUNT(*) AS Requests FROM Sale.CustomerRequest;
SELECT COUNT(*) AS Comments FROM Sale.CustomerRequestComment;
SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'AfterSales_CustomerRequest_List';
