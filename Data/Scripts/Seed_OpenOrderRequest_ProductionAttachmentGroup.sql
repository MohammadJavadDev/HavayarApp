/*
Seed_OpenOrderRequest_ProductionAttachmentGroup.sql
ایمیل تولید هنگام پیوست نوع 264 / 271 / 307.
قدیم: Sohrabi.z , golestaneh.a , Eftekhari.a , Estaki.a
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-oor-production-attachment';
DECLARE @Code NVARCHAR(100) = N'Sup.OpenOrderRequest.ProductionAttachment';

IF @DryRun = 0 AND NOT EXISTS (SELECT 1 FROM system.NotificationGroup WHERE Code = @Code)
BEGIN
    INSERT INTO system.NotificationGroup
        (Code, DisplayName, Description,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (@Code,
         N'تامین و خرید - درخواست های باز - ایمیل پیوست به تولید',
         N'گیرندگان ایمیل اعلام ایجاد پیوست برای نوع Data Sheet و Wiring Diagram و Nameplate. قدیم: Sohrabi.z، golestaneh.a، Eftekhari.a، Estaki.a',
         1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
    PRINT N'Group inserted';
END

DECLARE @Map TABLE (Username NVARCHAR(200));
INSERT INTO @Map (Username) VALUES (N'Sohrabi.z'), (N'golestaneh.a'), (N'Eftekhari.a'), (N'Estaki.a');

SELECT m.Username AS Unmatched
FROM @Map m
WHERE NOT EXISTS (SELECT 1 FROM system.[User] u WHERE LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1);

IF @DryRun = 0
BEGIN
    INSERT INTO system.NotificationGroupMember
        (NotificationGroupId, UserId, Email, FullName,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT g.Id, u.Id,
           COALESCE(NULLIF(u.Email, N''), u.Username + N'@havayar.com'),
           COALESCE(NULLIF(u.NameFa, N''), NULLIF(u.Name, N''), u.Username),
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Map m
    INNER JOIN system.NotificationGroup g ON g.Code = @Code
    INNER JOIN system.[User] u ON LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1
    WHERE NOT EXISTS (
        SELECT 1 FROM system.NotificationGroupMember x
        WHERE x.NotificationGroupId = g.Id AND x.UserId = u.Id);
    PRINT N'Inserted members: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
END
