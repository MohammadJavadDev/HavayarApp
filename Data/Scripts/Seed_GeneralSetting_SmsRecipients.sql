/*
  Seed_GeneralSetting_SmsRecipients.sql
  گیرنده‌های ثابت پیامک (جایگزین شماره‌های هاردکد سیستم قبلی) + گروه‌های اعلان پیامک.
  Idempotent. UTF-8 with BOM؛ با sqlcmd -x -f 65001 اجرا شود (متن اسکریپت‌های جاوااسکریپت $( ... ) ندارد، ولی -x بی‌خطر است).
  شماره‌ها را بعداً از «تنظیمات عمومی» ویرایش کنید. اعضای گروه‌ها را از «گروه‌های اعلان» اضافه کنید.

  Code گروه                         معادل HTS
  -------------------------------   ---------------------------------------------
  Srv.PmRequest.Factory             گروه کاربری 269 (پیامک درخواست PM کارخانه)
  Sale.TechnicalQuery.Technician    گروه تکنسین‌های پرسش فنی (TQ_TechnicalTechnician_UserGroup)
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-sms-recipients';

DECLARE @Seed TABLE (SettingKey nvarchar(150), SettingValue nvarchar(max), Title nvarchar(200), Description nvarchar(1000), IsSecret bit);
INSERT INTO @Seed VALUES
 (N'Sms:TestRedirectMobile', NULL, N'شماره آزمایشی (هدایت همه پیامک‌ها)', N'فقط برای تست. اگر پر باشد همه پیامک‌ها به این شماره می‌روند و شماره اصلی در «نام گیرنده» ثبت می‌شود. در حالت عادی خالی بماند.', 0),
 (N'Sms:Recipients:PmRequestCentral', N'09352201099', N'گیرنده پیامک درخواست PM (دفتر مرکزی)', NULL, 0),
 (N'Sms:Recipients:PmRequestFactoryExtra', N'09123670651;09191030853;09120218648', N'گیرندگان ثابت اضافه پیامک درخواست PM (کارخانه)', NULL, 0),
 (N'Sms:Recipients:ProdStatus:FinalInspectionStop', N'09122626827;09027406983;09366709846;09372074047;09195791032;09101620806;09108883279;09168552430;09398132373;09116933994', N'گیرندگان پیامک وضعیت سفارش ساخت: شروع توقف بازرسی نهایی', NULL, 0),
 (N'Sms:Recipients:ProdStatus:ProductionAndFinal', N'09122626827;09168552430;09398132373;09116933994', N'گیرندگان پیامک وضعیت سفارش ساخت: پایان تولید / شروع تست / توقف تست', NULL, 0),
 (N'Sms:Recipients:ProdStatus:TestEndAndInspectionStopEnd', N'09122626827;09168552430;09398132373;09372074047;09195791032;09108883279;09116933994', N'گیرندگان پیامک وضعیت سفارش ساخت: پایان تست / پایان توقف بازرسی', NULL, 0),
 (N'Sms:Recipients:ProdStatus:InspectionStartAndPackaging', N'09122626827;09372074047;09195791032;09108883279', N'گیرندگان پیامک وضعیت سفارش ساخت: شروع بازرسی / پایان بسته‌بندی', NULL, 0),
 (N'Sms:Recipients:ProdStatus:QcEmployees', N'09128617076;09195791030;09101620806;09358886419;09127034143;09123622319;09356898330', N'گیرندگان پیامک وضعیت سفارش ساخت: کارکنان کنترل کیفیت (به همه وضعیت‌ها اضافه می‌شود)', NULL, 0);

INSERT INTO system.GeneralSetting (SettingKey, SettingValue, Title, Description, IsSecret, IsActive, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
SELECT s.SettingKey, s.SettingValue, s.Title, s.Description, s.IsSecret, 1, @Now, @NowShamsi
FROM @Seed s
WHERE NOT EXISTS (SELECT 1 FROM system.GeneralSetting g WHERE g.SettingKey = s.SettingKey);
PRINT N'GeneralSetting rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

DECLARE @Groups TABLE (Code NVARCHAR(100), DisplayName NVARCHAR(200), Description NVARCHAR(1500));
INSERT INTO @Groups (Code, DisplayName, Description) VALUES
 (N'Srv.PmRequest.Factory', N'خدمات - درخواست PM - پیامک کارخانه', N'گیرندگان پیامک ثبت درخواست PM کارکنان کارخانه (معادل گروه کاربری 269 در HTS؛ فقط پرسنل شعبه کارخانه پیامک می‌گیرند)'),
 (N'Sale.TechnicalQuery.Technician', N'پرسش فنی - تکنسین‌ها - پیامک', N'گیرندگان پیامک تایید پرسش فنی جدید (معادل گروه تکنسین‌های TQ در HTS)');

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

SELECT SettingKey, SettingValue FROM system.GeneralSetting WHERE SettingKey LIKE N'Sms:Recipients:%' OR SettingKey = N'Sms:TestRedirectMobile' ORDER BY Id;
SELECT n.Id, n.Code, n.DisplayName FROM system.NotificationGroup n WHERE n.Code IN (SELECT Code FROM @Groups);