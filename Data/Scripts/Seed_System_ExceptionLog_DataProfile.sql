/*
  نمایه داده و منوی مشاهده system.ExceptionLog
  UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-exceptionlog';
    DECLARE @Name NVARCHAR(200) = N'System_ExceptionLog_List';
    DECLARE @Title NVARCHAR(200) = N'لیست خطاها';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.base.exceptionlog';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/system/exceptionlog/list';
    DECLARE @DetailPath NVARCHAR(300) = N'/panel/system/exceptionlog/detail';
    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"SELECT\n    [t1].[Id] AS [t1_Id],\n    [t1].[CreatedOnShamsi] AS [t1_CreatedOnShamsi],\n    [t1].[ExceptionType] AS [t1_ExceptionType],\n    [t1].[Message] AS [t1_Message],\n    [t1].[HttpMethod] AS [t1_HttpMethod],\n    [t1].[RequestPath] AS [t1_RequestPath],\n    [t1].[StatusCode] AS [t1_StatusCode],\n    [t1].[UserName] AS [t1_UserName],\n    [t1].[UserIp] AS [t1_UserIp]\nFROM [system].[ExceptionLog] AS [t1]","Parameters":[],"Selects":null}';
    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName":"system.ExceptionLog","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"system.ExceptionLog","ColumnName":"t1_CreatedOnShamsi","DisplayName":"تاریخ","Alliance":"t1_CreatedOnShamsi","Address":"[t1].[CreatedOnShamsi]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"system.ExceptionLog","ColumnName":"t1_ExceptionType","DisplayName":"نوع خطا","Alliance":"t1_ExceptionType","Address":"[t1].[ExceptionType]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === \"\") return \"\"; if (type !== \"display\") return data; var t = String(data).replace(/&/g, \"&amp;\").replace(/</g, \"&lt;\").replace(/>/g, \"&gt;\").replace(/\"/g, \"&quot;\"); return \"<span title=\\\"\" + t + \"\\\" style=\\\"display:inline-block;max-width:420px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;vertical-align:bottom\\\">\" + t + \"</span>\"; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":280,"Filterable":true,"Sortable":true,"ClassName":"ltr"},{"TableName":"system.ExceptionLog","ColumnName":"t1_Message","DisplayName":"پیام","Alliance":"t1_Message","Address":"[t1].[Message]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === \"\") return \"\"; if (type !== \"display\") return data; var t = String(data).replace(/&/g, \"&amp;\").replace(/</g, \"&lt;\").replace(/>/g, \"&gt;\").replace(/\"/g, \"&quot;\"); return \"<span title=\\\"\" + t + \"\\\" style=\\\"display:inline-block;max-width:420px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;vertical-align:bottom\\\">\" + t + \"</span>\"; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":420,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"system.ExceptionLog","ColumnName":"t1_HttpMethod","DisplayName":"متد","Alliance":"t1_HttpMethod","Address":"[t1].[HttpMethod]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":"ltr"},{"TableName":"system.ExceptionLog","ColumnName":"t1_RequestPath","DisplayName":"مسیر","Alliance":"t1_RequestPath","Address":"[t1].[RequestPath]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === \"\") return \"\"; if (type !== \"display\") return data; var t = String(data).replace(/&/g, \"&amp;\").replace(/</g, \"&lt;\").replace(/>/g, \"&gt;\").replace(/\"/g, \"&quot;\"); return \"<span title=\\\"\" + t + \"\\\" style=\\\"display:inline-block;max-width:420px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;vertical-align:bottom\\\">\" + t + \"</span>\"; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":320,"Filterable":true,"Sortable":true,"ClassName":"ltr"},{"TableName":"system.ExceptionLog","ColumnName":"t1_StatusCode","DisplayName":"کد وضعیت","Alliance":"t1_StatusCode","Address":"[t1].[StatusCode]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"system.ExceptionLog","ColumnName":"t1_UserName","DisplayName":"کاربر","Alliance":"t1_UserName","Address":"[t1].[UserName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"system.ExceptionLog","ColumnName":"t1_UserIp","DisplayName":"آی‌پی","Alliance":"t1_UserIp","Address":"[t1].[UserIp]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":"ltr"}]';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @Buttons NVARCHAR(MAX) = N'[{"id":"exceptionlog_detail","title":"جزئیات","colorClass":"btn-color-primary","iconClass":"ki-eye ki-outline","requiresSelection":true,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    if (!id) { toastr.error(\"یک ردیف را انتخاب کنید\"); return; }\n    get(\"/Panel/System/ExceptionLog/Detail?id=\" + id, function (r) {\n        if (!r || !r.isSuccess || !r.data) { toastr.error((r && r.message) || \"خطا یافت نشد\", \"خطا\"); return; }\n        var d = r.data;\n        function esc(v) { return $(\"<div>\").text(v == null ? \"\" : String(v)).html(); }\n        var path = esc(d.httpMethod) + \" \" + esc(d.requestPath) + (d.queryString ? (\"?\" + esc(d.queryString)) : \"\");\n        var html = \"<div style=\\\"max-height:70vh;overflow:auto;text-align:right\\\">\"\n            + \"<div class=\\\"mb-2\\\"><b>تاریخ:</b> \" + esc(d.createdOnShamsi) + \"</div>\"\n            + \"<div class=\\\"mb-2\\\"><b>نوع:</b> <span class=\\\"ltr d-inline-block\\\">\" + esc(d.exceptionType) + \"</span></div>\"\n            + \"<div class=\\\"mb-2\\\"><b>وضعیت:</b> \" + esc(d.statusCode) + \"</div>\"\n            + \"<div class=\\\"mb-2\\\"><b>مسیر:</b> <span class=\\\"ltr d-inline-block\\\">\" + path + \"</span></div>\"\n            + \"<div class=\\\"mb-2\\\"><b>کاربر:</b> \" + esc(d.userName) + \" <span class=\\\"ltr\\\">\" + esc(d.userIp) + \"</span></div>\"\n            + \"<div class=\\\"mb-2\\\"><b>پیام:</b><pre class=\\\"mb-0\\\" style=\\\"white-space:pre-wrap\\\">\" + esc(d.message) + \"</pre></div>\"\n            + \"<div class=\\\"mb-2\\\"><b>جزئیات:</b><pre class=\\\"mb-0 ltr\\\" style=\\\"white-space:pre-wrap;text-align:left\\\">\" + esc(d.stackTrace) + \"</pre></div>\"\n            + (d.innerMessage ? \"<div class=\\\"mb-2\\\"><b>خطای داخلی:</b><pre class=\\\"mb-0\\\" style=\\\"white-space:pre-wrap\\\">\" + esc(d.innerMessage) + \"</pre></div>\" : \"\")\n            + (d.innerStackTrace ? \"<div class=\\\"mb-2\\\"><b>جزئیات داخلی:</b><pre class=\\\"mb-0 ltr\\\" style=\\\"white-space:pre-wrap;text-align:left\\\">\" + esc(d.innerStackTrace) + \"</pre></div>\" : \"\")\n            + \"</div>\";\n        $.confirm({ title: \"جزئیات خطا\", content: html, rtl: true, columnClass: \"col-md-10 col-md-offset-1\", buttons: { close: { text: \"بستن\", action: function () {} } } });\n    });\n}"}]';
    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"لاگ خطاها","icon":"ki-information-3 ki-outline","iconColor":"#F87171","path":"/panel/system/exceptionlog/list","a_attr":{"href":"/panel/system/exceptionlog/list"},"data":{"iconColor":"#F87171","path":"/panel/system/exceptionlog/list"},"children":[]}';
    DECLARE @ProfileId BIGINT;

    IF ISJSON(@QueryJson) <> 1 OR ISJSON(@ColumnsJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@Buttons) <> 1 OR ISJSON(@Leaf) <> 1
        THROW 51650, N'JSON نمایه لاگ خطا معتبر نیست.', 1;

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
            CustomActionButtonsJson = @Buttons,
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
            @Buttons, N'{"onSelectedRow":"","onRowAdded":""}',
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
        3, 7, @EntityFullName, @Title,
        NULL, @ProfileId, r.Id,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM [system].[Role] r
    WHERE r.Name = N'admin'
      AND NOT EXISTS (
            SELECT 1 FROM [system].[RoleAccess] ra
            WHERE ra.RoleId = r.Id
              AND ra.Path = N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20))
              AND ra.ActionAccessType = 3
      );

    INSERT INTO [system].[RoleAccess]
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT v.Path, v.AType, v.IType, @EntityFullName, v.Title,
        NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM [system].[Role] r
    CROSS JOIN (VALUES
        (@PagePath, 1, 1, @Title),
        (@DetailPath, 2, 0, N'مشاهده خطا')
    ) v(Path, AType, IType, Title)
    WHERE r.Name = N'admin'
      AND NOT EXISTS (
            SELECT 1 FROM [system].[RoleAccess] ra
            WHERE ra.RoleId = r.Id AND ra.Path = v.Path AND ra.ActionAccessType = v.AType
      );

    DECLARE @Content NVARCHAR(MAX);
    SELECT @Content = Content FROM [system].[SystemMenu] WHERE Name = N'AllMenus';
    IF @Content IS NOT NULL AND CHARINDEX(@PagePath, @Content) = 0 AND ISJSON(@Content) = 1
    BEGIN
        DECLARE @Children NVARCHAR(MAX) = JSON_QUERY(@Content, N'$[1].children');
        IF JSON_VALUE(@Content, N'$[1].text') = N'حقوق دسترسی' AND @Children IS NOT NULL
        BEGIN
            DECLARE @Rebuilt NVARCHAR(MAX) = N'[';
            DECLARE @n INT = (SELECT COUNT(*) FROM OPENJSON(@Children));
            DECLARE @x INT = 0;
            DECLARE @comma BIT = 0;
            WHILE @x < @n
            BEGIN
                IF @comma = 1 SET @Rebuilt += N',';
                SET @comma = 1;
                SET @Rebuilt += JSON_QUERY(@Children, N'$[' + CAST(@x AS NVARCHAR(10)) + N']');
                IF JSON_VALUE(@Children, N'$[' + CAST(@x AS NVARCHAR(10)) + N'].path') = N'/panel/listrole'
                    SET @Rebuilt += N',' + @Leaf;
                SET @x += 1;
            END
            SET @Rebuilt += N']';
            SET @Content = JSON_MODIFY(@Content, N'$[1].children', JSON_QUERY(@Rebuilt));
            IF @Content IS NULL OR ISJSON(@Content) <> 1
                THROW 51651, N'JSON منو بعد از افزودن لاگ خطا معتبر نیست.', 1;
            UPDATE [system].[SystemMenu]
            SET Content = @Content,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Name = N'AllMenus';
        END
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'System_ExceptionLog_List';
