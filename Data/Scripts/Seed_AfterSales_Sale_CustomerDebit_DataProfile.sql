/*
  گزارش مانده حساب مشتری — HTS صفحه 310 / Sale_DebtBalanceReport.
  نمایه جمع و ریز حساب روی کاتالوگ گزارشات (EntityFullName = AfterSalesReports).
  داده از راهکاران (ERPS.Erps.dbo.Vw_Fin_AllVoucherItems)، معین 13014.
  از تاریخ پیش‌فرض 1395/01/01. سال‌های 1396 تا سال جاری فقط فهرست تفصیل را محدود می‌کنند.
  مبلغ جمع از ردیف‌های شعبه 1 در همان بازه تاریخ است.
  UTF-8 BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now datetime2 = GETDATE();
    DECLARE @NowShamsi nvarchar(30) = CONVERT(nvarchar(30), @Now, 120);
    DECLARE @SeedUser nvarchar(150) = N'seed-aftersales-customer-debit';
    DECLARE @EntityName nvarchar(200) = N'AfterSalesReports';
    DECLARE @PagePath nvarchar(300) = N'/panel/sale/aftersalesreport/list';
    DECLARE @Actions nvarchar(max) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    IF OBJECT_ID('tempdb..#HtsGroup') IS NOT NULL DROP TABLE #HtsGroup;
    CREATE TABLE #HtsGroup (HtsGroupId int NOT NULL PRIMARY KEY, Title nvarchar(200) NOT NULL, RoleId bigint NULL, RoleName nvarchar(200) NULL);
    INSERT INTO #HtsGroup (HtsGroupId, Title) VALUES
        (165, N'گزارش مانده حساب مشتری'),
        (350, N'مشاهده کلی HTS');

    UPDATE h SET RoleId = r.Id, RoleName = r.Name
    FROM #HtsGroup h INNER JOIN system.Role r ON r.Name = h.Title;

    DECLARE @Gid int, @Title nvarchar(200), @WantedId bigint, @RoleName nvarchar(200), @RoleId bigint;
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
    CREATE TABLE #UserRoleMap (Username nvarchar(200) NOT NULL, RoleName nvarchar(200) NOT NULL, RoleId bigint NOT NULL);

    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), h.RoleName, h.RoleId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    INNER JOIN #HtsGroup h ON h.HtsGroupId = m.UserGroup_FK
    WHERE u.IsActive = 1 AND h.RoleId IS NOT NULL
      AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

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

    DECLARE @ProfileName nvarchar(100) = N'AfterSales_Sale_CustomerDebit';
    DECLARE @ProfileTitle nvarchar(200) = N'گزارش مانده حساب مشتری';
    DECLARE @Sql nvarchar(max) = N'DECLARE @FromRaw nvarchar(30) = NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(30), @FromDate))), N'''');
DECLARE @ToRaw nvarchar(30) = NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(30), @ToDate))), N'''');
SET @FromRaw = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@FromRaw, N''-'', N''/''), N''۰'', N''0''), N''۱'', N''1''), N''۲'', N''2''), N''۳'', N''3''), N''۴'', N''4''), N''۵'', N''5''), N''۶'', N''6''), N''۷'', N''7''), N''۸'', N''8''), N''۹'', N''9''), N''٠'', N''0''), N''١'', N''1''), N''٢'', N''2''), N''٣'', N''3''), N''٤'', N''4''), N''٥'', N''5''), N''٦'', N''6''), N''٧'', N''7''), N''٨'', N''8''), N''٩'', N''9'');
SET @ToRaw = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@ToRaw, N''-'', N''/''), N''۰'', N''0''), N''۱'', N''1''), N''۲'', N''2''), N''۳'', N''3''), N''۴'', N''4''), N''۵'', N''5''), N''۶'', N''6''), N''۷'', N''7''), N''۸'', N''8''), N''۹'', N''9''), N''٠'', N''0''), N''١'', N''1''), N''٢'', N''2''), N''٣'', N''3''), N''٤'', N''4''), N''٥'', N''5''), N''٦'', N''6''), N''٧'', N''7''), N''٨'', N''8''), N''٩'', N''9'');
IF @FromRaw IS NOT NULL AND LEN(@FromRaw) > 10 SET @FromRaw = LEFT(@FromRaw, 10);
IF @ToRaw IS NOT NULL AND LEN(@ToRaw) > 10 SET @ToRaw = LEFT(@ToRaw, 10);
DECLARE @From date = CASE WHEN @FromRaw IS NULL THEN CAST(''2016-03-20'' AS date) ELSE CAST(dbo.fn_ShamsiToMiladiDate(@FromRaw, N''/'') AS date) END;
DECLARE @To date = CASE WHEN @ToRaw IS NULL THEN NULL ELSE CAST(dbo.fn_ShamsiToMiladiDate(@ToRaw, N''/'') AS date) END;
DECLARE @TodayShamsi nvarchar(10) = dbo.fn_MiladiToShamsi(GETDATE());
DECLARE @Py int = TRY_CONVERT(int, LEFT(@TodayShamsi, 4));
IF @Py IS NULL OR @Py < 1396 SET @Py = 1396;
IF OBJECT_ID(''tempdb..#HamkaranYears'') IS NOT NULL DROP TABLE #HamkaranYears;
CREATE TABLE #HamkaranYears (Y int NOT NULL PRIMARY KEY);
;WITH n AS (
    SELECT 1396 AS PersianYear
    UNION ALL
    SELECT PersianYear + 1 FROM n WHERE PersianYear < @Py
)
INSERT INTO #HamkaranYears (Y)
SELECT DISTINCT CASE WHEN PersianYear <= 1399 THEN PersianYear % 100 ELSE (PersianYear - 1399) + 99 END
FROM n
OPTION (MAXRECURSION 40);
--!--mainsection
SELECT
    CONCAT(
        h.Acc_DLCode COLLATE DATABASE_DEFAULT,
        N''|'' COLLATE DATABASE_DEFAULT,
        ISNULL(CONVERT(nvarchar(20), h.Customer_Code), N'''') COLLATE DATABASE_DEFAULT,
        N''|'' COLLATE DATABASE_DEFAULT,
        ISNULL(h.Province_Title, N'''') COLLATE DATABASE_DEFAULT
    ) AS [tRowKey],
    h.Acc_DLCode AS [tDl_Code],
    h.Customer_Code AS [tCustomer_Code],
    h.Province_Title AS [tProvince_Title],
    h.Acc_DLTitle AS [tDl_Title],
    CAST(ISNULL(d.Debit, 0) AS decimal(18, 2)) AS [tDebit],
    CAST(ISNULL(d.Credit, 0) AS decimal(18, 2)) AS [tCredit],
    CAST(ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0) AS decimal(18, 2)) AS [tRemain],
    CAST(CASE WHEN ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0) > 0 THEN ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0) ELSE 0 END AS decimal(18, 2)) AS [tRemainDebit],
    CAST(CASE WHEN ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0) < 0 THEN ABS(ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0)) ELSE 0 END AS decimal(18, 2)) AS [tRemainCredit]
FROM (
    SELECT
        DL.Code AS Acc_DLCode,
        tc.Customer_Code,
        tc.Province_Title,
        DL.Title AS Acc_DLTitle
    FROM ERPS.Erps.dbo.Vw_Fin_AllVoucherItems AS Itm WITH (NOLOCK)
    INNER JOIN ERPS.Erps.FIN3.DL AS DL ON Itm.dlRef = DL.Code
    LEFT JOIN ERPS.Erps.GNR3.Party AS crs ON crs.PartyID = DL.ReferenceID AND DL.DLTypeRef IN (1, 2)
    LEFT JOIN ERPS.Erps.SLS3.Customer AS c ON c.PartyRef = crs.PartyID
    LEFT JOIN TMS.TotalSystem.dbo.Vw_Customer AS tc
        ON CAST(tc.Customer_Code AS nvarchar(20)) COLLATE Persian_100_CI_AI = CAST(c.Number AS nvarchar(20)) COLLATE Persian_100_CI_AI
    WHERE (CASE WHEN Itm.Ctgry = 9 THEN 1 ELSE Itm.State END <> 0)
AND Itm.SLRef = 13014
AND Itm.Ctgry NOT IN (3, 5, 2, 8, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 151, 9)
AND (@From IS NULL OR Itm.VchDate >= @From)
AND (@To IS NULL OR Itm.VchDate < DATEADD(day, 1, @To))
      AND DL.Code <> N''00394''
      AND Itm.[Year] IN (SELECT Y FROM #HamkaranYears)
    GROUP BY DL.Code, DL.Title, tc.Customer_Code, tc.Province_Title
) AS h
LEFT JOIN (
    SELECT
        Itm.dlRef AS Acc_DLCode,
        SUM(Itm.Debit) AS Debit,
        SUM(Itm.Credit) AS Credit
    FROM ERPS.Erps.dbo.Vw_Fin_AllVoucherItems AS Itm WITH (NOLOCK)
    WHERE (CASE WHEN Itm.Ctgry = 9 THEN 1 ELSE Itm.State END <> 0)
AND Itm.SLRef = 13014
AND Itm.Ctgry NOT IN (3, 5, 2, 8, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 151, 9)
AND (@From IS NULL OR Itm.VchDate >= @From)
AND (@To IS NULL OR Itm.VchDate < DATEADD(day, 1, @To))
      AND Itm.Branch_Code = 1
    GROUP BY Itm.dlRef
) AS d ON d.Acc_DLCode = h.Acc_DLCode';
    DECLARE @Columns nvarchar(max) = N'[{"TableName":"","ColumnName":"tRowKey","DisplayName":"شناسه","Alliance":"tRowKey","Address":"CONCAT(h.Acc_DLCode COLLATE DATABASE_DEFAULT, N''|'' COLLATE DATABASE_DEFAULT, ISNULL(CONVERT(nvarchar(20), h.Customer_Code), N'''') COLLATE DATABASE_DEFAULT, N''|'' COLLATE DATABASE_DEFAULT, ISNULL(h.Province_Title, N'''') COLLATE DATABASE_DEFAULT)","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tDl_Code","DisplayName":"کد تفصیل","Alliance":"tDl_Code","Address":"h.Acc_DLCode","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":0,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"TMS.TotalSystem.dbo.Vw_Customer","ColumnName":"tCustomer_Code","DisplayName":"کد مشتری","Alliance":"tCustomer_Code","Address":"h.Customer_Code","SystemTypeName":"Int","SystemType":7,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"TMS.TotalSystem.dbo.Vw_Customer","ColumnName":"tProvince_Title","DisplayName":"استان","Alliance":"tProvince_Title","Address":"h.Province_Title","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.FIN3.DL","ColumnName":"tDl_Title","DisplayName":"تفصیل","Alliance":"tDl_Title","Address":"h.Acc_DLTitle","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tDebit","DisplayName":"بدهکار","Alliance":"tDebit","Address":"CAST(ISNULL(d.Debit, 0) AS decimal(18, 2))","SystemTypeName":"Decimal","SystemType":14,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tCredit","DisplayName":"بستانکار","Alliance":"tCredit","Address":"CAST(ISNULL(d.Credit, 0) AS decimal(18, 2))","SystemTypeName":"Decimal","SystemType":14,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tRemain","DisplayName":"مانده","Alliance":"tRemain","Address":"CAST(ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0) AS decimal(18, 2))","SystemTypeName":"Decimal","SystemType":14,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tRemainDebit","DisplayName":"مانده بدهکار","Alliance":"tRemainDebit","Address":"CAST(CASE WHEN ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0) > 0 THEN ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0) ELSE 0 END AS decimal(18, 2))","SystemTypeName":"Decimal","SystemType":14,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tRemainCredit","DisplayName":"مانده بستانکار","Alliance":"tRemainCredit","Address":"CAST(CASE WHEN ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0) < 0 THEN ABS(ISNULL(d.Debit, 0) - ISNULL(d.Credit, 0)) ELSE 0 END AS decimal(18, 2))","SystemTypeName":"Decimal","SystemType":14,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @Params nvarchar(max) = N'[{"Id":"fromDate","Name":"FromDate","DisplayName":"از تاریخ","DefaultValue":"1395/01/01","DataType":"string"},{"Id":"toDate","Name":"ToDate","DisplayName":"تا تاریخ","DefaultValue":"","DataType":"string"}]';
    DECLARE @Buttons nvarchar(max) = N'[{"id":"customerDebitDetail","title":"ریز حساب","dataActionName":"customerDebitDetail","colorClass":"btn-color-primary","iconClass":"ki-menu ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx){ ctx.$btn.trigger(''customerDebitDetail'', [ctx]); }"}]';
    DECLARE @QueryJson nvarchar(max) =
        N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"'
        + REPLACE(REPLACE(REPLACE(@Sql, NCHAR(13), N''), NCHAR(10), N' '), N'"', N'\"')
        + N'","Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.Parameters', JSON_QUERY(@Params));
    IF ISJSON(@QueryJson) <> 1 OR ISJSON(@Columns) <> 1 OR ISJSON(@Buttons) <> 1
        THROW 51640, N'JSON نمایه مانده حساب مشتری معتبر نیست.', 1;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
        UPDATE system.SavedQuery
        SET Title = @ProfileTitle, EntityFullName = @EntityName, Mode = 1, [Type] = 1,
            QueryJson = @QueryJson, ColumnsJson = @Columns, ActionOptions = @Actions,
            CustomActionButtonsJson = @Buttons, EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}', DiagramJson = N'{}',
            IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @ProfileName;
    ELSE
        INSERT INTO system.SavedQuery
            (Name, Title, EntityFullName, Mode, [Type], QueryJson, ColumnsJson, ActionOptions, CustomActionButtonsJson, EventScriptsJson, DiagramJson,
             CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@ProfileName, @ProfileTitle, @EntityName, 1, 1, @QueryJson, @Columns, @Actions, @Buttons, N'{"onSelectedRow":"","onRowAdded":""}', N'{}',
             1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

    DECLARE @ProfileId bigint = (SELECT Id FROM system.SavedQuery WHERE Name = @ProfileName);
    DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @ProfileId;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)), 3, 7, @EntityName, @ProfileTitle, NULL, @ProfileId, h.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #HtsGroup h WHERE h.RoleId IS NOT NULL;

    SET @ProfileName = N'AfterSales_Sale_CustomerDebit_Detail';
    SET @ProfileTitle = N'گزارش ریز مانده حساب مشتری';
    SET @Sql = N'DECLARE @FromRaw nvarchar(30) = NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(30), @FromDate))), N'''');
DECLARE @ToRaw nvarchar(30) = NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(30), @ToDate))), N'''');
SET @FromRaw = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@FromRaw, N''-'', N''/''), N''۰'', N''0''), N''۱'', N''1''), N''۲'', N''2''), N''۳'', N''3''), N''۴'', N''4''), N''۵'', N''5''), N''۶'', N''6''), N''۷'', N''7''), N''۸'', N''8''), N''۹'', N''9''), N''٠'', N''0''), N''١'', N''1''), N''٢'', N''2''), N''٣'', N''3''), N''٤'', N''4''), N''٥'', N''5''), N''٦'', N''6''), N''٧'', N''7''), N''٨'', N''8''), N''٩'', N''9'');
SET @ToRaw = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@ToRaw, N''-'', N''/''), N''۰'', N''0''), N''۱'', N''1''), N''۲'', N''2''), N''۳'', N''3''), N''۴'', N''4''), N''۵'', N''5''), N''۶'', N''6''), N''۷'', N''7''), N''۸'', N''8''), N''۹'', N''9''), N''٠'', N''0''), N''١'', N''1''), N''٢'', N''2''), N''٣'', N''3''), N''٤'', N''4''), N''٥'', N''5''), N''٦'', N''6''), N''٧'', N''7''), N''٨'', N''8''), N''٩'', N''9'');
IF @FromRaw IS NOT NULL AND LEN(@FromRaw) > 10 SET @FromRaw = LEFT(@FromRaw, 10);
IF @ToRaw IS NOT NULL AND LEN(@ToRaw) > 10 SET @ToRaw = LEFT(@ToRaw, 10);
DECLARE @From date = CASE WHEN @FromRaw IS NULL THEN CAST(''2016-03-20'' AS date) ELSE CAST(dbo.fn_ShamsiToMiladiDate(@FromRaw, N''/'') AS date) END;
DECLARE @To date = CASE WHEN @ToRaw IS NULL THEN NULL ELSE CAST(dbo.fn_ShamsiToMiladiDate(@ToRaw, N''/'') AS date) END;
DECLARE @TodayShamsi nvarchar(10) = dbo.fn_MiladiToShamsi(GETDATE());
DECLARE @Py int = TRY_CONVERT(int, LEFT(@TodayShamsi, 4));
IF @Py IS NULL OR @Py < 1396 SET @Py = 1396;
IF OBJECT_ID(''tempdb..#HamkaranYears'') IS NOT NULL DROP TABLE #HamkaranYears;
CREATE TABLE #HamkaranYears (Y int NOT NULL PRIMARY KEY);
;WITH n AS (
    SELECT 1396 AS PersianYear
    UNION ALL
    SELECT PersianYear + 1 FROM n WHERE PersianYear < @Py
)
INSERT INTO #HamkaranYears (Y)
SELECT DISTINCT CASE WHEN PersianYear <= 1399 THEN PersianYear % 100 ELSE (PersianYear - 1399) + 99 END
FROM n
OPTION (MAXRECURSION 40);
DECLARE @Dl nvarchar(50) = NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @DlCode))), N'''');
--!--mainsection
SELECT
    Itm.VchItmId AS [tVchItmId],
    Itm.dlRef AS [tDl_Code],
    dbo.fn_MiladiToShamsi(Itm.VchDate) AS [tVchDate_Shamsi],
    Itm.TempNum AS [tTempNum],
    Itm.LocalNum AS [tLocalNum],
    CAST(Itm.Debit AS decimal(18, 2)) AS [tDebit],
    CAST(Itm.Credit AS decimal(18, 2)) AS [tCredit],
    Itm.Descr AS [tDescription]
FROM ERPS.Erps.dbo.Vw_Fin_AllVoucherItems AS Itm WITH (NOLOCK)
WHERE (CASE WHEN Itm.Ctgry = 9 THEN 1 ELSE Itm.State END <> 0)
AND Itm.SLRef = 13014
AND Itm.Ctgry NOT IN (3, 5, 2, 8, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 151, 9)
AND (@From IS NULL OR Itm.VchDate >= @From)
AND (@To IS NULL OR Itm.VchDate < DATEADD(day, 1, @To))
  AND Itm.Branch_Code = 1
  AND Itm.dlRef COLLATE DATABASE_DEFAULT <> N''00394''
  AND @Dl IS NOT NULL
  AND Itm.dlRef COLLATE DATABASE_DEFAULT = @Dl';
    SET @Columns = N'[{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tVchItmId","DisplayName":"شناسه","Alliance":"tVchItmId","Address":"Itm.VchItmId","SystemTypeName":"Long","SystemType":6,"SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tDl_Code","DisplayName":"کد تفصیل","Alliance":"tDl_Code","Address":"Itm.dlRef","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tVchDate_Shamsi","DisplayName":"تاریخ","Alliance":"tVchDate_Shamsi","Address":"dbo.fn_MiladiToShamsi(Itm.VchDate)","SystemTypeName":"DateShamsi","SystemType":5,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":0,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tTempNum","DisplayName":"شماره موقت","Alliance":"tTempNum","Address":"Itm.TempNum","SystemTypeName":"Int","SystemType":7,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tLocalNum","DisplayName":"شماره دائم","Alliance":"tLocalNum","Address":"Itm.LocalNum","SystemTypeName":"Int","SystemType":7,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tDebit","DisplayName":"بدهکار","Alliance":"tDebit","Address":"CAST(Itm.Debit AS decimal(18, 2))","SystemTypeName":"Decimal","SystemType":14,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tCredit","DisplayName":"بستانکار","Alliance":"tCredit","Address":"CAST(Itm.Credit AS decimal(18, 2))","SystemTypeName":"Decimal","SystemType":14,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"ERPS.Erps.dbo.Vw_Fin_AllVoucherItems","ColumnName":"tDescription","DisplayName":"شرح","Alliance":"tDescription","Address":"Itm.Descr","SystemTypeName":"String","SystemType":0,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":360,"Filterable":true,"Sortable":true,"ClassName":""}]';
    SET @Params = N'[{"Id":"fromDate","Name":"FromDate","DisplayName":"از تاریخ","DefaultValue":"1395/01/01","DataType":"string"},{"Id":"toDate","Name":"ToDate","DisplayName":"تا تاریخ","DefaultValue":"","DataType":"string"},{"Id":"dlCode","Name":"DlCode","DisplayName":"کد تفصیل","DefaultValue":"","DataType":"string"}]';
    SET @Buttons = N'[]';
    SET @QueryJson =
        N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"'
        + REPLACE(REPLACE(REPLACE(@Sql, NCHAR(13), N''), NCHAR(10), N' '), N'"', N'\"')
        + N'","Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.Parameters', JSON_QUERY(@Params));
    IF ISJSON(@QueryJson) <> 1 OR ISJSON(@Columns) <> 1 OR ISJSON(@Buttons) <> 1
        THROW 51640, N'JSON نمایه مانده حساب مشتری معتبر نیست.', 1;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
        UPDATE system.SavedQuery
        SET Title = @ProfileTitle, EntityFullName = @EntityName, Mode = 1, [Type] = 1,
            QueryJson = @QueryJson, ColumnsJson = @Columns, ActionOptions = @Actions,
            CustomActionButtonsJson = @Buttons, EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}', DiagramJson = N'{}',
            IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @ProfileName;
    ELSE
        INSERT INTO system.SavedQuery
            (Name, Title, EntityFullName, Mode, [Type], QueryJson, ColumnsJson, ActionOptions, CustomActionButtonsJson, EventScriptsJson, DiagramJson,
             CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@ProfileName, @ProfileTitle, @EntityName, 1, 1, @QueryJson, @Columns, @Actions, @Buttons, N'{"onSelectedRow":"","onRowAdded":""}', N'{}',
             1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

    SET @ProfileId = (SELECT Id FROM system.SavedQuery WHERE Name = @ProfileName);
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
FROM system.SavedQuery q
WHERE q.Name IN (N'AfterSales_Sale_CustomerDebit', N'AfterSales_Sale_CustomerDebit_Detail');

SELECT q.Name, JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name IN (N'AfterSales_Sale_CustomerDebit', N'AfterSales_Sale_CustomerDebit_Detail')
ORDER BY q.Name, CONVERT(int, j.[key]);

SELECT r.Name, q.Name AS ProfileName
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
WHERE q.Name IN (N'AfterSales_Sale_CustomerDebit', N'AfterSales_Sale_CustomerDebit_Detail') AND ra.ActionAccessType = 3;
