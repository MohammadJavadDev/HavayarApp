/*
================================================================================
Update_ProductionOrderDelay_MaxDeliverDate.sql
================================================================================
تاریخ مجاز سفارش ساخت = بیشترین تاریخ بین «تحویل توافقی» و «تحویل استاندارد»
همه اقلام معتبر سفارش — نه تاریخ قلمِ دارای بیشترین تاخیر.

نمونه: سفارش 9174 بیشترین تاریخ اقلام 1405/08/04 است؛ منطق قبلی 1405/06/01
(تاریخ قلم بدون زمان استاندارد و بدون آماده‌سازی) را ملاک می‌گرفت.

نمایه‌ها:
  system.SavedQuery.Name = vw_All_Need_POD  (سفارش‌هایی که نیاز به ثبت علت تاخیر دارند)
  system.SavedQuery.Name = vw_All_POD        (سفارش‌های دارای تاخیر)

Idempotent: اجرای مکرر فقط CustomQuery/عنوان ستون را بازنویسی می‌کند.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'update-pod-max-deliver-date';

    DECLARE @ItemCte NVARCHAR(MAX) = N';WITH ItemCalc AS (
    SELECT
        poi.Id AS ProductionOrderItemId,
        poi.ProductionOrderId,
        poi.PreparationMiladiDate,
        poi.AgreedDeliverDate,
        CASE
            WHEN poi.StandardDeliveryMiladiDate IS NULL THEN NULL
            WHEN YEAR(poi.StandardDeliveryMiladiDate) >= 9000 THEN NULL
            ELSE poi.StandardDeliveryMiladiDate
        END AS StandardDeliveryMiladiDate,
        poi.SendToIndustrialMiladiDate,
        poi.Serial,
        poi.PartId,
        CASE WHEN poi.PreparationMiladiDate IS NULL THEN 1 ELSE 0 END AS IsPreparationMissing,
        CASE
            WHEN poi.AgreedDeliverDate IS NULL THEN
                CASE WHEN poi.StandardDeliveryMiladiDate IS NOT NULL AND YEAR(poi.StandardDeliveryMiladiDate) < 9000
                     THEN poi.StandardDeliveryMiladiDate END
            WHEN poi.StandardDeliveryMiladiDate IS NULL OR YEAR(poi.StandardDeliveryMiladiDate) >= 9000 THEN poi.AgreedDeliverDate
            WHEN poi.AgreedDeliverDate >= poi.StandardDeliveryMiladiDate THEN poi.AgreedDeliverDate
            ELSE poi.StandardDeliveryMiladiDate
        END AS ItemDeliverDate
    FROM Sale.ProductionOrderItem poi
    INNER JOIN Sale.ProductionOrder po ON po.Id = poi.ProductionOrderId
    WHERE poi.IsDeleted = 0
      AND poi.IsLatestVersion = 1
      AND poi.Status <> 581
      AND poi.SendToIndustrialMiladiDate IS NOT NULL
      AND po.IsActive = 1
      AND po.ProjectManagerId IS NULL
      AND po.DelayNotCalculated = 0
),
OrderDeliver AS (
    SELECT
        ProductionOrderId,
        MAX(ItemDeliverDate) AS OrderDeliverDate
    FROM ItemCalc
    GROUP BY ProductionOrderId
),
ItemDelay AS (
    SELECT
        ic.*,
        od.OrderDeliverDate,
        CASE
            WHEN od.OrderDeliverDate IS NULL THEN 0
            WHEN ic.StandardDeliveryMiladiDate IS NULL
                 AND ic.AgreedDeliverDate IS NOT NULL
                 AND CAST(ic.AgreedDeliverDate AS date) < CAST(ic.SendToIndustrialMiladiDate AS date)
                 AND ic.PreparationMiladiDate IS NOT NULL
                 AND CAST(ic.PreparationMiladiDate AS date) = CAST(ic.SendToIndustrialMiladiDate AS date)
                THEN 0
            WHEN ic.PreparationMiladiDate IS NOT NULL
                THEN CASE
                    WHEN DATEDIFF(DAY, od.OrderDeliverDate, ic.PreparationMiladiDate) > 0
                        THEN DATEDIFF(DAY, od.OrderDeliverDate, ic.PreparationMiladiDate)
                    ELSE 0
                END
            WHEN CAST(GETDATE() AS date) > CAST(od.OrderDeliverDate AS date)
                THEN DATEDIFF(DAY, od.OrderDeliverDate, CAST(GETDATE() AS date))
            ELSE 0
        END AS ItemDelayDays
    FROM ItemCalc ic
    INNER JOIN OrderDeliver od ON od.ProductionOrderId = ic.ProductionOrderId
),
RankedItemDelay AS (
    SELECT
        idl.*,
        ROW_NUMBER() OVER (
            PARTITION BY idl.ProductionOrderId
            ORDER BY idl.ItemDelayDays DESC, idl.ProductionOrderItemId
        ) AS rn
    FROM ItemDelay idl
),
MaxDelayItem AS (
    SELECT
        ProductionOrderId,
        PreparationMiladiDate AS MaxDelayItemPreparationDate,
        Serial AS DelayCauseSerial,
        PartId
    FROM RankedItemDelay
    WHERE rn = 1
),
OrderAgg AS (
    SELECT
        idl.ProductionOrderId,
        MAX(idl.OrderDeliverDate) AS MaxDeliverDateOfOrder,
        MAX(idl.ItemDelayDays) AS MaxDelayDays,
        SUM(idl.IsPreparationMissing) AS MissingPreparationCount,
        COUNT(*) AS TotalItemsCount
    FROM ItemDelay idl
    GROUP BY idl.ProductionOrderId
)';

    DECLARE @NeedPodQuery NVARCHAR(MAX) = @ItemCte + N', DelayAgg AS (
    SELECT
        pod.ProductionOrderId,
        SUM(ISNULL(pod.DelayDays, 0)) AS RegisteredDelayDays
    FROM Pln.ProductionOrderDelay pod
    WHERE pod.IsActive = 1
    GROUP BY pod.ProductionOrderId
)

--!--mainsection
SELECT
    r.ProductionOrderId,
    ISNULL(part.Name, N''-'') AS DelayCausePartName,
    ISNULL(part.Code, N''-'') AS DelayCausePartCode,
    ISNULL(mdi.DelayCauseSerial, N''-'') AS DelayCauseSerial,
    po.ProductionOrderNumber,
    r.MaxDeliverDateOfOrder,
    dbo.fn_MiladiToShamsi(r.MaxDeliverDateOfOrder) AS MaxDeliverDateOfOrderShamsi,
    mdi.MaxDelayItemPreparationDate AS PreparationDate,
    dbo.fn_MiladiToShamsi(mdi.MaxDelayItemPreparationDate) AS PreparationDateShamsi,
    r.MaxDelayDays AS DelayDay,
    CASE WHEN r.MissingPreparationCount = 0 THEN N''کامل شده'' ELSE N''کامل نشده'' END AS PreparationStatus,
    r.MissingPreparationCount,
    r.TotalItemsCount,
    ISNULL(da.RegisteredDelayDays, 0) AS RegisteredDelayDays,
    r.MaxDelayDays - ISNULL(da.RegisteredDelayDays, 0) AS NeedsDelayRegistrationDays
FROM OrderAgg r
INNER JOIN Sale.ProductionOrder po ON po.Id = r.ProductionOrderId
LEFT JOIN DelayAgg da ON da.ProductionOrderId = r.ProductionOrderId
LEFT JOIN MaxDelayItem mdi ON mdi.ProductionOrderId = r.ProductionOrderId
LEFT JOIN Inv.Part part ON part.Id = mdi.PartId
WHERE r.MaxDelayDays > 0
  AND ISNULL(da.RegisteredDelayDays, 0) < r.MaxDelayDays';

    DECLARE @AllPodQuery NVARCHAR(MAX) = @ItemCte + N'
--!--mainsection
SELECT
    r.ProductionOrderId,
    ISNULL(part.Name, N''-'') AS DelayCausePartName,
    ISNULL(part.Code, N''-'') AS DelayCausePartCode,
    ISNULL(mdi.DelayCauseSerial, N''-'') AS DelayCauseSerial,
    po.ProductionOrderNumber,
    r.MaxDeliverDateOfOrder,
    dbo.fn_MiladiToShamsi(r.MaxDeliverDateOfOrder) AS MaxDeliverDateOfOrderShamsi,
    mdi.MaxDelayItemPreparationDate AS PreparationDate,
    dbo.fn_MiladiToShamsi(mdi.MaxDelayItemPreparationDate) AS PreparationDateShamsi,
    r.MaxDelayDays AS DelayDay,
    CASE WHEN r.MissingPreparationCount = 0 THEN N''کامل شده'' ELSE N''کامل نشده'' END AS PreparationStatus,
    r.MissingPreparationCount,
    r.TotalItemsCount
FROM OrderAgg r
INNER JOIN Sale.ProductionOrder po ON po.Id = r.ProductionOrderId
LEFT JOIN MaxDelayItem mdi ON mdi.ProductionOrderId = r.ProductionOrderId
LEFT JOIN Inv.Part part ON part.Id = mdi.PartId
WHERE r.MaxDelayDays > 0';

    DECLARE @NeedPodId BIGINT;
    DECLARE @AllPodId BIGINT;
    DECLARE @NeedPodJson NVARCHAR(MAX);
    DECLARE @AllPodJson NVARCHAR(MAX);

    SELECT @NeedPodId = Id, @NeedPodJson = QueryJson
    FROM system.SavedQuery
    WHERE Name = N'vw_All_Need_POD';

    SELECT @AllPodId = Id, @AllPodJson = QueryJson
    FROM system.SavedQuery
    WHERE Name = N'vw_All_POD';

    IF @NeedPodId IS NULL OR @AllPodId IS NULL
        THROW 51010, N'نمایه vw_All_Need_POD یا vw_All_POD یافت نشد.', 1;

    SET @NeedPodJson = JSON_MODIFY(@NeedPodJson, '$.CustomQuery', @NeedPodQuery);
    SET @AllPodJson = JSON_MODIFY(@AllPodJson, '$.CustomQuery', @AllPodQuery);

    DECLARE @CauseColName NVARCHAR(MAX) = N'{"TableName":null,"ColumnName":"DelayCausePartName","DisplayName":"نام محصول","Alliance":null,"Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""}';
    DECLARE @CauseColCode NVARCHAR(MAX) = N'{"TableName":null,"ColumnName":"DelayCausePartCode","DisplayName":"کد محصول","Alliance":null,"Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""}';
    DECLARE @CauseColSerial NVARCHAR(MAX) = N'{"TableName":null,"ColumnName":"DelayCauseSerial","DisplayName":"سریال","Alliance":null,"Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""}';
    DECLARE @CauseCols NVARCHAR(MAX) = @CauseColName + N',' + @CauseColCode + N',' + @CauseColSerial;

    DECLARE @ProfileId BIGINT;
    DECLARE @ProfileColumns NVARCHAR(MAX);
    DECLARE @PkCol NVARCHAR(MAX);
    DECLARE @RestCols NVARCHAR(MAX);
    DECLARE @NewColumns NVARCHAR(MAX);
    DECLARE @Profiles TABLE (Id BIGINT);
    INSERT INTO @Profiles (Id) VALUES (@NeedPodId), (@AllPodId);

    DECLARE profile_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM @Profiles;
    OPEN profile_cursor;
    FETCH NEXT FROM profile_cursor INTO @ProfileId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @ProfileColumns = REPLACE(ColumnsJson, N'تاریخ مجاز قلم دارای بیشترین تأخیر', N'بیشترین تاریخ مجاز تحویل اقلام')
        FROM system.SavedQuery
        WHERE Id = @ProfileId;

        SELECT @PkCol = [value]
        FROM OPENJSON(@ProfileColumns)
        WHERE JSON_VALUE([value], '$.ColumnName') = N'ProductionOrderId';

        SELECT @RestCols = STRING_AGG(CAST([value] AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST([key] AS INT))
        FROM OPENJSON(@ProfileColumns)
        WHERE JSON_VALUE([value], '$.ColumnName') NOT IN (
            N'ProductionOrderId',
            N'DelayCausePartName',
            N'DelayCausePartCode',
            N'DelayCauseSerial'
        );

        SET @NewColumns = N'[' + ISNULL(@PkCol + N',', N'') + @CauseCols + CASE WHEN @RestCols IS NULL THEN N'' ELSE N',' + @RestCols END + N']';

        UPDATE system.SavedQuery
        SET QueryJson = CASE WHEN Id = @NeedPodId THEN @NeedPodJson ELSE @AllPodJson END,
            ColumnsJson = @NewColumns,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @ProfileId;

        FETCH NEXT FROM profile_cursor INTO @ProfileId;
    END
    CLOSE profile_cursor;
    DEALLOCATE profile_cursor;

    PRINT N'UPDATED vw_All_Need_POD Id=' + CAST(@NeedPodId AS NVARCHAR(20));
    PRINT N'UPDATED vw_All_POD Id=' + CAST(@AllPodId AS NVARCHAR(20));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
