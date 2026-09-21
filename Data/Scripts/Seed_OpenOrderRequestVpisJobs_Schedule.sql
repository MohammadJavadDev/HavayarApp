/* =============================================================================
   Seed_OpenOrderRequestVpisJobs_Schedule.sql  —  WP7 / D11 / D36 (Q8-a)

   زمان‌بندی روزانه دو جاب کلاس App.BackgroundJob.Jobs.Sup.OpenOrderRequestJob:

     1) SyncOpenOrderRequestVpisFromHts          → روزانه 07:02
        پل لینک‌های VPIS غایب از HTS تا خاموشی HTS + درج خودکار محلی
        (یک دقیقه بعد از Def 41 SyncOpenOrderRequestAttachmentsFromHts در 07:00)
     2) CheckVpisRevisionChangesAndNotify        → روزانه 07:30
        جاب موجود که تا امروز JobSchedule نداشت (D11)
        (پس از Def 48 SyncProjectVpisFromHts در 07:00)

   شکل ردیف‌ها عین ردیف Def 41:
     ScheduleType = 1 (Daily), IntervalSeconds = 86400, DailyIntervalDays = 1,
     LastStatus = 0, IsActive = 1.

   Idempotent: JobDefinition فقط اگر JobId نبود؛ JobSchedule فقط اگر برای آن تعریف ردیفی نبود.

   ترتیب اجرا (مهم):
     ابتدا نسخه جدید App.BackgroundJob (شامل SyncOpenOrderRequestVpisFromHts) مستقر و یک بار
     اجرا شود، سپس این اسکریپت. JobDiscoveryService در استارت، JobDefinition بدون متد [JobHandler]
     را همراه با JobSchedule پاک می‌کند.

   Q10: جاب ایمیل (Def 38) فعال نمی‌شود.
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @ClassType    nvarchar(500) = N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestJob';
DECLARE @AssemblyName nvarchar(500) =
(
    SELECT TOP (1) d.AssemblyName
      FROM system.JobDefinition d
     WHERE d.ClassType = N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestJob'
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
    N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestJob.SyncOpenOrderRequestVpisFromHts',
    N'SyncOpenOrderRequestVpisFromHts',
    N'همگام‌سازی لینک VPIS درخواست‌های باز از HTS',
    N'روزانه ۰۷:۰۲ — پل روزانه از TotalSystem.dbo.Sup_OpenOrderRequestVpis تا خاموشی HTS. لینک‌های غایب را با تطبیق درخواست (PurchaseRequestItemId+OrderRowId) و مدرک/پروژه (HtsId) کپی می‌کند؛ سپس درج خودکار محلی («درج بصورت اتوماتیک توسط سیستم») را اجرا می‌کند.',
    '07:02:00'
),
(
    N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestJob.CheckVpisRevisionChangesAndNotify',
    N'CheckVpisRevisionChangesAndNotify',
    N'بررسی تغییر Revision مدارک VPIS و اعلام به واحد فروش/پروژه',
    N'روزانه ۰۷:۳۰ — متد از قبل موجود بود ولی JobSchedule نداشت (D11). اگر رویژن مدرک لینک‌شده تغییر کند، لینک جدید IsLatest، تایید فروش پاک و اعلان به کارشناس/مدیر فروش/مدیر پروژه ثبت می‌شود.',
    '07:30:00'
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

SELECT d.Id AS JobDefinitionId, d.JobId, d.DisplayName,
       s.Id AS ScheduleId, s.IsActive, s.ScheduleType, s.DailyTime, s.NextRunTime, s.LastStatus
  FROM system.JobDefinition d
  LEFT JOIN system.JobSchedule s ON s.JobId = d.Id
 WHERE d.JobId IN (
        N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestJob.SyncOpenOrderRequestVpisFromHts',
        N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestJob.CheckVpisRevisionChangesAndNotify')
 ORDER BY s.DailyTime;
