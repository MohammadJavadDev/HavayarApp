-- ============================================
-- بازنویسی نماهای BOM محصول پس از تخت‌سازی
-- فقط ProductFormulItem + Part؛ SUM(UsingRate) روی محصول + قطعه
-- مطابق TMS.TotalSystem.dbo.Vw_Bom_Product_Items (بدون GROUP BY UsingRate)
-- Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
-- ============================================

SET NOCOUNT ON;

/* نام آبجکت در کاتالوگ: Bom.vw_ProductItem (CREATE قدیمی ممکن است vw_ProductItems باشد) */
CREATE OR ALTER VIEW Bom.vw_ProductItem
AS
SELECT
    pf.Id AS ProductFormulId,
    p.Code AS PardCode,
    p.Name AS PartName,
    CAST(SUM(pfi.UsingRate) AS DECIMAL(18, 6)) AS UsingRate,
    pf.ProductId AS PartId,
    pfi.PartId AS PartItemId
FROM Bom.ProductFormul AS pf
INNER JOIN Bom.ProductFormulItem AS pfi ON pfi.ProductFormulId = pf.Id
INNER JOIN Inv.Part AS p ON p.Id = pfi.PartId
GROUP BY
    pf.Id,
    pf.ProductId,
    pfi.PartId,
    p.Code,
    p.Name;
GO

/* نام آبجکت در کاتالوگ: dbo.vw_BomProductItem */
CREATE OR ALTER VIEW dbo.vw_BomProductItem
AS
SELECT
    pf.Id AS Id,
    p.Code AS Code,
    p.Name AS Name,
    CAST(SUM(pfi.UsingRate) AS DECIMAL(18, 6)) AS UsingRate,
    p.SaleRate AS SaleRate,
    pf.ProductId AS ProductId,
    pfi.PartId AS PartItemId
FROM Bom.ProductFormul AS pf
INNER JOIN Bom.ProductFormulItem AS pfi ON pfi.ProductFormulId = pf.Id
INNER JOIN Inv.Part AS p ON p.Id = pfi.PartId
GROUP BY
    pf.Id,
    pf.ProductId,
    pfi.PartId,
    p.Code,
    p.Name,
    p.SaleRate;
GO

PRINT N'نماهای Bom.vw_ProductItem و dbo.vw_BomProductItem بازنویسی شدند.';
