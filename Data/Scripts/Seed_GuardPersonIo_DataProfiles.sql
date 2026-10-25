/*
Seed_GuardPersonIo_DataProfiles.sql
نمایه داده ورود/خروج پرسنل/پیمانکار (HTS صفحه 427).
ستون‌ها همان گرید قدیم است. خروجی اکسل روشن است. دو دکمه دانلود تصویر ورود و خروج.
UTF-8 BOM. Idempotent.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_GuardPersonIo_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-guard-personio-dataprofiles';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @Name NVARCHAR(100) = N'Hrm_GuardPersonIo_List';
    DECLARE @Title NVARCHAR(200) = N'ورود/خروج پرسنل/پیمانکار';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.hrm.guardpersonio';
    DECLARE @Sel NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[OperatorType] AS [t1_OperatorType],
    LTRIM(RTRIM(ISNULL([t2].[Name],N'''')+'' ''+ISNULL([t2].[FamilyDisplay],N''''))) AS [t2_Person],
    [t3].[Title] AS [t3_Unit],
    [t4].[FullName] AS [t4_Contractor],
    [t1].[EntryShamsiDate] AS [t1_EntryShamsiDate],
    [t1].[EntryTime] AS [t1_EntryTime],
    [t1].[ExitShamsiDate] AS [t1_ExitShamsiDate],
    [t1].[ExitTime] AS [t1_ExitTime],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedByName] AS [t1_ModifiedByName],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t1].[Comment] AS [t1_Comment],
    [t1].[EntryFileId] AS [t1_EntryFileId],
    [t1].[ExitFileId] AS [t1_ExitFileId]
FROM [Hrm].[GuardPersonIo] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[Id] = [t1].[PersonId]
LEFT JOIN [Hrm].[OrgUnit] AS [t3] ON [t3].[Id] = [t1].[UnitId]
LEFT JOIN [Hrm].[GuardContractor] AS [t4] ON [t4].[Id] = [t1].[ContractorId]';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_OperatorType","DisplayName":"نوع","Alliance":"t1_OperatorType","Address":"[t1].[OperatorType]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Hrm.Enums.GuardOperatorTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hcm.Personel","ColumnName":"t2_Person","DisplayName":"پرسنل","Alliance":"t2_Person","Address":"LTRIM(RTRIM(ISNULL([t2].[Name],N'''')+'' ''+ISNULL([t2].[FamilyDisplay],N'''')))","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.OrgUnit","ColumnName":"t3_Unit","DisplayName":"واحد","Alliance":"t3_Unit","Address":"[t3].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardContractor","ColumnName":"t4_Contractor","DisplayName":"پیمانکار","Alliance":"t4_Contractor","Address":"[t4].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_EntryShamsiDate","DisplayName":"تاریخ ورود","Alliance":"t1_EntryShamsiDate","Address":"[t1].[EntryShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_EntryTime","DisplayName":"ساعت ورود","Alliance":"t1_EntryTime","Address":"[t1].[EntryTime]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_ExitShamsiDate","DisplayName":"تاریخ خروج","Alliance":"t1_ExitShamsiDate","Address":"[t1].[ExitShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_ExitTime","DisplayName":"ساعت خروج","Alliance":"t1_ExitTime","Address":"[t1].[ExitTime]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_CreatedByName","DisplayName":"کاربر ایجاد کننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_ModifiedByName","DisplayName":"کاربر ویرایش کننده","Alliance":"t1_ModifiedByName","Address":"[t1].[ModifiedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"تاریخ ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_EntryFileId","DisplayName":"تصویر ورود","Alliance":"t1_EntryFileId","Address":"[t1].[EntryFileId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.GuardPersonIo","ColumnName":"t1_ExitFileId","DisplayName":"تصویر خروج","Alliance":"t1_ExitFileId","Address":"[t1].[ExitFileId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[{"id":"guard_personio_download_entry","title":"تصویر ورود پرسنل/ پیمانکار","dataActionName":"downloadEntry","colorClass":"btn-color-info","iconClass":"ki-paper-clip ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = row.t1_EntryFileId;\n    if (id == null || id === '''' || id === 0 || id === ''0'') {\n        toastr.warning(''تصویر ورود پرسنل/ پیمانکار نمی‌تواند خالی باشد'');\n        return;\n    }\n    window.open(''/File/DownloadFile?id='' + id, ''_blank'');\n}"},{"id":"guard_personio_download_exit","title":"تصویر خروج پرسنل/ پیمانکار","dataActionName":"downloadExit","colorClass":"btn-color-info","iconClass":"ki-paper-clip ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = row.t1_ExitFileId;\n    if (id == null || id === '''' || id === 0 || id === ''0'') {\n        toastr.warning(''تصویر خروج پرسنل/ پیمانکار نمی‌تواند خالی باشد'');\n        return;\n    }\n    window.open(''/File/DownloadFile?id='' + id, ''_blank'');\n}"}]';
    DECLARE @Qj NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=N'{"onSelectedRow":"","onRowAdded":""}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@Name;
        PRINT N'  UPDATED ' + @Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, N'{"onSelectedRow":"","onRowAdded":""}',
             1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        PRINT N'  CREATED ' + @Name;
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_GuardPersonIo_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Hrm_GuardPersonIo_List';
SELECT q.Name, JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Hrm_GuardPersonIo_List'
ORDER BY CAST(j.[key] AS int);
