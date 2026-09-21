/*
================================================================================
Seed_BpmDocumentSystem_DataProfiles.sql
فاز ۸ — نقش، نمایه داده (ACL روی گرید datatableprofile)، RoleAccess و منو.

گرید Panel به /System/FetchDataProfile می‌زند. فیلتر کارتابل/محرمانگی در SavedQuery
است (معادل FetchData / FetchManageData / FetchPublishedData).

پس از اجرا WebApp را Restart کنید.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-bpm-document-system';

    DECLARE @EntityFolder NVARCHAR(200) = N'Entities.App.Gnr.DocumentFolder';
    DECLARE @EntityOrgDoc NVARCHAR(200) = N'Entities.App.Gnr.OrganizationalDocument';
    DECLARE @EntityRequest NVARCHAR(200) = N'Entities.App.Bpm.ProcessDocument.Request';
    DECLARE @EntityManage NVARCHAR(200) = N'Entities.App.Bpm.ProcessDocument.Manage';
    DECLARE @EntityPublished NVARCHAR(200) = N'Entities.App.Bpm.ProcessDocument.Published';
    DECLARE @EntityProcess NVARCHAR(200) = N'Entities.App.Bpm.ProcessDocument';

    PRINT N'=== [1/5] نقش‌ها ===';

    IF OBJECT_ID('tempdb..#BpmRoles') IS NOT NULL DROP TABLE #BpmRoles;
    CREATE TABLE #BpmRoles (Id BIGINT NOT NULL PRIMARY KEY, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #BpmRoles (Id, Name, Title) VALUES
        (400000, N'Bpm.ProcessDocument.ShowAll', N'اسناد فرآیندی - نمایش همه'),
        (400001, N'Bpm.ProcessDocument.Supervisor', N'اسناد فرآیندی - سرپرست سیستم‌ها و روش‌ها'),
        (400002, N'Bpm.ProcessDocument.Expert', N'اسناد فرآیندی - کارشناس سیستم‌ها و روش‌ها'),
        (400003, N'Bpm.ProcessDocument.ViewManage', N'اسناد فرآیندی - مشاهده مدیریت درخواست‌ها'),
        (400004, N'Bpm.ProcessDocument.PublishedList', N'اسناد فرآیندی - لیست اسناد ابلاغ‌شده'),
        (400005, N'Bpm.ProcessDocument.ViewAllAttachments', N'اسناد فرآیندی - مشاهده همه پیوست‌ها');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE role_cur CURSOR LOCAL FAST_FORWARD FOR SELECT Id, Name, Title FROM #BpmRoles;
    OPEN role_cur;
    FETCH NEXT FROM role_cur INTO @Rid, @RName, @RTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RName)
        BEGIN
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@Rid, @RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
            PRINT N'  CREATED ' + @RName;
        END
        FETCH NEXT FROM role_cur INTO @Rid, @RName, @RTitle;
    END
    CLOSE role_cur;
    DEALLOCATE role_cur;

    DECLARE @IdShowAll BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.ShowAll');
    DECLARE @IdSupervisor BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.Supervisor');
    DECLARE @IdExpert BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.Expert');
    DECLARE @IdShowAllMenus BIGINT = (SELECT TOP 1 Id FROM system.Role WHERE Name = N'ShowAllMenus');

    IF @IdShowAll IS NULL OR @IdSupervisor IS NULL OR @IdExpert IS NULL
        THROW 51010, N'نقش‌های BPM ساخته نشدند.', 1;

    PRINT N'=== [2/5] نمایه‌های DataProfile ===';

    DECLARE @DeclarePd NVARCHAR(MAX) = N'
DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
DECLARE @CuRoleIds NVARCHAR(MAX);
DECLARE @CuOrg BIGINT;
SELECT @CuRoles = Roles, @CuRoleIds = RoleIds, @CuOrg = OrgUnitId FROM system.[User] WHERE Id = @CuId;
DECLARE @IsAdmin BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoles) WHERE [value] = N''admin'') THEN 1 ELSE 0 END;
DECLARE @HasShowAll BIT = CASE WHEN @IsAdmin = 1 OR EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = '
        + CAST(@IdShowAll AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasSupervisor BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = '
        + CAST(@IdSupervisor AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasExpert BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = '
        + CAST(@IdExpert AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
--!--mainsection
';

    DECLARE @PdSelect NVARCHAR(MAX) = N'
SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[LastStatus] AS [t1_LastStatus],
    [t1].[DocumentTitle] AS [t1_DocumentTitle],
    [t1].[DocumentNumber] AS [t1_DocumentNumber],
    [t1].[Revision] AS [t1_Revision],
    [t1].[RequestType] AS [t1_RequestType],
    [t1].[DocumentType] AS [t1_DocumentType],
    [t1].[AccessLevel] AS [t1_AccessLevel],
    [t1].[Source] AS [t1_Source],
    [t1].[Priority] AS [t1_Priority],
    [t1].[ProcessSet] AS [t1_ProcessSet],
    [t1].[NotificationShamsiDate] AS [t1_NotificationShamsiDate],
    [t1].[IsDeprecate] AS [t1_IsDeprecate],
    [t1].[CreatedById] AS [t1_CreatedById],
    [t1].[ModifiedById] AS [t1_ModifiedById],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ProcessOwnerId] AS [t1_ProcessOwnerId],
    [t1].[ApproverId] AS [t1_ApproverId],
    [t1].[FinalApproverId] AS [t1_FinalApproverId],
    [t2].[Name] AS [t2_Name],
    [t3].[Name] AS [t3_Name],
    [t4].[Name] AS [t4_Name],
    [t5].[Name] AS [t5_Name]
FROM [Bpm].[ProcessDocument] AS [t1]
LEFT JOIN [system].[User] AS [t2] ON [t1].[RequesterUserId] = [t2].[Id]
LEFT JOIN [system].[User] AS [t3] ON [t1].[ProcessOwnerId] = [t3].[Id]
LEFT JOIN [system].[User] AS [t4] ON [t1].[ApproverId] = [t4].[Id]
LEFT JOIN [system].[User] AS [t5] ON [t1].[FinalApproverId] = [t5].[Id]';

    DECLARE @WhereRequest NVARCHAR(MAX) = N'
WHERE
    @HasShowAll = 1
    OR ((@HasSupervisor = 1 OR @HasExpert = 1) AND ISNULL([t1].[LastStatus], 0) <> 18)
    OR (([t1].[CreatedById] = @CuId OR [t1].[ModifiedById] = @CuId) AND ISNULL([t1].[LastStatus], 0) <> 18)';

    DECLARE @WhereManage NVARCHAR(MAX) = N'
WHERE
    @HasShowAll = 1
    OR (@HasSupervisor = 1 AND (
            [t1].[LastStatus] IN (1, 2, 20, 12, 14, 17, 16, 18, 19)
            OR [t1].[LastStatus] = 10
            OR ([t1].[LastStatus] = 11 AND [t1].[ProcessOwnerId] = @CuId)
            OR ([t1].[LastStatus] = 13 AND [t1].[ApproverId] = @CuId)
            OR ([t1].[LastStatus] = 15 AND [t1].[FinalApproverId] = @CuId)))
    OR (@HasExpert = 1 AND @HasSupervisor = 0 AND [t1].[LastStatus] IN (10, 21, 24, 1, 2, 20, 12, 14, 17, 16, 18, 19))
    OR (@HasSupervisor = 0 AND @HasExpert = 0 AND (
            ([t1].[LastStatus] = 11 AND [t1].[ProcessOwnerId] = @CuId)
            OR ([t1].[LastStatus] = 13 AND [t1].[ApproverId] = @CuId)
            OR ([t1].[LastStatus] = 15 AND [t1].[FinalApproverId] = @CuId)))';

    DECLARE @WherePublished NVARCHAR(MAX) = N'
WHERE
    @IsAdmin = 1
    OR (@HasShowAll = 1 AND [t1].[LastStatus] = 18)
    OR ([t1].[LastStatus] = 18 AND [t1].[IsDeprecate] = 0 AND [t1].[RequestType] <> 1720
        AND (([t1].[AccessLevel] NOT IN (1737, 1738))
            OR ([t1].[AccessLevel] = 1737 AND ([t1].[CreatedById] = @CuId
                OR EXISTS (SELECT 1 FROM [Bpm].[ProcessDocumentNotificationRecipient] r WHERE r.[ProcessDocumentId] = [t1].[Id] AND r.[UserId] = @CuId)))
            OR ([t1].[AccessLevel] = 1738 AND ([t1].[CreatedById] = @CuId
                OR EXISTS (SELECT 1 FROM [Bpm].[ProcessDocumentExecuterOrgUnit] e WHERE e.[ProcessDocumentId] = [t1].[Id] AND e.[OrgUnitId] = @CuOrg)))))';

    DECLARE @DeclareOrg NVARCHAR(MAX) = N'
DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
DECLARE @CuOrg BIGINT;
SELECT @CuRoles = Roles, @CuOrg = OrgUnitId FROM system.[User] WHERE Id = @CuId;
DECLARE @IsAdmin BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoles) WHERE [value] = N''admin'') THEN 1 ELSE 0 END;
--!--mainsection
';

    DECLARE @OrgSelect NVARCHAR(MAX) = N'
SELECT [t1].[Id] AS [t1_Id], [t1].[Title] AS [t1_Title], [t1].[TitleInLatin] AS [t1_TitleInLatin],
    [t1].[Brand] AS [t1_Brand], [t1].[Year] AS [t1_Year], [t1].[Revision] AS [t1_Revision],
    [t1].[Comment] AS [t1_Comment], [t1].[FileId] AS [t1_FileId],
    [t2].[Title] AS [t2_Title], [t2].[IsPrivate] AS [t2_IsPrivate],
    [t3].[OriginalName] AS [t3_OriginalName], [t3].[Size] AS [t3_Size]
FROM [Gnr].[OrganizationalDocument] AS [t1]
INNER JOIN [Gnr].[DocumentFolder] AS [t2] ON [t1].[FolderId] = [t2].[Id]
LEFT JOIN [dbo].[FileEntity] AS [t3] ON [t1].[FileId] = [t3].[Id]
WHERE @IsAdmin = 1 OR [t2].[IsPrivate] = 0 OR [t2].[OrganizationUnitId] = @CuOrg
    OR EXISTS (SELECT 1 FROM [Gnr].[DocumentFolderUser] u WHERE u.[DocumentFolderId] = [t2].[Id] AND u.[UserId] = @CuId)';

    DECLARE @FolderSelect NVARCHAR(MAX) = N'
SELECT [t1].[Id] AS [t1_Id], [t1].[Title] AS [t1_Title], [t1].[FolderTitle] AS [t1_FolderTitle],
    [t1].[FolderLevel] AS [t1_FolderLevel], [t1].[IsPrivate] AS [t1_IsPrivate], [t1].[Comment] AS [t1_Comment],
    [t2].[Title] AS [t2_Title]
FROM [Gnr].[DocumentFolder] AS [t1]
LEFT JOIN [Hrm].[OrgUnit] AS [t2] ON [t1].[OrganizationUnitId] = [t2].[Id]
WHERE [t1].[FolderLevel] = 1 OR [t1].[ParentId] IS NULL';

    DECLARE @Col NVARCHAR(MAX);
    DECLARE @PdColumnsJson NVARCHAR(MAX);
    DECLARE @OrgColumnsJson NVARCHAR(MAX);
    DECLARE @FolderColumnsJson NVARCHAR(MAX);

    ;WITH C AS (
        SELECT * FROM (VALUES
            (N'Bpm.ProcessDocument', N'Id', N'شناسه', N't1_Id', N'[t1].[Id]', N'Long', 6, 0, 1, 80),
            (N'Bpm.ProcessDocument', N'LastStatus', N'وضعیت', N't1_LastStatus', N'[t1].[LastStatus]', N'Select', 8, 1, 0, 200),
            (N'Bpm.ProcessDocument', N'DocumentTitle', N'عنوان سند', N't1_DocumentTitle', N'[t1].[DocumentTitle]', N'String', 0, 1, 0, 240),
            (N'Bpm.ProcessDocument', N'DocumentNumber', N'شماره سند', N't1_DocumentNumber', N'[t1].[DocumentNumber]', N'String', 0, 1, 0, 140),
            (N'Bpm.ProcessDocument', N'Revision', N'بازنگری', N't1_Revision', N'[t1].[Revision]', N'Int', 7, 1, 0, 80),
            (N'Bpm.ProcessDocument', N'RequestType', N'نوع درخواست', N't1_RequestType', N'[t1].[RequestType]', N'Select', 8, 1, 0, 140),
            (N'Bpm.ProcessDocument', N'DocumentType', N'نوع سند', N't1_DocumentType', N'[t1].[DocumentType]', N'Select', 8, 1, 0, 160),
            (N'Bpm.ProcessDocument', N'AccessLevel', N'سطح دسترسی', N't1_AccessLevel', N'[t1].[AccessLevel]', N'Select', 8, 1, 0, 120),
            (N'Bpm.ProcessDocument', N'Source', N'ورودی سند', N't1_Source', N'[t1].[Source]', N'Select', 8, 1, 0, 180),
            (N'Bpm.ProcessDocument', N'Priority', N'فوریت', N't1_Priority', N'[t1].[Priority]', N'Select', 8, 1, 0, 120),
            (N'Bpm.ProcessDocument', N'ProcessSet', N'مجموعه فرآیندی', N't1_ProcessSet', N'[t1].[ProcessSet]', N'Select', 8, 1, 0, 200),
            (N'Bpm.ProcessDocument', N'NotificationShamsiDate', N'تاریخ ابلاغ', N't1_NotificationShamsiDate', N'[t1].[NotificationShamsiDate]', N'DateShamsi', 5, 1, 0, 140),
            (N'Bpm.ProcessDocument', N'IsDeprecate', N'منسوخ', N't1_IsDeprecate', N'[t1].[IsDeprecate]', N'Boolean', 1, 0, 0, 80),
            (N'Bpm.ProcessDocument', N'CreatedByName', N'ایجادکننده', N't1_CreatedByName', N'[t1].[CreatedByName]', N'String', 0, 1, 0, 140),
            (N'Bpm.ProcessDocument', N'CreatedOnShamsiDateTime', N'تاریخ ایجاد', N't1_CreatedOnShamsiDateTime', N'[t1].[CreatedOnShamsiDateTime]', N'DateTimeShamsi', 4, 1, 0, 140),
            (N'system.User', N'Name', N'درخواست‌کننده', N't2_Name', N'[t2].[Name]', N'String', 0, 1, 0, 140),
            (N'system.User', N'Name', N'مالک فرآیند', N't3_Name', N'[t3].[Name]', N'String', 0, 1, 0, 140),
            (N'system.User', N'Name', N'تاییدکننده', N't4_Name', N'[t4].[Name]', N'String', 0, 1, 0, 140),
            (N'system.User', N'Name', N'تصویب‌کننده', N't5_Name', N'[t5].[Name]', N'String', 0, 1, 0, 140)
        ) v(TableName, ColumnName, DisplayName, Alliance, Address, SystemTypeName, SystemType, Visible, PrimaryKey, Width)
    )
    SELECT @PdColumnsJson = N'[' + STUFF((
        SELECT N',' + (
            N'{"TableName":"' + TableName + N'","ColumnName":"' + ColumnName + N'","DisplayName":"' + DisplayName
            + N'","Alliance":"' + Alliance + N'","Address":"' + Address + N'","SystemTypeName":"' + SystemTypeName
            + N'","SelectIndex":0,"Visible":' + CASE Visible WHEN 1 THEN N'true' ELSE N'false' END
            + N',"PrimaryKey":' + CASE PrimaryKey WHEN 1 THEN N'true' ELSE N'false' END
            + N',"SystemType":' + CAST(SystemType AS NVARCHAR(10))
            + N',"SortDirection":' + CASE PrimaryKey WHEN 1 THEN N'1' ELSE N'null' END
            + N',"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":'
            + CASE
                WHEN ColumnName = N'LastStatus' THEN N'{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Bpm.Enums.ProcessDocumentStatusEnum"}'
                WHEN ColumnName = N'RequestType' THEN N'{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Bpm.Enums.ProcessDocumentRequestTypeEnum"}'
                WHEN ColumnName = N'DocumentType' THEN N'{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Bpm.Enums.ProcessDocumentTypeEnum"}'
                WHEN ColumnName = N'AccessLevel' THEN N'{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Bpm.Enums.ProcessDocumentAccessLevelEnum"}'
                WHEN ColumnName = N'Source' THEN N'{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Bpm.Enums.ProcessDocumentSourceEnum"}'
                WHEN ColumnName = N'Priority' THEN N'{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Bpm.Enums.ProcessDocumentPriorityEnum"}'
                WHEN ColumnName = N'ProcessSet' THEN N'{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Bpm.Enums.ProcessDocumentProcessSetEnum"}'
                ELSE N'null'
              END
            + N',"Aggregate":null,"Render":'
            + CASE WHEN SystemTypeName = N'Select' THEN N'"function(data, type, row) { return data; }"' ELSE N'null' END
            + N',"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":'
            + CAST(Width AS NVARCHAR(10)) + N',"Filterable":true,"Sortable":true,"ClassName":""}'
        )
        FROM C
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

    ;WITH C AS (
        SELECT * FROM (VALUES
            (N'Gnr.OrganizationalDocument', N'Id', N'شناسه', N't1_Id', N'[t1].[Id]', N'Long', 6, 0, 1, 80),
            (N'Gnr.OrganizationalDocument', N'Title', N'عنوان', N't1_Title', N'[t1].[Title]', N'String', 0, 1, 0, 220),
            (N'Gnr.OrganizationalDocument', N'TitleInLatin', N'عنوان لاتین', N't1_TitleInLatin', N'[t1].[TitleInLatin]', N'String', 0, 1, 0, 200),
            (N'Gnr.DocumentFolder', N'Title', N'پوشه', N't2_Title', N'[t2].[Title]', N'String', 0, 1, 0, 200),
            (N'Gnr.OrganizationalDocument', N'Brand', N'برند', N't1_Brand', N'[t1].[Brand]', N'String', 0, 1, 0, 120),
            (N'Gnr.OrganizationalDocument', N'Year', N'سال', N't1_Year', N'[t1].[Year]', N'Int', 7, 1, 0, 80),
            (N'Gnr.OrganizationalDocument', N'Revision', N'بازنگری', N't1_Revision', N'[t1].[Revision]', N'Int', 7, 1, 0, 80),
            (N'dbo.FileEntity', N'OriginalName', N'نام فایل', N't3_OriginalName', N'[t3].[OriginalName]', N'String', 0, 1, 0, 200),
            (N'Gnr.OrganizationalDocument', N'Comment', N'توضیحات', N't1_Comment', N'[t1].[Comment]', N'String', 0, 1, 0, 200)
        ) v(TableName, ColumnName, DisplayName, Alliance, Address, SystemTypeName, SystemType, Visible, PrimaryKey, Width)
    )
    SELECT @OrgColumnsJson = N'[' + STUFF((
        SELECT N',' + (
            N'{"TableName":"' + TableName + N'","ColumnName":"' + ColumnName + N'","DisplayName":"' + DisplayName
            + N'","Alliance":"' + Alliance + N'","Address":"' + Address + N'","SystemTypeName":"' + SystemTypeName
            + N'","SelectIndex":0,"Visible":' + CASE Visible WHEN 1 THEN N'true' ELSE N'false' END
            + N',"PrimaryKey":' + CASE PrimaryKey WHEN 1 THEN N'true' ELSE N'false' END
            + N',"SystemType":' + CAST(SystemType AS NVARCHAR(10))
            + N',"SortDirection":' + CASE PrimaryKey WHEN 1 THEN N'1' ELSE N'null' END
            + N',"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":'
            + CAST(Width AS NVARCHAR(10)) + N',"Filterable":true,"Sortable":true,"ClassName":""}'
        )
        FROM C
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

    ;WITH C AS (
        SELECT * FROM (VALUES
            (N'Gnr.DocumentFolder', N'Id', N'شناسه', N't1_Id', N'[t1].[Id]', N'Long', 6, 0, 1, 80),
            (N'Gnr.DocumentFolder', N'Title', N'عنوان', N't1_Title', N'[t1].[Title]', N'String', 0, 1, 0, 240),
            (N'Gnr.DocumentFolder', N'FolderTitle', N'عنوان پوشه', N't1_FolderTitle', N'[t1].[FolderTitle]', N'String', 0, 1, 0, 180),
            (N'Hrm.OrgUnit', N'Title', N'واحد سازمانی', N't2_Title', N'[t2].[Title]', N'String', 0, 1, 0, 200),
            (N'Gnr.DocumentFolder', N'IsPrivate', N'خصوصی', N't1_IsPrivate', N'[t1].[IsPrivate]', N'Boolean', 1, 1, 0, 90),
            (N'Gnr.DocumentFolder', N'Comment', N'توضیحات', N't1_Comment', N'[t1].[Comment]', N'String', 0, 1, 0, 240)
        ) v(TableName, ColumnName, DisplayName, Alliance, Address, SystemTypeName, SystemType, Visible, PrimaryKey, Width)
    )
    SELECT @FolderColumnsJson = N'[' + STUFF((
        SELECT N',' + (
            N'{"TableName":"' + TableName + N'","ColumnName":"' + ColumnName + N'","DisplayName":"' + DisplayName
            + N'","Alliance":"' + Alliance + N'","Address":"' + Address + N'","SystemTypeName":"' + SystemTypeName
            + N'","SelectIndex":0,"Visible":' + CASE Visible WHEN 1 THEN N'true' ELSE N'false' END
            + N',"PrimaryKey":' + CASE PrimaryKey WHEN 1 THEN N'true' ELSE N'false' END
            + N',"SystemType":' + CAST(SystemType AS NVARCHAR(10))
            + N',"SortDirection":' + CASE PrimaryKey WHEN 1 THEN N'1' ELSE N'null' END
            + N',"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":'
            + CAST(Width AS NVARCHAR(10)) + N',"Filterable":true,"Sortable":true,"ClassName":""}'
        )
        FROM C
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

    DECLARE @ActionsCrud NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @ActionsEdit NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @ActionsNone NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    IF OBJECT_ID('tempdb..#BpmProfiles') IS NOT NULL DROP TABLE #BpmProfiles;
    CREATE TABLE #BpmProfiles (
        Name NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        EntityFullName NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        ColumnsJson NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL,
        SavedQueryId BIGINT NULL
    );

    INSERT INTO #BpmProfiles (Name, Title, EntityFullName, CustomQuery, ColumnsJson, ActionOptions) VALUES
        (N'Gnr_DocumentFolder_List', N'پوشه سطح مدرک', @EntityFolder, @FolderSelect, @FolderColumnsJson, @ActionsCrud),
        (N'Gnr_OrganizationalDocument_List', N'اسناد سازمانی', @EntityOrgDoc, @DeclareOrg + @OrgSelect, @OrgColumnsJson, @ActionsCrud),
        (N'Bpm_ProcessDocument_Request', N'کارتابل درخواست ایجاد/تغییر', @EntityRequest, @DeclarePd + @PdSelect + @WhereRequest, @PdColumnsJson, @ActionsCrud),
        (N'Bpm_ProcessDocument_Manage', N'کارتابل مدیریت درخواست‌ها', @EntityManage, @DeclarePd + @PdSelect + @WhereManage, @PdColumnsJson, @ActionsEdit),
        (N'Bpm_ProcessDocument_Published', N'لیست اسناد فرآیندی ابلاغ‌شده', @EntityPublished, @DeclarePd + @PdSelect + @WherePublished, @PdColumnsJson, @ActionsNone);

    DECLARE @PName NVARCHAR(100), @PTitle NVARCHAR(200), @PEntity NVARCHAR(200),
            @PQuery NVARCHAR(MAX), @PCols NVARCHAR(MAX), @PActions NVARCHAR(MAX), @PId BIGINT, @PQueryJson NVARCHAR(MAX);

    DECLARE pcur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, EntityFullName, CustomQuery, ColumnsJson, ActionOptions FROM #BpmProfiles;
    OPEN pcur;
    FETCH NEXT FROM pcur INTO @PName, @PTitle, @PEntity, @PQuery, @PCols, @PActions;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @PQueryJson = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PQuery);
        SELECT @PId = Id FROM system.SavedQuery WHERE Name = @PName;
        IF @PId IS NOT NULL
            UPDATE system.SavedQuery
            SET Title = @PTitle, QueryJson = @PQueryJson, ColumnsJson = @PCols, EntityFullName = @PEntity,
                ActionOptions = @PActions, Mode = 1, Type = 1, IsActive = 1,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @PId;
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
                (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
                 Mode, Type, EntityFullName, ActionOptions,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@PName, @PTitle, @PQueryJson, @PCols, N'{}', N'[]', NULL, 1, 1, @PEntity, @PActions,
                 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET @PId = SCOPE_IDENTITY();
        END
        UPDATE #BpmProfiles SET SavedQueryId = @PId WHERE Name = @PName;
        SET @PId = NULL;
        FETCH NEXT FROM pcur INTO @PName, @PTitle, @PEntity, @PQuery, @PCols, @PActions;
    END
    CLOSE pcur;
    DEALLOCATE pcur;

    PRINT N'=== [2b] دکمه‌های دانلود نمایه Published ===';

    DECLARE @ScriptDownloadFinal NVARCHAR(MAX) = N'function(ctx) {
    var id = ctx.primaryKeyValue
        || (ctx.primaryKeyValues && ctx.primaryKeyValues[0])
        || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.t1_id || ctx.selectedRow.id || ctx.selectedRow.Id));
    if (!id) {
        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');
        return;
    }
    window.open(''/Panel/Bpm/ProcessDocument/DownloadPublishedFinal?id='' + id, ''_blank'');
}';

    DECLARE @ScriptDownloadPdf NVARCHAR(MAX) = N'function(ctx) {
    var id = ctx.primaryKeyValue
        || (ctx.primaryKeyValues && ctx.primaryKeyValues[0])
        || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.t1_id || ctx.selectedRow.id || ctx.selectedRow.Id));
    if (!id) {
        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');
        return;
    }
    window.open(''/Panel/Bpm/ProcessDocument/DownloadPublishedFinalPdf?id='' + id, ''_blank'');
}';

    DECLARE @ScriptOnSelectedRow NVARCHAR(MAX) = N'function(ctx) {
    var btns = ctx.dataActionBtns || {};
    var $final = btns.downloadPublishedFinal;
    var $pdf = btns.downloadPublishedFinalPdf;
    function setBtns(canFinal, canPdf) {
        if ($final && $final.prop) $final.prop(''disabled'', !canFinal);
        if ($pdf && $pdf.prop) $pdf.prop(''disabled'', !canPdf);
    }
    var id = ctx.primaryKeyValue
        || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.t1_id || ctx.selectedRow.id || ctx.selectedRow.Id));
    if (ctx.isDeselect || !id) {
        setBtns(false, false);
        return;
    }
    setBtns(false, false);
    get(''/Panel/Bpm/ProcessDocument/GetPublishedDownloadInfo?id='' + id, function (r) {
        if (!r || !r.isSuccess || !r.data) {
            setBtns(false, false);
            return;
        }
        setBtns(!!r.data.canDownloadFinal, !!r.data.canDownloadFinalPdf);
    });
}';

    DECLARE @BtnFinal NVARCHAR(MAX) = (
        SELECT
            N'id_downloadPublishedFinal' AS id,
            N'دانلود فایل نهایی' AS title,
            N'downloadPublishedFinal' AS dataActionName,
            N'btn-color-primary' AS colorClass,
            N'fa fa-file' AS iconClass,
            CAST(1 AS bit) AS requiresSelection,
            CAST(0 AS bit) AS requiresMultiSelection,
            CAST(0 AS bit) AS useHtml,
            N'' AS html,
            @ScriptDownloadFinal AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    DECLARE @BtnPdf NVARCHAR(MAX) = (
        SELECT
            N'id_downloadPublishedFinalPdf' AS id,
            N'دانلود PDF نهایی' AS title,
            N'downloadPublishedFinalPdf' AS dataActionName,
            N'btn-color-danger' AS colorClass,
            N'fa fa-file-pdf' AS iconClass,
            CAST(1 AS bit) AS requiresSelection,
            CAST(0 AS bit) AS requiresMultiSelection,
            CAST(0 AS bit) AS useHtml,
            N'' AS html,
            @ScriptDownloadPdf AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    DECLARE @PublishedCustomButtons NVARCHAR(MAX) = N'[' + @BtnFinal + N',' + @BtnPdf + N']';

    DECLARE @PublishedEventScripts NVARCHAR(MAX) = (
        SELECT
            @ScriptOnSelectedRow AS onSelectedRow,
            CAST(N'' AS nvarchar(max)) AS onRowAdded
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    UPDATE system.SavedQuery
    SET CustomActionButtonsJson = @PublishedCustomButtons,
        EventScriptsJson = @PublishedEventScripts,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'Bpm_ProcessDocument_Published';

    IF @@ROWCOUNT = 0
        THROW 51011, N'نمایه Bpm_ProcessDocument_Published برای دکمه‌های دانلود یافت نشد.', 1;

    PRINT N'=== [3/5] RoleAccess نمایه ===';


    DELETE ra FROM system.RoleAccess ra
    INNER JOIN #BpmProfiles p ON p.SavedQueryId = ra.RowId
    WHERE ra.ActionAccessType = 3;

    IF OBJECT_ID('tempdb..#BpmProfileRoles') IS NOT NULL DROP TABLE #BpmProfileRoles;
    CREATE TABLE #BpmProfileRoles (ProfileName NVARCHAR(100) NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    -- آرشیو سازمانی و کارتابل درخواست: فقط نقش‌های عملیاتی DMS (نه PublishedList/ViewManage).
    -- درخواست‌دهنده عادی باید List + این نمایه را از /panel/Role/List بگیرد.
    INSERT INTO #BpmProfileRoles (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM (VALUES
        (N'Gnr_DocumentFolder_List'),
        (N'Gnr_OrganizationalDocument_List'),
        (N'Bpm_ProcessDocument_Request')
    ) p(Name)
    CROSS JOIN (VALUES
        (N'Bpm.ProcessDocument.ShowAll'),
        (N'Bpm.ProcessDocument.Supervisor'),
        (N'Bpm.ProcessDocument.Expert')
    ) r(Name);

    INSERT INTO #BpmProfileRoles (ProfileName, RoleName) VALUES
        (N'Bpm_ProcessDocument_Manage', N'Bpm.ProcessDocument.ShowAll'),
        (N'Bpm_ProcessDocument_Manage', N'Bpm.ProcessDocument.Supervisor'),
        (N'Bpm_ProcessDocument_Manage', N'Bpm.ProcessDocument.Expert'),
        (N'Bpm_ProcessDocument_Manage', N'Bpm.ProcessDocument.ViewManage'),
        (N'Bpm_ProcessDocument_Published', N'Bpm.ProcessDocument.ShowAll'),
        (N'Bpm_ProcessDocument_Published', N'Bpm.ProcessDocument.Supervisor'),
        (N'Bpm_ProcessDocument_Published', N'Bpm.ProcessDocument.Expert'),
        (N'Bpm_ProcessDocument_Published', N'Bpm.ProcessDocument.PublishedList'),
        (N'Bpm_ProcessDocument_Published', N'Bpm.ProcessDocument.ViewAllAttachments');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(p.SavedQueryId AS NVARCHAR(20)), 3, 7, p.EntityFullName, p.Title, NULL, p.SavedQueryId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #BpmProfileRoles map
    INNER JOIN #BpmProfiles p ON p.Name = map.ProfileName
    INNER JOIN system.Role r ON r.Name = map.RoleName;

    PRINT N'=== [4/5] RoleAccess اکشن ===';

    DELETE FROM system.RoleAccess
    WHERE ActionAccessType IN (1, 2)
      AND (Path LIKE N'/panel/bpm/processdocument/%'
        OR Path LIKE N'/panel/gnr/documentfolder/%'
        OR Path LIKE N'/panel/gnr/organizationaldocument/%');

    IF OBJECT_ID('tempdb..#BpmActionAccess') IS NOT NULL DROP TABLE #BpmActionAccess;
    CREATE TABLE #BpmActionAccess (
        RoleName NVARCHAR(200) NOT NULL, Path NVARCHAR(300) NOT NULL,
        ActionAccessType INT NOT NULL, ActionAccessItemType INT NOT NULL, EntityName NVARCHAR(200) NOT NULL
    );

    INSERT INTO #BpmActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.Name, a.Path, a.T, a.I, a.E
    FROM (VALUES
        (N'Bpm.ProcessDocument.ShowAll'),
        (N'Bpm.ProcessDocument.Supervisor'),
        (N'Bpm.ProcessDocument.Expert')
    ) r(Name)
    CROSS JOIN (VALUES
        (N'/panel/gnr/documentfolder/list', 1, 1, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/edit', 1, 5, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/new', 1, 4, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/fetchdata', 2, 2, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/save', 2, 3, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/add', 2, 4, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/update', 2, 5, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/delete', 2, 6, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/exporttoexcel', 2, 0, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/getchildren', 2, 2, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/documentfolder/savechild', 2, 3, N'Entities.App.Gnr.DocumentFolder'),
        (N'/panel/gnr/organizationaldocument/list', 1, 1, N'Entities.App.Gnr.OrganizationalDocument'),
        (N'/panel/gnr/organizationaldocument/edit', 1, 5, N'Entities.App.Gnr.OrganizationalDocument'),
        (N'/panel/gnr/organizationaldocument/new', 1, 4, N'Entities.App.Gnr.OrganizationalDocument'),
        (N'/panel/gnr/organizationaldocument/fetchdata', 2, 2, N'Entities.App.Gnr.OrganizationalDocument'),
        (N'/panel/gnr/organizationaldocument/save', 2, 3, N'Entities.App.Gnr.OrganizationalDocument'),
        (N'/panel/gnr/organizationaldocument/add', 2, 4, N'Entities.App.Gnr.OrganizationalDocument'),
        (N'/panel/gnr/organizationaldocument/update', 2, 5, N'Entities.App.Gnr.OrganizationalDocument'),
        (N'/panel/gnr/organizationaldocument/delete', 2, 6, N'Entities.App.Gnr.OrganizationalDocument'),
        (N'/panel/gnr/organizationaldocument/download', 1, 1000, N'Entities.App.Gnr.OrganizationalDocument'),
        (N'/panel/gnr/organizationaldocument/exporttoexcel', 2, 0, N'Entities.App.Gnr.OrganizationalDocument')
    ) AS a(Path, T, I, E);

    INSERT INTO #BpmActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, a.T, a.I, @EntityProcess
    FROM (VALUES (N'Bpm.ProcessDocument.ShowAll'), (N'Bpm.ProcessDocument.Supervisor'), (N'Bpm.ProcessDocument.Expert')) r(RoleName)
    CROSS JOIN (VALUES
        (N'/panel/bpm/processdocument/list', 1, 1),
        (N'/panel/bpm/processdocument/edit', 1, 5),
        (N'/panel/bpm/processdocument/new', 1, 4),
        (N'/panel/bpm/processdocument/fetchdata', 2, 2),
        (N'/panel/bpm/processdocument/save', 2, 3),
        (N'/panel/bpm/processdocument/add', 2, 4),
        (N'/panel/bpm/processdocument/update', 2, 5),
        (N'/panel/bpm/processdocument/delete', 2, 6),
        (N'/panel/bpm/processdocument/cancelrequest', 2, 5),
        (N'/panel/bpm/processdocument/downloadmain', 1, 1000),
        (N'/panel/bpm/processdocument/downloadtemp', 1, 1000),
        (N'/panel/bpm/processdocument/exporttoexcel', 2, 0)
    ) a(Path, T, I);

    INSERT INTO #BpmActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, a.T, a.I, @EntityProcess
    FROM (VALUES (N'Bpm.ProcessDocument.ShowAll'), (N'Bpm.ProcessDocument.Supervisor'),
                 (N'Bpm.ProcessDocument.Expert'), (N'Bpm.ProcessDocument.ViewManage')) r(RoleName)
    CROSS JOIN (VALUES
        (N'/panel/bpm/processdocument/manage', 1, 1),
        (N'/panel/bpm/processdocument/manageedit', 1, 1000),
        (N'/panel/bpm/processdocument/fetchmanagedata', 2, 2)
    ) a(Path, T, I);

    INSERT INTO #BpmActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, a.T, a.I, @EntityProcess
    FROM (VALUES (N'Bpm.ProcessDocument.ShowAll'), (N'Bpm.ProcessDocument.Supervisor')) r(RoleName)
    CROSS JOIN (VALUES
        (N'/panel/bpm/processdocument/savemanage', 2, 5),
        (N'/panel/bpm/processdocument/changestatus', 2, 5),
        (N'/panel/bpm/processdocument/downloadfinal', 1, 1000),
        (N'/panel/bpm/processdocument/downloadfinalpdf', 1, 1000),
        (N'/panel/bpm/processdocument/downloadcomment', 1, 1000)
    ) a(Path, T, I);

    INSERT INTO #BpmActionAccess VALUES
        (N'Bpm.ProcessDocument.Expert', N'/panel/bpm/processdocument/changestatus', 2, 5, @EntityProcess),
        (N'Bpm.ProcessDocument.Expert', N'/panel/bpm/processdocument/downloadcomment', 1, 1000, @EntityProcess);

    INSERT INTO #BpmActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT N'Bpm.ProcessDocument.ViewAllAttachments', a.Path, a.T, a.I, @EntityProcess
    FROM (VALUES
        (N'/panel/bpm/processdocument/downloadmain', 1, 1000),
        (N'/panel/bpm/processdocument/downloadtemp', 1, 1000),
        (N'/panel/bpm/processdocument/downloadfinal', 1, 1000),
        (N'/panel/bpm/processdocument/downloadfinalpdf', 1, 1000),
        (N'/panel/bpm/processdocument/downloadcomment', 1, 1000)
    ) a(Path, T, I);

    INSERT INTO #BpmActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, a.T, a.I, @EntityProcess
    FROM (VALUES (N'Bpm.ProcessDocument.ShowAll'), (N'Bpm.ProcessDocument.Supervisor'),
                 (N'Bpm.ProcessDocument.Expert'), (N'Bpm.ProcessDocument.PublishedList'),
                 (N'Bpm.ProcessDocument.ViewAllAttachments')) r(RoleName)
    CROSS JOIN (VALUES
        (N'/panel/bpm/processdocument/published', 1, 1),
        (N'/panel/bpm/processdocument/fetchpublisheddata', 2, 2),
        (N'/panel/bpm/processdocument/getpublisheddownloadinfo', 1, 1000),
        (N'/panel/bpm/processdocument/downloadpublishedfinal', 1, 1000),
        (N'/panel/bpm/processdocument/downloadpublishedfinalpdf', 1, 1000)
    ) a(Path, T, I);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #BpmActionAccess a
    INNER JOIN system.Role r ON r.Name = a.RoleName;

    PRINT N'=== [5/5] منو ===';

    DECLARE @MenuName NVARCHAR(150) = N'BpmDocuments';
    DECLARE @MenuTitle NVARCHAR(150) = N'سیستم مدیریت اسناد';
    DECLARE @MenuContent NVARCHAR(MAX) = N'[{"text":"اسناد سازمانی","icon":"ki-folder ki-outline","iconColor":"#d78819","path":"","a_attr":{"href":""},"data":{"iconColor":"#d78819","path":""},"children":[{"text":"پوشه سطح مدرک","icon":"ki-folder ki-outline","iconColor":"#1fc4e5","path":"/panel/gnr/documentfolder/list","a_attr":{"href":"/panel/gnr/documentfolder/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/gnr/documentfolder/list"},"children":[]},{"text":"اسناد","icon":"ki-document ki-outline","iconColor":"#1fe57f","path":"/panel/gnr/organizationaldocument/list","a_attr":{"href":"/panel/gnr/organizationaldocument/list"},"data":{"iconColor":"#1fe57f","path":"/panel/gnr/organizationaldocument/list"},"children":[]}]},{"text":"اسناد فرآیندی","icon":"ki-abstract-26 ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[{"text":"درخواست ایجاد/تغییر","icon":"ki-plus-square ki-outline","iconColor":"#1fc4e5","path":"/panel/bpm/processdocument/list","a_attr":{"href":"/panel/bpm/processdocument/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/bpm/processdocument/list"},"children":[]},{"text":"مدیریت درخواست‌ها","icon":"ki-setting-2 ki-outline","iconColor":"#ed230c","path":"/panel/bpm/processdocument/manage","a_attr":{"href":"/panel/bpm/processdocument/manage"},"data":{"iconColor":"#ed230c","path":"/panel/bpm/processdocument/manage"},"children":[]},{"text":"لیست اسناد فرآیندی","icon":"ki-notepad ki-outline","iconColor":"#50dcae","path":"/panel/bpm/processdocument/published","a_attr":{"href":"/panel/bpm/processdocument/published"},"data":{"iconColor":"#50dcae","path":"/panel/bpm/processdocument/published"},"children":[]}]}]';

    DECLARE @AccessRoles NVARCHAR(MAX);
    DECLARE @AccessRoleIds NVARCHAR(MAX);

    SELECT @AccessRoles = N'[' + STUFF((
        SELECT N',"'+ Name + N'"'
        FROM (SELECT Name FROM #BpmRoles UNION ALL SELECT N'ShowAllMenus' WHERE @IdShowAllMenus IS NOT NULL) x
        ORDER BY Name FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

    SELECT @AccessRoleIds = N'[' + STUFF((
        SELECT N',' + CAST(Id AS nvarchar(20))
        FROM (SELECT Id FROM #BpmRoles UNION ALL SELECT @IdShowAllMenus WHERE @IdShowAllMenus IS NOT NULL) x
        ORDER BY Id FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

    IF EXISTS (SELECT 1 FROM system.SystemMenu WHERE Name = @MenuName)
        UPDATE system.SystemMenu
        SET Title = @MenuTitle, Content = @MenuContent, AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @MenuName;
    ELSE
        INSERT INTO system.SystemMenu
            (Title, Name, Content, AccessRoles, AccessRoleIds,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@MenuTitle, @MenuName, @MenuContent, @AccessRoles, @AccessRoleIds,
            1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_BpmDocumentSystem_DataProfiles — Restart WebApp ===';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    BEGIN TRY SET IDENTITY_INSERT system.Role OFF; END TRY BEGIN CATCH END CATCH;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
