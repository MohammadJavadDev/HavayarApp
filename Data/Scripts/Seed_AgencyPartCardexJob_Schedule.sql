/*
  Seed_AgencyPartCardexJob_Schedule.sql
  زمان‌بندی کاردکس قطعه نمایندگی — معادل HTS AfterSalesAndRepairSystemTask
  هر ۲۰ دقیقه بین ۶ تا ۲۱ (گیت ساعت داخل خود جاب است).
  Encoding: UTF-8 with BOM.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @JobId nvarchar(500) = N'App.BackgroundJob.Jobs.Sale.AgencyPartCardexJob.AddNewItemsByCheckSaleOrders';
DECLARE @ClassType nvarchar(500) = N'App.BackgroundJob.Jobs.Sale.AgencyPartCardexJob';
DECLARE @AssemblyName nvarchar(500) =
(
    SELECT TOP (1) d.AssemblyName
      FROM system.JobDefinition d
     WHERE d.AssemblyName LIKE N'App.BackgroundJob%'
);
IF @AssemblyName IS NULL
    SET @AssemblyName = N'App.BackgroundJob, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null';

IF NOT EXISTS (SELECT 1 FROM system.JobDefinition d WHERE d.JobId = @JobId)
BEGIN
    INSERT INTO system.JobDefinition (JobId, DisplayName, Description, AssemblyName, ClassType, MethodName)
    VALUES (
        @JobId,
        N'بروزرسانی کاردکس قطعه نمایندگی از حواله و درخواست کالا',
        N'هر ۲۰ دقیقه بین ۶ تا ۲۱ — افزایش از حواله فروش دیروز، کاهش از درخواست کالای دستی نمایندگان دیروز.',
        @AssemblyName,
        @ClassType,
        N'AddNewItemsByCheckSaleOrders');
END;

DECLARE @DefId bigint = (SELECT d.Id FROM system.JobDefinition d WHERE d.JobId = @JobId);

IF @DefId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM system.JobSchedule s WHERE s.JobId = @DefId)
BEGIN
    INSERT INTO system.JobSchedule
        (JobId, IsActive, IntervalSeconds, NextRunTime, LastRunTime, LastStatus,
         ScheduleType, DailyIntervalDays, WeeklyDays, DailyTime, HourlyMinute)
    VALUES
        (@DefId, 1, 1200, DATEADD(MINUTE, 1, SYSDATETIME()), NULL, 0,
         0, 0, NULL, NULL, 0);
END;

COMMIT;

SELECT d.Id AS JobDefinitionId, d.JobId, d.DisplayName,
       s.Id AS ScheduleId, s.IsActive, s.ScheduleType, s.IntervalSeconds, s.NextRunTime, s.LastStatus
  FROM system.JobDefinition d
  LEFT JOIN system.JobSchedule s ON s.JobId = d.Id
 WHERE d.JobId = @JobId;
