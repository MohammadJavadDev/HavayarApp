/*
================================================================================
Reset_JobSchedule_SyncBuyCategoryJob.sql  (WP5 / D12)
================================================================================
جاب «هماهنگ کردن اطلاعات دسته بندی های خرید از راهکاران» (SyncBuyCategoryJobFromRahkaran)
از 2026-08-18 با خطای کلید تکراری می‌خورد و از 2026-09-02 با LastStatus=Error (3) و
NextRunTime گذشته گیر کرده است. بعد از deploy نسخهٴ اصلاح‌شدهٴ BuyCategoryJob.cs این اسکریپت
زمان‌بندی را به حالت Idle برمی‌گرداند تا در اجرای بعدی زمان‌بند دوباره اجرا شود.

JobStatus: 0 Idle · 1 Running · 2 Waiting · 3 Error
Idempotent. اجرا روی دیتابیس HavayarApp — فقط بعد از deploy کد جدید.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @MethodName NVARCHAR(200) = N'SyncBuyCategoryJobFromRahkaran';

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Targets TABLE (ScheduleId INT, JobDefinitionId BIGINT, JobId NVARCHAR(400), LastStatus INT, NextRunTime DATETIME2, LastRunTime DATETIME2, IsActive BIT);

    INSERT INTO @Targets (ScheduleId, JobDefinitionId, JobId, LastStatus, NextRunTime, LastRunTime, IsActive)
    SELECT s.Id, d.Id, d.JobId, s.LastStatus, s.NextRunTime, s.LastRunTime, s.IsActive
    FROM system.JobSchedule s
    INNER JOIN system.JobDefinition d ON d.Id = s.JobId
    WHERE d.MethodName = @MethodName
       OR d.JobId LIKE N'%' + @MethodName + N'%';

    IF NOT EXISTS (SELECT 1 FROM @Targets)
    BEGIN
        PRINT N'WARN: هیچ JobSchedule برای ' + @MethodName + N' یافت نشد — کاری انجام نشد';
    END
    ELSE
    BEGIN
        PRINT N'--- قبل از ریست ---';
        SELECT * FROM @Targets;

        UPDATE s
        SET s.LastStatus  = 0,          -- Idle
            s.NextRunTime = GETDATE(),  -- اجرای فوری در تیک بعدی زمان‌بند
            s.IsActive    = 1
        FROM system.JobSchedule s
        INNER JOIN @Targets t ON t.ScheduleId = s.Id;

        PRINT N'  JobSchedule ریست شد: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' ردیف';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Reset_JobSchedule_SyncBuyCategoryJob ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-- تایید نتیجه
SELECT s.Id AS ScheduleId, d.Id AS JobDefinitionId, d.JobId, d.DisplayName,
       s.IsActive, s.ScheduleType, s.IntervalSeconds, s.LastStatus, s.LastRunTime, s.NextRunTime
FROM system.JobSchedule s
INNER JOIN system.JobDefinition d ON d.Id = s.JobId
WHERE d.MethodName = @MethodName
   OR d.JobId LIKE N'%' + @MethodName + N'%';

-- آخرین ۵ اجرای جاب (برای مقایسه بعد از اجرای بعدی)
SELECT TOP (5) h.Id, h.ScheduleId, h.StartTime, h.EndTime, h.IsSuccess, LEFT(h.ErrorMessage, 300) AS ErrorMessage
FROM system.JobHistory h
INNER JOIN system.JobSchedule s ON s.Id = h.ScheduleId
INNER JOIN system.JobDefinition d ON d.Id = s.JobId
WHERE d.MethodName = @MethodName
   OR d.JobId LIKE N'%' + @MethodName + N'%'
ORDER BY h.StartTime DESC;
