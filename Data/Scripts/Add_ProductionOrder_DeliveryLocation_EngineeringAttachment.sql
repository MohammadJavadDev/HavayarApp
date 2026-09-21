/*
  Add_ProductionOrder_DeliveryLocation_EngineeringAttachment.sql
  تطبیق سربرگ سفارش ساخت با فرم زنده HTS (صفحه 463 — Pln_ProductionOrder):
    - DeliveryLocation        : «محل تحویل» (HTS: DeliveryLocation nvarchar(128))
    - EngineeringAttachmentId : پنجمین پیوست سربرگ «مهندسی / متره» (HTS: AttachmentFileName/Size/Path)
  Idempotent — safe to re-run. بدون EF migration (الگوی AddProductionOrderOperationalFields.sql).
*/
SET NOCOUNT ON;

IF COL_LENGTH('Sale.ProductionOrder', 'DeliveryLocation') IS NULL
BEGIN
    ALTER TABLE [Sale].[ProductionOrder] ADD [DeliveryLocation] NVARCHAR(128) NULL;
    PRINT 'Column DeliveryLocation added to Sale.ProductionOrder';
END
ELSE
    PRINT 'Column DeliveryLocation already exists on Sale.ProductionOrder';
GO

IF COL_LENGTH('Sale.ProductionOrder', 'EngineeringAttachmentId') IS NULL
BEGIN
    ALTER TABLE [Sale].[ProductionOrder] ADD [EngineeringAttachmentId] BIGINT NULL;
    PRINT 'Column EngineeringAttachmentId added to Sale.ProductionOrder';
END
ELSE
    PRINT 'Column EngineeringAttachmentId already exists on Sale.ProductionOrder';
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProductionOrder_FileEntity_EngineeringAttachmentId')
   AND OBJECT_ID('dbo.FileEntity') IS NOT NULL
BEGIN
    ALTER TABLE [Sale].[ProductionOrder] WITH CHECK
        ADD CONSTRAINT [FK_ProductionOrder_FileEntity_EngineeringAttachmentId]
        FOREIGN KEY ([EngineeringAttachmentId]) REFERENCES [dbo].[FileEntity]([Id]);
    PRINT 'FK_ProductionOrder_FileEntity_EngineeringAttachmentId created';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sale_ProductionOrder_EngineeringAttachmentId' AND object_id = OBJECT_ID('Sale.ProductionOrder'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Sale_ProductionOrder_EngineeringAttachmentId]
        ON [Sale].[ProductionOrder]([EngineeringAttachmentId]) WHERE [EngineeringAttachmentId] IS NOT NULL;
    PRINT 'IX_Sale_ProductionOrder_EngineeringAttachmentId created';
END
GO
