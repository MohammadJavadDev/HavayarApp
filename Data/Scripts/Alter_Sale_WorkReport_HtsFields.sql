/*
  Alter_Sale_WorkReport_HtsFields.sql
  ستون‌های فنی گزارش کار + جداول نوع خرابی/کد عملیاتی + AttachmentFileIds اعلان.
  Encoding: UTF-8 with BOM. Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'alter-workreport-hts';

    IF COL_LENGTH(N'Sale.WorkReport', N'TotalRunTime') IS NULL
        ALTER TABLE Sale.WorkReport ADD TotalRunTime BIGINT NOT NULL CONSTRAINT DF_Sale_WorkReport_TotalRunTime DEFAULT (0);
    IF COL_LENGTH(N'Sale.WorkReport', N'WorkHoursUnderload') IS NULL
        ALTER TABLE Sale.WorkReport ADD WorkHoursUnderload BIGINT NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'FlowIntensityUnderload') IS NULL
        ALTER TABLE Sale.WorkReport ADD FlowIntensityUnderload DECIMAL(18,4) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'FlowIntensityWithoutLoad') IS NULL
        ALTER TABLE Sale.WorkReport ADD FlowIntensityWithoutLoad DECIMAL(18,4) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'MaximumWorkingPressure') IS NULL
        ALTER TABLE Sale.WorkReport ADD MaximumWorkingPressure DECIMAL(18,4) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'MinimumWorkingPressure') IS NULL
        ALTER TABLE Sale.WorkReport ADD MinimumWorkingPressure DECIMAL(18,4) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'EnvironmentTemperature') IS NULL
        ALTER TABLE Sale.WorkReport ADD EnvironmentTemperature DECIMAL(18,4) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'DeviceTemperature') IS NULL
        ALTER TABLE Sale.WorkReport ADD DeviceTemperature DECIMAL(18,4) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'VoltagePowerGrid') IS NULL
        ALTER TABLE Sale.WorkReport ADD VoltagePowerGrid SMALLINT NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'AmountDryerDewpoint') IS NULL
        ALTER TABLE Sale.WorkReport ADD AmountDryerDewpoint DECIMAL(18,4) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'OldRunTime') IS NULL
        ALTER TABLE Sale.WorkReport ADD OldRunTime INT NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'ExpertGuaranteeMiladiDate') IS NULL
        ALTER TABLE Sale.WorkReport ADD ExpertGuaranteeMiladiDate DATETIME2 NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'ExpertGuaranteeShamsiDate') IS NULL
        ALTER TABLE Sale.WorkReport ADD ExpertGuaranteeShamsiDate NVARCHAR(10) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'ExpertGuaranteeComment') IS NULL
        ALTER TABLE Sale.WorkReport ADD ExpertGuaranteeComment NVARCHAR(4000) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'FailureTypeIds') IS NULL
        ALTER TABLE Sale.WorkReport ADD FailureTypeIds NVARCHAR(512) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'FailureTypeInText') IS NULL
        ALTER TABLE Sale.WorkReport ADD FailureTypeInText NVARCHAR(4000) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'RepairsTypeIds') IS NULL
        ALTER TABLE Sale.WorkReport ADD RepairsTypeIds NVARCHAR(512) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'RepairsTypeInText') IS NULL
        ALTER TABLE Sale.WorkReport ADD RepairsTypeInText NVARCHAR(4000) NULL;
    IF COL_LENGTH(N'Sale.WorkReport', N'IsSurveyLinkSended') IS NULL
        ALTER TABLE Sale.WorkReport ADD IsSurveyLinkSended BIT NULL;

    IF COL_LENGTH(N'system.Notification', N'AttachmentFileIds') IS NULL
        ALTER TABLE system.Notification ADD AttachmentFileIds NVARCHAR(MAX) NULL;

    IF OBJECT_ID(N'Sale.AfterSalesFailureType', N'U') IS NULL
    BEGIN
        CREATE TABLE Sale.AfterSalesFailureType (
            Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            HtsId BIGINT NOT NULL CONSTRAINT DF_Sale_AfterSalesFailureType_HtsId DEFAULT (0),
            Title NVARCHAR(256) NOT NULL,
            Code NVARCHAR(64) NULL,
            CreatedById BIGINT NULL,
            ModifiedById BIGINT NULL,
            CreatedByName NVARCHAR(150) NULL,
            ModifiedByName NVARCHAR(150) NULL,
            ModifiedDateMiladiDateTime DATETIME2 NULL,
            ModifiedDateShamsiDateTime NVARCHAR(30) NULL,
            CreatedOnMiladiDateTime DATETIME2 NULL,
            CreatedOnShamsiDateTime NVARCHAR(30) NULL,
            IsActive INT NULL
        );
        CREATE UNIQUE INDEX IX_Sale_AfterSalesFailureType_HtsId ON Sale.AfterSalesFailureType(HtsId) WHERE HtsId <> CAST(0 AS bigint);
    END

    IF OBJECT_ID(N'Sale.AfterSalesRepairsType', N'U') IS NULL
    BEGIN
        CREATE TABLE Sale.AfterSalesRepairsType (
            Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            HtsId BIGINT NOT NULL CONSTRAINT DF_Sale_AfterSalesRepairsType_HtsId DEFAULT (0),
            Title NVARCHAR(256) NOT NULL,
            Code NVARCHAR(64) NULL,
            CreatedById BIGINT NULL,
            ModifiedById BIGINT NULL,
            CreatedByName NVARCHAR(150) NULL,
            ModifiedByName NVARCHAR(150) NULL,
            ModifiedDateMiladiDateTime DATETIME2 NULL,
            ModifiedDateShamsiDateTime NVARCHAR(30) NULL,
            CreatedOnMiladiDateTime DATETIME2 NULL,
            CreatedOnShamsiDateTime NVARCHAR(30) NULL,
            IsActive INT NULL
        );
        CREATE UNIQUE INDEX IX_Sale_AfterSalesRepairsType_HtsId ON Sale.AfterSalesRepairsType(HtsId) WHERE HtsId <> CAST(0 AS bigint);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sale_WorkReportAttachment_HtsId' AND object_id = OBJECT_ID(N'Sale.WorkReportAttachment'))
        CREATE UNIQUE INDEX IX_Sale_WorkReportAttachment_HtsId ON Sale.WorkReportAttachment(HtsId) WHERE HtsId <> CAST(0 AS bigint);

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN
        IF OBJECT_ID('tempdb..#TmsFailure') IS NOT NULL DROP TABLE #TmsFailure;
        SELECT * INTO #TmsFailure
        FROM OPENQUERY([TMS], N'
            SELECT CAST(Id AS BIGINT) AS HtsId, LEFT(Title, 256) AS Title, LEFT(Code, 64) AS Code
            FROM dbo.Sale_FailureType
        ');

        SET IDENTITY_INSERT Sale.AfterSalesFailureType ON;
        MERGE Sale.AfterSalesFailureType AS t
        USING #TmsFailure AS s ON t.HtsId = s.HtsId
        WHEN MATCHED THEN UPDATE SET t.Title = s.Title, t.Code = s.Code,
            t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
            t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi, t.IsActive = 1
        WHEN NOT MATCHED THEN INSERT
            (Id, HtsId, Title, Code, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
             ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (s.HtsId, s.HtsId, s.Title, s.Code, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
        SET IDENTITY_INSERT Sale.AfterSalesFailureType OFF;
        PRINT N'AfterSalesFailureType MERGE touched';

        IF OBJECT_ID('tempdb..#TmsRepair') IS NOT NULL DROP TABLE #TmsRepair;
        SELECT * INTO #TmsRepair
        FROM OPENQUERY([TMS], N'
            SELECT CAST(Id AS BIGINT) AS HtsId, LEFT(Title, 256) AS Title, LEFT(Code, 64) AS Code
            FROM dbo.Sale_RepairsType
        ');

        SET IDENTITY_INSERT Sale.AfterSalesRepairsType ON;
        MERGE Sale.AfterSalesRepairsType AS t
        USING #TmsRepair AS s ON t.HtsId = s.HtsId
        WHEN MATCHED THEN UPDATE SET t.Title = s.Title, t.Code = s.Code,
            t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
            t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi, t.IsActive = 1
        WHEN NOT MATCHED THEN INSERT
            (Id, HtsId, Title, Code, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
             ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (s.HtsId, s.HtsId, s.Title, s.Code, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
        SET IDENTITY_INSERT Sale.AfterSalesRepairsType OFF;
        PRINT N'AfterSalesRepairsType MERGE touched';
    END
    ELSE
        PRINT N'WARN: Linked Server [TMS] not found — lookups not synced';

    COMMIT TRANSACTION;
    PRINT N'=== DONE Alter_Sale_WorkReport_HtsFields ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
