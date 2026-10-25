-- Seed: system.GeneralSetting کلیدهای پیامک (idempotent). مقدار ApiKey عمداً خالی است؛ بعد از اجرا در دیتابیس تنظیم شود.
SET NOCOUNT ON;

DECLARE @Seed TABLE (SettingKey nvarchar(150), SettingValue nvarchar(max), Title nvarchar(200), Description nvarchar(1000), IsSecret bit);

INSERT INTO @Seed VALUES
 (N'Sms:Enabled',            N'true',                      N'ارسال پیامک فعال باشد', N'true/false. اگر false باشد پیامک‌ها در صف می‌مانند و ارسال نمی‌شوند.', 0),
 (N'Sms:Provider',           N'SmsIr',                     N'پنل پیامکی', N'فعلاً فقط SmsIr پشتیبانی می‌شود.', 0),
 (N'Sms:SmsIr:BaseUrl',      N'https://api.sms.ir/v1',     N'آدرس API پنل sms.ir', NULL, 0),
 (N'Sms:SmsIr:ApiKey',       NULL,                         N'کلید API پنل sms.ir', NULL, 1),
 (N'Sms:SmsIr:LineNumber',   N'30002128765432',            N'شماره خط ارسال‌کننده', NULL, 0),
 (N'Sms:MaxAttempts',        N'3',                         N'حداکثر تعداد تلاش ارسال', NULL, 0),
 (N'Sms:RetryDelayMinutes',  N'5',                         N'فاصله تلاش مجدد (دقیقه)', N'فاصله تلاش n-ام برابر n × این مقدار است.', 0),
 (N'Sms:BatchSize',          N'100',                       N'تعداد پیامک در هر اجرای job', NULL, 0),
 (N'Sms:DeliveryBatchSize',  N'200',                       N'تعداد استعلام دلیوری در هر اجرا', NULL, 0),
 (N'Sms:DeliveryCheckHours', N'48',                        N'مدت پیگیری وضعیت دلیوری (ساعت)', N'بعد از این مدت از زمان ارسال، استعلام دلیوری متوقف می‌شود.', 0);

INSERT INTO system.GeneralSetting (SettingKey, SettingValue, Title, Description, IsSecret, IsActive, CreatedOnMiladiDateTime)
SELECT s.SettingKey, s.SettingValue, s.Title, s.Description, s.IsSecret, 1, GETDATE()
FROM @Seed s
WHERE NOT EXISTS (SELECT 1 FROM system.GeneralSetting g WHERE g.SettingKey = s.SettingKey);

SELECT SettingKey, CASE WHEN IsSecret = 1 THEN N'***' ELSE SettingValue END AS SettingValue, Title FROM system.GeneralSetting ORDER BY Id;