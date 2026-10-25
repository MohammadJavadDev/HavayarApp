/*
Sync_EpmsProposalActivity_FromTotalSystem.sql
One-time / idempotent sync (via linked server [TMS]):
  Edms_Project_Activity (ModuleType_FK = 387, EPMS)   -> Epms.ProjectActivity (entity ProposalActivity)
  Edms_Project_Activity_Comment (of those activities) -> Epms.ProposalActivityComment

Run Sync_EpmsProposal_FromTotalSystem.sql first (ProposalId is resolved through Epms.Proposal.HtsId).

HtsId:
  Activity = Edms_Project_Activity.Project_Activity_ID
  Comment  = Edms_Project_Activity_Comment.Project_Activity_Comment_ID
Portal rows with HtsId = 0 are never overwritten.

Mappings:
  ProposalId       : Epms.Proposal.HtsId = Proposal_FK. NULL when HTS has no proposal, or the proposal was not
                     migrated (e.g. skipped because its requested unit has no Portal unit).
  ProjectName      : HTS Project_Name. When it is empty:
                       - the HTS project "code name" (14 rows carry Project_FK; Epms has no project column), or
                       - the HTS proposal "code title" when the proposal could not be linked.
  BeneficiaryUnitId: Hamkaran id, else a UNIQUE exact title match, else NULL.
  Type             : ActivityType_FK (lookup 84). NULL stays NULL.
  Comment          : ActivityComment; HTS Comment (ignored when empty or '-') appended after "  |  توضیحات: ".
  MiladiDate       : ActivityDate_Miladi. ShamsiDate rebuilt from it as yyyy/MM/dd.
  Duration         : copied (minutes).
  CreatedById      : Gnr_User -> system.User; the script aborts when a creator cannot be mapped (no fallback).
  Status (parent)  : from the latest comment: HTS 5 Issue -> 1, HTS 2 Approve -> 4, HTS 4 Commented -> 2;
                     1 (Issue) when the activity has no comment.
  Comment.Status   : HTS 5 -> DocumentStatusEnums.Issue (2); 2 -> ApproveByApprover (7); 4 -> CommentedByApprover (8).
                     Any other value aborts the script.

Idempotent UPDATE-then-INSERT on HtsId. Never deletes target rows.
UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i ...
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] not found.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'Epms.ProjectActivity', N'HtsId') IS NULL OR COL_LENGTH(N'Epms.ProposalActivityComment', N'HtsId') IS NULL
   OR COL_LENGTH(N'Epms.Proposal', N'HtsId') IS NULL
BEGIN
    RAISERROR(N'HtsId columns missing - apply migration AddActivityHtsIdAndDocumentPreparedUser first.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @CommentSep NVARCHAR(40) = N'  |  ' + N'توضیحات' + N': ';

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== User map ========== */
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
                ORDER BY
                    CASE
                        WHEN LOWER(nu.Username) = LOWER(COALESCE(
                                NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N''),
                                LTRIM(RTRIM(ou.Username))))
                        THEN 0
                        WHEN LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username))) THEN 1
                        ELSE 2
                    END,
                    nu.Id
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN [system].[User] nu
            ON LOWER(nu.Username) = LOWER(COALESCE(
                    NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N''),
                    LTRIM(RTRIM(ou.Username))))
            OR LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    /* ========== OrgUnit map: Hamkaran id, else unique exact title, else NULL ========== */
    IF OBJECT_ID('tempdb..#OrgUnitMap') IS NOT NULL DROP TABLE #OrgUnitMap;
    SELECT
        hrm.OrgUnit_ID AS OldOrgUnitId,
        COALESCE(byHam.Id, byTitle.Id) AS NewOrgUnitId
    INTO #OrgUnitMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hrm
    OUTER APPLY (
        SELECT TOP (1) o.Id
        FROM Hrm.OrgUnit o
        WHERE ISNULL(hrm.Hamkaran_Unit_FK, 0) <> 0
          AND o.HamkaranUnitId = CAST(hrm.Hamkaran_Unit_FK AS BIGINT)
        ORDER BY o.Id
    ) byHam
    OUTER APPLY (
        SELECT MIN(o.Id) AS Id
        FROM Hrm.OrgUnit o
        WHERE byHam.Id IS NULL
          AND NULLIF(LTRIM(RTRIM(hrm.OrgUnit_Title)), N'') IS NOT NULL
          AND o.Title COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(hrm.OrgUnit_Title)) COLLATE DATABASE_DEFAULT
        HAVING COUNT(*) = 1
    ) byTitle;
    CREATE UNIQUE CLUSTERED INDEX IX_OrgUnitMap ON #OrgUnitMap (OldOrgUnitId);

    /* ========== Activity staging ========== */
    IF OBJECT_ID('tempdb..#ActRaw') IS NOT NULL DROP TABLE #ActRaw;
    SELECT
        CAST(a.Project_Activity_ID AS BIGINT) AS HtsId,
        a.Project_FK,
        a.Proposal_FK,
        NULLIF(LTRIM(RTRIM(a.Project_Name)), N'') AS ProjectName,
        a.BeneficiaryUnit_FK,
        CAST(a.ActivityType_FK AS INT) AS ActivityType,
        NULLIF(LTRIM(RTRIM(a.ActivityComment)), N'') AS ActivityComment,
        CASE WHEN NULLIF(LTRIM(RTRIM(a.Comment)), N'') IS NULL OR LTRIM(RTRIM(a.Comment)) = N'-' THEN NULL
             ELSE LTRIM(RTRIM(a.Comment)) END AS ExtraComment,
        CAST(a.ActivityDate_Miladi AS DATETIME2) AS MiladiDate,
        CAST(a.Duration AS BIGINT) AS Duration,
        CAST(a.CreatedUser_FK AS INT) AS OldCreatedUserId,
        LTRIM(RTRIM(a.CreatedDate)) AS CreatedDateRaw,
        LTRIM(RTRIM(a.CreatedTime)) AS CreatedTimeRaw
    INTO #ActRaw
    FROM [TMS].[TotalSystem].[dbo].[Edms_Project_Activity] a
    WHERE a.ModuleType_FK = 387;
    CREATE UNIQUE CLUSTERED INDEX IX_ActRaw ON #ActRaw (HtsId);

    IF EXISTS (SELECT 1 FROM #ActRaw WHERE MiladiDate IS NULL)
        THROW 51721, N'An HTS activity has no ActivityDate_Miladi.', 1;

    IF EXISTS (SELECT 1 FROM #ActRaw r LEFT JOIN #UserMap m ON m.OldUserId = r.OldCreatedUserId WHERE m.NewUserId IS NULL)
        THROW 51722, N'An HTS activity creator cannot be mapped to a Portal user (no fallback is used).', 1;

    IF OBJECT_ID('tempdb..#MilToSh') IS NOT NULL DROP TABLE #MilToSh;
    SELECT d AS MiladiDate, dbo.ToShamsiDate(CAST(d AS DATE), N'/') AS ShamsiDate
    INTO #MilToSh
    FROM (SELECT DISTINCT CAST(MiladiDate AS DATE) AS d FROM #ActRaw) x;
    CREATE UNIQUE CLUSTERED INDEX IX_MilToSh ON #MilToSh (MiladiDate);

    IF OBJECT_ID('tempdb..#ShNorm') IS NOT NULL DROP TABLE #ShNorm;
    SELECT
        raw,
        norm,
        TRY_CAST(dbo.fn_ShamsiToMiladiDate(norm, N'/') AS DATE) AS Miladi
    INTO #ShNorm
    FROM (
        SELECT
            raw,
            CASE WHEN LEN(raw) - LEN(REPLACE(raw, N'/', N'')) = 2
                      AND TRY_CAST(PARSENAME(REPLACE(raw, N'/', N'.'), 3) AS INT) IS NOT NULL
                      AND TRY_CAST(PARSENAME(REPLACE(raw, N'/', N'.'), 2) AS INT) IS NOT NULL
                      AND TRY_CAST(PARSENAME(REPLACE(raw, N'/', N'.'), 1) AS INT) IS NOT NULL
                 THEN PARSENAME(REPLACE(raw, N'/', N'.'), 3) + N'/'
                      + RIGHT(N'0' + PARSENAME(REPLACE(raw, N'/', N'.'), 2), 2) + N'/'
                      + RIGHT(N'0' + PARSENAME(REPLACE(raw, N'/', N'.'), 1), 2)
            END AS norm
        FROM (SELECT DISTINCT CreatedDateRaw AS raw FROM #ActRaw WHERE CreatedDateRaw IS NOT NULL) d
    ) n;

    /* Latest comment status per activity (HTS comment id order) */
    IF OBJECT_ID('tempdb..#LastCmt') IS NOT NULL DROP TABLE #LastCmt;
    SELECT Project_Activity_FK, Status_FK
    INTO #LastCmt
    FROM (
        SELECT c.Project_Activity_FK, c.Status_FK,
               ROW_NUMBER() OVER (PARTITION BY c.Project_Activity_FK ORDER BY c.Project_Activity_Comment_ID DESC) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Edms_Project_Activity_Comment] c
        INNER JOIN #ActRaw ar ON ar.HtsId = c.Project_Activity_FK
    ) z
    WHERE rn = 1;

    IF OBJECT_ID('tempdb..#ActSrc') IS NOT NULL DROP TABLE #ActSrc;
    SELECT
        r.HtsId,
        prop.Id AS ProposalId,
        r.Proposal_FK,
        COALESCE(
            r.ProjectName,
            CASE WHEN r.Project_FK IS NOT NULL
                 THEN NULLIF(LTRIM(RTRIM(CONCAT(hp.Project_Code, N' ', hp.Project_Name))), N'') END,
            CASE WHEN r.Proposal_FK IS NOT NULL AND prop.Id IS NULL
                 THEN NULLIF(LTRIM(RTRIM(CONCAT(hpr.Proposal_Code, N' ', hpr.Proposal_Title))), N'') END
        ) AS ProjectName,
        oum.NewOrgUnitId AS BeneficiaryUnitId,
        CASE WHEN r.ActivityType IN (373,374,375,376,377,378,379,380,381,382,383,384,385,401,402,403,463,464,465,466,467,468)
             THEN r.ActivityType END AS Type,
        r.ActivityType AS HtsType,
        CASE
            WHEN r.ActivityComment IS NOT NULL AND r.ExtraComment IS NOT NULL THEN r.ActivityComment + @CommentSep + r.ExtraComment
            WHEN r.ActivityComment IS NOT NULL THEN r.ActivityComment
            WHEN r.ExtraComment IS NOT NULL THEN LTRIM(@CommentSep) + r.ExtraComment
        END AS Comment,
        r.MiladiDate,
        ms.ShamsiDate,
        r.Duration,
        CASE lc.Status_FK WHEN 2 THEN 4 WHEN 4 THEN 2 ELSE 1 END AS Status,
        um.NewUserId AS CreatedById,
        um.NewUserName AS CreatedByName,
        COALESCE(
            DATEADD(MINUTE,
                ISNULL(DATEDIFF(MINUTE, CAST('00:00' AS TIME), TRY_CAST(r.CreatedTimeRaw AS TIME)), 0),
                CAST(sn.Miladi AS DATETIME2)),
            r.MiladiDate) AS CreatedOnMiladiDateTime,
        CASE WHEN sn.Miladi IS NOT NULL
             THEN sn.norm + N' ' + ISNULL(CASE WHEN TRY_CAST(r.CreatedTimeRaw AS TIME) IS NOT NULL THEN r.CreatedTimeRaw END, N'00:00') + N':00'
             ELSE ms.ShamsiDate + N' 00:00:00' END AS CreatedOnShamsiDateTime
    INTO #ActSrc
    FROM #ActRaw r
    LEFT JOIN Epms.Proposal prop ON prop.HtsId = r.Proposal_FK AND prop.HtsId <> 0
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Project] hp ON hp.Edms_Project_ID = r.Project_FK
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Epms_Proposal] hpr ON hpr.Proposal_ID = r.Proposal_FK
    LEFT JOIN #OrgUnitMap oum ON oum.OldOrgUnitId = r.BeneficiaryUnit_FK
    LEFT JOIN #LastCmt lc ON lc.Project_Activity_FK = r.HtsId
    INNER JOIN #UserMap um ON um.OldUserId = r.OldCreatedUserId
    INNER JOIN #MilToSh ms ON ms.MiladiDate = CAST(r.MiladiDate AS DATE)
    LEFT JOIN #ShNorm sn ON sn.raw = r.CreatedDateRaw;
    CREATE UNIQUE CLUSTERED INDEX IX_ActSrc ON #ActSrc (HtsId);

    PRINT N'Syncing Epms.ProjectActivity...';

    UPDATE t SET
        t.ProposalId = s.ProposalId,
        t.ProjectName = s.ProjectName,
        t.BeneficiaryUnitId = s.BeneficiaryUnitId,
        t.Type = s.Type,
        t.Comment = s.Comment,
        t.MiladiDate = s.MiladiDate,
        t.ShamsiDate = s.ShamsiDate,
        t.Duration = s.Duration,
        t.Status = s.Status,
        t.CreatedById = s.CreatedById,
        t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.ModifiedById = s.CreatedById,
        t.ModifiedByName = s.CreatedByName,
        t.ModifiedDateMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.ModifiedDateShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.IsActive = 1
    FROM Epms.ProjectActivity t
    INNER JOIN #ActSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  ProjectActivity updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Epms.ProjectActivity (
        HtsId, ProposalId, ProjectName, BeneficiaryUnitId, Type, Comment, MiladiDate, ShamsiDate, Duration, Status,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.ProposalId, s.ProjectName, s.BeneficiaryUnitId, s.Type, s.Comment, s.MiladiDate, s.ShamsiDate, s.Duration, s.Status,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, 1
    FROM #ActSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Epms.ProjectActivity t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  ProjectActivity inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ========== Comments ========== */
    IF OBJECT_ID('tempdb..#CmtSrc') IS NOT NULL DROP TABLE #CmtSrc;
    SELECT
        CAST(c.Project_Activity_Comment_ID AS BIGINT) AS HtsId,
        act.Id AS ProposalActivityId,
        CAST(c.Status_FK AS INT) AS HtsStatus,
        CASE c.Status_FK WHEN 5 THEN 2 WHEN 2 THEN 7 WHEN 4 THEN 8 END AS Status,
        ISNULL(LTRIM(RTRIM(c.Comment)), N'') AS Comment,
        um.NewUserId AS CreatedById,
        um.NewUserName AS CreatedByName,
        CAST(c.CreatedDate AS DATETIME2) AS CreatedOnMiladiDateTime,
        CASE WHEN LEN(LTRIM(RTRIM(c.CreatedDate_Shamsi))) = 10
             THEN LTRIM(RTRIM(c.CreatedDate_Shamsi)) + N' ' + ISNULL(LTRIM(RTRIM(c.CreatedTime)), N'00:00') + N':00' END AS CreatedOnShamsiDateTime
    INTO #CmtSrc
    FROM [TMS].[TotalSystem].[dbo].[Edms_Project_Activity_Comment] c
    INNER JOIN Epms.ProjectActivity act ON act.HtsId = CAST(c.Project_Activity_FK AS BIGINT) AND act.HtsId <> 0
    INNER JOIN #ActRaw ar ON ar.HtsId = c.Project_Activity_FK
    LEFT JOIN #UserMap um ON um.OldUserId = CAST(c.CreatedUser_FK AS INT);
    CREATE UNIQUE CLUSTERED INDEX IX_CmtSrc ON #CmtSrc (HtsId);

    IF EXISTS (SELECT 1 FROM #CmtSrc WHERE Status IS NULL)
        THROW 51723, N'An HTS activity comment has a status that is not mapped (expected 2, 4 or 5).', 1;

    UPDATE t SET
        t.ProposalActivityId = s.ProposalActivityId,
        t.Status = s.Status,
        t.Comment = s.Comment,
        t.CreatedById = s.CreatedById,
        t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.ModifiedById = s.CreatedById,
        t.ModifiedByName = s.CreatedByName,
        t.ModifiedDateMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.ModifiedDateShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.IsActive = 1
    FROM Epms.ProposalActivityComment t
    INNER JOIN #CmtSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  ProposalActivityComment updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Epms.ProposalActivityComment (
        HtsId, ProposalActivityId, Status, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.ProposalActivityId, s.Status, s.Comment,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, 1
    FROM #CmtSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Epms.ProposalActivityComment t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  ProposalActivityComment inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    PRINT N'=== VERIFY COUNTS ===';
    SELECT
        (SELECT COUNT(*) FROM #ActRaw) AS HtsActivities,
        (SELECT COUNT(*) FROM Epms.ProjectActivity WHERE HtsId <> 0) AS PortalActivitiesSynced,
        (SELECT COUNT(*) FROM Epms.ProjectActivity WHERE HtsId = 0) AS PortalOnlyActivities,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Edms_Project_Activity_Comment] c
            INNER JOIN #ActRaw ar ON ar.HtsId = c.Project_Activity_FK) AS HtsComments,
        (SELECT COUNT(*) FROM Epms.ProposalActivityComment WHERE HtsId <> 0) AS PortalCommentsSynced;

    PRINT N'=== INFORMATIONAL (kept as NULL / text) ===';
    SELECT
        SUM(CASE WHEN Proposal_FK IS NULL THEN 1 ELSE 0 END) AS NoProposalInHts,
        SUM(CASE WHEN Proposal_FK IS NOT NULL AND ProposalId IS NULL THEN 1 ELSE 0 END) AS ProposalNotMigrated,
        SUM(CASE WHEN HtsType IS NULL THEN 1 ELSE 0 END) AS NoTypeInHts,
        SUM(CASE WHEN HtsType IS NOT NULL AND Type IS NULL THEN 1 ELSE 0 END) AS UnknownTypeId,
        SUM(CASE WHEN BeneficiaryUnitId IS NULL THEN 1 ELSE 0 END) AS NoBeneficiaryUnit,
        SUM(CASE WHEN ProjectName IS NULL THEN 1 ELSE 0 END) AS NoProjectNameText
    FROM #ActSrc;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'Sync failed (line %d): %s', 16, 1, @Line, @Err);
END CATCH;
