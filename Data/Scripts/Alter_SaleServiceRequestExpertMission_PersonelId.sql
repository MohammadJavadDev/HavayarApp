/*
  PersonelId on Sale.ServiceRequestExpertMission (HTS Personel_FK parity, like MissionSalary).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH('Sale.ServiceRequestExpertMission', 'PersonelId') IS NULL
BEGIN
    ALTER TABLE Sale.ServiceRequestExpertMission ADD PersonelId bigint NULL;
    PRINT N'Added Sale.ServiceRequestExpertMission.PersonelId';
END
ELSE
    PRINT N'Sale.ServiceRequestExpertMission.PersonelId already exists';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Sale_ServiceRequestExpertMission_Personel'
)
AND COL_LENGTH('Sale.ServiceRequestExpertMission', 'PersonelId') IS NOT NULL
BEGIN
    ALTER TABLE Sale.ServiceRequestExpertMission
    ADD CONSTRAINT FK_Sale_ServiceRequestExpertMission_Personel
        FOREIGN KEY (PersonelId) REFERENCES Hcm.Personel (Id);
    PRINT N'Added FK_Sale_ServiceRequestExpertMission_Personel';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Sale_ServiceRequestExpertMission_PersonelId'
      AND object_id = OBJECT_ID(N'Sale.ServiceRequestExpertMission')
)
BEGIN
    CREATE INDEX IX_Sale_ServiceRequestExpertMission_PersonelId
        ON Sale.ServiceRequestExpertMission (PersonelId);
    PRINT N'Added IX_Sale_ServiceRequestExpertMission_PersonelId';
END

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);

UPDATE em
SET em.PersonelId = hp.Id,
    em.ModifiedById = 1,
    em.ModifiedByName = N'alter-srem-personel',
    em.ModifiedDateMiladiDateTime = @Now,
    em.ModifiedDateShamsiDateTime = @NowShamsi
FROM Sale.ServiceRequestExpertMission em
INNER JOIN system.[User] u ON u.Id = em.ExpertId
INNER JOIN Hcm.Personel hp ON hp.PartyId = u.PartyId
WHERE (em.PersonelId IS NULL OR em.PersonelId = 0)
  AND u.PartyId IS NOT NULL;
PRINT N'  PersonelId from User.Party: ' + CAST(@@ROWCOUNT AS nvarchar(20));

IF OBJECT_ID(N'[TMS].[TotalSystem].[dbo].[Sale_ServiceRequest_ExpertMission]') IS NOT NULL
AND OBJECT_ID(N'[TMS].[TotalSystem].[dbo].[HRM_Personel]') IS NOT NULL
BEGIN
    UPDATE em
    SET em.PersonelId = hp.Id,
        em.ModifiedById = 1,
        em.ModifiedByName = N'alter-srem-personel',
        em.ModifiedDateMiladiDateTime = @Now,
        em.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.ServiceRequestExpertMission em
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest_ExpertMission] h
        ON h.ServiceRequest_ExpertMission_ID = em.HtsId
    INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] p ON p.Personel_ID = h.Personel_FK
    INNER JOIN Hcm.Personel hp ON TRY_CONVERT(int, hp.Code) = p.Hamkaran_Personel_FK
    WHERE em.HtsId <> 0 AND (em.PersonelId IS NULL OR em.PersonelId = 0);
    PRINT N'  PersonelId from HTS Personel_FK: ' + CAST(@@ROWCOUNT AS nvarchar(20));
END

SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN PersonelId IS NOT NULL AND PersonelId <> 0 THEN 1 ELSE 0 END) AS PersonelSet,
    SUM(CASE WHEN PersonelId IS NULL OR PersonelId = 0 THEN 1 ELSE 0 END) AS PersonelMissing
FROM Sale.ServiceRequestExpertMission;
