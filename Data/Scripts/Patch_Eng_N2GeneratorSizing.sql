/*
  Patch_Eng_N2GeneratorSizing.sql
  دسترسی و منوی سایزینگ نیتروژن‌ساز (صفحه HTS 465).
  UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-eng-n2-sizing';
    DECLARE @RoleId BIGINT = 260384;
    DECLARE @RoleName NVARCHAR(200) = N'کارشناسان سایزینگ نیتروژن ساز';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Id = @RoleId AND IsActive = 1)
        THROW 51211, N'Role 260384 is missing or inactive.', 1;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT v.Path, v.ActionAccessType, v.ActionAccessItemType, NULL, NULL, NULL, NULL, @RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
        (N'/panel/eng/n2generatorsizing/calculator', 1, 0),
        (N'/panel/eng/n2generatorsizing/calculate', 2, 1000)
    ) v(Path, ActionAccessType, ActionAccessItemType)
    WHERE NOT EXISTS (
        SELECT 1
        FROM system.RoleAccess x
        WHERE x.RoleId = @RoleId
          AND x.Path = v.Path
          AND x.ActionAccessType = v.ActionAccessType
    );

    DECLARE @UserId BIGINT;
    DECLARE n2_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT u.Id
        FROM system.[User] u
        WHERE u.IsActive = 1
          AND (
                (u.Id = 216 AND LOWER(u.Username) = N'kashani.sh')
             OR (u.Id = 797 AND LOWER(u.Username) = N'hashemi.mo')
             OR (u.Id = 220 AND LOWER(u.Username) = N'rajabi.m')
             OR (u.Id = 240 AND LOWER(u.Username) = N'zare.mo')
             OR (u.Id = 294 AND LOWER(u.Username) = N'moradi.m')
             OR (u.Id = 40 AND LOWER(u.Username) = N'dr.h-ghoroori')
          )
          AND NOT EXISTS (
                SELECT 1
                FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) = @RoleId
          );
    OPEN n2_cur;
    FETCH NEXT FROM n2_cur INTO @UserId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        UPDATE system.[User]
        SET
            RoleIds = CASE
                WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                    THEN N'[' + CAST(@RoleId AS nvarchar(20)) + N']'
                ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS nvarchar(20)) + N']')
            END,
            Roles = CASE
                WHEN ISJSON(ISNULL(Roles, N'[]')) = 1
                     AND EXISTS (SELECT 1 FROM OPENJSON(Roles) j WHERE j.value = @RoleName) THEN Roles
                WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]' OR ISJSON(ISNULL(Roles, N'[]')) <> 1
                    THEN N'["' + @RoleName + N'"]'
                ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @RoleName + N'"]')
            END,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @UserId;

        FETCH NEXT FROM n2_cur INTO @UserId;
    END
    CLOSE n2_cur;
    DEALLOCATE n2_cur;

    DECLARE @Old NVARCHAR(MAX) = N'{"iconColor":"#FDBA74","path":"/panel/eng/filterwatertrap/calculator"},"children":[]}]}]},{"text":"مهندسی محصول"';
    DECLARE @New NVARCHAR(MAX) = N'{"iconColor":"#FDBA74","path":"/panel/eng/filterwatertrap/calculator"},"children":[]}]},{"text":"سایزینگ نیتروژن ساز","icon":"ki-technology-4 ki-outline","iconColor":"#FDBA74","path":"","a_attr":{"href":""},"data":{"iconColor":"#FDBA74","path":""},"children":[{"text":"سایزینگ نیتروژن ساز","icon":"ki-technology-4 ki-outline","iconColor":"#FDBA74","path":"/panel/eng/n2generatorsizing/calculator","a_attr":{"href":"/panel/eng/n2generatorsizing/calculator"},"data":{"iconColor":"#FDBA74","path":"/panel/eng/n2generatorsizing/calculator"},"children":[]}]}]},{"text":"مهندسی محصول"';

    UPDATE system.SystemMenu
    SET Content = REPLACE(Content, @Old, @New),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Content LIKE N'%/panel/eng/filterwatertrap/calculator%'
      AND Content NOT LIKE N'%/panel/eng/n2generatorsizing/calculator%'
      AND CHARINDEX(@Old, Content) > 0;

    IF EXISTS (
        SELECT 1
        FROM system.SystemMenu
        WHERE Content LIKE N'%/panel/eng/filterwatertrap/calculator%'
          AND Content NOT LIKE N'%/panel/eng/n2generatorsizing/calculator%'
          AND CHARINDEX(@Old, Content) > 0
    )
        THROW 51212, N'N2 menu node was not inserted.', 1;

    IF EXISTS (
        SELECT 1
        FROM system.SystemMenu
        WHERE Content LIKE N'%/panel/eng/n2generatorsizing/calculator%'
          AND ISJSON(Content) <> 1
    )
        THROW 51213, N'AllMenus JSON became invalid.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'N2 RoleAccess' AS CheckName, COUNT(*) AS Cnt
FROM system.RoleAccess
WHERE RoleId = 260384
  AND Path LIKE N'/panel/eng/n2generatorsizing/%';

SELECT u.Id, u.Username,
       CASE WHEN EXISTS (
            SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE TRY_CAST(j.value AS bigint) = 260384
       ) THEN 1 ELSE 0 END AS HasRole
FROM system.[User] u
WHERE u.Id IN (216, 797, 220, 240, 294, 40);
