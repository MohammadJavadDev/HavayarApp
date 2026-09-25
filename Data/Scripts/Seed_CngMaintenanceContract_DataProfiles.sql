/*
Seed_CngMaintenanceContract_DataProfiles.sql
نمایه داده قرارداد تعمیر و نگهداشت CNG (HTS صفحه 484). UTF-8 BOM. Idempotent.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_CngMaintenanceContract_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-maintenancecontract-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @Name NVARCHAR(100) = N'Cng_MaintenanceContract_List';
    DECLARE @Title NVARCHAR(200) = N'قرارداد تعمیر و نگهداشت';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.cng.maintenancecontract';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Cng.MaintenanceContract';
    DECLARE @Sel NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t3].[FullName] AS [t3_Agency],
    [t4].[StationTitle] AS [t4_Station],
    [t6].[FullName] AS [t6_StationBeneficiary],
    [t1].[StartDateInText] AS [t1_StartDateInText],
    [t1].[ContractDurationInMonth] AS [t1_ContractDurationInMonth],
    [t1].[EndDateInText] AS [t1_EndDateInText],
    [t1].[VisitCountInMonth] AS [t1_VisitCountInMonth],
    [t1].[MonthlyPrice] AS [t1_MonthlyPrice],
    [t1].[TotalPrice] AS [t1_TotalPrice],
    [t1].[Status] AS [t1_Status],
    [t1].[IsCanceled] AS [t1_IsCanceled],
    [t1].[CancelDescription] AS [t1_CancelDescription],
    [t1].[Comment] AS [t1_Comment],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedByName] AS [t1_ModifiedByName],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime]
FROM [Cng].[MaintenanceContract] AS [t1]
LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[AgencyId] = [t2].[Id]
LEFT JOIN [Gnr].[Party] AS [t3] ON [t2].[PartyId] = [t3].[Id]
LEFT JOIN [Cng].[StationInfo] AS [t4] ON [t1].[StationId] = [t4].[Id]
LEFT JOIN [SLS].[Customer] AS [t5] ON [t1].[StationBeneficiaryId] = [t5].[Id]
LEFT JOIN [Gnr].[Party] AS [t6] ON [t5].[PartyId] = [t6].[Id]';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"SelectIndex":0,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":true,"Visible":false,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6,"Width":80},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Gnr.Party","ColumnName":"t3_Agency","DisplayName":"نمایندگی","Alliance":"t3_Agency","Address":"[t3].[FullName]","SystemTypeName":"String","SystemType":0,"Width":200},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.StationInfo","ColumnName":"t4_Station","DisplayName":"جایگاه","Alliance":"t4_Station","Address":"[t4].[StationTitle]","SystemTypeName":"String","SystemType":0,"Width":200},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Gnr.Party","ColumnName":"t6_StationBeneficiary","DisplayName":"نام بهره بردار","Alliance":"t6_StationBeneficiary","Address":"[t6].[FullName]","SystemTypeName":"String","SystemType":0,"Width":200},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_StartDateInText","DisplayName":"تاریخ شروع قرارداد","Alliance":"t1_StartDateInText","Address":"[t1].[StartDateInText]","SystemTypeName":"DateShamsi","SystemType":5,"Width":110},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_ContractDurationInMonth","DisplayName":"مدت قرارداد (ماه)","Alliance":"t1_ContractDurationInMonth","Address":"[t1].[ContractDurationInMonth]","SystemTypeName":"Int","SystemType":7,"Width":120},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_EndDateInText","DisplayName":"تاریخ اتمام قرارداد","Alliance":"t1_EndDateInText","Address":"[t1].[EndDateInText]","SystemTypeName":"DateShamsi","SystemType":5,"Width":110},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_VisitCountInMonth","DisplayName":"تعداد بازدید در ماه","Alliance":"t1_VisitCountInMonth","Address":"[t1].[VisitCountInMonth]","SystemTypeName":"Int","SystemType":7,"Width":110},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_MonthlyPrice","DisplayName":"قیمت ماهانه","Alliance":"t1_MonthlyPrice","Address":"[t1].[MonthlyPrice]","SystemTypeName":"Long","SystemType":6,"Width":120},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_TotalPrice","DisplayName":"مجموع قیمت","Alliance":"t1_TotalPrice","Address":"[t1].[TotalPrice]","SystemTypeName":"Long","SystemType":6,"Width":120},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Cng.Enums.MaintenanceContractStatusEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_Status","DisplayName":"وضعیت","Alliance":"t1_Status","Address":"[t1].[Status]","SystemTypeName":"Select","SystemType":8,"Width":130},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_IsCanceled","DisplayName":"فسخ شده","Alliance":"t1_IsCanceled","Address":"[t1].[IsCanceled]","SystemTypeName":"Boolean","SystemType":1,"Width":110},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_CancelDescription","DisplayName":"توضیحات فسخ قرارداد","Alliance":"t1_CancelDescription","Address":"[t1].[CancelDescription]","SystemTypeName":"String","SystemType":0,"Width":200},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_Comment","DisplayName":"کامنت","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SystemType":0,"Width":250},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_CreatedByName","DisplayName":"کاربر ایجادکننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SystemType":0,"Width":150},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SystemType":4,"Width":100},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_ModifiedByName","DisplayName":"کاربر ویرایشگر","Alliance":"t1_ModifiedByName","Address":"[t1].[ModifiedByName]","SystemTypeName":"String","SystemType":0,"Width":150},{"SelectIndex":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","PrimaryKey":false,"Visible":true,"TableName":"Cng.MaintenanceContract","ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"زمان ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SystemType":4,"Width":150}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":"function(ctx) {\n    var data = ctx.rowData || {};\n    var $cell = ctx.$row.find(''td'').eq(0);\n    var status = data.t1_Status;\n    if (status == 2086) $cell.css(''background-color'', ''#e88282'');\n}"}';
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Pid BIGINT;

    SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
    IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51640, N'JSON نمایه Cng_MaintenanceContract_List معتبر نیست.', 1;

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
    WHERE r.Name IN (N'ShowAllMenus', N'Cng.MaintenanceContract.Manage', N'مشاهده کلی HTS');
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_CngMaintenanceContract_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Cng_MaintenanceContract_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Cng_MaintenanceContract_List'
ORDER BY CAST(j.[key] AS int);
