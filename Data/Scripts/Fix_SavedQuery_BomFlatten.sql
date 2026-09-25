-- ============================================
-- اصلاح SavedQuery پس از تخت‌سازی BOM
-- - پروفایل‌های فرمول غیرفعال می‌شوند
-- - نمایه‌های BOM محصول فقط از ProductFormulItem می‌خوانند
-- Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
-- ============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    /* پروفایل‌های موجودیت فرمول دیگر معتبر نیستند */
    UPDATE system.SavedQuery
    SET IsActive = 0,
        ModifiedByName = N'flatten-bom',
        ModifiedDateMiladiDateTime = SYSUTCDATETIME()
    WHERE Name IN (
        N'formul_listinfo',
        N'formulgroup_listinfo',
        N'formulgroup_listinfonew22',
        N'vw_formulwithItems'
    );

    /* لیست با Bom — فقط قطعات تخت‌شده روی ProductFormulItem */
    UPDATE system.SavedQuery
    SET QueryJson = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"SELECT\n    [t1].[Id] AS [t1_Id],\n    [t3].[Name] AS [t3_Name],\n    [t4].[Name] AS [t4_Name],\n    [t4].[Code] AS [t4_Code],\n    [t8].[Name] AS [t8_Name],\n    [t8].[Code] AS [t8_Code],\n    [t2].[UsingRate] AS [t2_UsingRate],\n    CAST(NULL AS nvarchar(max)) AS [t5_Title],\n    CAST(NULL AS decimal(18,2)) AS [t6_UsingRate],\n    CAST(NULL AS nvarchar(max)) AS [t7_Name],\n    CAST(NULL AS nvarchar(max)) AS [t7_Code],\n    CAST(NULL AS bit) AS [t7_Foreign],\n    CAST(NULL AS bit) AS [t7_EngineeringRoutine],\n    [t1].[ModifiedByName] AS [t1_ModifiedByName],\n    [t1].[CreatedByName] AS [t1_CreatedByName],\n    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],\n    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]\nFROM [Bom].[ProductFormul] AS [t1]\nLEFT JOIN [Inv].[Part] AS [t4]\n    ON [t1].[ProductId] = [t4].[Id]\nLEFT JOIN [Bom].[ProductGroup] AS [t3]\n    ON [t1].[ProductNameGroupId] = [t3].[Id]\nINNER JOIN [Bom].[ProductFormulItem] AS [t2]\n    ON [t2].[ProductFormulId] = [t1].[Id]\nINNER JOIN [Inv].[Part] AS [t8]\n    ON [t2].[PartId] = [t8].[Id]","Parameters":[],"Selects":null}',
        ModifiedByName = N'flatten-bom',
        ModifiedDateMiladiDateTime = SYSUTCDATETIME()
    WHERE Name = N'vw_productformulwithBom';

    /* لیست با Bom و قیمت — فقط قطعات تخت‌شده */
    UPDATE system.SavedQuery
    SET QueryJson = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"with lastPrice as (\r\n    select *, row_number() over (partition by PartId order by CreatedOnMiladiDateTime desc) rn\r\n    from Sup.InquiryPartPrice\r\n)\r\n\r\n--!--mainsection\r\n\r\nselect\r\n    t1.Id as ProductFormulId,\r\n    t3.Name as ProductGroupName,\r\n    t4.Name as ProductName,\r\n    t4.Code as ProductCode,\r\n    t1.ModifiedByName,\r\n    t1.CreatedByName,\r\n    t1.ModifiedDateShamsiDateTime,\r\n    t1.CreatedOnShamsiDateTime,\r\n\r\n    t2.UsingRate as UsingRate,\r\n    1 as ItemType,\r\n    t8.Id as PartId,\r\n    t8.Name as PartName,\r\n    t8.Code as PartCode,\r\n    t8.[Foreign] as PartForeign,\r\n    t8.EngineeringRoutine as PartEngineeringRoutine,\r\n    lp.UnitPrice as PartUnitPrice,\r\n    null [FormulTitle]\r\n\r\nfrom Bom.ProductFormul t1\r\nleft join Inv.Part t4 on t1.ProductId = t4.Id\r\nleft join Bom.ProductGroup t3 on t1.ProductNameGroupId = t3.Id\r\ninner join Bom.ProductFormulItem t2 on t2.ProductFormulId = t1.Id\r\ninner join Inv.Part t8 on t2.PartId = t8.Id\r\nleft join lastPrice lp on lp.PartId = t8.Id and lp.rn = 1","Parameters":[],"Selects":[{"Index":0,"Name":"Select1","Title":"Select1"}]}',
        ModifiedByName = N'flatten-bom',
        ModifiedDateMiladiDateTime = SYSUTCDATETIME()
    WHERE Name = N'vw_productformulwithBomWithPrice';

    /* دسترسی RoleAccess مسیرهای فرمول */
    DELETE FROM system.RoleAccess
    WHERE Path LIKE N'/panel/bom/formul/%'
       OR Path LIKE N'/panel/bom/formulgroup/%';

    /* منو: حذف آیتم‌های فرمول از Content JSON در صورت وجود path */
    UPDATE system.SystemMenu
    SET Content = REPLACE(REPLACE(Content, N'/panel/bom/formul/list', N'/panel/bom/productformul/list'), N'/panel/bom/formulgroup/list', N'/panel/bom/productformul/list'),
        ModifiedByName = N'flatten-bom',
        ModifiedDateMiladiDateTime = SYSUTCDATETIME()
    WHERE Content LIKE N'%/panel/bom/formul%'
       OR Content LIKE N'%formulgroup%';

    COMMIT TRANSACTION;
    PRINT N'SavedQuery / RoleAccess / Menu برای تخت‌سازی BOM به‌روز شد.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
