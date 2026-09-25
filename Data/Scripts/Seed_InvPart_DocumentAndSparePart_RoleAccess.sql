/*
Seed_InvPart_DocumentAndSparePart_RoleAccess.sql
دسترسی مدارک و قطعات یدکی کالا مطابق HTS:

مدارک — صفحه 263 (پیوست مدارک کالا):
  79  کارشناسان واحد مهندسی                         مشاهده + جدید
  238 کارشناسان پیوست مدارک کالا - (غیر از حذف)   مشاهده + جدید + ویرایش
  265 کارشناسان مشاهده پیوست مدارک کالا - انبار   فقط مشاهده
  305 کارشناسان افزودن خوشه‌ای پیوست کالا         مشاهده + جدید
  350 مشاهده کلی HTS                              فقط مشاهده
  اعطای مستقیم FullAccess (کاربران فعال)         حذف
  اعطای مستقیم Read کاربر HavayarControl         فقط مشاهده

قطعات یدکی — صفحه 16 (داخل فرم کالا؛ ویرایش یا ویرایش محدود):
  16  مدیر سیستم انبار                             مشاهده + ثبت/ویرایش/حذف
  58  دسترسی مشاهده-سیستم انبار                    فقط مشاهده
  79  ویرایش محدود                                 مشاهده + ثبت/ویرایش/حذف
  238 ویرایش محدود                                 مشاهده + ثبت/ویرایش/حذف
  265 ویرایش محدود                                 مشاهده + ثبت/ویرایش/حذف
  305 فقط باز کردن صفحه                           فقط مشاهده
  350 مشاهده کلی                                   فقط مشاهده
  488 دسترسی عمومی مهندسی                          فقط مشاهده
  اعطای مستقیم Read کاربر Yousefi.z              فقط مشاهده

ShowAllMenus همه عملیات را می‌گیرد.
UTF-8 BOM. Idempotent. Requires [TMS].
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-inv-part-children';

    PRINT N'=== [1] Roles ===';
    IF OBJECT_ID('tempdb..#Roles') IS NOT NULL DROP TABLE #Roles;
    CREATE TABLE #Roles (RoleId BIGINT NOT NULL PRIMARY KEY, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #Roles (RoleId, Name, Title) VALUES
        (260016, N'مدیر سیستم انبار', N'مدیر سیستم انبار'),
        (260058, N'دسترسی مشاهده-سیستم انبار', N'دسترسی مشاهده-سیستم انبار'),
        (260079, N'کارشناسان واحد مهندسی', N'کارشناسان واحد مهندسی'),
        (260238, N'کارشناسان پیوست مدارک کالا - (غیر از حذف)', N'کارشناسان پیوست مدارک کالا - (غیر از حذف)'),
        (260265, N'کارشناسان مشاهده پیوست مدارک کالا - انبار', N'کارشناسان مشاهده پیوست مدارک کالا - انبار'),
        (260305, N'کارشناسان افزودن خوشه ای پیوست کالا', N'کارشناسان افزودن خوشه ای پیوست کالا'),
        (260350, N'مشاهده کلی HTS', N'مشاهده کلی HTS'),
        (260488, N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی', N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی'),
        (269030, N'اعطا مستقیم - حذف پیوست مدارک کالا', N'اعطا مستقیم - حذف پیوست مدارک کالا'),
        (269031, N'اعطا مستقیم - مشاهده پیوست مدارک کالا', N'اعطا مستقیم - مشاهده پیوست مدارک کالا'),
        (269033, N'اعطا مستقیم - مشاهده قطعات یدکی کالا', N'اعطا مستقیم - مشاهده قطعات یدکی کالا');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200), @UseName NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT RoleId, Name, Title FROM #Roles;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @Rid)
            PRINT N'  EXISTS ' + CAST(@Rid AS nvarchar(20));
        ELSE
        BEGIN
            SET @UseName = @RName;
            IF EXISTS (SELECT 1 FROM system.Role WHERE Name = @UseName)
                SET @UseName = @RName + N' (' + CAST(@Rid AS nvarchar(20)) + N')';
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@Rid, @UseName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
            PRINT N'  CREATED ' + @UseName;
        END
        FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    END
    CLOSE rc; DEALLOCATE rc;

    PRINT N'=== [2] RoleAccess ===';
    IF OBJECT_ID('tempdb..#Map') IS NOT NULL DROP TABLE #Map;
    CREATE TABLE #Map (
        RoleId BIGINT NOT NULL,
        Path NVARCHAR(300) NOT NULL,
        AType INT NOT NULL,
        IType INT NOT NULL,
        EntityName NVARCHAR(200) NOT NULL
    );

    DECLARE @Doc NVARCHAR(200) = N'Entities.App.Inv.PartDocument';
    DECLARE @Spare NVARCHAR(200) = N'Entities.App.Inv.PartSparePart';

    -- Document view
    INSERT INTO #Map (RoleId, Path, AType, IType, EntityName)
    SELECT r.RoleId, a.Path, a.AType, a.IType, @Doc
    FROM (VALUES
        (N'/panel/inv/partdocument/list', 1, 1),
        (N'/panel/inv/partdocument/fetchdata', 2, 2),
        (N'/panel/inv/partdocument/listbyparentid', 1, 1),
        (N'/panel/inv/partdocument/getlistbyparentid', 2, 2),
        (N'/panel/inv/partdocument/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (260079),(260238),(260265),(260305),(260350),(269031),(200052)
    ) r(RoleId);

    -- Document new (79, 238, 305, ShowAll)
    INSERT INTO #Map (RoleId, Path, AType, IType, EntityName)
    SELECT r.RoleId, a.Path, a.AType, a.IType, @Doc
    FROM (VALUES
        (N'/panel/inv/partdocument/new', 1, 4),
        (N'/panel/inv/partdocument/add', 2, 4)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (260079),(260238),(260305),(200052)) r(RoleId);

    -- Document edit (238, ShowAll) — HTS page 263 permission Edit, not group 79
    INSERT INTO #Map (RoleId, Path, AType, IType, EntityName)
    SELECT r.RoleId, a.Path, a.AType, a.IType, @Doc
    FROM (VALUES
        (N'/panel/inv/partdocument/edit', 1, 5),
        (N'/panel/inv/partdocument/update', 2, 5),
        (N'/panel/inv/partdocument/save', 2, 3)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (260238),(200052)) r(RoleId);

    -- Document delete: direct FullAccess + ShowAll. No HTS group has Delete on page 263.
    INSERT INTO #Map (RoleId, Path, AType, IType, EntityName)
    SELECT r.RoleId, a.Path, a.AType, a.IType, @Doc
    FROM (VALUES (N'/panel/inv/partdocument/delete', 2, 6)) a(Path, AType, IType)
    CROSS JOIN (VALUES (269030),(200052)) r(RoleId);

    -- Spare view
    INSERT INTO #Map (RoleId, Path, AType, IType, EntityName)
    SELECT r.RoleId, a.Path, a.AType, a.IType, @Spare
    FROM (VALUES
        (N'/panel/inv/partsparepart/list', 1, 1),
        (N'/panel/inv/partsparepart/fetchdata', 2, 2),
        (N'/panel/inv/partsparepart/listbyparentid', 1, 1),
        (N'/panel/inv/partsparepart/getlistbyparentid', 2, 2),
        (N'/panel/inv/partsparepart/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (260016),(260058),(260079),(260238),(260265),(260305),(260350),(260488),(269033),(200052)
    ) r(RoleId);

    -- Spare write: Edit or LimitedEdit on HTS page 16
    INSERT INTO #Map (RoleId, Path, AType, IType, EntityName)
    SELECT r.RoleId, a.Path, a.AType, a.IType, @Spare
    FROM (VALUES
        (N'/panel/inv/partsparepart/new', 1, 4),
        (N'/panel/inv/partsparepart/add', 2, 4),
        (N'/panel/inv/partsparepart/edit', 1, 5),
        (N'/panel/inv/partsparepart/update', 2, 5),
        (N'/panel/inv/partsparepart/save', 2, 3),
        (N'/panel/inv/partsparepart/delete', 2, 6)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (260016),(260079),(260238),(260265),(200052)) r(RoleId);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT m.Path, m.AType, m.IType, m.EntityName, NULL, NULL, NULL, m.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #Map m
    WHERE EXISTS (SELECT 1 FROM system.Role r WHERE r.Id = m.RoleId)
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = m.RoleId AND x.Path = m.Path AND x.ActionAccessType = m.AType AND x.ActionAccessItemType = m.IType
      );
    PRINT N'  Inserted RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [3] Assign HTS group members ===';
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleId BIGINT NOT NULL);

    INSERT INTO #UserRoleMap (Username, RoleId)
    SELECT DISTINCT u.Username, g.RoleId
    FROM (VALUES
        (16, 260016),
        (58, 260058),
        (79, 260079),
        (238, 260238),
        (265, 260265),
        (305, 260305),
        (350, 260350),
        (488, 260488)
    ) g(HtsGroupId, RoleId)
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = g.HtsGroupId
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    WHERE u.IsActive = 1
      AND NULLIF(LTRIM(RTRIM(u.Username)), N'') IS NOT NULL;

    INSERT INTO #UserRoleMap (Username, RoleId)
    SELECT DISTINCT u.ActiveDirectoryUsername, src.RoleId
    FROM #UserRoleMap src
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.Username = src.Username
    WHERE u.IsActive = 1
      AND NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N'') IS NOT NULL
      AND LOWER(u.ActiveDirectoryUsername) <> LOWER(src.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE x.RoleId = src.RoleId AND LOWER(x.Username) = LOWER(u.ActiveDirectoryUsername)
      );

    INSERT INTO #UserRoleMap (Username, RoleId)
    SELECT v.Username, v.RoleId
    FROM (VALUES
        (N'abdolmaleki.h', 269030),
        (N'kadkhodaei.v', 269030),
        (N'khodakarami.f', 269030),
        (N'HavayarControl', 269031),
        (N'Yousefi.z', 269033)
    ) v(Username, RoleId)
    WHERE NOT EXISTS (
        SELECT 1 FROM #UserRoleMap x
        WHERE x.RoleId = v.RoleId AND LOWER(x.Username) = LOWER(v.Username)
    );

    ;WITH d AS (
        SELECT Username, RoleId,
               ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleId ORDER BY Username) AS rn
        FROM #UserRoleMap
    )
    DELETE FROM d WHERE rn > 1;

    DECLARE @MapUsername nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0, @BadJson int = 0;
    DECLARE @RoleIds nvarchar(max), @Roles nvarchar(max), @RoleName nvarchar(200);

    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleId FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MapUserId = NULL;
        SET @RoleIds = NULL;
        SET @Roles = NULL;
        SET @RoleName = NULL;

        SELECT TOP 1 @MapUserId = Id, @RoleIds = RoleIds, @Roles = Roles
        FROM system.[User]
        WHERE LOWER(Username) = LOWER(@MapUsername);

        SELECT @RoleName = Name FROM system.Role WHERE Id = @MapRoleId;

        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' role ' + CAST(@MapRoleId AS nvarchar(20));
            SET @Missing = @Missing + 1;
        END
        ELSE IF @RoleIds IS NOT NULL AND LTRIM(RTRIM(@RoleIds)) NOT IN (N'', N'[]') AND ISJSON(@RoleIds) <> 1
        BEGIN
            PRINT N'  BAD RoleIds JSON (skip): ' + @MapUsername;
            SET @BadJson = @BadJson + 1;
        END
        ELSE IF ISJSON(ISNULL(@RoleIds, N'[]')) = 1
             AND EXISTS (
                SELECT 1 FROM system.[User] u
                CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
             )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]')
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN @RoleName IS NULL THEN Roles
                    WHEN ISJSON(ISNULL(Roles, N'[]')) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @RoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]') OR ISJSON(ISNULL(Roles, N'[]')) <> 1
                        THEN N'["' + REPLACE(@RoleName, N'"', N'') + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + REPLACE(@RoleName, N'"', N'') + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END

        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleId;
    END
    CLOSE map_cur; DEALLOCATE map_cur;

    PRINT N'  Assigned: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already had role: ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing users: ' + CAST(@Missing AS nvarchar(20));
    PRINT N'  Bad JSON: ' + CAST(@BadJson AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT r.Id, r.Name, COUNT(ra.Id) AS AccessRows
FROM system.Role r
LEFT JOIN system.RoleAccess ra ON ra.RoleId = r.Id AND ra.Path LIKE N'/panel/inv/part%' AND ra.CreatedByName = N'seed-inv-part-children'
WHERE r.Id IN (260016,260058,260079,260238,260265,260305,260350,260488,269030,269031,269033,200052)
GROUP BY r.Id, r.Name
ORDER BY r.Id;
