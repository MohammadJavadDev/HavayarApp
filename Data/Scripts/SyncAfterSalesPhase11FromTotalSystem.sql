-- Phase 11 OilGasWorkReport + EquipmentMonitoring
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase11';
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;
BEGIN TRY
BEGIN TRANSACTION;
SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, MIN(nu.Id) AS NewUserId
INTO #UserMap FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu ON nu.Username = ou.Username GROUP BY CAST(ou.User_ID AS BIGINT);

MERGE Sale.OilGasWorkReport AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, pr.Id AS ProjectId, s.Employer, s.ProjectLocation, s.FormNumber, um.NewUserId AS ManagerId,
           s.EmployerProjectManager, s.EmployerProjectManagerPhoneNumber, s.DailyReport
    FROM [TMS].[TotalSystem].[dbo].[OilGas_WorkReport] s
    LEFT JOIN Edms.Project pr ON pr.HtsId = s.ProjectId
    LEFT JOIN #UserMap um ON um.OldUserId = s.ManagerId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.ProjectId=s.ProjectId, t.Employer=s.Employer, t.ProjectLocation=s.ProjectLocation, t.FormNumber=s.FormNumber,
    t.ManagerId=s.ManagerId, t.EmployerProjectManager=s.EmployerProjectManager, t.EmployerProjectManagerPhoneNumber=s.EmployerProjectManagerPhoneNumber, t.DailyReport=s.DailyReport,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, ProjectId, Employer, ProjectLocation, FormNumber, ManagerId, EmployerProjectManager, EmployerProjectManagerPhoneNumber, DailyReport,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.ProjectId, s.Employer, s.ProjectLocation, s.FormNumber, s.ManagerId, s.EmployerProjectManager, s.EmployerProjectManagerPhoneNumber, s.DailyReport,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Sale.EquipmentMonitoring AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, s.SerialNumber, s.HardwareID AS HardwareId, s.HardwareVersion, s.SoftwareVersion,
           ser.Id AS OrderDetailSerialId, c.Id AS CustomerId, p.Id AS PartId, s.IsConnected, s.LastConnectionDate AS LastConnectionMiladiDate,
           s.ConnectionDurationInSecond, s.ClientIpAddress, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Sale_EquipmentMonitoring] s
    LEFT JOIN Sale.OrderDetailSerial ser ON ser.HtsId = s.OrderDetailSerialId
    LEFT JOIN SLS.Customer c ON c.HtsId = s.CustomerId
    LEFT JOIN Inv.Part p ON p.HtsId = s.PartId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.SerialNumber=s.SerialNumber, t.HardwareId=s.HardwareId, t.HardwareVersion=s.HardwareVersion, t.SoftwareVersion=s.SoftwareVersion,
    t.OrderDetailSerialId=s.OrderDetailSerialId, t.CustomerId=s.CustomerId, t.PartId=s.PartId, t.IsConnected=s.IsConnected,
    t.LastConnectionMiladiDate=s.LastConnectionMiladiDate, t.ConnectionDurationInSecond=s.ConnectionDurationInSecond, t.ClientIpAddress=s.ClientIpAddress, t.Comment=s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, SerialNumber, HardwareId, HardwareVersion, SoftwareVersion, OrderDetailSerialId, CustomerId, PartId, IsConnected, LastConnectionMiladiDate, ConnectionDurationInSecond, ClientIpAddress, Comment,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.SerialNumber, s.HardwareId, s.HardwareVersion, s.SoftwareVersion, s.OrderDetailSerialId, s.CustomerId, s.PartId, s.IsConnected, s.LastConnectionMiladiDate, s.ConnectionDurationInSecond, s.ClientIpAddress, s.Comment,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
COMMIT; PRINT N'Phase 11 sync committed';
END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;
