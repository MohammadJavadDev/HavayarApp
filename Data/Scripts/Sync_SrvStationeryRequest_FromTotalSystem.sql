/*
Sync_SrvStationeryRequest_FromTotalSystem.sql
One-time sync: Srv_StationeryRequest + Srv_RequestComment (StationeryRequestId IS NOT NULL) via [TMS]
  -> Srv.StationeryRequest / Srv.StationeryRequestComment (HavayarApp)

HtsId:
  Header  = Srv_StationeryRequest.Id
  Comment = Srv_RequestComment.Id
Parent FK on comments: StationeryRequestId = Srv.StationeryRequest.Id (via header HtsId).
Do NOT sync Srv_Attachment.
Do NOT sync IsApply / BranchId.

Mappings:
  CreatedById: COALESCE(ActiveDirectoryUsername, Username) -> system.User.Username
    + allahverdi.s missing -> allahvirdi.s; OldUserId 40 alias if needed
  Unmapped creator -> user Id 1 (count, do not drop)
  StatusId, StationeryTypeId: copy as-is
  CreatedOnMiladiDateTime <- CreatedDate; CreatedOnShamsiDateTime <- CreatedDateInText when present
Idempotent UPDATE-then-INSERT on HtsId. Never overwrite HtsId=0. Do not delete target rows missing from source.
UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i ...
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Srv.StationeryRequest', N'U') IS NULL
   OR OBJECT_ID(N'Srv.StationeryRequestComment', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول Srv.StationeryRequest / Srv.StationeryRequestComment وجود ندارند — ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @Seed NVARCHAR(80) = N'sync-srv-stationeryrequest';
DECLARE @FallbackUserId BIGINT = 1;
DECLARE @FallbackUserName NVARCHAR(150) =
(
    SELECT TOP (1) COALESCE(NULLIF(LTRIM(RTRIM(NameFa)), N''), Name, Username)
    FROM [system].[User]
    WHERE Id = @FallbackUserId
);

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, NewUserId, NewUserName, Username
    INTO #UserMap
    FROM (
        SELECT
            CAST(ou.User_ID AS INT) AS OldUserId,
            nu.Id AS NewUserId,
            COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username) AS NewUserName,
            COALESCE(
                NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N''),
                NULLIF(LTRIM(RTRIM(ou.Username)), N'')
            ) AS Username,
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

    IF NOT EXISTS (SELECT 1 FROM #UserMap WHERE OldUserId = 40)
    BEGIN
        INSERT INTO #UserMap (OldUserId, NewUserId, NewUserName, Username)
        SELECT
            40,
            nu.Id,
            COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username),
            nu.Username
        FROM [system].[User] nu
        WHERE nu.Username = N'allahvirdi.s';
    END;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    IF OBJECT_ID('tempdb..#HdrSrc') IS NOT NULL DROP TABLE #HdrSrc;
    SELECT
        CAST(c.Id AS BIGINT) AS HtsId,
        CAST(c.StationeryTypeId AS INT) AS StationeryTypeId,
        CAST(c.Count AS SMALLINT) AS Count,
        LEFT(NULLIF(LTRIM(RTRIM(c.RequestDescription)), N''), 256) AS RequestDescription,
        LEFT(NULLIF(LTRIM(RTRIM(c.LastComment)), N''), 256) AS LastComment,
        CAST(c.StatusId AS SMALLINT) AS StatusId,
        LEFT(NULLIF(LTRIM(RTRIM(c.DoneDateTimeIntext)), N''), 20) AS DoneDateTimeInText,
        LEFT(NULLIF(LTRIM(RTRIM(c.CreatedDateInText)), N''), 20) AS CreatedDateInText,
        CAST(c.CreatedUserId AS INT) AS OldCreatedUserId,
        CASE WHEN umc.NewUserId IS NULL THEN 1 ELSE 0 END AS IsUnmappedCreator,
        ISNULL(umc.NewUserId, @FallbackUserId) AS CreatedById,
        ISNULL(umc.NewUserName, ISNULL(@FallbackUserName, @Seed)) AS CreatedByName,
        CAST(c.CreatedDate AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(c.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #HdrSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_StationeryRequest] c
    LEFT JOIN #UserMap umc ON umc.OldUserId = CAST(c.CreatedUserId AS INT);

    PRINT N'Syncing StationeryRequest headers (unmapped creator -> Id 1)...';

    UPDATE t SET
        t.StationeryTypeId = s.StationeryTypeId,
        t.Count = s.Count,
        t.RequestDescription = s.RequestDescription,
        t.LastComment = s.LastComment,
        t.StatusId = s.StatusId,
        t.DoneDateTimeInText = s.DoneDateTimeInText,
        t.CreatedDateInText = s.CreatedDateInText,
        t.CreatedById = s.CreatedById,
        t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.ModifiedById = s.CreatedById,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Srv.StationeryRequest t
    INNER JOIN #HdrSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  StationeryRequest updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.StationeryRequest (
        HtsId, StationeryTypeId, Count, RequestDescription, LastComment, StatusId,
        DoneDateTimeInText, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.StationeryTypeId, s.Count, s.RequestDescription, s.LastComment, s.StatusId,
        s.DoneDateTimeInText, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, @Seed, @Now, @NowShamsi, 1
    FROM #HdrSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.StationeryRequest t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  StationeryRequest inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    IF OBJECT_ID('tempdb..#CmtSrc') IS NOT NULL DROP TABLE #CmtSrc;
    SELECT
        CAST(rc.Id AS BIGINT) AS HtsId,
        hdr.Id AS StationeryRequestId,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(rc.Comment)), N''), N''), 256) AS Comment,
        CAST(rc.StatusId AS SMALLINT) AS StatusId,
        LEFT(NULLIF(LTRIM(RTRIM(rc.ApproximateExecutionDate)), N''), 20) AS ApproximateExecutionDate,
        LEFT(NULLIF(LTRIM(RTRIM(rc.CreatedDateInText)), N''), 20) AS CreatedDateInText,
        CAST(rc.CreatedUserId AS INT) AS OldCreatedUserId,
        CASE WHEN um.NewUserId IS NULL THEN 1 ELSE 0 END AS IsUnmappedCreator,
        ISNULL(um.NewUserId, @FallbackUserId) AS CreatedById,
        ISNULL(um.NewUserName, ISNULL(@FallbackUserName, @Seed)) AS CreatedByName,
        CAST(rc.CreatedDate AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(rc.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #CmtSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] rc
    INNER JOIN Srv.StationeryRequest hdr
        ON hdr.HtsId = CAST(rc.StationeryRequestId AS BIGINT)
       AND hdr.HtsId <> 0
    LEFT JOIN #UserMap um ON um.OldUserId = CAST(rc.CreatedUserId AS INT)
    WHERE rc.StationeryRequestId IS NOT NULL;

    UPDATE t SET
        t.StationeryRequestId = s.StationeryRequestId,
        t.Comment = s.Comment,
        t.StatusId = s.StatusId,
        t.ApproximateExecutionDate = s.ApproximateExecutionDate,
        t.CreatedDateInText = s.CreatedDateInText,
        t.CreatedById = s.CreatedById,
        t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.ModifiedById = s.CreatedById,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Srv.StationeryRequestComment t
    INNER JOIN #CmtSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  StationeryRequestComment updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.StationeryRequestComment (
        HtsId, StationeryRequestId, Comment, StatusId, ApproximateExecutionDate, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.StationeryRequestId, s.Comment, s.StatusId, s.ApproximateExecutionDate, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, @Seed, @Now, @NowShamsi, 1
    FROM #CmtSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.StationeryRequestComment t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  StationeryRequestComment inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    PRINT N'=== VERIFY COUNTS ===';
    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_StationeryRequest]) AS TmsHeaders,
        (SELECT COUNT(*) FROM Srv.StationeryRequest WHERE HtsId <> 0) AS HavayarHeadersSynced,
        (SELECT COUNT(*) FROM Srv.StationeryRequest) AS HavayarHeadersAll,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] WHERE StationeryRequestId IS NOT NULL) AS TmsComments,
        (SELECT COUNT(*) FROM Srv.StationeryRequestComment WHERE HtsId <> 0) AS HavayarCommentsSynced,
        (SELECT COUNT(*) FROM Srv.StationeryRequestComment) AS HavayarCommentsAll,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] rc
         WHERE rc.StationeryRequestId IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM Srv.StationeryRequest h
               WHERE h.HtsId = CAST(rc.StationeryRequestId AS BIGINT) AND h.HtsId <> 0
           )) AS SkippedOrphanComments;

    PRINT N'=== UNMAPPED CREATORS ===';
    SELECT
        (SELECT COUNT(*) FROM #HdrSrc WHERE IsUnmappedCreator = 1) AS UnmappedCreator,
        (SELECT COUNT(*) FROM #CmtSrc WHERE IsUnmappedCreator = 1) AS UnmappedCommentCreator;

    PRINT N'Sync_SrvStationeryRequest_FromTotalSystem completed.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'Sync failed (line %d): %s', 16, 1, @Line, @Err);
END CATCH;
