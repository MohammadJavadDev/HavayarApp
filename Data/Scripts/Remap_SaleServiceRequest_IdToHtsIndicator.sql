/*
  Remap_SaleServiceRequest_IdToHtsIndicator.sql

  In HTS and Havayar UI, شماره اندیکاتور for ServiceRequest is the primary key:
    HTS:    dbo.Sale_ServiceRequest.ServiceRequest_ID
    Havayar: Sale.ServiceRequest.Id  (displayed as شماره اندیکاتور)

  Phase5 MERGE inserted without IDENTITY_INSERT, so most rows have Id <> HtsId.
  This script remaps Id -> HtsId for synced rows and updates all ServiceRequestId FKs.

  Apply:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Remap_SaleServiceRequest_IdToHtsIndicator.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Offset BIGINT = 1000000000;
DECLARE @BeforeMismatch INT;
DECLARE @AfterMismatch INT;
DECLARE @NextLocal BIGINT;

SELECT @BeforeMismatch = COUNT(*)
FROM Sale.ServiceRequest
WHERE HtsId <> 0 AND Id <> HtsId;

PRINT N'Before: Id<>HtsId count = ' + CAST(@BeforeMismatch AS NVARCHAR(20));

IF @BeforeMismatch = 0
   AND NOT EXISTS (
        SELECT 1
        FROM Sale.ServiceRequest l
        WHERE l.HtsId = 0
          AND EXISTS (
                SELECT 1
                FROM Sale.ServiceRequest b
                WHERE b.HtsId = l.Id AND b.HtsId <> 0
              )
      )
BEGIN
    PRINT N'Nothing to remap.';
    RETURN;
END;

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#SR') IS NOT NULL DROP TABLE #SR;
IF OBJECT_ID('tempdb..#Map') IS NOT NULL DROP TABLE #Map;

SELECT *
INTO #SR
FROM Sale.ServiceRequest;

SELECT
    s.Id AS OldId,
    CASE WHEN s.HtsId <> 0 THEN s.HtsId ELSE s.Id END AS NewId
INTO #Map
FROM #SR s;

SELECT @NextLocal =
    ISNULL(MAX(v), 0) + 1
FROM (
    SELECT MAX(NewId) AS v FROM #Map
    UNION ALL
    SELECT MAX(OldId) FROM #Map
) x;

;WITH LocalConflicts AS (
    SELECT
        m.OldId,
        ROW_NUMBER() OVER (ORDER BY m.OldId) AS rn
    FROM #Map m
    INNER JOIN #SR s ON s.Id = m.OldId
    WHERE s.HtsId = 0
      AND EXISTS (
            SELECT 1
            FROM #Map o
            WHERE o.OldId <> m.OldId
              AND o.NewId = m.NewId
          )
)
UPDATE m
SET NewId = @NextLocal - 1 + c.rn
FROM #Map m
INNER JOIN LocalConflicts c ON c.OldId = m.OldId;

IF EXISTS (
    SELECT NewId
    FROM #Map
    GROUP BY NewId
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR(N'#Map NewId is not unique after local conflict resolution.', 16, 1);
END;

PRINT N'Map rows: ' + CAST((SELECT COUNT(*) FROM #Map) AS NVARCHAR(20));
PRINT N'Rows changing Id: ' + CAST((SELECT COUNT(*) FROM #Map WHERE OldId <> NewId) AS NVARCHAR(20));

ALTER TABLE Rpr.RepairRequest NOCHECK CONSTRAINT FK_RepairRequest_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.AgencyCartable NOCHECK CONSTRAINT FK_AgencyCartable_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.Mission NOCHECK CONSTRAINT FK_Mission_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.ServiceRequestAttachment NOCHECK CONSTRAINT FK_ServiceRequestAttachment_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.ServiceRequestDetail NOCHECK CONSTRAINT FK_ServiceRequestDetail_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.ServiceRequestExpertMission NOCHECK CONSTRAINT FK_ServiceRequestExpertMission_ServiceRequest_ServiceRequestId;

UPDATE Sale.ServiceRequestDetail
SET ServiceRequestId = ServiceRequestId + @Offset
WHERE ServiceRequestId IS NOT NULL;

UPDATE Sale.ServiceRequestExpertMission
SET ServiceRequestId = ServiceRequestId + @Offset
WHERE ServiceRequestId IS NOT NULL;

UPDATE Sale.ServiceRequestAttachment
SET ServiceRequestId = ServiceRequestId + @Offset
WHERE ServiceRequestId IS NOT NULL;

UPDATE Sale.Mission
SET ServiceRequestId = ServiceRequestId + @Offset
WHERE ServiceRequestId IS NOT NULL;

UPDATE Sale.AgencyCartable
SET ServiceRequestId = ServiceRequestId + @Offset
WHERE ServiceRequestId IS NOT NULL;

UPDATE Rpr.RepairRequest
SET ServiceRequestId = ServiceRequestId + @Offset
WHERE ServiceRequestId IS NOT NULL;

DELETE FROM Sale.ServiceRequest;

SET IDENTITY_INSERT Sale.ServiceRequest ON;

INSERT INTO Sale.ServiceRequest
(
    Id,
    HtsId,
    CustomerId,
    CustomerPhoneFromHistory,
    ContactMiladiDate,
    ContactShamsiDate,
    CustomerAddressId,
    ResponsibleZoneId,
    DispatchZoneId,
    ExpertMission,
    AllProductName,
    Unreal,
    DispatchMiladiDate,
    DispatchShamsiDate,
    CustomerAgentName,
    CustomerAgentJobPosition,
    CustomerAgentTell,
    CustomerAgentEmailAddress,
    CreatedOrgUnitId,
    Comment,
    IsWaitingToSend,
    LegacyRepairRequestIds,
    CreatedById,
    ModifiedById,
    CreatedByName,
    ModifiedByName,
    ModifiedDateMiladiDateTime,
    ModifiedDateShamsiDateTime,
    CreatedOnMiladiDateTime,
    CreatedOnShamsiDateTime,
    IsActive,
    ResponsiblePersonelId
)
SELECT
    m.NewId,
    s.HtsId,
    s.CustomerId,
    s.CustomerPhoneFromHistory,
    s.ContactMiladiDate,
    s.ContactShamsiDate,
    s.CustomerAddressId,
    s.ResponsibleZoneId,
    s.DispatchZoneId,
    s.ExpertMission,
    s.AllProductName,
    s.Unreal,
    s.DispatchMiladiDate,
    s.DispatchShamsiDate,
    s.CustomerAgentName,
    s.CustomerAgentJobPosition,
    s.CustomerAgentTell,
    s.CustomerAgentEmailAddress,
    s.CreatedOrgUnitId,
    s.Comment,
    s.IsWaitingToSend,
    s.LegacyRepairRequestIds,
    s.CreatedById,
    s.ModifiedById,
    s.CreatedByName,
    s.ModifiedByName,
    s.ModifiedDateMiladiDateTime,
    s.ModifiedDateShamsiDateTime,
    s.CreatedOnMiladiDateTime,
    s.CreatedOnShamsiDateTime,
    s.IsActive,
    s.ResponsiblePersonelId
FROM #SR s
INNER JOIN #Map m ON m.OldId = s.Id;

SET IDENTITY_INSERT Sale.ServiceRequest OFF;

UPDATE d
SET ServiceRequestId = m.NewId
FROM Sale.ServiceRequestDetail d
INNER JOIN #Map m ON d.ServiceRequestId = m.OldId + @Offset;

UPDATE d
SET ServiceRequestId = m.NewId
FROM Sale.ServiceRequestExpertMission d
INNER JOIN #Map m ON d.ServiceRequestId = m.OldId + @Offset;

UPDATE d
SET ServiceRequestId = m.NewId
FROM Sale.ServiceRequestAttachment d
INNER JOIN #Map m ON d.ServiceRequestId = m.OldId + @Offset;

UPDATE d
SET ServiceRequestId = m.NewId
FROM Sale.Mission d
INNER JOIN #Map m ON d.ServiceRequestId = m.OldId + @Offset;

UPDATE d
SET ServiceRequestId = m.NewId
FROM Sale.AgencyCartable d
INNER JOIN #Map m ON d.ServiceRequestId = m.OldId + @Offset;

UPDATE d
SET ServiceRequestId = m.NewId
FROM Rpr.RepairRequest d
INNER JOIN #Map m ON d.ServiceRequestId = m.OldId + @Offset;

ALTER TABLE Rpr.RepairRequest WITH CHECK CHECK CONSTRAINT FK_RepairRequest_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.AgencyCartable WITH CHECK CHECK CONSTRAINT FK_AgencyCartable_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.Mission WITH CHECK CHECK CONSTRAINT FK_Mission_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.ServiceRequestAttachment WITH CHECK CHECK CONSTRAINT FK_ServiceRequestAttachment_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.ServiceRequestDetail WITH CHECK CHECK CONSTRAINT FK_ServiceRequestDetail_ServiceRequest_ServiceRequestId;
ALTER TABLE Sale.ServiceRequestExpertMission WITH CHECK CHECK CONSTRAINT FK_ServiceRequestExpertMission_ServiceRequest_ServiceRequestId;

DECLARE @MaxId BIGINT = (SELECT ISNULL(MAX(Id), 0) FROM Sale.ServiceRequest);
DBCC CHECKIDENT ('Sale.ServiceRequest', RESEED, @MaxId) WITH NO_INFOMSGS;

SELECT @AfterMismatch = COUNT(*)
FROM Sale.ServiceRequest
WHERE HtsId <> 0 AND Id <> HtsId;

IF @AfterMismatch <> 0
BEGIN
    RAISERROR(N'Remap failed: remaining Id<>HtsId rows.', 16, 1);
END;

IF EXISTS (
    SELECT 1 FROM Sale.ServiceRequestDetail d
    WHERE d.ServiceRequestId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Sale.ServiceRequest sr WHERE sr.Id = d.ServiceRequestId)
)
    RAISERROR(N'Orphan ServiceRequestDetail.ServiceRequestId after remap.', 16, 1);

IF EXISTS (
    SELECT 1 FROM Sale.Mission d
    WHERE d.ServiceRequestId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Sale.ServiceRequest sr WHERE sr.Id = d.ServiceRequestId)
)
    RAISERROR(N'Orphan Mission.ServiceRequestId after remap.', 16, 1);

IF EXISTS (
    SELECT 1 FROM Sale.ServiceRequestExpertMission d
    WHERE d.ServiceRequestId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Sale.ServiceRequest sr WHERE sr.Id = d.ServiceRequestId)
)
    RAISERROR(N'Orphan ExpertMission.ServiceRequestId after remap.', 16, 1);

IF EXISTS (
    SELECT 1 FROM Sale.ServiceRequestAttachment d
    WHERE d.ServiceRequestId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Sale.ServiceRequest sr WHERE sr.Id = d.ServiceRequestId)
)
    RAISERROR(N'Orphan Attachment.ServiceRequestId after remap.', 16, 1);

IF EXISTS (
    SELECT 1 FROM Sale.AgencyCartable d
    WHERE d.ServiceRequestId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Sale.ServiceRequest sr WHERE sr.Id = d.ServiceRequestId)
)
    RAISERROR(N'Orphan AgencyCartable.ServiceRequestId after remap.', 16, 1);

IF EXISTS (
    SELECT 1 FROM Rpr.RepairRequest d
    WHERE d.ServiceRequestId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Sale.ServiceRequest sr WHERE sr.Id = d.ServiceRequestId)
)
    RAISERROR(N'Orphan RepairRequest.ServiceRequestId after remap.', 16, 1);

COMMIT TRANSACTION;

PRINT N'Remap committed. After Id<>HtsId = ' + CAST(@AfterMismatch AS NVARCHAR(20));
PRINT N'Identity reseeded to ' + CAST(@MaxId AS NVARCHAR(20));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    BEGIN TRY SET IDENTITY_INSERT Sale.ServiceRequest OFF; END TRY BEGIN CATCH END CATCH;
    THROW;
END CATCH;
