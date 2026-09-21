/*
  Fix_SaleMission_PersonelId.sql
  کارشناس ماموریت باید به Hcm.Personel وصل شود (مثل حق ماموریت پرسنل).
*/
SET NOCOUNT ON;

IF COL_LENGTH('Sale.Mission', 'PersonelId') IS NULL
    ALTER TABLE Sale.Mission ADD PersonelId bigint NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sale_Mission_PersonelId' AND object_id = OBJECT_ID(N'Sale.Mission'))
    CREATE INDEX IX_Sale_Mission_PersonelId ON Sale.Mission(PersonelId);

UPDATE m
SET m.PersonelId = p.Id
FROM Sale.Mission m
INNER JOIN system.[User] u ON u.Id = m.ExpertId
INNER JOIN Hcm.Personel p ON p.PartyId = u.PartyId
WHERE (m.PersonelId IS NULL OR m.PersonelId = 0) AND u.PartyId IS NOT NULL;

UPDATE m
SET m.PersonelId = ms.PersonelId
FROM Sale.Mission m
INNER JOIN Sale.MissionSalary ms ON ms.ExpertId = m.ExpertId
WHERE (m.PersonelId IS NULL OR m.PersonelId = 0) AND ms.PersonelId IS NOT NULL AND ms.PersonelId <> 0;

UPDATE m
SET m.PersonelId = ms.PersonelId
FROM Sale.Mission m
INNER JOIN Sale.MissionSalary ms ON ms.HtsPersonelId = m.HtsPersonelId AND m.HtsPersonelId <> 0
WHERE (m.PersonelId IS NULL OR m.PersonelId = 0) AND ms.PersonelId IS NOT NULL AND ms.PersonelId <> 0;

UPDATE m
SET m.PersonelId = p.Id
FROM Sale.Mission m
INNER JOIN system.[User] u ON u.Id = m.ExpertId
INNER JOIN Hcm.Personel p ON p.HamkaranId IS NOT NULL AND u.HamkaranId IS NOT NULL AND p.HamkaranId = u.HamkaranId
WHERE (m.PersonelId IS NULL OR m.PersonelId = 0);

SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN PersonelId IS NOT NULL AND PersonelId <> 0 THEN 1 ELSE 0 END) AS PersonelSet,
    SUM(CASE WHEN PersonelId IS NULL OR PersonelId = 0 THEN 1 ELSE 0 END) AS PersonelMissing
FROM Sale.Mission;
