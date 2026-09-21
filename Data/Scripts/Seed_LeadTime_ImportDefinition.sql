-- ============================================================================
-- WP6 / D6 — تعریف «ورود اطلاعات زمان در راه از اکسل» (System.ImportDefinition، حالت SQL)
--
-- معادل دکمه ImportExcel صفحه 414 HTS (LeadTimeController.AddLeadTimeByExcel):
--   ستون‌های اکسل HTS: PartCode | LeadTime | SupplierId (6 بازرگانی خارجی / 51 تامین و خرید) | Comment
--   اعتبارسنجی HTS: کد کالا خالی نباشد و بین کالاهای فعال باشد، LeadTime عدد باشد،
--                    SupplierId ∈ {6,51}، ردیف تکراری (کالا+تامین‌کننده+زمان) خطا.
--
-- رفتار در سیستم جدید (Q3: نگهداری در جدید):
--   * هر ردیف اکسل مستقل اجرا می‌شود (ImportExecutor، لاگ خطا به ازای ردیف؛ گزینه Rollback در UI)
--   * کلید یکتا (PartId, Supplier): اگر ردیفی برای همان کالا/تامین‌کننده وجود داشته باشد UPDATE می‌شود
--     (LeadTimeDay همیشه بازنویسی؛ NonRoutine/Comment فقط اگر در اکسل پر باشند)، وگرنه INSERT.
--     → دیگر ردیف تکراری تولید نمی‌شود (HTS ردیف تکراری را خطا می‌داد و بقیه را درج می‌کرد).
--   * ستون اختیاری جدید: NonRoutineLeadTime → Pln.LeadTime.NonRoutineLeadTimeInDay (D17)
--   * سرستون‌های اکسل: عنوان فارسی ستون‌ها (نمونه اکسل از همان صفحه دانلود می‌شود)؛ فایل‌های قدیمی
--     HTS با سرستون انگلیسی در مرحله «مپ کردن ستون‌ها» به‌صورت دستی نگاشت می‌شوند.
--
-- پارامترهای سیستمی ImportExecutor: @CurrentUserId, @CurrentUserName, @CurrentDateTimeMiladi,
--   @CurrentDateTimeShamsi (+ @CurrentDateMiladi, @CurrentDateShamsi)
--
-- دسترسی: RoleAccess (ActionAccessType=4 ImportData, Path='importData_<Id>') + صفحه /panel/importdata/list
--   برای نقش‌های SupplyAndPurchase و ShowAllMenus (HTS: فقط «کارشناسان زمان در راه» + FullAccess)
--
-- Idempotent: upsert بر اساس FullNameEntity = 'Entities.App.Pln.LeadTime'
-- پیش‌نیاز: ستون Pln.LeadTime.NonRoutineLeadTimeInDay
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH('Pln.LeadTime', 'NonRoutineLeadTimeInDay') IS NULL
BEGIN
    RAISERROR(N'ستون Pln.LeadTime.NonRoutineLeadTimeInDay وجود ندارد. ابتدا migration AddLeadTimeNonRoutineLeadTime یا Add_LeadTime_NonRoutineLeadTimeInDay.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-wp6-leadtime-import';

    DECLARE @FullNameEntity NVARCHAR(200) = N'Entities.App.Pln.LeadTime';   -- زیر کنترلر «زمان در راه» در صفحه نقش دیده می‌شود
    DECLARE @Title NVARCHAR(200) = N'ورود اطلاعات زمان در راه (اکسل)';
    DECLARE @Description NVARCHAR(500) = N'ورود/به‌روزرسانی زمان در راه کالاها از فایل اکسل — ستون‌ها: کد کالا*، زمان در راه (روز)*، زمان در راه غیرروتین (روز)، تامین کننده* (6 بازرگانی خارجی / 51 تامین و خرید)، کامنت. کلید: کالا + تامین کننده.';

    ---------------------------------------------------------------------------
    -- کوئری هر ردیف (پارامترها = ColumnName ستون‌ها)
    ---------------------------------------------------------------------------
    DECLARE @SqlQuery NVARCHAR(MAX) = N'SET NOCOUNT ON;

DECLARE @Code NVARCHAR(200) = LTRIM(RTRIM(CAST(@PartCode AS NVARCHAR(200))));
DECLARE @PartId BIGINT;
DECLARE @CommentTrim NVARCHAR(MAX) = NULLIF(LTRIM(RTRIM(CAST(@Comment AS NVARCHAR(MAX)))), N'''');
DECLARE @Msg NVARCHAR(400);

IF @Code IS NULL OR @Code = N''''
    THROW 51610, N''کد کالا خالی است.'', 1;

SELECT TOP (1) @PartId = p.Id
FROM Inv.Part p
WHERE p.Code = @Code
  AND ISNULL(p.IsActive, 1) = 1
ORDER BY p.Id;

IF @PartId IS NULL
BEGIN
    SET @Msg = N''کد کالا «'' + @Code + N''» در سیستم یافت نشد یا غیرفعال است.'';
    THROW 51611, @Msg, 1;
END;

IF @LeadTime IS NULL OR @LeadTime <= 0
    THROW 51612, N''زمان در راه (روز) باید عددی بزرگ‌تر از صفر باشد.'', 1;

IF @NonRoutineLeadTime IS NOT NULL AND @NonRoutineLeadTime <= 0
    THROW 51613, N''زمان در راه غیرروتین (روز) باید عددی بزرگ‌تر از صفر باشد.'', 1;

IF @SupplierId IS NULL OR @SupplierId NOT IN (6, 51)
    THROW 51614, N''تامین کننده فقط می‌تواند 6 (بازرگانی خارجی) یا 51 (تامین و خرید) باشد.'', 1;

IF EXISTS (SELECT 1 FROM Pln.LeadTime lt WHERE lt.PartId = @PartId AND lt.Supplier = @SupplierId)
BEGIN
    UPDATE Pln.LeadTime
    SET LeadTimeDay = @LeadTime,
        NonRoutineLeadTimeInDay = COALESCE(@NonRoutineLeadTime, NonRoutineLeadTimeInDay),
        Comment = COALESCE(@CommentTrim, Comment),
        ModifiedById = @CurrentUserId,
        ModifiedByName = @CurrentUserName,
        ModifiedDateMiladiDateTime = @CurrentDateTimeMiladi,
        ModifiedDateShamsiDateTime = @CurrentDateTimeShamsi,
        IsActive = 1
    WHERE PartId = @PartId AND Supplier = @SupplierId;
END
ELSE
BEGIN
    INSERT INTO Pln.LeadTime
        (PartId, LeadTimeDay, NonRoutineLeadTimeInDay, Supplier, Comment,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
         IsActive)
    VALUES
        (@PartId, @LeadTime, @NonRoutineLeadTime, @SupplierId, @CommentTrim,
         @CurrentUserId, @CurrentUserId, @CurrentUserName, @CurrentUserName,
         @CurrentDateTimeMiladi, @CurrentDateTimeShamsi, @CurrentDateTimeMiladi, @CurrentDateTimeShamsi,
         1);
END';

    ---------------------------------------------------------------------------
    -- ستون‌ها (ImportDefinitionColumn[]; DataType = Common.Attributes.SystemType)
    --   0 String · 7 Int · 8 Select (enum سیستمی LeadTimeSupplierEnum: 6/51 یا عنوان فارسی)
    ---------------------------------------------------------------------------
    DECLARE @Columns NVARCHAR(MAX) = N'[
{"ColumnName":"PartCode","DisplayName":"کد کالا","DataType":0,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":true,"SortOrder":0,"OptionSettings":null,"ImportDefinitionId":0},
{"ColumnName":"LeadTime","DisplayName":"زمان در راه (روز)","DataType":7,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":true,"SortOrder":1,"OptionSettings":null,"ImportDefinitionId":0},
{"ColumnName":"NonRoutineLeadTime","DisplayName":"زمان در راه غیرروتین (روز)","DataType":7,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":false,"SortOrder":2,"OptionSettings":null,"ImportDefinitionId":0},
{"ColumnName":"SupplierId","DisplayName":"تامین کننده","DataType":8,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":true,"SortOrder":3,"OptionSettings":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Pln.Enums.LeadTimeSupplierEnum"},"ImportDefinitionId":0},
{"ColumnName":"Comment","DisplayName":"کامنت","DataType":0,"IsSystemVariable":false,"SystemVariable":null,"IsRequired":false,"SortOrder":4,"OptionSettings":null,"ImportDefinitionId":0}
]';

    IF ISJSON(@Columns) <> 1
        THROW 51620, N'Columns JSON معتبر نیست.', 1;

    ---------------------------------------------------------------------------
    -- Upsert تعریف
    ---------------------------------------------------------------------------
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
            ImportType = 0,                 -- Sql
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

    ---------------------------------------------------------------------------
    -- RoleAccess: تعریف ورود (Type 4) + صفحه ورود اطلاعات (Type 1 View / List)
    ---------------------------------------------------------------------------
    DECLARE @ImportPath NVARCHAR(100) = N'importData_' + CAST(@DefId AS NVARCHAR(20));

    DECLARE @Rows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, EntityName NVARCHAR(200), RowId BIGINT NULL, RoleName NVARCHAR(200));
    INSERT INTO @Rows (Path, AccessType, ItemType, EntityName, RowId, RoleName) VALUES
        (@ImportPath,                 4, 7, @FullNameEntity, @DefId, N'SupplyAndPurchase'),
        (@ImportPath,                 4, 7, @FullNameEntity, @DefId, N'ShowAllMenus'),
        (N'/panel/importdata/list',   1, 1, N'ImportData',    NULL,   N'SupplyAndPurchase'),
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
    PRINT N'=== DONE Seed_LeadTime_ImportDefinition ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

-- بازبینی
SELECT d.Id, d.FullNameEntity, d.Title, d.ImportType, d.IsActive, LEN(d.SqlQuery) AS SqlLen, LEN(d.Columns) AS ColsLen
FROM [System].[ImportDefinition] d
WHERE LOWER(d.FullNameEntity) = N'entities.app.pln.leadtime';

SELECT ra.Path, ra.ActionAccessType, ra.ActionAccessItemType, ra.EntityName, ra.RowId, r.Name AS RoleName
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/importdata/list'
   OR (ra.ActionAccessType = 4 AND ra.EntityName = N'Entities.App.Pln.LeadTime')
ORDER BY ra.Path, r.Name;
