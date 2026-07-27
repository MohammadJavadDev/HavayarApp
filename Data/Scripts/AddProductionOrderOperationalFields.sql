-- Adds operational fields to Sale.ProductionOrderItem and Sale.ProductionOrder (HTS alignment)



-- ========== Sale.ProductionOrderItem ==========

IF COL_LENGTH('Sale.ProductionOrderItem', 'SendToIndustrialMiladiDate') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [SendToIndustrialMiladiDate] DATETIME2 NULL;

    PRINT 'Column SendToIndustrialMiladiDate added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'SendToIndustrialShamsiDate') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [SendToIndustrialShamsiDate] NVARCHAR(30) NULL;

    PRINT 'Column SendToIndustrialShamsiDate added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'FirstIndustrialChangeById') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [FirstIndustrialChangeById] BIGINT NULL;

    PRINT 'Column FirstIndustrialChangeById added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'FirstIndustrialChangeMiladiDate') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [FirstIndustrialChangeMiladiDate] DATETIME2 NULL;

    PRINT 'Column FirstIndustrialChangeMiladiDate added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'FirstIndustrialChangeShamsiDate') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [FirstIndustrialChangeShamsiDate] NVARCHAR(30) NULL;

    PRINT 'Column FirstIndustrialChangeShamsiDate added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'DocumentPreparationMiladiDate') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [DocumentPreparationMiladiDate] DATETIME2 NULL;

    PRINT 'Column DocumentPreparationMiladiDate added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'DocumentPreparationShamsiDate') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [DocumentPreparationShamsiDate] NVARCHAR(30) NULL;

    PRINT 'Column DocumentPreparationShamsiDate added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'StandardDeliveryMiladiDate') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [StandardDeliveryMiladiDate] DATETIME2 NULL;

    PRINT 'Column StandardDeliveryMiladiDate added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'StandardDeliveryShamsiDate') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [StandardDeliveryShamsiDate] NVARCHAR(30) NULL;

    PRINT 'Column StandardDeliveryShamsiDate added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'EngineeringConsideration') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [EngineeringConsideration] NVARCHAR(1024) NULL;

    PRINT 'Column EngineeringConsideration added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'IsRoutine') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [IsRoutine] BIT NOT NULL CONSTRAINT DF_ProductionOrderItem_IsRoutine DEFAULT (0);

    PRINT 'Column IsRoutine added to Sale.ProductionOrderItem';

END

GO



IF COL_LENGTH('Sale.ProductionOrderItem', 'PlanningConsideration') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrderItem] ADD [PlanningConsideration] NVARCHAR(1024) NULL;

    PRINT 'Column PlanningConsideration added to Sale.ProductionOrderItem';

END

GO



-- ========== Sale.ProductionOrder (header light) ==========

IF COL_LENGTH('Sale.ProductionOrder', 'IsDisableForTimelyDeliveryReport') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrder] ADD [IsDisableForTimelyDeliveryReport] BIT NOT NULL CONSTRAINT DF_ProductionOrder_IsDisableForTimelyDeliveryReport DEFAULT (0);

    PRINT 'Column IsDisableForTimelyDeliveryReport added to Sale.ProductionOrder';

END

GO



IF COL_LENGTH('Sale.ProductionOrder', 'DisableForTimelyDeliveryReportComment') IS NULL

BEGIN

    ALTER TABLE [Sale].[ProductionOrder] ADD [DisableForTimelyDeliveryReportComment] NVARCHAR(1024) NULL;

    PRINT 'Column DisableForTimelyDeliveryReportComment added to Sale.ProductionOrder';

END

GO

