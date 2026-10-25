/*
  ستون تاریخ پایان تست میلادی را در نمایه‌های اقلام سفارش ساخت،
  هم‌ترتیب با HTS، بعد از تاریخ پایان تولید میلادی و قبل از تاریخ آماده‌سازی میلادی می‌گذارد.

  معادل گرید قبلی: TestingEndDateInEurope بعد از ProductionEndDateInEurope.

  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderItem_TestingEndMiladi_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @OldSelect NVARCHAR(200) = N'[t1].[ProductionEndMiladiDate] AS [t1_ProductionEndMiladiDate],';
DECLARE @NewSelect NVARCHAR(400) =
    N'[t1].[ProductionEndMiladiDate] AS [t1_ProductionEndMiladiDate],'
    + NCHAR(92) + N'n    [t1].[TestingEndMiladiDate] AS [t1_TestingEndMiladiDate],';

UPDATE q
SET q.QueryJson = REPLACE(q.QueryJson, @OldSelect, @NewSelect)
FROM system.SavedQuery q
WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
  AND CHARINDEX(@OldSelect, q.QueryJson) > 0
  AND CHARINDEX(N'[t1].[TestingEndMiladiDate] AS [t1_TestingEndMiladiDate]', q.QueryJson) = 0;

;WITH src AS (
    SELECT
        q.Id,
        CAST(c.[key] AS int) AS ord,
        c.value AS obj,
        JSON_VALUE(c.value, N'$.Alliance') AS Alliance
    FROM system.SavedQuery q
    CROSS APPLY OPENJSON(q.ColumnsJson) c
    WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
      AND EXISTS (
          SELECT 1
          FROM OPENJSON(q.ColumnsJson) x
          WHERE JSON_VALUE(x.value, N'$.Alliance') = N't1_ProductionEndMiladiDate'
      )
      AND NOT EXISTS (
          SELECT 1
          FROM OPENJSON(q.ColumnsJson) x
          WHERE JSON_VALUE(x.value, N'$.Alliance') = N't1_TestingEndMiladiDate'
      )
),
pieces AS (
    SELECT Id, ord, 0 AS seq, obj
    FROM src
    UNION ALL
    SELECT
        Id,
        ord,
        1,
        JSON_MODIFY(
            JSON_MODIFY(
                JSON_MODIFY(
                    JSON_MODIFY(obj, N'$.ColumnName', N'TestingEndMiladiDate'),
                    N'$.DisplayName', N'تاریخ پایان تست'),
                N'$.Alliance', N't1_TestingEndMiladiDate'),
            N'$.Address', N'[t1].[TestingEndMiladiDate]')
    FROM src
    WHERE Alliance = N't1_ProductionEndMiladiDate'
),
rebuilt AS (
    SELECT
        Id,
        N'[' + STRING_AGG(CAST(obj AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY ord, seq) + N']' AS ColumnsJson
    FROM pieces
    GROUP BY Id
)
UPDATE q
SET
    q.ColumnsJson = r.ColumnsJson,
    q.ModifiedDateMiladiDateTime = SYSDATETIME(),
    q.ModifiedByName = N'add-testing-end-miladi'
FROM system.SavedQuery q
INNER JOIN rebuilt r ON r.Id = q.Id
WHERE ISJSON(r.ColumnsJson) = 1;

IF EXISTS (
    SELECT 1
    FROM system.SavedQuery q
    WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
      AND CHARINDEX(N'[t1].[ProductionEndMiladiDate] AS [t1_ProductionEndMiladiDate],', q.QueryJson) > 0
      AND (
          ISJSON(q.QueryJson) <> 1
          OR ISJSON(q.ColumnsJson) <> 1
          OR CHARINDEX(N'[t1].[TestingEndMiladiDate] AS [t1_TestingEndMiladiDate]', q.QueryJson) = 0
          OR NOT EXISTS (
              SELECT 1
              FROM OPENJSON(q.ColumnsJson) c
              WHERE JSON_VALUE(c.value, N'$.Alliance') = N't1_TestingEndMiladiDate'
                AND JSON_VALUE(c.value, N'$.DisplayName') = N'تاریخ پایان تست'
          )
      )
)
BEGIN
    ROLLBACK TRAN;
    THROW 50001, N'TestingEndMiladiDate profile update failed validation.', 1;
END;

COMMIT TRAN;

SELECT q.Name,
    CAST(c.[key] AS int) AS Ord,
    JSON_VALUE(c.value, N'$.Alliance') AS Alliance,
    JSON_VALUE(c.value, N'$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) c
WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
  AND JSON_VALUE(c.value, N'$.Alliance') IN (
      N't1_ProductionEndMiladiDate',
      N't1_TestingEndMiladiDate',
      N't1_PreparationMiladiDate'
  )
ORDER BY q.Name, CAST(c.[key] AS int);
