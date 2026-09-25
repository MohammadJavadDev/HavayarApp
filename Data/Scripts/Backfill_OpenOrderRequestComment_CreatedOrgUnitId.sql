/*
================================================================================
Backfill_OpenOrderRequestComment_CreatedOrgUnitId.sql
================================================================================
پر کردن CreatedOrgUnitId روی Sup.OpenOrderRequestComment از HTS
CreatedOrgUnit_FK با نگاشت Title واحد سازمانی (مثل SyncAfterSales).
تطبیق کامنت: درخواست (PR+OrderRow) + CommentValue + IsStop؛ یک ردیف به ازای هر کامنت جدید.
پیش‌نیاز: ستون CreatedOrgUnitId (migration).
UTF-8 with BOM؛ sqlcmd -f 65001
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'Sup.OpenOrderRequestComment', N'CreatedOrgUnitId') IS NULL
BEGIN
    RAISERROR(N'ستون CreatedOrgUnitId وجود ندارد. ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] وجود ندارد.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#OrgMap') IS NOT NULL DROP TABLE #OrgMap;
    IF OBJECT_ID('tempdb..#ReqMap') IS NOT NULL DROP TABLE #ReqMap;
    IF OBJECT_ID('tempdb..#Match') IS NOT NULL DROP TABLE #Match;

    SELECT
        CAST(hou.OrgUnit_ID AS INT) AS OldOrgId,
        MIN(o.Id) AS NewOrgId
    INTO #OrgMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hou
    INNER JOIN Hrm.OrgUnit o
        ON LTRIM(RTRIM(o.Title)) = LTRIM(RTRIM(hou.OrgUnit_Title))
    GROUP BY CAST(hou.OrgUnit_ID AS INT);

    CREATE UNIQUE CLUSTERED INDEX IX_OrgMap ON #OrgMap(OldOrgId);

    SELECT
        CAST(ho.OpenOrderRequest_ID AS BIGINT) AS OldRequestId,
        n.Id AS NewRequestId
    INTO #ReqMap
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest] ho
    INNER JOIN Sup.OpenOrderRequest n
        ON n.PurchaseRequestItemId = ho.PurchaseRequestItemId
       AND ISNULL(n.OrderRowId, -1) = ISNULL(ho.OrderRowId, -1);

    CREATE UNIQUE CLUSTERED INDEX IX_ReqMap ON #ReqMap(OldRequestId);

    ;WITH Ranked AS (
        SELECT
            c.Id AS NewCommentId,
            om.NewOrgId,
            ROW_NUMBER() OVER (
                PARTITION BY c.Id
                ORDER BY h.OpenOrderRequestComment_ID
            ) AS rn
        FROM Sup.OpenOrderRequestComment c
        INNER JOIN #ReqMap rm ON rm.NewRequestId = c.OpenOrderRequestId
        INNER JOIN [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_Comment] h
            ON h.OpenOrderRequest_FK = rm.OldRequestId
           AND ISNULL(h.Comment_Value, N'') = ISNULL(c.CommentValue, N'')
           AND CAST(ISNULL(h.IsStop, 0) AS bit) = c.IsStop
        INNER JOIN #OrgMap om ON om.OldOrgId = h.CreatedOrgUnit_FK
        WHERE c.CreatedOrgUnitId IS NULL
    )
    SELECT NewCommentId, NewOrgId
    INTO #Match
    FROM Ranked
    WHERE rn = 1;

    UPDATE c
    SET c.CreatedOrgUnitId = m.NewOrgId
    FROM Sup.OpenOrderRequestComment c
    INNER JOIN #Match m ON m.NewCommentId = c.Id;

    DECLARE @Updated INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT
        @Updated AS CommentsUpdated,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequestComment WHERE CreatedOrgUnitId IS NOT NULL) AS WithOrg,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequestComment WHERE CreatedOrgUnitId IS NULL) AS StillNull,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequestComment WHERE CreatedOrgUnitId = (SELECT TOP 1 Id FROM Hrm.OrgUnit WHERE Title = N'تامین و خرید')) AS SupplyOrgCount;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
