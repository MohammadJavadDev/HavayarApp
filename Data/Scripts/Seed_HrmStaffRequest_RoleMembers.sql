SET NOCOUNT ON;
DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @Map TABLE (GroupId INT, RoleName NVARCHAR(200));
INSERT INTO @Map VALUES
 (335, N'درخواست کنندگان نیرو'),
 (486, N'کارشناسان سیستم مهندسی دارای دسترسی سیستم منابع انسانی');

IF OBJECT_ID('tempdb..#R') IS NOT NULL DROP TABLE #R;
SELECT DISTINCT su.Id AS UserId, r.Id AS RoleId, r.Name AS RoleName
INTO #R
FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
INNER JOIN @Map map ON map.GroupId = m.UserGroup_FK
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = m.User_FK AND hu.IsActive = 1
INNER JOIN system.[User] su ON su.IsActive = 1 AND (
    LOWER(su.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(hu.ActiveDirectoryUsername)), N''), hu.Username))
    OR LOWER(su.Username) = LOWER(hu.Username))
INNER JOIN system.Role r ON r.Name = map.RoleName;

DECLARE @UserId BIGINT, @RoleId BIGINT, @RoleName NVARCHAR(200), @N INT = 0;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT UserId, RoleId, RoleName FROM #R;
OPEN c;
FETCH NEXT FROM c INTO @UserId, @RoleId, @RoleName;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE u.Id = @UserId AND TRY_CAST(j.value AS BIGINT) = @RoleId)
    BEGIN
        UPDATE system.[User]
        SET RoleIds = CASE WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]') THEN N'[' + CAST(@RoleId AS NVARCHAR(20)) + N']'
                ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS NVARCHAR(20)) + N']') END,
            Roles = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @RoleName) THEN Roles
                WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]') THEN N'["' + STRING_ESCAPE(@RoleName, 'json') + N'"]'
                ELSE STUFF(Roles, LEN(Roles), 1, N',"' + STRING_ESCAPE(@RoleName, 'json') + N'"]') END,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @UserId;
        SET @N = @N + 1;
    END
    FETCH NEXT FROM c INTO @UserId, @RoleId, @RoleName;
END
CLOSE c; DEALLOCATE c;
SELECT @N AS Assigned;
