/*
================================================================================
Seed_ProductionOrderItem_NotificationGroupMembers.sql
================================================================================
اعضای گروه‌های اعلان سفارش ساخت — استخراج‌شده از سیستم قدیم (TotalSystem، 2026-09-17):
  Gnr_UserGroupMember (فقط کاربران فعال) + لیست‌های ثابت ایمیل داخل کد HTS.

  گروه پنل                                          منبع HTS
  ------------------------------------------------  -------------------------------------------------------------
  Sale.ProductionOrderItem.SupplyCommitteeExperts   Gnr_UserGroup 380 «کارشناسان رئیس کمیته تامین (سفارش ساخت)»
  Sale.ProductionOrderItem.TestStart                Gnr_UserGroup 540 «دریافت‌کنندگان ایمیل شروع تست سفارش ساخت غیرروتین»
  Sale.ProductionOrderItem.NearDeliveryDate         Gnr_UserGroup 567 «دریافت‌کنندگان ایمیل هشدار کالاهای نزدیک به موعد تحویل»
  Sale.ProductionOrderItem.SerialChangeQc           گیرندگان واقعی SendChengedSerialNotificationEmail (maroufkhani.m، badrkhani.s)
                                                    (گروه 538 در آن کد خوانده می‌شد ولی استفاده نمی‌شد — گروه انبار/بازرسی ورودی است)
  Sale.ProductionOrderItem.Industrial               GetIndustrialStaticReceivers ∪ گیرندگان isSendToPlanningUnitMode (InquiryService)
  Sale.ProductionOrderItem.ProductionStatusChange   GetChangeStatusStaticReceivers ∪ Gnr_UserGroup 591 «دریافت‌کنندگان اعلانات
                                                    تغییرات در قلم سفارش ساخت» (CC پیش‌فرض فرم تغییر وضعیت HTS)

تطبیق کاربر: HTS Gnr_User.ActiveDirectoryUsername ↔ system.User.Username (بدون حساسیت به حروف).
Idempotent: عضو تکراری اضافه نمی‌شود؛ کاربران پیدانشده در خروجی گزارش می‌شوند.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-notificationgroup-members';

DECLARE @Map TABLE (GroupCode NVARCHAR(100), Username NVARCHAR(200), Source NVARCHAR(100));
INSERT INTO @Map (GroupCode, Username, Source) VALUES
 -- 380 کارشناسان رئیس کمیته تامین
 (N'Sale.ProductionOrderItem.SupplyCommitteeExperts', N'shirazi.m',          N'HTS group 380'),
 -- 540 شروع تست (فقط فعال‌ها)
 (N'Sale.ProductionOrderItem.TestStart', N'Ahsani.e',     N'HTS group 540'),
 (N'Sale.ProductionOrderItem.TestStart', N'bagheri.h',    N'HTS group 540'),
 (N'Sale.ProductionOrderItem.TestStart', N'ebrahimi.f',   N'HTS group 540'),
 (N'Sale.ProductionOrderItem.TestStart', N'golestaneh.m', N'HTS group 540'),
 (N'Sale.ProductionOrderItem.TestStart', N'kargar.m',     N'HTS group 540'),
 (N'Sale.ProductionOrderItem.TestStart', N'm.nikpour',    N'HTS group 540'),
 (N'Sale.ProductionOrderItem.TestStart', N'mahani.b',     N'HTS group 540'),
 (N'Sale.ProductionOrderItem.TestStart', N'moeini.r',     N'HTS group 540'),
 -- 567 نزدیک به موعد تحویل
 (N'Sale.ProductionOrderItem.NearDeliveryDate', N'sohrabi.z', N'HTS group 567'),
 -- تغییر سریال (گیرندگان واقعی کد HTS)
 (N'Sale.ProductionOrderItem.SerialChangeQc', N'maroufkhani.m', N'HTS SendChengedSerialNotificationEmail To'),
 (N'Sale.ProductionOrderItem.SerialChangeQc', N'badrkhani.s',   N'HTS SendChengedSerialNotificationEmail CC'),
 -- صنایع: GetIndustrialStaticReceivers ∪ isSendToPlanningUnitMode
 (N'Sale.ProductionOrderItem.Industrial', N'Khalili.aa',    N'HTS static industrial'),
 (N'Sale.ProductionOrderItem.Industrial', N'Mehrabi.s',     N'HTS static industrial'),
 (N'Sale.ProductionOrderItem.Industrial', N'Eezi.a',        N'HTS static industrial'),
 (N'Sale.ProductionOrderItem.Industrial', N'Azadbakhsh.s',  N'HTS static industrial'),
 (N'Sale.ProductionOrderItem.Industrial', N'Moradmand.a',   N'HTS static industrial'),
 (N'Sale.ProductionOrderItem.Industrial', N'Asadi.re',      N'HTS static industrial'),
 (N'Sale.ProductionOrderItem.Industrial', N'Azadbakhsh.sh', N'HTS static industrial'),
 (N'Sale.ProductionOrderItem.Industrial', N'Kian.r',        N'HTS static industrial'),
 (N'Sale.ProductionOrderItem.Industrial', N'Saemian.r',     N'HTS static industrial'),
 (N'Sale.ProductionOrderItem.Industrial', N'zamani.z',      N'HTS InquiryService planning-unit'),
 (N'Sale.ProductionOrderItem.Industrial', N'Zandi.n',       N'HTS InquiryService planning-unit'),
 (N'Sale.ProductionOrderItem.Industrial', N'Mohammadi.ma',  N'HTS InquiryService planning-unit'),
 (N'Sale.ProductionOrderItem.Industrial', N'kaboli.f',      N'HTS InquiryService planning-unit'),
 (N'Sale.ProductionOrderItem.Industrial', N'Asgari.sh',     N'HTS InquiryService planning-unit'),
 -- تغییر وضعیت تولید: GetChangeStatusStaticReceivers
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Ahmadi.sh',        N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Asadi.re',         N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Rezaei.r',         N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Sadeghpour.j',     N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Bakhtiari.a',      N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Robatsarpoushi.a', N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Sharifi.f',        N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Khanipour.p',      N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'shokrgozar.m',     N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Estaki.a',         N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Eftekhari.a',      N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'mosayebi.m',       N'HTS static change-status'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Ahrarnejad.h',     N'HTS static change-status'),
 -- تغییر وضعیت تولید: گروه 591 (CC پیش‌فرض فرم HTS — فقط فعال‌ها)
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Ahmadi.sh',        N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Azadbakhsh.s',     N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'badrkhani.s',      N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'bagheri.m',        N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Bamimohammadi.gh', N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'eezi.a',           N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Eftekhari.a',      N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Erfani.m',         N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'estaki.a',         N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'golestaneh.a',     N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'javadian.p',       N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'kadkhodaei.v',     N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'khalili.aa',       N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'khodakarami.f',    N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'kian.r',           N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Kondori.v',        N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'mehrabi.s',        N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Saeedi.m',         N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Saemian.r',        N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'salim.sh',         N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'Samii.m',          N'HTS group 591'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'sohrabi.z',        N'HTS group 591');

BEGIN TRY
    BEGIN TRANSACTION;

    -- تطبیق با کاربران پنل (فقط فعال)
    DECLARE @Resolved TABLE (GroupId BIGINT, GroupCode NVARCHAR(100), UserId BIGINT, Email NVARCHAR(200), FullName NVARCHAR(200));
    INSERT INTO @Resolved (GroupId, GroupCode, UserId, Email, FullName)
    SELECT DISTINCT g.Id, g.Code, u.Id,
           COALESCE(NULLIF(u.Email, N''), u.Username + N'@havayar.com'),
           COALESCE(NULLIF(u.NameFa, N''), NULLIF(u.Name, N''), u.Username)
    FROM @Map m
    INNER JOIN system.NotificationGroup g ON g.Code = m.GroupCode
    INNER JOIN system.[User] u ON LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1;

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
    PRINT N'  Inserted NotificationGroupMember rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_ProductionOrderItem_NotificationGroupMembers ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-- کاربران HTS که در پنل پیدا نشدند (یا غیرفعال‌اند) — باید دستی بررسی شوند
SELECT DISTINCT m.GroupCode, m.Username, m.Source
FROM @Map m
WHERE NOT EXISTS (SELECT 1 FROM system.[User] u WHERE LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1)
ORDER BY m.GroupCode, m.Username;

-- خلاصه اعضا
SELECT g.Code, COUNT(x.Id) AS Members
FROM system.NotificationGroup g
LEFT JOIN system.NotificationGroupMember x ON x.NotificationGroupId = g.Id AND x.IsActive = 1
WHERE g.Code LIKE N'Sale.ProductionOrderItem%'
GROUP BY g.Code
ORDER BY g.Code;
