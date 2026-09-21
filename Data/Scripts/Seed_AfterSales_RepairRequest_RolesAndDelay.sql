/*
  Seed_AfterSales_RepairRequest_RolesAndDelay.sql
  نقش‌های صفحه 270 از گروه‌های HTS 109/117/61/350/359/537
  و نمایه تاخیر با فرمول VwRepairRequestDelays
  Encoding: UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-repairrequest-roles';

    IF OBJECT_ID('tempdb..#RrRoles') IS NOT NULL DROP TABLE #RrRoles;
    CREATE TABLE #RrRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #RrRoles VALUES
        (500032, N'Rpr.RepairRequest.AfterSale', N'درخواست تعمیر - خدمات (HTS 109)'),
        (500033, N'Rpr.RepairRequest.Repairs', N'درخواست تعمیر - تعمیرات (HTS 117)'),
        (500034, N'Rpr.RepairRequest.View', N'درخواست تعمیر - مشاهده (HTS 61/350)'),
        (500035, N'Rpr.RepairRequest.ShowAll', N'درخواست تعمیر - نمایش همه (HTS 537)'),
        (500036, N'Rpr.RepairRequest.Imaging', N'تصویربرداری محتوا (HTS 359)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #RrRoles;
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
    WHERE ra.Path LIKE N'/panel/rpr/repairrequest%'
      AND ra.RoleId IN (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales');

    DELETE ra FROM system.RoleAccess ra
    WHERE ra.Path = N'/panel/rpr/repairrequest/sendtocontractor';

    DELETE ra FROM system.RoleAccess ra
    WHERE ra.Path LIKE N'/panel/rpr/repairrequest%'
      AND ra.RoleId IN (SELECT Id FROM system.Role WHERE Name IN (
          N'Rpr.RepairRequest.AfterSale', N'Rpr.RepairRequest.Repairs', N'Rpr.RepairRequest.View',
          N'Rpr.RepairRequest.ShowAll', N'Rpr.RepairRequest.Imaging'));

    IF OBJECT_ID('tempdb..#RrAcc') IS NOT NULL DROP TABLE #RrAcc;
    CREATE TABLE #RrAcc (Path NVARCHAR(300) NOT NULL, AType INT NOT NULL, IType INT NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #RrAcc (Path, AType, IType, RoleName)
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequest/list', 1, 1),
        (N'/panel/rpr/repairrequest/fetchdata', 2, 2),
        (N'/panel/rpr/repairrequest/edit', 1, 5),
        (N'/panel/rpr/repairrequest/exporttoexcel', 2, 0),
        (N'/panel/rpr/repairrequest/delaylist', 1, 1),
        (N'/panel/rpr/repairrequest/delaychart', 1, 13),
        (N'/panel/rpr/repairrequest/delaychartdata', 2, 2)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'Rpr.RepairRequest.AfterSale'),
        (N'Rpr.RepairRequest.Repairs'),
        (N'Rpr.RepairRequest.View'),
        (N'Rpr.RepairRequest.ShowAll'),
        (N'ShowAllMenus'),
        (N'Rpr.Repairs')
    ) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequest/new', 1, 4),
        (N'/panel/rpr/repairrequest/save', 2, 3),
        (N'/panel/rpr/repairrequest/add', 2, 4),
        (N'/panel/rpr/repairrequest/update', 2, 5),
        (N'/panel/rpr/repairrequest/aftersaleaccess', 1, 1000),
        (N'/panel/rpr/repairrequest/confirm', 2, 1000)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.AfterSale'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequest/save', 2, 3),
        (N'/panel/rpr/repairrequest/update', 2, 5),
        (N'/panel/rpr/repairrequest/repairsaccess', 1, 1000)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.Repairs'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequest/contentproductionimaging', 1, 1000),
        (N'/panel/rpr/repairrequest/edit', 1, 5),
        (N'/panel/rpr/repairrequest/save', 2, 3),
        (N'/panel/rpr/repairrequest/update', 2, 5)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.Imaging'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequestcomment/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestcomment/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestcomment/new', 1, 4),
        (N'/panel/rpr/repairrequestcomment/edit', 1, 5),
        (N'/panel/rpr/repairrequestcomment/save', 2, 3),
        (N'/panel/rpr/repairrequestattachment/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestattachment/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestattachment/new', 1, 4),
        (N'/panel/rpr/repairrequestattachment/edit', 1, 5),
        (N'/panel/rpr/repairrequestattachment/save', 2, 3)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.AfterSale'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequestpart/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestpart/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestpart/new', 1, 4),
        (N'/panel/rpr/repairrequestpart/edit', 1, 5),
        (N'/panel/rpr/repairrequestpart/save', 2, 3),
        (N'/panel/rpr/repairrequestusedpart/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestusedpart/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestusedpart/new', 1, 4),
        (N'/panel/rpr/repairrequestusedpart/edit', 1, 5),
        (N'/panel/rpr/repairrequestusedpart/save', 2, 3),
        (N'/panel/rpr/repairrequestmanhour/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestmanhour/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestmanhour/new', 1, 4),
        (N'/panel/rpr/repairrequestmanhour/edit', 1, 5),
        (N'/panel/rpr/repairrequestmanhour/save', 2, 3),
        (N'/panel/rpr/repairrequestcontractor/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestcontractor/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestcontractor/new', 1, 4),
        (N'/panel/rpr/repairrequestcontractor/edit', 1, 5),
        (N'/panel/rpr/repairrequestcontractor/save', 2, 3),
        (N'/panel/rpr/repairrequestwbs/listbyrepairrequestid', 1, 1),
        (N'/panel/rpr/repairrequestwbs/getlistbyrepairrequestid', 2, 2)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.Repairs'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.AType, a.IType, N'Entities.App.Rpr.RepairRequest', NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #RrAcc a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.AType);

    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL);
    INSERT INTO #UserRoleMap
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username),
           CASE
                WHEN g.UserGroup_FK = 109 THEN N'Rpr.RepairRequest.AfterSale'
                WHEN g.UserGroup_FK = 117 THEN N'Rpr.RepairRequest.Repairs'
                WHEN g.UserGroup_FK = 359 THEN N'Rpr.RepairRequest.Imaging'
                WHEN g.UserGroup_FK = 537 THEN N'Rpr.RepairRequest.ShowAll'
                ELSE N'Rpr.RepairRequest.View'
           END
    FROM (
        SELECT pug.UserGroup_FK, m.User_FK
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        WHERE pa.Page_FK = 270 AND pug.UserGroup_FK IN (61, 109, 117, 350, 359, 537)
        UNION
        SELECT CASE pa.Permission_FK
                   WHEN 70 THEN 109 WHEN 71 THEN 117 WHEN 130 THEN 359 WHEN 7 THEN 537 ELSE 61 END, pau.User_FK
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pau.PageAction_FK
        WHERE pa.Page_FK = 270
    ) g
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = g.User_FK
    WHERE COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username) IS NOT NULL;

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE um CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
    OPEN um; FETCH NEXT FROM um INTO @MapUsername, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName;
        SELECT TOP 1 @MapUserId = Id FROM [system].[User] u
        WHERE LOWER(u.Username) = LOWER(@MapUsername)
        ORDER BY u.Id;
        IF @MapUserId IS NOT NULL AND @MapRoleId IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM [system].[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
               WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
           )
        BEGIN
            UPDATE [system].[User]
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
        FETCH NEXT FROM um INTO @MapUsername, @MapRoleName;
    END
    CLOSE um; DEALLOCATE um;

    DECLARE @PName NVARCHAR(100) = N'AfterSales_RepairRequest_Delay';
    DECLARE @PTitle NVARCHAR(200) = N'تاخیرات تعمیرات';
    DECLARE @PEnt NVARCHAR(200) = N'entities.app.rpr.repairrequest';
    DECLARE @PPascal NVARCHAR(200) = N'Entities.App.Rpr.RepairRequest';
    DECLARE @PSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    TRY_CAST([t1].[IdNumber] AS bigint) AS [t1_IdNumber],
    [t2].[Code] AS [t2_Code],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t1].[IsGuarantee] AS [t1_IsGuarantee],
    [t1].[CustomerAddressManual] AS [t1_CustomerAddressManual],
    [t1].[Status] AS [t1_Status],
    CASE WHEN DATEDIFF(DAY, [t1].[RepairEntranceMiladiDate], [t1].[PreCheckMiladiDate]) > 2
         THEN DATEDIFF(DAY, [t1].[RepairEntranceMiladiDate], [t1].[PreCheckMiladiDate]) ELSE 0 END AS [PreCheckDelay],
    DATEDIFF(DAY, [t1].[AgreedMiladiDate], [t1].[WarehouseDeliveryMiladiDate]) AS [RepairDelay],
    DATEDIFF(DAY, [t1].[BuyRequestMiladiDate], [t1].[SupplyMiladiDate]) AS [SupplyDelay],
    DATEDIFF(DAY, [t1].[QcRequestMiladiDate], [t1].[WarehouseDeliveryMiladiDate]) AS [TestDelay],
    CASE WHEN [t1].[Status] NOT IN (2093, 304, 2236) AND [t1].[AgreedMiladiDate] < GETDATE()
         THEN DATEDIFF(DAY, [t1].[AgreedMiladiDate], GETDATE()) ELSE NULL END AS [AgreedDelayInDay],
    CASE WHEN [t1].[SafekeepingReturnMiladiDate] IS NULL
         THEN DATEDIFF(DAY, [t1].[WarehouseDeliveryMiladiDate], GETDATE())
         ELSE DATEDIFF(DAY, [t1].[WarehouseDeliveryMiladiDate], [t1].[SafekeepingReturnMiladiDate]) END AS [StayInWarehouseAfterRepair]
FROM [Rpr].[RepairRequest] AS [t1]
LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[CustomerId] = [t2].[Id]
LEFT JOIN [Inv].[Part] AS [t3] ON [t1].[ProductId] = [t3].[Id]';
    DECLARE @PCols NVARCHAR(MAX) = N'[{"TableName":"Rpr.RepairRequest","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"IdNumber","DisplayName":"شماره","Alliance":"t1_IdNumber","Address":"TRY_CAST([t1].[IdNumber] AS bigint)","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.Customer","ColumnName":"Code","DisplayName":"کد مشتری","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"Code","DisplayName":"کد تجهیز","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"Name","DisplayName":"عنوان تجهیز","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"IsGuarantee","DisplayName":"گارانتی","Alliance":"t1_IsGuarantee","Address":"[t1].[IsGuarantee]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"CustomerAddressManual","DisplayName":"سایت مشتری","Alliance":"t1_CustomerAddressManual","Address":"[t1].[CustomerAddressManual]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"Status","DisplayName":"وضعیت","Alliance":"t1_Status","Address":"[t1].[Status]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Rpr.Enums.RepairRequestStatusEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"PreCheckDelay","DisplayName":"تاخیر پیش‌بررسی","Alliance":"PreCheckDelay","Address":"CASE WHEN DATEDIFF(DAY, [t1].[RepairEntranceMiladiDate], [t1].[PreCheckMiladiDate]) > 2 THEN DATEDIFF(DAY, [t1].[RepairEntranceMiladiDate], [t1].[PreCheckMiladiDate]) ELSE 0 END","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"RepairDelay","DisplayName":"تاخیر تعمیر","Alliance":"RepairDelay","Address":"DATEDIFF(DAY, [t1].[AgreedMiladiDate], [t1].[WarehouseDeliveryMiladiDate])","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"SupplyDelay","DisplayName":"تاخیر تأمین","Alliance":"SupplyDelay","Address":"DATEDIFF(DAY, [t1].[BuyRequestMiladiDate], [t1].[SupplyMiladiDate])","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"TestDelay","DisplayName":"تاخیر تست","Alliance":"TestDelay","Address":"DATEDIFF(DAY, [t1].[QcRequestMiladiDate], [t1].[WarehouseDeliveryMiladiDate])","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"AgreedDelayInDay","DisplayName":"تاخیر توافقی","Alliance":"AgreedDelayInDay","Address":"CASE WHEN [t1].[Status] NOT IN (2093, 304, 2236) AND [t1].[AgreedMiladiDate] < GETDATE() THEN DATEDIFF(DAY, [t1].[AgreedMiladiDate], GETDATE()) ELSE NULL END","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"StayInWarehouseAfterRepair","DisplayName":"ماندگاری انبار","Alliance":"StayInWarehouseAfterRepair","Address":"CASE WHEN [t1].[SafekeepingReturnMiladiDate] IS NULL THEN DATEDIFF(DAY, [t1].[WarehouseDeliveryMiladiDate], GETDATE()) ELSE DATEDIFF(DAY, [t1].[WarehouseDeliveryMiladiDate], [t1].[SafekeepingReturnMiladiDate]) END","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @PAct NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @PBtn NVARCHAR(MAX) = N'[{"id":"as_edit_repairrequest_delay","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Rpr/RepairRequest/Edit?id='' + id, true, ''ویرایش'');\n}"}]';
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
    WHERE r.Name IN (N'ShowAllMenus', N'Rpr.Repairs', N'Rpr.RepairRequest.AfterSale', N'Rpr.RepairRequest.Repairs',
                     N'Rpr.RepairRequest.View', N'Rpr.RepairRequest.ShowAll');

    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'AfterSalesServiceSystem');
    IF @MenuId IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles SELECT DISTINCT LTRIM(RTRIM(j.value))
        FROM system.SystemMenu m CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND LTRIM(RTRIM(j.value)) <> N'';
        INSERT INTO #MenuRoles
        SELECT v.RoleName FROM (VALUES
            (N'Rpr.RepairRequest.AfterSale'),
            (N'Rpr.RepairRequest.Repairs'),
            (N'Rpr.RepairRequest.View'),
            (N'Rpr.RepairRequest.ShowAll'),
            (N'Rpr.RepairRequest.Imaging')
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
        SET AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
    END

    DECLARE @RrOnRowAdded NVARCHAR(MAX) = N'function(ctx) {
    var data = ctx.rowData || {};
    var $cell = ctx.$row.find(''td'').eq(0);
    var status = data.t1_Status;
    var color = null;
    if (status == 303) color = ''orange'';
    else if (status == 301 || status == 614 || status == 615) color = ''#6e85da'';
    else if (status == 305) color = ''lightgreen'';
    else if (status == 2092) color = ''yellow'';
    else if (status == 304) color = ''orangered'';
    else if (status == 2236) color = ''grey'';
    ctx.$row.css(''background-color'', '''');
    if (color) $cell.css(''background-color'', color);
}';
    DECLARE @RrEventScripts NVARCHAR(MAX) = (
        SELECT
            CAST(N'' AS nvarchar(max)) AS onSelectedRow,
            @RrOnRowAdded AS onRowAdded
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    UPDATE system.SavedQuery
    SET EventScriptsJson = @RrEventScripts,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (N'AfterSales_RepairRequest_List', N'AfterSales_RepairRequest_Delay');

    COMMIT;
    PRINT N'=== DONE Seed_AfterSales_RepairRequest_RolesAndDelay ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
