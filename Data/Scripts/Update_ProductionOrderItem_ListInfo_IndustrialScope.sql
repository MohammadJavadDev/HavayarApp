/*
================================================================================
Update_ProductionOrderItem_ListInfo_IndustrialScope.sql
================================================================================
نمایه «لیست اطلاعات» / اطلاعات ما (SavedQuery Id=104، productionorderitem_listinfonew)
برای نقش صنایع (Industries) همهٔ اقلام را نشان می‌داد؛ از جمله ثبت اولیه (1903)
و وضعیت‌های غیر کارتابل صنایع.

موتور Data Profile وقتی CustomQuery پر باشد Filters JSON را اعمال نمی‌کند؛
بنابراین شرط باید داخل خود CustomQuery بیاید.

بعد از این اسکریپت:
  - اطلاعات ما (104): فقط آخرین نسخهٔ حذف‌نشده با CheckStatus در کارتابل صنایع
    (1904 داخلی / 1905 خارجی)
  - کارتابل صنایع (110): همان شرط روی CustomQuery (Filters از قبل همین بود
    ولی اجرا نمی‌شد)

Idempotent: WHERE قبلی با همین الگو حذف و دوباره نوشته می‌شود.
104 همچنان منبع کپی seed است؛ Seed_ProductionOrderItem_DataProfiles.sql
قبل از کپی این WHERE را جدا می‌کند.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @SeedUser NVARCHAR(150) = N'update-poi-listinfo-industrial-scope';

    DECLARE @ListInfoName NVARCHAR(200) = N'productionorderitem_listinfonew';
    DECLARE @CartableName NVARCHAR(200) = N'POI_Cartable_Industrial';

    IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ListInfoName)
        THROW 52001, N'نمایه productionorderitem_listinfonew یافت نشد.', 1;

    DECLARE @BaseQueryJson NVARCHAR(MAX);
    SELECT @BaseQueryJson = QueryJson
    FROM system.SavedQuery
    WHERE Name = @ListInfoName;

    DECLARE @BaseCustomQuery NVARCHAR(MAX);
    SELECT @BaseCustomQuery = CustomQuery
    FROM OPENJSON(@BaseQueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery');

    IF @BaseCustomQuery IS NULL OR LEN(@BaseCustomQuery) = 0
        THROW 52002, N'CustomQuery نمایه لیست اطلاعات خالی است.', 1;

    SET @BaseCustomQuery = REPLACE(@BaseCustomQuery, N'[t6].[Name] AS [t9_Name]', N'[t9].[Name] AS [t9_Name]');

    IF CHARINDEX(N'WHERE [t1].[IsDeleted]', @BaseCustomQuery) > 0
        SET @BaseCustomQuery = LEFT(@BaseCustomQuery, CHARINDEX(N'WHERE [t1].[IsDeleted]', @BaseCustomQuery) - 1);

    SET @BaseCustomQuery = RTRIM(@BaseCustomQuery);

    DECLARE @WhereIndustrial NVARCHAR(MAX) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] IN (1904, 1905)';

    DECLARE @FiltersIndustrial NVARCHAR(MAX) = N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"},{"Id":"id_poi_chk","TableName":"Sale.ProductionOrderItem","ColumnName":"CheckStatus","DataType":"int","Operator":"in","Value":"1904,1905","LogicalOperator":"AND"}]';

    DECLARE @NewCustomQuery NVARCHAR(MAX) = @BaseCustomQuery + @WhereIndustrial;

    UPDATE q
    SET q.QueryJson = JSON_MODIFY(
            JSON_MODIFY(q.QueryJson, '$.CustomQuery', @NewCustomQuery),
            '$.Filters',
            JSON_QUERY(@FiltersIndustrial)
        ),
        q.ModifiedById = 1,
        q.ModifiedByName = @SeedUser,
        q.ModifiedDateMiladiDateTime = @Now
    FROM system.SavedQuery q
    WHERE q.Name = @ListInfoName;

    PRINT N'  UPDATED ' + @ListInfoName + N' (اطلاعات ما): CheckStatus IN (1904, 1905)';

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @CartableName)
    BEGIN
        UPDATE q
        SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @NewCustomQuery),
            q.ModifiedById = 1,
            q.ModifiedByName = @SeedUser,
            q.ModifiedDateMiladiDateTime = @Now
        FROM system.SavedQuery q
        WHERE q.Name = @CartableName;

        PRINT N'  UPDATED ' + @CartableName + N': CustomQuery هم‌تراز کارتابل صنایع';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Update_ProductionOrderItem_ListInfo_IndustrialScope committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-----------------------------------------------------------------------------
-- تایید: وجود WHERE صنعتی
-----------------------------------------------------------------------------
SELECT q.Id, q.Name, q.Title,
       CASE WHEN cq.CustomQuery LIKE N'%CheckStatus] IN (1904, 1905)%' THEN 1 ELSE 0 END AS HasIndustrialWhere
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') cq
WHERE q.Name IN (N'productionorderitem_listinfonew', N'POI_Cartable_Industrial');
