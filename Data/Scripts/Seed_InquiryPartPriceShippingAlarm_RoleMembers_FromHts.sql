/*
================================================================================
Seed_InquiryPartPriceShippingAlarm_RoleMembers_FromHts.sql
گروه 577 → دریافت کنندگان ایمیل هشدار اختلاف قیمت خرید
تطبیق: COALESCE(ActiveDirectoryUsername, Username) با system.[User].Username
اجرا: @DryRun = 1 فقط گزارش؛ @DryRun = 0 عضویت اضافه می‌کند (حذف نمی‌کند).
کاربر غیرفعال در HavayarApp اضافه نمی‌شود.
UTF-8 BOM. پیش‌نیاز: Seed_InquiryPartPriceShippingAlarm_Role.sql و Linked Server [TMS].
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_InquiryPartPriceShippingAlarm_RoleMembers_FromHts.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-shipping-price-alarm';

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, Source NVARCHAR(120) NOT NULL);

DECLARE @GroupRole TABLE (UserGroup_ID INT, RoleName NVARCHAR(200));
INSERT INTO @GroupRole VALUES
    (577, N'دریافت کنندگان ایمیل هشدار اختلاف قیمت خرید');

INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username), gr.RoleName,
       N'group:' + CAST(gr.UserGroup_ID AS NVARCHAR(10))
FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
INNER JOIN @GroupRole gr ON gr.UserGroup_ID = m.UserGroup_FK
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
WHERE u.IsActive = 1 AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username) IS NOT NULL;

INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT hu.Username, m.RoleName, m.Source + N'+htsUser'
FROM #UserRoleMap m
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON LOWER(hu.ActiveDirectoryUsername) = LOWER(m.Username)
WHERE hu.Username IS NOT NULL AND LOWER(hu.Username) <> LOWER(m.Username)
  AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE LOWER(x.Username) = LOWER(hu.Username) AND x.RoleName = m.RoleName);

IF OBJECT_ID('tempdb..#Resolved') IS NOT NULL DROP TABLE #Resolved;
CREATE TABLE #Resolved (
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

SELECT RoleName,
       SUM(CASE WHEN Status = N'add' THEN 1 ELSE 0 END) AS ToAdd,
       SUM(CASE WHEN Status = N'already' THEN 1 ELSE 0 END) AS Already,
       SUM(CASE WHEN Status = N'inactive-user' THEN 1 ELSE 0 END) AS InactiveUser,
       SUM(CASE WHEN Status = N'missing-user' THEN 1 ELSE 0 END) AS MissingUser
FROM #Resolved GROUP BY RoleName ORDER BY RoleName;

SELECT Status, RoleName, Username, Source FROM #Resolved WHERE Status <> N'already' ORDER BY Status, RoleName, Username;

IF @DryRun = 1
BEGIN
    PRINT N'=== DRY RUN — برای اعمال @DryRun = 0 ===';
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @UserId BIGINT, @RoleId BIGINT, @RoleName NVARCHAR(200), @Assigned INT = 0;
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
            SET @Assigned += 1;
        END
        FETCH NEXT FROM apply_cur INTO @UserId, @RoleId, @RoleName;
    END
    CLOSE apply_cur; DEALLOCATE apply_cur;
    COMMIT TRANSACTION;
    PRINT N'assignments: ' + CAST(@Assigned AS NVARCHAR(20));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH
