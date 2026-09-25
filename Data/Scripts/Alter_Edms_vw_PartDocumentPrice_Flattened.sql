﻿-- ============================================
-- بازنویسی Edms.vw_PartDocumentPrice پس از تخت‌سازی BOM
-- قیمت اجزا فقط از ProductFormulItem.PartId + UsingRate
-- (بدون Bom.Formul / FormulItem / ضرب دوباره نرخ)
-- ستون‌های خروجی بدون تغییر: Id, ProductId, InternalTotalBuyPrice, ExternalTotalBuyPrice, BuyPrice
-- Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
-- ============================================

SET NOCOUNT ON;
GO

CREATE OR ALTER VIEW [Edms].[vw_PartDocumentPrice]
AS
WITH lastPrice AS
(
    SELECT
        ipp.*,
        p.[Foreign],
        ROW_NUMBER() OVER (PARTITION BY ipp.PartId ORDER BY ipp.CreatedOnMiladiDateTime DESC) AS rn
    FROM Sup.InquiryPartPrice AS ipp
    INNER JOIN Inv.Part AS p ON ipp.PartId = p.Id
    WHERE ipp.Type <> 1675
      AND ipp.PriceStatus <> 2834
      AND ipp.PriceUnitId =
            CASE
                WHEN ISNULL(p.[Foreign], 0) = 0 THEN 1
                ELSE 4
            END
)
SELECT
    PVTTable.Id,
    PVTTable.ProductId,
    [0] AS InternalTotalBuyPrice,
    [1] AS ExternalTotalBuyPrice,
    ISNULL([0], 0) + ISNULL([1], 0) AS BuyPrice
FROM
(
    SELECT
        pf.Id,
        pf.ProductId,
        IIF(ipp.UnitPrice IS NULL, ipp2.[Foreign], ipp.[Foreign]) AS [Foreign],
        CAST(SUM(ISNULL(pfi.UsingRate, 1) * ipp.UnitPrice) AS BIGINT) AS BuyPrice
    FROM Bom.ProductFormul AS pf
    LEFT JOIN Bom.ProductFormulItem AS pfi WITH (NOLOCK)
        ON pf.Id = pfi.ProductFormulId
    LEFT JOIN lastPrice AS ipp
        ON pfi.PartId = ipp.PartId
       AND ipp.rn = 1
    LEFT JOIN lastPrice AS ipp2
        ON pf.ProductId = ipp2.PartId
       AND ipp2.rn = 1
    GROUP BY
        IIF(ipp.UnitPrice IS NULL, ipp2.[Foreign], ipp.[Foreign]),
        pf.Id,
        pf.ProductId
) AS s
PIVOT (SUM(BuyPrice) FOR [Foreign] IN ([0], [1])) AS PVTTable;
GO

PRINT N'Edms.vw_PartDocumentPrice بازنویسی شد (BOM تخت‌شده، بدون FormulItem).';
