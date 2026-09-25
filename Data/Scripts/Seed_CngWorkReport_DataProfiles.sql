/*
Seed_CngWorkReport_DataProfiles.sql
نمایه داده گزارش کار CNG (HTS صفحه 435). UTF-8 BOM. Idempotent.
فیلتر ShowAll: Expert / ShowAll / ShowAllMenus / admin؛ Agency فقط رکوردهای خودش.
دکمه‌ها: جدید، ویرایش، حذف، اکسل، دانلود فایل.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_CngWorkReport_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-workreport-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @Name NVARCHAR(100) = N'Cng_WorkReport_List';
    DECLARE @Title NVARCHAR(200) = N'گزارش کار';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.cng.workreport';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Cng.WorkReport';

    DECLARE @Sel NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
SELECT @CuRoles = Roles FROM system.[User] WHERE Id = @CuId;
DECLARE @HasShowAll BIT = CASE
    WHEN EXISTS (
        SELECT 1 FROM OPENJSON(ISNULL(@CuRoles, N''[]''))
        WHERE [value] IN (N''admin'', N''ShowAllMenus'', N''Cng.WorkReport.Expert'', N''Cng.WorkReport.ShowAll'')
    ) THEN 1 ELSE 0 END;
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CAST(CASE WHEN [t1].[FileId] IS NOT NULL OR NULLIF(LTRIM(RTRIM([t1].[WorkReportFileAddress])), N'''') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [t1_HasFile],
    [t1].[IsSurveyLinkSended] AS [t1_IsSurveyLinkSended],
    [t4].[FullName] AS [t4_FullName],
    [t2].[StationTitle] AS [t2_StationTitle],
    [t2].[EquipmentType] AS [t2_EquipmentType],
    [t1].[RequestTypeId] AS [t1_RequestTypeId],
    [t1].[ServiceTypeId] AS [t1_ServiceTypeId],
    [t1].[ReportNumber] AS [t1_ReportNumber],
    [t1].[ReportShamsiDate] AS [t1_ReportShamsiDate],
    [t1].[WorkingHours] AS [t1_WorkingHours],
    [t1].[CossTetaValue] AS [t1_CossTetaValue],
    [t1].[ConsumedOilTypeId] AS [t1_ConsumedOilTypeId],
    [t1].[IsApprovedByOwner] AS [t1_IsApprovedByOwner],
    [t1].[OwnerComment] AS [t1_OwnerComment],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedByName] AS [t1_ModifiedByName],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t1].[FailureExpertComment] AS [t1_FailureExpertComment]
FROM [Cng].[WorkReport] AS [t1]
LEFT JOIN [Cng].[StationInfo] AS [t2] ON [t2].[Id] = [t1].[StationInfoId]
LEFT JOIN [SLS].[Customer] AS [t3] ON [t3].[Id] = [t2].[CustomerId]
LEFT JOIN [Gnr].[Party] AS [t4] ON [t4].[Id] = [t3].[PartyId]
WHERE @HasShowAll = 1 OR [t1].[CreatedById] = @CuId';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":70,"ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6},{"TableName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data,type,row){return (data===true||data===1||data===''true''||data===''1'')?''<i class=\"ki-outline ki-check text-success\"></i>'':'''';}","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":80,"ColumnName":"t1_HasFile","DisplayName":"فایل","Alliance":"t1_HasFile","Address":null,"SystemTypeName":"Boolean","SystemType":1},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data){return (data===true||data===1||data===''true'')?''<i class=\"ki-outline ki-check text-success\"></i>'':'''';}","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":110,"ColumnName":"t1_IsSurveyLinkSended","DisplayName":"لینک ارسال شده","Alliance":"t1_IsSurveyLinkSended","Address":"[t1].[IsSurveyLinkSended]","SystemTypeName":"Boolean","SystemType":1},{"TableName":"Gnr.Party","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":220,"ColumnName":"t4_FullName","DisplayName":"مشتری","Alliance":"t4_FullName","Address":"[t4].[FullName]","SystemTypeName":"String","SystemType":0},{"TableName":"Cng.StationInfo","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":160,"ColumnName":"t2_StationTitle","DisplayName":"جایگاه","Alliance":"t2_StationTitle","Address":"[t2].[StationTitle]","SystemTypeName":"String","SystemType":0},{"TableName":"Cng.StationInfo","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Cng.Enums.StationEquipmentTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":110,"ColumnName":"t2_EquipmentType","DisplayName":"نوع تجهیز","Alliance":"t2_EquipmentType","Address":"[t2].[EquipmentType]","SystemTypeName":"Select","SystemType":8},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Cng.Enums.WorkReportRequestTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":180,"ColumnName":"t1_RequestTypeId","DisplayName":"نوع درخواست","Alliance":"t1_RequestTypeId","Address":"[t1].[RequestTypeId]","SystemTypeName":"Select","SystemType":8},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Cng.Enums.WorkReportServiceTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":110,"ColumnName":"t1_ServiceTypeId","DisplayName":"نوع خدمات","Alliance":"t1_ServiceTypeId","Address":"[t1].[ServiceTypeId]","SystemTypeName":"Select","SystemType":8},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_ReportNumber","DisplayName":"شماره گزارش","Alliance":"t1_ReportNumber","Address":"[t1].[ReportNumber]","SystemTypeName":"String","SystemType":0},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_ReportShamsiDate","DisplayName":"تاریخ گزارش","Alliance":"t1_ReportShamsiDate","Address":"[t1].[ReportShamsiDate]","SystemTypeName":"DateShamsi","SystemType":2},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_WorkingHours","DisplayName":"ساعت کارکرد","Alliance":"t1_WorkingHours","Address":"[t1].[WorkingHours]","SystemTypeName":"Int","SystemType":7},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":110,"ColumnName":"t1_CossTetaValue","DisplayName":"مقدار Coss تتا","Alliance":"t1_CossTetaValue","Address":"[t1].[CossTetaValue]","SystemTypeName":"String","SystemType":0},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Cng.Enums.WorkReportConsumedOilTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_ConsumedOilTypeId","DisplayName":"نوع روغن مصرفی","Alliance":"t1_ConsumedOilTypeId","Address":"[t1].[ConsumedOilTypeId]","SystemTypeName":"Select","SystemType":8},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data){return (data===true||data===1||data===''true'')?''<i class=\"ki-outline ki-check text-success\"></i>'':'''';}","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_IsApprovedByOwner","DisplayName":"تایید جایگاه دار","Alliance":"t1_IsApprovedByOwner","Address":"[t1].[IsApprovedByOwner]","SystemTypeName":"Boolean","SystemType":1},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":150,"ColumnName":"t1_OwnerComment","DisplayName":"توضیحات مالک","Alliance":"t1_OwnerComment","Address":"[t1].[OwnerComment]","SystemTypeName":"String","SystemType":0},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SystemType":4},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":140,"ColumnName":"t1_ModifiedByName","DisplayName":"کاربر ویرایش","Alliance":"t1_ModifiedByName","Address":"[t1].[ModifiedByName]","SystemTypeName":"String","SystemType":0},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"تاریخ ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SystemType":4},{"TableName":"Cng.WorkReport","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":280,"ColumnName":"t1_FailureExpertComment","DisplayName":"کامنت کارشناس فنی","Alliance":"t1_FailureExpertComment","Address":"[t1].[FailureExpertComment]","SystemTypeName":"String","SystemType":0}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[{"id":"cng_workreport_download","title":"دانلود فایل گزارش کار","dataActionName":"downloadFile","colorClass":"btn-color-info","iconClass":"ki-outline ki-file-down","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var row = (ctx && ctx.selectedRow) || {};\n    var has = row.t1_HasFile === true || row.t1_HasFile === 1 || row.t1_HasFile === ''true'' || row.t1_HasFile === ''1'';\n    if (!has) { toastr.warning(''این ردیف فایل ندارد''); return; }\n    window.open(''/Panel/Cng/WorkReport/Download?id='' + id, ''_blank'');\n}"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Pid BIGINT;

    SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
    IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51630, N'JSON نمایه Cng_WorkReport_List معتبر نیست.', 1;

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
        N'Cng.WorkReport.Agency', N'Cng.WorkReport.Expert', N'Cng.WorkReport.ShowAll'
    );
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_CngWorkReport_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Cng_WorkReport_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Cng_WorkReport_List'
ORDER BY CAST(j.[key] AS int);
