-- نمایه داده شرکت آموزش: ستون‌های گرید سیستم قدیم به‌همراه صنعت مشتری
-- Idempotent

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-trn-company-dataprofile';
DECLARE @Name NVARCHAR(200) = N'Trn_Company_WithIndustry';
DECLARE @Title NVARCHAR(200) = N'لیست با صنعت مشتری';
DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.trn.company';
DECLARE @EntityName NVARCHAR(200) = N'Entities.App.Trn.Company';
DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName": "new", "enable": true, "title": "جدید"}, {"dataActionName": "edit", "enable": true, "title": "ویرایش"}, {"dataActionName": "delete", "enable": true, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "SELECT\n    [t1].[Id] AS [t1_Id],\n    COALESCE([t2].[CompanyName], [t2].[FullName]) AS [t2_CompanyName],\n    [t1].[Title] AS [t1_Title],\n    [t1].[TitleInLatin] AS [t1_TitleInLatin],\n    [ind].[Title] AS [ind_Title],\n    [t1].[Phone] AS [t1_Phone],\n    [t1].[Fax] AS [t1_Fax],\n    [t1].[Email] AS [t1_Email],\n    [t1].[Address] AS [t1_Address],\n    [t1].[ModifiedByName] AS [t1_ModifiedByName],\n    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],\n    [t1].[CreatedByName] AS [t1_CreatedByName],\n    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]\nFROM [Trn].[Company] AS [t1]\nLEFT JOIN [Gnr].[Party] AS [t2] ON [t2].[Id] = [t1].[PartyId]\nOUTER APPLY (\n    SELECT TOP 1 [i].[Title]\n    FROM [SLS].[Customer] AS [cu]\n    INNER JOIN [Gnr].[PartyIndustry] AS [i] ON [i].[Id] = [cu].[IndustryId]\n    WHERE [cu].[PartyId] = [t1].[PartyId]\n    ORDER BY [cu].[Id]\n) AS [ind]", "Parameters": [], "Selects": null}';
DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName": null, "ColumnName": "t1_Id", "DisplayName": "شناسه", "Alliance": "t1_Id", "Address": null, "SystemTypeName": "Long", "SelectIndex": 0, "Visible": false, "PrimaryKey": true, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 80, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t2_CompanyName", "DisplayName": "نام شرکت از سیستم ارتباط با مشتری", "Alliance": "t2_CompanyName", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 220, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_Title", "DisplayName": "عنوان", "Alliance": "t1_Title", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 180, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_TitleInLatin", "DisplayName": "عنوان (انگلیسی)", "Alliance": "t1_TitleInLatin", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 180, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "ind_Title", "DisplayName": "صنعت مشتری", "Alliance": "ind_Title", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 180, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_Phone", "DisplayName": "تلفن", "Alliance": "t1_Phone", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_Fax", "DisplayName": "فکس", "Alliance": "t1_Fax", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_Email", "DisplayName": "ایمیل", "Alliance": "t1_Email", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 180, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_Address", "DisplayName": "آدرس", "Alliance": "t1_Address", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 320, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_ModifiedByName", "DisplayName": "ویرایش کننده", "Alliance": "t1_ModifiedByName", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 140, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_ModifiedDateShamsiDateTime", "DisplayName": "تاریخ ویرایش", "Alliance": "t1_ModifiedDateShamsiDateTime", "Address": null, "SystemTypeName": "DateShamsi", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 5, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": "function(data, type, row) { if (data) { return String(data).split(\" \")[0]; } return data; }", "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_CreatedByName", "DisplayName": "ایجاد کننده", "Alliance": "t1_CreatedByName", "Address": null, "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 140, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": null, "ColumnName": "t1_CreatedOnShamsiDateTime", "DisplayName": "تاریخ ایجاد", "Alliance": "t1_CreatedOnShamsiDateTime", "Address": null, "SystemTypeName": "DateShamsi", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 5, "SortDirection": 1, "SortOrder": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": "function(data, type, row) { if (data) { return String(data).split(\" \")[0]; } return data; }", "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}]';
DECLARE @ProfileId BIGINT;

IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name)
BEGIN
    UPDATE [system].[SavedQuery]
    SET Title = @Title,
        QueryJson = @QueryJson,
        ColumnsJson = @ColumnsJson,
        EntityFullName = @EntityFullName,
        Mode = 1,
        Type = 1,
        ActionOptions = @ActionOptions,
        CustomActionButtonsJson = N'[]',
        EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}',
        DiagramJson = N'{}',
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi,
        IsActive = 1
    WHERE Name = @Name;

    SELECT @ProfileId = Id FROM [system].[SavedQuery] WHERE Name = @Name;
END
ELSE
BEGIN
    INSERT INTO [system].[SavedQuery]
    (
        Name, Title, QueryJson, ColumnsJson, DiagramJson,
        CustomActionButtonsJson, EventScriptsJson,
        Mode, Type, EntityFullName, ActionOptions,
        CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    VALUES
    (
        @Name, @Title, @QueryJson, @ColumnsJson, N'{}',
        N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
        1, 1, @EntityFullName, @ActionOptions,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    );

    SET @ProfileId = SCOPE_IDENTITY();
END;

DELETE FROM [system].[RoleAccess]
WHERE ActionAccessType = 3 AND RowId = @ProfileId;

INSERT INTO [system].[RoleAccess]
(
    Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
    EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
    CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
    IsActive
)
SELECT
    N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)),
    3,
    7,
    @EntityName,
    NULL,
    NULL,
    @ProfileId,
    r.Id,
    1, 1, @SeedUser, @SeedUser,
    @Now, @NowShamsi, @Now, @NowShamsi,
    1
FROM [system].[Role] r
WHERE r.Name IN (N'Trn.FullAccess', N'Trn.ReadOnly');

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;

SELECT q.Id, q.Name, q.Title,
    JSON_VALUE(j.value, '$.Alliance') AS Alliance,
    JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Trn_Company_WithIndustry';
