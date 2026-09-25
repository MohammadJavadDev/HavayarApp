/*
================================================================================
Backfill_OpenOrderRequest_RequestedPersonelIds.sql
================================================================================
تکمیل RequestedPersonelIds / RequestedEngineeringPersonelIds از جدول فرزند HTS.
UserCategoryId 2829 = عادی، 2830 = مهندسی.
بدون جدول فرزند جدید. شناسه‌های جاافتاده ادغام می‌شوند.
UTF-8 with BOM؛ sqlcmd -f 65001 -x
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] وجود ندارد.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    IF OBJECT_ID('tempdb..#ReqMap') IS NOT NULL DROP TABLE #ReqMap;
    IF OBJECT_ID('tempdb..#HtsRp') IS NOT NULL DROP TABLE #HtsRp;
    IF OBJECT_ID('tempdb..#Agg') IS NOT NULL DROP TABLE #Agg;

    SELECT CAST(ou.User_ID AS INT) AS OldUserId, nu.Id AS NewUserId
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu ON nu.Username = ou.Username;

    INSERT INTO #UserMap (OldUserId, NewUserId)
    SELECT CAST(ou.User_ID AS INT), nu.Id
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON ou.ActiveDirectoryUsername IS NOT NULL
       AND LOWER(nu.Username) = LOWER(ou.ActiveDirectoryUsername)
    WHERE NOT EXISTS (SELECT 1 FROM #UserMap m WHERE m.OldUserId = CAST(ou.User_ID AS INT));

    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap(OldUserId);

    -- یک NewId به ازای هر OldId (اولین تطبیق)
    ;WITH Mapped AS (
        SELECT
            CAST(ho.OpenOrderRequest_ID AS BIGINT) AS OldRequestId,
            n.Id AS NewRequestId,
            ROW_NUMBER() OVER (PARTITION BY ho.OpenOrderRequest_ID ORDER BY n.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest] ho
        INNER JOIN Sup.OpenOrderRequest n
            ON n.PurchaseRequestItemId = ho.PurchaseRequestItemId
           AND ISNULL(n.OrderRowId, -1) = ISNULL(ho.OrderRowId, -1)
    )
    SELECT OldRequestId, NewRequestId
    INTO #ReqMap
    FROM Mapped
    WHERE rn = 1;

    CREATE UNIQUE CLUSTERED INDEX IX_ReqMap ON #ReqMap(OldRequestId);

    -- کش ردیف‌های فرزند HTS فقط برای درخواست‌های مپ‌شده
    SELECT
        CAST(rp.OpenOrderRequest_FK AS BIGINT) AS OldRequestId,
        CAST(rp.RequestedPersonel_FK AS INT) AS OldUserId,
        CAST(rp.UserCategoryId AS INT) AS UserCategoryId
    INTO #HtsRp
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_Requested_Personel] rp
    WHERE EXISTS (SELECT 1 FROM #ReqMap m WHERE m.OldRequestId = CAST(rp.OpenOrderRequest_FK AS BIGINT));

    CREATE CLUSTERED INDEX IX_HtsRp ON #HtsRp(OldRequestId, UserCategoryId);

    SELECT
        rm.NewRequestId,
        (
            SELECT STRING_AGG(CAST(x.NewUserId AS nvarchar(30)), N',') WITHIN GROUP (ORDER BY x.NewUserId)
            FROM (
                SELECT DISTINCT um.NewUserId
                FROM #HtsRp rp
                INNER JOIN #UserMap um ON um.OldUserId = rp.OldUserId
                WHERE rp.OldRequestId = rm.OldRequestId AND rp.UserCategoryId = 2829
            ) x
        ) AS NormalCsv,
        (
            SELECT STRING_AGG(CAST(x.NewUserId AS nvarchar(30)), N',') WITHIN GROUP (ORDER BY x.NewUserId)
            FROM (
                SELECT DISTINCT um.NewUserId
                FROM #HtsRp rp
                INNER JOIN #UserMap um ON um.OldUserId = rp.OldUserId
                WHERE rp.OldRequestId = rm.OldRequestId AND rp.UserCategoryId = 2830
            ) x
        ) AS EngCsv
    INTO #Agg
    FROM #ReqMap rm;

    CREATE UNIQUE CLUSTERED INDEX IX_Agg ON #Agg(NewRequestId);

    DECLARE @FilledNormal INT = 0, @MergedNormal INT = 0, @FilledEng INT = 0, @MergedEng INT = 0;

    UPDATE o
    SET o.RequestedPersonelIds = N'[' + a.NormalCsv + N']'
    FROM Sup.OpenOrderRequest o
    INNER JOIN #Agg a ON a.NewRequestId = o.Id
    WHERE a.NormalCsv IS NOT NULL
      AND (o.RequestedPersonelIds IS NULL OR LTRIM(RTRIM(o.RequestedPersonelIds)) IN (N'', N'[]', N'null'));
    SET @FilledNormal = @@ROWCOUNT;

    UPDATE o
    SET o.RequestedPersonelIds = N'[' + (
            SELECT STRING_AGG(CAST(x.Id AS nvarchar(30)), N',') WITHIN GROUP (ORDER BY x.Id)
            FROM (
                SELECT DISTINCT TRY_CAST(j.value AS bigint) AS Id
                FROM OPENJSON(CASE WHEN ISJSON(o.RequestedPersonelIds) = 1 THEN o.RequestedPersonelIds ELSE N'[]' END) j
                WHERE TRY_CAST(j.value AS bigint) IS NOT NULL
                UNION
                SELECT DISTINCT TRY_CAST(ss.value AS bigint)
                FROM STRING_SPLIT(a.NormalCsv, N',') ss
                WHERE TRY_CAST(ss.value AS bigint) IS NOT NULL
            ) x
        ) + N']'
    FROM Sup.OpenOrderRequest o
    INNER JOIN #Agg a ON a.NewRequestId = o.Id
    WHERE a.NormalCsv IS NOT NULL
      AND o.RequestedPersonelIds IS NOT NULL
      AND LTRIM(RTRIM(o.RequestedPersonelIds)) NOT IN (N'', N'[]', N'null')
      AND EXISTS (
            SELECT 1 FROM STRING_SPLIT(a.NormalCsv, N',') ss
            WHERE TRY_CAST(ss.value AS bigint) IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1 FROM OPENJSON(CASE WHEN ISJSON(o.RequestedPersonelIds) = 1 THEN o.RequestedPersonelIds ELSE N'[]' END) e
                  WHERE TRY_CAST(e.value AS bigint) = TRY_CAST(ss.value AS bigint)
              )
          );
    SET @MergedNormal = @@ROWCOUNT;

    UPDATE o
    SET o.RequestedEngineeringPersonelIds = N'[' + a.EngCsv + N']'
    FROM Sup.OpenOrderRequest o
    INNER JOIN #Agg a ON a.NewRequestId = o.Id
    WHERE a.EngCsv IS NOT NULL
      AND (o.RequestedEngineeringPersonelIds IS NULL OR LTRIM(RTRIM(o.RequestedEngineeringPersonelIds)) IN (N'', N'[]', N'null'));
    SET @FilledEng = @@ROWCOUNT;

    UPDATE o
    SET o.RequestedEngineeringPersonelIds = N'[' + (
            SELECT STRING_AGG(CAST(x.Id AS nvarchar(30)), N',') WITHIN GROUP (ORDER BY x.Id)
            FROM (
                SELECT DISTINCT TRY_CAST(j.value AS bigint) AS Id
                FROM OPENJSON(CASE WHEN ISJSON(o.RequestedEngineeringPersonelIds) = 1 THEN o.RequestedEngineeringPersonelIds ELSE N'[]' END) j
                WHERE TRY_CAST(j.value AS bigint) IS NOT NULL
                UNION
                SELECT DISTINCT TRY_CAST(ss.value AS bigint)
                FROM STRING_SPLIT(a.EngCsv, N',') ss
                WHERE TRY_CAST(ss.value AS bigint) IS NOT NULL
            ) x
        ) + N']'
    FROM Sup.OpenOrderRequest o
    INNER JOIN #Agg a ON a.NewRequestId = o.Id
    WHERE a.EngCsv IS NOT NULL
      AND o.RequestedEngineeringPersonelIds IS NOT NULL
      AND LTRIM(RTRIM(o.RequestedEngineeringPersonelIds)) NOT IN (N'', N'[]', N'null')
      AND EXISTS (
            SELECT 1 FROM STRING_SPLIT(a.EngCsv, N',') ss
            WHERE TRY_CAST(ss.value AS bigint) IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1 FROM OPENJSON(CASE WHEN ISJSON(o.RequestedEngineeringPersonelIds) = 1 THEN o.RequestedEngineeringPersonelIds ELSE N'[]' END) e
                  WHERE TRY_CAST(e.value AS bigint) = TRY_CAST(ss.value AS bigint)
              )
          );
    SET @MergedEng = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT
        @FilledNormal AS FilledNormal,
        @MergedNormal AS MergedNormal,
        @FilledEng AS FilledEng,
        @MergedEng AS MergedEng,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequest WHERE RequestedPersonelIds IS NOT NULL AND LTRIM(RTRIM(RequestedPersonelIds)) NOT IN (N'', N'[]', N'null')) AS WithNormal,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequest WHERE RequestedEngineeringPersonelIds IS NOT NULL AND LTRIM(RTRIM(RequestedEngineeringPersonelIds)) NOT IN (N'', N'[]', N'null')) AS WithEng,
        (SELECT COUNT(*) FROM #Agg WHERE NormalCsv IS NOT NULL) AS AggWithNormal,
        (SELECT COUNT(*) FROM #Agg WHERE EngCsv IS NOT NULL) AS AggWithEng;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
