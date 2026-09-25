/*
Sync_CngWorkReport_FromTotalSystem.sql
همگام‌سازی Cng.WorkReport + WorkReportConsumedPart + WorkReportFailure از [TMS].[TotalSystem].
HtsId = CAST(old.Id AS BIGINT).
Station via Cng.StationInfo.HtsId؛ Part via Inv.Part.HtsId؛
SaleOrder: ستون HTS SaleOrderDetailId = Sale_Order.Order_ID → Sale.[Order].HtsId؛
FailureInfo via Cng.FailureInfo.HtsId.
کاربر CreatedBy/ModifiedBy از #UserMap.
فایل: فقط متادیتا؛ FileId در INSERT خالی؛ در UPDATE اگر قبلاً مقدار داشته باشد دست نخورده می‌ماند.
کپی باینری: Sync_CngWorkReport_CopyFiles.ps1
Idempotent MERGE روی HtsId.
UTF-8 BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Cng.WorkReport', N'U') IS NULL
   OR OBJECT_ID(N'Cng.WorkReportConsumedPart', N'U') IS NULL
   OR OBJECT_ID(N'Cng.WorkReportFailure', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول Cng.WorkReport / ConsumedPart / Failure وجود ندارند.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-cng-workreport';

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
            COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username) AS NewUserName,
            ROW_NUMBER() OVER (
                PARTITION BY ou.User_ID
                ORDER BY
                    CASE WHEN LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username))) THEN 0 ELSE 1 END,
                    nu.Id
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN system.[User] nu
            ON LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
            OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
            OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    /* ========== Headers ========== */
    PRINT N'=== Cng.WorkReport ===';
    IF OBJECT_ID('tempdb..#HdrSrc') IS NOT NULL DROP TABLE #HdrSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        st.Id AS StationInfoId,
        CAST(s.RequestTypeId AS INT) AS RequestTypeId,
        CAST(s.ServiceTypeId AS INT) AS ServiceTypeId,
        NULLIF(LTRIM(RTRIM(s.ReportNumber)), N'') AS ReportNumber,
        CAST(s.ReportDate AS datetime2) AS ReportMiladiDate,
        NULLIF(LTRIM(RTRIM(s.ReportDateInText)), N'') AS ReportShamsiDate,
        s.WorkingHours,
        NULLIF(LTRIM(RTRIM(s.WorkingHoursInText)), N'') AS WorkingHoursInText,
        NULLIF(LTRIM(RTRIM(s.CossTetaValue)), N'') AS CossTetaValue,
        CAST(s.ConsumedOilTypeId AS INT) AS ConsumedOilTypeId,
        CAST(s.IsApprovedByOwner AS BIT) AS IsApprovedByOwner,
        NULLIF(LTRIM(RTRIM(s.FailureExpertComment)), N'') AS FailureExpertComment,
        NULLIF(LTRIM(RTRIM(s.OwnerComment)), N'') AS OwnerComment,
        NULLIF(LTRIM(RTRIM(s.WorkReportFileAddress)), N'') AS WorkReportFileAddress,
        NULLIF(LTRIM(RTRIM(s.WorkReportFileType)), N'') AS WorkReportFileType,
        CAST(s.IsSurveyLinkSended AS BIT) AS IsSurveyLinkSended,
        NULLIF(LTRIM(RTRIM(s.SurveyLinkSendedDateTime)), N'') AS SurveyLinkSendedDateTime,
        NULLIF(LTRIM(RTRIM(s.Comment)), N'') AS Comment,
        CAST(s.CreatedDate AS datetime2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        umCreate.NewUserId AS CreatedById,
        umCreate.NewUserName AS CreatedByName,
        CAST(s.UpdatedDate AS datetime2) AS ModifiedDateMiladiDateTime,
        NULLIF(LTRIM(RTRIM(s.UpdatedDateInText)), N'') AS ModifiedDateShamsiDateTime,
        umMod.NewUserId AS ModifiedById,
        umMod.NewUserName AS ModifiedByName,
        CASE WHEN st.Id IS NULL THEN 1 ELSE 0 END AS SkipStation
    INTO #HdrSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_WorkReport] s
    LEFT JOIN Cng.StationInfo st ON st.HtsId = CAST(s.StationInfoId AS BIGINT) AND ISNULL(st.HtsId, 0) <> 0
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = CAST(s.CreatedUserId AS INT)
    LEFT JOIN #UserMap umMod ON umMod.OldUserId = CAST(s.UpdatedUserId AS INT);

    DECLARE @HdrCnt INT = (SELECT COUNT(*) FROM #HdrSrc);
    DECLARE @SkipStation INT = (SELECT COUNT(*) FROM #HdrSrc WHERE SkipStation = 1);
    PRINT N'  Headers source: ' + CAST(@HdrCnt AS nvarchar(20));
    PRINT N'  Skipped (station unmapped): ' + CAST(@SkipStation AS nvarchar(20));

    IF OBJECT_ID('tempdb..#HdrMapped') IS NOT NULL DROP TABLE #HdrMapped;
    SELECT * INTO #HdrMapped FROM #HdrSrc WHERE SkipStation = 0;

    UPDATE t SET
        t.StationInfoId = s.StationInfoId,
        t.RequestTypeId = s.RequestTypeId,
        t.ServiceTypeId = s.ServiceTypeId,
        t.ReportNumber = s.ReportNumber,
        t.ReportMiladiDate = s.ReportMiladiDate,
        t.ReportShamsiDate = s.ReportShamsiDate,
        t.WorkingHours = s.WorkingHours,
        t.WorkingHoursInText = s.WorkingHoursInText,
        t.CossTetaValue = s.CossTetaValue,
        t.ConsumedOilTypeId = s.ConsumedOilTypeId,
        t.IsApprovedByOwner = s.IsApprovedByOwner,
        t.FailureExpertComment = s.FailureExpertComment,
        t.OwnerComment = s.OwnerComment,
        t.WorkReportFileAddress = s.WorkReportFileAddress,
        t.WorkReportFileType = s.WorkReportFileType,
        t.FileId = t.FileId,
        t.IsSurveyLinkSended = s.IsSurveyLinkSended,
        t.SurveyLinkSendedDateTime = s.SurveyLinkSendedDateTime,
        t.Comment = s.Comment,
        t.CreatedById = COALESCE(s.CreatedById, t.CreatedById),
        t.CreatedByName = COALESCE(s.CreatedByName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedOnMiladiDateTime, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(s.CreatedOnShamsiDateTime, t.CreatedOnShamsiDateTime),
        t.ModifiedById = COALESCE(s.ModifiedById, 1),
        t.ModifiedByName = COALESCE(s.ModifiedByName, @Seed),
        t.ModifiedDateMiladiDateTime = COALESCE(s.ModifiedDateMiladiDateTime, @Now),
        t.ModifiedDateShamsiDateTime = COALESCE(s.ModifiedDateShamsiDateTime, @NowShamsi),
        t.IsActive = 1
    FROM Cng.WorkReport t
    INNER JOIN #HdrMapped s ON s.HtsId = t.HtsId;
    PRINT N'  WorkReport updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.WorkReport (
        HtsId, StationInfoId, RequestTypeId, ServiceTypeId, ReportNumber,
        ReportMiladiDate, ReportShamsiDate, WorkingHours, WorkingHoursInText, CossTetaValue,
        ConsumedOilTypeId, IsApprovedByOwner, FailureExpertComment, OwnerComment,
        WorkReportFileAddress, WorkReportFileType, FileId, IsSurveyLinkSended, SurveyLinkSendedDateTime, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.StationInfoId, s.RequestTypeId, s.ServiceTypeId, s.ReportNumber,
        ISNULL(s.ReportMiladiDate, @Now), s.ReportShamsiDate, s.WorkingHours, s.WorkingHoursInText, s.CossTetaValue,
        s.ConsumedOilTypeId, ISNULL(s.IsApprovedByOwner, 0), s.FailureExpertComment, s.OwnerComment,
        s.WorkReportFileAddress, s.WorkReportFileType, NULL, s.IsSurveyLinkSended, s.SurveyLinkSendedDateTime, s.Comment,
        ISNULL(s.CreatedById, 1), ISNULL(s.CreatedByName, @Seed),
        ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
        ISNULL(s.ModifiedById, 1), ISNULL(s.ModifiedByName, @Seed),
        ISNULL(s.ModifiedDateMiladiDateTime, @Now), ISNULL(s.ModifiedDateShamsiDateTime, @NowShamsi),
        1
    FROM #HdrMapped s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.WorkReport t WHERE t.HtsId = s.HtsId);
    PRINT N'  WorkReport inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    /* ========== Consumed parts ========== */
    PRINT N'=== Cng.WorkReportConsumedPart ===';
    IF OBJECT_ID('tempdb..#PartSrc') IS NOT NULL DROP TABLE #PartSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        wr.Id AS WorkReportId,
        so.Id AS SaleOrderId,
        p.Id AS PartId,
        CAST(ISNULL(s.Mount, 0) AS DECIMAL(18, 4)) AS Mount,
        NULLIF(LTRIM(RTRIM(s.HotnessCondition)), N'') AS HotnessCondition,
        NULLIF(LTRIM(RTRIM(s.Comment)), N'') AS Comment,
        CAST(s.CreatedDate AS datetime2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        umCreate.NewUserId AS CreatedById,
        umCreate.NewUserName AS CreatedByName,
        CASE WHEN wr.Id IS NULL THEN 1 ELSE 0 END AS SkipParent,
        CASE WHEN p.Id IS NULL THEN 1 ELSE 0 END AS SkipPart
    INTO #PartSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_WorkReportConsumedPart] s
    LEFT JOIN Cng.WorkReport wr ON wr.HtsId = CAST(s.WorkReportId AS BIGINT)
    LEFT JOIN Inv.Part p ON p.HtsId = CAST(s.PartId AS BIGINT) AND ISNULL(p.HtsId, 0) <> 0
    LEFT JOIN Sale.[Order] so ON so.HtsId = CAST(s.SaleOrderDetailId AS BIGINT) AND ISNULL(so.HtsId, 0) <> 0
        AND s.SaleOrderDetailId IS NOT NULL AND ISNULL(s.SaleOrderDetailId, 0) <> 0
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = CAST(s.CreatedUserId AS INT);

    DECLARE @PartCnt INT = (SELECT COUNT(*) FROM #PartSrc);
    DECLARE @SkipParentP INT = (SELECT COUNT(*) FROM #PartSrc WHERE SkipParent = 1);
    DECLARE @SkipPartP INT = (SELECT COUNT(*) FROM #PartSrc WHERE SkipPart = 1);
    PRINT N'  ConsumedPart source: ' + CAST(@PartCnt AS nvarchar(20));
    PRINT N'  Skipped parent: ' + CAST(@SkipParentP AS nvarchar(20)) + N', part: ' + CAST(@SkipPartP AS nvarchar(20));

    IF OBJECT_ID('tempdb..#PartMapped') IS NOT NULL DROP TABLE #PartMapped;
    SELECT * INTO #PartMapped FROM #PartSrc WHERE SkipParent = 0 AND SkipPart = 0;

    UPDATE t SET
        t.WorkReportId = s.WorkReportId,
        t.SaleOrderId = s.SaleOrderId,
        t.PartId = s.PartId,
        t.Mount = s.Mount,
        t.HotnessCondition = s.HotnessCondition,
        t.Comment = s.Comment,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.WorkReportConsumedPart t
    INNER JOIN #PartMapped s ON s.HtsId = t.HtsId;
    PRINT N'  ConsumedPart updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.WorkReportConsumedPart (
        HtsId, WorkReportId, SaleOrderId, PartId, Mount, HotnessCondition, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.WorkReportId, s.SaleOrderId, s.PartId, s.Mount, s.HotnessCondition, s.Comment,
        ISNULL(s.CreatedById, 1), ISNULL(s.CreatedByName, @Seed),
        ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
        1, @Seed, @Now, @NowShamsi, 1
    FROM #PartMapped s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.WorkReportConsumedPart t WHERE t.HtsId = s.HtsId);
    PRINT N'  ConsumedPart inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    /* ========== Failures ========== */
    PRINT N'=== Cng.WorkReportFailure ===';
    IF OBJECT_ID('tempdb..#FailSrc') IS NOT NULL DROP TABLE #FailSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        wr.Id AS WorkReportId,
        fi.Id AS FailureInfoId,
        CAST(s.Value AS DECIMAL(18, 4)) AS Value,
        CAST(s.SetPointValue AS DECIMAL(18, 4)) AS SetPointValue,
        NULLIF(LTRIM(RTRIM(s.Comment)), N'') AS Comment,
        CAST(s.CreatedDate AS datetime2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        umCreate.NewUserId AS CreatedById,
        umCreate.NewUserName AS CreatedByName,
        CASE WHEN wr.Id IS NULL THEN 1 ELSE 0 END AS SkipParent,
        CASE WHEN fi.Id IS NULL THEN 1 ELSE 0 END AS SkipFailure
    INTO #FailSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_WorkReportFailure] s
    LEFT JOIN Cng.WorkReport wr ON wr.HtsId = CAST(s.WorkReportId AS BIGINT)
    LEFT JOIN Cng.FailureInfo fi ON fi.HtsId = CAST(s.FailureInfoId AS BIGINT) AND ISNULL(fi.HtsId, 0) <> 0
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = CAST(s.CreatedUserId AS INT);

    DECLARE @FailCnt INT = (SELECT COUNT(*) FROM #FailSrc);
    DECLARE @SkipParentF INT = (SELECT COUNT(*) FROM #FailSrc WHERE SkipParent = 1);
    DECLARE @SkipFailF INT = (SELECT COUNT(*) FROM #FailSrc WHERE SkipFailure = 1);
    PRINT N'  Failure source: ' + CAST(@FailCnt AS nvarchar(20));
    PRINT N'  Skipped parent: ' + CAST(@SkipParentF AS nvarchar(20)) + N', failureInfo: ' + CAST(@SkipFailF AS nvarchar(20));

    IF OBJECT_ID('tempdb..#FailMapped') IS NOT NULL DROP TABLE #FailMapped;
    SELECT * INTO #FailMapped FROM #FailSrc WHERE SkipParent = 0 AND SkipFailure = 0;

    UPDATE t SET
        t.WorkReportId = s.WorkReportId,
        t.FailureInfoId = s.FailureInfoId,
        t.Value = s.Value,
        t.SetPointValue = s.SetPointValue,
        t.Comment = s.Comment,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.WorkReportFailure t
    INNER JOIN #FailMapped s ON s.HtsId = t.HtsId;
    PRINT N'  Failure updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.WorkReportFailure (
        HtsId, WorkReportId, FailureInfoId, Value, SetPointValue, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.WorkReportId, s.FailureInfoId, s.Value, s.SetPointValue, s.Comment,
        ISNULL(s.CreatedById, 1), ISNULL(s.CreatedByName, @Seed),
        ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
        1, @Seed, @Now, @NowShamsi, 1
    FROM #FailMapped s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.WorkReportFailure t WHERE t.HtsId = s.HtsId);
    PRINT N'  Failure inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    DECLARE @NeedFile INT = (
        SELECT COUNT(*) FROM Cng.WorkReport
        WHERE FileId IS NULL
          AND WorkReportFileAddress IS NOT NULL
          AND LTRIM(RTRIM(WorkReportFileAddress)) <> N''
    );
    PRINT N'  Headers needing FileEntity copy: ' + CAST(@NeedFile AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Sync_CngWorkReport_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT N'WorkReport' AS T, COUNT(*) AS Cnt FROM Cng.WorkReport
UNION ALL SELECT N'ConsumedPart', COUNT(*) FROM Cng.WorkReportConsumedPart
UNION ALL SELECT N'Failure', COUNT(*) FROM Cng.WorkReportFailure
UNION ALL SELECT N'NeedFile', COUNT(*) FROM Cng.WorkReport
    WHERE FileId IS NULL AND WorkReportFileAddress IS NOT NULL AND LTRIM(RTRIM(WorkReportFileAddress)) <> N''
UNION ALL SELECT N'HTS_WorkReport', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_WorkReport]
UNION ALL SELECT N'HTS_ConsumedPart', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_WorkReportConsumedPart]
UNION ALL SELECT N'HTS_Failure', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_WorkReportFailure];
