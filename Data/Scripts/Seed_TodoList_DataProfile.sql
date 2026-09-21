-- ============================================
-- نمایه داده Trn.TodoList مطابق گرید سیستم قبلی (HTS)
-- ستون‌ها: شرکت، عنوان، شرح پیگیری، تاریخ پیگیری، کاربر ایجادکننده، تاریخ ایجاد
-- Idempotent
-- ============================================

SET NOCOUNT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-todolist-dataprofile';
DECLARE @Name NVARCHAR(200) = N'todolist_listinfo';
DECLARE @Title NVARCHAR(200) = N'لیست اطلاعات';
DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.trn.todolist';
DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[Title] AS [t2_Title],
    [t1].[Title] AS [t1_Title],
    [t1].[Description] AS [t1_Description],
    [t1].[TodoShamsiDate] AS [t1_TodoShamsiDate],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Trn].[TodoList] AS [t1]
LEFT JOIN [Trn].[Company] AS [t2] ON [t1].[CompanyId] = [t2].[Id]';

DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":null,"CustomQuery":null,"Parameters":[],"Selects":[{"Index":0,"Name":"Select1","Title":"Select1"}]}';
SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":null,"ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"t2_Title","DisplayName":"شرکت","Alliance":"t2_Title","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"t1_Title","DisplayName":"عنوان","Alliance":"t1_Title","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"t1_Description","DisplayName":"شرح پیگیری","Alliance":"t1_Description","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":350,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"t1_TodoShamsiDate","DisplayName":"تاریخ پیگیری","Alliance":"t1_TodoShamsiDate","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"t1_CreatedByName","DisplayName":"کاربر ایجادکننده","Alliance":"t1_CreatedByName","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data) { return data.split(\" \")[0]; } return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":null}
]';

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
        DiagramJson = N'[]',
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
        @Name, @Title, @QueryJson, @ColumnsJson, N'[]',
        N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
        1, 1, @EntityFullName, @ActionOptions,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    );

    SET @ProfileId = SCOPE_IDENTITY();
END;

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
    @EntityFullName,
    NULL,
    NULL,
    @ProfileId,
    r.Id,
    1, 1, @SeedUser, @SeedUser,
    @Now, @NowShamsi, @Now, @NowShamsi,
    1
FROM [system].[Role] r
WHERE r.Id IN (300000, 300001)
  AND NOT EXISTS (
        SELECT 1
        FROM [system].[RoleAccess] ra
        WHERE ra.Path = N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20))
          AND ra.RoleId = r.Id
          AND ra.ActionAccessType = 3
  );

SELECT @ProfileId AS SavedQueryId, @Name AS ProfileName;
