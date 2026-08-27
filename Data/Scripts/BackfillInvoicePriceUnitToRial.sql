SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @InvoiceType INT = 1676;
DECLARE @RialPriceUnitId BIGINT = 1;
DECLARE @AffectedRows BIGINT;

UPDATE Sup.InquiryPartPrice
SET PriceUnitId = @RialPriceUnitId,
    ModifiedDateMiladiDateTime = SYSDATETIME(),
    ModifiedByName = N'سیستم - اصلاح واحد فاکتور خرید'
WHERE Type = @InvoiceType
  AND PriceUnitId IS NULL;

SET @AffectedRows = @@ROWCOUNT;

IF EXISTS (
    SELECT 1
    FROM Sup.InquiryPartPrice
    WHERE Type = @InvoiceType
      AND PriceUnitId IS NULL
)
BEGIN
    THROW 51000, N'Backfill واحد ریال برای همه فاکتورهای خرید کامل نشد.', 1;
END;

COMMIT TRANSACTION;

SELECT @AffectedRows AS AffectedRows;
