/*
  Rahkaran (Erps, server ERPS 172.20.40.73): retire the old "new comment" e-mail mechanism.
  Replaced by HavayarApp App.BackgroundJob job
  RequirmentAdvertiseCommentEmailJob.QueueCommentEmailsAsync (every 5 minutes).

  Run ONLY AFTER the new job has been deployed and has run once successfully
  (JobLogs: no errors). Order matters, do not skip step 1:
    1. disable the SQL Agent job   (stops the old job from inserting into Gnr_EmailBucketList)
    2. drop the trigger            (it only exists to start that job)
  The job itself is kept (disabled) for a few weeks as a rollback; delete it later.
*/
USE [Erps]
GO

-- 1) disable the old job
EXEC msdb.dbo.sp_update_job
     @job_name = N'HTS_SaleRequirmentAdvertise_NewComment',
     @enabled  = 0;
GO

-- 2) drop the trigger on SYS3.Note
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = N'SaleRequirmentAdvertiseComments')
    DROP TRIGGER [SYS3].[SaleRequirmentAdvertiseComments];
GO

/*
  Rollback (re-enable the old mechanism):
    EXEC msdb.dbo.sp_update_job @job_name = N'HTS_SaleRequirmentAdvertise_NewComment', @enabled = 1;
    -- and re-create the trigger (AFTER INSERT on SYS3.Note -> msdb.dbo.sp_start_job), with TRY/CATCH.
*/
