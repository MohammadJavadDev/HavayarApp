/*
Sync_Inv_PartExtraInfo_FromTotalSystem.sql
شناسنامه محصولات: چهار جدول HTS → Inv.PartExtraInfo

  Inv_Part_ExtraInfo          InfoType = 1  کمپرسور اسکرو اویل اینجکت
  Inv_Part_OilFreeExtraInfo   InfoType = 2  کمپرسور اسکرو اویل فری
  Inv_Part_DryerExtraInfo     InfoType = 3  درایر
  Inv_Part_PsaExtraInfo       InfoType = 4  PSA   (EntityTypeId = 3050)
                              InfoType = 5  ثابت  (EntityTypeId = 3051)

کالا: Inv.Part.HtsId = Part_FK
نسخه تکراری همان کالا: آخرین شناسه HTS نگه داشته می‌شود.
درایر DesignType باید 708/709/3056 باشد، نه 654/655/656 (656 = Direct Coupling).
DryerExtraInfo برای نوع 3 و 4 و 5 دیکشنری دیتاشیت است: {"Silencer":{"Qty":1,"Brand":"..."}}.
کلیدها نام PartDataSheetEnum هستند. ردیف تخت HTS (SilencerQty) در دیتاشیت خوانده نمی‌شود.
UTF-8 BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'TMS linked server not found.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'Inv.PartExtraInfo', N'OilCapacityLiter') IS NULL
    ALTER TABLE Inv.PartExtraInfo ADD OilCapacityLiter decimal(18, 2) NULL;

IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
SELECT OldUserId, NewUserId, NewUserName
INTO #UserMap
FROM (
    SELECT
        CAST(ou.User_ID AS INT) AS OldUserId,
        nu.Id AS NewUserId,
        LEFT(COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username), 150) AS NewUserName,
        ROW_NUMBER() OVER (
            PARTITION BY ou.User_ID
            ORDER BY CASE WHEN LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username))) THEN 0 ELSE 1 END, nu.Id
        ) AS rn
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN system.[User] nu
        ON LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
        OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
            AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
        OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
            AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
) x
WHERE rn = 1;
CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

IF OBJECT_ID('tempdb..#PartMap') IS NOT NULL DROP TABLE #PartMap;
SELECT HtsId, Id AS ProductId
INTO #PartMap
FROM Inv.Part
WHERE HtsId <> 0;
CREATE UNIQUE CLUSTERED INDEX IX_PartMap ON #PartMap (HtsId);

/* درایرهای قبلی: نوع طراحی و پسوند مدل از JSON همان ردیف HTS */
UPDATE n
SET
    DesignType = COALESCE(
        TRY_CAST(JSON_VALUE(n.DryerExtraInfo, '$.DesignId') AS int),
        CASE n.DesignType WHEN 654 THEN 708 WHEN 656 THEN 709 WHEN 655 THEN 3056 ELSE n.DesignType END),
    ModelSuffix = COALESCE(
        NULLIF(LTRIM(RTRIM(n.ModelSuffix)), N''),
        NULLIF(LTRIM(RTRIM(JSON_VALUE(n.DryerExtraInfo, '$.ModelSuffix'))), N'')),
    CreatedById = COALESCE(n.CreatedById, u.NewUserId),
    CreatedByName = COALESCE(NULLIF(LTRIM(RTRIM(n.CreatedByName)), N''), u.NewUserName),
    CreatedOnShamsiDateTime = COALESCE(
        NULLIF(LTRIM(RTRIM(n.CreatedOnShamsiDateTime)), N''),
        LEFT(
            LTRIM(RTRIM(JSON_VALUE(n.DryerExtraInfo, '$.CreatedDate')))
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(JSON_VALUE(n.DryerExtraInfo, '$.CreatedTime'))), N'') IS NULL THEN N''
                ELSE N' ' + LTRIM(RTRIM(JSON_VALUE(n.DryerExtraInfo, '$.CreatedTime')))
              END, 30)),
    CreatedOnMiladiDateTime = COALESCE(
        n.CreatedOnMiladiDateTime,
        dbo.fn_ShamsiToMiladiDateTime(
            JSON_VALUE(n.DryerExtraInfo, '$.CreatedDate'),
            JSON_VALUE(n.DryerExtraInfo, '$.CreatedTime'),
            N'/', N':'))
FROM Inv.PartExtraInfo n
LEFT JOIN #UserMap u ON u.OldUserId = TRY_CAST(JSON_VALUE(n.DryerExtraInfo, '$.CreatedUser_FK') AS int)
WHERE n.InfoType = 3
  AND ISJSON(n.DryerExtraInfo) = 1;

IF OBJECT_ID('tempdb..#Oil') IS NOT NULL DROP TABLE #Oil;
SELECT *
INTO #Oil
FROM (
    SELECT e.*, ROW_NUMBER() OVER (PARTITION BY e.Part_FK, e.Revision ORDER BY e.Part_ExtraInfo_ID DESC) AS rn
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part_ExtraInfo] e
) x
WHERE x.rn = 1;

IF OBJECT_ID('tempdb..#Free') IS NOT NULL DROP TABLE #Free;
SELECT *
INTO #Free
FROM (
    SELECT e.*, ROW_NUMBER() OVER (PARTITION BY e.Part_FK, e.Revision ORDER BY e.Part_OilFreeExtraInfo_ID DESC) AS rn
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part_OilFreeExtraInfo] e
) x
WHERE x.rn = 1;

UPDATE n
SET
    n.OilCapacityLiter = s.OilCapacityLiter,
    n.AirendQty = COALESCE(n.AirendQty, s.AirendQty),
    n.AirendBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.AirendBrand)), N''), LEFT(s.AirendBrand, 256)),
    n.ElectroMotorQty = COALESCE(n.ElectroMotorQty, s.ElectroMotorQty),
    n.ElectroMotorBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.ElectroMotorBrand)), N''), LEFT(s.ElectroMotorBrand, 256)),
    n.CoolingFanQty = COALESCE(n.CoolingFanQty, s.CoolingFanQty),
    n.CoolingFanBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.CoolingFanBrand)), N''), LEFT(s.CoolingFanBrand, 256)),
    n.UnloaderValveQty = COALESCE(n.UnloaderValveQty, s.UnloaderValveQty),
    n.UnloaderValveBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.UnloaderValveBrand)), N''), LEFT(s.UnloaderValveBrand, 256)),
    n.MinimumPressureValveQty = COALESCE(n.MinimumPressureValveQty, s.MinimumPressureValveQty),
    n.MinimumPressureValveBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.MinimumPressureValveBrand)), N''), LEFT(s.MinimumPressureValveBrand, 256)),
    n.OilTermostaticValveQty = COALESCE(n.OilTermostaticValveQty, s.OilTermostaticValveQty),
    n.OilTermostaticValveBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.OilTermostaticValveBrand)), N''), LEFT(s.OilTermostaticValveBrand, 256)),
    n.AirOilFilterSepratorQty = COALESCE(n.AirOilFilterSepratorQty, s.AirOilFilterSepratorQty),
    n.AirOilFilterSepratorBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.AirOilFilterSepratorBrand)), N''), LEFT(s.AirOilFilterSepratorBrand, 256)),
    n.AirOilFilterSeparatorBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.AirOilFilterSeparatorBrand)), N''), LEFT(s.AirOilFilterSepratorBrand, 200)),
    n.AirIntakeFilterQty = COALESCE(n.AirIntakeFilterQty, s.AirIntakeFilterQty),
    n.AirIntakeFilterBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.AirIntakeFilterBrand)), N''), LEFT(s.AirIntakeFilterBrand, 256)),
    n.TemperatureSensorQty = COALESCE(n.TemperatureSensorQty, s.TemperatureSensorQty),
    n.TemperatureSensorBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.TemperatureSensorBrand)), N''), LEFT(s.TemperatureSensorBrand, 256)),
    n.PressureSensorQty = COALESCE(n.PressureSensorQty, s.PressureSensorQty),
    n.PressureSensorBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.PressureSensorBrand)), N''), LEFT(s.PressureSensorBrand, 256)),
    n.PressureIndicatorQty = COALESCE(n.PressureIndicatorQty, s.PressureIndicatorQty),
    n.PressureIndicatorBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.PressureIndicatorBrand)), N''), LEFT(s.PressureIndicatorBrand, 256)),
    n.SafetyValveQty = COALESCE(n.SafetyValveQty, s.SafetyValveQty),
    n.SafetyValveBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.SafetyValveBrand)), N''), LEFT(s.SafetyValveBrand, 256)),
    n.CouplingPulleyQty = COALESCE(n.CouplingPulleyQty, s.CouplingPulleyQty),
    n.CouplingPulleyBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.CouplingPulleyBrand)), N''), LEFT(s.CouplingPulleyBrand, 256)),
    n.CouplingPulley2Qty = COALESCE(n.CouplingPulley2Qty, s.CouplingPulley2Qty),
    n.CouplingPulley2Brand = COALESCE(NULLIF(LTRIM(RTRIM(n.CouplingPulley2Brand)), N''), LEFT(s.CouplingPulley2Brand, 256)),
    n.CoolerModelQty = COALESCE(n.CoolerModelQty, s.CoolerModelQty),
    n.CoolerModelBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.CoolerModelBrand)), N''), LEFT(s.CoolerModelBrand, 256)),
    n.OilFilterQty = COALESCE(n.OilFilterQty, s.OilFilterQty),
    n.OilFilterBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.OilFilterBrand)), N''), LEFT(s.OilFilterBrand, 256)),
    n.BeltSizeQty = COALESCE(n.BeltSizeQty, s.BeltSizeQty),
    n.BeltSizeBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.BeltSizeBrand)), N''), LEFT(s.BeltSizeBrand, 256)),
    n.OilCapacityQty = COALESCE(n.OilCapacityQty, s.OilCapacityQty),
    n.OilCapacityBrand = COALESCE(NULLIF(LTRIM(RTRIM(n.OilCapacityBrand)), N''), LEFT(s.OilCapacityBrand, 200)),
    n.FanElectroMotor = COALESCE(NULLIF(LTRIM(RTRIM(n.FanElectroMotor)), N''), LEFT(s.FanElectroMotor, 256)),
    n.PowerConsumption = COALESCE(NULLIF(LTRIM(RTRIM(n.PowerConsumption)), N''), CONVERT(nvarchar(50), s.PowerConsumption)),
    n.PressureSafetyValve = COALESCE(n.PressureSafetyValve, s.PressureSafetyValve),
    n.CreatedById = COALESCE(n.CreatedById, u.NewUserId),
    n.CreatedByName = COALESCE(NULLIF(LTRIM(RTRIM(n.CreatedByName)), N''), u.NewUserName)
FROM Inv.PartExtraInfo n
INNER JOIN #PartMap pm ON pm.ProductId = n.ProductId
INNER JOIN #Free s ON s.Part_FK = pm.HtsId AND s.Revision = n.Revision
LEFT JOIN #UserMap u ON u.OldUserId = s.CreatedUser_FK
WHERE n.InfoType = 2;

INSERT INTO Inv.PartExtraInfo (
    ProductId, InfoType, Revision, DesignType, Model, Size, PowerKw, MotorRpm, AirEndRPM, MotorModel,
    WorkPressure, AirEndMode, ClassName, Capacity, GearRatio, Inverter, PurityRate, MeasureAerated,
    CompressorType, PowerTransmission, MinMaxAmbientWorkingTemperature, InstallationLocation,
    HazardousAreaClassification, NoiseLevel, L, W, H, Weight, StartingMethod, ControlPanel, CoolingMethod,
    ConnectionSize, StarterPanel, OilHeater, MotorHeater, SpaceHeater, MotorPTC, AntiCondensationPanel,
    CoolerPanel, HeavyDutyIntakeFilter, DepartmentIntakeFilter, Comment,
    AirendQty, AirendBrand, ElectroMotorQty, ElectroMotorBrand, CoolingFanQty, CoolingFanBrand,
    UnloaderValveQty, UnloaderValveBrand, MinimumPressureValveQty, MinimumPressureValveBrand,
    OilTermostaticValveQty, OilTermostaticValveBrand, AirOilFilterSepratorQty, AirOilFilterSepratorBrand,
    AirOilFilterSeparatorBrand, AirIntakeFilterQty, AirIntakeFilterBrand, TemperatureSensorQty, TemperatureSensorBrand,
    PressureSensorQty, PressureSensorBrand, PressureIndicatorQty, PressureIndicatorBrand,
    SafetyValveQty, SafetyValveBrand, CouplingPulleyQty, CouplingPulleyBrand, CouplingPulley2Qty, CouplingPulley2Brand,
    CoolerModelQty, CoolerModelBrand, OilFilterQty, OilFilterBrand, BeltSizeQty, BeltSizeBrand,
    OilCapacityQty, OilCapacityBrand, BarometricPressure, MaximumTemperature, RelativeHumidity,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
)
SELECT
    pm.ProductId, 1, s.Revision, s.DesignTypeId, ISNULL(s.Model, N''), LEFT(CONVERT(nvarchar(20), s.Size), 150),
    LEFT(s.Power_KW, 200), CONVERT(nvarchar(20), s.Motor_RPM), TRY_CONVERT(int, s.Airend_RPM), LEFT(s.Model_Motor, 200),
    s.PressureWorking, LEFT(s.Model_Airend, 200), LEFT(s.Class, 200), s.Capacity, s.GearRatio, ISNULL(s.Inverter, 0),
    s.PurityRate, s.Measure_Aerated, LEFT(s.CompressorType, 200), s.PowerTransmissionId, LEFT(s.AmbientWorkingTemprature, 200),
    s.InstallationLocationId, s.HazardousAreaClasificationId, TRY_CONVERT(int, ROUND(s.NoiseLevel, 0)),
    s.DimensionL, s.DimensionW, s.DimensionH, TRY_CONVERT(int, ROUND(s.Weight, 0)),
    LEFT(s.StartingMethod, 200), LEFT(s.ControlPanel, 200), s.CoolingMethodId, LEFT(s.ConnectionSize, 200), LEFT(s.StarterPanel, 200),
    ISNULL(s.HasOilHeater, 0), ISNULL(s.HasMotorHeater, 0), ISNULL(s.HasSpaceHeater, 0), ISNULL(s.HasMotorPtc, 0),
    ISNULL(s.HasAntiCondencePanel, 0), ISNULL(s.HasCoolerPanel, 0), ISNULL(s.HasHeavyDutyIntakeFilter, 0), ISNULL(s.HasDptDpsIntakeFilter, 0),
    s.Comment,
    s.AirendQty, LEFT(s.AirendBrand, 256), s.ElectroMotorQty, LEFT(s.ElectroMotorBrand, 256),
    s.CoolingFanQty, LEFT(s.CoolingFanBrand, 256), s.UnloaderValveQty, LEFT(s.UnloaderValveBrand, 256),
    s.MinimumPressureValveQty, LEFT(s.MinimumPressureValveBrand, 256), s.OilTermostaticValveQty, LEFT(s.OilTermostaticValveBrand, 256),
    s.AirOilFilterSepratorQty, LEFT(s.AirOilFilterSepratorBrand, 256), LEFT(s.AirOilFilterSepratorBrand, 200),
    s.AirIntakeFilterQty, LEFT(s.AirIntakeFilterBrand, 256), s.TemperatureSensorQty, LEFT(s.TemperatureSensorBrand, 256),
    s.PressureSensorQty, LEFT(s.PressureSensorBrand, 256), s.PressureIndicatorQty, LEFT(s.PressureIndicatorBrand, 256),
    s.SafetyValveQty, LEFT(s.SafetyValveBrand, 256), s.CouplingPulleyQty, LEFT(s.CouplingPulleyBrand, 256),
    s.CouplingPulley2Qty, LEFT(s.CouplingPulley2Brand, 256), s.CoolerModelQty, LEFT(s.CoolerModelBrand, 256),
    s.OilFilterQty, LEFT(s.OilFilterBrand, 256), s.BeltSizeQty, LEFT(s.BeltSizeBrand, 256),
    s.OilCapacityQty, LEFT(s.OilCapacityBrand, 200), TRY_CONVERT(int, ROUND(s.BarometricPressure, 0)),
    CONVERT(nvarchar(50), s.MaximumTemperature), TRY_CONVERT(int, ROUND(s.RelativeHumidity, 0)),
    u.NewUserId, u.NewUserName, s.CreatedDate, LEFT(s.CreatedDateInText, 30), 1
FROM #Oil s
INNER JOIN #PartMap pm ON pm.HtsId = s.Part_FK
LEFT JOIN #UserMap u ON u.OldUserId = s.CreatedUser_FK
WHERE NOT EXISTS (
    SELECT 1 FROM Inv.PartExtraInfo n
    WHERE n.ProductId = pm.ProductId AND n.Revision = s.Revision AND n.InfoType = 1
);

INSERT INTO Inv.PartExtraInfo (
    ProductId, InfoType, Revision, DesignType, Model, Size, PowerKw, MotorRpm, AirEndRPM, MotorModel,
    WorkPressure, AirEndMode, ClassName, Capacity, GearRatio, Inverter, PurityRate, MeasureAerated,
    CompressorType, PowerTransmission, MinMaxAmbientWorkingTemperature, InstallationLocation,
    HazardousAreaClassification, NoiseLevel, L, W, H, Weight, StartingMethod, ControlPanel, CoolingMethod,
    ConnectionSize, StarterPanel, OilHeater, MotorHeater, SpaceHeater, MotorPTC, AntiCondensationPanel,
    CoolerPanel, HeavyDutyIntakeFilter, DepartmentIntakeFilter, Comment,
    AirendQty, AirendBrand, ElectroMotorQty, ElectroMotorBrand, CoolingFanQty, CoolingFanBrand,
    UnloaderValveQty, UnloaderValveBrand, MinimumPressureValveQty, MinimumPressureValveBrand,
    OilTermostaticValveQty, OilTermostaticValveBrand, AirOilFilterSepratorQty, AirOilFilterSepratorBrand,
    AirOilFilterSeparatorBrand, AirIntakeFilterQty, AirIntakeFilterBrand, TemperatureSensorQty, TemperatureSensorBrand,
    PressureSensorQty, PressureSensorBrand, PressureIndicatorQty, PressureIndicatorBrand,
    SafetyValveQty, SafetyValveBrand, CouplingPulleyQty, CouplingPulleyBrand, CouplingPulley2Qty, CouplingPulley2Brand,
    CoolerModelQty, CoolerModelBrand, OilFilterQty, OilFilterBrand, BeltSizeQty, BeltSizeBrand,
    OilCapacityQty, OilCapacityBrand, OilCapacityLiter, FanElectroMotor, PowerConsumption, PressureSafetyValve,
    BarometricPressure, MaximumTemperature, RelativeHumidity,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
)
SELECT
    pm.ProductId, 2, s.Revision, s.DesignTypeId, ISNULL(s.Model, N''), LEFT(CONVERT(nvarchar(20), s.Size), 150),
    LEFT(s.Power_KW, 200), CONVERT(nvarchar(20), s.Motor_RPM), TRY_CONVERT(int, s.Airend_RPM), LEFT(s.Model_Motor, 200),
    s.PressureWorking, LEFT(s.Model_Airend, 200), LEFT(s.Class, 200), s.Capacity, s.GearRatio, ISNULL(s.Inverter, 0),
    s.PurityRate, s.Measure_Aerated, LEFT(s.CompressorType, 200), s.PowerTransmissionId, LEFT(s.AmbientWorkingTemprature, 200),
    s.InstallationLocationId, s.HazardousAreaClasificationId, TRY_CONVERT(int, ROUND(s.NoiseLevel, 0)),
    s.DimensionL, s.DimensionW, s.DimensionH, TRY_CONVERT(int, ROUND(s.Weight, 0)),
    LEFT(s.StartingMethod, 200), LEFT(s.ControlPanel, 200), s.CoolingMethodId, LEFT(s.ConnectionSize, 200), LEFT(s.StarterPanel, 200),
    ISNULL(s.HasOilHeater, 0), ISNULL(s.HasMotorHeater, 0), ISNULL(s.HasSpaceHeater, 0), ISNULL(s.HasMotorPtc, 0),
    ISNULL(s.HasAntiCondencePanel, 0), ISNULL(s.HasCoolerPanel, 0), ISNULL(s.HasHeavyDutyIntakeFilter, 0), ISNULL(s.HasDptDpsIntakeFilter, 0),
    s.Comment,
    s.AirendQty, LEFT(s.AirendBrand, 256), s.ElectroMotorQty, LEFT(s.ElectroMotorBrand, 256),
    s.CoolingFanQty, LEFT(s.CoolingFanBrand, 256), s.UnloaderValveQty, LEFT(s.UnloaderValveBrand, 256),
    s.MinimumPressureValveQty, LEFT(s.MinimumPressureValveBrand, 256), s.OilTermostaticValveQty, LEFT(s.OilTermostaticValveBrand, 256),
    s.AirOilFilterSepratorQty, LEFT(s.AirOilFilterSepratorBrand, 256), LEFT(s.AirOilFilterSepratorBrand, 200),
    s.AirIntakeFilterQty, LEFT(s.AirIntakeFilterBrand, 256), s.TemperatureSensorQty, LEFT(s.TemperatureSensorBrand, 256),
    s.PressureSensorQty, LEFT(s.PressureSensorBrand, 256), s.PressureIndicatorQty, LEFT(s.PressureIndicatorBrand, 256),
    s.SafetyValveQty, LEFT(s.SafetyValveBrand, 256), s.CouplingPulleyQty, LEFT(s.CouplingPulleyBrand, 256),
    s.CouplingPulley2Qty, LEFT(s.CouplingPulley2Brand, 256), s.CoolerModelQty, LEFT(s.CoolerModelBrand, 256),
    s.OilFilterQty, LEFT(s.OilFilterBrand, 256), s.BeltSizeQty, LEFT(s.BeltSizeBrand, 256),
    s.OilCapacityQty, LEFT(s.OilCapacityBrand, 200), s.OilCapacityLiter, LEFT(s.FanElectroMotor, 256),
    CONVERT(nvarchar(50), s.PowerConsumption), s.PressureSafetyValve,
    TRY_CONVERT(int, ROUND(s.BarometricPressure, 0)), CONVERT(nvarchar(50), s.MaximumTemperature), TRY_CONVERT(int, ROUND(s.RelativeHumidity, 0)),
    u.NewUserId, u.NewUserName, s.CreatedDate, LEFT(s.CreatedDateInText, 30), 1
FROM #Free s
INNER JOIN #PartMap pm ON pm.HtsId = s.Part_FK
LEFT JOIN #UserMap u ON u.OldUserId = s.CreatedUser_FK
WHERE NOT EXISTS (
    SELECT 1 FROM Inv.PartExtraInfo n
    WHERE n.ProductId = pm.ProductId AND n.Revision = s.Revision AND n.InfoType = 2
);

IF OBJECT_ID('tempdb..#Dryer') IS NOT NULL DROP TABLE #Dryer;
SELECT s.*, pm.ProductId
INTO #Dryer
FROM (
    SELECT d.*, ROW_NUMBER() OVER (PARTITION BY d.Part_FK, d.Revision ORDER BY d.Part_DryerExtraInfo_ID DESC) AS rn
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part_DryerExtraInfo] d
) s
INNER JOIN #PartMap pm ON pm.HtsId = s.Part_FK
WHERE s.rn = 1;

INSERT INTO Inv.PartExtraInfo (
    ProductId, InfoType, Revision, DesignType, DryerType, Model, ModelSuffix, DewPoint, WorkPressure, FlowNM3H,
    MinMaxAmbientWorkingTemperature, MaximumTemperature, InstallationLocation, HazardousAreaClassification,
    NoiseLevel, CycleTime, HydrotestPressure, DesignPressure, L, W, H, Weight, InOutNozzle, PowerConsumption,
    LoaderAndPlatform, RalAndTypePaint, Inverter, OilHeater, MotorHeater, SpaceHeater, MotorPTC,
    AntiCondensationPanel, CoolerPanel, HeavyDutyIntakeFilter, DepartmentIntakeFilter,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
)
SELECT
    s.ProductId, 3, s.Revision, s.DesignId, s.DryerTypeId,
    ISNULL(COALESCE(NULLIF(LTRIM(RTRIM(lm.Lookup_Title_Fa)), N''), NULLIF(LTRIM(RTRIM(lm.Lookup_Title_En)), N'')), N''),
    s.ModelSuffix, LEFT(s.DewPoint, 400), s.WorkingPressure, s.Flow_NM3_H,
    LEFT(s.WorkingTemprature, 200), LEFT(s.AmbientTemperature, 400),
    s.InstallationLocationId, s.HazardousAreaClasificationId, TRY_CONVERT(int, ROUND(s.NoiseLevel, 0)),
    LEFT(s.CycleTime, 50), s.HydrotestPressure, s.DesignPressure, s.L, s.W, s.H, s.TotalWeight_Kg,
    LEFT(s.In_OutNozzle, 800), s.PowerConsumption, s.LoaderAndPlatformId, LEFT(s.RalAndTypePaint, 128),
    0, 0, 0, 0, 0, 0, 0, 0, 0,
    u.NewUserId, u.NewUserName,
    dbo.fn_ShamsiToMiladiDateTime(s.CreatedDate, s.CreatedTime, N'/', N':'),
    LEFT(LTRIM(RTRIM(s.CreatedDate)) + CASE WHEN NULLIF(LTRIM(RTRIM(s.CreatedTime)), N'') IS NULL THEN N'' ELSE N' ' + LTRIM(RTRIM(s.CreatedTime)) END, 30),
    1
FROM #Dryer s
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Lookup] lm ON lm.Lookup_ID = s.ModelId
LEFT JOIN #UserMap u ON u.OldUserId = s.CreatedUser_FK
WHERE NOT EXISTS (
    SELECT 1 FROM Inv.PartExtraInfo n
    WHERE n.ProductId = s.ProductId AND n.Revision = s.Revision AND n.InfoType = 3
);

/* دیتاشیت: Json(DryerExtraInfo, 'Silencer.Qty') — شکل {"Silencer":{"Qty":1,"Brand":"..."}} */
UPDATE n
SET n.DryerExtraInfo = j.Doc
FROM Inv.PartExtraInfo n
INNER JOIN #Dryer s ON s.ProductId = n.ProductId AND s.Revision = n.Revision
    AND n.InfoType = 3
CROSS APPLY (
    SELECT ISNULL(
        CAST(N'{' + STRING_AGG(CAST(v.Fragment AS nvarchar(max)), N',') WITHIN GROUP (ORDER BY v.JsonKey) + N'}' AS nvarchar(max)),
        N'{}') AS Doc
    FROM (
        SELECT
            k.JsonKey,
            N'"' + k.JsonKey + N'":' + JSON_OBJECT(
                N'Qty': k.Qty,
                N'Brand': NULLIF(LTRIM(RTRIM(k.Brand)), N'')
                ABSENT ON NULL
            ) AS Fragment
        FROM (VALUES
            (N'Control_Board_Type', s.ControlBoardTypeQty, s.ControlBoardTypeBrand),
            (N'Desiccant_Type_1', s.DesiccantType1Qty, s.DesiccantType1Brand),
            (N'Desiccant_Type_2', s.DesiccantType2Qty, s.DesiccantType2Brand),
            (N'Ceramic_Ball_Type', s.CeramicBallTypeQty, s.CeramicBallTypeBrand),
            (N'Pressure_Switch', s.PressureSwitchQty, s.PressureSwitchBrand),
            (N'Pressure_Gauge', s.PressureGaugeQty, s.PressureGaugeBrand),
            (N'Silencer', s.SilencerQty, s.SilencerBrand),
            (N'Heater', s.HeaterQty, s.HeaterBrand),
            (N'Regeneration_Valve', s.RegenerationValveQty, s.RegenerationValveBrand),
            (N'Adjusting_Purge_Valve_Type', s.AdjustingPurgeValveTypeQty, s.AdjustingPurgeValveTypeBrand),
            (N'Tempreture_Transmitter', s.TempretureTransmitterQty, s.TempretureTransmitterBrand),
            (N'Blower', s.BlowerQty, s.BlowerBrand),
            (N'Butterfly_Valve', s.ButterflyValveQty, s.ButterflyValveBrand),
            (N'Angle_Seat_Valve', s.AngleSeatValveQty, s.AngleSeatValveBrand),
            (N'Ball_Valve', s.BallValveQty, s.BallValveBrand),
            (N'Check_Valve', s.CheckValveQty, s.CheckValveBrand),
            (N'Shuttle_Valve', s.ShuttleValveQty, s.ShuttleValveBrand),
            (N'Pressure_Safety_Valve', s.PressureSafetyValveQty, s.PressureSafetyValveBrand),
            (N'Ball_Valve_12_inch', s.BallValveQty1_2, s.BallValveBrand1_2),
            (N'Ball_Valve_34_inch', s.BallValveQty3_4, s.BallValveBrand3_4),
            (N'Ball_Valve_1_inch', s.BallValveQty1, s.BallValveBrand1),
            (N'Ball_Valve_114_inch', s.BallValveQty1_1_4, s.BallValveBrand1_1_4),
            (N'Ball_Valve_112_inch', s.BallValveQty1_1_2, s.BallValveBrand1_1_2),
            (N'Ball_Valve_2_inch', s.BallValveQty2, s.BallValveBrand2),
            (N'Ball_Valve_212_inch', s.BallValveQty2_1_2, s.BallValveBrand2_1_2),
            (N'Ball_Valve_3_inch', s.BallValveQty3, s.BallValveBrand3),
            (N'Ball_Valve_4_inch', s.BallValveQty4, s.BallValveBrand4),
            (N'Ball_Valve_5_inch', s.BallValveQty5, s.BallValveBrand5),
            (N'Ball_Valve_6_inch', s.BallValveQty6, s.BallValveBrand6),
            (N'Ball_Valve_8_inch', s.BallValveQty8, s.BallValveBrand8),
            (N'Ball_Valve_10_inch', s.BallValveQty10, s.BallValveBrand10),
            (N'Check_Valve_12_inch', s.CheckValveQty1_2, s.CheckValveBrand1_2),
            (N'Check_Valve_34_inch', s.CheckValveQty3_4, s.CheckValveBrand3_4),
            (N'Check_Valve_1_inch', s.CheckValveQty1, s.CheckValveBrand1),
            (N'Check_Valve_114_inch', s.CheckValveQty1_1_4, s.CheckValveBrand1_1_4),
            (N'Check_Valve_112_inch', s.CheckValveQty1_1_2, s.CheckValveBrand1_1_2),
            (N'Check_Valve_2_inch', s.CheckValveQty2, s.CheckValveBrand2),
            (N'Check_Valve_212_inch', s.CheckValveQty2_1_2, s.CheckValveBrand2_1_2),
            (N'Check_Valve_3_inch', s.CheckValveQty3, s.CheckValveBrand3),
            (N'Check_Valve_4_inch', s.CheckValveQty4, s.CheckValveBrand4),
            (N'Check_Valve_5_inch', s.CheckValveQty5, s.CheckValveBrand5),
            (N'Check_Valve_6_inch', s.CheckValveQty6, s.CheckValveBrand6),
            (N'Check_Valve_8_inch', s.CheckValveQty8, s.CheckValveBrand8),
            (N'Check_Valve_10_inch', s.CheckValveQty10, s.CheckValveBrand10),
            (N'Butterfly_Valve_12_inch', s.ButterflyValveQty1_2, s.ButterflyValveBrand1_2),
            (N'Butterfly_Valve_34_inch', s.ButterflyValveQty3_4, s.ButterflyValveBrand3_4),
            (N'Butterfly_Valve_1_inch', s.ButterflyValveQty1, s.ButterflyValveBrand1),
            (N'Butterfly_Valve_114_inch', s.ButterflyValveQty1_1_4, s.ButterflyValveBrand1_1_4),
            (N'Butterfly_Valve_112_inch', s.ButterflyValveQty1_1_2, s.ButterflyValveBrand1_1_2),
            (N'Butterfly_Valve_2_inch', s.ButterflyValveQty2, s.ButterflyValveBrand2),
            (N'Butterfly_Valve_212_inch', s.ButterflyValveQty2_1_2, s.ButterflyValveBrand2_1_2),
            (N'Butterfly_Valve_3_inch', s.ButterflyValveQty3, s.ButterflyValveBrand3),
            (N'Butterfly_Valve_4_inch', s.ButterflyValveQty4, s.ButterflyValveBrand4),
            (N'Butterfly_Valve_5_inch', s.ButterflyValveQty5, s.ButterflyValveBrand5),
            (N'Butterfly_Valve_6_inch', s.ButterflyValveQty6, s.ButterflyValveBrand6),
            (N'Butterfly_Valve_8_inch', s.ButterflyValveQty8, s.ButterflyValveBrand8),
            (N'Butterfly_Valve_10_inch', s.ButterflyValveQty10, s.ButterflyValveBrand10),
            (N'Angle_Seat_Valve_12_inch', s.AngleSeatValveQty1_2, s.AngleSeatValveBrand1_2),
            (N'Angle_Seat_Valve_34_inch', s.AngleSeatValveQty3_4, s.AngleSeatValveBrand3_4),
            (N'Angle_Seat_Valve_1_inch', s.AngleSeatValveQty1, s.AngleSeatValveBrand1),
            (N'Angle_Seat_Valve_114_inch', s.AngleSeatValveQty1_1_4, s.AngleSeatValveBrand1_1_4),
            (N'Angle_Seat_Valve_112_inch', s.AngleSeatValveQty1_1_2, s.AngleSeatValveBrand1_1_2),
            (N'Angle_Seat_Valve_2_inch', s.AngleSeatValveQty2, s.AngleSeatValveBrand2),
            (N'Angle_Seat_Valve_212_inch', s.AngleSeatValveQty2_1_2, s.AngleSeatValveBrand2_1_2),
            (N'Angle_Seat_Valve_3_inch', s.AngleSeatValveQty3, s.AngleSeatValveBrand3),
            (N'Angle_Seat_Valve_4_inch', s.AngleSeatValveQty4, s.AngleSeatValveBrand4),
            (N'Angle_Seat_Valve_5_inch', s.AngleSeatValveQty5, s.AngleSeatValveBrand5),
            (N'Angle_Seat_Valve_6_inch', s.AngleSeatValveQty6, s.AngleSeatValveBrand6),
            (N'Angle_Seat_Valve_8_inch', s.AngleSeatValveQty8, s.AngleSeatValveBrand8),
            (N'Angle_Seat_Valve_10_inch', s.AngleSeatValveQty10, s.AngleSeatValveBrand10),
            (N'Shuttle_Valve_12_inch', s.ShuttleValveQty1_2, s.ShuttleValveBrand1_2),
            (N'Shuttle_Valve_34_inch', s.ShuttleValveQty3_4, s.ShuttleValveBrand3_4),
            (N'Shuttle_Valve_1_inch', s.ShuttleValveQty1, s.ShuttleValveBrand1),
            (N'Shuttle_Valve_114_inch', s.ShuttleValveQty1_1_4, s.ShuttleValveBrand1_1_4),
            (N'Shuttle_Valve_112_inch', s.ShuttleValveQty1_1_2, s.ShuttleValveBrand1_1_2),
            (N'Shuttle_Valve_2_inch', s.ShuttleValveQty2, s.ShuttleValveBrand2),
            (N'Shuttle_Valve_212_inch', s.ShuttleValveQty2_1_2, s.ShuttleValveBrand2_1_2),
            (N'Shuttle_Valve_3_inch', s.ShuttleValveQty3, s.ShuttleValveBrand3),
            (N'Shuttle_Valve_4_inch', s.ShuttleValveQty4, s.ShuttleValveBrand4),
            (N'Shuttle_Valve_5_inch', s.ShuttleValveQty5, s.ShuttleValveBrand5),
            (N'Shuttle_Valve_6_inch', s.ShuttleValveQty6, s.ShuttleValveBrand6),
            (N'Shuttle_Valve_8_inch', s.ShuttleValveQty8, s.ShuttleValveBrand8),
            (N'Shuttle_Valve_10_inch', s.ShuttleValveQty10, s.ShuttleValveBrand10),
            (N'Pressure_Safety_Valve_12_inch', s.PressureSafetyValveQty1_2, s.PressureSafetyValveBrand1_2),
            (N'Pressure_Safety_Valve_34_inch', s.PressureSafetyValveQty3_4, s.PressureSafetyValveBrand3_4),
            (N'Pressure_Safety_Valve_1_inch', s.PressureSafetyValveQty1, s.PressureSafetyValveBrand1),
            (N'Pressure_Safety_Valve_114_inch', s.PressureSafetyValveQty1_1_4, s.PressureSafetyValveBrand1_1_4),
            (N'Pressure_Safety_Valve_112_inch', s.PressureSafetyValveQty1_1_2, s.PressureSafetyValveBrand1_1_2),
            (N'Pressure_Safety_Valve_2_inch', s.PressureSafetyValveQty2, s.PressureSafetyValveBrand2),
            (N'Pressure_Safety_Valve_212_inch', s.PressureSafetyValveQty2_1_2, s.PressureSafetyValveBrand2_1_2),
            (N'Pressure_Safety_Valve_3_inch', s.PressureSafetyValveQty3, s.PressureSafetyValveBrand3),
            (N'Pressure_Safety_Valve_4_inch', s.PressureSafetyValveQty4, s.PressureSafetyValveBrand4),
            (N'Pressure_Safety_Valve_5_inch', s.PressureSafetyValveQty5, s.PressureSafetyValveBrand5),
            (N'Pressure_Safety_Valve_6_inch', s.PressureSafetyValveQty6, s.PressureSafetyValveBrand6),
            (N'Pressure_Safety_Valve_8_inch', s.PressureSafetyValveQty8, s.PressureSafetyValveBrand8),
            (N'Pressure_Safety_Valve_10_inch', s.PressureSafetyValveQty10, s.PressureSafetyValveBrand10)
        ) k(JsonKey, Qty, Brand)
        WHERE k.Qty IS NOT NULL OR NULLIF(LTRIM(RTRIM(k.Brand)), N'') IS NOT NULL
    ) v
) j
WHERE n.InfoType = 3;

IF OBJECT_ID('tempdb..#Psa') IS NOT NULL DROP TABLE #Psa;
SELECT s.*, pm.ProductId
INTO #Psa
FROM (
    SELECT d.*, ROW_NUMBER() OVER (PARTITION BY d.Part_FK, d.Revision, d.EntityTypeId ORDER BY d.Part_PsaExtraInfo_ID DESC) AS rn
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part_PsaExtraInfo] d
    WHERE d.EntityTypeId IN (3050, 3051)
) s
INNER JOIN #PartMap pm ON pm.HtsId = s.Part_FK
WHERE s.rn = 1;

INSERT INTO Inv.PartExtraInfo (
    ProductId, InfoType, Revision, DesignType, PsaType, OperationalDesign, OpeningType, Model, ModelSuffix,
    WorkPressure, MinMaxAmbientWorkingTemperature, InstallationLocation, HazardousAreaClassification,
    NoiseLevel, HydrotestPressure, DesignPressure, L, W, H, Weight, InOutNozzle, PowerConsumption,
    LoaderAndPlatform, InletAirPressureBarg, InletAirTemperature, N2FlowRate, N2Purity, DryAirConsumption,
    Inverter, OilHeater, MotorHeater, SpaceHeater, MotorPTC, AntiCondensationPanel, CoolerPanel,
    HeavyDutyIntakeFilter, DepartmentIntakeFilter,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
)
SELECT
    s.ProductId, CASE WHEN s.EntityTypeId = 3051 THEN 5 ELSE 4 END, s.Revision, s.DesignId, s.PsaTypeId,
    s.OperationalDesignId, s.OpeningTypeId,
    ISNULL(COALESCE(NULLIF(LTRIM(RTRIM(lm.Lookup_Title_Fa)), N''), NULLIF(LTRIM(RTRIM(lm.Lookup_Title_En)), N'')), N''),
    s.ModelSuffix, s.WorkingPressure, LEFT(s.WorkingTemprature, 200),
    s.InstallationLocationId, s.HazardousAreaClasificationId, TRY_CONVERT(int, ROUND(s.NoiseLevel, 0)),
    s.HydrotestPressure, s.DesignPressure, s.L, s.W, s.H, TRY_CONVERT(int, ROUND(s.Weight_Kg, 0)),
    LEFT(s.In_OutNozzle, 800), s.PowerConsumption, s.LoaderAndPlatformId,
    s.InletAirPressure_Barg, s.InletAirTemperature, s.N2FlowRate, s.N2Purity, s.DryAirConsumption,
    0, 0, 0, 0, 0, 0, 0, 0, 0,
    u.NewUserId, u.NewUserName,
    dbo.fn_ShamsiToMiladiDateTime(s.CreatedDate, s.CreatedTime, N'/', N':'),
    LEFT(LTRIM(RTRIM(s.CreatedDate)) + CASE WHEN NULLIF(LTRIM(RTRIM(s.CreatedTime)), N'') IS NULL THEN N'' ELSE N' ' + LTRIM(RTRIM(s.CreatedTime)) END, 30),
    1
FROM #Psa s
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Lookup] lm ON lm.Lookup_ID = s.ModelId
LEFT JOIN #UserMap u ON u.OldUserId = s.CreatedUser_FK
WHERE NOT EXISTS (
    SELECT 1 FROM Inv.PartExtraInfo n
    WHERE n.ProductId = s.ProductId
      AND n.Revision = s.Revision
      AND n.InfoType = CASE WHEN s.EntityTypeId = 3051 THEN 5 ELSE 4 END
);

UPDATE n
SET n.DryerExtraInfo = j.Doc
FROM Inv.PartExtraInfo n
INNER JOIN #Psa s ON s.ProductId = n.ProductId AND s.Revision = n.Revision
    AND n.InfoType = CASE WHEN s.EntityTypeId = 3051 THEN 5 ELSE 4 END
CROSS APPLY (
    SELECT ISNULL(
        CAST(N'{' + STRING_AGG(CAST(v.Fragment AS nvarchar(max)), N',') WITHIN GROUP (ORDER BY v.JsonKey) + N'}' AS nvarchar(max)),
        N'{}') AS Doc
    FROM (
        SELECT
            k.JsonKey,
            N'"' + k.JsonKey + N'":' + JSON_OBJECT(
                N'Qty': k.Qty,
                N'Brand': NULLIF(LTRIM(RTRIM(k.Brand)), N'')
                ABSENT ON NULL
            ) AS Fragment
        FROM (VALUES
            (N'Pressure_Safety_Valve', s.PressureSafetyValveQty, s.PressureSafetyValveBrand),
            (N'VIP_Valve_38', s.VipValve3_8Qty, s.VipValve3_8Brand),
            (N'VIP_Valve_12', s.VipValve1_2Qty, s.VipValve1_2Brand),
            (N'VIP_Valve_34', s.VipValve3_4Qty, s.VipValve3_4Brand),
            (N'VIP_Valve_1', s.VipValve1Qty, s.VipValve1Brand),
            (N'VIP_Valve_114', s.VipValve1_1_4Qty, s.VipValve1_1_4Brand),
            (N'VIP_Valve_112', s.VipValve1_1_2Qty, s.VipValve1_1_2Brand),
            (N'Angle_Seat_Valve', s.AngleSeatValveQty, s.AngleSeatValveBrand),
            (N'Angle_Seat_Valve_112_inch2', s.AngleSeatValve1_1_2Qty, s.AngleSeatValve1_1_2Brand),
            (N'Control_Board_Type', s.ControlBoardTypeQty, s.ControlBoardTypeBrand),
            (N'Desiccant_Type_1', s.DesiccantTypeQty, s.DesiccantTypeBrand),
            (N'Adsorber_Type_Kg', s.AdsorberTypeQty, s.AdsorberTypeBrand),
            (N'Ceramic_Ball_Type', s.CeramicBallType1Qty, s.CeramicBallType1Brand),
            (N'Ceramic_Ball_Type_2', s.CeramicBallType2Qty, s.CeramicBallType2Brand),
            (N'Coconut_Top_of_Adsorbers', s.CoconutTopOfAdsorbersQty, s.CoconutTopOfAdsorbersBrand),
            (N'Silencer', s.SilencerQty, s.SilencerBrand),
            (N'Pressure_Sensor', s.PressureSensorQty, s.PressureSensorBrand),
            (N'Pressure_Gauge', s.PressureIndicatorQty, s.PressureIndicatorBrand),
            (N'Oxygen_Analyzer', s.OxygenAnalyzerQty, s.OxygenAnalyzerBrand),
            (N'HDM_Valve_Block', s.HDMValveBlockQty, s.HDMValveBlockBrand),
            (N'Flowmeter', s.FlowmeterQty, s.FlowmeterBrand),
            (N'Diffuser', s.DiffuserQty, s.DiffuserBrand)
        ) k(JsonKey, Qty, Brand)
        WHERE k.Qty IS NOT NULL OR NULLIF(LTRIM(RTRIM(k.Brand)), N'') IS NOT NULL
    ) v
) j
WHERE n.InfoType IN (4, 5);

;WITH latest AS (
    SELECT ProductId, DesignType,
           ROW_NUMBER() OVER (PARTITION BY ProductId ORDER BY ISNULL(Revision, -1) DESC, Id DESC) AS rn
    FROM Inv.PartExtraInfo
)
UPDATE p
SET p.DesignTypeIsRoutine = CASE WHEN l.DesignType IN (654, 708) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
FROM Inv.Part p
INNER JOIN latest l ON l.ProductId = p.Id AND l.rn = 1
WHERE ISNULL(CAST(p.DesignTypeIsRoutine AS int), -1) <> CASE WHEN l.DesignType IN (654, 708) THEN 1 ELSE 0 END;

SELECT N'byType' AS Metric, CAST(InfoType AS nvarchar(20)) AS K, COUNT(*) AS C
FROM Inv.PartExtraInfo
GROUP BY InfoType
UNION ALL
SELECT N'dryerDesign', CAST(DesignType AS nvarchar(20)), COUNT(*)
FROM Inv.PartExtraInfo
WHERE InfoType = 3
GROUP BY DesignType
UNION ALL
SELECT N'oilUnmatched', N'1', COUNT(*)
FROM #Oil s
WHERE NOT EXISTS (SELECT 1 FROM #PartMap pm WHERE pm.HtsId = s.Part_FK)
UNION ALL
SELECT N'freeUnmatched', N'2', COUNT(*)
FROM #Free s
WHERE NOT EXISTS (SELECT 1 FROM #PartMap pm WHERE pm.HtsId = s.Part_FK)
UNION ALL
SELECT N'dryerUnmatched', N'3', COUNT(*)
FROM (
    SELECT d.Part_FK, d.Revision, ROW_NUMBER() OVER (PARTITION BY d.Part_FK, d.Revision ORDER BY d.Part_DryerExtraInfo_ID DESC) rn
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part_DryerExtraInfo] d
) s
WHERE s.rn = 1 AND NOT EXISTS (SELECT 1 FROM #PartMap pm WHERE pm.HtsId = s.Part_FK)
UNION ALL
SELECT N'psaUnmatched', N'4', COUNT(*)
FROM (
    SELECT d.Part_FK, ROW_NUMBER() OVER (PARTITION BY d.Part_FK, d.Revision, d.EntityTypeId ORDER BY d.Part_PsaExtraInfo_ID DESC) rn
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part_PsaExtraInfo] d
    WHERE d.EntityTypeId IN (3050, 3051)
) s
WHERE s.rn = 1 AND NOT EXISTS (SELECT 1 FROM #PartMap pm WHERE pm.HtsId = s.Part_FK)
UNION ALL
SELECT N'dryerSuffixEmpty', N'3', COUNT(*)
FROM Inv.PartExtraInfo
WHERE InfoType = 3 AND NULLIF(LTRIM(RTRIM(ModelSuffix)), N'') IS NULL
UNION ALL
SELECT N'oilFreeLiter', N'2', COUNT(*)
FROM Inv.PartExtraInfo
WHERE InfoType = 2 AND OilCapacityLiter IS NOT NULL
UNION ALL
SELECT N'dryerFlatKey', N'3', COUNT(*)
FROM Inv.PartExtraInfo
WHERE InfoType = 3
  AND (CHARINDEX(N'SilencerQty', DryerExtraInfo) > 0 OR CHARINDEX(N'Part_DryerExtraInfo_ID', DryerExtraInfo) > 0)
UNION ALL
SELECT N'psaFlatKey', N'4', COUNT(*)
FROM Inv.PartExtraInfo
WHERE InfoType IN (4, 5)
  AND (CHARINDEX(N'Part_PsaExtraInfo_ID', DryerExtraInfo) > 0 OR CHARINDEX(N'"PressureSafetyValveQty"', DryerExtraInfo) > 0)
UNION ALL
SELECT N'dryerSilencerMismatch', N'3', COUNT(*)
FROM Inv.PartExtraInfo n
INNER JOIN #Dryer s ON s.ProductId = n.ProductId AND s.Revision = n.Revision
WHERE n.InfoType = 3
  AND (
      ISNULL(s.SilencerQty, -1) <> ISNULL(TRY_CONVERT(decimal(18, 4), JSON_VALUE(n.DryerExtraInfo, '$.Silencer.Qty')), -1)
      OR ISNULL(LTRIM(RTRIM(s.SilencerBrand)), N'') <> ISNULL(JSON_VALUE(n.DryerExtraInfo, '$.Silencer.Brand'), N'')
  )
UNION ALL
SELECT N'dryerControlMismatch', N'3', COUNT(*)
FROM Inv.PartExtraInfo n
INNER JOIN #Dryer s ON s.ProductId = n.ProductId AND s.Revision = n.Revision
WHERE n.InfoType = 3
  AND (
      ISNULL(s.ControlBoardTypeQty, -1) <> ISNULL(TRY_CONVERT(decimal(18, 4), JSON_VALUE(n.DryerExtraInfo, '$.Control_Board_Type.Qty')), -1)
      OR ISNULL(LTRIM(RTRIM(s.ControlBoardTypeBrand)), N'') <> ISNULL(JSON_VALUE(n.DryerExtraInfo, '$.Control_Board_Type.Brand'), N'')
  )
UNION ALL
SELECT N'dryerDesiccantMismatch', N'3', COUNT(*)
FROM Inv.PartExtraInfo n
INNER JOIN #Dryer s ON s.ProductId = n.ProductId AND s.Revision = n.Revision
WHERE n.InfoType = 3
  AND (
      ISNULL(s.DesiccantType1Qty, -1) <> ISNULL(TRY_CONVERT(decimal(18, 4), JSON_VALUE(n.DryerExtraInfo, '$.Desiccant_Type_1.Qty')), -1)
      OR ISNULL(LTRIM(RTRIM(s.DesiccantType1Brand)), N'') <> ISNULL(JSON_VALUE(n.DryerExtraInfo, '$.Desiccant_Type_1.Brand'), N'')
  )
UNION ALL
SELECT N'dryerGaugeMismatch', N'3', COUNT(*)
FROM Inv.PartExtraInfo n
INNER JOIN #Dryer s ON s.ProductId = n.ProductId AND s.Revision = n.Revision
WHERE n.InfoType = 3
  AND (
      ISNULL(s.PressureGaugeQty, -1) <> ISNULL(TRY_CONVERT(decimal(18, 4), JSON_VALUE(n.DryerExtraInfo, '$.Pressure_Gauge.Qty')), -1)
      OR ISNULL(LTRIM(RTRIM(s.PressureGaugeBrand)), N'') <> ISNULL(JSON_VALUE(n.DryerExtraInfo, '$.Pressure_Gauge.Brand'), N'')
  )
UNION ALL
SELECT N'dryerBall12Mismatch', N'3', COUNT(*)
FROM Inv.PartExtraInfo n
INNER JOIN #Dryer s ON s.ProductId = n.ProductId AND s.Revision = n.Revision
WHERE n.InfoType = 3
  AND (
      ISNULL(s.BallValveQty1_2, -1) <> ISNULL(TRY_CONVERT(decimal(18, 4), JSON_VALUE(n.DryerExtraInfo, '$.Ball_Valve_12_inch.Qty')), -1)
      OR ISNULL(LTRIM(RTRIM(s.BallValveBrand1_2)), N'') <> ISNULL(JSON_VALUE(n.DryerExtraInfo, '$.Ball_Valve_12_inch.Brand'), N'')
  )
UNION ALL
SELECT N'psaGaugeMismatch', N'4', COUNT(*)
FROM Inv.PartExtraInfo n
INNER JOIN #Psa s ON s.ProductId = n.ProductId AND s.Revision = n.Revision
    AND n.InfoType = CASE WHEN s.EntityTypeId = 3051 THEN 5 ELSE 4 END
WHERE (
    ISNULL(s.PressureIndicatorQty, -1) <> ISNULL(TRY_CONVERT(decimal(18, 4), JSON_VALUE(n.DryerExtraInfo, '$.Pressure_Gauge.Qty')), -1)
    OR ISNULL(LTRIM(RTRIM(s.PressureIndicatorBrand)), N'') <> ISNULL(JSON_VALUE(n.DryerExtraInfo, '$.Pressure_Gauge.Brand'), N'')
)
UNION ALL
SELECT N'psaVip12Mismatch', N'4', COUNT(*)
FROM Inv.PartExtraInfo n
INNER JOIN #Psa s ON s.ProductId = n.ProductId AND s.Revision = n.Revision
    AND n.InfoType = CASE WHEN s.EntityTypeId = 3051 THEN 5 ELSE 4 END
WHERE (
    ISNULL(s.VipValve1_2Qty, -1) <> ISNULL(TRY_CONVERT(decimal(18, 4), JSON_VALUE(n.DryerExtraInfo, '$.VIP_Valve_12.Qty')), -1)
    OR ISNULL(LTRIM(RTRIM(s.VipValve1_2Brand)), N'') <> ISNULL(JSON_VALUE(n.DryerExtraInfo, '$.VIP_Valve_12.Brand'), N'')
);
