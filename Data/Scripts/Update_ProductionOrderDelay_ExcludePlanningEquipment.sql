/*
================================================================================
Update_ProductionOrderDelay_ExcludePlanningEquipment.sql
================================================================================
تجهیزات برنامه‌ریزی در سیکل تاخیرات نیاید:
  سفارش ساخت Id = 1004 (شماره سفارش 0)
  و اقلام بدون سفارش ساخت

نمایه‌ها:
  vw_All_Need_POD
  vw_All_POD
  vw_Need_POD_ByItem

Idempotent. UTF-8 with BOM.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderDelay_ExcludePlanningEquipment.sql"
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'update-pod-exclude-planning-equipment';
    DECLARE @Marker NVARCHAR(80) = N'AND po.DelayNotCalculated = 0';
    DECLARE @Extra NVARCHAR(200) = N'AND po.DelayNotCalculated = 0
      AND poi.ProductionOrderId IS NOT NULL
      AND po.Id <> 1004';

    DECLARE @Name NVARCHAR(100);
    DECLARE @Id BIGINT;
    DECLARE @Query NVARCHAR(MAX);
    DECLARE @Names TABLE (Name NVARCHAR(100));
    INSERT INTO @Names (Name) VALUES
        (N'vw_All_Need_POD'),
        (N'vw_All_POD'),
        (N'vw_Need_POD_ByItem');

    DECLARE name_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @Names;
    OPEN name_cursor;
    FETCH NEXT FROM name_cursor INTO @Name;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Id = NULL;
        SET @Query = NULL;

        SELECT @Id = s.Id
        FROM system.SavedQuery s
        WHERE s.Name = @Name;

        IF @Id IS NULL
            THROW 51030, N'نمایه داده یافت نشد.', 1;

        SELECT @Query = CAST(j.[value] AS NVARCHAR(MAX))
        FROM system.SavedQuery s
        CROSS APPLY OPENJSON(s.QueryJson) j
        WHERE s.Id = @Id
          AND j.[key] = N'CustomQuery';

        IF @Query IS NULL OR CHARINDEX(@Marker, @Query) = 0
            THROW 51031, N'شرط DelayNotCalculated در کوئری نمایه پیدا نشد.', 1;

        IF CHARINDEX(N'po.Id <> 1004', @Query) = 0
            SET @Query = REPLACE(@Query, @Marker, @Extra);

        UPDATE system.SavedQuery
        SET QueryJson = JSON_MODIFY(QueryJson, N'$.CustomQuery', @Query),
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @Id;

        FETCH NEXT FROM name_cursor INTO @Name;
    END

    CLOSE name_cursor;
    DEALLOCATE name_cursor;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
