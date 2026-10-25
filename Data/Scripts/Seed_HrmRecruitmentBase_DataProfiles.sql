/*
Seed_HrmRecruitmentBase_DataProfiles.sql
نمایه لیست اطلاعات پایه جذب و استخدام.
جدید برای پست سازمانی و رشته تحصیلی خاموش است؛ برای مهارت و کانال ارتباطی روشن است.
UTF-8 with BOM. Idempotent.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_HrmRecruitmentBase_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-hrm-recruitment-base-profiles';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    DECLARE @ActFull NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @ActNoNew NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    IF OBJECT_ID('tempdb..#Profiles') IS NOT NULL DROP TABLE #Profiles;
    CREATE TABLE #Profiles (
        Name NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        EntityLower NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        ColumnsJson NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL
    );

    INSERT INTO #Profiles VALUES
    (N'Hrm_OrgPost_List', N'پست های سازمانی', N'entities.app.hrm.orgpost',
     N'SELECT [t1].[Id] AS [t1_Id], [t1].[Title] AS [t1_Title], [t1].[ExternalPost] AS [t1_ExternalPost], [t1].[IsActive] AS [t1_IsActive] FROM [Hrm].[OrgPost] AS [t1]',
     N'[{"TableName":"Hrm.OrgPost","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.OrgPost","ColumnName":"t1_Title","DisplayName":"عنوان","Alliance":"t1_Title","Address":"[t1].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":320,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.OrgPost","ColumnName":"t1_ExternalPost","DisplayName":"پست خارجی","Alliance":"t1_ExternalPost","Address":"[t1].[ExternalPost]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.OrgPost","ColumnName":"t1_IsActive","DisplayName":"فعال","Alliance":"t1_IsActive","Address":"[t1].[IsActive]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.Base.IsActiveEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""}]',
     @ActNoNew),
    (N'Hrm_Skill_List', N'مهارت', N'entities.app.hrm.skill',
     N'SELECT [t1].[Id] AS [t1_Id], [t1].[Title] AS [t1_Title] FROM [Hrm].[Skill] AS [t1]',
     N'[{"TableName":"Hrm.Skill","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.Skill","ColumnName":"t1_Title","DisplayName":"عنوان","Alliance":"t1_Title","Address":"[t1].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":360,"Filterable":true,"Sortable":true,"ClassName":""}]',
     @ActFull),
    (N'Hrm_StudyCourse_List', N'رشته تحصیلی', N'entities.app.hrm.studycourse',
     N'SELECT [t1].[Id] AS [t1_Id], [t1].[Title] AS [t1_Title] FROM [Hrm].[StudyCourse] AS [t1]',
     N'[{"TableName":"Hrm.StudyCourse","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.StudyCourse","ColumnName":"t1_Title","DisplayName":"عنوان","Alliance":"t1_Title","Address":"[t1].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":360,"Filterable":true,"Sortable":true,"ClassName":""}]',
     @ActNoNew),
    (N'Hrm_IncomingSource_List', N'کانال ارتباطی', N'entities.app.hrm.incomingsource',
     N'SELECT [t1].[Id] AS [t1_Id], [t1].[Title] AS [t1_Title] FROM [Hrm].[IncomingSource] AS [t1]',
     N'[{"TableName":"Hrm.IncomingSource","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.IncomingSource","ColumnName":"t1_Title","DisplayName":"عنوان","Alliance":"t1_Title","Address":"[t1].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":360,"Filterable":true,"Sortable":true,"ClassName":""}]',
     @ActFull);

    DECLARE @Name NVARCHAR(100), @Title NVARCHAR(200), @Ent NVARCHAR(200);
    DECLARE @Sel NVARCHAR(MAX), @Cols NVARCHAR(MAX), @Act NVARCHAR(MAX), @Qj NVARCHAR(MAX);

    DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Name, Title, EntityLower, CustomQuery, ColumnsJson, ActionOptions FROM #Profiles;
    OPEN c; FETCH NEXT FROM c INTO @Name, @Title, @Ent, @Sel, @Cols, @Act;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
        BEGIN
            UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
                ActionOptions=@Act, CustomActionButtonsJson=N'[]', EventScriptsJson=N'{"onSelectedRow":"","onRowAdded":""}',
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
            VALUES (@Name, @Title, @Qj, @Cols, N'{}', N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
                 1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            PRINT N'  CREATED ' + @Name;
        END
        FETCH NEXT FROM c INTO @Name, @Title, @Ent, @Sel, @Cols, @Act;
    END
    CLOSE c; DEALLOCATE c;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_HrmRecruitmentBase_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT q.Name, JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name IN (N'Hrm_OrgPost_List', N'Hrm_Skill_List', N'Hrm_StudyCourse_List', N'Hrm_IncomingSource_List')
ORDER BY q.Name, CAST(j.[key] AS int);
