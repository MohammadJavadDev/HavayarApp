/*
Seed_SecPersonnel_DataProfiles.sql
نمایه داده مدیریت پرسنل دبیرخانه — فیلتر معادل GetGridData سیستم قدیم.
UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sec-personnel-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @Name NVARCHAR(100) = N'Sec_Personnel_List';
    DECLARE @Title NVARCHAR(200) = N'مدیریت پرسنل دبیرخانه';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.sec.personnel';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Sec.Personnel';
    DECLARE @Sel NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[IsActive] AS [t1_IsActive],
    [t1].[ForceToSend] AS [t1_ForceToSend],
    [t4].[ReciversTitle] AS [t4_ReciversTitle],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t2].[Family] AS [t2_Family],
    [t1].[NameDisplay] AS [t1_NameDisplay],
    [t1].[FamilyDisplay] AS [t1_FamilyDisplay],
    [t2].[FatherName] AS [t2_FatherName],
    CASE WHEN [t2].[GenderCode] = 1 THEN N''آقا'' ELSE N''خانم'' END AS [Sex_Title],
    [t2].[Mobile] AS [t2_Mobile],
    [t1].[BirthDate] AS [t1_BirthDate],
    [t2].[NationalID] AS [t2_NationalID],
    [t2].[IDNumber] AS [t2_IDNumber],
    [t2].[InsuranceNo] AS [t2_InsuranceNo],
    [t1].[EmploymentDate] AS [t1_EmploymentDate],
    [t2].[FieldOfStudy] AS [t2_FieldOfStudy],
    [t2].[DegreeOfEducation] AS [t2_DegreeOfEducation],
    CASE [t2].[BranchCode]
        WHEN 1 THEN N''دفتر مرکزی''
        WHEN 18 THEN N''دفتر مرکزی''
        WHEN 2 THEN N''کارخانه''
        ELSE CAST([t2].[BranchCode] AS nvarchar(20))
    END AS [BranchTitle],
    [t3].[Title] AS [t3_Title],
    CASE WHEN [t1].[Picture] IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS [IsUpload],
    CASE WHEN [t1].[SecondPicture] IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS [IsSecondPictureUpload]
FROM [Sec].[Personnel] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t1].[PersonelId] = [t2].[Id]
LEFT JOIN [Hrm].[OrgUnit] AS [t3] ON [t2].[OrgUnitId] = [t3].[Id]
LEFT JOIN [Sec].[ReciversGroup] AS [t4] ON [t1].[ReciversGroupId] = [t4].[Id]
WHERE
    [t2].[HamkaranId] IN (534, 15024, 15299)
    OR (
        ISNULL([t1].[NameDisplay], N'''') NOT LIKE N''%شرکت%''
        AND [t2].[BranchCode] IN (1, 2, 18)
    )';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName": "Sec.Personnel", "ColumnName": "t1_Id", "DisplayName": "شناسه", "Alliance": "t1_Id", "Address": "[t1].[Id]", "SystemTypeName": "Long", "SelectIndex": 0, "Visible": false, "PrimaryKey": true, "SystemType": 6, "SortDirection": 1, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 80, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t1_IsActive", "DisplayName": "فعال", "Alliance": "t1_IsActive", "Address": "[t1].[IsActive]", "SystemTypeName": "Select", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 8, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": {"ListOptions": [], "TypeOption": 3, "SystemTypeName": "Entities.Base.IsActiveEnum"}, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 80, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t1_ForceToSend", "DisplayName": "اجبار ارسال", "Alliance": "t1_ForceToSend", "Address": "[t1].[ForceToSend]", "SystemTypeName": "Boolean", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 1, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t4_ReciversTitle", "DisplayName": "گیرنده", "Alliance": "t4_ReciversTitle", "Address": "[t4].[ReciversTitle]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_Code", "DisplayName": "کد پرسنلی", "Alliance": "t2_Code", "Address": "[t2].[Code]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_Name", "DisplayName": "نام", "Alliance": "t2_Name", "Address": "[t2].[Name]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_Family", "DisplayName": "نام خانوادگی", "Alliance": "t2_Family", "Address": "[t2].[Family]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t1_NameDisplay", "DisplayName": "نام نمایشی", "Alliance": "t1_NameDisplay", "Address": "[t1].[NameDisplay]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t1_FamilyDisplay", "DisplayName": "نام‌خانوادگی نمایشی", "Alliance": "t1_FamilyDisplay", "Address": "[t1].[FamilyDisplay]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 130, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_FatherName", "DisplayName": "نام پدر", "Alliance": "t2_FatherName", "Address": "[t2].[FatherName]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "Sex_Title", "DisplayName": "جنسیت", "Alliance": "Sex_Title", "Address": "Sex_Title", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": true, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 90, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_Mobile", "DisplayName": "موبایل", "Alliance": "t2_Mobile", "Address": "[t2].[Mobile]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t1_BirthDate", "DisplayName": "تاریخ تولد", "Alliance": "t1_BirthDate", "Address": "[t1].[BirthDate]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_NationalID", "DisplayName": "کد ملی", "Alliance": "t2_NationalID", "Address": "[t2].[NationalID]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 110, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_IDNumber", "DisplayName": "شماره شناسنامه", "Alliance": "t2_IDNumber", "Address": "[t2].[IDNumber]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 110, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_InsuranceNo", "DisplayName": "شماره بیمه", "Alliance": "t2_InsuranceNo", "Address": "[t2].[InsuranceNo]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 110, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t1_EmploymentDate", "DisplayName": "تاریخ شروع به کار", "Alliance": "t1_EmploymentDate", "Address": "[t1].[EmploymentDate]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_FieldOfStudy", "DisplayName": "مدرک تحصیلی", "Alliance": "t2_FieldOfStudy", "Address": "[t2].[FieldOfStudy]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t2_DegreeOfEducation", "DisplayName": "رشته تحصیلی", "Alliance": "t2_DegreeOfEducation", "Address": "[t2].[DegreeOfEducation]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 200, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "BranchTitle", "DisplayName": "محل خدمت", "Alliance": "BranchTitle", "Address": "BranchTitle", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": true, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "t3_Title", "DisplayName": "واحد سازمانی", "Alliance": "t3_Title", "Address": "[t3].[Title]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 220, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "IsUpload", "DisplayName": "تصویر تولد", "Alliance": "IsUpload", "Address": "IsUpload", "SystemTypeName": "Boolean", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 1, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": true, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Sec.Personnel", "ColumnName": "IsSecondPictureUpload", "DisplayName": "تصویر سالروز", "Alliance": "IsSecondPictureUpload", "Address": "IsSecondPictureUpload", "SystemTypeName": "Boolean", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 1, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": true, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName": "new", "enable": true, "title": "جدید"}, {"dataActionName": "edit", "enable": true, "title": "ویرایش"}, {"dataActionName": "delete", "enable": true, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[{"id": "sec_edit_personnel", "title": "ویرایش", "dataActionName": "editRow", "colorClass": "btn-color-primary", "iconClass": "ki-pencil ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sec/Personnel/Edit?id='' + id, true, ''ویرایش پرسنل دبیرخانه'');\n}"}]';
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
    WHERE r.Name IN (N'ShowAllMenus', N'مشاهده کلی HTS', N'Sec.Personnel.Manage', N'Sec.Personnel.View');
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SecPersonnel_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Sec_Personnel_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Sec_Personnel_List'
ORDER BY CAST(j.[key] AS int);
