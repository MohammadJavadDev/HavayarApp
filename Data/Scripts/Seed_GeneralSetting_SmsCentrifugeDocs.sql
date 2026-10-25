/*
  Seed_GeneralSetting_SmsCentrifugeDocs.sql
  تنظیمات پیامک «لینک مدارک سانتریفیوژ» (جایگزین کار SQL راهکاران، نوع 2924).
  Idempotent. UTF-8 with BOM؛ با sqlcmd -x -f 65001 اجرا شود.
  پیش‌فرض خاموش است (Enabled=false) و StartFrom خالی. برای روشن کردن:
    1) کار قدیمی راهکاران (INSERT INTO [TMS].[TotalSystem].[dbo].[Gnr_SmsBucketList] ... 2924) را غیرفعال کنید؛
    2) Sms:CentrifugeDocs:StartFrom را روی زمان فعلی بگذارید (مثلاً 2026-10-17 08:00)؛
    3) Sms:CentrifugeDocs:Enabled را true کنید.
*/
SET NOCOUNT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);

DECLARE @Seed TABLE (SettingKey nvarchar(150), SettingValue nvarchar(max), Title nvarchar(200), Description nvarchar(1000));
INSERT INTO @Seed VALUES
 (N'Sms:CentrifugeDocs:Enabled', N'false', N'ارسال پیامک لینک مدارک سانتریفیوژ فعال باشد', N'true/false. فقط بعد از غیرفعال کردن کار SQL قدیمی راهکاران (ساخت ردیف نوع 2924) روشن شود، وگرنه مشتری دو پیامک می‌گیرد.'),
 (N'Sms:CentrifugeDocs:StartFrom', NULL, N'شروع پیامک لینک مدارک سانتریفیوژ از (تاریخ و ساعت)', N'قالب yyyy-MM-dd HH:mm (مثلاً 2026-10-17 08:00). فقط سفارش‌های ساخت که بعد از این زمان تایید مالی شده‌اند پیامک می‌گیرند؛ جلوی ارسال برای سفارش‌های قدیمی را می‌گیرد.'),
 (N'Sms:CentrifugeDocs:MaxAgeDays', N'3', N'حداکثر روز بعد از تایید برای ارسال لینک مدارک سانتریفیوژ', NULL);

INSERT INTO system.GeneralSetting (SettingKey, SettingValue, Title, Description, IsSecret, IsActive, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
SELECT s.SettingKey, s.SettingValue, s.Title, s.Description, 0, 1, @Now, @NowShamsi
FROM @Seed s
WHERE NOT EXISTS (SELECT 1 FROM system.GeneralSetting g WHERE g.SettingKey = s.SettingKey);
PRINT N'GeneralSetting rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

SELECT SettingKey, SettingValue FROM system.GeneralSetting WHERE SettingKey LIKE N'Sms:CentrifugeDocs:%';