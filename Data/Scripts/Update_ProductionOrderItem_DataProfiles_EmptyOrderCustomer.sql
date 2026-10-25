/*
  وقتی شماره سفارش ساخت خالی (NULL) یا صفر باشد،
  در نمایه‌های اقلام سفارش ساخت این دو مقدار نمایش داده می‌شود:
    مشتری          = برنامه ریزی
    دپارتمان فروش  = دفتر مرکزی

  فقط عبارت SELECT همین دو ستون عوض می‌شود.

  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderItem_DataProfiles_EmptyOrderCustomer.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @OldCustomer NVARCHAR(200) = N'[t5].[FullName] AS [t5_FullName]';
DECLARE @NewCustomer NVARCHAR(500) = N'CASE WHEN [t2].[ProductionOrderNumber] IS NULL OR [t2].[ProductionOrderNumber] = 0 THEN N''برنامه ریزی'' ELSE [t5].[FullName] END AS [t5_FullName]';

DECLARE @OldDept NVARCHAR(200) = N'[t8].[Title] AS [t8_Title]';
DECLARE @NewDept NVARCHAR(500) = N'CASE WHEN [t2].[ProductionOrderNumber] IS NULL OR [t2].[ProductionOrderNumber] = 0 THEN N''دفتر مرکزی'' ELSE [t8].[Title] END AS [t8_Title]';

UPDATE q
SET q.QueryJson = REPLACE(REPLACE(q.QueryJson, @OldCustomer, @NewCustomer), @OldDept, @NewDept)
FROM system.SavedQuery q
WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
  AND CHARINDEX(@OldCustomer, q.QueryJson) > 0
  AND CHARINDEX(@OldDept, q.QueryJson) > 0
  AND CHARINDEX(N'N''برنامه ریزی''', q.QueryJson) = 0;

IF EXISTS (
    SELECT 1
    FROM system.SavedQuery q
    WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
      AND ISJSON(q.QueryJson) <> 1
)
BEGIN
    RAISERROR(N'QueryJson بعد از جایگزینی JSON معتبر نیست.', 16, 1);
    ROLLBACK TRAN;
    RETURN;
END

COMMIT TRAN;

SELECT q.Id, q.Name,
       CASE WHEN cq.CustomQuery LIKE N'%N''برنامه ریزی''%' THEN 1 ELSE 0 END AS HasCustomerFallback,
       CASE WHEN cq.CustomQuery LIKE N'%N''دفتر مرکزی''%' THEN 1 ELSE 0 END AS HasDeptFallback,
       UNICODE(SUBSTRING(cq.CustomQuery, CHARINDEX(N'ریزی', cq.CustomQuery) + 1, 1)) AS YehCode
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery nvarchar(max) '$.CustomQuery') cq
WHERE q.EntityFullName = N'entities.app.sale.productionorderitem'
ORDER BY q.Id;
