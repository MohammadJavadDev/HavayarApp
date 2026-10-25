/*
Fix_EdmsDocument_HourUnitAndHtsLink.sql
One-time data fix for Edms.Document (no job is run). Run order: preview (DryRun=1), review, apply (DryRun=0).

Background
  Edms.Document.HourPrePerson is MINUTES (HTS Edms_Document.ConsumedManHours; the MDR report and the
  performance reports read it as minutes). After the cut-over Portal users typed HOURS in that field.
  The edit forms now take HH:MM (data-duration-hm) and save minutes.

Step 1  Link the 15 HTS documents that exist in Portal as unlinked copies (HtsId = 0)
        (same VPIS = project + code, same revision, same day). Explicit pair list, every pair is verified first.
        - Edms.ProjectVpis.HtsId is set for the VPIS that carry them (only while it is still 0).
        - Document.HtsId, HourPrePerson (= HTS minutes) and PreparedUserId (= HTS PreparedUser_FK mapped to a
          Portal user) are set from HTS. PublicationMiladiDate / PublicationShamsiDate only when NULL.
        - Status, Reviewer, Approver, comments, files and Modified* are NOT touched (Portal workflow state wins).
        - Pairs where HTS minutes <> Portal hours * 60 are flagged MISMATCH in the log (the HTS value is used).
Step 2  Documents already linked to HTS whose hours were edited in Portal (Portal <> HTS): hours * 60.
Step 3  Portal-only documents (HtsId = 0) created on/after 1405/07/01 (2026-09-23): hours * 60.
        Older Portal-only rows are left alone (id 40949 = 360 is already minutes).

Every changed row is written to dbo.Fix_EdmsDocument_HourUnit_Log (old and new values). A document that is already
in the log for a step is never changed again, so a second run does nothing. Modified* columns are not touched.

sqlcmd variable DryRun: 1 = show what would change and ROLLBACK, 0 = apply.
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -v DryRun=1 -i Data\Scripts\Fix_EdmsDocument_HourUnitAndHtsLink.sql
UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = $(DryRun);
DECLARE @Cutover DATETIME2 = '2026-09-23';   -- 1405/07/01

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] not found.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Fix_EdmsDocument_HourUnit_Log', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Fix_EdmsDocument_HourUnit_Log (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        DocumentId BIGINT NULL,
        Step NVARCHAR(30) NOT NULL,
        OldHourPrePerson INT NULL, NewHourPrePerson INT NULL,
        OldHtsId BIGINT NULL, NewHtsId BIGINT NULL,
        OldPreparedUserId BIGINT NULL, NewPreparedUserId BIGINT NULL,
        OldPublicationMiladiDate DATETIME2 NULL, NewPublicationMiladiDate DATETIME2 NULL,
        VpisId BIGINT NULL, OldVpisHtsId BIGINT NULL, NewVpisHtsId BIGINT NULL,
        Note NVARCHAR(200) NULL,
        LoggedOn DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_Fix_EdmsDocument_HourUnit_Log_Doc ON dbo.Fix_EdmsDocument_HourUnit_Log (DocumentId, Step) WHERE DocumentId IS NOT NULL;
    CREATE UNIQUE INDEX UX_Fix_EdmsDocument_HourUnit_Log_Vpis ON dbo.Fix_EdmsDocument_HourUnit_Log (VpisId, Step) WHERE VpisId IS NOT NULL;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    /* ===== HTS side (small projections) ===== */
    IF OBJECT_ID('tempdb..#H') IS NOT NULL DROP TABLE #H;
    SELECT CAST(d.Document_ID AS BIGINT) AS HtsDoc, CAST(d.Revision AS INT) AS Revision,
           CAST(d.Project_Vpis_FK AS BIGINT) AS HtsVpis, CAST(d.ConsumedManHours AS INT) AS Minutes,
           CAST(d.PreparedUser_FK AS INT) AS PreparedUser, CAST(d.Due_Date AS DATETIME2) AS DueDate,
           d.Due_Date_Shamsi AS DueDateShamsi, v.Project_FK AS HtsProject, v.Document_Number AS VpisCode
    INTO #H
    FROM [TMS].[TotalSystem].[dbo].[Edms_Document] d
    JOIN [TMS].[TotalSystem].[dbo].[Edms_Project_Vpis] v ON v.Project_Vpis_ID = d.Project_Vpis_FK;
    CREATE UNIQUE CLUSTERED INDEX IX_H ON #H (HtsDoc);

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, NewUserId INTO #UserMap FROM (
        SELECT CAST(ou.User_ID AS INT) AS OldUserId, nu.Id AS NewUserId,
               ROW_NUMBER() OVER (PARTITION BY ou.User_ID ORDER BY
                   CASE WHEN LOWER(nu.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N''), LTRIM(RTRIM(ou.Username)))) THEN 0
                        WHEN LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username))) THEN 1 ELSE 2 END, nu.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN [system].[User] nu
            ON LOWER(nu.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N''), LTRIM(RTRIM(ou.Username))))
            OR LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
    ) x WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    /* ===== Step 1: the 15 pairs (HTS document id -> Portal document id) ===== */
    IF OBJECT_ID('tempdb..#Pair') IS NOT NULL DROP TABLE #Pair;
    CREATE TABLE #Pair (HtsDoc BIGINT PRIMARY KEY, PortalDoc BIGINT NOT NULL);
    INSERT #Pair (HtsDoc, PortalDoc) VALUES
        (41312, 36328), (42266, 37256), (44557, 39505), (44564, 39512), (45863, 40804), (46034, 40976),
        (30007, 25611), (42231, 37221), (44707, 39654),
        (46039, 41009), (46040, 41010), (46041, 41011), (46042, 41012), (46043, 41008), (46044, 41007);

    IF EXISTS (SELECT 1 FROM #Pair p LEFT JOIN #H h ON h.HtsDoc = p.HtsDoc WHERE h.HtsDoc IS NULL)
        THROW 51801, N'A pair refers to an HTS document that does not exist.', 1;
    IF EXISTS (SELECT 1 FROM #Pair p LEFT JOIN Edms.Document d ON d.Id = p.PortalDoc WHERE d.Id IS NULL)
        THROW 51802, N'A pair refers to a Portal document that does not exist.', 1;

    -- only pairs that are not linked yet (a second run finds none)
    IF OBJECT_ID('tempdb..#Todo') IS NOT NULL DROP TABLE #Todo;
    SELECT p.HtsDoc, p.PortalDoc, d.DocumentVpisId AS PortalVpis, h.HtsVpis, h.Minutes, h.PreparedUser, h.DueDate, h.DueDateShamsi
    INTO #Todo
    FROM #Pair p
    JOIN Edms.Document d ON d.Id = p.PortalDoc
    JOIN #H h ON h.HtsDoc = p.HtsDoc
    WHERE d.HtsId = 0;

    IF EXISTS (SELECT 1 FROM #Todo t JOIN Edms.Document d ON d.Id = t.PortalDoc JOIN #H h ON h.HtsDoc = t.HtsDoc WHERE d.Revision <> h.Revision)
        THROW 51803, N'A pair has different revisions in HTS and Portal.', 1;
    IF EXISTS (SELECT 1 FROM #Todo t JOIN Edms.Document x ON x.HtsId = t.HtsDoc)
        THROW 51804, N'An HTS document id is already used by another Portal document.', 1;
    IF EXISTS (
        SELECT 1 FROM #Todo t
        JOIN #H h ON h.HtsDoc = t.HtsDoc
        JOIN Edms.ProjectVpis v ON v.Id = t.PortalVpis
        LEFT JOIN Edms.Project pr ON pr.Id = v.ProjectNameId
        WHERE ISNULL(pr.HtsId, -1) <> h.HtsProject
           OR v.Code COLLATE DATABASE_DEFAULT <> h.VpisCode COLLATE DATABASE_DEFAULT
           OR (v.HtsId <> 0 AND v.HtsId <> h.HtsVpis))
        THROW 51805, N'A pair VPIS does not match (project / code / HtsId).', 1;
    IF EXISTS (SELECT 1 FROM #Todo t JOIN Edms.ProjectVpis v ON v.HtsId = t.HtsVpis AND v.Id <> t.PortalVpis)
        THROW 51806, N'The HTS VPIS id is already linked to another Portal VPIS.', 1;

    -- 1a) VPIS link (only VPIS that still have HtsId = 0)
    INSERT dbo.Fix_EdmsDocument_HourUnit_Log (Step, VpisId, OldVpisHtsId, NewVpisHtsId, Note)
    SELECT N'1a-vpis-link', t.PortalVpis, v.HtsId, t.HtsVpis, N'Portal VPIS (same project + code) linked to the HTS VPIS'
    FROM (SELECT DISTINCT PortalVpis, HtsVpis FROM #Todo) t
    JOIN Edms.ProjectVpis v ON v.Id = t.PortalVpis
    WHERE v.HtsId = 0;

    UPDATE v SET v.HtsId = t.HtsVpis
    FROM Edms.ProjectVpis v
    JOIN (SELECT DISTINCT PortalVpis, HtsVpis FROM #Todo) t ON t.PortalVpis = v.Id
    WHERE v.HtsId = 0;
    DECLARE @VpisLinked INT = @@ROWCOUNT;

    -- 1b) documents
    INSERT dbo.Fix_EdmsDocument_HourUnit_Log
        (DocumentId, Step, OldHourPrePerson, NewHourPrePerson, OldHtsId, NewHtsId, OldPreparedUserId, NewPreparedUserId,
         OldPublicationMiladiDate, NewPublicationMiladiDate, Note)
    SELECT d.Id, N'1b-link-from-hts', d.HourPrePerson, t.Minutes, d.HtsId, t.HtsDoc, d.PreparedUserId, ISNULL(um.NewUserId, d.PreparedUserId),
           d.PublicationMiladiDate, COALESCE(d.PublicationMiladiDate, t.DueDate),
           CASE WHEN ISNULL(d.HourPrePerson, 0) = t.Minutes THEN N'ok: Portal value was already minutes = HTS minutes'
                WHEN ISNULL(d.HourPrePerson, 0) * 60 = t.Minutes THEN N'ok: Portal hours * 60 = HTS minutes'
                ELSE N'MISMATCH: HTS minutes <> Portal hours * 60 (HTS used)' END
    FROM #Todo t
    JOIN Edms.Document d ON d.Id = t.PortalDoc
    LEFT JOIN #UserMap um ON um.OldUserId = t.PreparedUser;

    UPDATE d SET
        d.HtsId = t.HtsDoc,
        d.HourPrePerson = t.Minutes,
        d.PreparedUserId = ISNULL(um.NewUserId, d.PreparedUserId),
        d.PublicationMiladiDate = COALESCE(d.PublicationMiladiDate, t.DueDate),
        d.PublicationShamsiDate = COALESCE(NULLIF(d.PublicationShamsiDate, N''), NULLIF(LTRIM(RTRIM(t.DueDateShamsi)), N''))
    FROM Edms.Document d
    JOIN #Todo t ON t.PortalDoc = d.Id
    LEFT JOIN #UserMap um ON um.OldUserId = t.PreparedUser
    WHERE d.HtsId = 0;
    DECLARE @PairsLinked INT = @@ROWCOUNT;

    /* ===== Step 2: documents linked to HTS whose hours were edited in Portal ===== */
    IF OBJECT_ID('tempdb..#S2') IS NOT NULL DROP TABLE #S2;
    SELECT d.Id AS DocumentId, d.HtsId, d.HourPrePerson AS OldMin, d.HourPrePerson * 60 AS NewMin, h.Minutes AS HtsMinutes
    INTO #S2
    FROM Edms.Document d
    JOIN #H h ON h.HtsDoc = d.HtsId
    WHERE d.HtsId > 0 AND d.HourPrePerson > 0 AND d.HourPrePerson <> ISNULL(h.Minutes, 0)
      AND d.Id NOT IN (SELECT PortalDoc FROM #Pair)
      AND NOT EXISTS (SELECT 1 FROM dbo.Fix_EdmsDocument_HourUnit_Log l WHERE l.DocumentId = d.Id AND l.Step = N'2-linked-x60');

    INSERT dbo.Fix_EdmsDocument_HourUnit_Log (DocumentId, Step, OldHourPrePerson, NewHourPrePerson, OldHtsId, NewHtsId, Note)
    SELECT DocumentId, N'2-linked-x60', OldMin, NewMin, HtsId, HtsId, CONCAT(N'HTS minutes = ', HtsMinutes) FROM #S2;

    UPDATE d SET d.HourPrePerson = s.NewMin
    FROM Edms.Document d JOIN #S2 s ON s.DocumentId = d.Id;
    DECLARE @Step2 INT = @@ROWCOUNT;

    /* ===== Step 3: Portal-only documents created since the cut-over ===== */
    IF OBJECT_ID('tempdb..#S3') IS NOT NULL DROP TABLE #S3;
    SELECT d.Id AS DocumentId, d.HourPrePerson AS OldMin, d.HourPrePerson * 60 AS NewMin
    INTO #S3
    FROM Edms.Document d
    WHERE d.HtsId = 0 AND d.HourPrePerson > 0 AND d.CreatedOnMiladiDateTime >= @Cutover
      AND NOT EXISTS (SELECT 1 FROM dbo.Fix_EdmsDocument_HourUnit_Log l WHERE l.DocumentId = d.Id AND l.Step = N'3-portal-x60');

    INSERT dbo.Fix_EdmsDocument_HourUnit_Log (DocumentId, Step, OldHourPrePerson, NewHourPrePerson, OldHtsId, NewHtsId, Note)
    SELECT DocumentId, N'3-portal-x60', OldMin, NewMin, 0, 0, N'Portal-only document: hours typed as hours -> minutes' FROM #S3;

    UPDATE d SET d.HourPrePerson = s.NewMin
    FROM Edms.Document d JOIN #S3 s ON s.DocumentId = d.Id;
    DECLARE @Step3 INT = @@ROWCOUNT;

    /* ===== Report ===== */
    SELECT @DryRun AS DryRun, @VpisLinked AS VpisLinked, @PairsLinked AS PairsLinked, @Step2 AS LinkedHoursX60, @Step3 AS PortalOnlyHoursX60;

    SELECT l.DocumentId, l.NewHtsId AS HtsDocId, l.OldHourPrePerson AS OldValue, l.NewHourPrePerson AS NewMinutes,
           l.OldPreparedUserId, l.NewPreparedUserId, l.OldPublicationMiladiDate, l.NewPublicationMiladiDate, l.Note
    FROM dbo.Fix_EdmsDocument_HourUnit_Log l
    WHERE l.Step = N'1b-link-from-hts' AND l.DocumentId IN (SELECT PortalDoc FROM #Pair)
    ORDER BY l.Note, l.DocumentId;

    SELECT Step, COUNT(*) AS Docs, SUM(CAST(OldHourPrePerson AS BIGINT)) AS OldSum, SUM(CAST(NewHourPrePerson AS BIGINT)) AS NewSum
    FROM dbo.Fix_EdmsDocument_HourUnit_Log GROUP BY Step ORDER BY Step;

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'DRY RUN - everything rolled back.';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'Applied.';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
