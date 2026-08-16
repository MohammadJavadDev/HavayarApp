/*
================================================================================
  داشبورد مهندسی MDR — کوئری منبع داده (Stimulsoft SavedQuery, حالت نوشتن Query)
  سه Result Set به‌ترتیب:
    1) MdrDocumentHeaders    — یک ردیف به‌ازای هر VPIS/مدرک (وضعیت فعلی + تاخیرات)
    2) MdrDocumentIssuances  — یک ردیف به‌ازای هر revision که واقعاً ارسال شده
    3) MdrTransmittals       — یک ردیف به‌ازای هر رکورد Edms.Transmital

  منطق پایه (وضعیت‌ها، fn_EdmsBusinessDayCount، روز کاری) عیناً از
  Docs/Sql/Edms_MDR_Report.sql به ارث رسیده است. آستانه‌های ۷ روز (هوایار) و
  ۱۰ روز (کارفرما) کاری، ثابت و مخصوص همین داشبورد هستند (مستقل از
  Project.LegalDelayHavayar / Project.LegalClientDayDelay).

  نکته اجرا: بخش ساخت تابع (فقط یک‌بار لازم است) باید در یک Batch جدا اجرا شود
  (به همین دلیل با GO از بقیه اسکریپت جدا شده است). متن سه SELECT پایانی همان
  چیزی است که در QueryJson.CustomQuery ذخیره می‌شود (به همراه تعریف
  متغیرهای جدولی و Temp Table های موردنیاز، بدون بخش ساخت تابع، چون تابع از
  قبل در دیتابیس موجود است).
================================================================================
*/

/* ---------- تابع شمارش روز کاری (مشترک با Edms_MDR_Report.sql) ---------- */
CREATE OR ALTER FUNCTION dbo.fn_EdmsBusinessDayCount
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

/*
================================================================================
  از این‌جا به بعد: متن دقیقی که باید در system.SavedQuery.QueryJson.CustomQuery
  (به همراه سه آیتم Selects: MdrDocumentHeaders / MdrDocumentIssuances /
  MdrTransmittals) ذخیره شود. تابع fn_EdmsBusinessDayCount در بالا از قبل
  ساخته شده است.
================================================================================
*/

SET NOCOUNT ON;
DECLARE @Now DATETIME2 = SYSDATETIME();

DECLARE @ClientStatusNormal TABLE (StatusId INT PRIMARY KEY);
INSERT INTO @ClientStatusNormal VALUES (13),(12),(15),(14);

DECLARE @ClientStatusVendor TABLE (StatusId INT PRIMARY KEY);
INSERT INTO @ClientStatusVendor VALUES (2),(10),(7),(11),(9),(21),(30),(13),(12),(15),(14);

DECLARE @ValidStatusClient TABLE (StatusId INT PRIMARY KEY);
INSERT INTO @ValidStatusClient VALUES (13),(12),(15),(14),(18),(21);

DECLARE @AcceptableStatusNormal TABLE (StatusId INT PRIMARY KEY);
INSERT INTO @AcceptableStatusNormal VALUES (12),(13),(14),(15),(18),(21),(28),(29),(16),(17);

/* ---------- VPIS پایه (پروژه‌های شروع‌نشده/خاتمه‌یافته حذف می‌شوند - مطابق Edms_MDR_Report.sql) ---------- */
IF OBJECT_ID('tempdb..#VpisBase') IS NOT NULL DROP TABLE #VpisBase;
SELECT
    v.Id AS VpisId, v.Code AS DocumentNumber, v.Title AS DocumentTitle,
    v.FirstDegreeDocumentMiladiDate AS FirstIssueDate, v.ReplaceMiladiDate AS RePlanDate,
    p.Id AS ProjectId, p.Code AS ProjectCode, p.Name AS ProjectName,
    p.ProjectIsVendoriType AS IsVendorProject,
    CASE WHEN p.ProjectIsVendoriType = 1 THEN N'VP' ELSE N'SP' END AS VendorTypeLabel
INTO #VpisBase
FROM Edms.ProjectVpis v
INNER JOIN Edms.Project p ON p.Id = v.ProjectNameId
WHERE ISNULL(v.IsActive, 1) <> 2 AND ISNULL(p.IsActive, 1) <> 2
  AND p.Status NOT IN (0, 2); -- StartNotStarted(0) / Completed(2)

IF OBJECT_ID('tempdb..#Documents') IS NOT NULL DROP TABLE #Documents;
SELECT d.Id, d.DocumentVpisId, d.Revision,
       d.CreatedOnMiladiDateTime, d.CreatedOnShamsiDateTime,
       ROW_NUMBER() OVER (PARTITION BY d.DocumentVpisId ORDER BY d.Revision DESC, d.Id DESC) AS RevDescRn
INTO #Documents
FROM Edms.Document d
WHERE ISNULL(d.IsActive, 1) <> 2;

IF OBJECT_ID('tempdb..#Comments') IS NOT NULL DROP TABLE #Comments;
SELECT c.Id, c.DocumentId, d.DocumentVpisId, d.Revision, c.Status, c.CreatedOnMiladiDateTime
INTO #Comments
FROM Edms.DocumentComment c
INNER JOIN #Documents d ON d.Id = c.DocumentId
WHERE ISNULL(c.IsActive, 1) <> 2;

IF OBJECT_ID('tempdb..#Transmittals') IS NOT NULL DROP TABLE #Transmittals;
SELECT t.Id, t.ProjectId, t.DocumentId, d.DocumentVpisId, d.Revision, t.Number,
       t.CreatedOnMiladiDateTime, t.CreatedOnShamsiDateTime
INTO #Transmittals
FROM Edms.Transmital t
INNER JOIN #Documents d ON d.Id = t.DocumentId
WHERE ISNULL(t.IsActive, 1) <> 2;

/* ---------- GetValidStatus (normal) — per document revision ---------- */
IF OBJECT_ID('tempdb..#RevisionValidStatus') IS NOT NULL DROP TABLE #RevisionValidStatus;
;WITH RevComments AS (
    SELECT c.DocumentId, c.Id, c.Status,
           ROW_NUMBER() OVER (PARTITION BY c.DocumentId ORDER BY c.Id DESC) AS RnDesc,
           COUNT(*) OVER (PARTITION BY c.DocumentId) AS CntAll
    FROM #Comments c
    WHERE c.Status IN (SELECT StatusId FROM @AcceptableStatusNormal)
),
LastValid AS (
    SELECT rc.DocumentId, rc.CntAll,
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
            CROSS APPLY (SELECT CASE c2.Status
                WHEN 12 THEN N'Reject By Employer' WHEN 13 THEN N'Approve By Employer'
                WHEN 14 THEN N'Approved As Note By Employer' WHEN 15 THEN N'Commented By Employer'
                WHEN 18 THEN N'Not Issued' WHEN 21 THEN N'Re Issued' END AS StatusLabel) sl
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
        WHEN lv.LastNonReplayStatus = 12 THEN N'Reject By Employer'
        WHEN lv.LastNonReplayStatus = 13 THEN N'Approve By Employer'
        WHEN lv.LastNonReplayStatus = 14 THEN N'Approved As Note By Employer'
        WHEN lv.LastNonReplayStatus = 15 THEN N'Commented By Employer'
        WHEN lv.LastNonReplayStatus = 18 THEN N'Not Review'
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
        WHEN v.Status IN (2, 7, 28) THEN N'Not Review'
        WHEN v.Status = 11 THEN N'Commented By Dcc'
        WHEN v.Status = 9 THEN
            CASE WHEN v.Cnt > 1
                 THEN (SELECT CASE v2.Status
                        WHEN 2 THEN N'Not Review' WHEN 7 THEN N'Not Review' WHEN 28 THEN N'Not Review'
                        WHEN 11 THEN N'Commented By Dcc' WHEN 9 THEN N'Approved By DCC' WHEN 10 THEN N'Reject By DCC'
                        WHEN 16 THEN N'Hold' WHEN 17 THEN N'UnHold' WHEN 21 THEN N'ReIssued' WHEN 30 THEN N'Issue For Client'
                        WHEN 12 THEN N'Reject By Employer' WHEN 13 THEN N'Approve By Employer'
                        WHEN 14 THEN N'Approved As Note By Employer' WHEN 15 THEN N'Commented By Employer' END
                       FROM Vc v2 WHERE v2.DocumentId = v.DocumentId AND v2.RnDesc = 2)
                 ELSE N'Approved By DCC'
            END
        WHEN v.Status = 16 THEN
            CASE WHEN v.Cnt > 1
                 THEN (SELECT CASE v2.Status
                        WHEN 2 THEN N'Not Review' WHEN 7 THEN N'Not Review' WHEN 28 THEN N'Not Review'
                        WHEN 11 THEN N'Commented By Dcc' WHEN 9 THEN N'Approved By DCC' WHEN 10 THEN N'Reject By DCC'
                        WHEN 21 THEN N'ReIssued' WHEN 30 THEN N'Issue For Client'
                        WHEN 12 THEN N'Reject By Employer' WHEN 13 THEN N'Approve By Employer'
                        WHEN 14 THEN N'Approved As Note By Employer' WHEN 15 THEN N'Commented By Employer' END
                       FROM Vc v2 WHERE v2.DocumentId = v.DocumentId AND v2.RnDesc = 2)
                 ELSE N'Not Issued' END
        WHEN v.Status = 17 THEN
            CASE WHEN v.Cnt > 2
                 THEN (SELECT CASE v2.Status
                        WHEN 2 THEN N'Not Review' WHEN 7 THEN N'Not Review' WHEN 28 THEN N'Not Review'
                        WHEN 11 THEN N'Commented By Dcc' WHEN 9 THEN N'Approved By DCC' WHEN 10 THEN N'Reject By DCC'
                        WHEN 21 THEN N'ReIssued' WHEN 30 THEN N'Issue For Client'
                        WHEN 12 THEN N'Reject By Employer' WHEN 13 THEN N'Approve By Employer'
                        WHEN 14 THEN N'Approved As Note By Employer' WHEN 15 THEN N'Commented By Employer' END
                       FROM Vc v2 WHERE v2.DocumentId = v.DocumentId AND v2.RnDesc = 3)
                 ELSE N'Not Issued' END
        WHEN v.Status = 30 THEN N'Issue For Client'
        WHEN v.Status = 12 THEN N'Reject By Employer'
        WHEN v.Status = 13 THEN N'Approve By Employer'
        WHEN v.Status = 14 THEN N'Approved As Note By Employer'
        WHEN v.Status = 15 THEN N'Commented By Employer'
        WHEN v.Status = 21 THEN N'ReIssued'
        ELSE N'Not Valid'
    END AS VendorStatus
INTO #RevisionVendorStatus
FROM Vc v
WHERE v.RnDesc = 1;

/* FinalStatus per VPIS = آخرین ValidStatus در chain revisions */
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

/* ---------- تاریخ صدور اولیه (برای fallback در محاسبه ClientRepliedTime) ---------- */
IF OBJECT_ID('tempdb..#VpisIssueDate') IS NOT NULL DROP TABLE #VpisIssueDate;
SELECT vb.VpisId,
    CASE WHEN vb.IsVendorProject = 1
         THEN (SELECT MAX(d.CreatedOnMiladiDateTime) FROM #Documents d WHERE d.DocumentVpisId = vb.VpisId)
         ELSE (SELECT TOP 1 t.CreatedOnMiladiDateTime
               FROM #Documents d INNER JOIN #Transmittals t ON t.DocumentId = d.Id
               WHERE d.DocumentVpisId = vb.VpisId
               ORDER BY d.Revision DESC, t.Id DESC)
    END AS IssueDate
INTO #VpisIssueDate
FROM #VpisBase vb;

/* ======================== Result Set 1: MdrDocumentHeaders ======================== */
;WITH
LastIssuedTransmittal AS (
    SELECT * FROM (
        SELECT d.DocumentVpisId, t.CreatedOnMiladiDateTime AS HavayarLastSendDate,
               ROW_NUMBER() OVER (PARTITION BY d.DocumentVpisId ORDER BY d.Revision DESC, t.Id DESC) AS Rn
        FROM #Documents d INNER JOIN #Transmittals t ON t.DocumentId = d.Id
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
Delays AS (
    SELECT
        vb.VpisId,
        /* HavayarRepliedTime: روز کاری از آخرین ارسال واقعی کارفرما تا امروز (وقتی نوبت پاسخ هوایار است) */
        CASE
            WHEN lcf.LastFlowStatus = 13 THEN NULL
            WHEN lcf.LastFlowStatus = 18 OR NOT EXISTS (
                SELECT 1 FROM ClientComments cc WHERE cc.VpisId = vb.VpisId AND cc.Status IN (12,15,14))
            THEN NULL
            ELSE dbo.fn_EdmsBusinessDayCount(CAST(lcf.ClientLastSentDate AS DATE), CAST(@Now AS DATE), 1, 1)
        END AS HavayarRepliedTime,
        /* ClientRepliedTime: روز کاری از آخرین ارسال هوایار (یا صدور اولیه) تا امروز (وقتی نوبت پاسخ کارفرماست) */
        CASE
            WHEN lcf.LastFlowStatus = 13 THEN NULL
            WHEN lcf.LastFlowStatus = 18 OR NOT EXISTS (
                SELECT 1 FROM ClientComments cc WHERE cc.VpisId = vb.VpisId AND cc.Status IN (12,15,14))
            THEN dbo.fn_EdmsBusinessDayCount(
                CAST(COALESCE(lit.HavayarLastSendDate, vid.IssueDate) AS DATE), CAST(@Now AS DATE),
                CASE WHEN vb.IsVendorProject = 1 THEN 0 ELSE 1 END, 1)
            ELSE NULL
        END AS ClientRepliedTime
    FROM #VpisBase vb
    LEFT JOIN LastClientFlow lcf ON lcf.VpisId = vb.VpisId
    LEFT JOIN LastIssuedTransmittal lit ON lit.DocumentVpisId = vb.VpisId
    LEFT JOIN #VpisIssueDate vid ON vid.VpisId = vb.VpisId
)
SELECT
    vb.ProjectId, vb.ProjectCode, vb.ProjectName, vb.IsVendorProject, vb.VendorTypeLabel,
    vb.DocumentNumber, vb.DocumentTitle,
    vfs.FinalStatus,
    CAST(COALESCE(vb.RePlanDate, vb.FirstIssueDate) AS DATE) AS ScheduleDate,
    dl.ClientRepliedTime,
    dl.HavayarRepliedTime,
    /* نسخه INT (۰/۱) برای جمع‌پذیر بودن در کارت‌های KPI (SUM ستون = COUNT ردیف‌های تاخیردار پس از اعمال فیلترهای تعاملی) */
    CAST(CASE
        WHEN (dl.ClientRepliedTime > 7)
          OR (vfs.FinalStatus = N'Not Issued' AND CAST(GETDATE() AS DATE) > CAST(COALESCE(vb.RePlanDate, vb.FirstIssueDate) AS DATE))
        THEN 1 ELSE 0 END AS INT) AS IsHavayarDelay,
    CAST(CASE WHEN dl.HavayarRepliedTime > 10 THEN 1 ELSE 0 END AS INT) AS IsClientDelay,
    CASE
        WHEN (dl.ClientRepliedTime > 7)
          OR (vfs.FinalStatus = N'Not Issued' AND CAST(GETDATE() AS DATE) > CAST(COALESCE(vb.RePlanDate, vb.FirstIssueDate) AS DATE))
        THEN COALESCE(dl.ClientRepliedTime, DATEDIFF(DAY, CAST(COALESCE(vb.RePlanDate, vb.FirstIssueDate) AS DATE), CAST(GETDATE() AS DATE)))
        WHEN dl.HavayarRepliedTime > 10 THEN dl.HavayarRepliedTime
        ELSE NULL
    END AS DelayDays,
    CASE
        WHEN ((dl.ClientRepliedTime > 7) OR (vfs.FinalStatus = N'Not Issued' AND CAST(GETDATE() AS DATE) > CAST(COALESCE(vb.RePlanDate, vb.FirstIssueDate) AS DATE)))
             AND dl.HavayarRepliedTime > 10 THEN N'هوایار و کارفرما'
        WHEN (dl.ClientRepliedTime > 7) OR (vfs.FinalStatus = N'Not Issued' AND CAST(GETDATE() AS DATE) > CAST(COALESCE(vb.RePlanDate, vb.FirstIssueDate) AS DATE)) THEN N'هوایار'
        WHEN dl.HavayarRepliedTime > 10 THEN N'کارفرما'
        ELSE NULL
    END AS DelayType,
    CAST(1 AS INT) AS Cnt /* برای نمودار «تعداد بر اساس وضعیت»: SUM(Cnt) گروه‌بندی‌شده روی FinalStatus */
FROM #VpisBase vb
LEFT JOIN #VpisFinalStatus vfs ON vfs.VpisId = vb.VpisId
LEFT JOIN Delays dl ON dl.VpisId = vb.VpisId
ORDER BY
    CASE WHEN ((dl.ClientRepliedTime > 7) OR (vfs.FinalStatus = N'Not Issued' AND CAST(GETDATE() AS DATE) > CAST(COALESCE(vb.RePlanDate, vb.FirstIssueDate) AS DATE)) OR dl.HavayarRepliedTime > 10) THEN 0 ELSE 1 END,
    vb.ProjectCode, vb.DocumentNumber;

/* ======================== Result Set 2: MdrDocumentIssuances ======================== */
SELECT
    vb.ProjectId, vb.ProjectCode, vb.IsVendorProject, vb.VendorTypeLabel,
    vb.DocumentNumber,
    CAST(d.Revision AS VARCHAR(10)) AS RevNo,
    CAST(calc.HavayarSendDt AS DATE) AS HavayarSendingDate,
    LEFT(calc.ShamsiSrc, 4) AS ShamsiYear,
    SUBSTRING(calc.ShamsiSrc, 6, 2) AS ShamsiMonth,
    CAST(1 AS INT) AS Cnt /* برای KPI: SUM(Cnt) پس از فیلتر = تعداد ردیف */
FROM #VpisBase vb
INNER JOIN #Documents d ON d.DocumentVpisId = vb.VpisId
OUTER APPLY (SELECT TOP 1 t.* FROM #Transmittals t WHERE t.DocumentId = d.Id ORDER BY t.Id DESC) t_issue
CROSS APPLY (SELECT
    CASE WHEN vb.IsVendorProject = 1 THEN d.CreatedOnMiladiDateTime ELSE t_issue.CreatedOnMiladiDateTime END AS HavayarSendDt,
    CASE WHEN vb.IsVendorProject = 1 THEN d.CreatedOnShamsiDateTime ELSE t_issue.CreatedOnShamsiDateTime END AS ShamsiSrc
) calc
WHERE calc.HavayarSendDt IS NOT NULL
ORDER BY vb.ProjectCode, vb.DocumentNumber, d.Revision;

/* ======================== Result Set 3: MdrTransmittals ======================== */
SELECT
    p.Id AS ProjectId, p.Code AS ProjectCode, p.ProjectIsVendoriType AS IsVendorProject,
    CASE WHEN p.ProjectIsVendoriType = 1 THEN N'VP' ELSE N'SP' END AS VendorTypeLabel,
    t.Number AS TransmittalNumber,
    CAST(t.CreatedOnMiladiDateTime AS DATE) AS TransmittalDate,
    LEFT(t.CreatedOnShamsiDateTime, 4) AS ShamsiYear,
    SUBSTRING(t.CreatedOnShamsiDateTime, 6, 2) AS ShamsiMonth,
    CAST(1 AS INT) AS Cnt /* برای KPI: SUM(Cnt) پس از فیلتر = تعداد ردیف */
FROM Edms.Transmital t
INNER JOIN Edms.Project p ON p.Id = t.ProjectId
WHERE ISNULL(t.IsActive, 1) <> 2 AND ISNULL(p.IsActive, 1) <> 2
ORDER BY t.CreatedOnMiladiDateTime DESC;

DROP TABLE IF EXISTS #VpisBase, #Documents, #Comments, #Transmittals,
    #RevisionValidStatus, #RevisionVendorStatus, #VpisFinalStatus, #VpisIssueDate;
