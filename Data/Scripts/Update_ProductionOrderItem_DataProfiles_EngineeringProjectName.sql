/*
  نمایه‌های اقلام سفارش ساخت:
  - ستون «نام پروژه مهندسی» = Edms.Project.Name
    (همان Edms_Project.Project_Name سیستم قدیم، وصل با ProductionOrder.EdmsProject = Project.HtsId)
  - جای ستون: بلافاصله بعد از «عنوان پروژه» (t10_Title) و قبل از «توضیحات فروش»
    نمایه آماده ارسال عنوان پروژه ندارد؛ ستون قبل از «ملاحظات مهندسی» می‌نشیند.
  - ستون تاریخچه (آیکن + / custom_id_lx6hqfp5w) در همه نمایه‌ها اولین ستون است.

  Idempotent.
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderItem_DataProfiles_EngineeringProjectName.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @NewCol NVARCHAR(MAX) = N'{"TableName":"Edms.Project","ColumnName":"t11_Name","DisplayName":"نام پروژه مهندسی","Alliance":"t11_Name","Address":"[t11].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""}';

    IF ISJSON(@NewCol) <> 1
        THROW 51650, N'JSON ستون نام پروژه مهندسی معتبر نیست.', 1;

    DECLARE @id BIGINT;
    DECLARE @name NVARCHAR(200);
    DECLARE @queryJson NVARCHAR(MAX);
    DECLARE @columns NVARCHAR(MAX);
    DECLARE @cq NVARCHAR(MAX);
    DECLARE @anchorTitle INT;
    DECLARE @anchorEng INT;
    DECLARE @built NVARCHAR(MAX);
    DECLARE @obj NVARCHAR(MAX);
    DECLARE @err NVARCHAR(4000);
    DECLARE @desc NVARCHAR(MAX);

    DECLARE @cols TABLE (
        Ord INT NOT NULL,
        Alliance NVARCHAR(400) NULL,
        Obj NVARCHAR(MAX) NOT NULL
    );

    DECLARE profile_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT q.Id, q.Name, q.QueryJson, q.ColumnsJson
        FROM system.SavedQuery q
        WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
          AND q.Type = 1
          AND ISJSON(q.QueryJson) = 1
          AND ISJSON(q.ColumnsJson) = 1
        ORDER BY q.Id;

    OPEN profile_cur;
    FETCH NEXT FROM profile_cur INTO @id, @name, @queryJson, @columns;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @cq = CustomQuery
        FROM OPENJSON(@queryJson)
        WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery');

        IF @cq IS NULL OR LEN(@cq) < 20
        BEGIN
            SET @err = N'CustomQuery خالی است: ' + @name;
            THROW 51651, @err, 1;
        END

        IF CHARINDEX(N'AS [t11_Name]', @cq) = 0
        BEGIN
            IF CHARINDEX(N'[t10].[Title] AS [t10_Title],', @cq) > 0
                SET @cq = REPLACE(
                    @cq,
                    N'[t10].[Title] AS [t10_Title],',
                    N'[t10].[Title] AS [t10_Title],' + NCHAR(10) + N'    [t11].[Name] AS [t11_Name],');
            ELSE IF CHARINDEX(N'AS [tWh_InWarehouseTimeInDay]', @cq) > 0
                SET @cq = REPLACE(
                    @cq,
                    N'AS [tWh_InWarehouseTimeInDay]',
                    N'AS [tWh_InWarehouseTimeInDay],' + NCHAR(10) + N'    [t11].[Name] AS [t11_Name]');
            ELSE
            BEGIN
                SET @err = N'جای درج SELECT برای نام پروژه مهندسی پیدا نشد: ' + @name;
                THROW 51652, @err, 1;
            END
        END

        IF CHARINDEX(N'[Edms].[Project] AS [t11]', @cq) = 0
        BEGIN
            IF CHARINDEX(N'ON [t2].[ProjectDlId] = [t10].[Id]', @cq) > 0
                SET @cq = REPLACE(
                    @cq,
                    N'ON [t2].[ProjectDlId] = [t10].[Id]',
                    N'ON [t2].[ProjectDlId] = [t10].[Id]' + NCHAR(10)
                    + N'LEFT JOIN [Edms].[Project] AS [t11]' + NCHAR(10)
                    + N'    ON [t11].[HtsId] = [t2].[EdmsProject]' + NCHAR(10)
                    + N'   AND [t11].[HtsId] <> 0');
            ELSE IF CHARINDEX(N'LEFT JOIN [Sale].[ProductionOrder] AS [t2] ON [t1].[ProductionOrderId] = [t2].[Id]', @cq) > 0
                SET @cq = REPLACE(
                    @cq,
                    N'LEFT JOIN [Sale].[ProductionOrder] AS [t2] ON [t1].[ProductionOrderId] = [t2].[Id]',
                    N'LEFT JOIN [Sale].[ProductionOrder] AS [t2] ON [t1].[ProductionOrderId] = [t2].[Id]' + NCHAR(10)
                    + N'LEFT JOIN [Edms].[Project] AS [t11]' + NCHAR(10)
                    + N'    ON [t11].[HtsId] = [t2].[EdmsProject]' + NCHAR(10)
                    + N'   AND [t11].[HtsId] <> 0');
            ELSE IF CHARINDEX(N'ON [t1].[ProductionOrderId] = [t2].[Id]', @cq) > 0
                SET @cq = REPLACE(
                    @cq,
                    N'ON [t1].[ProductionOrderId] = [t2].[Id]',
                    N'ON [t1].[ProductionOrderId] = [t2].[Id]' + NCHAR(10)
                    + N'LEFT JOIN [Edms].[Project] AS [t11]' + NCHAR(10)
                    + N'    ON [t11].[HtsId] = [t2].[EdmsProject]' + NCHAR(10)
                    + N'   AND [t11].[HtsId] <> 0');
            ELSE
            BEGIN
                SET @err = N'جای درج JOIN پروژه مهندسی پیدا نشد: ' + @name;
                THROW 51653, @err, 1;
            END
        END

        SET @desc = @cq;
        IF CHARINDEX(N'@CurrentUserId', @desc) > 0
            SET @desc = N'DECLARE @CurrentUserId BIGINT = NULL;' + NCHAR(10) + @desc;

        SET @err = NULL;
        SELECT TOP (1) @err = error_message
        FROM sys.dm_exec_describe_first_result_set(@desc, NULL, 0)
        WHERE error_message IS NOT NULL;

        IF @err IS NOT NULL
        BEGIN
            SET @err = @name + N' — ' + @err;
            THROW 51654, @err, 1;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM sys.dm_exec_describe_first_result_set(@desc, NULL, 0)
            WHERE name = N't11_Name'
        )
            THROW 51655, N'خروجی کوئری ستون t11_Name ندارد.', 1;

        SET @queryJson = JSON_MODIFY(@queryJson, N'$.CustomQuery', @cq);
        IF @queryJson IS NULL OR ISJSON(@queryJson) <> 1
            THROW 51656, N'QueryJson بعد از ویرایش معتبر نیست.', 1;

        DELETE FROM @cols;

        INSERT INTO @cols (Ord, Alliance, Obj)
        SELECT CAST(j.[key] AS INT), JSON_VALUE(j.[value], N'$.Alliance'), j.[value]
        FROM OPENJSON(@columns) j
        WHERE ISNULL(JSON_VALUE(j.[value], N'$.Alliance'), N'') <> N't11_Name';

        SET @anchorTitle = (SELECT TOP (1) Ord FROM @cols WHERE Alliance = N't10_Title');
        SET @anchorEng = (SELECT TOP (1) Ord FROM @cols WHERE Alliance = N't1_EngineeringConsideration');

        IF @anchorTitle IS NOT NULL
        BEGIN
            UPDATE @cols SET Ord = Ord + 1 WHERE Ord > @anchorTitle;
            INSERT INTO @cols (Ord, Alliance, Obj) VALUES (@anchorTitle + 1, N't11_Name', @NewCol);
        END
        ELSE IF @anchorEng IS NOT NULL
        BEGIN
            UPDATE @cols SET Ord = Ord + 1 WHERE Ord >= @anchorEng;
            INSERT INTO @cols (Ord, Alliance, Obj) VALUES (@anchorEng, N't11_Name', @NewCol);
        END
        ELSE
            THROW 51657, N'ستون لنگر برای جای نام پروژه مهندسی پیدا نشد.', 1;

        SET @built = N'';

        DECLARE build_cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT Obj
            FROM @cols
            ORDER BY CASE WHEN Alliance = N'custom_id_lx6hqfp5w' THEN 0 ELSE 1 END, Ord;

        OPEN build_cur;
        FETCH NEXT FROM build_cur INTO @obj;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @built = N''
                SET @built = @obj;
            ELSE
                SET @built = @built + N',' + @obj;
            FETCH NEXT FROM build_cur INTO @obj;
        END
        CLOSE build_cur;
        DEALLOCATE build_cur;

        SET @built = N'[' + @built + N']';
        IF ISJSON(@built) <> 1
            THROW 51658, N'ColumnsJson ساخته‌شده معتبر نیست.', 1;

        UPDATE system.SavedQuery
        SET QueryJson = @queryJson,
            ColumnsJson = @built,
            ModifiedById = 1,
            ModifiedByName = N'poi-engineering-project-name',
            ModifiedDateMiladiDateTime = SYSDATETIME()
        WHERE Id = @id;

        PRINT N'UPDATED ' + @name;

        FETCH NEXT FROM profile_cur INTO @id, @name, @queryJson, @columns;
    END

    CLOSE profile_cur;
    DEALLOCATE profile_cur;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery q
        CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') cq
        WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
          AND q.Type = 1
          AND (
                CHARINDEX(N'AS [t11_Name]', cq.CustomQuery) = 0
             OR CHARINDEX(N'[Edms].[Project] AS [t11]', cq.CustomQuery) = 0
          )
    )
        THROW 51659, N'بعضی نمایه‌ها هنوز JOIN یا SELECT نام پروژه مهندسی را ندارند.', 1;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery q
        CROSS APPLY (
            SELECT TOP (1) JSON_VALUE(j.[value], N'$.Alliance') AS FirstAlliance
            FROM OPENJSON(q.ColumnsJson) j
            ORDER BY CAST(j.[key] AS INT)
        ) f
        WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
          AND q.Type = 1
          AND f.FirstAlliance <> N'custom_id_lx6hqfp5w'
    )
        THROW 51660, N'ستون تاریخچه در بعضی نمایه‌ها اولین ستون نیست.', 1;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery q
        WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
          AND q.Type = 1
          AND NOT EXISTS (
                SELECT 1
                FROM OPENJSON(q.ColumnsJson) j
                WHERE JSON_VALUE(j.[value], N'$.Alliance') = N't11_Name'
                  AND JSON_VALUE(j.[value], N'$.DisplayName') = N'نام پروژه مهندسی'
          )
    )
        THROW 51661, N'عنوان ستون نام پروژه مهندسی در بعضی نمایه‌ها درست نیست.', 1;

    IF EXISTS (
        SELECT q.Id
        FROM system.SavedQuery q
        CROSS APPLY OPENJSON(q.ColumnsJson) t10
        CROSS APPLY OPENJSON(q.ColumnsJson) t11
        WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
          AND q.Type = 1
          AND JSON_VALUE(t10.[value], N'$.Alliance') = N't10_Title'
          AND JSON_VALUE(t11.[value], N'$.Alliance') = N't11_Name'
          AND CAST(t11.[key] AS INT) <> CAST(t10.[key] AS INT) + 1
    )
        THROW 51662, N'نام پروژه مهندسی بلافاصله بعد از عنوان پروژه نیست.', 1;

    IF EXISTS (
        SELECT q.Id
        FROM system.SavedQuery q
        CROSS APPLY OPENJSON(q.ColumnsJson) eng
        CROSS APPLY OPENJSON(q.ColumnsJson) t11
        WHERE q.Name = N'POI_All_ReadyForShipping'
          AND JSON_VALUE(eng.[value], N'$.Alliance') = N't1_EngineeringConsideration'
          AND JSON_VALUE(t11.[value], N'$.Alliance') = N't11_Name'
          AND CAST(t11.[key] AS INT) <> CAST(eng.[key] AS INT) - 1
    )
        THROW 51663, N'در نمایه آماده ارسال، نام پروژه مهندسی قبل از ملاحظات مهندسی نیست.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

/*
  شناسه پروژه مهندسی روی سفارش‌هایی که در سیستم قدیم وصل است و در سیستم جدید خالی مانده.
  فقط وقتی شماره سفارش یکتا است و پروژه با همان HtsId در Edms.Project وجود دارد.
*/
UPDATE a
SET a.EdmsProject = h.EdmsProjectId,
    a.ModifiedByName = N'poi-engineering-project-name',
    a.ModifiedDateMiladiDateTime = SYSDATETIME()
FROM Sale.ProductionOrder a
INNER JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] h
    ON h.Number = a.ProductionOrderNumber
INNER JOIN Edms.Project p
    ON p.HtsId = h.EdmsProjectId
   AND p.HtsId <> 0
WHERE h.EdmsProjectId IS NOT NULL
  AND a.EdmsProject IS NULL
  AND (
        SELECT COUNT(*)
        FROM Sale.ProductionOrder d
        WHERE d.ProductionOrderNumber = a.ProductionOrderNumber
      ) = 1;

SELECT q.Id, q.Name,
       JSON_VALUE(firstCol.[value], N'$.DisplayName') AS FirstColumn,
       JSON_VALUE(t11.[value], N'$.DisplayName') AS EngineeringProjectName,
       CAST(t10.[key] AS INT) AS TitleOrd,
       CAST(t11.[key] AS INT) AS NameOrd
FROM system.SavedQuery q
CROSS APPLY (
    SELECT TOP (1) j.[value]
    FROM OPENJSON(q.ColumnsJson) j
    ORDER BY CAST(j.[key] AS INT)
) firstCol
CROSS APPLY OPENJSON(q.ColumnsJson) t11
OUTER APPLY OPENJSON(q.ColumnsJson) t10
WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
  AND q.Type = 1
  AND JSON_VALUE(t11.[value], N'$.Alliance') = N't11_Name'
  AND (
        JSON_VALUE(t10.[value], N'$.Alliance') = N't10_Title'
        OR t10.[key] IS NULL
      )
ORDER BY q.Id;
