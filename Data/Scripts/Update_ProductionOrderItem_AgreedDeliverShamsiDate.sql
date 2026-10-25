/*
  ستون تاریخ تحویل توافقی شمسی را بعد از تاریخ تحویل توافقی
  در نمایه‌های قلم سفارش ساخت می‌گذارد.
  مقدار ستون از Sale.ProductionOrderItem.AgreedDeliverShamsiDate خوانده می‌شود
  (پر شده از AgreedDeliverDate با dbo.ToShamsiDate).

  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderItem_AgreedDeliverShamsiDate.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @OldSelect NVARCHAR(400) =
    N'[t1].[AgreedDeliverDate] AS [t1_AgreedDeliverDate],' + NCHAR(92) + N'n    ';
DECLARE @NewSelect NVARCHAR(500) =
    N'[t1].[AgreedDeliverDate] AS [t1_AgreedDeliverDate],' + NCHAR(92) + N'n    '
    + N'[t1].[AgreedDeliverShamsiDate] AS [t1_AgreedDeliverShamsiDate],' + NCHAR(92) + N'n    ';

UPDATE q
SET q.QueryJson = REPLACE(q.QueryJson, @OldSelect, @NewSelect)
FROM system.SavedQuery q
WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
  AND CHARINDEX(@OldSelect, q.QueryJson) > 0
  AND CHARINDEX(N'[t1].[AgreedDeliverShamsiDate] AS [t1_AgreedDeliverShamsiDate]', q.QueryJson) = 0;

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
          WHERE JSON_VALUE(x.value, N'$.Alliance') = N't1_AgreedDeliverDate'
      )
      AND NOT EXISTS (
          SELECT 1
          FROM OPENJSON(q.ColumnsJson) x
          WHERE JSON_VALUE(x.value, N'$.Alliance') = N't1_AgreedDeliverShamsiDate'
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
                    JSON_MODIFY(
                        JSON_MODIFY(
                            JSON_MODIFY(
                                JSON_MODIFY(obj, N'$.ColumnName', N'AgreedDeliverShamsiDate'),
                                N'$.DisplayName', N'تاریخ تحویل توافقی شمسی'),
                            N'$.Alliance', N't1_AgreedDeliverShamsiDate'),
                        N'$.Address', N'[t1].[AgreedDeliverShamsiDate]'),
                    N'$.SystemTypeName', N'DateShamsi'),
                N'$.SystemType', 5),
            N'$.Render', NULL)
    FROM src
    WHERE Alliance = N't1_AgreedDeliverDate'
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
    q.ModifiedDateShamsiDateTime = dbo.ToShamsiDate(CAST(SYSDATETIME() AS date), N'/') + N' ' + CONVERT(VARCHAR(8), SYSDATETIME(), 108),
    q.ModifiedByName = N'add-agreed-deliver-shamsi'
FROM system.SavedQuery q
INNER JOIN rebuilt r ON r.Id = q.Id
WHERE ISJSON(r.ColumnsJson) = 1;

IF EXISTS (
    SELECT 1
    FROM system.SavedQuery q
    WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
      AND CHARINDEX(@OldSelect, q.QueryJson) > 0
      AND (
          ISJSON(q.QueryJson) <> 1
          OR ISJSON(q.ColumnsJson) <> 1
          OR CHARINDEX(N'[t1].[AgreedDeliverShamsiDate] AS [t1_AgreedDeliverShamsiDate]', q.QueryJson) = 0
          OR NOT EXISTS (
              SELECT 1
              FROM OPENJSON(q.ColumnsJson) c
              WHERE JSON_VALUE(c.value, N'$.Alliance') = N't1_AgreedDeliverShamsiDate'
                AND JSON_VALUE(c.value, N'$.DisplayName') = N'تاریخ تحویل توافقی شمسی'
          )
      )
)
BEGIN
    ROLLBACK TRAN;
    THROW 50001, N'AgreedDeliverShamsiDate profile update failed validation.', 1;
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
      N't1_AgreedDeliverDate',
      N't1_AgreedDeliverShamsiDate'
  )
ORDER BY q.Name, CAST(c.[key] AS int);
