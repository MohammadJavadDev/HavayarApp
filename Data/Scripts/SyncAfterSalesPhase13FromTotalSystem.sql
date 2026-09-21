-- Phase 13 CRM surveys TypeId IN (1008, 1942, 2332)
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase13';
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;
BEGIN TRY
BEGIN TRANSACTION;
SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, MIN(nu.Id) AS NewUserId
INTO #UserMap FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu ON nu.Username = ou.Username GROUP BY CAST(ou.User_ID AS BIGINT);

MERGE Crm.CustomerSatisfactionSurvey AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, s.TypeId, o.Id AS SaleOrderId, od.Id AS SaleOrderDetailId, wr.Id AS WorkReportId, m.Id AS MissionId,
           s.CngWorkReportId AS HtsCngWorkReportId, c.Id AS CustomerId, um.NewUserId AS InterviewerId, s.FormSerial, s.CustomerFeedbackText,
           s.AdditionalComments, s.CustomerAddress, s.CustomerTell, s.CustomerMobile, s.ResponderFullname, s.CurrentStatusId
    FROM [TMS].[TotalSystem].[dbo].[Crm_CustomerSatisfactionSurvey] s
    LEFT JOIN Sale.[Order] o ON o.HtsId = s.SaleOrderId
    LEFT JOIN Sale.OrderDetail od ON od.HtsId = s.SaleOrderDetailId
    LEFT JOIN Sale.WorkReport wr ON wr.HtsId = s.ReportId
    LEFT JOIN Sale.Mission m ON m.HtsId = s.MissionId
    LEFT JOIN SLS.Customer c ON c.HtsId = s.CustomerId
    LEFT JOIN #UserMap um ON um.OldUserId = s.InterviewerId
    WHERE s.TypeId IN (1008, 1942, 2332)
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.TypeId=s.TypeId, t.SaleOrderId=s.SaleOrderId, t.SaleOrderDetailId=s.SaleOrderDetailId, t.WorkReportId=s.WorkReportId,
    t.MissionId=s.MissionId, t.HtsCngWorkReportId=s.HtsCngWorkReportId, t.CustomerId=s.CustomerId, t.InterviewerId=s.InterviewerId, t.FormSerial=s.FormSerial,
    t.CustomerFeedbackText=s.CustomerFeedbackText, t.AdditionalComments=s.AdditionalComments, t.CustomerAddress=s.CustomerAddress, t.CustomerTell=s.CustomerTell,
    t.CustomerMobile=s.CustomerMobile, t.ResponderFullname=s.ResponderFullname, t.CurrentStatusId=s.CurrentStatusId,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, TypeId, SaleOrderId, SaleOrderDetailId, WorkReportId, MissionId, HtsCngWorkReportId, CustomerId, InterviewerId, FormSerial,
    CustomerFeedbackText, AdditionalComments, CustomerAddress, CustomerTell, CustomerMobile, ResponderFullname, CurrentStatusId,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.TypeId, s.SaleOrderId, s.SaleOrderDetailId, s.WorkReportId, s.MissionId, s.HtsCngWorkReportId, s.CustomerId, s.InterviewerId, s.FormSerial,
    s.CustomerFeedbackText, s.AdditionalComments, s.CustomerAddress, s.CustomerTell, s.CustomerMobile, s.ResponderFullname, s.CurrentStatusId,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
COMMIT; PRINT N'Phase 13 sync committed';
END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;
