/*
  رنگ ستون ردیف مدارک مهندسی بر اساس وضعیت.
  t1_Status از نوع Select است و قبل از onRowAdded به متن تبدیل می‌شود،
  پس کد عددی در ستون مخفی t1_StatusId می‌ماند.
  UTF-8 with BOM.
  sqlcmd -S <server> -d HavayarApp -C -b -I -x -f 65001 -i Data\Scripts\Patch_EdmsDocument_StatusColors.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-edms-document-status-colors';
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
    DECLARE @StatusCol NVARCHAR(MAX) = N'{"TableName":"Edms.Document","ColumnName":"t1_StatusId","DisplayName":"کد وضعیت","Alliance":"t1_StatusId","Address":"[t1].[Status]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":false,"Sortable":false,"ClassName":""}';
    DECLARE @Name NVARCHAR(100);
    DECLARE @Cols NVARCHAR(MAX);
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Sql NVARCHAR(MAX);
    DECLARE @Check NVARCHAR(MAX);
    DECLARE @Ev NVARCHAR(MAX);
    DECLARE @Updated INT = 0;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name
        FROM system.SavedQuery
        WHERE EntityFullName = N'entities.app.edms.document'
        ORDER BY Id;

    OPEN cur;
    FETCH NEXT FROM cur INTO @Name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT
            @Cols = ColumnsJson,
            @Qj = QueryJson,
            @Ev = EventScriptsJson
        FROM system.SavedQuery
        WHERE Name = @Name;

        IF CHARINDEX(N'"Alliance":"t1_StatusId"', @Cols) = 0
        BEGIN
            IF CHARINDEX(N'{"TableName":"Edms.Document","ColumnName":"IsLatest"', @Cols) > 0
                SET @Cols = REPLACE(
                    @Cols,
                    N'{"TableName":"Edms.Document","ColumnName":"IsLatest"',
                    @StatusCol + N',{"TableName":"Edms.Document","ColumnName":"IsLatest"');
            ELSE IF CHARINDEX(N'{"TableName":null,"ColumnName":"IsLatest"', @Cols) > 0
                SET @Cols = REPLACE(
                    @Cols,
                    N'{"TableName":null,"ColumnName":"IsLatest"',
                    @StatusCol + N',{"TableName":null,"ColumnName":"IsLatest"');
            ELSE
                THROW 51670, '51670 IsLatest column anchor was not found', 1;
        END

        SELECT @Sql = CustomQuery
        FROM OPENJSON(@Qj)
        WITH (CustomQuery NVARCHAR(MAX) N'$.CustomQuery');

        IF @Sql IS NULL
            THROW 51671, N'CustomQuery نمایه مدرک خوانده نشد.', 1;

        IF CHARINDEX(N'AS [t1_StatusId]', @Sql) = 0
        BEGIN
            IF CHARINDEX(N'[t1].[Status] AS [t1_Status],', @Sql) = 0
                THROW 51672, N'عبارت وضعیت در کوئری نمایه مدرک پیدا نشد.', 1;

            SET @Sql = REPLACE(
                @Sql,
                N'[t1].[Status] AS [t1_Status],',
                N'[t1].[Status] AS [t1_Status],' + NCHAR(10) + N'    [t1].[Status] AS [t1_StatusId],');
        END

        SET @Qj = JSON_MODIFY(@Qj, N'$.CustomQuery', @Sql);
        IF @Qj IS NULL OR ISJSON(@Qj) <> 1
            THROW 51673, N'بازسازی QueryJson نمایه مدرک ناموفق بود.', 1;

        SELECT @Check = CustomQuery
        FROM OPENJSON(@Qj)
        WITH (CustomQuery NVARCHAR(MAX) N'$.CustomQuery');

        IF CHARINDEX(N'AS [t1_StatusId]', @Check) = 0
            OR CHARINDEX(N'FROM [Edms].[Document]', @Check) = 0
            OR LEN(@Check) + 20 < LEN(@Sql)
            THROW 51674, '51674 query roundtrip lost status id', 1;

        SET @Ev = JSON_MODIFY(
            CASE WHEN ISJSON(@Ev) = 1 THEN @Ev ELSE N'{"onSelectedRow":"","onRowAdded":""}' END,
            N'$.onRowAdded',
            @OnRow);

        IF @Ev IS NULL OR ISJSON(@Ev) <> 1 OR CHARINDEX(N'#0FF135', @Ev) = 0
            THROW 51675, N'اسکریپت رنگ وضعیت ذخیره نشد.', 1;

        IF ISJSON(@Cols) <> 1 OR CHARINDEX(N'"Alliance":"t1_StatusId"', @Cols) = 0
            THROW 51676, N'ستون کد وضعیت در ColumnsJson ذخیره نشد.', 1;

        UPDATE system.SavedQuery
        SET ColumnsJson = @Cols,
            QueryJson = @Qj,
            EventScriptsJson = @Ev,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @Name;

        SET @Updated += 1;
        FETCH NEXT FROM cur INTO @Name;
    END

    CLOSE cur;
    DEALLOCATE cur;

    IF @Updated < 1
        THROW 51677, N'هیچ نمایه مدرکی به‌روز نشد.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    IF CURSOR_STATUS('local', 'cur') >= 0
    BEGIN
        CLOSE cur;
        DEALLOCATE cur;
    END
    ELSE IF CURSOR_STATUS('local', 'cur') = -1
        DEALLOCATE cur;

    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT
    q.Name,
    CASE WHEN CHARINDEX(N'"Alliance":"t1_StatusId"', q.ColumnsJson) > 0 THEN 1 ELSE 0 END AS HasStatusColumn,
    CASE WHEN CHARINDEX(N't1_StatusId', q.QueryJson) > 0 THEN 1 ELSE 0 END AS HasStatusSelect,
    CASE WHEN CHARINDEX(N'#0FF135', q.EventScriptsJson) > 0 THEN 1 ELSE 0 END AS HasColorScript,
    CASE WHEN ISJSON(q.ColumnsJson) = 1 AND ISJSON(q.QueryJson) = 1 AND ISJSON(q.EventScriptsJson) = 1 THEN 1 ELSE 0 END AS JsonOk
FROM system.SavedQuery q
WHERE q.EntityFullName = N'entities.app.edms.document'
ORDER BY q.Name;
