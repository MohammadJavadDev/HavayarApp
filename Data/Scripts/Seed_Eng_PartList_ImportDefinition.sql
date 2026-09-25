-- ============================================================================
-- ورود اکسل پارت لیست محصول — ستون‌های HTS:
--   ProductCode, CategoryTitle, PartCode, Amount, Order, InActive, Comment
-- UTF-8 with BOM
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-eng-partlist-import';
    DECLARE @FullNameEntity NVARCHAR(200) = N'Entities.App.Eng.PartListProductBom';
    DECLARE @Title NVARCHAR(200) = N'ورود پارت لیست محصول (اکسل)';
    DECLARE @Description NVARCHAR(500) = N'ستون‌ها: ProductCode، CategoryTitle، PartCode، Amount، Order، InActive، Comment';

    DECLARE @SqlQuery NVARCHAR(MAX) = N'SET NOCOUNT ON;
DECLARE @ProductCode NVARCHAR(200) = LTRIM(RTRIM(CAST(@ProductCode AS NVARCHAR(200))));
DECLARE @CategoryTitle NVARCHAR(255) = LTRIM(RTRIM(CAST(@CategoryTitle AS NVARCHAR(255))));
DECLARE @PartCode NVARCHAR(200) = LTRIM(RTRIM(CAST(@PartCode AS NVARCHAR(200))));
DECLARE @Amount DECIMAL(18,4) = TRY_CAST(@Amount AS DECIMAL(18,4));
DECLARE @Ord SMALLINT = TRY_CAST(@Order AS SMALLINT);
DECLARE @InActiveBit BIT = CASE
    WHEN LOWER(LTRIM(RTRIM(CAST(@InActive AS NVARCHAR(20))))) IN (N''1'', N''true'', N''yes'', N''بله'') THEN 1
    ELSE 0 END;
DECLARE @Comment NVARCHAR(1024) = NULLIF(LTRIM(RTRIM(CAST(@Comment AS NVARCHAR(1024)))), N'''');
DECLARE @ProductId BIGINT;
DECLARE @PartId BIGINT;
DECLARE @SectionId BIGINT;

IF @ProductCode IS NULL OR @ProductCode = N'''' THROW 51701, N''کد محصول خالی است.'', 1;
IF @PartCode IS NULL OR @PartCode = N'''' THROW 51702, N''کد کالا خالی است.'', 1;
IF @CategoryTitle IS NULL OR @CategoryTitle = N'''' THROW 51703, N''عنوان دسته خالی است.'', 1;
IF @Amount IS NULL THROW 51704, N''مقدار نامعتبر است.'', 1;
IF @Ord IS NULL SET @Ord = 1;

SELECT TOP 1 @ProductId = Id FROM Inv.Part WHERE Code = @ProductCode;
IF @ProductId IS NULL THROW 51705, N''محصول یافت نشد.'', 1;
SELECT TOP 1 @PartId = Id FROM Inv.Part WHERE Code = @PartCode;
IF @PartId IS NULL THROW 51706, N''کالا یافت نشد.'', 1;

SELECT TOP 1 @SectionId = Id FROM Eng.PartListProductSection
WHERE ProductId = @ProductId AND Title = @CategoryTitle;
IF @SectionId IS NULL
BEGIN
    DECLARE @MaxOrd TINYINT = ISNULL((SELECT MAX([Order]) FROM Eng.PartListProductSection WHERE ProductId = @ProductId), 0);
    INSERT INTO Eng.PartListProductSection (HtsId, ProductId, Title, [Order], IsCover, IsDisabled, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    VALUES (0, @ProductId, @CategoryTitle, @MaxOrd + 1, 0, 0, @CurrentUserName, @CurrentDateTimeMiladi, @CurrentDateTimeShamsi, 1);
    SET @SectionId = SCOPE_IDENTITY();
END

IF EXISTS (
    SELECT 1 FROM Eng.PartListProductBom
    WHERE ProductId = @ProductId AND ProductSectionId = @SectionId AND PartId = @PartId AND [Order] = @Ord
)
BEGIN
    UPDATE Eng.PartListProductBom
    SET Qty = @Amount, IsIgnored = @InActiveBit, Comment = @Comment,
        ModifiedByName = @CurrentUserName, ModifiedDateMiladiDateTime = @CurrentDateTimeMiladi, ModifiedDateShamsiDateTime = @CurrentDateTimeShamsi
    WHERE ProductId = @ProductId AND ProductSectionId = @SectionId AND PartId = @PartId AND [Order] = @Ord;
END
ELSE
BEGIN
    INSERT INTO Eng.PartListProductBom
    (HtsId, ProductSectionId, ProductId, PartId, Qty, [Order], IsIgnored, Comment, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    VALUES
    (0, @SectionId, @ProductId, @PartId, @Amount, @Ord, @InActiveBit, @Comment, @CurrentUserName, @CurrentDateTimeMiladi, @CurrentDateTimeShamsi, 1);
END
';

    DECLARE @Columns NVARCHAR(MAX) = N'[
{"ColumnName":"ProductCode","DisplayName":"کد محصول","SystemType":"String","IsRequired":true,"Order":1},
{"ColumnName":"CategoryTitle","DisplayName":"عنوان دسته","SystemType":"String","IsRequired":true,"Order":2},
{"ColumnName":"PartCode","DisplayName":"کد کالا","SystemType":"String","IsRequired":true,"Order":3},
{"ColumnName":"Amount","DisplayName":"مقدار","SystemType":"Decimal","IsRequired":true,"Order":4},
{"ColumnName":"Order","DisplayName":"ترتیب","SystemType":"Int","IsRequired":false,"Order":5},
{"ColumnName":"InActive","DisplayName":"غیرفعال","SystemType":"String","IsRequired":false,"Order":6},
{"ColumnName":"Comment","DisplayName":"توضیحات","SystemType":"String","IsRequired":false,"Order":7}
]';

    DECLARE @DefId BIGINT;
    SELECT TOP 1 @DefId = Id FROM [System].[ImportDefinition] WHERE FullNameEntity = @FullNameEntity;

    IF @DefId IS NOT NULL
        UPDATE [System].[ImportDefinition]
        SET Title = @Title, Description = @Description, ImportType = 0, SqlQuery = @SqlQuery, Columns = @Columns,
            ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Id = @DefId;
    ELSE
    BEGIN
        INSERT INTO [System].[ImportDefinition]
        (FullNameEntity, Title, Description, ImportType, SqlQuery, ApiUrl, ApiHttpMethod, ApiCallMode, ApiIsInternal, Columns,
         CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
        VALUES
        (@FullNameEntity, @Title, @Description, 0, @SqlQuery, N'', N'POST', NULL, 0, @Columns,
         @SeedUser, @Now, @NowShamsi, 1);
        SET @DefId = SCOPE_IDENTITY();
    END

    DECLARE @ImportPath NVARCHAR(100) = N'importData_' + CAST(@DefId AS NVARCHAR(20));

    INSERT INTO [system].[RoleAccess]
    (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
     CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    SELECT @ImportPath, 4, 7, @FullNameEntity, NULL, NULL, @DefId, r.Id, @SeedUser, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE r.Name IN (N'ShowAllMenus', N'Engineering', N'FullAccess')
       OR EXISTS (SELECT 1 FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND ra.Path LIKE N'/panel/eng/partlist%')
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x WHERE x.RoleId = r.Id AND x.Path = @ImportPath AND x.ActionAccessType = 4
    );

    INSERT INTO [system].[RoleAccess]
    (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
     CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    SELECT N'/panel/importdata/list', 1, 1, N'ImportData', NULL, NULL, NULL, r.Id, @SeedUser, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE r.Name IN (N'ShowAllMenus', N'Engineering', N'FullAccess')
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x WHERE x.RoleId = r.Id AND x.Path = N'/panel/importdata/list' AND x.ActionAccessType = 1
    );

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Eng_PartList_ImportDefinition ===';
    SELECT Id, FullNameEntity, Title FROM [System].[ImportDefinition] WHERE Id = @DefId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH
