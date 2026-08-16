/*
  ستون‌های جدید برای معادل‌سازی تریگرهای HTS (plnTriggers.xlsx)
  Idempotent — قابل اجرای مجدد
*/
SET NOCOUNT ON;

IF COL_LENGTH('Sale.ProductionOrderItem', 'LastComment') IS NULL
BEGIN
    ALTER TABLE Sale.ProductionOrderItem ADD LastComment NVARCHAR(2048) NULL;
    PRINT N'Added Sale.ProductionOrderItem.LastComment';
END

IF COL_LENGTH('Sale.ProductionOrderItem', 'SecondInstallmentShamsiDate') IS NULL
BEGIN
    ALTER TABLE Sale.ProductionOrderItem ADD SecondInstallmentShamsiDate NVARCHAR(30) NULL;
    PRINT N'Added SecondInstallmentShamsiDate';
END

IF COL_LENGTH('Sale.ProductionOrderItem', 'ThirdInstallmentShamsiDate') IS NULL
BEGIN
    ALTER TABLE Sale.ProductionOrderItem ADD ThirdInstallmentShamsiDate NVARCHAR(30) NULL;
    PRINT N'Added ThirdInstallmentShamsiDate';
END

IF COL_LENGTH('Sale.ProductionOrderItem', 'FourthInstallmentShamsiDate') IS NULL
BEGIN
    ALTER TABLE Sale.ProductionOrderItem ADD FourthInstallmentShamsiDate NVARCHAR(30) NULL;
    PRINT N'Added FourthInstallmentShamsiDate';
END

IF COL_LENGTH('Sale.ProductionOrderItem', 'FifthInstallmentShamsiDate') IS NULL
BEGIN
    ALTER TABLE Sale.ProductionOrderItem ADD FifthInstallmentShamsiDate NVARCHAR(30) NULL;
    PRINT N'Added FifthInstallmentShamsiDate';
END

PRINT N'Done: PlnTriggers equivalent columns';
