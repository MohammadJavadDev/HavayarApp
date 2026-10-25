/*
  Seed_PartCardex_Access_Menu.sql
  دسترسی و منوی «کاردکس کالا» — معادل HTS صفحه Inv_Part_Cardex (Page_ID 105، سیستم انبار).
  HTS permissions: NoAccess(1) FullAccess(2) Read(3) ShowPrice(8) ExportToExcel(10).
    - صفحه فقط نمایش + خروجی اکسل دارد؛ ShowPrice در کد صفحه استفاده نمی‌شود (منتقل نمی‌شود).
    - هر کسی که صفحه را می‌بیند خروجی اکسل هم دارد (مثل HTS).
  Groups 16,30,32,41,58,350,377,488  -> role Id = 260000 + HtsGroupId (قرارداد پروژه)
  Direct users (Gnr_PageAction_User) -> role Inv.PartCardex.Direct
  Idempotent. Requires linked server [TMS] and Data/Scripts/Seed_PartCardex_DataProfile.sql applied first.
  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-inv-partcardex';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/inv/partcardex/list';
    DECLARE @EntityPascal NVARCHAR(300) = N'Entities.App.Inv.PartCardex';
    DECLARE @PageTitle NVARCHAR(200) = N'کاردکس کالا';

    DECLARE @ProfileId BIGINT = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Inv_PartCardex_Report');
    IF @ProfileId IS NULL
        THROW 51750, N'نمایه Inv_PartCardex_Report پیدا نشد؛ ابتدا Seed_PartCardex_DataProfile.sql را اجرا کنید.', 1;

    ---------------------------------------------------------------------------
    PRINT N'=== [1] Roles ===';
    ---------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Grp') IS NOT NULL DROP TABLE #Grp;
    CREATE TABLE #Grp (HtsGroupId INT NOT NULL PRIMARY KEY, RoleId BIGINT NOT NULL, RoleName NVARCHAR(200) NOT NULL);
    INSERT INTO #Grp (HtsGroupId, RoleId, RoleName)
    SELECT g.UserGroup_ID, 260000 + g.UserGroup_ID, g.UserGroup_Title
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroup] g
    WHERE g.UserGroup_ID IN (16, 30, 32, 41, 58, 350, 377, 488);
    IF (SELECT COUNT(*) FROM #Grp) <> 8
        THROW 51751, N'همه ۸ گروه HTS پیدا نشد.', 1;

    -- existing role ids keep their current names
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

    DECLARE @DirectName NVARCHAR(200) = N'Inv.PartCardex.Direct';
    DECLARE @DirectId BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = @DirectName);
    IF @DirectId IS NULL
    BEGIN
        INSERT INTO [system].[Role] (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@DirectName, N'دسترسی مستقیم کاردکس کالا', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET @DirectId = SCOPE_IDENTITY();
        PRINT N'  CREATED ' + @DirectName;
    END

    IF OBJECT_ID('tempdb..#AllRoles') IS NOT NULL DROP TABLE #AllRoles;
    SELECT RoleId, RoleName INTO #AllRoles FROM #Grp
    UNION ALL SELECT @DirectId, @DirectName
    UNION ALL SELECT r.Id, r.Name FROM [system].[Role] r WHERE r.Id = 200052;   -- ShowAllMenus (project convention)

    ---------------------------------------------------------------------------
    PRINT N'=== [2] RoleAccess (page + data profile) ===';
    ---------------------------------------------------------------------------
    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @EntityPascal, @PageTitle, NULL, NULL, r.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #AllRoles r
    WHERE NOT EXISTS (SELECT 1 FROM [system].[RoleAccess] x
                      WHERE x.RoleId = r.RoleId AND x.Path = @PagePath AND x.ActionAccessType = 1);
    PRINT N'  page grants: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)), 3, 7, @EntityPascal, @PageTitle, NULL, @ProfileId, r.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #AllRoles r
    WHERE NOT EXISTS (SELECT 1 FROM [system].[RoleAccess] x
                      WHERE x.RoleId = r.RoleId AND x.ActionAccessType = 3 AND x.RowId = @ProfileId);
    PRINT N'  profile grants: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    ---------------------------------------------------------------------------
    PRINT N'=== [3] Assign users (active HTS group members + direct users) ===';
    ---------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Map') IS NOT NULL DROP TABLE #Map;
    CREATE TABLE #Map (Username NVARCHAR(200) NOT NULL, RoleId BIGINT NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #Map (Username, RoleId, RoleName)
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
           g.RoleId, g.RoleName
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    JOIN #Grp g ON g.HtsGroupId = m.UserGroup_FK
    WHERE u.IsActive = 1;

    INSERT INTO #Map (Username, RoleId, RoleName)
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
           @DirectId, @DirectName
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pu
    JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pu.PageAction_FK
    JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pu.User_FK
    WHERE pa.Page_FK = 105 AND pa.Permission_FK IN (2, 3) AND u.IsActive = 1;

    -- alternate HTS Username when it differs from the AD name
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

    ---------------------------------------------------------------------------
    PRINT N'=== [4] Menu leaf: سیستم انبار > عملیات > کاردکس کالا (AllMenus) ===';
    ---------------------------------------------------------------------------
    DECLARE @MenuId BIGINT, @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content FROM [system].[SystemMenu] WHERE Name = N'AllMenus' ORDER BY Id;
    IF @MenuId IS NULL THROW 51752, N'منوی AllMenus یافت نشد.', 1;

    IF CHARINDEX(@PagePath, @Content) > 0
        PRINT N'  menu leaf already present';
    ELSE
    BEGIN
        DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"کاردکس کالا","icon":"ki-book ki-outline","iconColor":"#FDE047","path":"/panel/inv/partcardex/list","a_attr":{"href":"/panel/inv/partcardex/list"},"data":{"iconColor":"#FDE047","path":"/panel/inv/partcardex/list"},"children":[]}';
        DECLARE @Target NVARCHAR(4000) = NULL;
        -- anchor: the branch «عملیات» of «سیستم انبار» that holds the sibling leaf کدینگ کالا
        ;WITH walk AS (
            SELECT CAST(N'$[' + j.[key] + N']' AS NVARCHAR(4000)) AS p, j.[value] AS v
            FROM OPENJSON(@Content) AS j
            UNION ALL
            SELECT CAST(w.p + N'.children[' + c.[key] + N']' AS NVARCHAR(4000)), c.[value]
            FROM walk AS w CROSS APPLY OPENJSON(w.v, N'$.children') AS c
        )
        SELECT TOP (1) @Target = p + N'.children'
        FROM walk
        WHERE JSON_VALUE(v, N'$.text') = N'عملیات'
          AND EXISTS (SELECT 1 FROM OPENJSON(v, N'$.children') AS k
                      WHERE JSON_VALUE(k.[value], N'$.path') = N'/panel/inv/partcodingcategory/index');        IF @Target IS NULL THROW 51753, N'شاخه سیستم انبار > عملیات در منو پیدا نشد.', 1;

        SET @Content = JSON_MODIFY(@Content, N'append ' + @Target, JSON_QUERY(@Leaf));
        IF @Content IS NULL OR ISJSON(@Content) <> 1 THROW 51754, N'Content منو بعد از درج معتبر نیست.', 1;

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
       AND (ra.Path = N'/panel/inv/partcardex/list' OR ra.RowId = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Inv_PartCardex_Report'))) AS Grants
FROM [system].[Role] r
WHERE r.Id IN (260016, 260030, 260032, 260041, 260058, 260350, 260377, 260488, 200052) OR r.Name = N'Inv.PartCardex.Direct';
