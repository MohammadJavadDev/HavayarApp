-- Adds PlanningConsideration (ملاحظات صنایع) to Sale.ProductionOrderItem — HTS equivalent
IF COL_LENGTH('Sale.ProductionOrderItem', 'PlanningConsideration') IS NULL
BEGIN
    ALTER TABLE [Sale].[ProductionOrderItem] ADD [PlanningConsideration] NVARCHAR(1024) NULL;
    PRINT 'Column PlanningConsideration added to Sale.ProductionOrderItem';
END
ELSE
BEGIN
    PRINT 'Column PlanningConsideration already exists on Sale.ProductionOrderItem';
END
GO
