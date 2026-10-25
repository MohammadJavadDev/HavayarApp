/*
================================================================================
Seed_ProductionOrderDelay_AllOrders_DataProfile.sql
================================================================================
نمایه «تمامی سفارش‌های ساخت» روی همان لیست تاخیر سفارش ساخت.
پایه: vw_All_Need_POD
تفاوت:
  - سفارش‌های بدون تاخیر هم می‌آیند (فیلتر MaxDelayDays برداشته شده)
  - سفارش پروژه‌ای (مدیر پروژه یا تیک کاربر) هم می‌آید
  - دو ستون: پروژه‌ای (دو مقداری) و علت پروژه‌ای شدن
      اگر کاربر تیک زده باشد: DelayNotCalculatedComment
      وگرنه اگر مدیر پروژه داشته باشد: دارای مدیر پروژه

تجهیزات برنامه‌ریزی (سفارش 1004) و سفارش غیرفعال، مثل نمایه مبنا، نمی‌آیند.
UTF-8 with BOM.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_ProductionOrderDelay_AllOrders_DataProfile.sql"
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-pod-all-orders';
    DECLARE @SourceName NVARCHAR(100) = N'vw_All_Need_POD';
    DECLARE @Name NVARCHAR(100) = N'vw_All_ProductionOrder_POD';
    DECLARE @Title NVARCHAR(200) = N'تمامی سفارش‌های ساخت';
    DECLARE @Entity NVARCHAR(300) = N'entities.app.pln.productionorderdelay';
    DECLARE @EntityPascal NVARCHAR(300) = N'Entities.App.Pln.ProductionOrderDelay';

    DECLARE @SourceQuery NVARCHAR(MAX);
    DECLARE @SourceColumns NVARCHAR(MAX);
    DECLARE @Buttons NVARCHAR(MAX);
    DECLARE @Actions NVARCHAR(MAX);
    DECLARE @Events NVARCHAR(MAX);

    SELECT
        @SourceQuery = CAST(j.[value] AS NVARCHAR(MAX)),
        @SourceColumns = s.ColumnsJson,
        @Buttons = s.CustomActionButtonsJson,
        @Actions = s.ActionOptions,
        @Events = s.EventScriptsJson
    FROM system.SavedQuery s
    CROSS APPLY OPENJSON(s.QueryJson) j
    WHERE s.Name = @SourceName
      AND j.[key] = N'CustomQuery';

    IF @SourceQuery IS NULL
        THROW 51040, N'کوئری نمایه vw_All_Need_POD پیدا نشد.', 1;

    IF CHARINDEX(N'AND po.ProjectManagerId IS NULL', @SourceQuery) = 0
        OR CHARINDEX(N'AND po.DelayNotCalculated = 0', @SourceQuery) = 0
        OR CHARINDEX(N'WHERE r.MaxDelayDays > 0 AND ISNULL(da.RegisteredDelayDays, 0) < r.MaxDelayDays', @SourceQuery) = 0
        OR CHARINDEX(N'FROM OrderAgg r', @SourceQuery) = 0
        THROW 51041, N'شکل کوئری نمایه مبنا برای ساخت نمایه تمامی سفارش‌ها قابل استفاده نیست.', 1;

    DECLARE @Query NVARCHAR(MAX) = @SourceQuery;
    SET @Query = REPLACE(@Query, N'AND po.ProjectManagerId IS NULL', N'');
    SET @Query = REPLACE(@Query, N'AND po.DelayNotCalculated = 0', N'');
    SET @Query = REPLACE(@Query, N'WHERE r.MaxDelayDays > 0 AND ISNULL(da.RegisteredDelayDays, 0) < r.MaxDelayDays', N'');

    DECLARE @Inject NVARCHAR(MAX) = N',
    CASE
        WHEN po.DelayNotCalculated = 1 OR po.ProjectManagerId IS NOT NULL THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS IsProjectOrder,
    CASE
        WHEN po.DelayNotCalculated = 1 THEN po.DelayNotCalculatedComment
        WHEN po.ProjectManagerId IS NOT NULL THEN N''دارای مدیر پروژه''
        ELSE NULL
    END AS ProjectReason
';

    SET @Query = REPLACE(@Query, N'FROM OrderAgg r', @Inject + N'FROM OrderAgg r');

    IF CHARINDEX(N'AND po.ProjectManagerId IS NULL', @Query) > 0
        OR CHARINDEX(N'AND po.DelayNotCalculated = 0', @Query) > 0
        OR CHARINDEX(N'MaxDelayDays > 0', @Query) > 0
        OR CHARINDEX(N'AS IsProjectOrder', @Query) = 0
        OR CHARINDEX(N'AS ProjectReason', @Query) = 0
        OR CHARINDEX(N'po.Id <> 1004', @Query) = 0
        THROW 51042, N'تبدیل کوئری نمایه تمامی سفارش‌ها کامل نشد.', 1;

    DECLARE @IsProjectCol NVARCHAR(MAX) = (
        SELECT
            N'Sale.ProductionOrder' AS TableName,
            N'IsProjectOrder' AS ColumnName,
            N'پروژه‌ای' AS DisplayName,
            N'IsProjectOrder' AS Alliance,
            N'CASE WHEN po.DelayNotCalculated = 1 OR po.ProjectManagerId IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END' AS Address,
            N'Boolean' AS SystemTypeName,
            0 AS SelectIndex,
            CAST(1 AS bit) AS Visible,
            CAST(0 AS bit) AS PrimaryKey,
            1 AS SystemType,
            CAST(NULL AS int) AS SortDirection,
            CAST(NULL AS int) AS SortOrder,
            CAST(0 AS bit) AS GroupBy,
            JSON_QUERY(N'[]') AS Options,
            CAST(NULL AS nvarchar(200)) AS OptionSetting,
            CAST(NULL AS nvarchar(50)) AS Aggregate,
            CAST(NULL AS nvarchar(max)) AS Render,
            CAST(0 AS bit) AS IsCustom,
            CAST(NULL AS nvarchar(50)) AS CustomColType,
            CAST(NULL AS nvarchar(max)) AS HtmlTemplate,
            CAST(NULL AS nvarchar(max)) AS BtnConfig,
            CAST(NULL AS nvarchar(max)) AS InputConfig,
            120 AS Width,
            CAST(1 AS bit) AS Filterable,
            CAST(1 AS bit) AS Sortable,
            N'' AS ClassName
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
    );

    DECLARE @ReasonCol NVARCHAR(MAX) = (
        SELECT
            N'Sale.ProductionOrder' AS TableName,
            N'ProjectReason' AS ColumnName,
            N'علت پروژه‌ای شدن' AS DisplayName,
            N'ProjectReason' AS Alliance,
            N'CASE WHEN po.DelayNotCalculated = 1 THEN po.DelayNotCalculatedComment WHEN po.ProjectManagerId IS NOT NULL THEN N''دارای مدیر پروژه'' ELSE NULL END' AS Address,
            N'String' AS SystemTypeName,
            0 AS SelectIndex,
            CAST(1 AS bit) AS Visible,
            CAST(0 AS bit) AS PrimaryKey,
            0 AS SystemType,
            CAST(NULL AS int) AS SortDirection,
            CAST(NULL AS int) AS SortOrder,
            CAST(0 AS bit) AS GroupBy,
            JSON_QUERY(N'[]') AS Options,
            CAST(NULL AS nvarchar(200)) AS OptionSetting,
            CAST(NULL AS nvarchar(50)) AS Aggregate,
            CAST(NULL AS nvarchar(max)) AS Render,
            CAST(0 AS bit) AS IsCustom,
            CAST(NULL AS nvarchar(50)) AS CustomColType,
            CAST(NULL AS nvarchar(max)) AS HtmlTemplate,
            CAST(NULL AS nvarchar(max)) AS BtnConfig,
            CAST(NULL AS nvarchar(max)) AS InputConfig,
            280 AS Width,
            CAST(1 AS bit) AS Filterable,
            CAST(1 AS bit) AS Sortable,
            N'' AS ClassName
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
    );

    DECLARE @ColumnsJson NVARCHAR(MAX);
    SELECT @ColumnsJson = N'[' + STRING_AGG(
        CAST(c.[value] AS NVARCHAR(MAX))
        + CASE
            WHEN JSON_VALUE(c.[value], '$.Alliance') = N'ProductionOrderNumber'
                THEN N',' + @IsProjectCol + N',' + @ReasonCol
            ELSE N''
          END,
        N',') WITHIN GROUP (ORDER BY CONVERT(int, c.[key])) + N']'
    FROM OPENJSON(@SourceColumns) c;

    IF @ColumnsJson IS NULL
        OR ISJSON(@ColumnsJson) <> 1
        OR CHARINDEX(N'"Alliance":"IsProjectOrder"', @ColumnsJson) = 0
        OR CHARINDEX(N'"Alliance":"ProjectReason"', @ColumnsJson) = 0
        THROW 51043, N'ستون‌های نمایه تمامی سفارش‌ها ساخته نشد.', 1;

    DECLARE @QueryJson NVARCHAR(MAX) = (
        SELECT
            JSON_QUERY(N'[]') AS Tables,
            JSON_QUERY(N'[]') AS Relations,
            JSON_QUERY(N'[]') AS Filters,
            JSON_QUERY(N'[]') AS CustomConditions,
            @Query AS CustomQuery,
            JSON_QUERY(N'[]') AS Parameters,
            CAST(NULL AS nvarchar(max)) AS Selects
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
    );

    IF @Events IS NULL OR ISJSON(@Events) <> 1
        SET @Events = N'{"onSelectedRow":"","onRowAdded":""}';
    IF @Buttons IS NULL OR ISJSON(@Buttons) <> 1
        SET @Buttons = N'[]';
    IF @Actions IS NULL OR ISJSON(@Actions) <> 1
        THROW 51044, N'دکمه‌های نمایه مبنا معتبر نیست.', 1;

    IF (SELECT COUNT(*) FROM system.Role WHERE Name IN (N'Pln.ProductionOrderDelay.FullAccess', N'ShowAllMenus')) <> 2
        THROW 51045, N'نقش دسترسی نمایه پیدا نشد.', 1;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            DiagramJson = N'{}',
            CustomActionButtonsJson = @Buttons,
            EventScriptsJson = @Events,
            ActionOptions = @Actions,
            EntityFullName = @Entity,
            Mode = 1,
            Type = 1,
            IsActive = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             ActionOptions, EntityFullName, Mode, Type, IsActive,
             CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
        VALUES
            (@Name, @Title, @QueryJson, @ColumnsJson, N'{}', @Buttons, @Events,
             @Actions, @Entity, 1, 1, 1,
             1, @SeedUser, 1, @SeedUser,
             @Now, @NowShamsi, @Now, @NowShamsi);
    END

    DECLARE @ProfileId BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @Name);

    DELETE FROM system.RoleAccess
    WHERE ActionAccessType = 3
      AND RowId = @ProfileId;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)),
        3, 7, @EntityPascal, @Title, NULL, @ProfileId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'Pln.ProductionOrderDelay.FullAccess', N'ShowAllMenus');

    IF @@ROWCOUNT <> 2
        THROW 51046, N'دسترسی نقش‌های نمایه ثبت نشد.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
