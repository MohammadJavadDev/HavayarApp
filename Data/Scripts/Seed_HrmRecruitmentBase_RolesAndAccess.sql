/*
Seed_HrmRecruitmentBase_RolesAndAccess.sql
دسترسی و منوی اطلاعات پایه جذب و استخدام.
نقش‌ها:
  ۲۶۰۰۱۳ «مدیرسیستم جذب واستخدام» — دسترسی کامل
  ۲۶۰۰۵۹ «دسترسی مشاهده-سیستم جذب و استخدام» — فقط مشاهده
  «مشاهده کلی HTS» — فقط مشاهده
  «کارشناسان خدمات پس از فروش» — فقط ویرایش پست سازمانی، بدون جدید و حذف
گرایش منوی جدا ندارد.
اعضای گروه ۱۳ و ۵۹ از Gnr_UserGroupMember می‌آیند.
UTF-8 with BOM. Idempotent.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_HrmRecruitmentBase_RolesAndAccess.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-hrm-recruitment-base-access';

DECLARE @ManagerName NVARCHAR(200) = N'مدیرسیستم جذب واستخدام';
DECLARE @ViewerName NVARCHAR(200) = N'دسترسی مشاهده-سیستم جذب و استخدام';
DECLARE @ViewAllName NVARCHAR(200) = N'مشاهده کلی HTS';
DECLARE @AfterSalesName NVARCHAR(200) = N'کارشناسان خدمات پس از فروش';

IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = N'Hrm_OrgPost_List')
BEGIN
    RAISERROR(N'نمایه اطلاعات پایه یافت نشد. ابتدا Seed_HrmRecruitmentBase_DataProfiles.sql را اجرا کنید.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @ManagerName)
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Id = 260013)
        BEGIN
            SET IDENTITY_INSERT [system].[Role] ON;
            INSERT INTO [system].[Role]
                (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (260013, @ManagerName, @ManagerName, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT [system].[Role] OFF;
        END
        ELSE
            INSERT INTO [system].[Role]
                (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@ManagerName, @ManagerName, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @ViewerName)
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Id = 260059)
        BEGIN
            SET IDENTITY_INSERT [system].[Role] ON;
            INSERT INTO [system].[Role]
                (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (260059, @ViewerName, @ViewerName, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT [system].[Role] OFF;
        END
        ELSE
            INSERT INTO [system].[Role]
                (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@ViewerName, @ViewerName, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
    END

    DECLARE @Manager BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = @ManagerName);
    DECLARE @Viewer BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = @ViewerName);
    DECLARE @ViewAll BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = @ViewAllName);
    DECLARE @AfterSales BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = @AfterSalesName);
    IF @Manager IS NULL OR @Viewer IS NULL OR @ViewAll IS NULL OR @AfterSales IS NULL
    BEGIN
        RAISERROR(N'یکی از نقش‌های مدیر جذب، مشاهده جذب، مشاهده کلی یا خدمات پس از فروش یافت نشد.', 16, 1);
    END

    DECLARE @Act TABLE (Suffix NVARCHAR(40), AType INT, IType INT, ForView BIT, ForWrite BIT, ForEditOnly BIT);
    INSERT INTO @Act VALUES
        (N'list',          1, 1,    1, 1, 1),
        (N'fetchdata',     2, 2,    1, 1, 1),
        (N'new',           1, 4,    0, 1, 0),
        (N'add',           2, 4,    0, 1, 0),
        (N'edit',          1, 5,    0, 1, 1),
        (N'update',        2, 5,    0, 1, 1),
        (N'delete',        2, 6,    0, 1, 0),
        (N'exporttoexcel', 2, 1000, 1, 1, 1);

    DECLARE @Page TABLE (Prefix NVARCHAR(80), EntityName NVARCHAR(200), ProfileName NVARCHAR(100), AllowNew BIT, ForAfterSales BIT);
    INSERT INTO @Page VALUES
        (N'/panel/hrm/orgpost/', N'Entities.App.Hrm.OrgPost', N'Hrm_OrgPost_List', 0, 1),
        (N'/panel/hrm/skill/', N'Entities.App.Hrm.Skill', N'Hrm_Skill_List', 1, 0),
        (N'/panel/hrm/studycourse/', N'Entities.App.Hrm.StudyCourse', N'Hrm_StudyCourse_List', 0, 0),
        (N'/panel/hrm/incomingsource/', N'Entities.App.Hrm.IncomingSource', N'Hrm_IncomingSource_List', 1, 0);

    DECLARE @Grants TABLE (RoleId BIGINT, Path NVARCHAR(300), AType INT, IType INT, EntityName NVARCHAR(200));

    INSERT INTO @Grants
    SELECT @Manager, p.Prefix + a.Suffix, a.AType, a.IType, p.EntityName
    FROM @Page p
    CROSS JOIN @Act a
    WHERE a.ForWrite = 1 AND (p.AllowNew = 1 OR a.Suffix NOT IN (N'new', N'add'));

    INSERT INTO @Grants
    SELECT r.RoleId, p.Prefix + a.Suffix, a.AType, a.IType, p.EntityName
    FROM @Page p
    CROSS JOIN @Act a
    CROSS JOIN (SELECT @Viewer AS RoleId UNION ALL SELECT @ViewAll) r
    WHERE a.ForView = 1;

    INSERT INTO @Grants
    SELECT @AfterSales, p.Prefix + a.Suffix, a.AType, a.IType, p.EntityName
    FROM @Page p
    CROSS JOIN @Act a
    WHERE p.ForAfterSales = 1 AND a.ForEditOnly = 1;

    INSERT INTO @Grants
    SELECT @Manager, N'/panel/hrm/trendofstudycourse/' + a.Suffix, a.AType, a.IType, N'Entities.App.Hrm.TrendOfStudyCourse'
    FROM (VALUES
        (N'getbystudycourse', 2, 2),
        (N'new', 1, 4),
        (N'add', 2, 4),
        (N'edit', 1, 5),
        (N'update', 2, 5),
        (N'delete', 2, 6)
    ) a(Suffix, AType, IType);

    INSERT INTO @Grants
    SELECT r.RoleId, N'/panel/hrm/trendofstudycourse/getbystudycourse', 2, 2, N'Entities.App.Hrm.TrendOfStudyCourse'
    FROM (SELECT @Viewer AS RoleId UNION ALL SELECT @ViewAll) r;

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.RoleId IN (@Manager, @Viewer, @ViewAll, @AfterSales)
      AND (
            ra.Path LIKE N'/panel/hrm/orgpost/%'
         OR ra.Path LIKE N'/panel/hrm/skill/%'
         OR ra.Path LIKE N'/panel/hrm/studycourse/%'
         OR ra.Path LIKE N'/panel/hrm/incomingsource/%'
         OR ra.Path LIKE N'/panel/hrm/trendofstudycourse/%'
         OR (ra.ActionAccessType = 3 AND ra.Path IN (
                SELECT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20))
                FROM [system].[SavedQuery] q
                WHERE q.Name IN (N'Hrm_OrgPost_List', N'Hrm_Skill_List', N'Hrm_StudyCourse_List', N'Hrm_IncomingSource_List')))
      );

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT g.Path, g.AType, g.IType, g.EntityName, g.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Grants g;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)), 3, 7, p.EntityName, q.Title, q.Id, g.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Page p
    INNER JOIN [system].[SavedQuery] q ON q.Name = p.ProfileName
    INNER JOIN (SELECT DISTINCT RoleId, EntityName FROM @Grants) g ON g.EntityName = p.EntityName
    WHERE g.RoleId IN (@Manager, @Viewer, @ViewAll)
       OR (g.RoleId = @AfterSales AND p.ForAfterSales = 1);

    DECLARE @LeafPost NVARCHAR(MAX) = N'{"text":"پست های سازمانی","icon":"ki-profile-user ki-outline","iconColor":"#c084fc","path":"/panel/hrm/orgpost/list","a_attr":{"href":"/panel/hrm/orgpost/list"},"data":{"iconColor":"#c084fc","path":"/panel/hrm/orgpost/list"},"children":[]}';
    DECLARE @LeafSkill NVARCHAR(MAX) = N'{"text":"مهارت","icon":"ki-element-11 ki-outline","iconColor":"#c084fc","path":"/panel/hrm/skill/list","a_attr":{"href":"/panel/hrm/skill/list"},"data":{"iconColor":"#c084fc","path":"/panel/hrm/skill/list"},"children":[]}';
    DECLARE @LeafCourse NVARCHAR(MAX) = N'{"text":"رشته تحصیلی","icon":"ki-element-11 ki-outline","iconColor":"#c084fc","path":"/panel/hrm/studycourse/list","a_attr":{"href":"/panel/hrm/studycourse/list"},"data":{"iconColor":"#c084fc","path":"/panel/hrm/studycourse/list"},"children":[]}';
    DECLARE @LeafSource NVARCHAR(MAX) = N'{"text":"کانال ارتباطی","icon":"ki-element-11 ki-outline","iconColor":"#c084fc","path":"/panel/hrm/incomingsource/list","a_attr":{"href":"/panel/hrm/incomingsource/list"},"data":{"iconColor":"#c084fc","path":"/panel/hrm/incomingsource/list"},"children":[]}';
    DECLARE @BaseGroup NVARCHAR(MAX) = N'{"text":"اطلاعات پایه","icon":"ki-element-11 ki-outline","iconColor":"#c084fc","path":"","a_attr":{"href":""},"data":{"iconColor":"#c084fc","path":""},"children":['
        + @LeafPost + N',' + @LeafSkill + N',' + @LeafCourse + N',' + @LeafSource + N']}';

    DECLARE @AllContent NVARCHAR(MAX) = (SELECT Content FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
    DECLARE @HrIdx INT = (SELECT TOP 1 CAST(j.[key] AS INT) FROM OPENJSON(@AllContent) j WHERE JSON_VALUE(j.value, '$.text') = N'منابع انسانی');
    IF @HrIdx IS NULL
    BEGIN
        RAISERROR(N'گروه «منابع انسانی» در منوی AllMenus یافت نشد.', 16, 1);
    END

    DECLARE @Hr NVARCHAR(MAX) = (SELECT TOP 1 CAST(j.value AS NVARCHAR(MAX)) FROM OPENJSON(@AllContent) j WHERE CAST(j.[key] AS INT) = @HrIdx);
    DECLARE @HrChildren NVARCHAR(MAX) = JSON_QUERY(@Hr, '$.children');
    DECLARE @RecIdx INT = (SELECT TOP 1 CAST(c.[key] AS INT) FROM OPENJSON(@HrChildren) c WHERE JSON_VALUE(c.value, '$.text') = N'سیستم جذب و استخدام');
    IF @RecIdx IS NULL
    BEGIN
        RAISERROR(N'گروه «سیستم جذب و استخدام» در منوی AllMenus یافت نشد.', 16, 1);
    END

    DECLARE @Rec NVARCHAR(MAX) = (SELECT TOP 1 CAST(c.value AS NVARCHAR(MAX)) FROM OPENJSON(@HrChildren) c WHERE CAST(c.[key] AS INT) = @RecIdx);
    DECLARE @RecChildren NVARCHAR(MAX) = JSON_QUERY(@Rec, '$.children');
    SELECT @RecChildren = N'[' + ISNULL(STRING_AGG(CAST(c.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(c.[key] AS INT)), N'') + N']'
    FROM OPENJSON(ISNULL(@RecChildren, N'[]')) c
    WHERE JSON_VALUE(c.value, '$.text') <> N'اطلاعات پایه';
    SET @RecChildren = JSON_MODIFY(@RecChildren, 'append $', JSON_QUERY(@BaseGroup));
    SET @Rec = JSON_MODIFY(@Rec, '$.children', JSON_QUERY(@RecChildren));
    SET @HrChildren = JSON_MODIFY(@HrChildren, '$[' + CAST(@RecIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@Rec));
    SET @Hr = JSON_MODIFY(@Hr, '$.children', JSON_QUERY(@HrChildren));
    SET @AllContent = JSON_MODIFY(@AllContent, '$[' + CAST(@HrIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@Hr));

    DECLARE @AllIds NVARCHAR(MAX) = (SELECT AccessRoleIds FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
    DECLARE @AllRoleNames NVARCHAR(MAX) = (SELECT AccessRoles FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
    SELECT @AllIds = N'[' + STRING_AGG(CAST(x.Id AS NVARCHAR(20)), N',') + N']'
    FROM (
        SELECT TRY_CAST(j.value AS BIGINT) AS Id FROM OPENJSON(ISNULL(@AllIds, N'[]')) j
        UNION
        SELECT r.Id FROM [system].[Role] r WHERE r.Id IN (@Manager, @Viewer, @ViewAll, @AfterSales)
    ) x
    WHERE x.Id IS NOT NULL;
    SELECT @AllRoleNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
    FROM (
        SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@AllRoleNames, N'[]')) j
        UNION
        SELECT r.Name FROM [system].[Role] r WHERE r.Id IN (@Manager, @Viewer, @ViewAll, @AfterSales)
    ) x;

    UPDATE [system].[SystemMenu]
    SET Content = @AllContent, AccessRoleIds = @AllIds, AccessRoles = @AllRoleNames,
        ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'AllMenus';

    COMMIT TRANSACTION;
    PRINT N'=== DONE access + menu ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
    RETURN;
END CATCH;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد. دسترسی و منو اعمال شد؛ عضویت گروه‌ها اعمال نشد.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, Source NVARCHAR(120) NOT NULL);

INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username),
       CASE m.UserGroup_FK WHEN 13 THEN @ManagerName ELSE @ViewerName END,
       N'group:' + CAST(m.UserGroup_FK AS NVARCHAR(10))
FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
WHERE m.UserGroup_FK IN (13, 59) AND u.IsActive = 1;

INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT hu.Username, m.RoleName, m.Source + N'+htsUser'
FROM #UserRoleMap m
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON LOWER(hu.ActiveDirectoryUsername) = LOWER(m.Username)
WHERE hu.Username IS NOT NULL AND LOWER(hu.Username) <> LOWER(m.Username)
  AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE LOWER(x.Username) = LOWER(hu.Username) AND x.RoleName = m.RoleName);

IF OBJECT_ID('tempdb..#Resolved') IS NOT NULL DROP TABLE #Resolved;
CREATE TABLE #Resolved
(
    Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, Source NVARCHAR(120) NOT NULL,
    UserId BIGINT NULL, RoleId BIGINT NULL, Status NVARCHAR(20) NOT NULL
);

INSERT INTO #Resolved (Username, RoleName, Source, UserId, RoleId, Status)
SELECT m.Username, m.RoleName, m.Source, u.Id, r.Id,
    CASE
        WHEN r.Id IS NULL THEN N'missing-role'
        WHEN u.Id IS NULL THEN N'missing-user'
        WHEN ISNULL(u.IsActive, 0) <> 1 THEN N'inactive-user'
        WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS BIGINT) = r.Id) THEN N'already'
        ELSE N'add'
    END
FROM #UserRoleMap m
LEFT JOIN system.Role r ON r.Name = m.RoleName
OUTER APPLY (
    SELECT TOP 1 x.Id, x.IsActive, x.RoleIds
    FROM system.[User] x
    WHERE LOWER(x.Username) = LOWER(m.Username)
    ORDER BY CASE WHEN x.IsActive = 1 THEN 0 ELSE 1 END, x.Id
) u;

;WITH d AS (
    SELECT UserId, RoleName,
           ROW_NUMBER() OVER (PARTITION BY UserId, RoleName
               ORDER BY CASE Status WHEN N'add' THEN 0 WHEN N'already' THEN 1 ELSE 2 END, Username) AS rn
    FROM #Resolved WHERE UserId IS NOT NULL
)
DELETE FROM d WHERE rn > 1;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @UserId BIGINT, @RoleId BIGINT, @RoleName NVARCHAR(200);
    DECLARE @Assigned INT = 0;
    DECLARE apply_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT UserId, RoleId, RoleName FROM #Resolved WHERE Status = N'add';
    OPEN apply_cur;
    FETCH NEXT FROM apply_cur INTO @UserId, @RoleId, @RoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @UserId AND TRY_CAST(j.value AS BIGINT) = @RoleId)
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@RoleId AS NVARCHAR(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS NVARCHAR(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @RoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + STRING_ESCAPE(@RoleName, 'json') + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + STRING_ESCAPE(@RoleName, 'json') + N'"]')
                END,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @UserId;
            SET @Assigned = @Assigned + 1;
        END
        FETCH NEXT FROM apply_cur INTO @UserId, @RoleId, @RoleName;
    END
    CLOSE apply_cur;
    DEALLOCATE apply_cur;
    COMMIT TRANSACTION;
    PRINT N'  User role assignments applied: ' + CAST(@Assigned AS NVARCHAR(20));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg2 NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg2, 16, 1);
END CATCH;

SELECT r.Name,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType <> 3
           AND (ra.Path LIKE N'/panel/hrm/orgpost/%' OR ra.Path LIKE N'/panel/hrm/skill/%'
             OR ra.Path LIKE N'/panel/hrm/studycourse/%' OR ra.Path LIKE N'/panel/hrm/incomingsource/%'
             OR ra.Path LIKE N'/panel/hrm/trendofstudycourse/%')) AS PageRows
FROM [system].[Role] r
WHERE r.Name IN (@ManagerName, @ViewerName, @ViewAllName, @AfterSalesName);

SELECT JSON_VALUE(leaf.value, '$.text') AS Leaf, JSON_VALUE(leaf.value, '$.path') AS Path
FROM [system].[SystemMenu] m
CROSS APPLY OPENJSON(m.Content) hr
CROSS APPLY OPENJSON(JSON_QUERY(hr.value, '$.children')) rec
CROSS APPLY OPENJSON(JSON_QUERY(rec.value, '$.children')) grp
CROSS APPLY OPENJSON(JSON_QUERY(grp.value, '$.children')) leaf
WHERE m.Name = N'AllMenus'
  AND JSON_VALUE(hr.value, '$.text') = N'منابع انسانی'
  AND JSON_VALUE(rec.value, '$.text') = N'سیستم جذب و استخدام'
  AND JSON_VALUE(grp.value, '$.text') = N'اطلاعات پایه';
