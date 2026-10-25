/*
  شناسنامه محصولات (PartExtraInfo):
  - نمایه‌های موجود فقط آخرین ریویژن هر کالا و نوع شناسنامه را برمی‌گردانند.
  - نمایه «پیشینه شناسنامه ها» همه ریویژن‌ها را با ستون ریویژن و نوع شناسنامه نشان می‌دهد.
  - دسترسی پیشینه = اجتماع نقش‌هایی که به نمایه‌های فعلی شناسنامه دسترسی دارند.
  UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-partextrainfo-history';
    DECLARE @HistName NVARCHAR(100) = N'partextrainfo_history';
    DECLARE @HistTitle NVARCHAR(200) = N'پیشینه شناسنامه ها';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.inv.partextrainfo';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Inv.PartExtraInfo';
    DECLARE @Pred NVARCHAR(MAX) = N'ISNULL([t1].[Revision], 0) = ISNULL((SELECT MAX(ISNULL([x].[Revision], 0)) FROM [Inv].[PartExtraInfo] AS [x] WHERE [x].[ProductId] = [t1].[ProductId] AND [x].[InfoType] = [t1].[InfoType]), 0) /*last-revision-only*/';

    UPDATE q
    SET QueryJson = JSON_MODIFY(
            q.QueryJson,
            '$.CustomQuery',
            CASE
                WHEN CHARINDEX(N'/*last-revision-only*/', cq.CustomQuery) > 0 THEN cq.CustomQuery
                WHEN CHARINDEX(N'WHERE [t1].[InfoType]', cq.CustomQuery) > 0 THEN cq.CustomQuery + CHAR(13) + CHAR(10) + N'AND ' + @Pred
                ELSE cq.CustomQuery + CHAR(13) + CHAR(10) + N'WHERE ' + @Pred
            END),
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    FROM system.SavedQuery q
    CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') cq
    WHERE q.Name IN (
        N'partextrainfo_listinfo',
        N'vw_PartextrainfoOilInject',
        N'vw_PartextrainfoOilFree',
        N'vw_PartextrainfoDerayer',
        N'vw_PartextrainfoPsa',
        N'vw_PartextrainfoStatic');

    IF @@ROWCOUNT <> 6
        THROW 50011, N'expected 6 PartExtraInfo profiles to update', 1;

    DECLARE @HistQuery NVARCHAR(MAX);
    DECLARE @BaseCols NVARCHAR(MAX);
    DECLARE @BaseQueryJson NVARCHAR(MAX);
    DECLARE @Act NVARCHAR(MAX);
    DECLARE @Btn NVARCHAR(MAX);
    DECLARE @Events NVARCHAR(MAX);

    SELECT
        @HistQuery = cq.CustomQuery,
        @BaseCols = q.ColumnsJson,
        @BaseQueryJson = q.QueryJson,
        @Act = q.ActionOptions,
        @Btn = q.CustomActionButtonsJson,
        @Events = q.EventScriptsJson
    FROM system.SavedQuery q
    CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') cq
    WHERE q.Name = N'partextrainfo_listinfo';

    IF @HistQuery IS NULL OR @BaseCols IS NULL
        THROW 50012, N'partextrainfo_listinfo was not found', 1;

    DECLARE @PredStart INT = CHARINDEX(@Pred, @HistQuery);
    IF @PredStart > 0
    BEGIN
        DECLARE @i INT = @PredStart - 1;
        WHILE @i > 0 AND SUBSTRING(@HistQuery, @i, 1) IN (N' ', NCHAR(9), NCHAR(10), NCHAR(13))
            SET @i -= 1;

        IF @i >= 3 AND SUBSTRING(@HistQuery, @i - 2, 3) = N'AND'
            SET @HistQuery = STUFF(@HistQuery, @i - 2, (@PredStart + LEN(@Pred)) - (@i - 2), N'');
        ELSE IF @i >= 5 AND SUBSTRING(@HistQuery, @i - 4, 5) = N'WHERE'
            SET @HistQuery = STUFF(@HistQuery, @i - 4, (@PredStart + LEN(@Pred)) - (@i - 4), N'');
        ELSE
            THROW 50013, N'could not strip last-revision filter from history query', 1;
    END

    IF CHARINDEX(N'/*last-revision-only*/', @HistQuery) > 0
        THROW 50014, N'history query still contains the last-revision filter', 1;

    IF CHARINDEX(N'AS [t1_Revision]', @HistQuery) = 0
        SET @HistQuery = REPLACE(
            @HistQuery,
            N'[t2].[Name] AS [t2_Name],',
            N'[t2].[Name] AS [t2_Name],' + CHAR(13) + CHAR(10)
                + N'    [t1].[Revision] AS [t1_Revision],' + CHAR(13) + CHAR(10)
                + N'    [t1].[InfoType] AS [t1_InfoType],');

    IF CHARINDEX(N'AS [t1_Revision]', @HistQuery) = 0 OR CHARINDEX(N'AS [t1_InfoType]', @HistQuery) = 0
        THROW 50015, N'history query is missing revision or info type', 1;

    DECLARE @RevCol NVARCHAR(MAX) = N'{"TableName":"Inv.PartExtraInfo","ColumnName":"t1_Revision","DisplayName":"ریویژن","Alliance":"t1_Revision","Address":"[t1].[Revision]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""}';
    DECLARE @TypeCol NVARCHAR(MAX) = N'{"TableName":"Inv.PartExtraInfo","ColumnName":"t1_InfoType","DisplayName":"نوع شناسنامه","Alliance":"t1_InfoType","Address":"[t1].[InfoType]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartExtraInfoInfoTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""}';
    DECLARE @NameOrd INT = (
        SELECT CAST(j.[key] AS INT)
        FROM OPENJSON(@BaseCols) j
        WHERE JSON_VALUE(j.[value], '$.Alliance') = N't2_Name');

    IF @NameOrd IS NULL
        THROW 50016, N't2_Name column was not found on partextrainfo_listinfo', 1;

    DECLARE @Cols NVARCHAR(MAX) = (
        SELECT N'[' + STRING_AGG(CAST(piece AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY ord) + N']'
        FROM (
            SELECT CAST(j.[key] AS INT) * 10 AS ord, j.[value] AS piece
            FROM OPENJSON(@BaseCols) j
            WHERE JSON_VALUE(j.[value], '$.Alliance') NOT IN (N't1_Revision', N't1_InfoType')
            UNION ALL
            SELECT @NameOrd * 10 + 1, @RevCol
            UNION ALL
            SELECT @NameOrd * 10 + 2, @TypeCol
        ) s
    );

    IF ISJSON(@Cols) <> 1 OR ISJSON(@RevCol) <> 1 OR ISJSON(@TypeCol) <> 1
        THROW 50017, N'history ColumnsJson is not valid JSON', 1;

    IF NOT EXISTS (
        SELECT 1 FROM OPENJSON(@Cols)
        WHERE JSON_VALUE([value], '$.Alliance') = N't1_Revision'
          AND JSON_VALUE([value], '$.DisplayName') = N'ریویژن')
        THROW 50018, N'revision column DisplayName is not ریویژن', 1;

    DECLARE @Qj NVARCHAR(MAX) = JSON_MODIFY(@BaseQueryJson, '$.CustomQuery', @HistQuery);
    IF ISJSON(@Qj) <> 1
        THROW 50019, N'history QueryJson is not valid JSON', 1;

    DECLARE @Pid BIGINT;
    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @HistName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @HistTitle,
            QueryJson = @Qj,
            ColumnsJson = @Cols,
            EntityFullName = @Ent,
            Mode = 1,
            Type = 1,
            ActionOptions = @Act,
            CustomActionButtonsJson = @Btn,
            EventScriptsJson = @Events,
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @HistName;

        SELECT @Pid = Id FROM system.SavedQuery WHERE Name = @HistName;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@HistName, @HistTitle, @Qj, @Cols, N'{}', @Btn, @Events,
             1, 1, @Ent, @Act,
             1, 1, @SeedUser, @SeedUser,
             @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END

    DELETE ra
    FROM system.RoleAccess ra
    WHERE ra.ActionAccessType = 3
      AND ra.RowId = @Pid;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(@Pid AS NVARCHAR(20)),
        3,
        7,
        @Pascal,
        @HistTitle,
        NULL,
        @Pid,
        r.Id,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.RoleAccess src
    INNER JOIN system.SavedQuery q ON q.Id = src.RowId
    INNER JOIN system.Role r ON r.Id = src.RoleId
    WHERE src.ActionAccessType = 3
      AND q.Name IN (
          N'partextrainfo_listinfo',
          N'vw_PartextrainfoOilInject',
          N'vw_PartextrainfoOilFree',
          N'vw_PartextrainfoDerayer',
          N'vw_PartextrainfoPsa',
          N'vw_PartextrainfoStatic');

    IF @@ROWCOUNT < 1
        THROW 50020, N'no roles were granted the history profile', 1;

    DECLARE @Checks TABLE (Name NVARCHAR(200) PRIMARY KEY, SqlText NVARCHAR(MAX));
    INSERT INTO @Checks (Name, SqlText)
    SELECT q.Name, N'SELECT TOP (0) 1 AS [ok] FROM (' + cq.CustomQuery + N') AS [v]'
    FROM system.SavedQuery q
    CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') cq
    WHERE q.Name IN (
        N'partextrainfo_listinfo',
        N'vw_PartextrainfoOilInject',
        N'vw_PartextrainfoOilFree',
        N'vw_PartextrainfoDerayer',
        N'vw_PartextrainfoPsa',
        N'vw_PartextrainfoStatic',
        @HistName);

    IF (SELECT COUNT(*) FROM @Checks) <> 7
        THROW 50021, N'expected 7 profiles to compile', 1;

    DECLARE @CheckName NVARCHAR(200);
    DECLARE @CheckSql NVARCHAR(MAX);
    WHILE EXISTS (SELECT 1 FROM @Checks)
    BEGIN
        SELECT TOP (1) @CheckName = Name, @CheckSql = SqlText FROM @Checks ORDER BY Name;
        EXEC sys.sp_executesql @CheckSql;
        DELETE FROM @Checks WHERE Name = @CheckName;
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT q.Id, q.Name, q.Title,
       CASE WHEN cq.CustomQuery LIKE N'%/*last-revision-only*/%' THEN 1 ELSE 0 END AS HasLastRevisionFilter
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') cq
WHERE q.Name IN (
    N'partextrainfo_listinfo',
    N'vw_PartextrainfoOilInject',
    N'vw_PartextrainfoOilFree',
    N'vw_PartextrainfoDerayer',
    N'vw_PartextrainfoPsa',
    N'vw_PartextrainfoStatic',
    N'partextrainfo_history')
ORDER BY q.Id;

SELECT JSON_VALUE(j.[value], '$.Alliance') AS Alliance,
       JSON_VALUE(j.[value], '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.[value], '$.Visible') AS Visible
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'partextrainfo_history'
  AND JSON_VALUE(j.[value], '$.Alliance') IN (N't1_Revision', N't1_InfoType', N't1_IsLastRevision', N't2_Code');

SELECT r.Name AS RoleName
FROM system.RoleAccess ra
INNER JOIN system.SavedQuery q ON q.Id = ra.RowId AND q.Name = N'partextrainfo_history'
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType = 3
ORDER BY r.Name;
