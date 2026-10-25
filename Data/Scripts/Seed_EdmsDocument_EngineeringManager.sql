/*
  نقش مدیر مهندسی و کارتابل جدا برای مدارک.
  نمایه‌های موجود تغییر نمی‌کنند. ستون و فیلتر این نمایه از گرید مدیریتی چک/تایید HTS است.
  UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-engineering-manager';
    DECLARE @RoleId BIGINT = 200072;
    DECLARE @RoleName NVARCHAR(200) = N'Edms.Document.EngineeringManager';
    DECLARE @RoleTitle NVARCHAR(200) = N'مهندسی - مدارک - مدیر مهندسی';
    DECLARE @ProfileName NVARCHAR(100) = N'Edms_Document_EngineeringManagerReviewCartable';
    DECLARE @ProfileTitle NVARCHAR(200) = N'کارتابل بررسی و تایید مدیر مهندسی';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Edms.Document';
    DECLARE @SourceName NVARCHAR(100) = N'Edms_Document_ReviewApproveCartable';
    DECLARE @Ent NVARCHAR(200);
    DECLARE @Cols NVARCHAR(MAX);
    DECLARE @Btn NVARCHAR(MAX);
    DECLARE @Act NVARCHAR(MAX);
    DECLARE @EventScripts NVARCHAR(MAX);
    DECLARE @OnRow NVARCHAR(MAX) = N'function(ctx) {
    var data = ctx.rowData || {};
    var status = parseInt(data.t1_StatusId, 10);
    var color = null;
    var text = "#111111";
    if (status === 14) color = "#0FF135";
    else if (status === 13) { color = "#077319"; text = "#ffffff"; }
    else if (status === 4 || status === 7 || status === 9) { color = "#6A935B"; text = "#ffffff"; }
    else if (status === 5 || status === 8 || status === 11 || status === 15 || status === 25 || status === 34) { color = "#6414CE"; text = "#ffffff"; }
    else if (status === 21) color = "#F573CA";
    else if (status === 3 || status === 6 || status === 10 || status === 12 || status === 26) { color = "#E01629"; text = "#ffffff"; }
    else if (status === 2 || status === 30) color = "#70C8F0";
    if (!color || !ctx.table) return;
    var colIdx = -1;
    ctx.table.columns().every(function (i) {
        if (this.dataSrc() === "_rowNumber") colIdx = i;
    });
    var row = ctx.$row;
    var node = colIdx >= 0 ? ctx.table.cell(row, colIdx).node() : (row && row.find ? row.find("td").get(0) : null);
    if (!node) return;
    node.style.setProperty("background-color", color, "important");
    node.style.setProperty("box-shadow", "inset 0 0 0 9999px " + color, "important");
    node.style.setProperty("color", text, "important");
}';
    DECLARE @Sql NVARCHAR(MAX);
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Pid BIGINT;
    DECLARE @UserId BIGINT;

    IF EXISTS (SELECT 1 FROM system.Role WHERE Name = @RoleName)
        SELECT @RoleId = Id FROM system.Role WHERE Name = @RoleName;
    ELSE IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @RoleId)
        THROW 51642, N'شناسه نقش 200072 برای نقش دیگری استفاده شده است.', 1;

    IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @RoleId)
    BEGIN
        UPDATE system.Role
        SET Name = @RoleName,
            Title = @RoleTitle,
            IsActive = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @RoleId;
    END
    ELSE
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role
            (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
             CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@RoleId, @RoleName, @RoleTitle, 1, @SeedUser, 1, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
    END

    SELECT @UserId = Id
    FROM system.[User]
    WHERE LOWER(UserName) = N'khodakarami.f';

    IF @UserId IS NULL
        THROW 51643, N'کاربر khodakarami.f یافت نشد.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM system.[User] u
        CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
        WHERE u.Id = @UserId AND TRY_CAST(j.value AS bigint) = @RoleId
    )
    BEGIN
        UPDATE system.[User]
        SET
            RoleIds = CASE
                WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                    THEN N'[' + CAST(@RoleId AS nvarchar(20)) + N']'
                ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS nvarchar(20)) + N']')
            END,
            Roles = CASE
                WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @RoleName) THEN Roles
                WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                    THEN N'["' + @RoleName + N'"]'
                ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @RoleName + N'"]')
            END,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @UserId;
    END

    SELECT
        @Ent = EntityFullName,
        @Btn = CustomActionButtonsJson,
        @Act = ActionOptions,
        @EventScripts = EventScriptsJson
    FROM system.SavedQuery
    WHERE Name = @SourceName;

    SET @EventScripts = JSON_MODIFY(
        CASE WHEN ISJSON(@EventScripts) = 1 THEN @EventScripts ELSE N'{"onSelectedRow":"","onRowAdded":""}' END,
        N'$.onRowAdded',
        @OnRow);

    IF @Btn IS NULL OR ISJSON(@Btn) <> 1
        THROW 51644, N'دکمه کارتابل بررسی و تایید برای کپی یافت نشد.', 1;

    /* همان ستون‌های مشترک نمایه‌های مدرک، با ترتیب گرید مدارک */
    SET @Cols = N'[
{"TableName":"Edms.Document","ColumnName":"IsChecked","DisplayName":"بررسی شده هست نیست","Alliance":"IsChecked","Address":"CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Project","ColumnName":"t2_Code","DisplayName":"کد پروژه","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.Project","ColumnName":"t2_Name","DisplayName":"نام پروژه","Alliance":"t2_Name","Address":"[t2].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"t3_Code","DisplayName":"کد مدرک","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"t3_Title","DisplayName":"عنوان مدرک","Alliance":"t3_Title","Address":"[t3].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":280,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_Revision","DisplayName":"نسخه(رویزن)","Alliance":"t1_Revision","Address":"[t1].[Revision]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_Status","DisplayName":"وضعیت مدرک","Alliance":"t1_Status","Address":"[t1].[Status]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Edms.Enums.DocumentStatusEnums"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_StatusId","DisplayName":"کد وضعیت","Alliance":"t1_StatusId","Address":"[t1].[Status]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":false,"Sortable":false,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"IsLatest","DisplayName":"نسخه آخر هست نیست","Alliance":"IsLatest","Address":"[t1].[IsLatest]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_GoalOfProduction","DisplayName":"هدف از نشر","Alliance":"t1_GoalOfProduction","Address":"[t1].[GoalOfProduction]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Edms.Enums.DocumentGoalOfProductionEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"t3_Displaying","DisplayName":"دیسیپلین","Alliance":"t3_Displaying","Address":"[t3].[Displaying]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Edms.Enums.ProjectVpisDisplayingEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.FileEntity","ColumnName":"t4_OriginalName","DisplayName":"عنوان فایل","Alliance":"t4_OriginalName","Address":"[t4].[OriginalName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.Document","ColumnName":"t1_Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ شمسی ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"تاریخ شمسی ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"t6_Name","DisplayName":"کاربر آماده کننده","Alliance":"t6_Name","Address":"[t6].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"t5_Name","DisplayName":"کاربر بررسی کننده","Alliance":"t5_Name","Address":"[t5].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"t7_Name","DisplayName":"کاربر تایید کننده","Alliance":"t7_Name","Address":"[t7].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_ProjectId","DisplayName":"شناسه پروژه","Alliance":"t1_ProjectId","Address":"[t1].[ProjectId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":false,"ClassName":""}
]';

    /*
      فیلتر نمایش‌همه صفحه چک/تایید HTS (ControlDocuments، Permission نمایش همه):
      Issue، Approve، تایید کارفرما بدون تاریخ تایید روی پروژه وندور، IssueForClient، ReplaySheet.
      در هوایار Approve بررسی‌کننده = 4 است. تایید نهایی (7) در کارتابل DCC می‌ماند.
      کامنت بررسی‌کننده (5) و کامنت تاییدکننده (8) در این صف نیستند.
      این صف به بررسی‌کننده/تاییدکننده همان VPIS محدود نیست.
    */
    SET @Sql = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
--!--mainsection
SELECT
    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t3].[Code] AS [t3_Code],
    [t3].[Title] AS [t3_Title],
    [t1].[Revision] AS [t1_Revision],
    [t1].[Status] AS [t1_Status],
    [t1].[Status] AS [t1_StatusId],
    [t1].[IsLatest] AS [IsLatest],
    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],
    [t3].[Displaying] AS [t3_Displaying],
    [t4].[OriginalName] AS [t4_OriginalName],
    [t1].[Comment] AS [t1_Comment],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t6].[Name] AS [t6_Name],
    [t5].[Name] AS [t5_Name],
    [t7].[Name] AS [t7_Name],
    [t1].[Id] AS [t1_Id],
    [t1].[ProjectId] AS [t1_ProjectId]
FROM [Edms].[Document] AS [t1]
INNER JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]
INNER JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]
LEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]
LEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]
LEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]
LEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]
OUTER APPLY (
    SELECT TOP (1) c.CreatedById
    FROM [Edms].[DocumentComment] AS c
    WHERE c.DocumentId = [t1].[Id]
    ORDER BY c.Id DESC
) AS lastComment
WHERE [t1].[IsLatest] = 1
AND (
    ([t1].[Status] = 2 AND ISNULL(lastComment.CreatedById, 0) <> @CuId)
    OR [t1].[Status] = 4
    OR (
        ISNULL([t2].[ProjectIsVendoriType], 0) = 1
        AND [t1].[Status] = 13
        AND [t1].[ApprovedMiladiDateTime] IS NULL
    )
    OR [t1].[Status] IN (28, 30)
)
AND ISNULL([t2].[Status], -1) NOT IN (2, 4)';

    SET @Qj = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"'
        + STRING_ESCAPE(@Sql, 'json')
        + N'","Parameters":[],"Selects":null}';

    IF ISJSON(@Qj) <> 1 OR ISJSON(@Cols) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Act) <> 1
        THROW 51645, N'JSON نمایه مدیر مهندسی معتبر نیست.', 1;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @ProfileTitle,
            QueryJson = @Qj,
            ColumnsJson = @Cols,
            EntityFullName = @Ent,
            Mode = 1,
            Type = 1,
            ActionOptions = @Act,
            CustomActionButtonsJson = @Btn,
            EventScriptsJson = @EventScripts,
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @ProfileName;
        SELECT @Pid = Id FROM system.SavedQuery WHERE Name = @ProfileName;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@ProfileName, @ProfileTitle, @Qj, @Cols, N'{}', @Btn, @EventScripts,
             1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END

    DELETE FROM system.RoleAccess
    WHERE ActionAccessType = 3 AND RowId = @Pid;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @ProfileTitle, NULL, @Pid, @RoleId,
         1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT p.Path, p.ActionAccessType, p.ActionAccessItemType, @Pascal, p.DisplayName, NULL, NULL, @RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
        (N'/panel/edms/document/list', 1, 1, N'لیست اطلاعات'),
        (N'/panel/edms/document/fetchdata', 2, 2, N'دریافت اطلاعات'),
        (N'/panel/edms/document/edit', 1, 5, N'ویرایش اطلاعات'),
        (N'/panel/edms/document/editasgroup', 1, 5, N'ویرایش اطلاعات تکمیلی')
    ) p(Path, ActionAccessType, ActionAccessItemType, DisplayName)
    WHERE NOT EXISTS (
        SELECT 1
        FROM system.RoleAccess ra
        WHERE ra.RoleId = @RoleId
          AND ra.Path = p.Path
          AND ra.ActionAccessType = p.ActionAccessType
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT r.Id, r.Name, r.Title
FROM system.Role r
WHERE r.Name = N'Edms.Document.EngineeringManager';

SELECT u.Id, u.UserName
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE u.UserName = N'khodakarami.f'
  AND TRY_CAST(j.value AS bigint) = (SELECT Id FROM system.Role WHERE Name = N'Edms.Document.EngineeringManager');

SELECT q.Id, q.Name, q.Title
FROM system.SavedQuery q
WHERE q.Name = N'Edms_Document_EngineeringManagerReviewCartable';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_Document_EngineeringManagerReviewCartable'
ORDER BY CAST(j.[key] AS int);
