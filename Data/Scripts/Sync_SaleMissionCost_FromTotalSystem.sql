/*
  Sync Sale.Mission cost inputs from TotalSystem.Sale_Mission.
  Other_Cost was not copied by the first phase-6 merge, and CostCenter_FK was not mapped.
  Fee, count, and factor columns are refreshed so the HTS Total_Cost formula matches.
  Encoding: ASCII comments. Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked server [TMS] was not found.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'Sale.Mission', N'OtherCost') IS NULL
    ALTER TABLE [Sale].[Mission] ADD [OtherCost] bigint NULL;

IF OBJECT_ID('tempdb..#MissionCostCenterMap') IS NOT NULL DROP TABLE #MissionCostCenterMap;
SELECT
    cc.CostCenter_ID AS OldCostCenterId,
    COALESCE(byHam.Id, byTitle.Id) AS NewCostCenterId
INTO #MissionCostCenterMap
FROM [TMS].[TotalSystem].[dbo].[Acc_CostCenter] cc
OUTER APPLY (
    SELECT TOP (1) c.Id
    FROM Gnr.CostCenter c
    WHERE ISNULL(cc.Hamkaran_CostCenter_FK, 0) <> 0
      AND c.HamkaranId = CAST(cc.Hamkaran_CostCenter_FK AS BIGINT)
    ORDER BY c.Id
) byHam
OUTER APPLY (
    SELECT TOP (1) c.Id
    FROM Gnr.CostCenter c
    WHERE byHam.Id IS NULL
      AND NULLIF(LTRIM(RTRIM(cc.CostCenter_Title)), N'') IS NOT NULL
      AND LTRIM(RTRIM(c.Title)) = LTRIM(RTRIM(cc.CostCenter_Title))
    ORDER BY c.Id
) byTitle;

UPDATE t
SET
    t.BreakfastFee = s.Breakfast_Fee,
    t.Breakfast = s.Breakfast,
    t.LunchFee = s.Lunch_Fee,
    t.Lunch = s.Lunch,
    t.DinnerFee = s.Dinner_Fee,
    t.Dinner = s.Dinner,
    t.NightRight = s.NightRight,
    t.NightRightFactor = s.NightRight_Factor,
    t.WorkOvertimeFee = s.Work_Overtime_Fee,
    t.WorkOvertimeHour = s.Work_Overtime_Hour,
    t.MissionRight = s.Mission_Right,
    t.MissionRightFactor = s.Mission_Right_Factor,
    t.MissionHolidayRight = s.Mission_HolidayRight,
    t.MissionHolidayRightFactor = s.Mission_HolidayRight_Factor,
    t.TransportationPersonal = s.Transportation_Personal,
    t.TransportationDriving = s.Transportation_Driving,
    t.OtherCost = s.Other_Cost,
    t.HasFactorBreakfast = ISNULL(s.HasFactorBreakfast, 0),
    t.HasFactorLunch = ISNULL(s.HasFactorLunch, 0),
    t.HasFactorDinner = ISNULL(s.HasFactorDinner, 0),
    t.HasFactorHotel = ISNULL(s.HasFactorHotel, 0),
    t.HotelCost = s.HotelCost,
    t.CostCenterId = CASE WHEN s.CostCenter_FK IS NULL THEN NULL ELSE ccm.NewCostCenterId END
FROM [Sale].[Mission] t
INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Mission] s ON s.Mission_Id = t.HtsId
LEFT JOIN #MissionCostCenterMap ccm ON ccm.OldCostCenterId = s.CostCenter_FK
WHERE t.HtsId <> 0;

SELECT @@ROWCOUNT AS MissionsUpdated;
