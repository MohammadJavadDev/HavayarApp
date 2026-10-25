/*
Seed_OpenOrderRequest_AutoAcceptNotificationGroups.sql
گروه‌های ایمیل تایید خودکار مهندسی (HTS SendNewAttachmentsEmailNotification).
عضویت از پنل گروه اعلان قابل کم و زیاد شدن است.

  Sup.OpenOrderRequest.AutoAcceptFixedCc   رونوشت ثابت قدیم: Sharifi.b
  Sup.OpenOrderRequest.AutoAcceptPeerTo    اگر متولی خرید یکی از این‌ها باشد، بقیه هم گیرنده می‌شوند: Bagheri.h, Shahpordeli.s, Sohrabi.za
رونوشت واحد صنایع از گروه موجود Sup.OpenOrderRequest.Industrial خوانده می‌شود.

@DryRun = 1 پیش‌فرض. برای درج @DryRun = 0.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-oor-autoaccept-groups';

DECLARE @Groups TABLE (Code NVARCHAR(100), DisplayName NVARCHAR(200), Description NVARCHAR(1500));
INSERT INTO @Groups (Code, DisplayName, Description) VALUES
 (N'Sup.OpenOrderRequest.AutoAcceptFixedCc',
  N'تامین و خرید - درخواست های باز - رونوشت تایید خودکار مهندسی',
  N'رونوشت ثابت ایمیل تایید خودکار مهندسی. قدیم: Sharifi.b'),
 (N'Sup.OpenOrderRequest.AutoAcceptPeerTo',
  N'تامین و خرید - درخواست های باز - گیرندگان همتای تایید خودکار مهندسی',
  N'اگر ایمیل متولی خرید با یکی از اعضا جور باشد، بقیه اعضا هم گیرنده همان ایمیل می‌شوند. قدیم: Bagheri.h / Shahpordeli.s / Sohrabi.za');

DECLARE @Map TABLE (GroupCode NVARCHAR(100), Username NVARCHAR(200));
INSERT INTO @Map (GroupCode, Username) VALUES
 (N'Sup.OpenOrderRequest.AutoAcceptFixedCc', N'Sharifi.b'),
 (N'Sup.OpenOrderRequest.AutoAcceptPeerTo', N'Bagheri.h'),
 (N'Sup.OpenOrderRequest.AutoAcceptPeerTo', N'Shahpordeli.s'),
 (N'Sup.OpenOrderRequest.AutoAcceptPeerTo', N'Sohrabi.za');

PRINT N'--- Groups (' + CASE WHEN @DryRun = 1 THEN N'DRY RUN' ELSE N'APPLY' END + N') ---';

IF @DryRun = 0
BEGIN
    INSERT INTO system.NotificationGroup
        (Code, DisplayName, Description,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT g.Code, g.DisplayName, g.Description,
           1, 1, @SeedUser, @SeedUser,
           @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Groups g
    WHERE NOT EXISTS (SELECT 1 FROM system.NotificationGroup n WHERE n.Code = g.Code);
    PRINT N'NotificationGroup rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
END

DECLARE @Resolved TABLE (GroupId BIGINT, GroupCode NVARCHAR(100), UserId BIGINT, Email NVARCHAR(200), FullName NVARCHAR(200));
INSERT INTO @Resolved (GroupId, GroupCode, UserId, Email, FullName)
SELECT DISTINCT g.Id, g.Code, u.Id,
       COALESCE(NULLIF(u.Email, N''), u.Username + N'@havayar.com'),
       COALESCE(NULLIF(u.NameFa, N''), NULLIF(u.Name, N''), u.Username)
FROM @Map m
INNER JOIN system.NotificationGroup g ON g.Code = m.GroupCode
INNER JOIN system.[User] u ON LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1;

SELECT DISTINCT m.GroupCode, m.Username AS Unmatched
FROM @Map m
WHERE NOT EXISTS (SELECT 1 FROM system.[User] u WHERE LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1)
ORDER BY m.GroupCode, m.Username;

IF @DryRun = 1
BEGIN
    PRINT N'DRY RUN — گروهی درج نشد. برای اجرا @DryRun = 0 بگذارید.';
    SELECT g.Code, g.DisplayName FROM @Groups g;
END
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;
        INSERT INTO system.NotificationGroupMember
            (NotificationGroupId, UserId, Email, FullName,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        SELECT r.GroupId, r.UserId, r.Email, r.FullName,
               1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
        FROM @Resolved r
        WHERE NOT EXISTS (
            SELECT 1 FROM system.NotificationGroupMember x
            WHERE x.NotificationGroupId = r.GroupId AND x.UserId = r.UserId);
        PRINT N'Inserted NotificationGroupMember rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
    END CATCH
END
