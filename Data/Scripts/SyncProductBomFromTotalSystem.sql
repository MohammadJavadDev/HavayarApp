-- ============================================
-- اسکریپت همگام‌سازی ProductBom از TotalSystem به HavayarApp
-- جداول: ProductGroup, ProductFormul, ProductFormulItem
-- از طریق Linked Server با نام TMS
-- تطبیق Part بر اساس Code
-- Id های درج شده دقیقاً مطابق سیستم قبلی (TotalSystem) هستند
-- ============================================

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- ۱. حذف داده‌های قدیمی (به ترتیب وابستگی FK)
    DELETE FROM Bom.ProductFormulItem;

    DELETE FROM Bom.ProductFormul;

    -- FormulChangeRequest به ProductGroup وابسته است
    DELETE FROM Bom.FormulChangeRequest;

    DELETE FROM Bom.ProductGroup;

    -- ۲. درج ProductGroup از TotalSystem
    SET IDENTITY_INSERT Bom.ProductGroup ON;

    INSERT INTO Bom.ProductGroup
    (
    Id,
    Name,
    CreatedById,
    ModifiedById,
    CreatedByName,
    ModifiedByName,
    ModifiedDateMiladiDateTime,
    ModifiedDateShamsiDateTime,
    CreatedOnMiladiDateTime,
    CreatedOnShamsiDateTime,
    IsActive
    )
SELECT
    CAST(ProductGroup_ID AS BIGINT)   AS Id,
    ProductGroup_Title                AS Name,
    NULL                             AS CreatedById,
    NULL                             AS ModifiedById,
    NULL                             AS CreatedByName,
    NULL                             AS ModifiedByName,
    NULL                             AS ModifiedDateMiladiDateTime,
    NULL                             AS ModifiedDateShamsiDateTime,
    NULL                             AS CreatedOnMiladiDateTime,
    NULL                             AS CreatedOnShamsiDateTime,
    1                                AS IsActive
FROM [TMS].[TotalSystem].[dbo].[Bom_ProductGroup];

    SET IDENTITY_INSERT Bom.ProductGroup OFF;
    DBCC CHECKIDENT ('Bom.ProductGroup', RESEED) WITH NO_INFOMSGS;

    -- ۳. درج ProductFormul از TotalSystem
    -- ProductId و ProductNameGroupId از Part_FK و ProductGroup_FK با تطبیق Code/Id
    SET IDENTITY_INSERT Bom.ProductFormul ON;

    INSERT INTO Bom.ProductFormul
    (
    Id,
    ProductId,
    ProductNameGroupId,
    Comment,
    CreatedById,
    ModifiedById,
    CreatedByName,
    ModifiedByName,
    ModifiedDateMiladiDateTime,
    ModifiedDateShamsiDateTime,
    CreatedOnMiladiDateTime,
    CreatedOnShamsiDateTime,
    IsActive
    )
SELECT
    CAST(pf.ProductFormul_ID AS BIGINT)  AS Id,
    p_product.Id                          AS ProductId, /* Part_FK → ProductId تطبیق با Code */
    CAST(pf.ProductGroup_FK AS BIGINT)    AS ProductNameGroupId,
    ISNULL(pf.Comment, N'')               AS Comment,
    NULL                                  AS CreatedById,
    NULL                                  AS ModifiedById,
    NULL                                  AS CreatedByName,
    NULL                                  AS ModifiedByName,
    NULL                                  AS ModifiedDateMiladiDateTime,
    NULL                                  AS ModifiedDateShamsiDateTime,
    NULL                                  AS CreatedOnMiladiDateTime,
    NULL                                  AS CreatedOnShamsiDateTime,
    1                                     AS IsActive
FROM [TMS].[TotalSystem].[dbo].[Bom_ProductFormul] pf
    INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] pt ON pt.Part_ID = pf.Part_FK
    INNER JOIN Inv.Part p_product ON p_product.Code = pt.Part_Code;

    SET IDENTITY_INSERT Bom.ProductFormul OFF;
    DBCC CHECKIDENT ('Bom.ProductFormul', RESEED) WITH NO_INFOMSGS;

    -- ۴. درج ProductFormulItem از TotalSystem
    -- PartId با تطبیق Code
    SET IDENTITY_INSERT Bom.ProductFormulItem ON;

    INSERT INTO Bom.ProductFormulItem
    (
    Id,
    ProductFormulId,
    FormulId,
    PartId,
    UsingRate,
    CreatedById,
    ModifiedById,
    CreatedByName,
    ModifiedByName,
    ModifiedDateMiladiDateTime,
    ModifiedDateShamsiDateTime,
    CreatedOnMiladiDateTime,
    CreatedOnShamsiDateTime,
    IsActive
    )
SELECT
    CAST(pfi.ProductFormul_Itm_ID AS BIGINT)     AS Id,
    CAST(pfi.ProductFormul_FK AS BIGINT)        AS ProductFormulId,
    CASE WHEN f.Id IS NOT NULL THEN CAST(pfi.Bom_Formul_FK AS BIGINT) ELSE NULL END AS FormulId, /* Bom_Formul_FK → FormulId (فقط اگر Formul در Bom.Formul وجود داشته باشد) */
    p.Id                                         AS PartId, /* تطبیق Part بر اساس Code */
    ISNULL(pfi.UsingRate, 0)        AS UsingRate,
    NULL                                         AS CreatedById,
    NULL                                         AS ModifiedById,
    NULL                                         AS CreatedByName,
    NULL                                         AS ModifiedByName,
    NULL                                         AS ModifiedDateMiladiDateTime,
    NULL                                         AS ModifiedDateShamsiDateTime,
    pfi.CreatedDate                              AS CreatedOnMiladiDateTime,
    pfi.CreatedDateInText                        AS CreatedOnShamsiDateTime,
    1                                            AS IsActive
FROM [TMS].[TotalSystem].[dbo].[Bom_ProductFormul_Itm] pfi
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] pt ON pt.Part_ID = pfi.Part_FK
    LEFT JOIN Inv.Part p ON p.Code = pt.Part_Code
    LEFT JOIN Bom.Formul f ON f.Id = pfi.Bom_Formul_FK
WHERE EXISTS (
        SELECT 1
FROM [TMS].[TotalSystem].[dbo].[Bom_ProductFormul] pf
    INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] pt2 ON pt2.Part_ID = pf.Part_FK
    INNER JOIN Inv.Part p2 ON p2.Code = pt2.Part_Code
WHERE pf.ProductFormul_ID = pfi.ProductFormul_FK
    );

    SET IDENTITY_INSERT Bom.ProductFormulItem OFF;
    DBCC CHECKIDENT ('Bom.ProductFormulItem', RESEED) WITH NO_INFOMSGS;


    UPDATE p
    SET p.[Foreign] = ip.IsExternal
    from Inv.Part p
    inner JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] ip ON p.Code = ip.Part_Code

    COMMIT TRANSACTION;

    PRINT N'همگام‌سازی ProductBom با موفقیت انجام شد.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
