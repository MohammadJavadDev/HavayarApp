/*
Sync_EdmsProjectActivity_FromTotalSystem.sql
One-time / idempotent sync (via linked server [TMS]):
  Edms_Project_Activity (ModuleType_FK = 386, EDMS)  -> Edms.ProjectActivity
  Edms_Project_Activity_Comment (of those activities) -> Edms.ProjectActivityComment

HtsId:
  Activity = Edms_Project_Activity.Project_Activity_ID
  Comment  = Edms_Project_Activity_Comment.Project_Activity_Comment_ID
Portal rows with HtsId = 0 (entered in Portal after cut-over) are never overwritten.

Mappings:
  ProjectId        : Edms.Project.HtsId = Project_FK. NULL when the HTS row has no project (free-text ProjectName only).
  ProjectName      : copied as is (free text kept).
  BeneficiaryUnitId: HRM_OrgUnit.Hamkaran_Unit_FK -> Hrm.OrgUnit.HamkaranUnitId, else a UNIQUE exact title match,
                     else NULL (never another unit).
  Type             : ActivityType_FK (lookup 84 = ProjectActivityTypeEnum values). NULL stays NULL.
  Comment          : ActivityComment (HTS "شرح فعالیت"). When HTS Comment (separate "توضیحات" field, ignored when
                     empty or '-') exists it is appended after "  |  توضیحات: " so nothing is lost.
  MiladiDate       : ActivityDate_Miladi (never NULL in HTS).
  ShamsiDate       : rebuilt from ActivityDate_Miladi as yyyy/MM/dd (HTS ActivityDate is often not zero padded
                     or '????/??/??').
  Duration         : copied (minutes).
  CreatedById      : Gnr_User -> system.User by ActiveDirectoryUsername / Username. No fallback to another user:
                     the script aborts when a creator cannot be mapped.
  CreatedOn*       : HTS CreatedDate (Shamsi) + CreatedTime; falls back to ActivityDate_Miladi when malformed.
  Modified*        : equal to Created* (HTS keeps no modification data).
  Comment.Status   : HTS 5 (Issue) -> DocumentStatusEnums.Issue (2); HTS 2 (Approve) -> ApproveByApprover (7);
                     HTS 4 (Commented) -> CommentedByApprover (8). Any other value aborts the script.
  Duplicate Issue comments created by HTS on every edit are copied as they are (real history).

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

IF COL_LENGTH(N'Edms.ProjectActivity', N'HtsId') IS NULL OR OBJECT_ID(N'Edms.ProjectActivityComment', N'U') IS NULL
BEGIN
    RAISERROR(N'Edms.ProjectActivity.HtsId / Edms.ProjectActivityComment missing - apply migration AddActivityHtsIdAndDocumentPreparedUser first.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @Seed NVARCHAR(80) = N'sync-edms-projectactivity';
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
    WHERE a.ModuleType_FK = 386;
    CREATE UNIQUE CLUSTERED INDEX IX_ActRaw ON #ActRaw (HtsId);

    IF EXISTS (SELECT 1 FROM #ActRaw WHERE MiladiDate IS NULL)
        THROW 51711, N'An HTS activity has no ActivityDate_Miladi.', 1;

    IF EXISTS (SELECT 1 FROM #ActRaw r LEFT JOIN #UserMap m ON m.OldUserId = r.OldCreatedUserId WHERE m.NewUserId IS NULL)
        THROW 51712, N'An HTS activity creator cannot be mapped to a Portal user (no fallback is used).', 1;

    /* Shamsi date text per distinct Miladi date (avoid the scalar loop function on every row) */
    IF OBJECT_ID('tempdb..#MilToSh') IS NOT NULL DROP TABLE #MilToSh;
    SELECT d AS MiladiDate, dbo.ToShamsiDate(CAST(d AS DATE), N'/') AS ShamsiDate
    INTO #MilToSh
    FROM (SELECT DISTINCT CAST(MiladiDate AS DATE) AS d FROM #ActRaw) x;
    CREATE UNIQUE CLUSTERED INDEX IX_MilToSh ON #MilToSh (MiladiDate);

    /* HTS CreatedDate is a Shamsi text, sometimes not zero padded: normalise once per distinct text */
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

    IF OBJECT_ID('tempdb..#ActSrc') IS NOT NULL DROP TABLE #ActSrc;
    SELECT
        r.HtsId,
        p.Id AS ProjectId,
        r.Project_FK,
        r.ProjectName,
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
    LEFT JOIN Edms.Project p ON p.HtsId = r.Project_FK AND p.HtsId <> 0
    LEFT JOIN #OrgUnitMap oum ON oum.OldOrgUnitId = r.BeneficiaryUnit_FK
    INNER JOIN #UserMap um ON um.OldUserId = r.OldCreatedUserId
    INNER JOIN #MilToSh ms ON ms.MiladiDate = CAST(r.MiladiDate AS DATE)
    LEFT JOIN #ShNorm sn ON sn.raw = r.CreatedDateRaw;
    CREATE UNIQUE CLUSTERED INDEX IX_ActSrc ON #ActSrc (HtsId);

    PRINT N'Syncing Edms.ProjectActivity...';

    UPDATE t SET
        t.ProjectId = s.ProjectId,
        t.ProjectName = s.ProjectName,
        t.BeneficiaryUnitId = s.BeneficiaryUnitId,
        t.Type = s.Type,
        t.Comment = s.Comment,
        t.MiladiDate = s.MiladiDate,
        t.ShamsiDate = s.ShamsiDate,
        t.Duration = s.Duration,
        t.CreatedById = s.CreatedById,
        t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.ModifiedById = s.CreatedById,
        t.ModifiedByName = s.CreatedByName,
        t.ModifiedDateMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.ModifiedDateShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.IsActive = 1
    FROM Edms.ProjectActivity t
    INNER JOIN #ActSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  ProjectActivity updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Edms.ProjectActivity (
        HtsId, ProjectId, ProjectName, BeneficiaryUnitId, Type, Comment, MiladiDate, ShamsiDate, Duration,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.ProjectId, s.ProjectName, s.BeneficiaryUnitId, s.Type, s.Comment, s.MiladiDate, s.ShamsiDate, s.Duration,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, 1
    FROM #ActSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Edms.ProjectActivity t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  ProjectActivity inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ========== Comments ========== */
    IF OBJECT_ID('tempdb..#CmtSrc') IS NOT NULL DROP TABLE #CmtSrc;
    SELECT
        CAST(c.Project_Activity_Comment_ID AS BIGINT) AS HtsId,
        act.Id AS ProjectActivityId,
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
    INNER JOIN Edms.ProjectActivity act ON act.HtsId = CAST(c.Project_Activity_FK AS BIGINT) AND act.HtsId <> 0
    INNER JOIN #ActRaw ar ON ar.HtsId = c.Project_Activity_FK
    LEFT JOIN #UserMap um ON um.OldUserId = CAST(c.CreatedUser_FK AS INT);
    CREATE UNIQUE CLUSTERED INDEX IX_CmtSrc ON #CmtSrc (HtsId);

    IF EXISTS (SELECT 1 FROM #CmtSrc WHERE Status IS NULL)
        THROW 51713, N'An HTS activity comment has a status that is not mapped (expected 2, 4 or 5).', 1;

    UPDATE t SET
        t.ProjectActivityId = s.ProjectActivityId,
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
    FROM Edms.ProjectActivityComment t
    INNER JOIN #CmtSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  ProjectActivityComment updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Edms.ProjectActivityComment (
        HtsId, ProjectActivityId, Status, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.ProjectActivityId, s.Status, s.Comment,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, 1
    FROM #CmtSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Edms.ProjectActivityComment t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  ProjectActivityComment inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    PRINT N'=== VERIFY COUNTS ===';
    SELECT
        (SELECT COUNT(*) FROM #ActRaw) AS HtsActivities,
        (SELECT COUNT(*) FROM Edms.ProjectActivity WHERE HtsId <> 0) AS PortalActivitiesSynced,
        (SELECT COUNT(*) FROM Edms.ProjectActivity WHERE HtsId = 0) AS PortalOnlyActivities,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Edms_Project_Activity_Comment] c
            INNER JOIN #ActRaw ar ON ar.HtsId = c.Project_Activity_FK) AS HtsComments,
        (SELECT COUNT(*) FROM Edms.ProjectActivityComment WHERE HtsId <> 0) AS PortalCommentsSynced;

    PRINT N'=== INFORMATIONAL (kept as NULL / text) ===';
    SELECT
        SUM(CASE WHEN Project_FK IS NULL THEN 1 ELSE 0 END) AS NoProjectInHts,
        SUM(CASE WHEN Project_FK IS NOT NULL AND ProjectId IS NULL THEN 1 ELSE 0 END) AS ProjectNotFoundInPortal,
        SUM(CASE WHEN HtsType IS NULL THEN 1 ELSE 0 END) AS NoTypeInHts,
        SUM(CASE WHEN HtsType IS NOT NULL AND Type IS NULL THEN 1 ELSE 0 END) AS UnknownTypeId,
        SUM(CASE WHEN BeneficiaryUnitId IS NULL THEN 1 ELSE 0 END) AS NoBeneficiaryUnit
    FROM #ActSrc;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'Sync failed (line %d): %s', 16, 1, @Line, @Err);
END CATCH;
