/*
Seed_Qc_MeasurementParameterItem_DataProfiles.sql
نمایه داده پارامتر اندازه گیری.
ستون‌های نمایان: گروه بندی پارامترهای اندازه گیری، اقلام پارامتر اندازه گیری، نماد رایج.
اکشن‌های گرید: جدید، ویرایش، حذف. خروجی اکسل در صفحه HTS نیست.
UTF-8 BOM. Idempotent. نقش‌ها باید از سید دسترسی همین صفحه وجود داشته باشند.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_MeasurementParameterItem_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-measurementparameteritem-dataprofiles';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @Name NVARCHAR(100) = N'Qc_MeasurementParameterItem_List';
    DECLARE @Title NVARCHAR(200) = N'پارامتر اندازه گیری';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.qc.measurementparameteritem';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Qc.MeasurementParameterItem';
    DECLARE @Sel NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[Title] AS [t2_Title],
    [t1].[Title] AS [t1_Title],
    [t1].[CommonSymbol] AS [t1_CommonSymbol]
FROM [Qc].[MeasurementParameterItem] AS [t1]
LEFT JOIN [Qc].[MeasurementParameter] AS [t2] ON [t2].[Id] = [t1].[MeasurementParameterId]';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName":"Qc.MeasurementParameterItem","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.MeasurementParameter","ColumnName":"t2_Title","DisplayName":"گروه بندی پارامترهای اندازه گیری","Alliance":"t2_Title","Address":"[t2].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":280,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.MeasurementParameterItem","ColumnName":"t1_Title","DisplayName":"اقلام پارامتر اندازه گیری","Alliance":"t1_Title","Address":"[t1].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":400,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.MeasurementParameterItem","ColumnName":"t1_CommonSymbol","DisplayName":"نماد رایج","Alliance":"t1_CommonSymbol","Address":"[t1].[CommonSymbol]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[]';
    DECLARE @Qj NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
    DECLARE @Pid BIGINT;

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
    WHERE r.Name IN (N'ShowAllMenus', N'مشاهده کلی HTS', N'Qc.MeasurementParameter.Experts', N'Qc.MeasurementParameter.SystemExperts');
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    IF (SELECT COUNT(*) FROM system.RoleAccess WHERE ActionAccessType=3 AND RowId=@Pid) <> 4
        THROW 50051, N'Qc_MeasurementParameterItem_List RoleAccess count is not 4. Run the roles seed first.', 1;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_MeasurementParameterItem_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Qc_MeasurementParameterItem_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName, JSON_VALUE(j.value, '$.Visible') AS Visible
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Qc_MeasurementParameterItem_List';
