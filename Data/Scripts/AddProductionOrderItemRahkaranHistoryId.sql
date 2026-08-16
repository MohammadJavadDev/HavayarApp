-- Adds RahkaranHistoryId (شناسه Sale_ProductionOrderItemHistory.Id) to Sale.ProductionOrderItem
-- Equivalent to HTS Pln_ProductionOrderItem.RahkaranId which stored History.Id per version
IF COL_LENGTH('Sale.ProductionOrderItem', 'RahkaranHistoryId') IS NULL
BEGIN
    ALTER TABLE [Sale].[ProductionOrderItem] ADD [RahkaranHistoryId] BIGINT NULL;
    PRINT 'Column RahkaranHistoryId added to Sale.ProductionOrderItem';
END
ELSE
BEGIN
    PRINT 'Column RahkaranHistoryId already exists on Sale.ProductionOrderItem';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ProductionOrderItem_RahkaranHistoryId'
      AND object_id = OBJECT_ID(N'Sale.ProductionOrderItem')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProductionOrderItem_RahkaranHistoryId]
        ON [Sale].[ProductionOrderItem] ([RahkaranHistoryId])
        WHERE [RahkaranHistoryId] IS NOT NULL;
    PRINT 'Index IX_ProductionOrderItem_RahkaranHistoryId created';
END
GO
