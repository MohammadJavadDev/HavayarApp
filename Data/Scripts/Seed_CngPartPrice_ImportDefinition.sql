/*
Seed_CngPartPrice_ImportDefinition.sql
ورود اکسل مدیریت قیمت قطعات CNG — قواعد HTS CngPartPriceManagementController.AddByExcel.
UTF-8 BOM. Idempotent روی FullNameEntity = Entities.App.Cng.PartPrice
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-partprice-import';

    DECLARE @FullNameEntity NVARCHAR(200) = N'Entities.App.Cng.PartPrice';
    DECLARE @Title NVARCHAR(200) = N'ورود اطلاعات قیمت قطعات CNG (اکسل)';
    DECLARE @Description NVARCHAR(500) = N'ورود/به‌روزرسانی قیمت قطعات CNG از اکسل — ستون‌ها: کد کالا*، قیمت*، کامنت، توضیحات، تاریخ بروزرسانی، داخلی*، خارجی*. کلید: کالا (PartId).';

    DECLARE @SqlQuery NVARCHAR(MAX) = N'SET NOCOUNT ON;

DECLARE @Code NVARCHAR(200) = LTRIM(RTRIM(CAST(@PartCode AS NVARCHAR(200))));
DECLARE @PartId BIGINT;
DECLARE @PriceVal BIGINT;
DECLARE @Msg NVARCHAR(400);
DECLARE @IsInternalRaw NVARCHAR(50) = LTRIM(RTRIM(CAST(@IsInternal AS NVARCHAR(50))));
DECLARE @IsExternalRaw NVARCHAR(50) = LTRIM(RTRIM(CAST(@IsExternal AS NVARCHAR(50))));
DECLARE @UpdatedShamsi NVARCHAR(20) = LTRIM(RTRIM(CAST(@UpdatedDate AS NVARCHAR(20))));
DECLARE @UpdatedMiladi DATETIME2;
DECLARE @TodayShamsi NVARCHAR(10) = LEFT(dbo.ToShamsiDate(CAST(GETDATE() AS DATE), N''/''), 10);
DECLARE @CommentVal NVARCHAR(4000) = CAST(@Comment AS NVARCHAR(4000));
DECLARE @DescVal NVARCHAR(4000) = CAST(@Description AS NVARCHAR(4000));
DECLARE @IsInternalBit BIT;
DECLARE @IsExternalBit BIT;

IF @Code IS NULL OR @Code = N''''
    THROW 51610, N''کد کالا خالی است.'', 1;

SELECT TOP (1) @PartId = p.Id
FROM Inv.Part p
WHERE p.Code = @Code
ORDER BY p.Id;

IF @PartId IS NULL
BEGIN
    SET @Msg = N''کد کالا «'' + @Code + N''» در سیستم یافت نشد.'';
    THROW 51611, @Msg, 1;
END;

IF @Price IS NULL
    THROW 51612, N''قیمت خالی است.'', 1;

SET @PriceVal = TRY_CAST(@Price AS BIGINT);
IF @PriceVal IS NULL
    THROW 51613, N''قیمت معتبر نیست.'', 1;

IF @IsInternalRaw IS NULL OR @IsInternalRaw = N''''
    THROW 51614, N''داخلی خالی است.'', 1;
IF @IsExternalRaw IS NULL OR @IsExternalRaw = N''''
    THROW 51615, N''خارجی خالی است.'', 1;

SET @IsInternalBit = CASE WHEN @IsInternalRaw = N''1'' THEN 1 ELSE 0 END;
SET @IsExternalBit = CASE WHEN @IsExternalRaw = N''1'' THEN 1 ELSE 0 END;

IF @UpdatedShamsi IS NULL OR @UpdatedShamsi = N''''
    SET @UpdatedShamsi = @TodayShamsi;

SET @UpdatedMiladi = TRY_CAST(dbo.fn_ShamsiToMiladiDate(@UpdatedShamsi, N''/'') AS DATETIME2);
IF @UpdatedMiladi IS NULL
BEGIN
    SET @UpdatedShamsi = @TodayShamsi;
    SET @UpdatedMiladi = CAST(GETDATE() AS DATE);
END;

IF EXISTS (SELECT 1 FROM Cng.PartPrice pp WHERE pp.PartId = @PartId)
BEGIN
    UPDATE Cng.PartPrice
    SET Price = @PriceVal,
        Comment = @CommentVal,
        Description = @DescVal,
        UpdatedDate = @UpdatedMiladi,
        UpdatedDateShamsi = LEFT(@UpdatedShamsi, 10),
        IsInternal = @IsInternalBit,
        IsExternal = @IsExternalBit,
        ModifiedById = @CurrentUserId,
        ModifiedByName = @CurrentUserName,
        ModifiedDateMiladiDateTime = @CurrentDateTimeMiladi,
        ModifiedDateShamsiDateTime = @CurrentDateTimeShamsi,
        IsActive = 1
    WHERE PartId = @PartId;
END
ELSE
BEGIN
    INSERT INTO Cng.PartPrice
        (HtsId, PartId, Price, Comment, Description, UpdatedDate, UpdatedDateShamsi, IsInternal, IsExternal,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
         IsActive)
    VALUES
        (0, @PartId, @PriceVal, @CommentVal, @DescVal, @UpdatedMiladi, LEFT(@UpdatedShamsi, 10), @IsInternalBit, @IsExternalBit,
         @CurrentUserId, @CurrentUserId, @CurrentUserName, @CurrentUserName,
         @CurrentDateTimeMiladi, @CurrentDateTimeShamsi, @CurrentDateTimeMiladi, @CurrentDateTimeShamsi,
         1);
END;
';

    DECLARE @Columns NVARCHAR(MAX) = N'[
{"ColumnName":"PartCode","DisplayName":"کد کالا","DataType":0,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":true,"SortOrder":0,"OptionSettings":null,"ImportDefinitionId":0},
{"ColumnName":"Price","DisplayName":"قیمت","DataType":6,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":true,"SortOrder":1,"OptionSettings":null,"ImportDefinitionId":0},
{"ColumnName":"Comment","DisplayName":"کامنت","DataType":0,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":false,"SortOrder":2,"OptionSettings":null,"ImportDefinitionId":0},
{"ColumnName":"Description","DisplayName":"توضیحات","DataType":0,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":false,"SortOrder":3,"OptionSettings":null,"ImportDefinitionId":0},
{"ColumnName":"UpdatedDate","DisplayName":"تاریخ بروزرسانی","DataType":0,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":false,"SortOrder":4,"OptionSettings":null,"ImportDefinitionId":0},
{"ColumnName":"IsInternal","DisplayName":"داخلی","DataType":0,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":true,"SortOrder":5,"OptionSettings":null,"ImportDefinitionId":0},
{"ColumnName":"IsExternal","DisplayName":"خارجی","DataType":0,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":true,"SortOrder":6,"OptionSettings":null,"ImportDefinitionId":0}
]';

    IF ISJSON(@Columns) <> 1
        THROW 51620, N'Columns JSON معتبر نیست.', 1;

    DECLARE @DefId BIGINT;
    SELECT TOP (1) @DefId = Id
    FROM [System].[ImportDefinition]
    WHERE LOWER(FullNameEntity) = LOWER(@FullNameEntity)
    ORDER BY Id;

    IF @DefId IS NOT NULL
    BEGIN
        UPDATE [System].[ImportDefinition]
        SET FullNameEntity = @FullNameEntity,
            Title = @Title,
            Description = @Description,
            ImportType = 0,
            SqlQuery = @SqlQuery,
            ApiUrl = N'',
            ApiHttpMethod = N'POST',
            ApiCallMode = NULL,
            ApiIsInternal = 0,
            Columns = @Columns,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Id = @DefId;
        PRINT N'ImportDefinition updated: Id=' + CAST(@DefId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        INSERT INTO [System].[ImportDefinition]
        (
            FullNameEntity, Title, Description, ImportType, SqlQuery,
            ApiUrl, ApiHttpMethod, ApiCallMode, ApiIsInternal, Columns,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        VALUES
        (
            @FullNameEntity, @Title, @Description, 0, @SqlQuery,
            N'', N'POST', NULL, 0, @Columns,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @DefId = SCOPE_IDENTITY();
        PRINT N'ImportDefinition inserted: Id=' + CAST(@DefId AS NVARCHAR(20));
    END;

    DECLARE @ImportPath NVARCHAR(100) = N'importData_' + CAST(@DefId AS NVARCHAR(20));

    DECLARE @Rows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, EntityName NVARCHAR(200), RowId BIGINT NULL, RoleName NVARCHAR(200));
    INSERT INTO @Rows (Path, AccessType, ItemType, EntityName, RowId, RoleName) VALUES
        (@ImportPath,                 4, 7, @FullNameEntity, @DefId, N'Cng.PartPrice.Manage'),
        (@ImportPath,                 4, 7, @FullNameEntity, @DefId, N'ShowAllMenus'),
        (N'/panel/importdata/list',   1, 1, N'ImportData',    NULL,   N'Cng.PartPrice.Manage'),
        (N'/panel/importdata/list',   1, 1, N'ImportData',    NULL,   N'ShowAllMenus');

    INSERT INTO [system].[RoleAccess]
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId,
        CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        x.Path, x.AccessType, x.ItemType, x.EntityName, NULL,
        NULL, x.RowId, r.Id,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM @Rows x
    INNER JOIN [system].[Role] r ON r.Name = x.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM [system].[RoleAccess] ra
        WHERE ra.RoleId = r.Id AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType);

    PRINT N'RoleAccess rows added: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    SELECT DISTINCT x.RoleName AS MissingRole
    FROM @Rows x
    WHERE NOT EXISTS (SELECT 1 FROM [system].[Role] r WHERE r.Name = x.RoleName);

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_CngPartPrice_ImportDefinition ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT d.Id, d.FullNameEntity, d.Title, d.ImportType, d.IsActive
FROM [System].[ImportDefinition] d
WHERE LOWER(d.FullNameEntity) = N'entities.app.cng.partprice';
