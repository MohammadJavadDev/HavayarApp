/*
  نمایه‌های Query اقلام سفارش ساخت:
  ColumnName را با alias خروجی SELECT (Alliance) یکی می‌کند.
  مثال: ColumnName = Id و Alliance = t1_Id  →  ColumnName = t1_Id

  نمایه‌های Diagram (Mode = 0) عوض نمی‌شوند؛ آنجا ColumnName نام ستون واقعی جدول است
  و کوئری از روی [alias].[ColumnName] AS [Alliance] ساخته می‌شود.

  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderItem_DataProfiles_ColumnNameAlias.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @id BIGINT;
DECLARE @json NVARCHAR(MAX);
DECLARE @i INT;
DECLARE @n INT;
DECLARE @path NVARCHAR(40);
DECLARE @alliance NVARCHAR(400);
DECLARE @columnName NVARCHAR(400);
DECLARE @changed INT = 0;

DECLARE profile_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT q.Id, q.ColumnsJson
    FROM system.SavedQuery q
    WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
      AND q.Type = 1
      AND q.Mode = 1
      AND ISJSON(q.ColumnsJson) = 1;

OPEN profile_cur;
FETCH NEXT FROM profile_cur INTO @id, @json;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @n = (SELECT COUNT(*) FROM OPENJSON(@json));
    SET @i = 0;

    WHILE @i < @n
    BEGIN
        SET @path = N'$[' + CAST(@i AS NVARCHAR(10)) + N'].ColumnName';
        SET @alliance = JSON_VALUE(@json, N'$[' + CAST(@i AS NVARCHAR(10)) + N'].Alliance');
        SET @columnName = JSON_VALUE(@json, @path);

        IF @alliance IS NOT NULL AND ISNULL(@columnName, N'') <> @alliance
        BEGIN
            SET @json = JSON_MODIFY(@json, @path, @alliance);
            SET @changed += 1;
        END

        SET @i += 1;
    END

    IF ISJSON(@json) <> 1
    BEGIN
        ROLLBACK TRAN;
        THROW 51630, N'ColumnsJson بعد از هم‌نام‌سازی alias معتبر نیست.', 1;
    END

    UPDATE system.SavedQuery
    SET ColumnsJson = @json,
        ModifiedDateMiladiDateTime = SYSDATETIME(),
        ModifiedByName = N'align-column-alias'
    WHERE Id = @id;

    FETCH NEXT FROM profile_cur INTO @id, @json;
END

CLOSE profile_cur;
DEALLOCATE profile_cur;

IF EXISTS (
    SELECT 1
    FROM system.SavedQuery q
    CROSS APPLY OPENJSON(q.ColumnsJson) c
    WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
      AND q.Type = 1
      AND q.Mode = 1
      AND (
          ISJSON(q.ColumnsJson) <> 1
          OR (
              JSON_VALUE(c.value, N'$.Alliance') IS NOT NULL
              AND ISNULL(JSON_VALUE(c.value, N'$.ColumnName'), N'') <> JSON_VALUE(c.value, N'$.Alliance')
          )
          OR JSON_VALUE(c.value, N'$.DisplayName') LIKE N'%Ø%'
          OR JSON_VALUE(c.value, N'$.DisplayName') LIKE N'%Ù%'
      )
)
BEGIN
    ROLLBACK TRAN;
    THROW 51631, N'هم‌نام‌سازی ColumnName با alias ناموفق بود.', 1;
END

COMMIT TRAN;

SELECT
    q.Name,
    SUM(CASE WHEN ISNULL(JSON_VALUE(c.value, N'$.ColumnName'), N'') = ISNULL(JSON_VALUE(c.value, N'$.Alliance'), N'') THEN 1 ELSE 0 END) AS Matched,
    SUM(CASE WHEN ISNULL(JSON_VALUE(c.value, N'$.ColumnName'), N'') <> ISNULL(JSON_VALUE(c.value, N'$.Alliance'), N'') THEN 1 ELSE 0 END) AS Mismatched
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) c
WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
  AND q.Type = 1
  AND q.Mode = 1
GROUP BY q.Name
ORDER BY q.Name;
