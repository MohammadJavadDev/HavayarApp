-- ============================================
-- اسکریپت همگام‌سازی Bom از TotalSystem به HavayarApp
-- از طریق Linked Server با نام TMS
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

    -- ۱. حذف داده‌های قدیمی (به ترتیب وابستگی FK)
    -- ابتدا FormulItem (وابسته به Formul)
    DELETE FROM Bom.FormulItem;

    -- سپس Formul (وابسته به FormulGroup)
    DELETE FROM Bom.Formul;

    -- در آخر FormulGroup
    DELETE FROM Bom.FormulGroup;

   

    -- ۲. درج FormulGroup از TotalSystem
    SET IDENTITY_INSERT Bom.FormulGroup ON;

    INSERT INTO Bom.FormulGroup
    (
    Id,
    Title,
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
    CAST(FormulGroup_ID AS BIGINT)    AS Id,
    FormulGroup_Title                 AS Title,
    NULL                              AS CreatedById,
    NULL                              AS ModifiedById,
    NULL                              AS CreatedByName,
    NULL                              AS ModifiedByName,
    NULL                              AS ModifiedDateMiladiDateTime,
    NULL                              AS ModifiedDateShamsiDateTime,
    NULL                              AS CreatedOnMiladiDateTime,
    NULL                              AS CreatedOnShamsiDateTime,
    1                                 AS IsActive
FROM [TMS].[TotalSystem].[dbo].[Bom_FormulGroup];

    SET IDENTITY_INSERT Bom.FormulGroup OFF;
    DBCC CHECKIDENT ('Bom.FormulGroup', RESEED) WITH NO_INFOMSGS;

    -- ۳. درج Formul از TotalSystem
    SET IDENTITY_INSERT Bom.Formul ON;

    INSERT INTO Bom.Formul
    (
    Id,
    Title,
    GroupId,
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
    CAST(Formul_ID AS BIGINT)         AS Id,
    Formul_Title                      AS Title,
    CAST(FormulGroup_FK AS BIGINT)    AS GroupId,
    NULL                              AS CreatedById,
    NULL                              AS ModifiedById,
    NULL                              AS CreatedByName,
    NULL                              AS ModifiedByName,
    NULL                              AS ModifiedDateMiladiDateTime,
    NULL                              AS ModifiedDateShamsiDateTime,
    NULL                              AS CreatedOnMiladiDateTime,
    NULL                              AS CreatedOnShamsiDateTime,
    1                                 AS IsActive
FROM [TMS].[TotalSystem].[dbo].[Bom_Formul]
WHERE FormulGroup_FK IS NOT NULL;

    SET IDENTITY_INSERT Bom.Formul OFF;
    DBCC CHECKIDENT ('Bom.Formul', RESEED) WITH NO_INFOMSGS;

    -- ۴. درج FormulItem از TotalSystem
    SET IDENTITY_INSERT Bom.FormulItem ON;

    INSERT INTO Bom.FormulItem
    (
    Id,
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
    CAST(Formul_Itm_ID AS BIGINT)     AS Id,
    CAST(Formul_Fk AS BIGINT)         AS FormulId,
    p.Id                              AS PartId, /* تطبیق Part بر اساس Code */
    ISNULL(fi.UsingRate, 0) AS UsingRate,
    NULL                              AS CreatedById,
    NULL                              AS ModifiedById,
    NULL                              AS CreatedByName,
    NULL                              AS ModifiedByName,
    NULL                              AS ModifiedDateMiladiDateTime,
    NULL                              AS ModifiedDateShamsiDateTime,
    fi.CreatedDate                    AS CreatedOnMiladiDateTime,
    fi.CreatedDateInText              AS CreatedOnShamsiDateTime,
    1                                 AS IsActive
FROM [TMS].[TotalSystem].[dbo].[Bom_Formul_Itm] fi
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] pt ON pt.Part_ID = fi.Part_Fk
    LEFT JOIN Inv.Part p ON p.Code = pt.Part_Code
WHERE EXISTS (SELECT 1
FROM [TMS].[TotalSystem].[dbo].[Bom_Formul] f
WHERE f.Formul_ID = fi.Formul_Fk AND f.FormulGroup_FK IS NOT NULL);

    SET IDENTITY_INSERT Bom.FormulItem OFF;
    DBCC CHECKIDENT ('Bom.FormulItem', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'همگام‌سازی Bom با موفقیت انجام شد.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
