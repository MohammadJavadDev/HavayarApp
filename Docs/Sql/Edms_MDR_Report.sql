/*
================================================================================
  گزارش MDR — EdmsDocumentUploadService.GetMdrModel
  شامل: GerProgress, GetValidStatus, GetVendorValidStatus, GetInternalStatus,
        RemoveHolidays (روز کاری — بدون تعطیلات رسمی شمسی)

  نگاشت DocClass:  قدیم 309=IFA / 310=IFI  →  جدید ClassDocument 0=IFA / 1=IFI
  نگاشت POI:      قدیم DocPoi_ID 4,5       →  جدید GoalOfProduction 3=AFC / 4=AB
================================================================================
*/

SET NOCOUNT ON;

/* ---------- تابع شمارش روز کاری (معادل RemoveHolidays — بدون IsHolidayDay) ---------- */
IF OBJECT_ID(N'dbo.fn_EdmsBusinessDayCount', N'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_EdmsBusinessDayCount;
GO
CREATE FUNCTION dbo.fn_EdmsBusinessDayCount
(
    @FromDate     DATE,
    @ToDate       DATE,
    @IsPersian    BIT,  -- true: حذف پنج‌شنبه+چهارشنبه | false: حذف شنبه+یکشنبه
    @WedIsHoliday BIT = 1
)
RETURNS INT
AS
BEGIN
    IF @FromDate IS NULL OR @ToDate IS NULL RETURN 0;
    IF @FromDate = '0001-01-01' SET @FromDate = @ToDate;
    IF @FromDate > @ToDate
    BEGIN
        DECLARE @Swap DATE = @FromDate;
        SET @FromDate = @ToDate;
        SET @ToDate = @Swap;
    END;

    DECLARE @Cnt INT = 0;
    DECLARE @D DATE = @FromDate;

    WHILE @D <= @ToDate
    BEGIN
        DECLARE @Wd INT = ((DATEPART(WEEKDAY, @D) + @@DATEFIRST - 2) % 7); -- 0=Mon..6=Sun
        DECLARE @Keep BIT = 1;

        IF @IsPersian = 1
        BEGIN
            IF @Wd = 3 SET @Keep = 0; -- Thursday
            IF @WedIsHoliday = 1 AND @Wd = 2 SET @Keep = 0; -- Wednesday
            /* TODO: IsHolidayDay() — نیاز به جدول تعطیلات رسمی شمسی */
        END
        ELSE
        BEGIN
            IF @Wd IN (5, 6) SET @Keep = 0; -- Saturday, Sunday
        END;

        IF @Keep = 1 SET @Cnt += 1;
        SET @D = DATEADD(DAY, 1, @D);
    END;

    RETURN CASE WHEN @Cnt < 1 THEN 1 ELSE @Cnt END;
END;
GO

SET NOCOUNT ON;
DECLARE @Now DATETIME2 = SYSDATETIME();

/* Document_Status_Title — معادل Edms_Document_Status (در صورت تفاوت بعد از migration اصلاح کنید) */
DECLARE @StatusLabel TABLE (StatusId INT PRIMARY KEY, StatusLabel NVARCHAR(100));
INSERT INTO @StatusLabel (StatusId, StatusLabel) VALUES
 (1 , N'Not Issued'), (2 , N'Issue'), (3 , N'Reject'), (4 , N'Approve'),
 (5 , N'Commented'), (6 , N'Reject'), (7 , N'Approve'), (8 , N'Commented'),
 (9 , N'Approved By DCC'), (10, N'Reject By DCC'), (11, N'Commented By DCC'),
 (12, N'Reject By Employer'), (13, N'Approve By Employer'),
 (14, N'Approved As Note By Employer'), (15, N'Commented By Employer'),
 (16, N'Hold'), (17, N'UnHold'), (18, N'Not Review'), (19, N'UnHold By Employer'),
 (20, N'Send Email To Client'), (21, N'ReIssued'), (22, N'Commited'), (23, N'UnCommit'),
 (24, N'Approved By Sale'), (25, N'Commented By Sale'), (26, N'Rejected By Sale'),
 (27, N'AsBuild'), (28, N'Replay Sheet'), (29, N'Replay Sheet From Client'),
 (30, N'Issue For Client'), (31, N'Archived'), (32, N'Hold By Project'),
 (33, N'Convert To General Package'), (34, N'Commented Internal'),
 (35, N'Client Commented'), (36, N'Client Approval'), (37, N'Approval Internal'),
 (38, N'Review Issuer');

DECLARE @ClientStatusNormal TABLE (StatusId INT PRIMARY KEY);
INSERT INTO @ClientStatusNormal VALUES (13),(12),(15),(14);

DECLARE @ClientStatusVendor TABLE (StatusId INT PRIMARY KEY);
INSERT INTO @ClientStatusVendor VALUES (2),(10),(7),(11),(9),(21),(30),(13),(12),(15),(14);

/* client + NotReview + ReIssued — برای GetValidStatus (Hold/UnHold/ReIssued) */
DECLARE @ValidStatusClient TABLE (StatusId INT PRIMARY KEY);
INSERT INTO @ValidStatusClient VALUES (13),(12),(15),(14),(18),(21);

DECLARE @AcceptableStatusNormal TABLE (StatusId INT PRIMARY KEY);
INSERT INTO @AcceptableStatusNormal VALUES (12),(13),(14),(15),(18),(21),(28),(29),(16),(17);

/* ---------- VPIS پایه ---------- */
IF OBJECT_ID('tempdb..#VpisBase') IS NOT NULL DROP TABLE #VpisBase;

SELECT
    v.Id AS VpisId, v.Code AS DocumentNumber, v.Title AS DocumentTitle,
    v.Weight AS WeightFactor, v.ClassDocument AS DocClassEnum, v.Type AS DocTypeEnum,
    v.Displaying AS DisciplineEnum,
    v.FirstDegreeDocumentMiladiDate AS FirstIssueDate, v.ReplaceMiladiDate AS RePlanDate,
    v.ProducerId AS MainResponsibleId, v.ReviewerNames AS Responsible,
    v.ProducerId, v.ApproverId,
    p.Id AS ProjectId, p.Code AS ProjectCode, p.Name AS ProjectName,
    p.ProjectIsVendoriType AS IsVendorProject,
    ISNULL(p.LegalDelayHavayar, 0) AS HavayarDelayDay,
    ISNULL(p.LegalClientDayDelay, 0) AS ClientDelayDay,
    ou.Title AS BeneficiaryUnit
INTO #VpisBase
FROM Edms.ProjectVpis v
INNER JOIN Edms.Project p ON p.Id = v.ProjectNameId
LEFT JOIN Hrm.OrgUnit ou ON ou.Id = p.SubjectUnitId
WHERE ISNULL(v.IsActive, 1) <> 2 AND ISNULL(p.IsActive, 1) <> 2
  AND p.Status NOT IN (0, 2); -- قدیم: شروع‌نشده(3)/خاتمه‌یافته(5)

IF OBJECT_ID('tempdb..#Documents') IS NOT NULL DROP TABLE #Documents;
SELECT d.Id, d.DocumentVpisId, d.Revision, d.GoalOfProduction, d.HourPrePerson,
       d.PublicationMiladiDate, d.CreatedOnMiladiDateTime, d.CreatedOnShamsiDateTime,
       ROW_NUMBER() OVER (PARTITION BY d.DocumentVpisId ORDER BY d.Revision, d.Id) - 1 AS RevIndex,
       ROW_NUMBER() OVER (PARTITION BY d.DocumentVpisId ORDER BY d.Revision DESC, d.Id DESC) AS RevDescRn
INTO #Documents
FROM Edms.Document d
WHERE ISNULL(d.IsActive, 1) <> 2;

IF OBJECT_ID('tempdb..#Comments') IS NOT NULL DROP TABLE #Comments;
SELECT c.Id, c.DocumentId, d.DocumentVpisId, d.Revision, c.Status, c.Comment, c.LegalHolder,
       c.CreatedOnMiladiDateTime, c.CreatedOnShamsiDateTime
INTO #Comments
FROM Edms.DocumentComment c
INNER JOIN #Documents d ON d.Id = c.DocumentId
WHERE ISNULL(c.IsActive, 1) <> 2;

IF OBJECT_ID('tempdb..#Transmittals') IS NOT NULL DROP TABLE #Transmittals;
SELECT t.Id, t.DocumentId, d.DocumentVpisId, d.Revision, t.Number, t.IsForReplySheet, t.CreatedOnMiladiDateTime
INTO #Transmittals
FROM Edms.Transmital t
INNER JOIN #Documents d ON d.Id = t.DocumentId
WHERE ISNULL(t.IsActive, 1) <> 2;

IF OBJECT_ID('tempdb..#ProjectProgress') IS NOT NULL DROP TABLE #ProjectProgress;
SELECT ppp.ProjectId, ppp.Status, ppp.Percentage
INTO #ProjectProgress
FROM Edms.ProjectProgressPercentage ppp
WHERE ISNULL(ppp.IsActive, 1) <> 2;

/* ---------- GetValidStatus (normal) — per document revision ---------- */
IF OBJECT_ID('tempdb..#RevisionValidStatus') IS NOT NULL DROP TABLE #RevisionValidStatus;

;WITH RevComments AS (
    SELECT c.DocumentId, c.Id, c.Status,
           ROW_NUMBER() OVER (PARTITION BY c.DocumentId ORDER BY c.Id) AS RnAsc,
           ROW_NUMBER() OVER (PARTITION BY c.DocumentId ORDER BY c.Id DESC) AS RnDesc,
           COUNT(*) OVER (PARTITION BY c.DocumentId) AS CntAll
    FROM #Comments c
    WHERE c.Status IN (SELECT StatusId FROM @AcceptableStatusNormal)
),
LastValid AS (
    SELECT rc.DocumentId, rc.Status AS LastStatus, rc.CntAll,
           (SELECT TOP 1 c2.Status FROM #Comments c2
            WHERE c2.DocumentId = rc.DocumentId
              AND c2.Status IN (SELECT StatusId FROM @AcceptableStatusNormal)
              AND c2.Status NOT IN (28, 29)
            ORDER BY c2.Id DESC) AS LastNonReplayStatus
    FROM RevComments rc
    WHERE rc.RnDesc = 1
),
LastClientInRev AS (
    SELECT c.DocumentId,
           (SELECT TOP 1 sl.StatusLabel FROM #Comments c2
            INNER JOIN @StatusLabel sl ON sl.StatusId = c2.Status
            WHERE c2.DocumentId = c.DocumentId
              AND c2.Status IN (SELECT StatusId FROM @ValidStatusClient)
            ORDER BY c2.Id DESC) AS LastClientTitle
    FROM #Comments c
    GROUP BY c.DocumentId
)
SELECT
    lv.DocumentId,
    CASE
        WHEN lv.LastNonReplayStatus IS NULL THEN N'Not Issued'
        WHEN lv.LastNonReplayStatus IN (12,13,14,15,18)
            THEN (SELECT StatusLabel FROM @StatusLabel WHERE StatusId = lv.LastNonReplayStatus)
        WHEN lv.LastNonReplayStatus = 16 THEN
            CASE WHEN lv.CntAll <= 1 THEN N'Not Issued' ELSE lc.LastClientTitle END
        WHEN lv.LastNonReplayStatus = 17 THEN
            CASE WHEN lv.CntAll <= 2 THEN N'Not Issued' ELSE lc.LastClientTitle END
        WHEN lv.LastNonReplayStatus = 21 THEN
            CASE WHEN lv.CntAll <= 2 THEN N'Re Issued' ELSE lc.LastClientTitle END
        ELSE N'Not Issued'
    END AS ValidStatus
INTO #RevisionValidStatus
FROM LastValid lv
LEFT JOIN LastClientInRev lc ON lc.DocumentId = lv.DocumentId;

/* ---------- GetVendorValidStatus — per document revision ---------- */
IF OBJECT_ID('tempdb..#RevisionVendorStatus') IS NOT NULL DROP TABLE #RevisionVendorStatus;

;WITH Vc AS (
    SELECT c.DocumentId, c.Id, c.Status,
           ROW_NUMBER() OVER (PARTITION BY c.DocumentId ORDER BY c.Id) AS RnAsc,
           ROW_NUMBER() OVER (PARTITION BY c.DocumentId ORDER BY c.Id DESC) AS RnDesc,
           COUNT(*) OVER (PARTITION BY c.DocumentId) AS Cnt
    FROM #Comments c
    WHERE c.Status IN (2,7,9,11,16,17,21,28,30,12,13,14,15,10)
)
SELECT
    v.DocumentId,
    CASE
        WHEN v.RnDesc <> 1 THEN NULL
        WHEN v.Status IS NULL THEN N'Not Issued'
        WHEN v.Status IN (2, 7, 28) THEN N'Not Review'  /* Issue, Approve, ReplaySheet */
        WHEN v.Status = 11 THEN (SELECT StatusLabel FROM @StatusLabel WHERE StatusId = 11) /* Commented/CommentedByDcc */
        WHEN v.Status = 9 THEN
            CASE WHEN v.Cnt > 1
                 THEN (SELECT sl.StatusLabel FROM Vc v2 INNER JOIN @StatusLabel sl ON sl.StatusId = v2.Status
                       WHERE v2.DocumentId = v.DocumentId AND v2.RnDesc = 2)
                 ELSE (SELECT StatusLabel FROM @StatusLabel WHERE StatusId = v.Status)
            END
        WHEN v.Status = 16 THEN
            CASE WHEN v.Cnt > 1
                 THEN (SELECT sl.StatusLabel FROM Vc v2 INNER JOIN @StatusLabel sl ON sl.StatusId = v2.Status
                       WHERE v2.DocumentId = v.DocumentId AND v2.RnDesc = 2)
                 ELSE N'Not Issued' END
        WHEN v.Status = 17 THEN
            CASE WHEN v.Cnt > 2
                 THEN (SELECT sl.StatusLabel FROM Vc v2 INNER JOIN @StatusLabel sl ON sl.StatusId = v2.Status
                       WHERE v2.DocumentId = v.DocumentId AND v2.RnDesc = 3)
                 ELSE N'Not Issued' END
        WHEN v.Status IN (30,12,13,14,15,21)
            THEN (SELECT StatusLabel FROM @StatusLabel WHERE StatusId = v.Status)
        ELSE N'Not Valid'
    END AS VendorStatus
INTO #RevisionVendorStatus
FROM Vc v
WHERE v.RnDesc = 1;

/* FinalStatus per VPIS = آخرین ValidStatus در chain revisions (GetRevisions) */
IF OBJECT_ID('tempdb..#VpisFinalStatus') IS NOT NULL DROP TABLE #VpisFinalStatus;
SELECT vb.VpisId,
       COALESCE(
           (SELECT TOP 1 CASE WHEN vb.IsVendorProject = 1 THEN rvs.VendorStatus ELSE rvs2.ValidStatus END
            FROM #Documents d
            LEFT JOIN #RevisionValidStatus rvs2 ON rvs2.DocumentId = d.Id
            LEFT JOIN #RevisionVendorStatus rvs ON rvs.DocumentId = d.Id
            WHERE d.DocumentVpisId = vb.VpisId
            ORDER BY d.Revision DESC, d.Id DESC),
           N'Not Issued') AS FinalStatus
INTO #VpisFinalStatus
FROM #VpisBase vb;

/* ---------- GetInternalStatus — آخرین کامنت VPIS ---------- */
IF OBJECT_ID('tempdb..#VpisInternalStatus') IS NOT NULL DROP TABLE #VpisInternalStatus;
SELECT x.DocumentVpisId,
    CASE
        WHEN x.Status IS NULL THEN N'Not Issued'
        WHEN x.Status = 2  THEN N'Not Review'
        WHEN x.Status IN (16,17,11,14,10,3,6) /* Hold,UnHold,Commented*,Reject* */
            THEN (SELECT StatusLabel FROM @StatusLabel WHERE StatusId = x.Status)
        WHEN x.Status IN (9,18) THEN N'Issued'
        ELSE N''
    END AS InternalStatus
INTO #VpisInternalStatus
FROM (
    SELECT c.DocumentVpisId, c.Status,
           ROW_NUMBER() OVER (PARTITION BY c.DocumentVpisId ORDER BY c.Id DESC) AS Rn
    FROM #Comments c
) x WHERE x.Rn = 1;

/* ---------- GerProgress ---------- */
IF OBJECT_ID('tempdb..#VpisIssueDate') IS NOT NULL DROP TABLE #VpisIssueDate;
SELECT vb.VpisId,
    CASE WHEN vb.IsVendorProject = 1
         THEN (SELECT MAX(d.CreatedOnMiladiDateTime) FROM #Documents d WHERE d.DocumentVpisId = vb.VpisId)
         ELSE (SELECT TOP 1 t.CreatedOnMiladiDateTime
               FROM #Documents d INNER JOIN #Transmittals t ON t.DocumentId = d.Id
               WHERE d.DocumentVpisId = vb.VpisId
               ORDER BY d.Revision DESC, t.Id DESC)
    END AS IssueDate
INTO #VpisIssueDate;

IF OBJECT_ID('tempdb..#ProgressComments') IS NOT NULL DROP TABLE #ProgressComments;
SELECT vb.VpisId, c.Id, c.Status,
       ROW_NUMBER() OVER (PARTITION BY vb.VpisId ORDER BY c.Id) AS SeqAsc,
       ROW_NUMBER() OVER (PARTITION BY vb.VpisId ORDER BY c.Id DESC) AS SeqDesc,
       COUNT(*) OVER (PARTITION BY vb.VpisId) AS Cnt
INTO #ProgressComments
FROM #VpisBase vb
INNER JOIN #Comments c ON c.DocumentVpisId = vb.VpisId
WHERE (vb.IsVendorProject = 0 AND (c.Status IN (SELECT StatusId FROM @ClientStatusNormal) OR c.Status = 18))
   OR (vb.IsVendorProject = 1 AND (c.Status IN (SELECT StatusId FROM @ClientStatusVendor) OR c.Status = 18));

IF OBJECT_ID('tempdb..#GerProgress') IS NOT NULL DROP TABLE #GerProgress;
SELECT
    vb.VpisId,
    CASE
    CASE
        WHEN vid.IssueDate IS NULL OR vid.IssueDate <= '1900-01-01' THEN 0
        WHEN EXISTS (SELECT 1 FROM #ProjectProgress pp WHERE pp.ProjectId = vb.ProjectId AND pp.Status = 21)
             AND lri.LastPoiId IN (3, 4) /* قدیم DocPoi 4,5 → AFC=3 AB=4 */
        THEN (SELECT TOP 1 pp.Percentage FROM #ProjectProgress pp
              WHERE pp.ProjectId = vb.ProjectId AND pp.Status = 21)
        WHEN vb.DocClassEnum = 1 THEN 100 /* IFI — قدیم 310 */
        WHEN vb.DocClassEnum = 0 THEN /* IFA — قدیم 309 */
            CASE
                WHEN pc_last.Status = 18 AND pc_last.Cnt > 1
                     AND pc_prev.Status IN (15, 14)
                THEN COALESCE((SELECT TOP 1 pp.Percentage FROM #ProjectProgress pp
                               WHERE pp.ProjectId = vb.ProjectId AND pp.Status = pc_prev.Status), 0)
                ELSE COALESCE((
                    SELECT TOP 1 pp.Percentage
                    FROM #ProgressComments pcf
                    INNER JOIN #ProjectProgress pp ON pp.ProjectId = vb.ProjectId AND pp.Status = pcf.Status
                    WHERE pcf.VpisId = vb.VpisId
                      AND (pcf.Status IN (SELECT StatusId FROM @ClientStatusNormal) OR pcf.Status = 18)
                    ORDER BY pcf.Id DESC
                ), 0)
            END
        ELSE 0
    END AS Progress
INTO #GerProgress
FROM #VpisBase vb
LEFT JOIN #VpisIssueDate vid ON vid.VpisId = vb.VpisId
LEFT JOIN (
    SELECT d.DocumentVpisId, d.GoalOfProduction AS LastPoiId,
           ROW_NUMBER() OVER (PARTITION BY d.DocumentVpisId ORDER BY d.Revision DESC, d.Id DESC) AS Rn
    FROM #Documents d
) lri ON lri.DocumentVpisId = vb.VpisId AND lri.Rn = 1
LEFT JOIN #ProgressComments pc_last ON pc_last.VpisId = vb.VpisId AND pc_last.SeqDesc = 1
LEFT JOIN #ProgressComments pc_prev ON pc_prev.VpisId = vb.VpisId AND pc_prev.SeqDesc = 2;

/* fallback: Not Review + revision قبلی progress بالاتر */
IF OBJECT_ID('tempdb..#GerProgressFinal') IS NOT NULL DROP TABLE #GerProgressFinal;
SELECT vb.VpisId,
    CASE
        WHEN (SELECT COUNT(*) FROM #Documents d WHERE d.DocumentVpisId = vb.VpisId) > 1
             AND vfs.FinalStatus = N'Not Review'
             AND gp_alt.Progress > gp.Progress
        THEN gp_alt.Progress
        ELSE gp.Progress
    END AS Progress
INTO #GerProgressFinal
FROM #VpisBase vb
INNER JOIN #GerProgress gp ON gp.VpisId = vb.VpisId
INNER JOIN #VpisFinalStatus vfs ON vfs.VpisId = vb.VpisId
OUTER APPLY (
    SELECT CASE
    CASE
        WHEN vid.IssueDate IS NULL OR vid.IssueDate <= '1900-01-01' THEN 0
        WHEN vb.DocClassEnum = 1 THEN 100
        WHEN vb.DocClassEnum = 0 THEN
            COALESCE((
                SELECT TOP 1 pp.Percentage
                FROM #Comments c
                INNER JOIN #Documents d ON d.Id = c.DocumentId
                INNER JOIN #Documents d_last ON d_last.DocumentVpisId = vb.VpisId AND d_last.RevDescRn = 1
                INNER JOIN #ProjectProgress pp ON pp.ProjectId = vb.ProjectId AND pp.Status = c.Status
                WHERE c.DocumentVpisId = vb.VpisId
                  AND d.Revision < d_last.Revision
                  AND (c.Status IN (SELECT StatusId FROM @ClientStatusNormal) OR c.Status = 18)
                ORDER BY c.Id DESC
            ), 0)
        ELSE 0 END AS Progress
    FROM #VpisIssueDate vid
    WHERE vid.VpisId = vb.VpisId
) gp_alt;

/* ======================== Result Set 1: MDR Header ======================== */
;WITH
LastVpisComment AS (
    SELECT * FROM (
        SELECT c.*, ROW_NUMBER() OVER (PARTITION BY c.DocumentVpisId ORDER BY c.Id DESC) AS Rn
        FROM #Comments c
    ) x WHERE x.Rn = 1
),
LastIssuedTransmittal AS (
    SELECT * FROM (
        SELECT d.DocumentVpisId, t.Number AS HavayarLastSendTransmittal, t.CreatedOnMiladiDateTime AS HavayarLastSendDate,
               ROW_NUMBER() OVER (PARTITION BY d.DocumentVpisId ORDER BY d.Revision DESC, t.Id DESC) AS Rn
        FROM #Documents d INNER JOIN #Transmittals t ON t.DocumentId = d.Id
    ) x WHERE x.Rn = 1
),
HavayarRds AS (
    SELECT * FROM (
        SELECT c.DocumentVpisId,
               CONVERT(VARCHAR(10), c.CreatedOnMiladiDateTime, 23) AS HavayarRdsDate,
               (SELECT TOP 1 t.Number FROM #Transmittals t
                WHERE t.DocumentId = c.DocumentId AND t.CreatedOnMiladiDateTime > c.CreatedOnMiladiDateTime
                ORDER BY t.Id DESC) AS HavayarRdsNo,
               ROW_NUMBER() OVER (PARTITION BY c.DocumentVpisId ORDER BY c.Id DESC) AS Rn
        FROM #Comments c WHERE c.Status = 28
    ) x WHERE x.Rn = 1
),
ClientRds AS (
    SELECT * FROM (
        SELECT c.DocumentVpisId,
               CONVERT(VARCHAR(10), c.CreatedOnMiladiDateTime, 23) AS ClientLastRepliedRdsDate,
               CAST(NULL AS NVARCHAR(200)) AS ClientLastRepliedRdsNo, /* TODO: ReceivedTransmittalNumber */
               ROW_NUMBER() OVER (PARTITION BY c.DocumentVpisId ORDER BY c.Id DESC) AS Rn
        FROM #Comments c WHERE c.Status = 29
    ) x WHERE x.Rn = 1
),
ClientComments AS (
    SELECT vb.VpisId, c.Id, c.Status, c.CreatedOnMiladiDateTime,
           ROW_NUMBER() OVER (PARTITION BY vb.VpisId ORDER BY c.Id DESC) AS RnAll,
           ROW_NUMBER() OVER (PARTITION BY vb.VpisId ORDER BY CASE WHEN c.Status <> 18 THEN c.Id ELSE 0 END DESC) AS RnClientOnly
    FROM #VpisBase vb INNER JOIN #Comments c ON c.DocumentVpisId = vb.VpisId
    WHERE (vb.IsVendorProject = 0 AND (c.Status IN (SELECT StatusId FROM @ClientStatusNormal) OR c.Status = 18))
       OR (vb.IsVendorProject = 1 AND (c.Status IN (SELECT StatusId FROM @ClientStatusVendor) OR c.Status = 18))
),
LastClientFlow AS (
    SELECT cc.VpisId,
           MAX(CASE WHEN cc.RnAll = 1 THEN cc.Status END) AS LastFlowStatus,
           MAX(CASE WHEN cc.RnClientOnly = 1 AND cc.Status <> 18 THEN cc.CreatedOnMiladiDateTime END) AS ClientLastSentDate
    FROM ClientComments cc GROUP BY cc.VpisId
),
LastRevisionInfo AS (
    SELECT * FROM (
        SELECT d.DocumentVpisId, CAST(d.Revision AS VARCHAR(10)) AS LastRev,
               CASE d.GoalOfProduction WHEN 0 THEN 'IFR' WHEN 1 THEN 'IFA' WHEN 2 THEN 'IFI'
                    WHEN 3 THEN 'AFC' WHEN 4 THEN 'AB' ELSE NULL END AS LastPoi,
               ROW_NUMBER() OVER (PARTITION BY d.DocumentVpisId ORDER BY d.Revision DESC, d.Id DESC) AS Rn
        FROM #Documents d
    ) x WHERE x.Rn = 1
),
UserEn AS (
    SELECT u.Id,
           COALESCE(NULLIF(p.FullNameInEnglish, N''),
                    NULLIF(LTRIM(RTRIM(ISNULL(p.FirstNameInEnglish,N'')+N' '+ISNULL(p.LastNameInEnglish,N''))), N''),
                    u.Name) AS FullNameEn
    FROM system.[User] u LEFT JOIN Gnr.Party p ON p.Id = u.PartyId
)
SELECT
    vb.ProjectCode, vb.ProjectName, vb.BeneficiaryUnit,
    vb.DocumentNumber, vb.DocumentTitle,
    CASE vb.DisciplineEnum WHEN 0 THEN 'GN' ELSE CAST(vb.DisciplineEnum AS VARCHAR(10)) END AS Disipline,
    vb.MainResponsibleId, vb.Responsible,
    checker.FullNameEn AS Checker,
    approver.FullNameEn AS Approver,
    CAST(ISNULL(gpf.Progress,0) * ISNULL(vb.WeightFactor,0) / 100.0 AS DECIMAL(18,4)) AS OverallProgress,
    vb.WeightFactor,
    ISNULL(gpf.Progress, 0) AS Progress,
    CASE vb.DocClassEnum WHEN 0 THEN N'IFA' WHEN 1 THEN N'IFI' END AS DocClass,
    CASE vb.DocTypeEnum
        WHEN 0 THEN 'SCH' WHEN 1 THEN 'RPT' WHEN 2 THEN 'CHR' WHEN 3 THEN 'LST' WHEN 4 THEN 'DWG'
        WHEN 5 THEN 'DGM' WHEN 6 THEN 'DSN' WHEN 7 THEN 'PHL' WHEN 8 THEN 'CAL' WHEN 9 THEN 'DSH'
        WHEN 10 THEN 'BOM' WHEN 11 THEN 'PRC' WHEN 12 THEN 'ANL' WHEN 13 THEN 'SPC' WHEN 14 THEN 'CRT'
        WHEN 15 THEN 'MAN' END AS DocType,
    CASE WHEN d0.HourPrePerson IS NOT NULL
         THEN CONCAT(d0.HourPrePerson / 60, ':', RIGHT('0'+CAST(d0.HourPrePerson % 60 AS VARCHAR(2)), 2))
         END AS ConsumedManHoursInText,
    CONVERT(VARCHAR(10), vb.FirstIssueDate, 23) AS FirstIssueDate,
    CONVERT(VARCHAR(10), vb.RePlanDate, 23) AS RePlanDate,
    CASE WHEN lvc.Status = 16 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HoldDocuments,
    CASE WHEN lvc.Status = 16 THEN lvc.Comment END AS DescriptionForHoldDocuments,
    CASE WHEN lvc.Status = 16 THEN
        CASE lvc.LegalHolder
            WHEN 0 THEN N'مهندسی' WHEN 1 THEN N'پروژه' WHEN 2 THEN N'کارفرما' WHEN 3 THEN N'تدارکات'
            WHEN 4 THEN N'فروش' WHEN 5 THEN N'مدیریت' WHEN 6 THEN N'تولید' WHEN 7 THEN N'کنترل کیفیت'
            WHEN 8 THEN N'بازرگانی خارجی' WHEN 9 THEN N'خدمات' WHEN 10 THEN N'پیمانکار' END
    END AS HoldResponsible,
    lri.LastRev,
    CONVERT(VARCHAR(10), lit.HavayarLastSendDate, 23) AS HavayarLastSendDate,
    CASE WHEN vb.IsVendorProject = 0 THEN lit.HavayarLastSendTransmittal END AS HavayarLastSendTransmittal,
    hr.HavayarRdsDate, hr.HavayarRdsNo, lri.LastPoi,
    CONVERT(VARCHAR(10), lcf.ClientLastSentDate, 23) AS ClientLastSentDate,
    CASE WHEN vb.IsVendorProject = 0 THEN CAST(NULL AS NVARCHAR(200)) END AS ClientLastRepliedTransmittal,
    cr.ClientLastRepliedRdsDate, cr.ClientLastRepliedRdsNo,
    CASE
        WHEN lcf.LastFlowStatus = 13 THEN NULL
        WHEN lcf.LastFlowStatus = 18 OR NOT EXISTS (
            SELECT 1 FROM ClientComments cc WHERE cc.VpisId = vb.VpisId AND cc.Status IN (12,15,14))
        THEN NULL
        ELSE CAST(dbo.fn_EdmsBusinessDayCount(CAST(lcf.ClientLastSentDate AS DATE), CAST(@Now AS DATE), 1, 1) AS VARCHAR(20))
    END AS HavayarRepliedTime,
    CASE
        WHEN lcf.LastFlowStatus = 13 THEN NULL
        WHEN lcf.LastFlowStatus = 18 OR NOT EXISTS (
            SELECT 1 FROM ClientComments cc WHERE cc.VpisId = vb.VpisId AND cc.Status IN (12,15,14))
        THEN CAST(dbo.fn_EdmsBusinessDayCount(
            CAST(COALESCE(lit.HavayarLastSendDate, vid.IssueDate) AS DATE), CAST(@Now AS DATE),
            CASE WHEN vb.IsVendorProject = 1 THEN 0 ELSE 1 END, 1) AS VARCHAR(20))
        ELSE NULL
    END AS ClientRepliedTime,
    vfs.FinalStatus,
    vis.InternalStatus,
    vb.IsVendorProject
FROM #VpisBase vb
LEFT JOIN UserEn checker ON checker.Id = vb.ProducerId
LEFT JOIN UserEn approver ON approver.Id = vb.ApproverId
LEFT JOIN #Documents d0 ON d0.DocumentVpisId = vb.VpisId AND d0.RevIndex = 0
LEFT JOIN LastVpisComment lvc ON lvc.DocumentVpisId = vb.VpisId
LEFT JOIN LastIssuedTransmittal lit ON lit.DocumentVpisId = vb.VpisId
LEFT JOIN HavayarRds hr ON hr.DocumentVpisId = vb.VpisId
LEFT JOIN ClientRds cr ON cr.DocumentVpisId = vb.VpisId
LEFT JOIN LastRevisionInfo lri ON lri.DocumentVpisId = vb.VpisId
LEFT JOIN LastClientFlow lcf ON lcf.VpisId = vb.VpisId
LEFT JOIN #GerProgressFinal gpf ON gpf.VpisId = vb.VpisId
LEFT JOIN #VpisFinalStatus vfs ON vfs.VpisId = vb.VpisId
LEFT JOIN #VpisInternalStatus vis ON vis.DocumentVpisId = vb.VpisId
LEFT JOIN #VpisIssueDate vid ON vid.VpisId = vb.VpisId
ORDER BY vb.ProjectCode, vb.DocumentNumber;

/* ======================== Result Set 2: Revision Detail ======================== */
SELECT
    vb.ProjectCode, vb.DocumentNumber, d.RevIndex,
    CAST(d.Revision AS VARCHAR(10)) AS RevNo,
    CASE WHEN vb.IsVendorProject = 1 THEN CONVERT(VARCHAR(10), d.CreatedOnMiladiDateTime, 23)
         ELSE CONVERT(VARCHAR(10), t_issue.CreatedOnMiladiDateTime, 23) END AS HavayarSendingDate,
    CASE WHEN vb.IsVendorProject = 0 THEN t_issue.Number END AS HavayarSendingTransmittal,
    CONVERT(VARCHAR(10), c_rs.CreatedOnMiladiDateTime, 23) AS HavayarRdsDate,
    t_rds.Number AS HavayarRdsNo,
    CASE d.GoalOfProduction WHEN 0 THEN 'IFR' WHEN 1 THEN 'IFA' WHEN 2 THEN 'IFI'
         WHEN 3 THEN 'AFC' WHEN 4 THEN 'AB' END AS Poi,
    CAST(dbo.fn_EdmsBusinessDayCount(
        CAST(COALESCE(t_issue.CreatedOnMiladiDateTime, d.CreatedOnMiladiDateTime) AS DATE),
        CAST(c_client.CreatedOnMiladiDateTime AS DATE),
        CASE WHEN vb.IsVendorProject = 1 THEN 0 ELSE 1 END, 1) AS VARCHAR(20)) AS ClientTime,
    CONVERT(VARCHAR(10), c_client.CreatedOnMiladiDateTime, 23) AS ClientReplyDate,
    CAST(NULL AS NVARCHAR(200)) AS ClientReplyTransmittal,
    CONVERT(VARCHAR(10), c_rsc.CreatedOnMiladiDateTime, 23) AS ClientRdsDate,
    CAST(NULL AS NVARCHAR(200)) AS ClientRdsNo,
    CAST(CASE WHEN d.Revision = 0 THEN
            dbo.fn_EdmsBusinessDayCount(CAST(vb.FirstIssueDate AS DATE),
                CAST(COALESCE(t_issue.CreatedOnMiladiDateTime, d.CreatedOnMiladiDateTime) AS DATE),
                CASE WHEN vb.IsVendorProject = 1 THEN 0 ELSE 1 END, 1)
         ELSE
            CASE WHEN dbo.fn_EdmsBusinessDayCount(
                    CAST(COALESCE(c_before.CreatedOnMiladiDateTime, c_client.CreatedOnMiladiDateTime) AS DATE),
                    CAST(COALESCE(t_issue.CreatedOnMiladiDateTime, d.CreatedOnMiladiDateTime) AS DATE),
                    CASE WHEN vb.IsVendorProject = 1 THEN 0 ELSE 1 END, 1) - vb.HavayarDelayDay < 0 THEN 0
                 ELSE dbo.fn_EdmsBusinessDayCount(
                    CAST(COALESCE(c_before.CreatedOnMiladiDateTime, c_client.CreatedOnMiladiDateTime) AS DATE),
                    CAST(COALESCE(t_issue.CreatedOnMiladiDateTime, d.CreatedOnMiladiDateTime) AS DATE),
                    CASE WHEN vb.IsVendorProject = 1 THEN 0 ELSE 1 END, 1) - vb.HavayarDelayDay
            END
         END AS VARCHAR(20)) AS HavayarDelay,
    CAST(CASE WHEN dbo.fn_EdmsBusinessDayCount(
            CAST(COALESCE(t_issue.CreatedOnMiladiDateTime, d.CreatedOnMiladiDateTime) AS DATE),
            CAST(c_client.CreatedOnMiladiDateTime AS DATE),
            CASE WHEN vb.IsVendorProject = 1 THEN 0 ELSE 1 END, 1) - vb.ClientDelayDay < 0 THEN 0
         ELSE dbo.fn_EdmsBusinessDayCount(
            CAST(COALESCE(t_issue.CreatedOnMiladiDateTime, d.CreatedOnMiladiDateTime) AS DATE),
            CAST(c_client.CreatedOnMiladiDateTime AS DATE),
            CASE WHEN vb.IsVendorProject = 1 THEN 0 ELSE 1 END, 1) - vb.ClientDelayDay
         END AS VARCHAR(20)) AS ClientDelay,
    CASE WHEN vb.IsVendorProject = 1 THEN rvs.VendorStatus ELSE rvs2.ValidStatus END AS [Status]
FROM #VpisBase vb
INNER JOIN #Documents d ON d.DocumentVpisId = vb.VpisId
OUTER APPLY (SELECT TOP 1 t.* FROM #Transmittals t WHERE t.DocumentId = d.Id ORDER BY t.Id DESC) t_issue
OUTER APPLY (SELECT TOP 1 c.* FROM #Comments c WHERE c.DocumentId = d.Id AND c.Status = 28 ORDER BY c.Id DESC) c_rs
OUTER APPLY (SELECT TOP 1 t.Number, t.CreatedOnMiladiDateTime FROM #Transmittals t
             WHERE t.DocumentId = d.Id AND t.CreatedOnMiladiDateTime > c_rs.CreatedOnMiladiDateTime ORDER BY t.Id DESC) t_rds
OUTER APPLY (
    SELECT TOP 1 c.*
    FROM #Comments c
    WHERE c.DocumentId = d.Id
      AND ((vb.IsVendorProject = 0 AND c.Status IN (SELECT StatusId FROM @ClientStatusNormal))
        OR (vb.IsVendorProject = 1 AND c.Status IN (SELECT StatusId FROM @ClientStatusVendor)
            AND (vb.IsVendorProject = 0 OR c.Status <> 2)))
    ORDER BY c.Id DESC
) c_client
OUTER APPLY (
    SELECT TOP 1 c.CreatedOnMiladiDateTime
    FROM #Comments c
    INNER JOIN #Documents d2 ON d2.Id = c.DocumentId
    WHERE d2.DocumentVpisId = vb.VpisId AND d2.Revision <= d.Revision
      AND ((vb.IsVendorProject = 0 AND c.Status IN (SELECT StatusId FROM @ClientStatusNormal))
        OR (vb.IsVendorProject = 1 AND c.Status IN (SELECT StatusId FROM @ClientStatusVendor)))
      AND c.Id <> c_client.Id
    ORDER BY c.Id DESC
) c_before
OUTER APPLY (SELECT TOP 1 c.* FROM #Comments c WHERE c.DocumentId = d.Id AND c.Status = 29 ORDER BY c.Id DESC) c_rsc
LEFT JOIN #RevisionValidStatus rvs2 ON rvs2.DocumentId = d.Id
LEFT JOIN #RevisionVendorStatus rvs ON rvs.DocumentId = d.Id
ORDER BY vb.ProjectCode, vb.DocumentNumber, d.RevIndex;

DROP TABLE IF EXISTS #VpisBase, #Documents, #Comments, #Transmittals, #ProjectProgress,
    #RevisionValidStatus, #RevisionVendorStatus, #VpisFinalStatus, #VpisInternalStatus,
    #VpisIssueDate, #ProgressComments, #GerProgress, #GerProgressFinal;
