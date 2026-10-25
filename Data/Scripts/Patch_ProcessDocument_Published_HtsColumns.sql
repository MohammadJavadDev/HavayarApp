/*
================================================================================
Patch_ProcessDocument_Published_HtsColumns.sql
نمایه داده Bpm_ProcessDocument_Published («اسناد فرآیندی - لیست اسناد») عیناً مطابق HTS
(_BpmDocumentReport.cshtml؛ گرید روی BpmDocumentModel است، پس عنوان‌ها از BpmDocumentModel می‌آیند).

ستون‌ها: شماره درخواست | شماره بازنگری | نوع سند | عنوان سند | کد سند | مجموعه فرآیندی | مالک فرآیند |
  مجری فرآیند | واحد های مرتبط | مستندات مرتبط | تیم بازنگری/تدوین | زمان ابلاغ | سطح محرمانگی
رنگ سلول ردیف: steelblue وقتی IsRevisedAndHasSupervisorApprove (نوع درخواست بازنگری و
  (لاگ «ویرایش سیستم ها و روش ها (۲۰)» دارد یا هیچ گردشی ندارد)).
دکمه‌ها: دانلود فایل نهایی، دانلود فایل نهایی (PDF) (قواعد فعال‌سازی/دسترسی قبلی حفظ می‌شود).
فیلتر دسترسی (ACL) همان قبلی است.

اجرا (UTF-8 با BOM؛ -x چون اسکریپت JS شامل $(...) است):
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -x -i "Data\Scripts\Patch_ProcessDocument_Published_HtsColumns.sql"
پس از اجرا WebApp را Restart کنید (کش Redis نمایه).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-process-document-published-hts';
    DECLARE @ProfileName NVARCHAR(100) = N'Bpm_ProcessDocument_Published';

    DECLARE @IdShowAll BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.ShowAll');
    IF @IdShowAll IS NULL
        THROW 51050, N'نقش Bpm.ProcessDocument.ShowAll یافت نشد.', 1;

    DECLARE @CustomQuery NVARCHAR(MAX) = CAST(N'' AS NVARCHAR(MAX)) + N'
DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
DECLARE @CuRoleIds NVARCHAR(MAX);
DECLARE @CuOrg BIGINT;
SELECT @CuRoles = Roles, @CuRoleIds = RoleIds, @CuOrg = OrgUnitId FROM system.[User] WHERE Id = @CuId;
DECLARE @IsAdmin BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoles) WHERE [value] = N''admin'') THEN 1 ELSE 0 END;
DECLARE @HasShowAll BIT = CASE WHEN @IsAdmin = 1 OR EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = '
        + CAST(@IdShowAll AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
--!--mainsection

SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[Revision] AS [t1_Revision],
    [t1].[DocumentType] AS [t1_DocumentType],
    [t1].[DocumentTitle] AS [t1_DocumentTitle],
    [t1].[DocumentNumber] AS [t1_DocumentNumber],
    [t1].[ProcessSet] AS [t1_ProcessSet],
    COALESCE(NULLIF(LTRIM(RTRIM([t3].[NameFa])), N''''), [t3].[Name]) AS [t3_NameFa],
    (SELECT STRING_AGG([o].[Title], N'','') FROM [Bpm].[ProcessDocumentProcessExecuterOrgUnit] AS [pe]
        INNER JOIN [Hrm].[OrgUnit] AS [o] ON [o].[Id] = [pe].[OrgUnitId]
        WHERE [pe].[ProcessDocumentId] = [t1].[Id]) AS [t1_ProcessExecuters],
    (SELECT STRING_AGG([o].[Title], N'','') FROM [Bpm].[ProcessDocumentRelatedOrgUnit] AS [ro]
        INNER JOIN [Hrm].[OrgUnit] AS [o] ON [o].[Id] = [ro].[OrgUnitId]
        WHERE [ro].[ProcessDocumentId] = [t1].[Id]) AS [t1_RelatedUnits],
    [t1].[RelatedDocumentsText] AS [t1_RelatedDocumentsText],
    (SELECT STRING_AGG(COALESCE(NULLIF(LTRIM(RTRIM([u].[NameFa])), N''''), [u].[Name]), N'','') FROM [Bpm].[ProcessDocumentEditingUser] AS [eu]
        INNER JOIN [system].[User] AS [u] ON [u].[Id] = [eu].[UserId]
        WHERE [eu].[ProcessDocumentId] = [t1].[Id]) AS [t1_EditingTeam],
    [t1].[NotificationShamsiDate] AS [t1_NotificationShamsiDate],
    [t1].[AccessLevel] AS [t1_AccessLevel],
    CASE WHEN [t1].[RequestType] = 1721
              AND (EXISTS (SELECT 1 FROM [Bpm].[ProcessDocumentStatusLog] AS [sl] WHERE [sl].[ProcessDocumentId] = [t1].[Id] AND [sl].[Status] = 20)
                   OR NOT EXISTS (SELECT 1 FROM [Bpm].[ProcessDocumentStatusLog] AS [sl2] WHERE [sl2].[ProcessDocumentId] = [t1].[Id]))
         THEN 1 ELSE 0 END AS [t1_IsRevised]
FROM [Bpm].[ProcessDocument] AS [t1]
LEFT JOIN [system].[User] AS [t3] ON [t1].[ProcessOwnerId] = [t3].[Id]
WHERE
    @IsAdmin = 1
    OR (@HasShowAll = 1 AND [t1].[LastStatus] = 18)
    OR ([t1].[LastStatus] = 18 AND [t1].[IsDeprecate] = 0 AND [t1].[RequestType] <> 1720
        AND (([t1].[AccessLevel] NOT IN (1737, 1738))
            OR ([t1].[AccessLevel] = 1737 AND ([t1].[CreatedById] = @CuId
                OR EXISTS (SELECT 1 FROM [Bpm].[ProcessDocumentNotificationRecipient] r WHERE r.[ProcessDocumentId] = [t1].[Id] AND r.[UserId] = @CuId)))
            OR ([t1].[AccessLevel] = 1738 AND ([t1].[CreatedById] = @CuId
                OR EXISTS (SELECT 1 FROM [Bpm].[ProcessDocumentExecuterOrgUnit] e WHERE e.[ProcessDocumentId] = [t1].[Id] AND e.[OrgUnitId] = @CuOrg)))))';

    DECLARE @RenderSelect NVARCHAR(MAX) = N'function(data, type, row) { return data; }';
    DECLARE @EnumNs NVARCHAR(100) = N'Entities.App.Bpm.Enums.';
    DECLARE @ColumnsJson NVARCHAR(MAX);

    ;WITH C AS (
        SELECT * FROM (VALUES
            (1,  N'Bpm.ProcessDocument', N'Id',                 N'شماره درخواست',      N't1_Id',                    N'[t1].[Id]',                    N'Long',      6, 1, 1, 100, CAST(NULL AS NVARCHAR(100)), 1, 0),
            (2,  N'Bpm.ProcessDocument', N'Revision',           N'شماره بازنگری',      N't1_Revision',              N'[t1].[Revision]',              N'Int',       7, 1, 0,  90, NULL, NULL, NULL),
            (3,  N'Bpm.ProcessDocument', N'DocumentType',       N'نوع سند',            N't1_DocumentType',          N'[t1].[DocumentType]',          N'Select',    8, 1, 0, 120, N'ProcessDocumentTypeEnum', NULL, NULL),
            (4,  N'Bpm.ProcessDocument', N'DocumentTitle',      N'عنوان سند',          N't1_DocumentTitle',         N'[t1].[DocumentTitle]',         N'String',    0, 1, 0, 300, NULL, NULL, NULL),
            (5,  N'Bpm.ProcessDocument', N'DocumentNumber',     N'کد سند',             N't1_DocumentNumber',        N'[t1].[DocumentNumber]',        N'String',    0, 1, 0, 130, NULL, NULL, NULL),
            (6,  N'Bpm.ProcessDocument', N'ProcessSet',         N'مجموعه فرآیندی',     N't1_ProcessSet',            N'[t1].[ProcessSet]',            N'Select',    8, 1, 0, 175, N'ProcessDocumentProcessSetEnum', NULL, NULL),
            (7,  N'system.User',         N'NameFa',             N'مالک فرآیند',        N't3_NameFa',                N'[t3].[NameFa]',                N'String',    0, 1, 0, 160, NULL, NULL, NULL),
            (8,  N'Bpm.ProcessDocument', N't1_ProcessExecuters',N'مجری فرآیند',        N't1_ProcessExecuters',      N'[t1_ProcessExecuters]',        N'String',    0, 1, 0, 200, NULL, NULL, NULL),
            (9,  N'Bpm.ProcessDocument', N't1_RelatedUnits',    N'واحد های مرتبط',     N't1_RelatedUnits',          N'[t1_RelatedUnits]',            N'String',    0, 1, 0, 200, NULL, NULL, NULL),
            (10, N'Bpm.ProcessDocument', N'RelatedDocumentsText',N'مستندات مرتبط',     N't1_RelatedDocumentsText',  N'[t1].[RelatedDocumentsText]',  N'String',    0, 1, 0, 200, NULL, NULL, NULL),
            (11, N'Bpm.ProcessDocument', N't1_EditingTeam',     N'تیم بازنگری/تدوین',  N't1_EditingTeam',           N'[t1_EditingTeam]',             N'String',    0, 1, 0, 200, NULL, NULL, NULL),
            (12, N'Bpm.ProcessDocument', N'NotificationShamsiDate', N'زمان ابلاغ',     N't1_NotificationShamsiDate',N'[t1].[NotificationShamsiDate]',N'DateShamsi',5, 1, 0, 110, NULL, NULL, NULL),
            (13, N'Bpm.ProcessDocument', N'AccessLevel',        N'سطح محرمانگی',       N't1_AccessLevel',           N'[t1].[AccessLevel]',           N'Select',    8, 1, 0, 120, N'ProcessDocumentAccessLevelEnum', NULL, NULL),
            (14, N'Bpm.ProcessDocument', N't1_IsRevised',       N'بازنگری با تایید سرپرست', N't1_IsRevised',        N'[t1_IsRevised]',               N'Int',       7, 0, 0,  80, NULL, NULL, NULL)
        ) v(Ord, TableName, ColumnName, DisplayName, Alliance, Address, SystemTypeName, SystemType, VisibleFlag, PrimaryKey, Width, EnumName, SortDirection, SortOrder)
    )
    SELECT @ColumnsJson = (
        SELECT
            TableName, ColumnName, DisplayName, Alliance, Address, SystemTypeName,
            CAST(0 AS INT) AS SelectIndex,
            CAST(VisibleFlag AS BIT) AS Visible,
            CAST(PrimaryKey AS BIT) AS PrimaryKey,
            SystemType, SortDirection, SortOrder,
            CAST(0 AS BIT) AS GroupBy,
            JSON_QUERY(N'[]') AS Options,
            CASE WHEN EnumName IS NULL THEN NULL
                 ELSE JSON_QUERY(N'{"ListOptions":[],"TypeOption":3,"SystemTypeName":"' + @EnumNs + EnumName + N'"}') END AS OptionSetting,
            CAST(NULL AS NVARCHAR(10)) AS Aggregate,
            CASE WHEN SystemTypeName = N'Select' THEN @RenderSelect ELSE NULL END AS Render,
            CAST(0 AS BIT) AS IsCustom,
            CAST(NULL AS NVARCHAR(10)) AS CustomColType,
            CAST(NULL AS NVARCHAR(10)) AS HtmlTemplate,
            CAST(NULL AS NVARCHAR(10)) AS BtnConfig,
            CAST(NULL AS NVARCHAR(10)) AS InputConfig,
            Width,
            CAST(1 AS BIT) AS Filterable,
            CAST(1 AS BIT) AS Sortable,
            CAST(N'' AS NVARCHAR(10)) AS ClassName
        FROM C
        ORDER BY Ord
        FOR JSON PATH, INCLUDE_NULL_VALUES
    );

    -- HTS: GetOrderedRequest => Id نزولی (ستون شماره درخواست در این گرید نمایش داده می‌شود)

    -- دکمه‌ها: عنوان دومی مطابق HTS (DownloadFinalPdfFile)؛ قواعد دسترسی/فعال‌سازی قبلی دست‌نخورده است
    DECLARE @Buttons NVARCHAR(MAX) = (SELECT CustomActionButtonsJson FROM system.SavedQuery WHERE Name = @ProfileName);
    SET @Buttons = REPLACE(@Buttons, N'"title":"دانلود PDF نهایی"', N'"title":"دانلود فایل نهایی (PDF)"');

    DECLARE @OnSelectedRow NVARCHAR(MAX) = (
        SELECT s.onSelectedRow
        FROM system.SavedQuery q
        CROSS APPLY OPENJSON(q.EventScriptsJson) WITH (onSelectedRow NVARCHAR(MAX) '$.onSelectedRow') s
        WHERE q.Name = @ProfileName);

    DECLARE @OnRowAdded NVARCHAR(MAX) = N'function(ctx) {
    var data = ctx.rowData || {};
    if (Number(data.t1_IsRevised) !== 1 || !ctx.table) return;
    var colIdx = -1;
    ctx.table.columns().every(function (i) {
        if (this.dataSrc() === ''_rowNumber'') colIdx = i;
    });
    var node = colIdx >= 0 ? ctx.table.cell(ctx.$row, colIdx).node() : ctx.$row.find(''td'').get(0);
    if (node) $(node).css(''background-color'', ''steelblue'');
}';

    DECLARE @Legend NVARCHAR(MAX) = N'[
  { "color": "steelblue", "text": "#ffffff", "label": "بازنگری با تایید سرپرست (سیستم ها و روش ها)" }
]';

    DECLARE @EventScripts NVARCHAR(MAX) = (
        SELECT @OnSelectedRow AS onSelectedRow, @OnRowAdded AS onRowAdded, @Legend AS legend
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    DECLARE @QueryJson NVARCHAR(MAX) = JSON_MODIFY(
        N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}',
        '$.CustomQuery', @CustomQuery);

    IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
        THROW 51051, N'نمایه Bpm_ProcessDocument_Published یافت نشد؛ ابتدا Seed_BpmDocumentSystem_DataProfiles.sql را اجرا کنید.', 1;

    UPDATE system.SavedQuery
    SET Title = N'لیست اسناد فرآیندی',
        QueryJson = @QueryJson,
        ColumnsJson = @ColumnsJson,
        CustomActionButtonsJson = @Buttons,
        EventScriptsJson = @EventScripts,
        Mode = 1, Type = 1, IsActive = 1,
        ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = @ProfileName;

    COMMIT TRANSACTION;
    PRINT N'OK: ' + @ProfileName;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Patch failed: %s', 16, 1, @Err);
END CATCH;

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible, JSON_VALUE(j.value, '$.SortDirection') AS SortDirection
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Bpm_ProcessDocument_Published';
