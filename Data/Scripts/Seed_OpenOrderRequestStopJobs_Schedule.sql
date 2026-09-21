/* =============================================================================
   Seed_OpenOrderRequestStopJobs_Schedule.sql  —  WP3 / D7 (درخواست‌های باز — جاب‌های روزانه توقف)

   زمان‌بندی روزانه سه جاب کلاس App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob
   (معادل HtsTaskService.SupplysSystemTask در HTS که هر روز ۰۹:۳۵ اجرا می‌شد):

     1) SendStopDeadlineReminders  →  روزانه 09:35  (یادآوری پایان مهلت پاسخگویی عامل توقف — فقط روز مهلت)
     2) EscalateStaleStops         →  روزانه 09:36  (بیش از ۱۴ روز بدون اقدام ⇒ StopStatus = 2821 + اعلان تدارکات)
     3) SendUnsentDispatchDigest   →  روزانه 09:37  (دایجست «تامین درخواست های برگ ارسال نخورده»)
        * یک دقیقه بعد از یادآوری، مطابق الگوی ردیف‌های 07:00 / 07:01 موجود (Def 41 و Def 45) تا لاگ‌ها تفکیک‌شده باشند.

   شکل ردیف‌ها عین ردیف Def 41 «SyncOpenOrderRequestAttachmentsFromHts» (روزانه 07:00):
     ScheduleType = 1 (Daily), IntervalSeconds = 86400, DailyIntervalDays = 1, HourlyMinute = 0,
     WeeklyDays = NULL, LastStatus = 0 (Idle), IsActive = 1, NextRunTime = اولین 09:35 آینده.

   Idempotent:
     - system.JobDefinition: اگر ردیف با همان JobId نبود درج می‌شود (JobDiscoveryService در استارت بعدی
       App.BackgroundJob همان JobId را پیدا می‌کند و فقط DisplayName/Description را همگام می‌کند).
     - system.JobSchedule: فقط اگر برای آن JobDefinition هیچ ردیفی وجود نداشته باشد درج می‌شود.
     - اجرای مجدد اسکریپت هیچ ردیف تکراری نمی‌سازد و زمان‌بندی‌های موجود را دست نمی‌زند.

   ترتیب اجرا (مهم):
     ابتدا نسخه جدید App.BackgroundJob (شامل OpenOrderRequestStopJob) مستقر و یک بار اجرا شود، سپس این اسکریپت.
     دلیل: JobDiscoveryService در استارت، هر JobDefinition‌ای که متد [JobHandler] متناظر در اسمبلی نداشته باشد را
     همراه با JobSchedule‌هایش حذف می‌کند؛ اگر اسکریپت قبل از استقرار نسخه جدید اجرا و سرویس قدیمی ری‌استارت شود،
     ردیف‌ها حذف می‌شوند.

   غیرفعال‌سازی (مثلاً تا go-live یا در صورت نیاز):
     UPDATE s SET s.IsActive = 0
       FROM system.JobSchedule s
       JOIN system.JobDefinition d ON d.Id = s.JobId
      WHERE d.JobId IN (N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob.SendStopDeadlineReminders',
                        N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob.EscalateStaleStops',
                        N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob.SendUnsentDispatchDigest');
     -- فعال‌سازی مجدد: همان دستور با IsActive = 1 و در صورت گیر کردن در وضعیت خطا:
     --   , s.LastStatus = 0, s.NextRunTime = GETDATE()
     -- (ایمیل‌ها تا فعال‌شدن Def 38 System.EmailJob.SendPendingEmailsAsync فقط به‌صورت اعلان درون‌برنامه‌ای دیده می‌شوند — تصمیم Q10)
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @ClassType    nvarchar(500) = N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob';
DECLARE @AssemblyName nvarchar(500) =
(
    -- همان AssemblyName ردیف‌های موجود App.BackgroundJob (Type.GetType در JobWorker به آن نیاز دارد)
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
    N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob.SendStopDeadlineReminders',
    N'SendStopDeadlineReminders',
    N'یادآوری پایان مهلت پاسخگویی عامل توقف (درخواست‌های باز)',
    N'روزانه ۰۹:۳۵ — معادل SendStopCommentsThatResponseDeadlineDateHasArrived در HTS. برای هر توقف در انتظار بررسی عامل توقف (وضعیت 2807/2816) که مهلت پاسخ آن رسیده یا گذشته، حداکثر یک اعلان در روز به عامل توقف (رونوشت: ثبت‌کننده توقف) ثبت می‌کند. ایمیل از صف EmailJob ارسال می‌شود.',
    '09:35:00'
),
(
    N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob.EscalateStaleStops',
    N'EscalateStaleStops',
    N'ارسال خودکار توقف‌های بررسی‌نشده درخواست‌های باز به کارتابل تدارکات (۱۴ روز ⇐ 2821)',
    N'روزانه ۰۹:۳۶ — معادل SendStopCommentsThatNextStepOfStopOperatorDoseNotTriggered در HTS. اگر بیش از ۱۴ روز از اعلام نتیجه عامل توقف (وضعیت 2808/2810/2811/2812/2813) گذشته و اقدامی نشده باشد، وضعیت توقف همیشه به 2821 تغییر می‌کند (تصمیم Q6)، کامنت سیستمی ثبت و به گروه تدارکات، متولی خرید، ثبت‌کننده و عامل توقف اعلان داده می‌شود.',
    '09:36:00'
),
(
    N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob.SendUnsentDispatchDigest',
    N'SendUnsentDispatchDigest',
    N'دایجست تامین درخواست های برگ ارسال نخورده',
    N'روزانه ۰۹:۳۷ — معادل SendSupplyCommentNotifications در HTS. کامنت‌های غیرتوقف امروز درخواست‌های فعال را در یک اعلان HTML به گروه Sup.OpenOrderRequest.UnsentDispatchDigest می‌فرستد.',
    '09:37:00'
);

/* ---------- 1) JobDefinition (فقط اگر نبود) ---------- */
INSERT INTO system.JobDefinition (JobId, DisplayName, Description, AssemblyName, ClassType, MethodName)
SELECT j.JobId, j.DisplayName, j.Description, @AssemblyName, @ClassType, j.MethodName
  FROM @Jobs j
 WHERE NOT EXISTS (SELECT 1 FROM system.JobDefinition d WHERE d.JobId = j.JobId);

/* ---------- 2) JobSchedule (فقط اگر برای آن JobDefinition ردیفی نبود) ---------- */
DECLARE @Today datetime2 = CAST(CAST(SYSDATETIME() AS date) AS datetime2);

INSERT INTO system.JobSchedule
    (JobId, IsActive, IntervalSeconds, NextRunTime, LastRunTime, LastStatus,
     ScheduleType, DailyIntervalDays, WeeklyDays, DailyTime, HourlyMinute)
SELECT d.Id,
       1,                                   -- IsActive
       86400,                               -- IntervalSeconds (مانند ردیف‌های روزانه موجود)
       CASE                                 -- NextRunTime = اولین وقوع DailyTime در آینده
            WHEN DATEADD(SECOND, DATEDIFF(SECOND, '00:00:00', j.DailyTime), @Today) > SYSDATETIME()
            THEN DATEADD(SECOND, DATEDIFF(SECOND, '00:00:00', j.DailyTime), @Today)
            ELSE DATEADD(DAY, 1, DATEADD(SECOND, DATEDIFF(SECOND, '00:00:00', j.DailyTime), @Today))
       END,
       NULL,                                -- LastRunTime
       0,                                   -- LastStatus = Idle
       1,                                   -- ScheduleType = Daily
       1,                                   -- DailyIntervalDays
       NULL,                                -- WeeklyDays
       j.DailyTime,                         -- DailyTime
       0                                    -- HourlyMinute
  FROM @Jobs j
  JOIN system.JobDefinition d ON d.JobId = j.JobId
 WHERE NOT EXISTS (SELECT 1 FROM system.JobSchedule s WHERE s.JobId = d.Id);

COMMIT;

/* ---------- کنترل نتیجه ---------- */
SELECT d.Id        AS JobDefinitionId,
       d.JobId,
       d.DisplayName,
       s.Id        AS ScheduleId,
       s.IsActive,
       s.ScheduleType,
       s.IntervalSeconds,
       s.DailyIntervalDays,
       s.DailyTime,
       s.NextRunTime,
       s.LastRunTime,
       s.LastStatus
  FROM system.JobDefinition d
  LEFT JOIN system.JobSchedule s ON s.JobId = d.Id
 WHERE d.ClassType = N'App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob'
 ORDER BY s.DailyTime;
