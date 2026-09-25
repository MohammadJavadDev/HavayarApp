-- ============================================
-- اسکریپت همگام‌سازی ProductBom از TotalSystem به HavayarApp
-- جداول: ProductGroup, ProductFormul, ProductFormulItem (فقط قطعات تخت‌شده)
-- فرمول HTS فقط برای انفجار خوانده می‌شود و در Bom.Formul* درج نمی‌شود.
-- ضریب = UsingRate قلم محصول × UsingRate قلم فرمول، سپس SUM(ProductFormulId, PartId)
-- شناسه ProductFormul از HTS حفظ می‌شود؛ شناسه ProductFormulItem identity جدید است.
-- از طریق Linked Server با نام TMS
-- ============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    SELECT
        CAST(source.ProductGroup_ID AS BIGINT) AS Id,
        source.ProductGroup_Title AS Name
    INTO #ProductGroup
    FROM [TMS].[TotalSystem].[dbo].[Bom_ProductGroup] AS source;

SELECT
    CAST(source.ProductFormul_ID AS BIGINT) AS Id,
    targetProduct.Id AS ProductId,
    CAST(source.ProductGroup_FK AS BIGINT) AS ProductNameGroupId,
    ISNULL(source.Comment, N'') AS Comment,
    NULLIF(LTRIM(RTRIM(source.CreatedDate)), '') AS CreatedOnShamsiDateTime
INTO #ProductFormul
FROM [TMS].[TotalSystem].[dbo].[Bom_ProductFormul] AS source
INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] AS legacyProduct ON legacyProduct.Part_ID = source.Part_FK
INNER JOIN Inv.Part AS targetProduct ON targetProduct.Code = legacyProduct.Part_Code;

/* حفظ BOMهایی که تاریخچه قیمت دارند ولی دیگر از HTS نگاشت نمی‌شوند */
INSERT INTO #ProductFormul (Id, ProductId, ProductNameGroupId, Comment, CreatedOnShamsiDateTime)
SELECT
    pf.Id,
    pf.ProductId,
    pf.ProductNameGroupId,
    ISNULL(pf.Comment, N''),
    pf.CreatedOnShamsiDateTime
FROM Bom.ProductFormul AS pf
WHERE EXISTS (SELECT 1 FROM Bom.ProductPriceHistory h WHERE h.ProductFormulId = pf.Id)
  AND NOT EXISTS (SELECT 1 FROM #ProductFormul s WHERE s.Id = pf.Id);

INSERT INTO #ProductGroup (Id, Name)
SELECT pg.Id, pg.Name
FROM Bom.ProductGroup AS pg
WHERE EXISTS (
        SELECT 1
        FROM #ProductFormul pf
        WHERE pf.ProductNameGroupId = pg.Id
    )
  AND NOT EXISTS (SELECT 1 FROM #ProductGroup g WHERE g.Id = pg.Id);

    SELECT
        ProductFormulId,
        PartId,
        UsingRate
    INTO #ProductFormulItemRaw
    FROM
    (
        SELECT
            CAST(source.ProductFormul_FK AS BIGINT) AS ProductFormulId,
            targetPart.Id AS PartId,
            ISNULL(source.UsingRate, 0) AS UsingRate
        FROM [TMS].[TotalSystem].[dbo].[Bom_ProductFormul_Itm] AS source
        INNER JOIN #ProductFormul AS parent ON parent.Id = source.ProductFormul_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] AS legacyPart ON legacyPart.Part_ID = source.Part_FK
        INNER JOIN Inv.Part AS targetPart ON targetPart.Code = legacyPart.Part_Code
        WHERE source.Part_FK IS NOT NULL

        UNION ALL

        SELECT
            CAST(source.ProductFormul_FK AS BIGINT) AS ProductFormulId,
            targetPart.Id AS PartId,
            ISNULL(source.UsingRate, 0) * ISNULL(formulaItem.UsingRate, 0) AS UsingRate
        FROM [TMS].[TotalSystem].[dbo].[Bom_ProductFormul_Itm] AS source
        INNER JOIN #ProductFormul AS parent ON parent.Id = source.ProductFormul_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Bom_Formul_Itm] AS formulaItem
            ON formulaItem.Formul_Fk = source.Bom_Formul_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] AS legacyPart ON legacyPart.Part_ID = formulaItem.Part_Fk
        INNER JOIN Inv.Part AS targetPart ON targetPart.Code = legacyPart.Part_Code
        WHERE source.Bom_Formul_FK IS NOT NULL
    ) AS exploded;

    /* اقلام مستقیم محلی برای BOMهایی که فقط به‌خاطر تاریخچه قیمت حفظ شده‌اند */
    INSERT INTO #ProductFormulItemRaw (ProductFormulId, PartId, UsingRate)
    SELECT
        pfi.ProductFormulId,
        pfi.PartId,
        ISNULL(pfi.UsingRate, 0)
    FROM Bom.ProductFormulItem AS pfi
    WHERE pfi.PartId IS NOT NULL
      AND EXISTS (SELECT 1 FROM #ProductFormul pf WHERE pf.Id = pfi.ProductFormulId)
      AND NOT EXISTS (
            SELECT 1
            FROM [TMS].[TotalSystem].[dbo].[Bom_ProductFormul] AS src
            WHERE CAST(src.ProductFormul_ID AS BIGINT) = pfi.ProductFormulId
        );

    SELECT
        ProductFormulId,
        PartId,
        SUM(UsingRate) AS UsingRate
    INTO #ProductFormulItem
    FROM
    (
        /* مطابق Vw_Bom_Product_Items که UNION (نه UNION ALL) است و ردیف‌های کاملاً یکسان را قبل از SUM حذف می‌کند */
        SELECT DISTINCT
            ProductFormulId,
            PartId,
            UsingRate
        FROM #ProductFormulItemRaw
    ) AS distinctRates
    GROUP BY ProductFormulId, PartId;

    IF EXISTS (SELECT Id FROM #ProductGroup GROUP BY Id HAVING COUNT_BIG(*) <> 1)
        THROW 51004, N'شناسه تکراری در گروه‌های محصول HTS یافت شد.', 1;

    IF EXISTS (SELECT Id FROM #ProductFormul GROUP BY Id HAVING COUNT_BIG(*) <> 1)
        THROW 51005, N'شناسه تکراری در فرمول محصولات HTS یا نگاشت Product یافت شد.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM #ProductFormul AS source
        LEFT JOIN #ProductGroup AS parent ON parent.Id = source.ProductNameGroupId
        WHERE source.ProductNameGroupId IS NOT NULL AND parent.Id IS NULL
    )
        THROW 51008, N'گروه یکی از فرمول‌های محصول HTS در منبع وجود ندارد.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM Bom.ProductPriceHistory AS history
        LEFT JOIN #ProductFormul AS source ON source.Id = history.ProductFormulId
        WHERE source.Id IS NULL
    )
        THROW 51009, N'حداقل یک فرمول دارای تاریخچه قیمت در داده قابل‌انتقال HTS وجود ندارد؛ عملیات متوقف شد.', 1;

    ALTER TABLE Bom.ProductPriceHistory
        NOCHECK CONSTRAINT FK_ProductPriceHistory_ProductFormul_ProductFormulId;

    DELETE FROM Bom.ProductFormulItem;
    DELETE FROM Bom.ProductFormul;
    DELETE FROM Bom.ProductGroup;

    SET IDENTITY_INSERT Bom.ProductGroup ON;
    INSERT INTO Bom.ProductGroup
    (
        Id, Name, CreatedByName, CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime, IsActive
    )
    SELECT Id, Name, N'HTS', NULL, NULL, 1
    FROM #ProductGroup;
    SET IDENTITY_INSERT Bom.ProductGroup OFF;
    DBCC CHECKIDENT ('Bom.ProductGroup', RESEED) WITH NO_INFOMSGS;

    SET IDENTITY_INSERT Bom.ProductFormul ON;
    INSERT INTO Bom.ProductFormul
    (
        Id, ProductId, ProductNameGroupId, Comment,
        CreatedByName, CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime, IsActive
    )
    SELECT
        Id, ProductId, ProductNameGroupId, Comment,
        N'HTS', NULL, CreatedOnShamsiDateTime, 1
    FROM #ProductFormul;
    SET IDENTITY_INSERT Bom.ProductFormul OFF;
    DBCC CHECKIDENT ('Bom.ProductFormul', RESEED) WITH NO_INFOMSGS;

    INSERT INTO Bom.ProductFormulItem
    (
        ProductFormulId, PartId, UsingRate,
        CreatedByName, CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime, IsActive
    )
    SELECT
        ProductFormulId, PartId, UsingRate,
        N'HTS', NULL, NULL, 1
    FROM #ProductFormulItem;

    ALTER TABLE Bom.ProductPriceHistory WITH CHECK
        CHECK CONSTRAINT FK_ProductPriceHistory_ProductFormul_ProductFormulId;

    DBCC CHECKIDENT ('Bom.ProductFormulItem', RESEED) WITH NO_INFOMSGS;

    UPDATE p
    SET p.[Foreign] = ip.IsExternal
    FROM Inv.Part p
    INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] ip ON p.Code = ip.Part_Code;

    IF (SELECT COUNT_BIG(*) FROM Bom.ProductGroup) <> (SELECT COUNT_BIG(*) FROM #ProductGroup)
        THROW 51013, N'تعداد گروه‌های محصول درج‌شده با staging برابر نیست.', 1;

    IF (SELECT COUNT_BIG(*) FROM Bom.ProductFormul) <> (SELECT COUNT_BIG(*) FROM #ProductFormul)
        THROW 51014, N'تعداد فرمول محصولات درج‌شده با staging برابر نیست.', 1;

    IF (SELECT COUNT_BIG(*) FROM Bom.ProductFormulItem) <> (SELECT COUNT_BIG(*) FROM #ProductFormulItem)
        THROW 51015, N'تعداد اقلام BOM محصول درج‌شده با staging برابر نیست.', 1;

    COMMIT TRANSACTION;

    DECLARE @PgCnt BIGINT = (SELECT COUNT_BIG(*) FROM #ProductGroup);
    DECLARE @PfCnt BIGINT = (SELECT COUNT_BIG(*) FROM #ProductFormul);
    DECLARE @PfiCnt BIGINT = (SELECT COUNT_BIG(*) FROM #ProductFormulItem);

    PRINT N'همگام‌سازی ProductBom (تخت‌شده) با موفقیت انجام شد.';
    PRINT N'گروه محصول: ' + CAST(@PgCnt AS NVARCHAR(20));
    PRINT N'BOM محصول: ' + CAST(@PfCnt AS NVARCHAR(20));
    PRINT N'قلم قطعه: ' + CAST(@PfiCnt AS NVARCHAR(20));

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
