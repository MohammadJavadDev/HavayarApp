/*
جایگزین شاخه INSERT در تریگر
USR3.AddProductionOrderToHts_UpdateRevisionAndApplySomeChanges
روی USR3.Sale_ProductionOrder.

به‌جای این دو خط:
    SET @OperationType = 1;
    EXEC msdb.dbo.sp_start_job @job_name = 'HTS_AddProductionOrderJob';

همین بلوک را بگذارید. شاخه UPDATE (ریویژن راهکاران و جاب ایمیل) دست نخورد.
Id در dbo.Sale_ProductionOrderSyncQueue از نوع IDENTITY است؛ در INSERT نیاید.
*/
SET @OperationType = 1;

INSERT INTO dbo.Sale_ProductionOrderSyncQueue
(
    Sale_ProductionOrderID,
    CreatedAt
)
SELECT
    Inserted.Sale_ProductionOrderID,
    SYSDATETIME()
FROM Inserted;

BEGIN TRY
    UPDATE sch
    SET sch.NextRunTime = DATEADD(SECOND, 10, GETDATE()),
        sch.LastStatus = 2 /* Waiting */
    FROM [Portal.HavayarApp].[HavayarApp].[system].[JobSchedule] AS sch
    INNER JOIN [Portal.HavayarApp].[HavayarApp].[system].[JobDefinition] AS def
        ON def.Id = sch.JobId
    WHERE def.MethodName = N'AddProductionOrderFromRahkaran'
      AND sch.IsActive = 1
      AND sch.LastStatus <> 1; /* Running */
END TRY
BEGIN CATCH
    /* اگر بیدار کردن جاب خطا بدهد، درج سفارش راهکاران برنگردد.
       شناسه در صف می‌ماند و زمان‌بندی عادی جاب آن را برمی‌دارد.
       اگر تراکنش doomed شد (XACT_STATE() = -1) این CATCH جلوی rollback را نمی‌گیرد. */
    IF XACT_STATE() = -1
        THROW;
END CATCH
