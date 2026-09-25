/* =============================================================================
   Seed_OpenOrderRequestEmailAttachmentJob_Schedule.sql

   زمان‌بندی روزانه جاب App.BackgroundJob.Jobs.Sup.OpenOrderRequestEmailAttachmentJob:

     SyncEmailToSupplierAttachmentsFromHts → روزانه 07:05
     کپی فایل‌های پیوست پیشینه ارسال به پیمانکار از HTS به FileEntity
     (پنج دقیقه بعد از Def 41 SyncOpenOrderRequestAttachmentsFromHts در 07:00)

   شکل ردیف عین ردیف Def 41:
     ScheduleType = 1 (Daily), IntervalSeconds = 86400, DailyIntervalDays = 1,
     LastStatus = 0, IsActive = 1.

   Idempotent: JobDefinition فقط اگر JobId نبود؛ JobSchedule فقط اگر برای آن تعریف ردیفی نبود.

   ترتیب اجرا (مهم):
     ابتدا نسخه جدید App.BackgroundJob (شامل SyncEmailToSupplierAttachmentsFromHts) مستقر و یک بار
     اجرا شود، سپس این اسکریپت. JobDiscoveryService در استارت، JobDefinition بدون متد [JobHandler]
     را همراه با JobSchedule پاک می‌کند.
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @ClassType    nvarchar(500) = N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestEmailAttachmentJob';
DECLARE @AssemblyName nvarchar(500) =
(
    SELECT TOP (1) d.AssemblyName
      FROM system.JobDefinition d
     WHERE d.ClassType = N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestEmailAttachmentJob'
        OR d.ClassType = N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestJob'
);
IF @AssemblyName IS NULL
    SET @AssemblyName = N'App.BackgroundJob, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null';

DECLARE @Jobs TABLE
(
    JobId       nvarchar(500) NOT NULL,
    MethodName  nvarchar(200) NOT NULL,
    DisplayName nvarchar(500) NOT NULL,
    Description nvarchar(max) NOT NULL,
    DailyTime   time(7)       NOT NULL
);

INSERT INTO @Jobs (JobId, MethodName, DisplayName, Description, DailyTime)
VALUES
(
    N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestEmailAttachmentJob.SyncEmailToSupplierAttachmentsFromHts',
    N'SyncEmailToSupplierAttachmentsFromHts',
    N'کپی پیوست‌های پیشینه ارسال به پیمانکار از HTS',
    N'روزانه ۰۷:۰۵ — یک‌باره / تکراری امن: برای ردیف‌های EmailToSupplier که HtsId دارند و ستون پیوست خالی است، فایل را از HTS می‌خواند و UploadAsync می‌کند (یا از پیوست/مدرک/کالای از قبل مهاجرت‌شده لینک می‌گیرد).',
    '07:05:00'
);

INSERT INTO system.JobDefinition (JobId, DisplayName, Description, AssemblyName, ClassType, MethodName)
SELECT j.JobId, j.DisplayName, j.Description, @AssemblyName, @ClassType, j.MethodName
  FROM @Jobs j
 WHERE NOT EXISTS (SELECT 1 FROM system.JobDefinition d WHERE d.JobId = j.JobId);

DECLARE @Today datetime2 = CAST(CAST(SYSDATETIME() AS date) AS datetime2);

INSERT INTO system.JobSchedule
    (JobId, IsActive, IntervalSeconds, NextRunTime, LastRunTime, LastStatus,
     ScheduleType, DailyIntervalDays, WeeklyDays, DailyTime, HourlyMinute)
SELECT d.Id,
       1,
       86400,
       CASE
            WHEN DATEADD(SECOND, DATEDIFF(SECOND, '00:00:00', j.DailyTime), @Today) > SYSDATETIME()
            THEN DATEADD(SECOND, DATEDIFF(SECOND, '00:00:00', j.DailyTime), @Today)
            ELSE DATEADD(DAY, 1, DATEADD(SECOND, DATEDIFF(SECOND, '00:00:00', j.DailyTime), @Today))
       END,
       NULL,
       0,
       1,
       1,
       NULL,
       j.DailyTime,
       0
  FROM @Jobs j
  JOIN system.JobDefinition d ON d.JobId = j.JobId
 WHERE NOT EXISTS (SELECT 1 FROM system.JobSchedule s WHERE s.JobId = d.Id);

COMMIT;

SELECT d.Id AS JobDefinitionId, d.JobId, d.MethodName, d.DisplayName,
       s.Id AS ScheduleId, s.IsActive, s.ScheduleType, s.IntervalSeconds, s.DailyTime, s.NextRunTime, s.LastStatus
  FROM system.JobDefinition d
  LEFT JOIN system.JobSchedule s ON s.JobId = d.Id
 WHERE d.JobId = N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestEmailAttachmentJob.SyncEmailToSupplierAttachmentsFromHts'
 ORDER BY s.DailyTime;
