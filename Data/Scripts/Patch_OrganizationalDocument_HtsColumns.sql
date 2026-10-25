/*
================================================================================
Patch_OrganizationalDocument_HtsColumns.sql
نمایه داده Gnr_OrganizationalDocument_List — ستون‌ها و عنوان‌ها عیناً مطابق HTS
(Gnr_Document / _Document.cshtml / GnrDocumentModel + CaptionsLibrary.fa-IR).

ترتیب ستون‌های HTS:
  واحد مرتبط | گروه اسناد | زیرمجموعه | عنوان / مدل | عنوان / مدل (En) | برند | سال |
  نسخه | نام فایل | سایز فایل | مسیر فایل | کاربر ایجادکننده | تاریخ ایجاد | کامنت
دکمه «مشاهده» = دانلود فایل ردیف انتخاب‌شده (btnViewAttachedFile در HTS).

اجرا (UTF-8 با BOM):
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Patch_OrganizationalDocument_HtsColumns.sql"
پس از اجرا WebApp را Restart کنید (کش Redis نمایه).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-org-document-hts';
    DECLARE @ProfileName NVARCHAR(100) = N'Gnr_OrganizationalDocument_List';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'
DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
DECLARE @CuOrg BIGINT;
SELECT @CuRoles = Roles, @CuOrg = OrgUnitId FROM system.[User] WHERE Id = @CuId;
DECLARE @IsAdmin BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoles) WHERE [value] = N''admin'') THEN 1 ELSE 0 END;
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t4].[Title] AS [t4_Title],
    [t2].[Title] AS [t2_Title],
    [t2].[FolderTitle] AS [t2_FolderTitle],
    [t1].[Title] AS [t1_Title],
    [t1].[TitleInLatin] AS [t1_TitleInLatin],
    [t1].[Brand] AS [t1_Brand],
    [t1].[Year] AS [t1_Year],
    [t1].[Revision] AS [t1_Revision],
    [t3].[OriginalName] AS [t3_OriginalName],
    [t3].[Size] AS [t3_Size],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[Comment] AS [t1_Comment],
    [t1].[FileId] AS [t1_FileId]
FROM [Gnr].[OrganizationalDocument] AS [t1]
INNER JOIN [Gnr].[DocumentFolder] AS [t2] ON [t1].[FolderId] = [t2].[Id]
LEFT JOIN [dbo].[FileEntity] AS [t3] ON [t1].[FileId] = [t3].[Id]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t2].[OrganizationUnitId] = [t4].[Id]
WHERE @IsAdmin = 1 OR [t2].[IsPrivate] = 0 OR [t2].[OrganizationUnitId] = @CuOrg
    OR EXISTS (SELECT 1 FROM [Gnr].[DocumentFolderUser] u WHERE u.[DocumentFolderId] = [t2].[Id] AND u.[UserId] = @CuId)';

    DECLARE @RenderSize NVARCHAR(MAX) = N'function(data, type, row) { if (data == null || data === '''') return ''''; if (type !== ''display'') return data; var b = Number(data); if (isNaN(b)) return data; if (b < 1024) return b + '' B''; var u = [''KB'', ''MB'', ''GB'', ''TB'']; var i = -1; do { b = b / 1024; i++; } while (b >= 1024 && i < u.length - 1); return (Math.round(b * 100) / 100) + '' '' + u[i]; }';

    DECLARE @ColumnsJson NVARCHAR(MAX);

    ;WITH C AS (
        SELECT * FROM (VALUES
            (1,  N'Gnr.OrganizationalDocument', N'Id',               N'شناسه',            N't1_Id',                    N'[t1].[Id]',                    N'Long',           6, 0, 1,  80, N'',    CAST(NULL AS NVARCHAR(MAX)), CAST(NULL AS INT), CAST(NULL AS INT)),
            (2,  N'Hrm.OrgUnit',                 N'Title',            N'واحد مرتبط',       N't4_Title',                 N'[t4].[Title]',                 N'String',         0, 1, 0, 150, N'',    NULL, NULL, NULL),
            (3,  N'Gnr.DocumentFolder',          N'Title',            N'گروه اسناد',       N't2_Title',                 N'[t2].[Title]',                 N'String',         0, 1, 0, 200, N'',    NULL, NULL, NULL),
            (4,  N'Gnr.DocumentFolder',          N'FolderTitle',      N'زیرمجموعه',        N't2_FolderTitle',           N'[t2].[FolderTitle]',           N'String',         0, 1, 0, 200, N'',    NULL, NULL, NULL),
            (5,  N'Gnr.OrganizationalDocument', N'Title',            N'عنوان / مدل',      N't1_Title',                 N'[t1].[Title]',                 N'String',         0, 1, 0, 200, N'',    NULL, NULL, NULL),
            (6,  N'Gnr.OrganizationalDocument', N'TitleInLatin',     N'عنوان / مدل (En)', N't1_TitleInLatin',          N'[t1].[TitleInLatin]',          N'String',         0, 1, 0, 180, N'',    NULL, NULL, NULL),
            (7,  N'Gnr.OrganizationalDocument', N'Brand',            N'برند',             N't1_Brand',                 N'[t1].[Brand]',                 N'String',         0, 1, 0, 120, N'',    NULL, NULL, NULL),
            (8,  N'Gnr.OrganizationalDocument', N'Year',             N'سال',              N't1_Year',                  N'[t1].[Year]',                  N'Int',            7, 1, 0,  90, N'',    NULL, NULL, NULL),
            (9,  N'Gnr.OrganizationalDocument', N'Revision',         N'نسخه',             N't1_Revision',              N'[t1].[Revision]',              N'Int',            7, 1, 0,  80, N'',    NULL, NULL, NULL),
            (10, N'dbo.FileEntity',              N'OriginalName',     N'نام فایل',         N't3_OriginalName',          N'[t3].[OriginalName]',          N'String',         0, 1, 0, 170, N'ltr', NULL, NULL, NULL),
            (11, N'dbo.FileEntity',              N'Size',             N'سایز فایل',        N't3_Size',                  N'[t3].[Size]',                  N'Long',           6, 1, 0, 100, N'',    @RenderSize, NULL, NULL),
            (13, N'Gnr.OrganizationalDocument', N'CreatedByName',    N'کاربر ایجادکننده', N't1_CreatedByName',         N'[t1].[CreatedByName]',         N'String',         0, 1, 0, 150, N'',    NULL, NULL, NULL),
            (14, N'Gnr.OrganizationalDocument', N'CreatedOnShamsiDateTime', N'تاریخ ایجاد', N't1_CreatedOnShamsiDateTime', N'[t1].[CreatedOnShamsiDateTime]', N'DateTimeShamsi', 4, 1, 0, 115, N'', NULL, 1, 0),
            (15, N'Gnr.OrganizationalDocument', N'Comment',          N'کامنت',            N't1_Comment',               N'[t1].[Comment]',               N'String',         0, 1, 0, 350, N'',    NULL, NULL, NULL),
            (16, N'Gnr.OrganizationalDocument', N'FileId',           N'شناسه فایل',       N't1_FileId',                N'[t1].[FileId]',                N'Long',           6, 0, 0,  80, N'',    NULL, NULL, NULL)
        ) v(Ord, TableName, ColumnName, DisplayName, Alliance, Address, SystemTypeName, SystemType, Visible, PrimaryKey, Width, ClassName, Render, SortDirection, SortOrder)
    )
    SELECT @ColumnsJson = (
        SELECT
            TableName,
            ColumnName,
            DisplayName,
            Alliance,
            Address,
            SystemTypeName,
            CAST(0 AS INT) AS SelectIndex,
            CAST(Visible AS BIT) AS Visible,
            CAST(PrimaryKey AS BIT) AS PrimaryKey,
            SystemType,
            SortDirection,
            SortOrder,
            CAST(0 AS BIT) AS GroupBy,
            JSON_QUERY(N'[]') AS Options,
            CAST(NULL AS NVARCHAR(10)) AS OptionSetting,
            CAST(NULL AS NVARCHAR(10)) AS Aggregate,
            Render,
            CAST(0 AS BIT) AS IsCustom,
            CAST(NULL AS NVARCHAR(10)) AS CustomColType,
            CAST(NULL AS NVARCHAR(10)) AS HtmlTemplate,
            CAST(NULL AS NVARCHAR(10)) AS BtnConfig,
            CAST(NULL AS NVARCHAR(10)) AS InputConfig,
            Width,
            CAST(1 AS BIT) AS Filterable,
            CAST(1 AS BIT) AS Sortable,
            ClassName
        FROM C
        ORDER BY Ord
        FOR JSON PATH, INCLUDE_NULL_VALUES
    );

    DECLARE @ScriptView NVARCHAR(MAX) = N'function(ctx) {
    var id = ctx.primaryKeyValue
        || (ctx.primaryKeyValues && ctx.primaryKeyValues[0])
        || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.t1_id || ctx.selectedRow.id || ctx.selectedRow.Id));
    if (!id) {
        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');
        return;
    }
    window.open(''/Panel/Gnr/OrganizationalDocument/Download?id='' + id, ''_blank'');
}';

    DECLARE @CustomButtons NVARCHAR(MAX) = N'[' + (
        SELECT
            N'id_viewOrganizationalDocumentFile' AS id,
            N'مشاهده' AS title,
            N'viewOrganizationalDocumentFile' AS dataActionName,
            N'btn-color-info' AS colorClass,
            N'fa fa-eye' AS iconClass,
            CAST(1 AS bit) AS requiresSelection,
            CAST(0 AS bit) AS requiresMultiSelection,
            CAST(0 AS bit) AS useHtml,
            N'' AS html,
            @ScriptView AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) + N']';

    DECLARE @QueryJson NVARCHAR(MAX) = JSON_MODIFY(
        N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}',
        '$.CustomQuery', @CustomQuery);

    IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
        THROW 51020, N'نمایه Gnr_OrganizationalDocument_List یافت نشد؛ ابتدا Seed_BpmDocumentSystem_DataProfiles.sql را اجرا کنید.', 1;

    UPDATE system.SavedQuery
    SET Title = N'اسناد سازمانی',
        QueryJson = @QueryJson,
        ColumnsJson = @ColumnsJson,
        CustomActionButtonsJson = @CustomButtons,
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
WHERE q.Name = N'Gnr_OrganizationalDocument_List';

SELECT Name, Title, LEFT(CustomActionButtonsJson, 120) AS CustomActionButtonsJson
FROM system.SavedQuery WHERE Name = N'Gnr_OrganizationalDocument_List';
