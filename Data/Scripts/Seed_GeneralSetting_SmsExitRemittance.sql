/*
  Seed_GeneralSetting_SmsExitRemittance.sql
  تنظیمات پیامک «خروج از کارخانه» (جایگزین کار SQL راهکاران، نوع 2746).
  Idempotent. UTF-8 with BOM؛ با sqlcmd -x -f 65001 اجرا شود.
  پیش‌فرض خاموش است. برای روشن کردن:
    1) کار قدیمی راهکاران (INSERT INTO [TMS].[TotalSystem].[dbo].[Gnr_SmsBucketList] ... 2746 / IssuePermit) را غیرفعال کنید؛
    2) Sms:ExitRemittance:StartFrom را روی امروز بگذارید (yyyy-MM-dd)؛ فقط حواله‌های با تاریخ از این روز به بعد؛
    3) Sms:ExitRemittance:Enabled را true کنید.
*/
SET NOCOUNT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);

DECLARE @Seed TABLE (SettingKey nvarchar(150), SettingValue nvarchar(max), Title nvarchar(200), Description nvarchar(1000));
INSERT INTO @Seed VALUES
 (N'Sms:ExitRemittance:Enabled', N'false', N'ارسال پیامک خروج از کارخانه فعال باشد', N'true/false. فقط بعد از غیرفعال کردن کار SQL قدیمی راهکاران (ساخت ردیف نوع 2746) روشن شود، وگرنه مشتری دو پیامک می‌گیرد.'),
 (N'Sms:ExitRemittance:StartFrom', NULL, N'پیامک خروج از کارخانه: فقط حواله‌های با تاریخ از این روز به بعد', N'قالب yyyy-MM-dd (مثلاً 2026-10-16). جلوی ارسال برای حواله‌های قدیمی را می‌گیرد. اگر خالی یا نامعتبر باشد چیزی ارسال نمی‌شود.'),
 (N'Sms:ExitRemittance:DelayDays', N'3', N'پیامک خروج از کارخانه: چند روز بعد از تاریخ حواله ارسال شود', N'در راهکاران قدیم: حواله‌ای که بیش از ۲ روز از تاریخش گذشته (از روز سوم به بعد).');

INSERT INTO system.GeneralSetting (SettingKey, SettingValue, Title, Description, IsSecret, IsActive, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
SELECT s.SettingKey, s.SettingValue, s.Title, s.Description, 0, 1, @Now, @NowShamsi
FROM @Seed s
WHERE NOT EXISTS (SELECT 1 FROM system.GeneralSetting g WHERE g.SettingKey = s.SettingKey);
PRINT N'GeneralSetting rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

SELECT SettingKey, SettingValue FROM system.GeneralSetting WHERE SettingKey LIKE N'Sms:ExitRemittance:%';