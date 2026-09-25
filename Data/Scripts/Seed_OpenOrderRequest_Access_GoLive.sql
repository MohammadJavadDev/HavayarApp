/*
================================================================================
Seed_OpenOrderRequest_Access_GoLive.sql
================================================================================
Go-live access alignment for Open Order Request (todo: access + SendToSupplier RoleAccess).

Rules:
  - Role Sup.OpenOrderRequest.EngineeringAccept (100000): REPLACE members so they are
    exactly the mapped HTS users with Vw_Permission Page_ID=74 / Permission_Name=N'Accept_Engineering'.
    No new engineering role. system.[User] has no HtsId — map via Username / AD name
    (Gnr_User.Username, ActiveDirectoryUsername) same as SyncOpenOrderRequestFromTotalSystem.
  - Page 76 FullAccess|Read missing from ConfigManage (100011): ADD only (do not remove).
  - Page 77 FullAccess|Read missing from HistoryView (100014): ADD only (do not remove).
  - SendToSupplier / SendToSupplierPartial RoleAccess for ConfigManage + SupplyAndPurchase
    (SupplyAndPurchase already has other open-order config paths).
  - dataProfile_99 / dataProfile_73: only add missing required role rows (none expected).
  - ConfigManageStaticPersonel (100012): HTS had a hardcoded static-beneficiary list, not a
    PageAction permission — no RoleAccess guessed; report only.

Requires linked server [TMS] -> TotalSystem.
Apply: sqlcmd -S ... -d HavayarApp -C -b -I -f 65001 -i this file
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-oor-access-golive';
DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sup.OpenOrderRequest';
DECLARE @Base NVARCHAR(100) = N'/panel/sup/openorderrequest';

DECLARE @EngRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Sup.OpenOrderRequest.EngineeringAccept');
DECLARE @ConfigRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Sup.OpenOrderRequest.ConfigManage');
DECLARE @HistRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Sup.OpenOrderRequest.HistoryView');
DECLARE @SupplyRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = N'SupplyAndPurchase');
DECLARE @EngRoleName NVARCHAR(200) = N'Sup.OpenOrderRequest.EngineeringAccept';
DECLARE @ConfigRoleName NVARCHAR(200) = N'Sup.OpenOrderRequest.ConfigManage';
DECLARE @HistRoleName NVARCHAR(200) = N'Sup.OpenOrderRequest.HistoryView';

IF @EngRoleId IS NULL OR @ConfigRoleId IS NULL OR @HistRoleId IS NULL OR @SupplyRoleId IS NULL
BEGIN
    RAISERROR(N'Required OpenOrder roles missing (100000/100011/100014/SupplyAndPurchase).', 16, 1);
    RETURN;
END

-----------------------------------------------------------------------------
PRINT N'=== [0] Note: system.[User] has no HtsId; map HTS User_FK via Username/AD ===';
PRINT N'=== [0b] ConfigManageStaticPersonel (100012): no HTS PageAction for نفرات ثابت — skip RoleAccess ===';
-----------------------------------------------------------------------------

IF OBJECT_ID('tempdb..#HtsUser') IS NOT NULL DROP TABLE #HtsUser;
CREATE TABLE #HtsUser
(
    Bucket     NVARCHAR(20) NOT NULL,
    HtsUserId  INT NOT NULL,
    HtsUsername NVARCHAR(200) NULL,
    AdName     NVARCHAR(200) NULL
);

INSERT INTO #HtsUser (Bucket, HtsUserId, HtsUsername, AdName)
SELECT DISTINCT N'Eng', p.User_FK,
    NULLIF(LTRIM(RTRIM(hu.Username)), N''),
    NULLIF(LTRIM(RTRIM(p.ActiveDirectoryUsername)), N'')
FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
WHERE p.Page_ID = 74 AND p.Permission_Name = N'Accept_Engineering' AND p.User_FK IS NOT NULL;

INSERT INTO #HtsUser (Bucket, HtsUserId, HtsUsername, AdName)
SELECT DISTINCT N'Config', p.User_FK,
    NULLIF(LTRIM(RTRIM(hu.Username)), N''),
    NULLIF(LTRIM(RTRIM(p.ActiveDirectoryUsername)), N'')
FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
WHERE p.Page_ID = 76 AND p.Permission_Name IN (N'FullAccess', N'Read') AND p.User_FK IS NOT NULL;

INSERT INTO #HtsUser (Bucket, HtsUserId, HtsUsername, AdName)
SELECT DISTINCT N'Hist', p.User_FK,
    NULLIF(LTRIM(RTRIM(hu.Username)), N''),
    NULLIF(LTRIM(RTRIM(p.ActiveDirectoryUsername)), N'')
FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
WHERE p.Page_ID = 77 AND p.Permission_Name IN (N'FullAccess', N'Read') AND p.User_FK IS NOT NULL;

IF OBJECT_ID('tempdb..#Mapped') IS NOT NULL DROP TABLE #Mapped;
CREATE TABLE #Mapped
(
    Bucket    NVARCHAR(20) NOT NULL,
    HtsUserId INT NOT NULL,
    UserId    BIGINT NOT NULL,
    Username  NVARCHAR(200) NOT NULL
);

;WITH Ranked AS (
    SELECT h.Bucket, h.HtsUserId, u.Id AS UserId, u.Username,
           ROW_NUMBER() OVER (
               PARTITION BY h.Bucket, h.HtsUserId
               ORDER BY CASE WHEN u.IsActive = 1 THEN 0 ELSE 1 END,
                        CASE WHEN h.HtsUsername IS NOT NULL AND LOWER(u.Username) = LOWER(h.HtsUsername) THEN 0 ELSE 1 END,
                        u.Id
           ) AS rn
    FROM #HtsUser h
    INNER JOIN system.[User] u ON (
        (h.HtsUsername IS NOT NULL AND LOWER(u.Username) = LOWER(h.HtsUsername))
        OR (h.AdName IS NOT NULL AND LOWER(u.Username) = LOWER(h.AdName))
    )
)
INSERT INTO #Mapped (Bucket, HtsUserId, UserId, Username)
SELECT Bucket, HtsUserId, UserId, Username
FROM Ranked WHERE rn = 1;

IF OBJECT_ID('tempdb..#Unmapped') IS NOT NULL DROP TABLE #Unmapped;
SELECT h.Bucket, h.HtsUserId, h.HtsUsername, h.AdName
INTO #Unmapped
FROM #HtsUser h
WHERE NOT EXISTS (
    SELECT 1 FROM #Mapped m WHERE m.Bucket = h.Bucket AND m.HtsUserId = h.HtsUserId
);

PRINT N'--- Unmapped HTS users (by bucket) ---';
SELECT Bucket, HtsUserId, HtsUsername, AdName FROM #Unmapped ORDER BY Bucket, HtsUserId;

-----------------------------------------------------------------------------
PRINT N'=== [1] Diff sets (before write) ===';
-----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#EngTarget') IS NOT NULL DROP TABLE #EngTarget;
IF OBJECT_ID('tempdb..#EngCurrent') IS NOT NULL DROP TABLE #EngCurrent;
IF OBJECT_ID('tempdb..#EngAdd') IS NOT NULL DROP TABLE #EngAdd;
IF OBJECT_ID('tempdb..#EngRemove') IS NOT NULL DROP TABLE #EngRemove;

SELECT DISTINCT UserId INTO #EngTarget FROM #Mapped WHERE Bucket = N'Eng';
SELECT DISTINCT u.Id AS UserId
INTO #EngCurrent
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS BIGINT) = @EngRoleId;

SELECT t.UserId INTO #EngAdd FROM #EngTarget t
WHERE NOT EXISTS (SELECT 1 FROM #EngCurrent c WHERE c.UserId = t.UserId);
SELECT c.UserId INTO #EngRemove FROM #EngCurrent c
WHERE NOT EXISTS (SELECT 1 FROM #EngTarget t WHERE t.UserId = c.UserId);

IF OBJECT_ID('tempdb..#CfgTarget') IS NOT NULL DROP TABLE #CfgTarget;
IF OBJECT_ID('tempdb..#CfgAdd') IS NOT NULL DROP TABLE #CfgAdd;
SELECT DISTINCT UserId INTO #CfgTarget FROM #Mapped WHERE Bucket = N'Config';
SELECT t.UserId INTO #CfgAdd FROM #CfgTarget t
WHERE NOT EXISTS (
    SELECT 1 FROM system.[User] u
    CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
    WHERE u.Id = t.UserId AND TRY_CAST(j.value AS BIGINT) = @ConfigRoleId
);

IF OBJECT_ID('tempdb..#HistTarget') IS NOT NULL DROP TABLE #HistTarget;
IF OBJECT_ID('tempdb..#HistAdd') IS NOT NULL DROP TABLE #HistAdd;
SELECT DISTINCT UserId INTO #HistTarget FROM #Mapped WHERE Bucket = N'Hist';
SELECT t.UserId INTO #HistAdd FROM #HistTarget t
WHERE NOT EXISTS (
    SELECT 1 FROM system.[User] u
    CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
    WHERE u.Id = t.UserId AND TRY_CAST(j.value AS BIGINT) = @HistRoleId
);

DECLARE @EngHts INT = (SELECT COUNT(*) FROM #HtsUser WHERE Bucket = N'Eng');
DECLARE @EngMapped INT = (SELECT COUNT(*) FROM #EngTarget);
DECLARE @EngCur INT = (SELECT COUNT(*) FROM #EngCurrent);
DECLARE @EngAddCnt INT = (SELECT COUNT(*) FROM #EngAdd);
DECLARE @EngRemCnt INT = (SELECT COUNT(*) FROM #EngRemove);
DECLARE @CfgAddCnt INT = (SELECT COUNT(*) FROM #CfgAdd);
DECLARE @HistAddCnt INT = (SELECT COUNT(*) FROM #HistAdd);
DECLARE @UnmappedCnt INT = (SELECT COUNT(*) FROM #Unmapped);

PRINT N'  Eng HTS=' + CAST(@EngHts AS NVARCHAR(10))
    + N' mapped=' + CAST(@EngMapped AS NVARCHAR(10))
    + N' current=' + CAST(@EngCur AS NVARCHAR(10))
    + N' ADD=' + CAST(@EngAddCnt AS NVARCHAR(10))
    + N' REMOVE=' + CAST(@EngRemCnt AS NVARCHAR(10));
PRINT N'  Config ADD=' + CAST(@CfgAddCnt AS NVARCHAR(10))
    + N'  History ADD=' + CAST(@HistAddCnt AS NVARCHAR(10))
    + N'  UnmappedHtsRows=' + CAST(@UnmappedCnt AS NVARCHAR(10));

PRINT N'--- Engineering REMOVE ---';
SELECT u.Id, u.Username, u.NameFa
FROM #EngRemove r INNER JOIN system.[User] u ON u.Id = r.UserId
ORDER BY u.Username;

PRINT N'--- Engineering ADD count ---';
SELECT COUNT(*) AS EngAddCount FROM #EngAdd;

BEGIN TRY
    BEGIN TRANSACTION;

    -------------------------------------------------------------------------
    PRINT N'=== [2] EngineeringAccept: remove extras ===';
    -------------------------------------------------------------------------
    DECLARE @Uid BIGINT, @RoleIdsJson NVARCHAR(MAX), @RolesJson NVARCHAR(MAX);
    DECLARE rem_cur CURSOR LOCAL FAST_FORWARD FOR SELECT UserId FROM #EngRemove;
    OPEN rem_cur;
    FETCH NEXT FROM rem_cur INTO @Uid;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @RoleIdsJson = RoleIds, @RolesJson = Roles FROM system.[User] WHERE Id = @Uid;

        SELECT @RoleIdsJson = N'[' + ISNULL(STUFF((
            SELECT N',' + j.value
            FROM OPENJSON(CASE WHEN ISJSON(ISNULL(@RoleIdsJson, N'[]')) = 1 THEN @RoleIdsJson ELSE N'[]' END) j
            WHERE TRY_CAST(j.value AS BIGINT) <> @EngRoleId
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';
        IF @RoleIdsJson IS NULL SET @RoleIdsJson = N'[]';

        SELECT @RolesJson = N'[' + ISNULL(STUFF((
            SELECT N',"' + REPLACE(j.value, N'"', N'\"') + N'"'
            FROM OPENJSON(CASE WHEN ISJSON(ISNULL(@RolesJson, N'[]')) = 1 THEN @RolesJson ELSE N'[]' END) j
            WHERE j.value <> @EngRoleName
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';
        IF @RolesJson IS NULL SET @RolesJson = N'[]';

        UPDATE system.[User]
        SET RoleIds = @RoleIdsJson,
            Roles = @RolesJson,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @Uid;

        FETCH NEXT FROM rem_cur INTO @Uid;
    END
    CLOSE rem_cur;
    DEALLOCATE rem_cur;
    PRINT N'  Removed: ' + CAST(@EngRemCnt AS NVARCHAR(10));

    -------------------------------------------------------------------------
    PRINT N'=== [3] Add role memberships (Eng / Config / Hist) ===';
    -------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#ToAdd') IS NOT NULL DROP TABLE #ToAdd;
    CREATE TABLE #ToAdd (UserId BIGINT NOT NULL, RoleId BIGINT NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #ToAdd (UserId, RoleId, RoleName)
    SELECT UserId, @EngRoleId, @EngRoleName FROM #EngAdd
    UNION ALL
    SELECT UserId, @ConfigRoleId, @ConfigRoleName FROM #CfgAdd
    UNION ALL
    SELECT UserId, @HistRoleId, @HistRoleName FROM #HistAdd;

    DECLARE @RoleId BIGINT, @RoleName NVARCHAR(200), @Assigned INT = 0;
    DECLARE add_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT UserId, RoleId, RoleName FROM #ToAdd;
    OPEN add_cur;
    FETCH NEXT FROM add_cur INTO @Uid, @RoleId, @RoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @Uid AND TRY_CAST(j.value AS BIGINT) = @RoleId
        )
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@RoleId AS NVARCHAR(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS NVARCHAR(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @RoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + @RoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @RoleName + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @Uid;
            SET @Assigned = @Assigned + 1;
        END
        FETCH NEXT FROM add_cur INTO @Uid, @RoleId, @RoleName;
    END
    CLOSE add_cur;
    DEALLOCATE add_cur;
    PRINT N'  Membership adds applied: ' + CAST(@Assigned AS NVARCHAR(10));

    -------------------------------------------------------------------------
    PRINT N'=== [4] SendToSupplier RoleAccess (ConfigManage + SupplyAndPurchase) ===';
    -------------------------------------------------------------------------
    DECLARE @RaRows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, RoleId BIGINT);

    INSERT INTO @RaRows (Path, AccessType, ItemType, RoleId)
    SELECT a.Path, a.AccessType, a.ItemType, r.RoleId
    FROM (VALUES
        (@Base + N'/sendtosupplier',        2, 1000),
        (@Base + N'/sendtosupplierpartial', 2, 1000),
        (@Base + N'/sendtosupplierpartial', 1, 1000)
    ) a(Path, AccessType, ItemType)
    CROSS JOIN (VALUES (@ConfigRoleId), (@SupplyRoleId)) r(RoleId);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT x.Path, x.AccessType, x.ItemType, @Entity, N'', NULL, NULL, x.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @RaRows x
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = x.RoleId AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType
    );
    PRINT N'  Inserted SendToSupplier RoleAccess: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

    -------------------------------------------------------------------------
    PRINT N'=== [5] dataProfile_99 / dataProfile_73 — add only if required role missing ===';
    -------------------------------------------------------------------------
    DECLARE @DpRows TABLE (Path NVARCHAR(100), RoleId BIGINT, Title NVARCHAR(200), RowId BIGINT);
    INSERT INTO @DpRows (Path, RoleId, Title, RowId)
    SELECT N'dataProfile_99', r.Id, N'تنظیمات درخواست های باز', 99
    FROM system.Role r
    WHERE r.Name IN (
        N'Sup.OpenOrderRequest.ConfigManage',
        N'Sup.OpenOrderRequest.Industrial',
        N'Industries',
        N'SupplyAndPurchase',
        N'ShowAllMenus'
    )
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.Path = N'dataProfile_99' AND ra.RoleId = r.Id AND ra.ActionAccessType = 3
    );

    INSERT INTO @DpRows (Path, RoleId, Title, RowId)
    SELECT N'dataProfile_73', r.Id, N'پیشینه درخواست ها', 73
    FROM system.Role r
    WHERE r.Name IN (
        N'Sup.OpenOrderRequest.HistoryView',
        N'Sup.OpenOrderRequest.EngineeringAccept',
        N'Sup.OpenOrderRequest.ShowAll',
        N'Sup.OpenOrderRequest.Industrial',
        N'Sup.OpenOrderRequest.Supply',
        N'Industries',
        N'SupplyAndPurchase',
        N'ShowAllMenus'
    )
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.Path = N'dataProfile_73' AND ra.RoleId = r.Id AND ra.ActionAccessType = 3
    );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT d.Path, 3, 7, @Entity, d.Title, NULL, d.RowId, d.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @DpRows d;
    PRINT N'  Inserted dataProfile RoleAccess gaps: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

    COMMIT TRANSACTION;
    PRINT N'=== COMMIT Seed_OpenOrderRequest_Access_GoLive ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-----------------------------------------------------------------------------
PRINT N'=== [6] Verification ===';
-----------------------------------------------------------------------------
DECLARE @EngAfter INT = (
    SELECT COUNT(*) FROM system.[User] u
    CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
    WHERE TRY_CAST(j.value AS BIGINT) = @EngRoleId
);
DECLARE @EngTargetAfter INT = (SELECT COUNT(*) FROM #EngTarget);
DECLARE @CfgGap INT = (
    SELECT COUNT(*) FROM #CfgTarget t
    WHERE NOT EXISTS (
        SELECT 1 FROM system.[User] u
        CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
        WHERE u.Id = t.UserId AND TRY_CAST(j.value AS BIGINT) = @ConfigRoleId
    )
);
DECLARE @HistGap INT = (
    SELECT COUNT(*) FROM #HistTarget t
    WHERE NOT EXISTS (
        SELECT 1 FROM system.[User] u
        CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
        WHERE u.Id = t.UserId AND TRY_CAST(j.value AS BIGINT) = @HistRoleId
    )
);

SELECT
    @EngHts AS EngHtsCount,
    @EngMapped AS EngMappedTarget,
    @EngAddCnt AS EngAdded,
    @EngRemCnt AS EngRemoved,
    @EngAfter AS EngMembersAfter,
    CASE WHEN @EngAfter = @EngTargetAfter THEN 1 ELSE 0 END AS EngCountMatchesTarget,
    @CfgAddCnt AS ConfigAdded,
    @CfgGap AS ConfigGapsRemaining,
    @HistAddCnt AS HistAdded,
    @HistGap AS HistGapsRemaining,
    @UnmappedCnt AS UnmappedHtsRows;

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path LIKE N'/panel/sup/openorderrequest/sendtosupplier%'
ORDER BY ra.Path, r.Name;

SELECT Bucket, COUNT(*) AS Cnt FROM #Unmapped GROUP BY Bucket;
