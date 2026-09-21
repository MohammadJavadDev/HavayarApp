/*
  SyncAfterSales_ServiceRequestPartWiden.sql
  Widen Sale.ServiceRequestPart to HTS ProductRequest fields and backfill from TMS.
  Encoding: UTF-8 with BOM. Apply:
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\SyncAfterSales_ServiceRequestPartWiden.sql"
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-servicerequestpart-widen';

IF COL_LENGTH('Sale.ServiceRequestPart', 'ProjectVchShamsiDate') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD ProjectVchShamsiDate NVARCHAR(10) NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'ProjectVchMiladiDate') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD ProjectVchMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'PartUnitId') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD PartUnitId BIGINT NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'CostCenterId') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD CostCenterId BIGINT NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'OtherCostCenterPartyId') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD OtherCostCenterPartyId BIGINT NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'ReturnLicence') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD ReturnLicence BIT NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'ReturnDamagedPart') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD ReturnDamagedPart BIT NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'DamagedPartType') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD DamagedPartType INT NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'DamagedPartIds') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD DamagedPartIds NVARCHAR(2048) NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'DamagedParts') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD DamagedParts NVARCHAR(MAX) NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'DamagedPartDescription') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD DamagedPartDescription NVARCHAR(1024) NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'HasFailure') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD HasFailure BIT NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'FailurePartIds') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD FailurePartIds NVARCHAR(2048) NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'FailureParts') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD FailureParts NVARCHAR(MAX) NULL;
IF COL_LENGTH('Sale.ServiceRequestPart', 'FailureDescription') IS NULL
    ALTER TABLE Sale.ServiceRequestPart ADD FailureDescription NVARCHAR(2048) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    PRINT N'Linked Server [TMS] یافت نشد — فقط ALTER انجام شد.';
    RETURN;
END;

IF OBJECT_ID('tempdb..#TmsSrPartWiden') IS NOT NULL DROP TABLE #TmsSrPartWiden;
SELECT * INTO #TmsSrPartWiden
FROM OPENQUERY([TMS], N'
    SELECT
        CAST(ServiceRequest_Part_ID AS BIGINT) AS HtsId,
        Project_VchDate AS ProjectVchShamsiDate,
        Project_VchDateLatin AS ProjectVchMiladiDate,
        CAST(Project_VchPartUnit_FK AS BIGINT) AS PartUnitHtsId,
        CAST(CostCenter_FK AS BIGINT) AS CostCenterHtsId,
        CAST(OtherCostCenter_FK AS BIGINT) AS OtherCostCenterHtsId,
        ReturnLicence,
        Return_DamagedPart AS ReturnDamagedPart,
        CAST(DamagedPartTypeId AS INT) AS DamagedPartType,
        LEFT(DamagedPartIds, 2048) AS DamagedPartIds,
        DamagedParts,
        LEFT(DamagedPartDescription, 1024) AS DamagedPartDescription,
        HasFailure,
        LEFT(FailurePartIds, 2048) AS FailurePartIds,
        FailureParts,
        LEFT(FailureDescription, 2048) AS FailureDescription
    FROM dbo.Sale_ServiceRequest_Part
');

UPDATE t
SET
    t.ProjectVchShamsiDate = s.ProjectVchShamsiDate,
    t.ProjectVchMiladiDate = s.ProjectVchMiladiDate,
    t.PartUnitId = pu.Id,
    t.CostCenterId = cc.Id,
    t.OtherCostCenterPartyId = py.Id,
    t.ReturnLicence = s.ReturnLicence,
    t.ReturnDamagedPart = s.ReturnDamagedPart,
    t.DamagedPartType = s.DamagedPartType,
    t.DamagedPartIds = s.DamagedPartIds,
    t.DamagedParts = s.DamagedParts,
    t.DamagedPartDescription = s.DamagedPartDescription,
    t.HasFailure = s.HasFailure,
    t.FailurePartIds = s.FailurePartIds,
    t.FailureParts = s.FailureParts,
    t.FailureDescription = s.FailureDescription,
    t.ModifiedById = 1,
    t.ModifiedByName = @SeedUser,
    t.ModifiedDateMiladiDateTime = @Now,
    t.ModifiedDateShamsiDateTime = @NowShamsi
FROM Sale.ServiceRequestPart t
INNER JOIN #TmsSrPartWiden s ON s.HtsId = t.HtsId AND t.HtsId <> 0
LEFT JOIN Inv.PartUnit pu ON pu.HamkaranId = s.PartUnitHtsId
LEFT JOIN Gnr.CostCenter cc ON cc.HamkaranId = s.CostCenterHtsId OR cc.Number = s.CostCenterHtsId
OUTER APPLY (
    SELECT TOP (1) p.Id
    FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
    INNER JOIN Gnr.Party p ON p.HamkaranId = mc.Hamkaran_ManCompany_FK AND ISNULL(mc.Hamkaran_ManCompany_FK, 0) <> 0
    WHERE mc.ManCompany_ID = s.OtherCostCenterHtsId
) py;

PRINT N'ServiceRequestPart widen updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
