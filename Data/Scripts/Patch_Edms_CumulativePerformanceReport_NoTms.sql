/*
Patch_Edms_CumulativePerformanceReport_NoTms.sql
گزارش تجمعی عملکرد/فعالیت (system.SavedQuery Name = Edms_CumulativePerformanceReport):
rewrite the grid query so it reads ONLY Portal tables (no [TMS] linked server).

Run order: Seed_Edms_CumulativePerformanceReport_DataProfile.sql (creates the profile, TMS version)
           -> this patch (replaces CustomQuery). Re-running the seed alone puts the [TMS] query back.

What changes (columns, aliases, parameters FromDate/ToDate and role logic are unchanged):
  Documents  : Edms.Document + Edms.ProjectVpis
                 hours   = Document.HourPrePerson            (HTS Edms_Document.ConsumedManHours)
                 user    = Document.PreparedUserId           (snapshot copied from HTS PreparedUser_FK /
                                                              from the VPIS producer when the document was created)
                 date    = CAST(Document.CreatedOnMiladiDateTime AS date)   (HTS PreparedDate = CreatedDate)
                 project = Document.ProjectId, else ProjectVpis.ProjectNameId
                 excluded: IsActive <> 1, Status Hold (16) and NotIssue (1, VPIS placeholder, no file)
  Activities : Edms.ProjectActivity (HTS ModuleType_FK = 386) by CreatedById and MiladiDate
  Users      : system.User (NameFa, else Name, else Username) - shown instead of the HTS English full name
  Access     : unchanged. ShowAll = admin role or role «کارشناسان مشاهده کلی گزارش تجمعی عملکرد/فعالیت EDMS»;
               otherwise only the current user's own rows (@CuId = system.User.Id, no Gnr_User lookup).
  Row logic  : same as HTS - one row per (preparer, project) with documents; users with activities but no document
               in the range get one summary row (RowKind 2).

The prefix of the old query (date parameter normalisation, role flags) is kept as it is.
Idempotent: does nothing when the query no longer contains [TMS].
UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i ...
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Name NVARCHAR(200) = N'Edms_CumulativePerformanceReport';
    DECLARE @Id BIGINT, @Q NVARCHAR(MAX), @Old NVARCHAR(MAX);

    SELECT @Id = q.Id, @Q = q.QueryJson
    FROM system.SavedQuery q
    WHERE q.Name = @Name;

    IF @Id IS NULL
        THROW 51741, N'SavedQuery Edms_CumulativePerformanceReport not found.', 1;

    SELECT @Old = j.CustomQuery
    FROM OPENJSON(@Q) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') j;

    IF CHARINDEX(N'[TMS]', @Old) = 0
    BEGIN
        PRINT N'Already patched (no [TMS] in the query) - nothing to do.';
        COMMIT TRANSACTION;
        RETURN;
    END;

    DECLARE @Cut INT = CHARINDEX(N'SELECT TOP (1) @HtsUserId', @Old);
    IF @Cut = 0
        THROW 51742, N'Unexpected query shape (HtsUserId lookup not found).', 1;

    DECLARE @Prefix NVARCHAR(MAX) = SUBSTRING(@Old, 1, @Cut - 1);
    SET @Prefix = REPLACE(@Prefix, N'DECLARE @HtsUserId int = NULL;' + CHAR(13) + CHAR(10), N'');
    IF CHARINDEX(N'@HtsUserId', @Prefix) > 0
        THROW 51743, N'@HtsUserId is still referenced in the kept prefix.', 1;
    IF CHARINDEX(N'SET @HasShowAll', @Prefix) = 0
        THROW 51744, N'Kept prefix does not contain @HasShowAll.', 1;

    DECLARE @Tail NVARCHAR(MAX) = N'DECLARE @CanSee bit = CASE
    WHEN @HasRange = 0 THEN 0
    WHEN @HasShowAll = 1 THEN 1
    WHEN @CuId IS NULL THEN 0
    ELSE 1
END;

;WITH FilteredDocs AS (
    SELECT
        COALESCE(d.ProjectId, v.ProjectNameId) AS ProjectId,
        d.PreparedUserId AS UserId,
        COALESCE(NULLIF(LTRIM(RTRIM(u.NameFa)), N''''), u.Name, u.Username) AS PreparedUser,
        CAST(ISNULL(d.HourPrePerson, 0) AS bigint) AS ConsumedManHours
    FROM Edms.Document AS d
    LEFT JOIN Edms.ProjectVpis AS v ON v.Id = d.DocumentVpisId
    INNER JOIN system.[User] AS u ON u.Id = d.PreparedUserId
    WHERE @CanSee = 1
      AND d.IsActive = 1
      AND COALESCE(d.ProjectId, v.ProjectNameId) IS NOT NULL
      AND (d.Status IS NULL OR d.Status NOT IN (1, 16))
      AND (@HasShowAll = 1 OR d.PreparedUserId = @CuId)
      AND (@From IS NULL OR CAST(d.CreatedOnMiladiDateTime AS date) >= @From)
      AND (@To IS NULL OR CAST(d.CreatedOnMiladiDateTime AS date) <= @To)
),
DocAgg AS (
    SELECT
        ProjectId,
        UserId,
        MIN(PreparedUser) AS PreparedUser,
        SUM(ConsumedManHours) AS TotalPerformance
    FROM FilteredDocs
    GROUP BY ProjectId, UserId
),
FilteredActs AS (
    SELECT
        a.ProjectId,
        a.CreatedById AS UserId,
        COALESCE(NULLIF(LTRIM(RTRIM(u.NameFa)), N''''), u.Name, u.Username) AS FullName,
        CAST(ISNULL(a.Duration, 0) AS bigint) AS Duration
    FROM Edms.ProjectActivity AS a
    INNER JOIN system.[User] AS u ON u.Id = a.CreatedById
    WHERE @CanSee = 1
      AND a.IsActive = 1
      AND (@HasShowAll = 1 OR a.CreatedById = @CuId)
      AND (@From IS NULL OR CAST(a.MiladiDate AS date) >= @From)
      AND (@To IS NULL OR CAST(a.MiladiDate AS date) <= @To)
),
ActByProject AS (
    SELECT ProjectId, UserId, SUM(Duration) AS TotalActivity
    FROM FilteredActs
    GROUP BY ProjectId, UserId
),
DocRows AS (
    SELECT
        CAST(1 AS int) AS RowKind,
        d.UserId,
        CAST(d.PreparedUser AS nvarchar(1024)) AS PreparedUser,
        CAST(p.Code AS nvarchar(256)) AS ProjectCode,
        CAST(p.Name AS nvarchar(512)) AS ProjectName,
        CAST(ou.Title AS nvarchar(256)) AS BeneficiaryUnit,
        d.TotalPerformance,
        ISNULL(a.TotalActivity, CAST(0 AS bigint)) AS TotalActivity
    FROM DocAgg AS d
    INNER JOIN Edms.Project AS p
        ON p.Id = d.ProjectId
    LEFT JOIN Hrm.OrgUnit AS ou
        ON ou.Id = p.SubjectUnitId
    LEFT JOIN ActByProject AS a
        ON a.ProjectId = d.ProjectId AND a.UserId = d.UserId
),
ActOnly AS (
    SELECT
        CAST(2 AS int) AS RowKind,
        f.UserId,
        CAST(MIN(f.FullName) AS nvarchar(1024)) AS PreparedUser,
        CAST(NULL AS nvarchar(256)) AS ProjectCode,
        CAST(NULL AS nvarchar(512)) AS ProjectName,
        CAST(NULL AS nvarchar(256)) AS BeneficiaryUnit,
        CAST(0 AS bigint) AS TotalPerformance,
        SUM(f.Duration) AS TotalActivity
    FROM FilteredActs AS f
    WHERE NOT EXISTS (SELECT 1 FROM DocAgg AS d WHERE d.UserId = f.UserId)
    GROUP BY f.UserId
),
Combined AS (
    SELECT * FROM DocRows
    UNION ALL
    SELECT * FROM ActOnly
)
--!--mainsection
SELECT
    ROW_NUMBER() OVER (ORDER BY c.PreparedUser, c.ProjectCode, c.UserId, c.RowKind) AS [t1_Id],
    c.PreparedUser AS [t1_PreparedUser],
    c.ProjectCode AS [t1_ProjectCode],
    c.ProjectName AS [t1_ProjectName],
    c.BeneficiaryUnit AS [t1_BeneficiaryUnit],
    c.TotalPerformance AS [t1_TotalPerformance],
    CASE WHEN c.RowKind = 2 THEN NULL ELSE perf.HourText END AS [t1_TotalPerformanceInText],
    c.TotalActivity AS [t1_TotalActivity],
    act.HourText AS [t1_TotalActivityInText],
    SUM(c.TotalPerformance) OVER () AS [t1_PerformanceSum],
    SUM(c.TotalActivity) OVER () AS [t1_ActivitySum]
FROM Combined AS c
CROSS APPLY (
    SELECT CONCAT(
        CASE WHEN c.TotalPerformance < 0 THEN N''-'' ELSE N'''' END,
        CASE WHEN ABS(c.TotalPerformance) / 60 < 10 THEN N''0'' ELSE N'''' END,
        CONVERT(nvarchar(20), ABS(c.TotalPerformance) / 60),
        N'':'',
        RIGHT(N''0'' + CONVERT(nvarchar(20), ABS(c.TotalPerformance) % 60), 2)
    ) AS HourText
) AS perf
CROSS APPLY (
    SELECT CONCAT(
        CASE WHEN c.TotalActivity < 0 THEN N''-'' ELSE N'''' END,
        CASE WHEN ABS(c.TotalActivity) / 60 < 10 THEN N''0'' ELSE N'''' END,
        CONVERT(nvarchar(20), ABS(c.TotalActivity) / 60),
        N'':'',
        RIGHT(N''0'' + CONVERT(nvarchar(20), ABS(c.TotalActivity) % 60), 2)
    ) AS HourText
) AS act';

    DECLARE @New NVARCHAR(MAX) = @Prefix + @Tail;

    IF CHARINDEX(N'[TMS]', @New) > 0
        THROW 51745, N'The new query still references [TMS].', 1;

    DECLARE @NewQueryJson NVARCHAR(MAX) = JSON_MODIFY(@Q, '$.CustomQuery', @New);
    IF ISJSON(@NewQueryJson) <> 1
        THROW 51746, N'Patched QueryJson is not valid JSON.', 1;

    UPDATE system.SavedQuery
    SET QueryJson = @NewQueryJson,
        ModifiedByName = N'patch-edms-cumulative-no-tms',
        ModifiedDateMiladiDateTime = SYSDATETIME()
    WHERE Id = @Id;

    COMMIT TRANSACTION;
    PRINT N'Edms_CumulativePerformanceReport now reads Portal tables only.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
