/*
SyncAfterSalesPhase2FromTotalSystem.sql
PriceConfig, PartPrice, WebShopOrder, WebShopPartForSale, WebShopPartGroup
Requires [TMS] and Inv.Part.HtsId.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase2';

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;

BEGIN TRY
BEGIN TRANSACTION;

PRINT N'=== Sale.PriceConfig ===';
MERGE Sale.PriceConfig AS t
USING (
    SELECT CAST(s.PriceConfig_ID AS BIGINT) AS HtsId, s.PriceConfig_Title AS Title, s.Profit, s.Overhead, s.Shipping, s.Gomrok
    FROM [TMS].[TotalSystem].[dbo].[Sale_PriceConfig] s
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.Title=s.Title, t.Profit=s.Profit, t.Overhead=s.Overhead, t.Shipping=s.Shipping, t.Gomrok=s.Gomrok,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, Title, Profit, Overhead, Shipping, Gomrok,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.Title, s.Profit, s.Overhead, s.Shipping, s.Gomrok, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

PRINT N'=== Sale.PartPrice ===';
MERGE Sale.PartPrice AS t
USING (
    SELECT CAST(s.PartPrice_ID AS BIGINT) AS HtsId, p.Id AS PartId, CAST(s.Price AS BIGINT) AS Price,
           s.SaleFactor, s.NonRoutineWithTransferringFeeSaleFactor, s.NonRoutineWithoutTransferringFeeSaleFactor
    FROM [TMS].[TotalSystem].[dbo].[Sale_PartPrice] s
    LEFT JOIN Inv.Part p ON p.HtsId = s.Part_FK
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.PartId=s.PartId, t.Price=s.Price, t.SaleFactor=s.SaleFactor,
    t.NonRoutineWithTransferringFeeSaleFactor=s.NonRoutineWithTransferringFeeSaleFactor,
    t.NonRoutineWithoutTransferringFeeSaleFactor=s.NonRoutineWithoutTransferringFeeSaleFactor,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, PartId, Price, SaleFactor, NonRoutineWithTransferringFeeSaleFactor, NonRoutineWithoutTransferringFeeSaleFactor,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.PartId, s.Price, s.SaleFactor, s.NonRoutineWithTransferringFeeSaleFactor, s.NonRoutineWithoutTransferringFeeSaleFactor,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

PRINT N'=== Sale.WebShopPartGroup ===';
MERGE Sale.WebShopPartGroup AS t
USING (
    SELECT CAST(s.PartGroup_ID AS BIGINT) AS HtsId, s.PrefixCode, s.PartGroupTitle AS Title
    FROM [TMS].[TotalSystem].[dbo].[Sale_WebShop_PartGroup] s
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.PrefixCode=s.PrefixCode, t.Title=s.Title,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, PrefixCode, Title,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.PrefixCode, s.Title, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

PRINT N'=== Sale.WebShopPartForSale ===';
MERGE Sale.WebShopPartForSale AS t
USING (
    SELECT CAST(s.Sale_WebdShop_PartForSale_ID AS BIGINT) AS HtsId, p.Id AS PartId, s.PartType_FK AS PartType, CAST(s.Price AS BIGINT) AS Price
    FROM [TMS].[TotalSystem].[dbo].[Sale_WebShop_PartForSale] s
    LEFT JOIN Inv.Part p ON p.HtsId = s.Part_FK
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.PartId=s.PartId, t.PartType=s.PartType, t.Price=s.Price,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, PartId, PartType, Price,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.PartId, s.PartType, s.Price, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

PRINT N'=== Sale.WebShopOrder ===';
MERGE Sale.WebShopOrder AS t
USING (
    SELECT CAST(s.WebShopOrder_ID AS BIGINT) AS HtsId, o.Id AS SaleOrderId, s.CustomerCode, s.OrderCode, s.FirstName, s.LastName,
           CAST(s.BuyPrice AS BIGINT) AS BuyPrice, CAST(s.VatPrice AS BIGINT) AS VatPrice, CAST(s.TotalDiscount AS BIGINT) AS TotalDiscount,
           CAST(s.TaxPrice AS BIGINT) AS TaxPrice, CAST(s.TotalPrice AS BIGINT) AS TotalPrice, CAST(s.Hamkaran_Factor_FK AS BIGINT) AS HamkaranFactorId, s.FactoredPrice, s.FactoredDate
    FROM [TMS].[TotalSystem].[dbo].[WebShop_Order] s
    LEFT JOIN Sale.[Order] o ON o.HtsId = s.Order_FK
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.SaleOrderId=s.SaleOrderId, t.CustomerCode=s.CustomerCode, t.OrderCode=s.OrderCode,
    t.FirstName=s.FirstName, t.LastName=s.LastName, t.BuyPrice=s.BuyPrice, t.VatPrice=s.VatPrice, t.TotalDiscount=s.TotalDiscount,
    t.TaxPrice=s.TaxPrice, t.TotalPrice=s.TotalPrice, t.HamkaranFactorId=s.HamkaranFactorId, t.FactoredPrice=s.FactoredPrice,
    t.FactoredShamsiDate=s.FactoredDate,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, SaleOrderId, CustomerCode, OrderCode, FirstName, LastName, BuyPrice, VatPrice, TotalDiscount, TaxPrice, TotalPrice, HamkaranFactorId, FactoredPrice, FactoredShamsiDate,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.SaleOrderId, s.CustomerCode, s.OrderCode, s.FirstName, s.LastName, s.BuyPrice, s.VatPrice, s.TotalDiscount, s.TaxPrice, s.TotalPrice, s.HamkaranFactorId, s.FactoredPrice, s.FactoredDate,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

COMMIT;
PRINT N'Phase 2 sync committed';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
