/*
================================================================================
Seed_OpenOrderRequest_NotificationGroupMembers.sql  —  WP8
================================================================================
اعضای گروه‌های اعلان درخواست‌های باز از لیست‌های هاردکد HTS / جاب فعلی.

تطبیق: Username بدون دامنه ↔ system.User.Username (بدون حساسیت به حروف).
Idempotent: عضو تکراری اضافه نمی‌شود؛ عضویت موجود حذف نمی‌شود.
@DryRun = 1 (پیش‌فرض): فقط شمارش و فهرست بی‌تطبیق؛ 0 = درج.

  گروه                              منبع
  --------------------------------  ------------------------------------------------
  SupplyUnit                        تدارکات HTS (Saemian + گیرنده‌های دایجست)
  Industrial                        ArrivalExtraEmails منهای Saemian + اسامی صنایع HTS
  ArrivalWarehouse                  ArrivalExtraEmails
  ArrivalPartial                    Saemian.r
  QcRejection                       QcRejectionExtraEmails
  UnsentDispatchDigest              Dordab.y + CC پنج‌نفره
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-oor-notificationgroup-members';

DECLARE @Map TABLE (GroupCode NVARCHAR(100), Username NVARCHAR(200), Source NVARCHAR(150));
INSERT INTO @Map (GroupCode, Username, Source) VALUES
 -- SupplyUnit
 (N'Sup.OpenOrderRequest.SupplyUnit', N'Saemian.r',      N'HTS تدارکات / رسید'),
 (N'Sup.OpenOrderRequest.SupplyUnit', N'Dordab.y',       N'HTS digest To'),
 (N'Sup.OpenOrderRequest.SupplyUnit', N'Yaltaghian.f',   N'HTS digest CC'),
 (N'Sup.OpenOrderRequest.SupplyUnit', N'Bagheri.h',      N'HTS digest CC'),
 (N'Sup.OpenOrderRequest.SupplyUnit', N'Shahpordeli.s',  N'HTS digest CC'),
 (N'Sup.OpenOrderRequest.SupplyUnit', N'Sohrabi.za',     N'HTS digest CC'),
 (N'Sup.OpenOrderRequest.SupplyUnit', N'Sharifi.b',      N'HTS digest CC'),
 -- Industrial
 (N'Sup.OpenOrderRequest.Industrial', N'Ahmadi.sh',      N'HTS ArrivalExtra'),
 (N'Sup.OpenOrderRequest.Industrial', N'Asadi.re',       N'HTS ArrivalExtra / industrial'),
 (N'Sup.OpenOrderRequest.Industrial', N'Moradmand.a',    N'HTS ArrivalExtra / industrial'),
 (N'Sup.OpenOrderRequest.Industrial', N'Azadbakhsh.s',   N'HTS ArrivalExtra / industrial'),
 (N'Sup.OpenOrderRequest.Industrial', N'Azadbakhsh.sh',  N'HTS ArrivalExtra / industrial'),
 (N'Sup.OpenOrderRequest.Industrial', N'zamani.z',       N'HTS ArrivalExtra'),
 (N'Sup.OpenOrderRequest.Industrial', N'Zandi.n',        N'HTS ArrivalExtra'),
 (N'Sup.OpenOrderRequest.Industrial', N'Eezi.a',         N'HTS ArrivalExtra / industrial'),
 (N'Sup.OpenOrderRequest.Industrial', N'khederzadeh.s',  N'HTS ArrivalExtra'),
 (N'Sup.OpenOrderRequest.Industrial', N'Khalili.aa',     N'HTS industrial static'),
 (N'Sup.OpenOrderRequest.Industrial', N'Mehrabi.s',      N'HTS industrial static'),
 (N'Sup.OpenOrderRequest.Industrial', N'Kian.r',         N'HTS industrial static'),
 -- ArrivalWarehouse
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'Ahmadi.sh',      N'HTS ArrivalExtraEmails'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'Asadi.re',       N'HTS ArrivalExtraEmails'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'Moradmand.a',    N'HTS ArrivalExtraEmails'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'Azadbakhsh.s',   N'HTS ArrivalExtraEmails'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'Azadbakhsh.sh',  N'HTS ArrivalExtraEmails'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'zamani.z',       N'HTS ArrivalExtraEmails'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'Zandi.n',        N'HTS ArrivalExtraEmails'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'Eezi.a',         N'HTS ArrivalExtraEmails'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'khederzadeh.s',  N'HTS ArrivalExtraEmails'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'Saemian.r',      N'HTS ArrivalExtraEmails'),
 -- ArrivalPartial
 (N'Sup.OpenOrderRequest.ArrivalPartial', N'Saemian.r', N'HTS partial receipt'),
 -- QcRejection
 (N'Sup.OpenOrderRequest.QcRejection', N'Salim.sh',           N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Bagheri.h',          N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Shahpordeli.s',      N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Sharifi.b',          N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Zabihian.s',         N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Dordab.y',           N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Hosseini.h',         N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Sohrabi.za',         N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Yaltaghian.f',       N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Daryaft',            N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Fazeli.e',           N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Tafakor.s',          N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Ahsani.e',           N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Kargar.m',           N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Ashrafi.m',          N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'azami.m',            N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Sarmadi.p',          N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Rajablou.a',         N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Bagheri.m',          N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Razaghmanesh.z',     N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Zamani.ar',          N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Banafshechin.y',     N'HTS QcRejectionExtraEmails'),
 (N'Sup.OpenOrderRequest.QcRejection', N'Mousavi.a',          N'HTS QcRejectionExtraEmails'),
 -- UnsentDispatchDigest
 (N'Sup.OpenOrderRequest.UnsentDispatchDigest', N'Dordab.y',      N'HTS digest To'),
 (N'Sup.OpenOrderRequest.UnsentDispatchDigest', N'Yaltaghian.f',  N'HTS digest CC'),
 (N'Sup.OpenOrderRequest.UnsentDispatchDigest', N'Bagheri.h',     N'HTS digest CC'),
 (N'Sup.OpenOrderRequest.UnsentDispatchDigest', N'Shahpordeli.s', N'HTS digest CC'),
 (N'Sup.OpenOrderRequest.UnsentDispatchDigest', N'Sohrabi.za',    N'HTS digest CC'),
 (N'Sup.OpenOrderRequest.UnsentDispatchDigest', N'Sharifi.b',     N'HTS digest CC');

DECLARE @Resolved TABLE (GroupId BIGINT, GroupCode NVARCHAR(100), UserId BIGINT, Email NVARCHAR(200), FullName NVARCHAR(200));
INSERT INTO @Resolved (GroupId, GroupCode, UserId, Email, FullName)
SELECT DISTINCT g.Id, g.Code, u.Id,
       COALESCE(NULLIF(u.Email, N''), u.Username + N'@havayar.com'),
       COALESCE(NULLIF(u.NameFa, N''), NULLIF(u.Name, N''), u.Username)
FROM @Map m
INNER JOIN system.NotificationGroup g ON g.Code = m.GroupCode
INNER JOIN system.[User] u ON LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1;

PRINT N'--- Summary (' + CASE WHEN @DryRun = 1 THEN N'DRY RUN' ELSE N'APPLY' END + N') ---';
SELECT m.GroupCode, COUNT(*) AS Mapped, COUNT(r.UserId) AS Resolved
FROM (SELECT DISTINCT GroupCode, Username FROM @Map) m
LEFT JOIN @Resolved r ON r.GroupCode = m.GroupCode AND r.UserId IN (
    SELECT u.Id FROM system.[User] u WHERE LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1)
GROUP BY m.GroupCode
ORDER BY m.GroupCode;

SELECT DISTINCT m.GroupCode, m.Username, m.Source AS Unmatched
FROM @Map m
WHERE NOT EXISTS (SELECT 1 FROM system.[User] u WHERE LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1)
ORDER BY m.GroupCode, m.Username;

IF @DryRun = 1
BEGIN
    PRINT N'DRY RUN — هیچ عضوی درج نشد. برای اجرا @DryRun = 0 بگذارید.';
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
        PRINT N'=== DONE Seed_OpenOrderRequest_NotificationGroupMembers ===';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();
        RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
    END CATCH
END

SELECT g.Code, COUNT(x.Id) AS Members
FROM system.NotificationGroup g
LEFT JOIN system.NotificationGroupMember x ON x.NotificationGroupId = g.Id AND x.IsActive = 1
WHERE g.Code LIKE N'Sup.OpenOrderRequest%'
GROUP BY g.Code
ORDER BY g.Code;
