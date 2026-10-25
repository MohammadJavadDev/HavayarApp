/*
  Seed_RepairRequestManHour_ByRequest_DataProfile.sql
  نمایه «نفرساعت این درخواست» (AfterSales_RepairRequestManHour_ByRequest)
  برای جدول صفحهٔ نفرساعت درخواست تعمیر.

  صفحه: /Panel/Rpr/RepairRequestManHour/ListByParentId?repairRequestId=<id>
  موجودیت: Entities.App.Rpr.RepairRequestManHour

  - @RepairRequestId از data-query-params صفحه می‌آید (DECLARE نمی‌شود).
  - مجوز ردیف در SQL: admin یا نقشی که /panel/rpr/repairrequestmanhour/listbyparentid را در RoleAccess دارد.
  - RoleAccess نمایه: همهٔ نقش‌هایی که همین الان به صفحه دسترسی دارند.
  - نام نمایه یکتاست تا با AfterSales_RepairRequestManHour_List قاطی نشود.

  Apply: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_RepairRequestManHour_ByRequest_DataProfile.sql
  بعد از اجرا WebApp را ری‌استارت کنید (کش Redis نمایه‌ها).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-rr-manhour-by-request';

    DECLARE @Name NVARCHAR(200) = N'AfterSales_RepairRequestManHour_ByRequest';
    DECLARE @Title NVARCHAR(200) = N'نفرساعت این درخواست';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.rpr.repairrequestmanhour';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Rpr.RepairRequestManHour';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(CONVERT(NVARCHAR(30), @CurrentUserId) AS BIGINT);
DECLARE @ReqId BIGINT = TRY_CAST(CONVERT(NVARCHAR(30), @RepairRequestId) AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
SELECT @CuRoles = Roles FROM [system].[User] WHERE Id = @CuId;
DECLARE @CanRead BIT = CASE
    WHEN @ReqId IS NULL OR @CuId IS NULL THEN 0
    WHEN EXISTS (
        SELECT 1 FROM OPENJSON(@CuRoles)
        WHERE [value] = N''admin''
    ) THEN 1
    WHEN EXISTS (
        SELECT 1
        FROM OPENJSON(@CuRoles) AS [o]
        INNER JOIN [system].[Role] AS [r] ON [r].[Name] = [o].[value]
        INNER JOIN [system].[RoleAccess] AS [ra] ON [ra].[RoleId] = [r].[Id]
        WHERE [ra].[Path] = N''/panel/rpr/repairrequestmanhour/listbyparentid''
    ) THEN 1
    ELSE 0 END;
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    LTRIM(RTRIM(CONCAT(ISNULL([t2].[Code], N''''), N'' '', ISNULL([t2].[Name], N''''), N'' '', ISNULL([t2].[Family], N'''')))) AS [t2_ExpertName],
    [t1].[WorkShamsiDate] AS [t1_WorkShamsiDate],
    [t1].[StartTime] AS [t1_StartTime],
    [t1].[EndTime] AS [t1_EndTime],
    [t1].[ManHourPrice] AS [t1_ManHourPrice],
    [t1].[ManHourSalePrice] AS [t1_ManHourSalePrice],
    [t1].[Comment] AS [t1_Comment],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Rpr].[RepairRequestManHour] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[Id] = [t1].[ExpertId]
WHERE @CanRead = 1
  AND [t1].[RepairRequestId] = @ReqId';
    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.Parameters', JSON_QUERY(N'[{"Id":"repairRequestId","Name":"RepairRequestId","DisplayName":"شناسه درخواست تعمیر","DefaultValue":"","DataType":"string"}]'));

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName":"Rpr.RepairRequestManHour","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hcm.Personel","ColumnName":"Name","DisplayName":"کارشناس","Alliance":"t2_ExpertName","Address":"LTRIM(RTRIM(CONCAT(ISNULL([t2].[Code], N''''), N'' '', ISNULL([t2].[Name], N''''), N'' '', ISNULL([t2].[Family], N''''))))","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestManHour","ColumnName":"WorkShamsiDate","DisplayName":"تاریخ کار","Alliance":"t1_WorkShamsiDate","Address":"[t1].[WorkShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestManHour","ColumnName":"StartTime","DisplayName":"ساعت شروع","Alliance":"t1_StartTime","Address":"[t1].[StartTime]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestManHour","ColumnName":"EndTime","DisplayName":"ساعت پایان","Alliance":"t1_EndTime","Address":"[t1].[EndTime]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestManHour","ColumnName":"ManHourPrice","DisplayName":"نرخ نفرساعت","Alliance":"t1_ManHourPrice","Address":"[t1].[ManHourPrice]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestManHour","ColumnName":"ManHourSalePrice","DisplayName":"نرخ فروش نفرساعت","Alliance":"t1_ManHourSalePrice","Address":"[t1].[ManHourSalePrice]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestManHour","ColumnName":"Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestManHour","ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[{"id":"rr_mh_add","title":"افزودن","dataActionName":"rrManHourAdd","colorClass":"btn-color-primary","iconClass":"fa fa-plus-circle","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''rrManHourAction'', [''add'', ctx]); }"},{"id":"rr_mh_edit","title":"ویرایش","dataActionName":"rrManHourEdit","colorClass":"btn-color-warning","iconClass":"fa fa-pencil","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''rrManHourAction'', [''edit'', ctx]); }"},{"id":"rr_mh_delete","title":"حذف","dataActionName":"rrManHourDelete","colorClass":"btn-color-danger","iconClass":"fa fa-trash","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''rrManHourAction'', [''delete'', ctx]); }"}]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1
       OR ISJSON(@EventScripts) <> 1 OR ISJSON(@CustomButtons) <> 1
        THROW 51632, N'ColumnsJson / QueryJson / ActionOptions / EventScriptsJson / CustomActionButtonsJson معتبر نیست.', 1;

    DECLARE @ProfileId BIGINT;
    SELECT @ProfileId = Id FROM [system].[SavedQuery] WHERE Name = @Name;

    IF @ProfileId IS NOT NULL
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            EntityFullName = @EntityFullName,
            Mode = 1,
            [Type] = 1,
            ActionOptions = @ActionOptions,
            CustomActionButtonsJson = @CustomButtons,
            EventScriptsJson = @EventScripts,
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Id = @ProfileId;
        PRINT N'SavedQuery updated: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
        (
            Name, Title, QueryJson, ColumnsJson, DiagramJson,
            CustomActionButtonsJson, EventScriptsJson,
            Mode, [Type], EntityFullName, ActionOptions,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        VALUES
        (
            @Name, @Title, @QueryJson, @ColumnsJson, N'{}',
            @CustomButtons, @EventScripts,
            1, 1, @EntityFullName, @ActionOptions,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @ProfileId = SCOPE_IDENTITY();
        PRINT N'SavedQuery inserted: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.RowId = @ProfileId
      AND ra.ActionAccessType = 3
      AND ra.EntityName = @EntityPascal;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)), 3, 7, @EntityPascal, @Title, NULL, @ProfileId, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE EXISTS (
            SELECT 1 FROM [system].[RoleAccess] page_ra
            WHERE page_ra.RoleId = r.Id
              AND page_ra.Path = N'/panel/rpr/repairrequestmanhour/listbyparentid'
       );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT Id, Name, Title, EntityFullName, Mode, [Type]
FROM [system].[SavedQuery]
WHERE Name = N'AfterSales_RepairRequestManHour_ByRequest';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible,
       JSON_VALUE(j.value, '$.SortDirection') AS SortDirection
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'AfterSales_RepairRequestManHour_ByRequest';

SELECT JSON_VALUE(j.value, '$.title') AS ButtonTitle, JSON_VALUE(j.value, '$.dataActionName') AS DataAction
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.CustomActionButtonsJson) j
WHERE q.Name = N'AfterSales_RepairRequestManHour_ByRequest';

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.RowId
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType = 3
  AND ra.EntityName = N'Entities.App.Rpr.RepairRequestManHour'
  AND ra.RowId = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'AfterSales_RepairRequestManHour_ByRequest')
ORDER BY r.Name;
