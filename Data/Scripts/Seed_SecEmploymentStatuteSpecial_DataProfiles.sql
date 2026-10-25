/*
Seed_SecEmploymentStatuteSpecial_DataProfiles.sql
نمایه داده لیست استثنائات سابقه حضور — ستون‌های گرید HTS.
UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sec-employment-statute-special-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @Name NVARCHAR(100) = N'Sec_EmploymentStatuteSpecial_List';
    DECLARE @Title NVARCHAR(200) = N'لیست استثنائات سابقه حضور';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.sec.employmentstatutespecial';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Sec.EmploymentStatuteSpecial';
    DECLARE @Sel NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[Code] AS [t2_Code],
    LTRIM(RTRIM(CONCAT(ISNULL([t2].[Name], N''''), N'' '', ISNULL([t2].[Family], N'''')))) AS [Fullname],
    [t1].[RealEmploymentShamsiDate] AS [t1_RealEmploymentShamsiDate],
    [t1].[AdditionalYear] AS [t1_AdditionalYear],
    [t1].[AdditionalMonth] AS [t1_AdditionalMonth],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Sec].[EmploymentStatuteSpecial] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t1].[PersonelId] = [t2].[Id]';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName": "Sec.EmploymentStatuteSpecial", "ColumnName": "t1_Id", "DisplayName": "شناسه", "Alliance": "t1_Id", "Address": "[t1].[Id]", "SystemTypeName": "Long", "SelectIndex": 0, "Visible": false, "PrimaryKey": true, "SystemType": 6, "SortDirection": 1, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 80, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.EmploymentStatuteSpecial", "ColumnName": "t2_Code", "DisplayName": "کد پرسنلی", "Alliance": "t2_Code", "Address": "[t2].[Code]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.EmploymentStatuteSpecial", "ColumnName": "Fullname", "DisplayName": "نام و نام خانوادگی", "Alliance": "Fullname", "Address": "Fullname", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": true, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 180, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.EmploymentStatuteSpecial", "ColumnName": "t1_RealEmploymentShamsiDate", "DisplayName": "تاریخ استخدام واقعی", "Alliance": "t1_RealEmploymentShamsiDate", "Address": "[t1].[RealEmploymentShamsiDate]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.EmploymentStatuteSpecial", "ColumnName": "t1_AdditionalYear", "DisplayName": "میزان افزایش سال", "Alliance": "t1_AdditionalYear", "Address": "[t1].[AdditionalYear]", "SystemTypeName": "Int", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 7, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.EmploymentStatuteSpecial", "ColumnName": "t1_AdditionalMonth", "DisplayName": "میزان افزایش ماه", "Alliance": "t1_AdditionalMonth", "Address": "[t1].[AdditionalMonth]", "SystemTypeName": "Int", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 7, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.EmploymentStatuteSpecial", "ColumnName": "t1_CreatedByName", "DisplayName": "کاربر ایجادکننده", "Alliance": "t1_CreatedByName", "Address": "[t1].[CreatedByName]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 140, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.EmploymentStatuteSpecial", "ColumnName": "t1_CreatedOnShamsiDateTime", "DisplayName": "تاریخ و زمان درج", "Alliance": "t1_CreatedOnShamsiDateTime", "Address": "[t1].[CreatedOnShamsiDateTime]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName": "new", "enable": true, "title": "جدید"}, {"dataActionName": "edit", "enable": true, "title": "ویرایش"}, {"dataActionName": "delete", "enable": true, "title": "حذف"}]';
    DECLARE @Qj NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
    DECLARE @Pid BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=N'[]', EventScriptsJson=N'{"onSelectedRow":"","onRowAdded":""}',
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
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
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
    WHERE r.Name IN (N'ShowAllMenus', N'مشاهده کلی HTS', N'Sec.EmploymentStatuteSpecial.Manage');
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SecEmploymentStatuteSpecial_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Sec_EmploymentStatuteSpecial_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Sec_EmploymentStatuteSpecial_List'
ORDER BY CAST(j.[key] AS int);
