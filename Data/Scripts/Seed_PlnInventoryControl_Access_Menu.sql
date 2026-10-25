/*
  Seed_PlnInventoryControl_Access_Menu.sql
  دسترسی و منوی «کنترل موجودی» (برنامه‌ریزی) — معادل HTS صفحه Pln_InventoryControl (Page_ID 409).
  HTS: Read(3) = همه ستون‌ها؛ Pln_OtherUnitsPermission(121) = نمای محدود (سایر واحدها).
  Groups 282, 350 (Read) و 307 (121) -> role Id = 260000 + HtsGroupId
  Direct users (Gnr_PageAction_User)  -> Pln.InventoryControl.Direct / Pln.InventoryControl.OtherUnits.Direct
  Idempotent. Requires [TMS] and Seed_PlnInventoryControl_DataProfile.sql applied first.
  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -x -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-pln-inventorycontrol';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/inv/partcardex/inventorycontrol';
    DECLARE @EntityPascal NVARCHAR(300) = N'Entities.App.Inv.PartCardex';
    DECLARE @PageTitle NVARCHAR(200) = N'کنترل موجودی';

    DECLARE @FullId BIGINT = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Pln_InventoryControl_Report');
    DECLARE @OtherId BIGINT = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Pln_InventoryControl_OtherUnits_Report');
    IF @FullId IS NULL OR @OtherId IS NULL
        THROW 51770, N'نمایه‌های کنترل موجودی پیدا نشد؛ ابتدا Seed_PlnInventoryControl_DataProfile.sql را اجرا کنید.', 1;

    ---------------------------------------------------------------------------
    PRINT N'=== [1] Roles ===';
    ---------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Grp') IS NOT NULL DROP TABLE #Grp;
    CREATE TABLE #Grp (HtsGroupId INT NOT NULL PRIMARY KEY, RoleId BIGINT NOT NULL, RoleName NVARCHAR(200) NOT NULL, IsOther BIT NOT NULL);
    INSERT INTO #Grp (HtsGroupId, RoleId, RoleName, IsOther)
    SELECT g.UserGroup_ID, 260000 + g.UserGroup_ID, g.UserGroup_Title, CASE WHEN g.UserGroup_ID = 307 THEN 1 ELSE 0 END
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroup] g
    WHERE g.UserGroup_ID IN (282, 350, 307);
    IF (SELECT COUNT(*) FROM #Grp) <> 3 THROW 51771, N'همه ۳ گروه HTS پیدا نشد.', 1;

    UPDATE g SET RoleName = r.Name FROM #Grp g JOIN [system].[Role] r ON r.Id = g.RoleId;

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @UseName NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT RoleId, RoleName FROM #Grp;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Id = @Rid)
        BEGIN
            SET @UseName = @RName;
            IF EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @UseName)
                SET @UseName = @RName + N' (' + CAST(@Rid AS NVARCHAR(20)) + N')';
            SET IDENTITY_INSERT [system].[Role] ON;
            INSERT INTO [system].[Role] (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@Rid, @UseName, @RName, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT [system].[Role] OFF;
            UPDATE #Grp SET RoleName = @UseName WHERE RoleId = @Rid;
            PRINT N'  CREATED ' + @UseName;
        END
        FETCH NEXT FROM rc INTO @Rid, @RName;
    END
    CLOSE rc; DEALLOCATE rc;

    DECLARE @DirectName NVARCHAR(200) = N'Pln.InventoryControl.Direct';
    DECLARE @DirectOtherName NVARCHAR(200) = N'Pln.InventoryControl.OtherUnits.Direct';
    DECLARE @DirectId BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = @DirectName);
    DECLARE @DirectOtherId BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = @DirectOtherName);
    IF @DirectId IS NULL
    BEGIN
        INSERT INTO [system].[Role] (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@DirectName, N'دسترسی مستقیم کنترل موجودی', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET @DirectId = SCOPE_IDENTITY();
    END
    IF @DirectOtherId IS NULL
    BEGIN
        INSERT INTO [system].[Role] (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@DirectOtherName, N'دسترسی مستقیم کنترل موجودی (سایر واحدها)', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET @DirectOtherId = SCOPE_IDENTITY();
    END

    -- RoleId -> which profile it may use
    IF OBJECT_ID('tempdb..#Access') IS NOT NULL DROP TABLE #Access;
    CREATE TABLE #Access (RoleId BIGINT NOT NULL, ProfileId BIGINT NOT NULL);
    INSERT INTO #Access (RoleId, ProfileId)
    SELECT RoleId, CASE WHEN IsOther = 1 THEN @OtherId ELSE @FullId END FROM #Grp
    UNION ALL SELECT @DirectId, @FullId
    UNION ALL SELECT @DirectOtherId, @OtherId
    UNION ALL SELECT r.Id, @FullId FROM [system].[Role] r WHERE r.Id = 200052;   -- ShowAllMenus (project convention)

    ---------------------------------------------------------------------------
    PRINT N'=== [2] RoleAccess (page + data profile) ===';
    ---------------------------------------------------------------------------
    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @EntityPascal, @PageTitle, NULL, NULL, a.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (SELECT DISTINCT RoleId FROM #Access) a
    WHERE NOT EXISTS (SELECT 1 FROM [system].[RoleAccess] x WHERE x.RoleId = a.RoleId AND x.Path = @PagePath AND x.ActionAccessType = 1);
    PRINT N'  page grants: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(a.ProfileId AS NVARCHAR(20)), 3, 7, @EntityPascal, @PageTitle, NULL, a.ProfileId, a.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #Access a
    WHERE NOT EXISTS (SELECT 1 FROM [system].[RoleAccess] x WHERE x.RoleId = a.RoleId AND x.ActionAccessType = 3 AND x.RowId = a.ProfileId);
    PRINT N'  profile grants: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    ---------------------------------------------------------------------------
    PRINT N'=== [3] Assign users ===';
    ---------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Map') IS NOT NULL DROP TABLE #Map;
    CREATE TABLE #Map (Username NVARCHAR(200) NOT NULL, RoleId BIGINT NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #Map (Username, RoleId, RoleName)
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), g.RoleId, g.RoleName
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    JOIN #Grp g ON g.HtsGroupId = m.UserGroup_FK
    WHERE u.IsActive = 1;

    INSERT INTO #Map (Username, RoleId, RoleName)
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
           CASE WHEN pa.Permission_FK = 121 THEN @DirectOtherId ELSE @DirectId END,
           CASE WHEN pa.Permission_FK = 121 THEN @DirectOtherName ELSE @DirectName END
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pu
    JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pu.PageAction_FK
    JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pu.User_FK
    WHERE pa.Page_FK = 409 AND pa.Permission_FK IN (3, 121) AND u.IsActive = 1;

    INSERT INTO #Map (Username, RoleId, RoleName)
    SELECT DISTINCT u.Username, m.RoleId, m.RoleName
    FROM #Map m
    JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON LOWER(u.ActiveDirectoryUsername) = LOWER(m.Username)
    WHERE u.Username IS NOT NULL AND LOWER(u.Username) <> LOWER(m.Username)
      AND NOT EXISTS (SELECT 1 FROM #Map x WHERE LOWER(x.Username) = LOWER(u.Username) AND x.RoleId = m.RoleId);
    DELETE FROM #Map WHERE Username IS NULL;

    DECLARE @U NVARCHAR(200), @MapRoleId BIGINT, @MapRoleName NVARCHAR(200), @Uid BIGINT;
    DECLARE @Assigned INT = 0, @Missing INT = 0, @Already INT = 0;
    DECLARE mc CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleId, RoleName FROM #Map;
    OPEN mc; FETCH NEXT FROM mc INTO @U, @MapRoleId, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Uid = NULL;
        SELECT TOP 1 @Uid = Id FROM [system].[User] WHERE LOWER(Username) = LOWER(@U);
        IF @Uid IS NULL
        BEGIN
            PRINT N'  MISSING USER: ' + @U + N' (role ' + @MapRoleName + N')';
            SET @Missing += 1;
        END
        ELSE IF EXISTS (SELECT 1 FROM [system].[User] x CROSS APPLY OPENJSON(ISNULL(x.RoleIds, N'[]')) j
                        WHERE x.Id = @Uid AND TRY_CAST(j.value AS BIGINT) = @MapRoleId)
            SET @Already += 1;
        ELSE
        BEGIN
            UPDATE [system].[User]
            SET RoleIds = CASE WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]')
                               THEN N'[' + CAST(@MapRoleId AS NVARCHAR(20)) + N']'
                               ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS NVARCHAR(20)) + N']') END,
                Roles = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                             WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]')
                               THEN N'["' + REPLACE(@MapRoleName, N'"', N'') + N'"]'
                             ELSE STUFF(Roles, LEN(Roles), 1, N',"' + REPLACE(@MapRoleName, N'"', N'') + N'"]') END,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @Uid;
            SET @Assigned += 1;
        END
        FETCH NEXT FROM mc INTO @U, @MapRoleId, @MapRoleName;
    END
    CLOSE mc; DEALLOCATE mc;
    PRINT N'  assigned=' + CAST(@Assigned AS NVARCHAR(20)) + N' already=' + CAST(@Already AS NVARCHAR(20)) + N' missingUsers=' + CAST(@Missing AS NVARCHAR(20));

    -- users that would see BOTH profiles (HTS showed them only the restricted view)
    SELECT N'users with both full and restricted roles' AS Info, COUNT(*) AS C
    FROM #Map a JOIN #Map b ON LOWER(a.Username) = LOWER(b.Username)
    WHERE a.RoleId IN (SELECT RoleId FROM #Access WHERE ProfileId = @FullId)
      AND b.RoleId IN (SELECT RoleId FROM #Access WHERE ProfileId = @OtherId);

    ---------------------------------------------------------------------------
    PRINT N'=== [4] Menu leaf: سیستم برنامه ریزی > عملیات / گزارشات > کنترل موجودی (AllMenus) ===';
    ---------------------------------------------------------------------------
    DECLARE @MenuId BIGINT, @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content FROM [system].[SystemMenu] WHERE Name = N'AllMenus' ORDER BY Id;
    IF @MenuId IS NULL THROW 51772, N'منوی AllMenus یافت نشد.', 1;

    IF CHARINDEX(@PagePath, @Content) > 0
        PRINT N'  menu leaf already present';
    ELSE
    BEGIN
        DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"کنترل موجودی","icon":"ki-chart-simple ki-outline","iconColor":"#F9A8D4","path":"/panel/inv/partcardex/inventorycontrol","a_attr":{"href":"/panel/inv/partcardex/inventorycontrol"},"data":{"iconColor":"#F9A8D4","path":"/panel/inv/partcardex/inventorycontrol"},"children":[]}';
        DECLARE @Target NVARCHAR(4000) = NULL;
        ;WITH walk AS (
            SELECT CAST(N'$[' + j.[key] + N']' AS NVARCHAR(4000)) AS p, j.[value] AS v
            FROM OPENJSON(@Content) AS j
            UNION ALL
            SELECT CAST(w.p + N'.children[' + c.[key] + N']' AS NVARCHAR(4000)), c.[value]
            FROM walk AS w CROSS APPLY OPENJSON(w.v, N'$.children') AS c
        )
        SELECT TOP (1) @Target = p + N'.children'
        FROM walk
        WHERE EXISTS (SELECT 1 FROM OPENJSON(v, N'$.children') AS k
                      WHERE JSON_VALUE(k.[value], N'$.path') = N'/panel/productionorderitem/list');
        IF @Target IS NULL THROW 51773, N'شاخه سیستم برنامه ریزی > عملیات / گزارشات در منو پیدا نشد.', 1;

        SET @Content = JSON_MODIFY(@Content, N'append ' + @Target, JSON_QUERY(@Leaf));
        IF @Content IS NULL OR ISJSON(@Content) <> 1 THROW 51774, N'Content منو بعد از درج معتبر نیست.', 1;

        UPDATE [system].[SystemMenu]
        SET Content = @Content, ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
        PRINT N'  menu added at ' + @Target;
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT r.Id, r.Name, (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id
       AND (ra.Path = N'/panel/inv/partcardex/inventorycontrol'
            OR ra.RowId IN (SELECT Id FROM [system].[SavedQuery] WHERE Name LIKE N'Pln_InventoryControl%'))) AS Grants
FROM [system].[Role] r
WHERE r.Id IN (260282, 260307, 260350, 200052) OR r.Name LIKE N'Pln.InventoryControl%';
