/*
Seed_CngBasicInfo_DataProfiles.sql
نمایه داده اطلاعات پایه CNG (HTS 433/434). UTF-8 BOM. Idempotent.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_CngBasicInfo_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-basicinfo-dataprofiles';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    IF OBJECT_ID('tempdb..#CngProfiles') IS NOT NULL DROP TABLE #CngProfiles;
    CREATE TABLE #CngProfiles (
        Name NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        EntityLower NVARCHAR(200) NOT NULL,
        EntityPascal NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        ColumnsJson NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL,
        CustomButtons NVARCHAR(MAX) NOT NULL
    );

    INSERT INTO #CngProfiles VALUES
    (N'Cng_FailureInfo_List', N'پارامترهای فنی', N'entities.app.cng.failureinfo', N'Entities.App.Cng.FailureInfo',
     N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[HtsId] AS [t1_HtsId],
    [t1].[Group] AS [t1_Group],
    [t1].[Title] AS [t1_Title],
    [t1].[InitialValue] AS [t1_InitialValue],
    [t1].[Comment] AS [t1_Comment],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Cng].[FailureInfo] AS [t1]', N'[{"TableName":"Cng.FailureInfo","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.FailureInfo","ColumnName":"t1_HtsId","DisplayName":"شناسه HTS","Alliance":"t1_HtsId","Address":"[t1].[HtsId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.FailureInfo","ColumnName":"t1_Group","DisplayName":"گروه","Alliance":"t1_Group","Address":"[t1].[Group]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Cng.Enums.FailureInfoGroupEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.FailureInfo","ColumnName":"t1_Title","DisplayName":"عنوان","Alliance":"t1_Title","Address":"[t1].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.FailureInfo","ColumnName":"t1_InitialValue","DisplayName":"مقدار اولیه","Alliance":"t1_InitialValue","Address":"[t1].[InitialValue]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.FailureInfo","ColumnName":"t1_CreatedByName","DisplayName":"ایجاد کننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.FailureInfo","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.FailureInfo","ColumnName":"t1_Comment","DisplayName":"کامنت","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[{"id":"cng_edit_failureinfo","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Cng/FailureInfo/Edit?id='' + id, true, ''ویرایش'');\n}"}]'),
    (N'Cng_StationInfo_List', N'اطلاعات مشتری/جایگاه', N'entities.app.cng.stationinfo', N'Entities.App.Cng.StationInfo',
     N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
SELECT @CuRoles = Roles FROM system.[User] WHERE Id = @CuId;
DECLARE @HasShowAll BIT = CASE
    WHEN EXISTS (
        SELECT 1 FROM OPENJSON(ISNULL(@CuRoles, N''[]''))
        WHERE [value] IN (N''admin'', N''ShowAllMenus'', N''Cng.StationInfo.Manage'', N''Cng.StationInfo.ShowAll'')
    ) THEN 1 ELSE 0 END;

--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[HtsId] AS [t1_HtsId],
    [t2].[Code] AS [t2_Code],
    [t3].[FullName] AS [t3_FullName],
    [t1].[StationTitle] AS [t1_StationTitle],
    [t1].[EquipmentType] AS [t1_EquipmentType],
    [t1].[CompressorSerial] AS [t1_CompressorSerial],
    [t1].[DryerSerial] AS [t1_DryerSerial],
    [t1].[Dispenser1Serial] AS [t1_Dispenser1Serial],
    [t1].[Dispenser2Serial] AS [t1_Dispenser2Serial],
    [t1].[Dispenser3Serial] AS [t1_Dispenser3Serial],
    [t1].[Dispenser4Serial] AS [t1_Dispenser4Serial],
    [t1].[StorageModule] AS [t1_StorageModule],
    [t1].[LaunchShamsiDate] AS [t1_LaunchShamsiDate],
    [t1].[PermanentDeliveryShamsiDate] AS [t1_PermanentDeliveryShamsiDate],
    [t1].[RepresentativeName] AS [t1_RepresentativeName],
    [t1].[RepresentativeTellNumber] AS [t1_RepresentativeTellNumber],
    [t1].[RepresentativeMobileNumber] AS [t1_RepresentativeMobileNumber],
    [t1].[RepresentativeEmailAddress] AS [t1_RepresentativeEmailAddress],
    [t1].[Comment] AS [t1_Comment],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Cng].[StationInfo] AS [t1]
LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[CustomerId] = [t2].[Id]
LEFT JOIN [Gnr].[Party] AS [t3] ON [t2].[PartyId] = [t3].[Id]
WHERE @HasShowAll = 1 OR [t1].[CreatedById] = @CuId', N'[{"TableName":"Cng.StationInfo","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_HtsId","DisplayName":"شناسه HTS","Alliance":"t1_HtsId","Address":"[t1].[HtsId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.Customer","ColumnName":"t2_Code","DisplayName":"کد مشتری","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Party","ColumnName":"t3_FullName","DisplayName":"مشتری","Alliance":"t3_FullName","Address":"[t3].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_StationTitle","DisplayName":"جایگاه","Alliance":"t1_StationTitle","Address":"[t1].[StationTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_EquipmentType","DisplayName":"نوع تجهیز","Alliance":"t1_EquipmentType","Address":"[t1].[EquipmentType]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Cng.Enums.StationEquipmentTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_CompressorSerial","DisplayName":"سریال کمپرسور","Alliance":"t1_CompressorSerial","Address":"[t1].[CompressorSerial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_DryerSerial","DisplayName":"سریال درایر","Alliance":"t1_DryerSerial","Address":"[t1].[DryerSerial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_Dispenser1Serial","DisplayName":"سریال دیسپنسر1","Alliance":"t1_Dispenser1Serial","Address":"[t1].[Dispenser1Serial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_Dispenser2Serial","DisplayName":"سریال دیسپنسر2","Alliance":"t1_Dispenser2Serial","Address":"[t1].[Dispenser2Serial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_Dispenser3Serial","DisplayName":"سریال دیسپنسر3","Alliance":"t1_Dispenser3Serial","Address":"[t1].[Dispenser3Serial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_Dispenser4Serial","DisplayName":"سریال دیسپنسر4","Alliance":"t1_Dispenser4Serial","Address":"[t1].[Dispenser4Serial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_StorageModule","DisplayName":"ماژول ذخیره سازی","Alliance":"t1_StorageModule","Address":"[t1].[StorageModule]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_LaunchShamsiDate","DisplayName":"تاریخ راه اندازی","Alliance":"t1_LaunchShamsiDate","Address":"[t1].[LaunchShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_PermanentDeliveryShamsiDate","DisplayName":"تاریخ تحویل نهایی","Alliance":"t1_PermanentDeliveryShamsiDate","Address":"[t1].[PermanentDeliveryShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_CreatedByName","DisplayName":"ایجاد کننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_Comment","DisplayName":"کامنت","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_RepresentativeName","DisplayName":"عنوان و سمت نماینده","Alliance":"t1_RepresentativeName","Address":"[t1].[RepresentativeName]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_RepresentativeTellNumber","DisplayName":"شماره تلفن نماینده","Alliance":"t1_RepresentativeTellNumber","Address":"[t1].[RepresentativeTellNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_RepresentativeMobileNumber","DisplayName":"شماره موبایل نماینده","Alliance":"t1_RepresentativeMobileNumber","Address":"[t1].[RepresentativeMobileNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.StationInfo","ColumnName":"t1_RepresentativeEmailAddress","DisplayName":"آدرس ایمیل نماینده","Alliance":"t1_RepresentativeEmailAddress","Address":"[t1].[RepresentativeEmailAddress]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[{"id":"cng_edit_stationinfo","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Cng/StationInfo/Edit?id='' + id, true, ''ویرایش'');\n}"}]');

    DECLARE @Name NVARCHAR(100), @Title NVARCHAR(200), @Ent NVARCHAR(200), @Pascal NVARCHAR(200);
    DECLARE @Sel NVARCHAR(MAX), @Cols NVARCHAR(MAX), @Act NVARCHAR(MAX), @Btn NVARCHAR(MAX), @Qj NVARCHAR(MAX), @Pid BIGINT;

    DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Name, Title, EntityLower, EntityPascal, CustomQuery, ColumnsJson, ActionOptions, CustomButtons FROM #CngProfiles;
    OPEN c; FETCH NEXT FROM c INTO @Name, @Title, @Ent, @Pascal, @Sel, @Cols, @Act, @Btn;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
        BEGIN
            UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
                ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=N'{"onSelectedRow":"","onRowAdded":""}',
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
            VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, N'{"onSelectedRow":"","onRowAdded":""}',
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
        WHERE (@Name = N'Cng_FailureInfo_List' AND r.Name IN (N'ShowAllMenus', N'مشاهده کلی HTS', N'Cng.FailureInfo.Manage', N'Cng.FailureInfo.View'))
           OR (@Name = N'Cng_StationInfo_List' AND r.Name IN (N'ShowAllMenus', N'مشاهده کلی HTS', N'Cng.StationInfo.Manage', N'Cng.StationInfo.ShowAll', N'Cng.StationInfo.Agency', N'Cng.StationInfo.View'));
        PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));
        FETCH NEXT FROM c INTO @Name, @Title, @Ent, @Pascal, @Sel, @Cols, @Act, @Btn;
    END
    CLOSE c; DEALLOCATE c;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_CngBasicInfo_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name IN (N'Cng_FailureInfo_List', N'Cng_StationInfo_List');
SELECT q.Name, JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name IN (N'Cng_FailureInfo_List', N'Cng_StationInfo_List')
ORDER BY q.Name, CAST(j.[key] AS int);
