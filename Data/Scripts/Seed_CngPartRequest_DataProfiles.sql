/*
Seed_CngPartRequest_DataProfiles.sql
نمایه داده درخواست کالا CNG (HTS صفحه 453). UTF-8 BOM. Idempotent.
بدون exportExcell. فیلتر ShowAll مثل StationInfo.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_CngPartRequest_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-partrequest-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @Name NVARCHAR(100) = N'Cng_PartRequest_List';
    DECLARE @Title NVARCHAR(200) = N'درخواست کالا';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.cng.partrequest';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Cng.PartRequest';
    DECLARE @Sel NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
SELECT @CuRoles = Roles FROM system.[User] WHERE Id = @CuId;
DECLARE @HasShowAll BIT = CASE
    WHEN EXISTS (
        SELECT 1 FROM OPENJSON(ISNULL(@CuRoles, N''[]''))
        WHERE [value] IN (N''admin'', N''ShowAllMenus'', N''Cng.PartRequest.Status'', N''Cng.PartRequest.ShowAll'')
    ) THEN 1 ELSE 0 END;
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[LastStatusId] AS [t1_LastStatusId],
    [t1].[InvoiceNumber] AS [t1_InvoiceNumber],
    [t1].[Receiver] AS [t1_Receiver],
    [t1].[SendingLocation] AS [t1_SendingLocation],
    [t1].[AttachmentFileName] AS [t1_AttachmentFileName],
    [t1].[RequestDescription] AS [t1_RequestDescription],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedByName] AS [t1_ModifiedByName],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t1].[Comment] AS [t1_Comment],
    [t1].[LastStatusComment] AS [t1_LastStatusComment]
FROM [Cng].[PartRequest] AS [t1]
WHERE @HasShowAll = 1 OR [t1].[CreatedById] = @CuId';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6,"Width":60},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Cng.Enums.PartRequestStatusEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_LastStatusId","DisplayName":"وضعیت","Alliance":"t1_LastStatusId","Address":"[t1].[LastStatusId]","SystemTypeName":"Select","SystemType":8,"Width":130},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_InvoiceNumber","DisplayName":"شماره پیش فاکتور","Alliance":"t1_InvoiceNumber","Address":"[t1].[InvoiceNumber]","SystemTypeName":"String","SystemType":0,"Width":130},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_Receiver","DisplayName":"گیرنده","Alliance":"t1_Receiver","Address":"[t1].[Receiver]","SystemTypeName":"String","SystemType":0,"Width":140},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_SendingLocation","DisplayName":"محل ارسال","Alliance":"t1_SendingLocation","Address":"[t1].[SendingLocation]","SystemTypeName":"String","SystemType":0,"Width":160},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_AttachmentFileName","DisplayName":"نام فایل پیوست","Alliance":"t1_AttachmentFileName","Address":"[t1].[AttachmentFileName]","SystemTypeName":"String","SystemType":0,"Width":160},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_RequestDescription","DisplayName":"شرح درخواست","Alliance":"t1_RequestDescription","Address":"[t1].[RequestDescription]","SystemTypeName":"String","SystemType":0,"Width":280},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_CreatedByName","DisplayName":"کاربر ایجادکننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SystemType":0,"Width":140},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SystemType":4,"Width":120},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_ModifiedByName","DisplayName":"کاربر ویرایش","Alliance":"t1_ModifiedByName","Address":"[t1].[ModifiedByName]","SystemTypeName":"String","SystemType":0,"Width":140},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"تاریخ ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SystemType":4,"Width":120},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_Comment","DisplayName":"کامنت","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SystemType":0,"Width":200},{"TableName":"Cng.PartRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_LastStatusComment","DisplayName":"کامنت وضعیت","Alliance":"t1_LastStatusComment","Address":"[t1].[LastStatusComment]","SystemTypeName":"String","SystemType":0,"Width":200}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Pid BIGINT;

    SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
    IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51620, N'JSON نمایه Cng_PartRequest_List معتبر نیست.', 1;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@EventScripts, DiagramJson=N'{}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@Name;
        SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
        PRINT N'  UPDATED ' + @Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @EventScripts,
             1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
        PRINT N'  CREATED ' + @Name;
    END

    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType=3 AND ra.RowId=@Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (
        N'ShowAllMenus', N'مشاهده کلی HTS',
        N'Cng.PartRequest.Manage', N'Cng.PartRequest.Status', N'Cng.PartRequest.ShowAll'
    );
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_CngPartRequest_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Cng_PartRequest_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Cng_PartRequest_List'
ORDER BY CAST(j.[key] AS int);
