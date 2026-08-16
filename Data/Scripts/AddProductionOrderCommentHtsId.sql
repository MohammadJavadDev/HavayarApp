-- Adds HtsId to Sale.ProductionOrderComment / Sale.ProductionOrderItemComment
-- for idempotent comment migration from TotalSystem (HTS).

IF COL_LENGTH('Sale.ProductionOrderComment', 'HtsId') IS NULL
BEGIN
    ALTER TABLE [Sale].[ProductionOrderComment]
    ADD [HtsId] BIGINT NOT NULL CONSTRAINT DF_ProductionOrderComment_HtsId DEFAULT (0);

    PRINT 'Column HtsId added to Sale.ProductionOrderComment';
END
ELSE
BEGIN
    PRINT 'Column HtsId already exists on Sale.ProductionOrderComment';
END
GO

IF COL_LENGTH('Sale.ProductionOrderItemComment', 'HtsId') IS NULL
BEGIN
    ALTER TABLE [Sale].[ProductionOrderItemComment]
    ADD [HtsId] BIGINT NOT NULL CONSTRAINT DF_ProductionOrderItemComment_HtsId DEFAULT (0);

    PRINT 'Column HtsId added to Sale.ProductionOrderItemComment';
END
ELSE
BEGIN
    PRINT 'Column HtsId already exists on Sale.ProductionOrderItemComment';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ProductionOrderComment_HtsId'
      AND object_id = OBJECT_ID('Sale.ProductionOrderComment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProductionOrderComment_HtsId
        ON [Sale].[ProductionOrderComment]([HtsId])
        WHERE [HtsId] <> 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ProductionOrderItemComment_HtsId'
      AND object_id = OBJECT_ID('Sale.ProductionOrderItemComment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProductionOrderItemComment_HtsId
        ON [Sale].[ProductionOrderItemComment]([HtsId])
        WHERE [HtsId] <> 0;
END
GO
