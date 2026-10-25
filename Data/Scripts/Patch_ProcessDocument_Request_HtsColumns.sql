/*
================================================================================
Patch_ProcessDocument_Request_HtsColumns.sql
نمایه داده Bpm_ProcessDocument_Request («اسناد فرآیندی - درخواست ایجاد/تغییر»)
عیناً مطابق HTS (_BpmDocument.cshtml / BpmDocumentModel / CaptionsLibrary.fa-IR).

ترتیب ستون‌های HTS:
  شماره درخواست | آخرین وضعیت | عنوان سند | کد سند | شماره بازنگری | نوع درخواست | نوع سند |
  سطح محرمانگی | ورودی درخواست | فوریت رسیدگی | مالک فرآیند | تایید کننده | تصویب کننده |
  درخواست کننده | تاریخ ایجاد | کاربر ایجادکننده | شرح درخواست

- نام کاربران = NameFa (معادل Gnr_User.FullName در HTS)
- رنگ سلول ردیف بر اساس وضعیت (HTS bpmDocument_changeRowsColor)
- ویرایش/لغو فقط برای وضعیت ۱،۲،۹ و مدیر یا ایجادکننده (HTS bpmDocument_UpdateLayout)
- دکمه‌های «دریافت ف اصلی» و «دانلود فایل متفرقه»
فقط این نمایه تغییر می‌کند؛ Manage و Published دست نمی‌خورند.

اجرا (UTF-8 با BOM):
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -x -i "Data\Scripts\Patch_ProcessDocument_Request_HtsColumns.sql"   (-x: بدون جایگزینی $(...) در اسکریپت JS)
پس از اجرا WebApp را Restart کنید (کش Redis نمایه).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-process-document-request-hts';
    DECLARE @ProfileName NVARCHAR(100) = N'Bpm_ProcessDocument_Request';

    DECLARE @IdShowAll BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.ShowAll');
    DECLARE @IdSupervisor BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.Supervisor');
    DECLARE @IdExpert BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.Expert');
    IF @IdShowAll IS NULL OR @IdSupervisor IS NULL OR @IdExpert IS NULL
        THROW 51030, N'نقش‌های Bpm.ProcessDocument یافت نشدند.', 1;

    DECLARE @CustomQuery NVARCHAR(MAX) = CAST(N'' AS NVARCHAR(MAX)) + N'
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
    COALESCE(NULLIF(LTRIM(RTRIM([t3].[NameFa])), N''''), [t3].[Name]) AS [t3_NameFa],
    COALESCE(NULLIF(LTRIM(RTRIM([t4].[NameFa])), N''''), [t4].[Name]) AS [t4_NameFa],
    COALESCE(NULLIF(LTRIM(RTRIM([t5].[NameFa])), N''''), [t5].[Name]) AS [t5_NameFa],
    COALESCE(NULLIF(LTRIM(RTRIM([t2].[NameFa])), N''''), [t2].[Name]) AS [t2_NameFa],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    COALESCE(NULLIF(LTRIM(RTRIM([t6].[NameFa])), N''''), [t1].[CreatedByName]) AS [t6_NameFa],
    [t1].[Comment] AS [t1_Comment],
    [t1].[LastStatus] AS [t1_LastStatusId],
    CASE WHEN ISNULL([t1].[LastStatus], 0) IN (1, 2, 9) AND (@IsAdmin = 1 OR [t1].[CreatedById] = @CuId) THEN 1 ELSE 0 END AS [t1_CanEdit],
    [t1].[MainFileId] AS [t1_MainFileId],
    [t1].[TempFileId] AS [t1_TempFileId]
FROM [Bpm].[ProcessDocument] AS [t1]
LEFT JOIN [system].[User] AS [t2] ON [t1].[RequesterUserId] = [t2].[Id]
LEFT JOIN [system].[User] AS [t3] ON [t1].[ProcessOwnerId] = [t3].[Id]
LEFT JOIN [system].[User] AS [t4] ON [t1].[ApproverId] = [t4].[Id]
LEFT JOIN [system].[User] AS [t5] ON [t1].[FinalApproverId] = [t5].[Id]
LEFT JOIN [system].[User] AS [t6] ON [t1].[CreatedById] = [t6].[Id]
WHERE
    @HasShowAll = 1
    OR ((@HasSupervisor = 1 OR @HasExpert = 1) AND ISNULL([t1].[LastStatus], 0) <> 18)
    OR (([t1].[CreatedById] = @CuId OR [t1].[ModifiedById] = @CuId) AND ISNULL([t1].[LastStatus], 0) <> 18)';

    DECLARE @RenderSelect NVARCHAR(MAX) = N'function(data, type, row) { return data; }';
    DECLARE @EnumNs NVARCHAR(100) = N'Entities.App.Bpm.Enums.';
    DECLARE @ColumnsJson NVARCHAR(MAX);

    ;WITH C AS (
        SELECT * FROM (VALUES
            (1,  N'Bpm.ProcessDocument', N'Id',               N'شماره درخواست',    N't1_Id',                    N'[t1].[Id]',                    N'Long',           6, 1, 1, 100, CAST(NULL AS NVARCHAR(100)), CAST(NULL AS INT), CAST(NULL AS INT)),
            (2,  N'Bpm.ProcessDocument', N'LastStatus',       N'آخرین وضعیت',      N't1_LastStatus',            N'[t1].[LastStatus]',            N'Select',         8, 1, 0, 160, N'ProcessDocumentStatusEnum', NULL, NULL),
            (3,  N'Bpm.ProcessDocument', N'DocumentTitle',    N'عنوان سند',        N't1_DocumentTitle',         N'[t1].[DocumentTitle]',         N'String',         0, 1, 0, 250, NULL, NULL, NULL),
            (4,  N'Bpm.ProcessDocument', N'DocumentNumber',   N'کد سند',           N't1_DocumentNumber',        N'[t1].[DocumentNumber]',        N'String',         0, 1, 0, 120, NULL, NULL, NULL),
            (5,  N'Bpm.ProcessDocument', N'Revision',         N'شماره بازنگری',    N't1_Revision',              N'[t1].[Revision]',              N'Int',            7, 1, 0, 120, NULL, NULL, NULL),
            (6,  N'Bpm.ProcessDocument', N'RequestType',      N'نوع درخواست',      N't1_RequestType',           N'[t1].[RequestType]',           N'Select',         8, 1, 0, 140, N'ProcessDocumentRequestTypeEnum', NULL, NULL),
            (7,  N'Bpm.ProcessDocument', N'DocumentType',     N'نوع سند',          N't1_DocumentType',          N'[t1].[DocumentType]',          N'Select',         8, 1, 0, 120, N'ProcessDocumentTypeEnum', NULL, NULL),
            (8,  N'Bpm.ProcessDocument', N'AccessLevel',      N'سطح محرمانگی',     N't1_AccessLevel',           N'[t1].[AccessLevel]',           N'Select',         8, 1, 0, 120, N'ProcessDocumentAccessLevelEnum', NULL, NULL),
            (9,  N'Bpm.ProcessDocument', N'Source',           N'ورودی درخواست',    N't1_Source',                N'[t1].[Source]',                N'Select',         8, 1, 0, 120, N'ProcessDocumentSourceEnum', NULL, NULL),
            (10, N'Bpm.ProcessDocument', N'Priority',         N'فوریت رسیدگی',     N't1_Priority',              N'[t1].[Priority]',              N'Select',         8, 1, 0, 120, N'ProcessDocumentPriorityEnum', NULL, NULL),
            (11, N'system.User',         N'NameFa',           N'مالک فرآیند',      N't3_NameFa',                N'[t3].[NameFa]',                N'String',         0, 1, 0, 150, NULL, NULL, NULL),
            (12, N'system.User',         N'NameFa',           N'تایید کننده',      N't4_NameFa',                N'[t4].[NameFa]',                N'String',         0, 1, 0, 150, NULL, NULL, NULL),
            (13, N'system.User',         N'NameFa',           N'تصویب کننده',      N't5_NameFa',                N'[t5].[NameFa]',                N'String',         0, 1, 0, 150, NULL, NULL, NULL),
            (14, N'system.User',         N'NameFa',           N'درخواست کننده',    N't2_NameFa',                N'[t2].[NameFa]',                N'String',         0, 1, 0, 180, NULL, NULL, NULL),
            (15, N'Bpm.ProcessDocument', N'CreatedOnShamsiDateTime', N'تاریخ ایجاد', N't1_CreatedOnShamsiDateTime', N'[t1].[CreatedOnShamsiDateTime]', N'DateTimeShamsi', 4, 1, 0, 110, NULL, 1, 0),
            (16, N'system.User',         N'NameFa',           N'کاربر ایجادکننده', N't6_NameFa',                N'[t6].[NameFa]',                N'String',         0, 1, 0, 140, NULL, NULL, NULL),
            (17, N'Bpm.ProcessDocument', N'Comment',          N'شرح درخواست',      N't1_Comment',               N'[t1].[Comment]',               N'String',         0, 1, 0, 300, NULL, NULL, NULL),
            (18, N'Bpm.ProcessDocument', N'LastStatus',       N'شناسه وضعیت',      N't1_LastStatusId',          N'[t1].[LastStatus]',            N'Int',            7, 0, 0,  80, NULL, NULL, NULL),
            (19, N'Bpm.ProcessDocument', N't1_CanEdit',       N'قابل ویرایش',      N't1_CanEdit',               N'[t1_CanEdit]',                 N'Int',            7, 0, 0,  80, NULL, NULL, NULL),
            (20, N'Bpm.ProcessDocument', N'MainFileId',       N'شناسه فایل اصلی',  N't1_MainFileId',            N'[t1].[MainFileId]',            N'Long',           6, 0, 0,  80, NULL, NULL, NULL),
            (21, N'Bpm.ProcessDocument', N'TempFileId',       N'شناسه فایل متفرقه', N't1_TempFileId',           N'[t1].[TempFileId]',            N'Long',           6, 0, 0,  80, NULL, NULL, NULL)
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

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"اصلاح"},{"dataActionName":"delete","enable":true,"title":"لغو درخواست"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    DECLARE @ScriptDownloadMain NVARCHAR(MAX) = N'function(ctx) {
    var id = ctx.primaryKeyValue
        || (ctx.primaryKeyValues && ctx.primaryKeyValues[0])
        || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.t1_id || ctx.selectedRow.id || ctx.selectedRow.Id));
    if (!id) {
        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');
        return;
    }
    window.open(''/Panel/Bpm/ProcessDocument/DownloadMain?id='' + id, ''_blank'');
}';

    DECLARE @ScriptDownloadTemp NVARCHAR(MAX) = N'function(ctx) {
    var id = ctx.primaryKeyValue
        || (ctx.primaryKeyValues && ctx.primaryKeyValues[0])
        || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.t1_id || ctx.selectedRow.id || ctx.selectedRow.Id));
    if (!id) {
        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');
        return;
    }
    window.open(''/Panel/Bpm/ProcessDocument/DownloadTemp?id='' + id, ''_blank'');
}';

    DECLARE @BtnMain NVARCHAR(MAX) = (
        SELECT
            N'id_downloadRequestMain' AS id,
            N'دریافت ف اصلی' AS title,
            N'downloadRequestMain' AS dataActionName,
            N'btn-color-info' AS colorClass,
            N'fa fa-paperclip' AS iconClass,
            CAST(1 AS bit) AS requiresSelection,
            CAST(0 AS bit) AS requiresMultiSelection,
            CAST(0 AS bit) AS useHtml,
            N'' AS html,
            @ScriptDownloadMain AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    DECLARE @BtnTemp NVARCHAR(MAX) = (
        SELECT
            N'id_downloadRequestTemp' AS id,
            N'دانلود فایل متفرقه' AS title,
            N'downloadRequestTemp' AS dataActionName,
            N'btn-color-info' AS colorClass,
            N'fa fa-paperclip' AS iconClass,
            CAST(1 AS bit) AS requiresSelection,
            CAST(0 AS bit) AS requiresMultiSelection,
            CAST(0 AS bit) AS useHtml,
            N'' AS html,
            @ScriptDownloadTemp AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    DECLARE @CustomButtons NVARCHAR(MAX) = N'[' + @BtnMain + N',' + @BtnTemp + N']';

    -- رنگ سلول ردیف (HTS bpmDocument_changeRowsColor): 11, 9, 10, 16
    DECLARE @OnRowAdded NVARCHAR(MAX) = N'function(ctx) {
    var data = ctx.rowData || {};
    var s = Number(data.t1_LastStatusId);
    var color = null;
    if (s === 11) color = ''#ffc080'';
    else if (s === 9) color = ''lightgreen'';
    else if (s === 10) color = ''#6e85da'';
    else if (s === 16) color = ''#eba7ec'';
    if (!color || !ctx.table) return;
    var colIdx = -1;
    ctx.table.columns().every(function (i) {
        if (this.dataSrc() === ''_rowNumber'') colIdx = i;
    });
    var node = colIdx >= 0 ? ctx.table.cell(ctx.$row, colIdx).node() : ctx.$row.find(''td'').get(0);
    if (node) $(node).css(''background-color'', color);
}';

    -- فعال/غیرفعال شدن دکمه‌ها (HTS bpmDocument_UpdateLayout)
    DECLARE @OnSelectedRow NVARCHAR(MAX) = N'function(ctx) {
    var btns = ctx.dataActionBtns || {};
    var row = (!ctx.isDeselect && ctx.selectedRow) ? ctx.selectedRow : null;
    function force($b, on, cls) {
        if (!$b || !$b.length) return;
        if (on) { $b.attr(''forcedisabled'', ''true'').prop(''disabled'', true).removeClass(cls); }
        else { $b.removeAttr(''forcedisabled''); }
    }
    function apply() {
        var canEdit = row && Number(row.t1_CanEdit) === 1;
        force(btns.edit, !canEdit && !!row, ''btn-color-warning'');
        force(btns.delete, !canEdit && !!row, ''btn-color-danger'');
        if (btns.downloadRequestMain && btns.downloadRequestMain.length)
            btns.downloadRequestMain.prop(''disabled'', !(row && row.t1_MainFileId));
        if (btns.downloadRequestTemp && btns.downloadRequestTemp.length)
            btns.downloadRequestTemp.prop(''disabled'', !(row && row.t1_TempFileId));
    }
    apply();
    setTimeout(apply, 50);
}';

    -- راهنمای رنگ‌ها (EventScriptsJson.legend) — همان رنگ‌های HTS
    DECLARE @Legend NVARCHAR(MAX) = N'[
  { "color": "#ffc080", "text": "#111111", "label": "در انتظار تایید مالک" },
  { "color": "lightgreen", "text": "#111111", "label": "رد شده" },
  { "color": "#6e85da", "text": "#ffffff", "label": "در جریان" },
  { "color": "#eba7ec", "text": "#111111", "label": "تایید، تصویب کننده" }
]';

    DECLARE @EventScripts NVARCHAR(MAX) = (
        SELECT @OnSelectedRow AS onSelectedRow, @OnRowAdded AS onRowAdded, @Legend AS legend
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    DECLARE @QueryJson NVARCHAR(MAX) = JSON_MODIFY(
        N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}',
        '$.CustomQuery', @CustomQuery);

    IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
        THROW 51031, N'نمایه Bpm_ProcessDocument_Request یافت نشد؛ ابتدا Seed_BpmDocumentSystem_DataProfiles.sql را اجرا کنید.', 1;

    UPDATE system.SavedQuery
    SET Title = N'کارتابل درخواست ایجاد/تغییر',
        QueryJson = @QueryJson,
        ColumnsJson = @ColumnsJson,
        ActionOptions = @ActionOptions,
        CustomActionButtonsJson = @CustomButtons,
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
WHERE q.Name = N'Bpm_ProcessDocument_Request';
