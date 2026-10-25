/*
  Seed_SmsRecipientGroups_Members.sql
  اعضای گروه‌های اعلان پیامک، منتقل‌شده از گروه‌های کاربری HTS (Gnr_UserGroupMember) با تطبیق Username:
    Srv.PmRequest.Factory          <- گروه 269  (sohrabi.z, pm, mahmoodi.j؛ فقط پرسنل شعبه 2 پیامک می‌گیرند)
    Sale.TechnicalQuery.Technician <- گروه 450  (TQ_TechnicalTechnician_UserGroup؛ ۹ نفر)
  Idempotent. UTF-8 with BOM؛ با sqlcmd -x -f 65001 اجرا شود.
  توجه: پیامک فقط به اعضایی می‌رسد که User.PartyId آن‌ها به Hcm.Personel (دارای Mobile) وصل باشد.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-sms-group-members';

DECLARE @Map TABLE (GroupCode NVARCHAR(100), Username NVARCHAR(160));
INSERT INTO @Map VALUES
 (N'Srv.PmRequest.Factory', N'sohrabi.z'),
 (N'Srv.PmRequest.Factory', N'pm'),
 (N'Srv.PmRequest.Factory', N'mahmoodi.j'),
 (N'Sale.TechnicalQuery.Technician', N'bagheri.h'),
 (N'Sale.TechnicalQuery.Technician', N'moeini.r'),
 (N'Sale.TechnicalQuery.Technician', N'salim.sh'),
 (N'Sale.TechnicalQuery.Technician', N'sharifi.m'),
 (N'Sale.TechnicalQuery.Technician', N'golestaneh.m'),
 (N'Sale.TechnicalQuery.Technician', N'kargar.m'),
 (N'Sale.TechnicalQuery.Technician', N'kadkhodaei.v'),
 (N'Sale.TechnicalQuery.Technician', N'khodakarami.f'),
 (N'Sale.TechnicalQuery.Technician', N'javadian.p');

INSERT INTO system.NotificationGroupMember
    (NotificationGroupId, UserId, Email, FullName,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT g.Id, u.Id, ISNULL(u.Email, N''), COALESCE(NULLIF(u.NameFa, N''), NULLIF(u.Name, N''), u.Username),
       1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM @Map m
JOIN system.NotificationGroup g ON g.Code = m.GroupCode
JOIN system.[User] u ON LOWER(u.Username) = LOWER(m.Username)
WHERE NOT EXISTS (SELECT 1 FROM system.NotificationGroupMember x WHERE x.NotificationGroupId = g.Id AND x.UserId = u.Id);
PRINT N'NotificationGroupMember rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

SELECT g.Code, u.Username, m.FullName, p.Mobile, p.BranchCode,
       CASE WHEN p.Id IS NULL THEN N'بدون پرسنل (پیامک نمی‌رسد)' ELSE N'ok' END AS Linked
FROM system.NotificationGroupMember m
JOIN system.NotificationGroup g ON g.Id = m.NotificationGroupId
JOIN system.[User] u ON u.Id = m.UserId
LEFT JOIN Hcm.Personel p ON p.PartyId = u.PartyId
WHERE g.Code IN (N'Srv.PmRequest.Factory', N'Sale.TechnicalQuery.Technician')
ORDER BY g.Code, u.Username;