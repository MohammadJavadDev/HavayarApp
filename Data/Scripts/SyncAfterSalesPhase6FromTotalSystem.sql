/*
================================================================================
SyncAfterSalesPhase6FromTotalSystem.sql
================================================================================
ماموریت، حق ماموریت، گزارش کار (+ پیوست عنوان)، قطعه درخواست پشتیبانی.
MERGE روی HtsId. باینری پیوست کپی نمی‌شود.
کاربر: Gnr_User.Username → system.User.Username (از طریق Personel_FK برای حقوق/ماموریت).
FK بدون نگاشت لاگ می‌شود؛ کل دسته فقط با @Strict=1 شکست می‌خورد.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Strict BIT = 0;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase6';
DECLARE @Rc INT;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;

BEGIN TRY
    EXEC sp_testlinkedserver [TMS];
END TRY
BEGIN CATCH
    DECLARE @Ls NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'sp_testlinkedserver [TMS] ناموفق: %s', 16, 1, @Ls);
    RETURN;
END CATCH;

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#Unmapped') IS NOT NULL DROP TABLE #Unmapped;
CREATE TABLE #Unmapped (Kind NVARCHAR(80) NOT NULL, OldId BIGINT NULL, Detail NVARCHAR(400) NULL);

SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, MIN(nu.Id) AS NewUserId
INTO #UserMap
FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu
    ON nu.Username = ou.Username
    OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
        AND nu.Username = LTRIM(RTRIM(ou.ActiveDirectoryUsername)))
GROUP BY CAST(ou.User_ID AS BIGINT);

SELECT CAST(ou.Personel_FK AS BIGINT) AS OldPersonelId, MIN(nu.Id) AS NewUserId
INTO #PersonelUserMap
FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu
    ON nu.Username = ou.Username
    OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
        AND nu.Username = LTRIM(RTRIM(ou.ActiveDirectoryUsername)))
WHERE ou.Personel_FK IS NOT NULL
GROUP BY CAST(ou.Personel_FK AS BIGINT);

PRINT N'=== Sale.Mission ===';
MERGE Sale.Mission AS t
USING (
    SELECT CAST(s.Mission_Id AS BIGINT) AS HtsId, sr.Id AS ServiceRequestId, pum.NewUserId AS ExpertId,
           CAST(s.Personel_FK AS BIGINT) AS HtsPersonelId,
           s.Have_Gurantee AS HaveGuarantee, s.Hokm_Number AS HokmNumber,
           s.Dispatch_Date AS DispatchMiladiDate, s.Dispatch_Date_Sahmsi AS DispatchShamsiDate,
           LEFT(s.Dispatch_Time, 5) AS DispatchTime,
           s.Return_Date AS ReturnMiladiDate, s.Return_Date_Sahmsi AS ReturnShamsiDate,
           LEFT(s.Return_Time, 5) AS ReturnTime,
           s.Breakfast_Fee AS BreakfastFee, s.Breakfast, s.Lunch_Fee AS LunchFee, s.Lunch,
           s.Dinner_Fee AS DinnerFee, s.Dinner, s.NightRight, s.NightRight_Factor AS NightRightFactor,
           s.Work_Overtime_Fee AS WorkOvertimeFee, s.Work_Overtime_Hour AS WorkOvertimeHour,
           s.Mission_Right AS MissionRight, s.Mission_Right_Factor AS MissionRightFactor,
           s.Mission_HolidayRight AS MissionHolidayRight, s.Mission_HolidayRight_Factor AS MissionHolidayRightFactor,
           s.Transportation_Personal AS TransportationPersonal, s.Transportation_Driving AS TransportationDriving
    FROM [TMS].[TotalSystem].[dbo].[Sale_Mission] s
    LEFT JOIN Sale.ServiceRequest sr ON sr.HtsId = s.ServiceRequest_FK AND sr.HtsId <> 0
    LEFT JOIN #PersonelUserMap pum ON pum.OldPersonelId = s.Personel_FK
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.ServiceRequestId=s.ServiceRequestId, t.ExpertId=s.ExpertId, t.HtsPersonelId=s.HtsPersonelId,
    t.HaveGuarantee=s.HaveGuarantee, t.HokmNumber=s.HokmNumber, t.DispatchMiladiDate=s.DispatchMiladiDate,
    t.DispatchShamsiDate=s.DispatchShamsiDate, t.DispatchTime=s.DispatchTime, t.ReturnMiladiDate=s.ReturnMiladiDate,
    t.ReturnShamsiDate=s.ReturnShamsiDate, t.ReturnTime=s.ReturnTime, t.BreakfastFee=s.BreakfastFee, t.Breakfast=s.Breakfast,
    t.LunchFee=s.LunchFee, t.Lunch=s.Lunch, t.DinnerFee=s.DinnerFee, t.Dinner=s.Dinner, t.NightRight=s.NightRight,
    t.NightRightFactor=s.NightRightFactor, t.WorkOvertimeFee=s.WorkOvertimeFee, t.WorkOvertimeHour=s.WorkOvertimeHour,
    t.MissionRight=s.MissionRight, t.MissionRightFactor=s.MissionRightFactor, t.MissionHolidayRight=s.MissionHolidayRight,
    t.MissionHolidayRightFactor=s.MissionHolidayRightFactor, t.TransportationPersonal=s.TransportationPersonal,
    t.TransportationDriving=s.TransportationDriving, t.ModifiedById=1, t.ModifiedByName=@SeedUser,
    t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, ServiceRequestId, ExpertId, HtsPersonelId, HaveGuarantee, HokmNumber,
    DispatchMiladiDate, DispatchShamsiDate, DispatchTime, ReturnMiladiDate, ReturnShamsiDate, ReturnTime,
    BreakfastFee, Breakfast, LunchFee, Lunch, DinnerFee, Dinner, NightRight, NightRightFactor, WorkOvertimeFee,
    WorkOvertimeHour, MissionRight, MissionRightFactor, MissionHolidayRight, MissionHolidayRightFactor,
    TransportationPersonal, TransportationDriving,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName,
    ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.ServiceRequestId, s.ExpertId, s.HtsPersonelId, s.HaveGuarantee, s.HokmNumber,
    s.DispatchMiladiDate, s.DispatchShamsiDate, s.DispatchTime, s.ReturnMiladiDate, s.ReturnShamsiDate, s.ReturnTime,
    s.BreakfastFee, s.Breakfast, s.LunchFee, s.Lunch, s.DinnerFee, s.Dinner, s.NightRight, s.NightRightFactor,
    s.WorkOvertimeFee, s.WorkOvertimeHour, s.MissionRight, s.MissionRightFactor, s.MissionHolidayRight,
    s.MissionHolidayRightFactor, s.TransportationPersonal, s.TransportationDriving,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @Rc = @@ROWCOUNT;
PRINT N'Mission MERGE touched: ' + CAST(@Rc AS NVARCHAR(20));

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'MissionExpert', CAST(s.Mission_Id AS BIGINT), N'Personel_FK not mapped to system.User'
FROM [TMS].[TotalSystem].[dbo].[Sale_Mission] s
WHERE NOT EXISTS (SELECT 1 FROM #PersonelUserMap p WHERE p.OldPersonelId = s.Personel_FK);

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'MissionServiceRequest', CAST(s.Mission_Id AS BIGINT), N'ServiceRequest_FK not mapped'
FROM [TMS].[TotalSystem].[dbo].[Sale_Mission] s
WHERE s.ServiceRequest_FK IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Sale.ServiceRequest sr WHERE sr.HtsId = s.ServiceRequest_FK AND sr.HtsId <> 0);

PRINT N'=== Sale.MissionSalary ===';
IF OBJECT_ID('tempdb..#TmsMissionSalary') IS NOT NULL DROP TABLE #TmsMissionSalary;
SELECT * INTO #TmsMissionSalary
FROM OPENQUERY([TMS], N'
    SELECT
        CAST(Personel_MissionSalary_ID AS BIGINT) AS HtsId,
        CAST(Personel_FK AS BIGINT) AS PersonelHtsId,
        Breakfast_Fee AS BreakfastFee,
        Lunch_Fee AS LunchFee,
        Dinner_Fee AS DinnerFee,
        NightRight,
        Work_Overtime_Fee AS WorkOvertimeFee,
        Mission_Right AS MissionRight,
        Mission_HolidayRight AS MissionHolidayRight,
        Daily_Salary AS DailySalary
    FROM dbo.Sale_Personel_MissionSalary
');

MERGE Sale.MissionSalary AS t
USING (
    SELECT s.HtsId,
           COALESCE(pum.NewUserId, usr.Id) AS ExpertId,
           hp.Id AS PersonelId,
           s.PersonelHtsId AS HtsPersonelId,
           s.BreakfastFee, s.LunchFee, s.DinnerFee, s.NightRight, s.WorkOvertimeFee,
           s.MissionRight, s.MissionHolidayRight, s.DailySalary
    FROM #TmsMissionSalary s
    LEFT JOIN #PersonelUserMap pum ON pum.OldPersonelId = s.PersonelHtsId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] hrm ON hrm.Personel_ID = s.PersonelHtsId
    LEFT JOIN Hcm.Personel hp ON TRY_CONVERT(int, hp.Code) = hrm.Hamkaran_Personel_FK
    OUTER APPLY (
        SELECT TOP (1) u.Id
        FROM system.[User] u
        WHERE hp.PartyId IS NOT NULL AND u.PartyId = hp.PartyId
        ORDER BY u.Id
    ) usr
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.ExpertId=s.ExpertId, t.PersonelId=s.PersonelId, t.HtsPersonelId=s.HtsPersonelId,
    t.BreakfastFee=s.BreakfastFee, t.LunchFee=s.LunchFee, t.DinnerFee=s.DinnerFee, t.NightRight=s.NightRight,
    t.WorkOvertimeFee=s.WorkOvertimeFee, t.MissionRight=s.MissionRight, t.MissionHolidayRight=s.MissionHolidayRight,
    t.DailySalary=s.DailySalary, t.ModifiedById=1, t.ModifiedByName=@SeedUser,
    t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, ExpertId, PersonelId, HtsPersonelId, BreakfastFee, LunchFee, DinnerFee, NightRight,
    WorkOvertimeFee, MissionRight, MissionHolidayRight, DailySalary,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName,
    ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.ExpertId, s.PersonelId, s.HtsPersonelId, s.BreakfastFee, s.LunchFee, s.DinnerFee, s.NightRight,
    s.WorkOvertimeFee, s.MissionRight, s.MissionHolidayRight, s.DailySalary,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @Rc = @@ROWCOUNT;
PRINT N'MissionSalary MERGE touched: ' + CAST(@Rc AS NVARCHAR(20));

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'MissionSalaryPersonel', s.HtsId, N'HRM Personel not mapped to Hcm.Personel'
FROM #TmsMissionSalary s
LEFT JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] hrm ON hrm.Personel_ID = s.PersonelHtsId
LEFT JOIN Hcm.Personel hp ON TRY_CONVERT(int, hp.Code) = hrm.Hamkaran_Personel_FK
WHERE hp.Id IS NULL;

PRINT N'=== Sale.WorkReport ===';
IF OBJECT_ID('tempdb..#TmsWorkReport') IS NOT NULL DROP TABLE #TmsWorkReport;
SELECT * INTO #TmsWorkReport
FROM OPENQUERY([TMS], N'
    SELECT
        CAST(Report_ID AS BIGINT) AS HtsId,
        CAST(Mission_FK AS BIGINT) AS MissionHtsId,
        CAST(ServiceRequest_Detail_FK AS BIGINT) AS DetailHtsId,
        Report_Number AS ReportNumber,
        ServiceStart_Date AS ServiceStartMiladiDate,
        ServiceStart_Date_Shamsi AS ServiceStartShamsiDate,
        LEFT(ServiceStart_Time, 5) AS ServiceStartTime,
        ServiceEnd_Date AS ServiceEndMiladiDate,
        ServiceEnd_Date_Shamsi AS ServiceEndShamsiDate,
        LEFT(ServiceEnd_Time, 5) AS ServiceEndTime,
        LEFT(Request_Description, 4000) AS RequestDescription,
        LEFT(Executive_Barriers_Description, 4000) AS ExecutiveBarriersDescription,
        CAST(Mission_Result_FK AS INT) AS MissionResult,
        Compressor_House_Status AS CompressorHouseStatus,
        Electrical_Panel_Status AS ElectricalPanelStatus,
        Piping_Status AS PipingStatus,
        CAST(ISNULL(TotalRunTime, 0) AS BIGINT) AS TotalRunTime,
        CAST(WorkHours_Underload AS BIGINT) AS WorkHoursUnderload,
        Flow_Intensity_Underload AS FlowIntensityUnderload,
        Flow_Intensity_Without_load AS FlowIntensityWithoutLoad,
        Maximum_Working_Pressure AS MaximumWorkingPressure,
        Minimum_Working_Pressure AS MinimumWorkingPressure,
        Environment_Temperature AS EnvironmentTemperature,
        Device_Temperature AS DeviceTemperature,
        CAST(Voltage_Power_Grid AS SMALLINT) AS VoltagePowerGrid,
        Amount_Dryer_Dyupoint AS AmountDryerDewpoint,
        CAST(OldRunTime AS INT) AS OldRunTime,
        Expert_Guarantee_Date AS ExpertGuaranteeMiladiDate,
        LEFT(Expert_Guarantee_Date_Shamsi, 10) AS ExpertGuaranteeShamsiDate,
        LEFT(Expert_Guarantee_Comment, 4000) AS ExpertGuaranteeComment,
        LEFT(FailureTypeIds, 512) AS FailureTypeIds,
        LEFT(FailureTypeInText, 4000) AS FailureTypeInText,
        LEFT(RepairsTypeIds, 512) AS RepairsTypeIds,
        LEFT(RepairsTypeInText, 4000) AS RepairsTypeInText,
        IsSurveyLinkSended
    FROM dbo.Sale_Report
');

MERGE Sale.WorkReport AS t
USING (
    SELECT s.HtsId, m.Id AS MissionId, d.Id AS ServiceRequestDetailId, s.ReportNumber,
           s.ServiceStartMiladiDate, s.ServiceStartShamsiDate, NULLIF(LTRIM(RTRIM(s.ServiceStartTime)), N'') AS ServiceStartTime,
           s.ServiceEndMiladiDate, s.ServiceEndShamsiDate, NULLIF(LTRIM(RTRIM(s.ServiceEndTime)), N'') AS ServiceEndTime,
           s.RequestDescription, s.ExecutiveBarriersDescription, s.MissionResult,
           ISNULL(s.CompressorHouseStatus, 0) AS CompressorHouseStatus,
           ISNULL(s.ElectricalPanelStatus, 0) AS ElectricalPanelStatus,
           ISNULL(s.PipingStatus, 0) AS PipingStatus,
           ISNULL(s.TotalRunTime, 0) AS TotalRunTime, s.WorkHoursUnderload,
           s.FlowIntensityUnderload, s.FlowIntensityWithoutLoad, s.MaximumWorkingPressure, s.MinimumWorkingPressure,
           s.EnvironmentTemperature, s.DeviceTemperature, s.VoltagePowerGrid, s.AmountDryerDewpoint, s.OldRunTime,
           s.ExpertGuaranteeMiladiDate, s.ExpertGuaranteeShamsiDate, s.ExpertGuaranteeComment,
           s.FailureTypeIds, s.FailureTypeInText, s.RepairsTypeIds, s.RepairsTypeInText, s.IsSurveyLinkSended
    FROM #TmsWorkReport s
    LEFT JOIN Sale.Mission m ON m.HtsId = s.MissionHtsId AND m.HtsId <> 0
    LEFT JOIN Sale.ServiceRequestDetail d ON d.HtsId = s.DetailHtsId AND d.HtsId <> 0
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.MissionId=s.MissionId, t.ServiceRequestDetailId=s.ServiceRequestDetailId,
    t.ReportNumber=s.ReportNumber, t.ServiceStartMiladiDate=s.ServiceStartMiladiDate,
    t.ServiceStartShamsiDate=s.ServiceStartShamsiDate, t.ServiceStartTime=s.ServiceStartTime,
    t.ServiceEndMiladiDate=s.ServiceEndMiladiDate, t.ServiceEndShamsiDate=s.ServiceEndShamsiDate,
    t.ServiceEndTime=s.ServiceEndTime, t.RequestDescription=s.RequestDescription,
    t.ExecutiveBarriersDescription=s.ExecutiveBarriersDescription, t.MissionResult=s.MissionResult,
    t.CompressorHouseStatus=s.CompressorHouseStatus, t.ElectricalPanelStatus=s.ElectricalPanelStatus,
    t.PipingStatus=s.PipingStatus, t.TotalRunTime=s.TotalRunTime, t.WorkHoursUnderload=s.WorkHoursUnderload,
    t.FlowIntensityUnderload=s.FlowIntensityUnderload, t.FlowIntensityWithoutLoad=s.FlowIntensityWithoutLoad,
    t.MaximumWorkingPressure=s.MaximumWorkingPressure, t.MinimumWorkingPressure=s.MinimumWorkingPressure,
    t.EnvironmentTemperature=s.EnvironmentTemperature, t.DeviceTemperature=s.DeviceTemperature,
    t.VoltagePowerGrid=s.VoltagePowerGrid, t.AmountDryerDewpoint=s.AmountDryerDewpoint, t.OldRunTime=s.OldRunTime,
    t.ExpertGuaranteeMiladiDate=s.ExpertGuaranteeMiladiDate, t.ExpertGuaranteeShamsiDate=s.ExpertGuaranteeShamsiDate,
    t.ExpertGuaranteeComment=s.ExpertGuaranteeComment, t.FailureTypeIds=s.FailureTypeIds,
    t.FailureTypeInText=s.FailureTypeInText, t.RepairsTypeIds=s.RepairsTypeIds, t.RepairsTypeInText=s.RepairsTypeInText,
    t.IsSurveyLinkSended=s.IsSurveyLinkSended, t.ModifiedById=1, t.ModifiedByName=@SeedUser,
    t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, MissionId, ServiceRequestDetailId, ReportNumber, ServiceStartMiladiDate,
    ServiceStartShamsiDate, ServiceStartTime, ServiceEndMiladiDate, ServiceEndShamsiDate, ServiceEndTime,
    RequestDescription, ExecutiveBarriersDescription, MissionResult, CompressorHouseStatus, ElectricalPanelStatus,
    PipingStatus, TotalRunTime, WorkHoursUnderload, FlowIntensityUnderload, FlowIntensityWithoutLoad,
    MaximumWorkingPressure, MinimumWorkingPressure, EnvironmentTemperature, DeviceTemperature, VoltagePowerGrid,
    AmountDryerDewpoint, OldRunTime, ExpertGuaranteeMiladiDate, ExpertGuaranteeShamsiDate, ExpertGuaranteeComment,
    FailureTypeIds, FailureTypeInText, RepairsTypeIds, RepairsTypeInText, IsSurveyLinkSended,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById,
    ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.MissionId, s.ServiceRequestDetailId, s.ReportNumber, s.ServiceStartMiladiDate,
    s.ServiceStartShamsiDate, s.ServiceStartTime, s.ServiceEndMiladiDate, s.ServiceEndShamsiDate, s.ServiceEndTime,
    s.RequestDescription, s.ExecutiveBarriersDescription, s.MissionResult, s.CompressorHouseStatus,
    s.ElectricalPanelStatus, s.PipingStatus, s.TotalRunTime, s.WorkHoursUnderload, s.FlowIntensityUnderload,
    s.FlowIntensityWithoutLoad, s.MaximumWorkingPressure, s.MinimumWorkingPressure, s.EnvironmentTemperature,
    s.DeviceTemperature, s.VoltagePowerGrid, s.AmountDryerDewpoint, s.OldRunTime, s.ExpertGuaranteeMiladiDate,
    s.ExpertGuaranteeShamsiDate, s.ExpertGuaranteeComment, s.FailureTypeIds, s.FailureTypeInText, s.RepairsTypeIds,
    s.RepairsTypeInText, s.IsSurveyLinkSended,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @Rc = @@ROWCOUNT;
PRINT N'WorkReport MERGE touched: ' + CAST(@Rc AS NVARCHAR(20));

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'WorkReportMission', s.HtsId, N'Mission_FK not mapped'
FROM #TmsWorkReport s
WHERE s.MissionHtsId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Sale.Mission m WHERE m.HtsId = s.MissionHtsId AND m.HtsId <> 0);

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'WorkReportDetail', s.HtsId, N'ServiceRequest_Detail_FK not mapped'
FROM #TmsWorkReport s
WHERE s.DetailHtsId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Sale.ServiceRequestDetail d WHERE d.HtsId = s.DetailHtsId AND d.HtsId <> 0);

PRINT N'=== Sale.WorkReportAttachment (metadata only; no binary) ===';
IF OBJECT_ID('tempdb..#TmsWorkReportAtt') IS NOT NULL DROP TABLE #TmsWorkReportAtt;
SELECT * INTO #TmsWorkReportAtt
FROM OPENQUERY([TMS], N'
    SELECT
        CAST(Report_Attachment_ID AS BIGINT) AS HtsId,
        CAST(Report_FK AS BIGINT) AS ReportHtsId,
        LEFT(Attachment_FileName, 256) AS Title
    FROM dbo.Sale_Report_Attachment
');

MERGE Sale.WorkReportAttachment AS t
USING (
    SELECT s.HtsId, wr.Id AS WorkReportId, s.Title
    FROM #TmsWorkReportAtt s
    INNER JOIN Sale.WorkReport wr ON wr.HtsId = s.ReportHtsId AND wr.HtsId <> 0
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.WorkReportId=s.WorkReportId, t.Title=s.Title,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, WorkReportId, Title,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName,
    ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.WorkReportId, s.Title, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @Rc = @@ROWCOUNT;
PRINT N'WorkReportAttachment MERGE touched: ' + CAST(@Rc AS NVARCHAR(20));

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'WorkReportAttachment', s.HtsId, N'WorkReport parent not mapped'
FROM #TmsWorkReportAtt s
WHERE NOT EXISTS (SELECT 1 FROM Sale.WorkReport wr WHERE wr.HtsId = s.ReportHtsId AND wr.HtsId <> 0);

PRINT N'=== Sale.ServiceRequestPart ===';
IF OBJECT_ID('tempdb..#TmsSrPart') IS NOT NULL DROP TABLE #TmsSrPart;
SELECT * INTO #TmsSrPart
FROM OPENQUERY([TMS], N'
    SELECT
        CAST(ServiceRequest_Part_ID AS BIGINT) AS HtsId,
        CAST(ServiceRequest_Detail_FK AS BIGINT) AS DetailHtsId,
        CAST(OrderDetail_FK AS BIGINT) AS OrderDetailHtsId,
        CAST(ReplacePart_FK AS BIGINT) AS ReplacePartHtsId,
        CAST(ServiceType_FK AS INT) AS ServiceType,
        Mount,
        UnitPrice,
        IsRegisteredManually,
        Project_VchItm_Hamkaran_FK AS HtsProjectVchItemId,
        Project_VchPart_FK AS HtsProjectVchPartId,
        Project_VchNum AS ProjectVchNum,
        Project_VchYear AS ProjectVchYear,
        LEFT(Comment, 4000) AS Comment
    FROM dbo.Sale_ServiceRequest_Part
');

MERGE Sale.ServiceRequestPart AS t
USING (
    SELECT s.HtsId, d.Id AS ServiceRequestDetailId, od.Id AS OrderDetailId, p.Id AS ReplacePartId,
           s.ServiceType, s.Mount, s.UnitPrice, ISNULL(s.IsRegisteredManually, 0) AS IsRegisteredManually,
           s.HtsProjectVchItemId, s.HtsProjectVchPartId, s.ProjectVchNum, s.ProjectVchYear, s.Comment
    FROM #TmsSrPart s
    LEFT JOIN Sale.ServiceRequestDetail d ON d.HtsId = s.DetailHtsId AND d.HtsId <> 0
    LEFT JOIN Sale.OrderDetail od ON od.HtsId = s.OrderDetailHtsId AND od.HtsId <> 0
    LEFT JOIN Inv.Part p ON p.HtsId = s.ReplacePartHtsId AND p.HtsId <> 0
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.ServiceRequestDetailId=s.ServiceRequestDetailId, t.OrderDetailId=s.OrderDetailId,
    t.ReplacePartId=s.ReplacePartId, t.ServiceType=s.ServiceType, t.Mount=s.Mount, t.UnitPrice=s.UnitPrice,
    t.IsRegisteredManually=s.IsRegisteredManually, t.HtsProjectVchItemId=s.HtsProjectVchItemId,
    t.HtsProjectVchPartId=s.HtsProjectVchPartId, t.ProjectVchNum=s.ProjectVchNum, t.ProjectVchYear=s.ProjectVchYear,
    t.Comment=s.Comment, t.ModifiedById=1, t.ModifiedByName=@SeedUser,
    t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, ServiceRequestDetailId, OrderDetailId, ReplacePartId, ServiceType, Mount,
    UnitPrice, IsRegisteredManually, HtsProjectVchItemId, HtsProjectVchPartId, ProjectVchNum, ProjectVchYear, Comment,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName,
    ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.ServiceRequestDetailId, s.OrderDetailId, s.ReplacePartId, s.ServiceType, s.Mount,
    s.UnitPrice, s.IsRegisteredManually, s.HtsProjectVchItemId, s.HtsProjectVchPartId, s.ProjectVchNum, s.ProjectVchYear,
    s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @Rc = @@ROWCOUNT;
PRINT N'ServiceRequestPart MERGE touched: ' + CAST(@Rc AS NVARCHAR(20));

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'ServiceRequestPartDetail', s.HtsId, N'ServiceRequest_Detail_FK not mapped'
FROM #TmsSrPart s
WHERE s.DetailHtsId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Sale.ServiceRequestDetail d WHERE d.HtsId = s.DetailHtsId AND d.HtsId <> 0);

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'ServiceRequestPartOrderDetail', s.HtsId, N'OrderDetail_FK not mapped'
FROM #TmsSrPart s
WHERE s.OrderDetailHtsId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Sale.OrderDetail od WHERE od.HtsId = s.OrderDetailHtsId AND od.HtsId <> 0);

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'ServiceRequestPartReplacePart', s.HtsId, N'ReplacePart_FK not mapped to Inv.Part.HtsId'
FROM #TmsSrPart s
WHERE s.ReplacePartHtsId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Inv.Part p WHERE p.HtsId = s.ReplacePartHtsId AND p.HtsId <> 0);

SELECT Kind, COUNT(*) AS Cnt FROM #Unmapped GROUP BY Kind ORDER BY Kind;
SELECT TOP 20 * FROM #Unmapped ORDER BY Kind, OldId;

SELECT
    (SELECT COUNT(*) FROM Sale.Mission) AS MissionHavayar,
    (SELECT COUNT(*) FROM Sale.MissionSalary) AS MissionSalaryHavayar,
    (SELECT COUNT(*) FROM Sale.WorkReport) AS WorkReportHavayar,
    (SELECT COUNT(*) FROM Sale.WorkReportAttachment) AS WorkReportAttHavayar,
    (SELECT COUNT(*) FROM Sale.ServiceRequestPart) AS SrPartHavayar;

IF @Strict = 1 AND EXISTS (SELECT 1 FROM #Unmapped)
BEGIN
    RAISERROR(N'@Strict=1 and unmapped FK rows remain in phase 6.', 16, 1);
END;

COMMIT TRANSACTION;
PRINT N'=== DONE SyncAfterSalesPhase6FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
