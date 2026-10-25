/*
  نمایه‌های اقلام سفارش ساخت: ستون‌های بولی به‌جای بله/خیر مقدار TRUE/FALSE نشان می‌دهند.
  تبدیل بله/خیر در گرید سراسری است؛ Render همین نمایه‌ها خروجی سلول را TRUE/FALSE می‌کند.
  Idempotent.
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderItem_DataProfiles_BooleanTrueFalse.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Render NVARCHAR(MAX) = N'function(data, type, row) { if (data === true || data === ''بله'' || data === ''TRUE'' || data === 1 || data === ''1'') return ''TRUE''; return ''FALSE''; }';

    DECLARE @id BIGINT;
    DECLARE @name NVARCHAR(200);
    DECLARE @json NVARCHAR(MAX);
    DECLARE @i INT;
    DECLARE @n INT;
    DECLARE @path NVARCHAR(40);
    DECLARE @systemType NVARCHAR(20);
    DECLARE @systemTypeName NVARCHAR(50);
    DECLARE @changed INT;

    DECLARE profile_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT q.Id, q.Name, q.ColumnsJson
        FROM system.SavedQuery q
        WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
          AND q.Type = 1
          AND ISJSON(q.ColumnsJson) = 1;

    OPEN profile_cur;
    FETCH NEXT FROM profile_cur INTO @id, @name, @json;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @n = (SELECT COUNT(*) FROM OPENJSON(@json));
        SET @i = 0;
        SET @changed = 0;

        WHILE @i < @n
        BEGIN
            SET @systemType = JSON_VALUE(@json, N'$[' + CAST(@i AS NVARCHAR(10)) + N'].SystemType');
            SET @systemTypeName = JSON_VALUE(@json, N'$[' + CAST(@i AS NVARCHAR(10)) + N'].SystemTypeName');

            IF @systemType = N'1' OR @systemTypeName = N'Boolean'
            BEGIN
                SET @path = N'$[' + CAST(@i AS NVARCHAR(10)) + N'].Render';
                SET @json = JSON_MODIFY(@json, @path, @Render);
                SET @changed = @changed + 1;
            END

            SET @i = @i + 1;
        END

        IF @changed = 0 OR @json IS NULL OR ISJSON(@json) <> 1
        BEGIN
            DECLARE @err NVARCHAR(400) = N'ستون بولی در نمایه به‌روز نشد: ' + @name;
            THROW 51670, @err, 1;
        END

        UPDATE system.SavedQuery
        SET ColumnsJson = @json,
            ModifiedById = 1,
            ModifiedByName = N'poi-boolean-true-false',
            ModifiedDateMiladiDateTime = SYSDATETIME()
        WHERE Id = @id;

        PRINT N'UPDATED ' + @name + N' boolColumns=' + CAST(@changed AS NVARCHAR(10));

        FETCH NEXT FROM profile_cur INTO @id, @name, @json;
    END

    CLOSE profile_cur;
    DEALLOCATE profile_cur;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery q
        CROSS APPLY OPENJSON(q.ColumnsJson) j
        WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
          AND q.Type = 1
          AND (
                JSON_VALUE(j.value, N'$.SystemType') = N'1'
             OR JSON_VALUE(j.value, N'$.SystemTypeName') = N'Boolean'
          )
          AND ISNULL(JSON_VALUE(j.value, N'$.Render'), N'') NOT LIKE N'%return ''TRUE''%'
    )
        THROW 51671, N'بعضی ستون‌های بولی هنوز Render مربوط به TRUE/FALSE را ندارند.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
