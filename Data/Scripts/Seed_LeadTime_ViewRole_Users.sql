-- ============================================================================
-- زمان در راه (HTS صفحه 414 / Supp_LeadTime)
--
-- واقعیت دیتابیس TotalSystem (نه عنوان گروه):
--   گروه 291 «کارشناسان زمان در راه» و گروه 524 «کارشناسان مشاهده زمان در راه»
--   هر دو فقط PageAction مشاهده (Permission Read = 3) دارند.
--   اکشن‌های جدید / ویرایش / حذف (PageAction 2488/2489/2490) به هیچ گروه یا کاربری
--   داده نشده‌اند؛ سرور HTS همان‌ها را رد می‌کند.
--   ورود از اکسل (DoExcelOperation) فقط وجود دسترسی صفحه را چک می‌کند، پس با Read مجاز است.
--   خروجی اکسل گرید در ویوی HTS برای همین صفحه روشن است.
--
-- این اسکریپت برای پنج کاربر درخواستی نقش مشاهده می‌سازد:
--   لیست، دریافت داده، خروجی اکسل، نمایه فقط‌خواندنی، ورود اکسل (importData_11).
--   جدید / ذخیره / ویرایش / حذف داده نمی‌شود.
--
-- Idempotent. بعد از اجرا: ری‌استارت WebApp و ورود مجدد کاربران.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-leadtime-view-users';
    DECLARE @RoleId BIGINT = 200073;
    DECLARE @RoleName NVARCHAR(200) = N'Pln.LeadTime.View';
    DECLARE @RoleTitle NVARCHAR(300) = N'برنامه ریزی - زمان در راه - مشاهده';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Pln.LeadTime';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.pln.leadtime';
    DECLARE @ProfileName NVARCHAR(200) = N'leadtime_view';
    DECLARE @ProfileTitle NVARCHAR(200) = N'زمان در راه';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    ---------------------------------------------------------------------------
    -- نقش
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @RoleName)
    BEGIN
        IF EXISTS (SELECT 1 FROM [system].[Role] WHERE Id = @RoleId)
            THROW 51620, N'شناسه نقش 200073 قبلاً استفاده شده است.', 1;

        SET IDENTITY_INSERT [system].[Role] ON;
        INSERT INTO [system].[Role]
        (
            Id, Name, Title,
            CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
            CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        VALUES
        (
            @RoleId, @RoleName, @RoleTitle,
            1, @SeedUser, 1, @SeedUser,
            @Now, @Now,
            @NowShamsi, @NowShamsi,
            1
        );
        SET IDENTITY_INSERT [system].[Role] OFF;
    END
    ELSE
        SELECT @RoleId = Id FROM [system].[Role] WHERE Name = @RoleName;

    ---------------------------------------------------------------------------
    -- نمایه فقط‌مشاهده (کپی ستون‌های leadtime_listinfo، بدون دکمه‌های نوشتن)
    -- دکمه «ورود از اکسل» حفظ می‌شود چون در HTS با دسترسی مشاهده مجاز است.
    ---------------------------------------------------------------------------
    DECLARE @SourceProfileId BIGINT =
    (
        SELECT TOP (1) Id
        FROM [system].[SavedQuery]
        WHERE Name = N'leadtime_listinfo'
        ORDER BY Id
    );

    IF @SourceProfileId IS NULL
        THROW 51621, N'نمایه leadtime_listinfo پیدا نشد.', 1;

    DECLARE @ProfileId BIGINT =
    (
        SELECT TOP (1) Id
        FROM [system].[SavedQuery]
        WHERE Name = @ProfileName
        ORDER BY Id
    );

    IF @ProfileId IS NULL
    BEGIN
        INSERT INTO [system].[SavedQuery]
        (
            Name, Title, QueryJson, ColumnsJson, DiagramJson,
            CustomActionButtonsJson, EventScriptsJson,
            Mode, [Type], EntityFullName, ActionOptions,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
            ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        SELECT
            @ProfileName, @ProfileTitle, QueryJson, ColumnsJson, DiagramJson,
            CustomActionButtonsJson, EventScriptsJson,
            Mode, [Type], @EntityFullName, @ActionOptions,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        FROM [system].[SavedQuery]
        WHERE Id = @SourceProfileId;

        SET @ProfileId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE v
        SET
            Title = @ProfileTitle,
            QueryJson = s.QueryJson,
            ColumnsJson = s.ColumnsJson,
            CustomActionButtonsJson = s.CustomActionButtonsJson,
            EventScriptsJson = s.EventScriptsJson,
            EntityFullName = @EntityFullName,
            ActionOptions = @ActionOptions,
            Mode = s.Mode,
            [Type] = s.[Type],
            IsActive = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        FROM [system].[SavedQuery] v
        CROSS JOIN [system].[SavedQuery] s
        WHERE v.Id = @ProfileId
          AND s.Id = @SourceProfileId;
    END

    ---------------------------------------------------------------------------
    -- RoleAccess کنترلر + نمایه + ورود اکسل
    ---------------------------------------------------------------------------
    DECLARE @Access TABLE
    (
        Path NVARCHAR(300) NOT NULL,
        ActionAccessType INT NOT NULL,
        ActionAccessItemType INT NOT NULL,
        EntityName NVARCHAR(200) NULL,
        RowId BIGINT NULL
    );

    INSERT INTO @Access (Path, ActionAccessType, ActionAccessItemType, EntityName, RowId)
    VALUES
        (N'/panel/pln/leadtime/list',           1, 1, @EntityPascal, NULL),
        (N'/panel/pln/leadtime/fetchdata',      2, 2, @EntityPascal, NULL),
        (N'/panel/pln/leadtime/exporttoexcel',  2, 0, @EntityPascal, NULL),
        (N'/panel/importdata/list',             1, 1, @EntityPascal, NULL),
        (N'importData_11',                      4, 7, @EntityPascal, 11),
        (N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)), 3, 7, @EntityPascal, @ProfileId);

    INSERT INTO [system].[RoleAccess]
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId,
        CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, NULL,
        NULL, a.RowId, @RoleId,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM @Access a
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [system].[RoleAccess] x
        WHERE x.RoleId = @RoleId
          AND x.Path = a.Path
          AND x.ActionAccessType = a.ActionAccessType
    );

    ---------------------------------------------------------------------------
    -- منوی «همه منوها» تا آیتم زمان در راه برای این نقش دیده شود
    ---------------------------------------------------------------------------
    IF NOT EXISTS
    (
        SELECT 1
        FROM [system].[SystemMenu] m
        CROSS APPLY OPENJSON(m.AccessRoleIds) j
        WHERE m.Id = 13
          AND TRY_CAST(j.[value] AS BIGINT) = @RoleId
    )
    BEGIN
        UPDATE [system].[SystemMenu]
        SET
            AccessRoleIds = JSON_MODIFY(AccessRoleIds, 'append $', @RoleId),
            AccessRoles = JSON_MODIFY(AccessRoles, 'append $', @RoleName),
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = 13;
    END

    ---------------------------------------------------------------------------
    -- عضویت پنج کاربر
    -- HTS: 291 مشاهده = احمدی، صائمیان، آزادبخش شیما، آزادبخش سپیده
    --      524 مشاهده = کیان
    ---------------------------------------------------------------------------
    DECLARE @Users TABLE (Username NVARCHAR(200) NOT NULL PRIMARY KEY);
    INSERT INTO @Users (Username) VALUES
        (N'ahmadi.sh'),
        (N'saemian.r'),
        (N'kian.r'),
        (N'azadbakhsh.sh'),
        (N'azadbakhsh.s');

    UPDATE u
    SET
        RoleIds = CASE
            WHEN u.RoleIds IS NULL OR LTRIM(RTRIM(u.RoleIds)) IN (N'', N'[]')
                THEN N'[' + CAST(@RoleId AS NVARCHAR(20)) + N']'
            ELSE STUFF(u.RoleIds, LEN(u.RoleIds), 1, N',' + CAST(@RoleId AS NVARCHAR(20)) + N']')
        END,
        Roles = CASE
            WHEN u.Roles IS NULL OR LTRIM(RTRIM(u.Roles)) IN (N'', N'[]')
                THEN N'["' + @RoleName + N'"]'
            ELSE STUFF(u.Roles, LEN(u.Roles), 1, N',"' + @RoleName + N'"]')
        END,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    FROM [system].[User] u
    INNER JOIN @Users x ON LOWER(u.Username) = x.Username
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
        WHERE TRY_CAST(j.[value] AS BIGINT) = @RoleId
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SET IDENTITY_INSERT [system].[Role] OFF;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    THROW 51622, @Err, 1;
END CATCH;

SELECT r.Id, r.Name, r.Title
FROM [system].[Role] r
WHERE r.Name = N'Pln.LeadTime.View';

SELECT q.Id, q.Name, q.Title,
       JSON_VALUE(q.ActionOptions, '$[0].enable') AS NewEnabled,
       JSON_VALUE(q.ActionOptions, '$[3].enable') AS ExcelEnabled
FROM [system].[SavedQuery] q
WHERE q.Name = N'leadtime_view';

SELECT ra.Path, ra.ActionAccessType, ra.ActionAccessItemType, ra.RowId
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE r.Name = N'Pln.LeadTime.View'
ORDER BY ra.Path;

SELECT u.Id, u.Username, u.NameFa
FROM [system].[User] u
CROSS APPLY OPENJSON(u.RoleIds) j
INNER JOIN [system].[Role] r ON r.Id = TRY_CAST(j.[value] AS BIGINT)
WHERE r.Name = N'Pln.LeadTime.View'
  AND LOWER(u.Username) IN (N'ahmadi.sh', N'saemian.r', N'kian.r', N'azadbakhsh.sh', N'azadbakhsh.s')
ORDER BY u.Username;
