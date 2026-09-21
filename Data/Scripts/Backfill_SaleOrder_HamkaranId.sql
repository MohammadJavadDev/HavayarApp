-- ============================================
-- Backfill شناسه‌های راهکاران برای سفارش فروش
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- هدف:
--   Sale.[Order].HamkaranId / OrderNumber
--     <- TMS.Sale_Order.VchHdrId / VchNo  (Id = Order_ID)
--   Sale.OrderDetail.HamkaranId / HamkaranOrderHdrId / DecPrice
--     <- TMS.Sale_OrderDetail.Hamkaran_Vchitm_FK / Hamkaran_VchHdr_FK / DecPrice
--       (Id = OrderDetail_ID)
--
-- قبل از اجرا:
--   1) روی دیتابیس HavayarApp اجرا شود
--   2) Migration AddSaleOrderHamkaranFields اعمال شده باشد
--   3) Linked Server [TMS] در دسترس باشد
-- ============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @BatchSize INT = 20000;
DECLARE @BatchRows INT;
DECLARE @BatchNo INT;
DECLARE @OrderUpdated BIGINT = 0;
DECLARE @OrderDetailUpdated BIGINT = 0;
DECLARE @OrderPending INT;
DECLARE @OrderDetailPending INT;
DECLARE @OrderMatched INT;
DECLARE @OrderDetailMatched INT;
DECLARE @StartedAt DATETIME2 = SYSUTCDATETIME();

PRINT N'===== Backfill_SaleOrder_HamkaranId started at ' + CONVERT(NVARCHAR(30), @StartedAt, 121) + N' =====';

-- آمار اولیه
SELECT @OrderPending = COUNT(*)
FROM Sale.[Order]
WHERE HamkaranId IS NULL OR OrderNumber IS NULL;

SELECT @OrderDetailPending = COUNT(*)
FROM Sale.OrderDetail
WHERE HamkaranId IS NULL OR HamkaranOrderHdrId IS NULL;

SELECT @OrderMatched = COUNT(*)
FROM (
    SELECT DISTINCT o.Id
    FROM Sale.[Order] o
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Order] t ON o.Id = t.Order_ID
    WHERE o.HamkaranId IS NULL OR o.OrderNumber IS NULL
) x;

SELECT @OrderDetailMatched = COUNT(*)
FROM (
    SELECT DISTINCT od.Id
    FROM Sale.OrderDetail od
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_OrderDetail] t ON od.Id = t.OrderDetail_ID
    WHERE od.HamkaranId IS NULL OR od.HamkaranOrderHdrId IS NULL
) x;

PRINT N'Order pending rows (null HamkaranId/OrderNumber): ' + CAST(@OrderPending AS NVARCHAR(20));
PRINT N'Order matched via TMS (will update): ' + CAST(@OrderMatched AS NVARCHAR(20));
PRINT N'OrderDetail pending rows (null HamkaranId/Hdr): ' + CAST(@OrderDetailPending AS NVARCHAR(20));
PRINT N'OrderDetail matched via TMS (will update): ' + CAST(@OrderDetailMatched AS NVARCHAR(20));
PRINT N'Batch size: ' + CAST(@BatchSize AS NVARCHAR(20));

BEGIN TRY
    /* ─────────────────────────────────────────────
       1) Backfill Sale.[Order]
       ───────────────────────────────────────────── */
    PRINT N'--- Updating Sale.[Order] ---';
    SET @BatchNo = 0;

    WHILE 1 = 1
    BEGIN
        BEGIN TRANSACTION;

        ;WITH Candidates AS
        (
            SELECT DISTINCT o.Id
            FROM Sale.[Order] o
            INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Order] t ON o.Id = t.Order_ID
            WHERE o.HamkaranId IS NULL OR o.OrderNumber IS NULL
        ),
        Batch AS
        (
            SELECT TOP (@BatchSize) Id
            FROM Candidates
            ORDER BY Id
        )
        UPDATE o
        SET o.HamkaranId = t.VchHdrId,
            o.OrderNumber = t.VchNo
        FROM Sale.[Order] o
        INNER JOIN Batch b ON b.Id = o.Id
        INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Order] t ON o.Id = t.Order_ID;

        SET @BatchRows = @@ROWCOUNT;
        SET @OrderUpdated = @OrderUpdated + @BatchRows;
        SET @BatchNo = @BatchNo + 1;

        COMMIT TRANSACTION;

        PRINT N'  Order batch ' + CAST(@BatchNo AS NVARCHAR(10))
            + N': updated ' + CAST(@BatchRows AS NVARCHAR(20))
            + N' (total ' + CAST(@OrderUpdated AS NVARCHAR(20)) + N')';

        IF @BatchRows = 0
            BREAK;
    END;

    /* ─────────────────────────────────────────────
       2) Backfill Sale.OrderDetail
       ───────────────────────────────────────────── */
    PRINT N'--- Updating Sale.OrderDetail ---';
    SET @BatchNo = 0;

    WHILE 1 = 1
    BEGIN
        BEGIN TRANSACTION;

        ;WITH Candidates AS
        (
            SELECT DISTINCT od.Id
            FROM Sale.OrderDetail od
            INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_OrderDetail] t ON od.Id = t.OrderDetail_ID
            WHERE od.HamkaranId IS NULL OR od.HamkaranOrderHdrId IS NULL
        ),
        Batch AS
        (
            SELECT TOP (@BatchSize) Id
            FROM Candidates
            ORDER BY Id
        )
        UPDATE od
        SET od.HamkaranId = t.Hamkaran_Vchitm_FK,
            od.HamkaranOrderHdrId = t.Hamkaran_VchHdr_FK,
            od.DecPrice = CAST(t.DecPrice AS BIGINT)
        FROM Sale.OrderDetail od
        INNER JOIN Batch b ON b.Id = od.Id
        INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_OrderDetail] t ON od.Id = t.OrderDetail_ID;

        SET @BatchRows = @@ROWCOUNT;
        SET @OrderDetailUpdated = @OrderDetailUpdated + @BatchRows;
        SET @BatchNo = @BatchNo + 1;

        COMMIT TRANSACTION;

        PRINT N'  OrderDetail batch ' + CAST(@BatchNo AS NVARCHAR(10))
            + N': updated ' + CAST(@BatchRows AS NVARCHAR(20))
            + N' (total ' + CAST(@OrderDetailUpdated AS NVARCHAR(20)) + N')';

        IF @BatchRows = 0
            BREAK;
    END;

    /* ─────────────────────────────────────────────
       3) آمار نهایی
       ───────────────────────────────────────────── */
    DECLARE @OrderFilled BIGINT;
    DECLARE @OrderStillNull BIGINT;
    DECLARE @OrderDetailFilled BIGINT;
    DECLARE @OrderDetailStillNull BIGINT;
    DECLARE @EndedAt DATETIME2 = SYSUTCDATETIME();

    SELECT @OrderFilled = COUNT(*) FROM Sale.[Order] WHERE HamkaranId IS NOT NULL;
    SELECT @OrderStillNull = COUNT(*) FROM Sale.[Order] WHERE HamkaranId IS NULL;
    SELECT @OrderDetailFilled = COUNT(*) FROM Sale.OrderDetail WHERE HamkaranId IS NOT NULL;
    SELECT @OrderDetailStillNull = COUNT(*) FROM Sale.OrderDetail WHERE HamkaranId IS NULL;

    PRINT N'===== Backfill completed at ' + CONVERT(NVARCHAR(30), @EndedAt, 121) + N' =====';
    PRINT N'Duration (sec): ' + CAST(DATEDIFF(SECOND, @StartedAt, @EndedAt) AS NVARCHAR(20));
    PRINT N'Order rows updated this run: ' + CAST(@OrderUpdated AS NVARCHAR(20));
    PRINT N'OrderDetail rows updated this run: ' + CAST(@OrderDetailUpdated AS NVARCHAR(20));
    PRINT N'Order with HamkaranId: ' + CAST(@OrderFilled AS NVARCHAR(20))
        + N' | still NULL: ' + CAST(@OrderStillNull AS NVARCHAR(20));
    PRINT N'OrderDetail with HamkaranId: ' + CAST(@OrderDetailFilled AS NVARCHAR(20))
        + N' | still NULL: ' + CAST(@OrderDetailStillNull AS NVARCHAR(20));

    SELECT
        @OrderUpdated AS OrderRowsUpdated,
        @OrderDetailUpdated AS OrderDetailRowsUpdated,
        @OrderFilled AS OrderWithHamkaranId,
        @OrderStillNull AS OrderHamkaranIdStillNull,
        @OrderDetailFilled AS OrderDetailWithHamkaranId,
        @OrderDetailStillNull AS OrderDetailHamkaranIdStillNull,
        DATEDIFF(SECOND, @StartedAt, @EndedAt) AS DurationSeconds;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrNum INT = ERROR_NUMBER();
    DECLARE @ErrLine INT = ERROR_LINE();

    PRINT N'ERROR ' + CAST(@ErrNum AS NVARCHAR(20))
        + N' at line ' + CAST(@ErrLine AS NVARCHAR(20))
        + N': ' + @ErrMsg;

    THROW;
END CATCH;
GO
