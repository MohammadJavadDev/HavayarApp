/* =============================================================================
   Seed_SaleOrderHtsSyncJob_Schedule.sql

   زمان‌بندی همگام‌سازی Order / OrderDetail / OrderDetailSerial از HTS.
   پیش‌نیاز: استقرار App.BackgroundJob شامل SyncSaleOrdersFromHts و اجرای
   Data/Scripts/Sync_SaleOrder_FromHts.sql (رویه Sale.SyncOrderFromHts).

   Idempotent: JobDefinition فقط اگر JobId نبود؛ JobSchedule فقط اگر برای آن تعریف ردیفی نبود.
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @JobId nvarchar(500) = N'App.BackgroundJob.Jobs.Sale.SaleOrderJob.SyncSaleOrdersFromHts';
DECLARE @ClassType nvarchar(500) = N'App.BackgroundJob.Jobs.Sale.SaleOrderJob';
DECLARE @AssemblyName nvarchar(500) =
(
    SELECT TOP (1) d.AssemblyName
      FROM system.JobDefinition d
     WHERE d.ClassType = @ClassType
);
IF @AssemblyName IS NULL
    SET @AssemblyName = N'App.BackgroundJob, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null';

IF NOT EXISTS (SELECT 1 FROM system.JobDefinition d WHERE d.JobId = @JobId)
BEGIN
    INSERT INTO system.JobDefinition (JobId, DisplayName, Description, AssemblyName, ClassType, MethodName)
    VALUES (
        @JobId,
        N'همگام‌سازی سفارش و سریال فروش از HTS',
        N'هر ۲۰ دقیقه — زدن HtsId روی سفارش/قلم موجود راهکاران، درج ردیف‌های فقط-HTS، MERGE سریال از TotalSystem.',
        @AssemblyName,
        @ClassType,
        N'SyncSaleOrdersFromHts');
END;

DECLARE @DefId bigint = (SELECT d.Id FROM system.JobDefinition d WHERE d.JobId = @JobId);

IF @DefId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM system.JobSchedule s WHERE s.JobId = @DefId)
BEGIN
    INSERT INTO system.JobSchedule
        (JobId, IsActive, IntervalSeconds, NextRunTime, LastRunTime, LastStatus,
         ScheduleType, DailyIntervalDays, WeeklyDays, DailyTime, HourlyMinute)
    VALUES
        (@DefId, 1, 1200, DATEADD(MINUTE, 5, SYSDATETIME()), NULL, 0,
         0, 0, NULL, NULL, 0);
END;

COMMIT;

SELECT d.Id AS JobDefinitionId, d.JobId, d.DisplayName,
       s.Id AS ScheduleId, s.IsActive, s.ScheduleType, s.IntervalSeconds, s.NextRunTime, s.LastStatus
  FROM system.JobDefinition d
  LEFT JOIN system.JobSchedule s ON s.JobId = d.Id
 WHERE d.JobId = @JobId;
