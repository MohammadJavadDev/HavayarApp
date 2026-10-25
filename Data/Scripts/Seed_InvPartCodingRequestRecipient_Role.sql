/*
Seed_InvPartCodingRequestRecipient_Role.sql
نقش گیرنده ایمیل «درخواست ایجاد کد کالا».
کد: PartCodingCategoryController.RequestRecipientRoleName
عضو اولیه: abdolmaleki.h
این نقش دسترسی صفحه ندارد؛ فقط فهرست گیرنده ایمیل است.
Encoding: UTF-8 with BOM
sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_InvPartCodingRequestRecipient_Role.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-inv-partcoding-request-recipient';
DECLARE @RoleName NVARCHAR(200) = N'InvPartCodingRequestRecipient';
DECLARE @RoleTitle NVARCHAR(200) = N'انبار-درخواست کد کالا - گیرنده ایمیل درخواست';
DECLARE @Username NVARCHAR(200) = N'Abdolmaleki.h';

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RoleName)
BEGIN
    INSERT INTO system.Role
        (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
         CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (@RoleName, @RoleTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
END
ELSE
BEGIN
    UPDATE system.Role
    SET Title = @RoleTitle,
        IsActive = 1,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = @RoleName
      AND (ISNULL(Title, N'') <> @RoleTitle OR ISNULL(IsActive, 0) <> 1);
END

DECLARE @RoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = @RoleName);
DECLARE @UserId BIGINT = (
    SELECT TOP 1 Id
    FROM system.[User]
    WHERE LOWER(Username) = LOWER(@Username)
       OR LOWER(REPLACE(ISNULL(Email, N''), N'@havayar.com', N'')) = LOWER(@Username)
);

IF @UserId IS NULL OR @RoleId IS NULL
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, N'کاربر Abdolmaleki.h یا نقش InvPartCodingRequestRecipient پیدا نشد.', 1;
END

DECLARE @RolesJson NVARCHAR(MAX);
DECLARE @RoleIdsJson NVARCHAR(MAX);
SELECT @RolesJson = ISNULL(Roles, N'[]'), @RoleIdsJson = ISNULL(RoleIds, N'[]')
FROM system.[User]
WHERE Id = @UserId;

IF ISJSON(@RolesJson) <> 1 OR ISJSON(@RoleIdsJson) <> 1
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50002, N'Roles یا RoleIds کاربر JSON معتبر نیست.', 1;
END

IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RolesJson) WHERE LOWER([value]) = LOWER(@RoleName))
    SET @RolesJson = JSON_MODIFY(@RolesJson, N'append $', @RoleName);

IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RoleIdsJson) WHERE TRY_CAST([value] AS BIGINT) = @RoleId)
    SET @RoleIdsJson = JSON_MODIFY(@RoleIdsJson, N'append $', @RoleId);

UPDATE system.[User]
SET Roles = @RolesJson,
    RoleIds = @RoleIdsJson,
    ModifiedById = 1,
    ModifiedByName = @SeedUser,
    ModifiedDateMiladiDateTime = @Now,
    ModifiedDateShamsiDateTime = @NowShamsi
WHERE Id = @UserId;

COMMIT TRANSACTION;

SELECT r.Id, r.Name, r.Title, r.IsActive
FROM system.Role r
WHERE r.Name = @RoleName;

SELECT u.Id, u.Username, u.Email, u.NameFa
FROM system.[User] u
WHERE EXISTS (
    SELECT 1
    FROM OPENJSON(u.Roles) WITH (RoleName nvarchar(max) '$')
    WHERE RoleName = @RoleName
);
