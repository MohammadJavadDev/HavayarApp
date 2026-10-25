/*
================================================================================
Patch_ProcessDocument_Manage_HtsColumns.sql
نمایه داده Bpm_ProcessDocument_Manage («اسناد فرآیندی - مدیریت درخواست‌ها»)
عیناً مطابق HTS (_BpmManageDocument.cshtml / BpmDocumentModel / CaptionsLibrary.fa-IR).

ستون‌ها: شماره درخواست | آخرین وضعیت | عنوان سند | کد سند | شماره بازنگری | نوع درخواست | نوع سند |
  سطح محرمانگی | ورودی درخواست | فوریت رسیدگی | مالک فرآیند | تایید کننده | تصویب کننده |
  واحدهای دریافت کننده ابلاغ | تاریخ ایجاد | کاربر ایجادکننده | شرح درخواست
دکمه‌ها: بررسی / تایید (مودال)، دریافت ف اصلی، دانلود فایل متفرقه، دریافت فایل یادداشت،
  دانلود فایل نهایی، دانلود فایل نهایی (PDF).  (جدید/ویرایش/حذف در HTS وجود ندارد.)

اجرا (UTF-8 با BOM؛ -x چون اسکریپت JS شامل $(...) است):
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -x -i "Data\Scripts\Patch_ProcessDocument_Manage_HtsColumns.sql"
پس از اجرا WebApp را Restart کنید (کش Redis نمایه).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-process-document-manage-hts';
    DECLARE @ProfileName NVARCHAR(100) = N'Bpm_ProcessDocument_Manage';

    DECLARE @IdShowAll BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.ShowAll');
    DECLARE @IdSupervisor BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.Supervisor');
    DECLARE @IdExpert BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.Expert');
    DECLARE @IdViewAll BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Bpm.ProcessDocument.ViewAllAttachments');
    IF @IdShowAll IS NULL OR @IdSupervisor IS NULL OR @IdExpert IS NULL OR @IdViewAll IS NULL
        THROW 51040, N'نقش‌های Bpm.ProcessDocument یافت نشدند.', 1;

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
DECLARE @HasViewAll BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = '
        + CAST(@IdViewAll AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
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
    (SELECT STRING_AGG([o].[Title], N'','') FROM [Bpm].[ProcessDocumentExecuterOrgUnit] AS [eo]
        INNER JOIN [Hrm].[OrgUnit] AS [o] ON [o].[Id] = [eo].[OrgUnitId]
        WHERE [eo].[ProcessDocumentId] = [t1].[Id]) AS [t1_ExecuterOrgUnits],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    COALESCE(NULLIF(LTRIM(RTRIM([t6].[NameFa])), N''''), [t1].[CreatedByName]) AS [t6_NameFa],
    [t1].[Comment] AS [t1_Comment],
    [t1].[LastStatus] AS [t1_LastStatusId],
    CASE WHEN
            (ISNULL([t1].[LastStatus], 0) IN (1, 2) AND @HasSupervisor = 1)
         OR ([t1].[DocumentType] IS NOT NULL AND (
                (ISNULL([t1].[LastStatus], 0) IN (12, 14, 16, 17, 19, 20) AND @HasSupervisor = 1)
             OR (ISNULL([t1].[LastStatus], 0) IN (10, 21, 24) AND @HasExpert = 1)))
         OR ([t1].[LastStatus] = 11 AND (@IsAdmin = 1 OR [t1].[ProcessOwnerId] = @CuId))
         OR ([t1].[LastStatus] = 13 AND (@IsAdmin = 1 OR [t1].[ApproverId] = @CuId))
         OR ([t1].[LastStatus] = 15 AND (@IsAdmin = 1 OR [t1].[FinalApproverId] = @CuId))
         THEN 1 ELSE 0 END AS [t1_CanTransition],
    [t1].[MainFileId] AS [t1_MainFileId],
    CASE WHEN [t1].[TempFileId] IS NOT NULL AND (
            @IsAdmin = 1 OR @HasSupervisor = 1 OR @HasExpert = 1 OR @HasViewAll = 1
            OR (ISNULL([t1].[ProcessOwnerId], 0) <> @CuId AND ISNULL([t1].[ApproverId], 0) <> @CuId AND ISNULL([t1].[FinalApproverId], 0) <> @CuId))
         THEN 1 ELSE 0 END AS [t1_CanTemp],
    CASE WHEN [t1].[CommentFileId] IS NOT NULL AND (@IsAdmin = 1 OR @HasSupervisor = 1 OR @HasExpert = 1 OR @HasViewAll = 1)
         THEN 1 ELSE 0 END AS [t1_CanComment],
    [t1].[FinalFileId] AS [t1_FinalFileId],
    [t1].[FinalPdfFileId] AS [t1_FinalPdfFileId]
FROM [Bpm].[ProcessDocument] AS [t1]
LEFT JOIN [system].[User] AS [t3] ON [t1].[ProcessOwnerId] = [t3].[Id]
LEFT JOIN [system].[User] AS [t4] ON [t1].[ApproverId] = [t4].[Id]
LEFT JOIN [system].[User] AS [t5] ON [t1].[FinalApproverId] = [t5].[Id]
LEFT JOIN [system].[User] AS [t6] ON [t1].[CreatedById] = [t6].[Id]
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

    DECLARE @RenderSelect NVARCHAR(MAX) = N'function(data, type, row) { return data; }';
    DECLARE @EnumNs NVARCHAR(100) = N'Entities.App.Bpm.Enums.';
    DECLARE @ColumnsJson NVARCHAR(MAX);

    ;WITH C AS (
        SELECT * FROM (VALUES
            (1,  N'Bpm.ProcessDocument', N'Id',               N'شماره درخواست',                N't1_Id',                    N'[t1].[Id]',                    N'Long',           6, 1, 1, 100, CAST(NULL AS NVARCHAR(100)), CAST(NULL AS INT), CAST(NULL AS INT)),
            (2,  N'Bpm.ProcessDocument', N'LastStatus',       N'آخرین وضعیت',                  N't1_LastStatus',            N'[t1].[LastStatus]',            N'Select',         8, 1, 0, 160, N'ProcessDocumentStatusEnum', NULL, NULL),
            (3,  N'Bpm.ProcessDocument', N'DocumentTitle',    N'عنوان سند',                    N't1_DocumentTitle',         N'[t1].[DocumentTitle]',         N'String',         0, 1, 0, 300, NULL, NULL, NULL),
            (4,  N'Bpm.ProcessDocument', N'DocumentNumber',   N'کد سند',                       N't1_DocumentNumber',        N'[t1].[DocumentNumber]',        N'String',         0, 1, 0, 120, NULL, NULL, NULL),
            (5,  N'Bpm.ProcessDocument', N'Revision',         N'شماره بازنگری',                N't1_Revision',              N'[t1].[Revision]',              N'Int',            7, 1, 0,  90, NULL, NULL, NULL),
            (6,  N'Bpm.ProcessDocument', N'RequestType',      N'نوع درخواست',                  N't1_RequestType',           N'[t1].[RequestType]',           N'Select',         8, 1, 0, 100, N'ProcessDocumentRequestTypeEnum', NULL, NULL),
            (7,  N'Bpm.ProcessDocument', N'DocumentType',     N'نوع سند',                      N't1_DocumentType',          N'[t1].[DocumentType]',          N'Select',         8, 1, 0, 110, N'ProcessDocumentTypeEnum', NULL, NULL),
            (8,  N'Bpm.ProcessDocument', N'AccessLevel',      N'سطح محرمانگی',                 N't1_AccessLevel',           N'[t1].[AccessLevel]',           N'Select',         8, 1, 0, 110, N'ProcessDocumentAccessLevelEnum', NULL, NULL),
            (9,  N'Bpm.ProcessDocument', N'Source',           N'ورودی درخواست',                N't1_Source',                N'[t1].[Source]',                N'Select',         8, 1, 0, 110, N'ProcessDocumentSourceEnum', NULL, NULL),
            (10, N'Bpm.ProcessDocument', N'Priority',         N'فوریت رسیدگی',                 N't1_Priority',              N'[t1].[Priority]',              N'Select',         8, 1, 0, 110, N'ProcessDocumentPriorityEnum', NULL, NULL),
            (11, N'system.User',         N'NameFa',           N'مالک فرآیند',                  N't3_NameFa',                N'[t3].[NameFa]',                N'String',         0, 1, 0, 150, NULL, NULL, NULL),
            (12, N'system.User',         N'NameFa',           N'تایید کننده',                  N't4_NameFa',                N'[t4].[NameFa]',                N'String',         0, 1, 0, 150, NULL, NULL, NULL),
            (13, N'system.User',         N'NameFa',           N'تصویب کننده',                  N't5_NameFa',                N'[t5].[NameFa]',                N'String',         0, 1, 0, 150, NULL, NULL, NULL),
            (14, N'Bpm.ProcessDocument', N't1_ExecuterOrgUnits', N'واحدهای دریافت کننده ابلاغ', N't1_ExecuterOrgUnits',     N'[t1_ExecuterOrgUnits]',        N'String',         0, 1, 0, 200, NULL, NULL, NULL),
            (15, N'Bpm.ProcessDocument', N'CreatedOnShamsiDateTime', N'تاریخ ایجاد',           N't1_CreatedOnShamsiDateTime', N'[t1].[CreatedOnShamsiDateTime]', N'DateTimeShamsi', 4, 1, 0, 110, NULL, 1, 0),
            (16, N'system.User',         N'NameFa',           N'کاربر ایجادکننده',             N't6_NameFa',                N'[t6].[NameFa]',                N'String',         0, 1, 0, 140, NULL, NULL, NULL),
            (17, N'Bpm.ProcessDocument', N'Comment',          N'شرح درخواست',                  N't1_Comment',               N'[t1].[Comment]',               N'String',         0, 1, 0, 300, NULL, NULL, NULL),
            (18, N'Bpm.ProcessDocument', N'LastStatus',       N'شناسه وضعیت',                  N't1_LastStatusId',          N'[t1].[LastStatus]',            N'Int',            7, 0, 0,  80, NULL, NULL, NULL),
            (19, N'Bpm.ProcessDocument', N't1_CanTransition', N'قابل بررسی/تایید',             N't1_CanTransition',         N'[t1_CanTransition]',           N'Int',            7, 0, 0,  80, NULL, NULL, NULL),
            (20, N'Bpm.ProcessDocument', N'MainFileId',       N'شناسه فایل اصلی',              N't1_MainFileId',            N'[t1].[MainFileId]',            N'Long',           6, 0, 0,  80, NULL, NULL, NULL),
            (21, N'Bpm.ProcessDocument', N't1_CanTemp',       N'دسترسی فایل متفرقه',           N't1_CanTemp',               N'[t1_CanTemp]',                 N'Int',            7, 0, 0,  80, NULL, NULL, NULL),
            (22, N'Bpm.ProcessDocument', N't1_CanComment',    N'دسترسی فایل کامنت',            N't1_CanComment',            N'[t1_CanComment]',              N'Int',            7, 0, 0,  80, NULL, NULL, NULL),
            (23, N'Bpm.ProcessDocument', N'FinalFileId',      N'شناسه فایل نهایی',             N't1_FinalFileId',           N'[t1].[FinalFileId]',           N'Long',           6, 0, 0,  80, NULL, NULL, NULL),
            (24, N'Bpm.ProcessDocument', N'FinalPdfFileId',   N'شناسه فایل نهایی PDF',         N't1_FinalPdfFileId',        N'[t1].[FinalPdfFileId]',        N'Long',           6, 0, 0,  80, NULL, NULL, NULL)
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

    -- HTS: فقط «بررسی / تایید» (مودال)؛ جدید/ویرایش/حذف وجود ندارد
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    DECLARE @ScriptReview NVARCHAR(MAX) = N'function(ctx) {
    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));
    if (!id) { toastr.warning(''لطفاً یک ردیف انتخاب کنید'', ''هشدار''); return; }
    // اسکریپت دکمه در scope سراسری اجرا می‌شود و متغیر page ندارد؛ صفحهٔ فعال از appController گرفته می‌شود.
    var pg = (window.appController && appController.activePage) ? appController.activePage : null;
    var getter = (pg && pg.getPartialView) ? pg.getPartialView.bind(pg) : null;
    if (!getter) { toastr.error(''امکان باز کردن فرم وجود ندارد''); return; }
    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;
    getter(''/Panel/Bpm/ProcessDocument/ManageStatusForm?id='' + id, function (r) {
        if ($btn) $btn.block(false);
        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }
        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }
        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };
        var $bodyCn = $(''<div style="overflow-x:hidden;overflow-y:auto;max-height:75vh;"></div>'').append(decoded.html || '''');
        $.confirm({
            title: ''بررسی / تایید'',
            columnClass: ''col-md-10'',
            content: $bodyCn,
            rtl: true,
            closeIcon: true,
            buttons: {
                save: {
                    text: ''ذخیره'',
                    btnClass: ''btn-primary'',
                    action: function () {
                        var modal = this;
                        var api = modal.$content.find(''#manageStatusRoot'').data(''api'');
                        if (!api) return false;
                        api.submit($(modal.buttons.save.el), function () {
                            modal.close();
                            toastr.success(''عملیات مورد نظر با موفقیت انجام شد'');
                            if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);
                        });
                        return false;
                    }
                },
                cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }
            },
            onContentReady: function () {
                var modal = this;
                (decoded.scripts || []).forEach(function (code) {
                    try { new Function(''page'', ''$$'', ''self'', code)(pg, (pg && pg.$$) ? pg.$$ : (typeof $$ !== ''undefined'' ? $$ : jQuery), pg ? pg.self : null); } catch (e) { console.error(e); }
                });
                setTimeout(function () {
                    if (typeof initPersionDatePicker === ''function'') initPersionDatePicker(modal.$body);
                    if (typeof initFileUploaders === ''function'') initFileUploaders(modal.$body);
                    modal.$body.find(''.entity-selector-wrapper'').each(function () {
                        if (!$(this).data(''readonly'') && typeof $(this).entitySelector === ''function'') $(this).entitySelector();
                    });
                }, 100);
            }
        });
    });
}';

    DECLARE @ScriptDownload NVARCHAR(MAX) = N'function(ctx) {
    var id = ctx.primaryKeyValue
        || (ctx.primaryKeyValues && ctx.primaryKeyValues[0])
        || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.t1_id || ctx.selectedRow.id || ctx.selectedRow.Id));
    if (!id) {
        toastr.warning(''لطفاً یک ردیف انتخاب کنید'', ''هشدار'');
        return;
    }
    window.open(''/Panel/Bpm/ProcessDocument/__ACT__?id='' + id, ''_blank'');
}';

    DECLARE @CustomButtons NVARCHAR(MAX) = (
        SELECT id, title, dataActionName, colorClass, iconClass,
               CAST(1 AS bit) AS requiresSelection, CAST(0 AS bit) AS requiresMultiSelection,
               CAST(0 AS bit) AS useHtml, N'' AS html, actionScript
        FROM (VALUES
            (1, N'id_reviewManage',         N'بررسی / تایید',           N'reviewManage',         N'btn-color-success', N'fa fa-list-check', @ScriptReview),
            (2, N'id_downloadManageMain',   N'دریافت ف اصلی',           N'downloadManageMain',   N'btn-color-info',    N'fa fa-paperclip',  REPLACE(@ScriptDownload, N'__ACT__', N'DownloadMain')),
            (3, N'id_downloadManageTemp',   N'دانلود فایل متفرقه',      N'downloadManageTemp',   N'btn-color-info',    N'fa fa-paperclip',  REPLACE(@ScriptDownload, N'__ACT__', N'DownloadTemp')),
            (4, N'id_downloadManageComment',N'دریافت فایل یادداشت',     N'downloadManageComment',N'btn-color-info',    N'fa fa-paperclip',  REPLACE(@ScriptDownload, N'__ACT__', N'DownloadComment')),
            (5, N'id_downloadManageFinal',  N'دانلود فایل نهایی',       N'downloadManageFinal',  N'btn-color-info',    N'fa fa-paperclip',  REPLACE(@ScriptDownload, N'__ACT__', N'DownloadFinal')),
            (6, N'id_downloadManageFinalPdf',N'دانلود فایل نهایی (PDF)', N'downloadManageFinalPdf',N'btn-color-info',   N'fa fa-paperclip',  REPLACE(@ScriptDownload, N'__ACT__', N'DownloadFinalPdf'))
        ) b(ord, id, title, dataActionName, colorClass, iconClass, actionScript)
        ORDER BY ord
        FOR JSON PATH
    );

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

    DECLARE @OnSelectedRow NVARCHAR(MAX) = N'function(ctx) {
    var btns = ctx.dataActionBtns || {};
    var row = (!ctx.isDeselect && ctx.selectedRow) ? ctx.selectedRow : null;
    function en($b, ok) { if ($b && $b.length) $b.prop(''disabled'', !(row && ok)); }
    function apply() {
        en(btns.reviewManage, row && Number(row.t1_CanTransition) === 1);
        en(btns.downloadManageMain, row && row.t1_MainFileId);
        en(btns.downloadManageTemp, row && Number(row.t1_CanTemp) === 1);
        en(btns.downloadManageComment, row && Number(row.t1_CanComment) === 1);
        en(btns.downloadManageFinal, row && row.t1_FinalFileId);
        en(btns.downloadManageFinalPdf, row && row.t1_FinalPdfFileId);
    }
    apply();
    setTimeout(apply, 50);
}';

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
        THROW 51041, N'نمایه Bpm_ProcessDocument_Manage یافت نشد؛ ابتدا Seed_BpmDocumentSystem_DataProfiles.sql را اجرا کنید.', 1;

    UPDATE system.SavedQuery
    SET Title = N'کارتابل مدیریت درخواست‌ها',
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
WHERE q.Name = N'Bpm_ProcessDocument_Manage';
