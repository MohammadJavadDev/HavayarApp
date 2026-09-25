/*
Sync_SrvAmbassadorRequest_FromTotalSystem.sql
One-time sync: Srv_AmbassadorRequest + Srv_RequestComment (AmbassadorRequestId IS NOT NULL) via [TMS]
  -> Srv.AmbassadorRequest / Srv.AmbassadorRequestComment (HavayarApp)

HtsId:
  Header  = Srv_AmbassadorRequest.Id
  Comment = Srv_RequestComment.Id
Parent FK on comments: AmbassadorRequestId = Srv.AmbassadorRequest.Id (via header HtsId).
Do NOT sync Srv_Attachment (zero ambassador files).
Do NOT insert IsApply / IsOutdoorAmbassadorService / BranchId (HTS-only; not on target).

Mappings:
  CreatedById / ModifiedById: COALESCE(ActiveDirectoryUsername, Username) -> system.User.Username
    + only alias: allahverdi.s missing -> allahvirdi.s (see Sync_SrvCarRequest / Sync_SrvTicketHotelRequest / SyncCourse)
    If still unmapped -> use user Id 1; count as UnmappedCreator (do not drop row).
  AmbassadorId: HRM_Personel.Hamkaran_Personel_FK -> Hcm.Personel.HamkaranId, else PrsCode -> Code; NULL if unmatched
    (source AmbassadorId is smallint personnel id)
  TypeId, JobTypeId, OutdoorAmbassadorServiceId, StatusId: copy HTS lookup ids as-is (StatusId 0 stays 0)
  NeedDate / NeedDateInText / NeedTime / PackageDescription / Destination / LastComment /
    WaitingTime / DoneDateTimeIntext -> DoneDateTimeInText / CreatedDateInText: copy as stored (no year-0720 fix)
  CreatedOnMiladiDateTime <- HTS CreatedDate; CreatedOnShamsiDateTime <- CreatedDateInText when present
  IsActive = 1
  Comment: Comment, StatusId, CreatedDateInText only (do NOT sync ApproximateExecutionDate)

Idempotent UPDATE-then-INSERT on HtsId. Do not delete target rows absent from source.
Havayar-only rows (HtsId = 0) are never overwritten.
UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Sync_SrvAmbassadorRequest_FromTotalSystem.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Srv.AmbassadorRequest', N'U') IS NULL
   OR OBJECT_ID(N'Srv.AmbassadorRequestComment', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول Srv.AmbassadorRequest / Srv.AmbassadorRequestComment وجود ندارند — ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @Seed NVARCHAR(80) = N'sync-srv-ambassadorrequest';
DECLARE @FallbackUserId BIGINT = 1;
DECLARE @FallbackUserName NVARCHAR(150) =
(
    SELECT TOP (1) COALESCE(NULLIF(LTRIM(RTRIM(NameFa)), N''), Name, Username)
    FROM [system].[User]
    WHERE Id = @FallbackUserId
);

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== User map: COALESCE(ActiveDirectoryUsername, Username) + allahverdi.s -> allahvirdi.s ========== */
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

    /* ========== Personel map: Hamkaran_Personel_FK -> HamkaranId, else PrsCode -> Code ========== */
    IF OBJECT_ID('tempdb..#PersonelMap') IS NOT NULL DROP TABLE #PersonelMap;
    SELECT
        CAST(hp.Personel_ID AS INT) AS OldPersonelId,
        COALESCE(byHam.Id, byCode.Id) AS NewPersonelId
    INTO #PersonelMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hp
    OUTER APPLY (
        SELECT TOP (1) p.Id
        FROM Hcm.Personel p
        WHERE hp.Hamkaran_Personel_FK IS NOT NULL
          AND ISNULL(hp.Hamkaran_Personel_FK, 0) <> 0
          AND p.HamkaranId = CAST(hp.Hamkaran_Personel_FK AS BIGINT)
        ORDER BY p.Id
    ) byHam
    OUTER APPLY (
        SELECT TOP (1) p.Id
        FROM Hcm.Personel p
        WHERE byHam.Id IS NULL
          AND NULLIF(LTRIM(RTRIM(CAST(hp.PrsCode AS NVARCHAR(50)))), N'') IS NOT NULL
          AND LTRIM(RTRIM(p.Code)) = LTRIM(RTRIM(CAST(hp.PrsCode AS NVARCHAR(50))))
        ORDER BY p.Id
    ) byCode;
    CREATE UNIQUE CLUSTERED INDEX IX_PersonelMap ON #PersonelMap (OldPersonelId);

    /* ========== Header staging ========== */
    IF OBJECT_ID('tempdb..#HdrSrc') IS NOT NULL DROP TABLE #HdrSrc;
    SELECT
        CAST(a.Id AS BIGINT) AS HtsId,
        CAST(a.TypeId AS INT) AS TypeId,
        CAST(a.JobTypeId AS INT) AS JobTypeId,
        CAST(a.NeedDate AS DATE) AS NeedDate,
        LEFT(NULLIF(LTRIM(RTRIM(a.NeedTime)), N''), 5) AS NeedTime,
        NULLIF(LTRIM(RTRIM(a.NeedDateInText)), N'') AS NeedDateInText,
        NULLIF(LTRIM(RTRIM(a.PackageDescription)), N'') AS PackageDescription,
        CASE WHEN a.AmbassadorId IS NULL THEN NULL ELSE pmAmb.NewPersonelId END AS AmbassadorId,
        CAST(a.AmbassadorId AS INT) AS OldAmbassadorId,
        CAST(a.OutdoorAmbassadorServiceId AS INT) AS OutdoorAmbassadorServiceId,
        NULLIF(LTRIM(RTRIM(a.Destination)), N'') AS Destination,
        NULLIF(LTRIM(RTRIM(a.LastComment)), N'') AS LastComment,
        CAST(a.StatusId AS SMALLINT) AS StatusId,
        CAST(a.WaitingTime AS DECIMAL(18, 1)) AS WaitingTime,
        NULLIF(LTRIM(RTRIM(a.DoneDateTimeIntext)), N'') AS DoneDateTimeInText,
        NULLIF(LTRIM(RTRIM(a.CreatedDateInText)), N'') AS CreatedDateInText,
        CAST(a.CreatedUserId AS INT) AS OldCreatedUserId,
        CASE WHEN umc.NewUserId IS NULL THEN 1 ELSE 0 END AS IsUnmappedCreator,
        ISNULL(umc.NewUserId, @FallbackUserId) AS CreatedById,
        ISNULL(umc.NewUserName, ISNULL(@FallbackUserName, @Seed)) AS CreatedByName,
        CAST(a.CreatedDate AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(a.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #HdrSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_AmbassadorRequest] a
    LEFT JOIN #PersonelMap pmAmb ON a.AmbassadorId IS NOT NULL AND pmAmb.OldPersonelId = CAST(a.AmbassadorId AS INT)
    LEFT JOIN #UserMap umc ON umc.OldUserId = CAST(a.CreatedUserId AS INT);

    PRINT N'Syncing AmbassadorRequest headers (unmapped ambassador -> NULL; unmapped creator -> Id 1)...';

    /* ========== Headers: UPDATE then INSERT (skip HtsId = 0 targets) ========== */
    UPDATE t SET
        t.TypeId = s.TypeId,
        t.JobTypeId = s.JobTypeId,
        t.NeedDate = s.NeedDate,
        t.NeedTime = s.NeedTime,
        t.NeedDateInText = s.NeedDateInText,
        t.PackageDescription = s.PackageDescription,
        t.AmbassadorId = s.AmbassadorId,
        t.OutdoorAmbassadorServiceId = s.OutdoorAmbassadorServiceId,
        t.Destination = s.Destination,
        t.LastComment = s.LastComment,
        t.StatusId = s.StatusId,
        t.WaitingTime = s.WaitingTime,
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
    FROM Srv.AmbassadorRequest t
    INNER JOIN #HdrSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  AmbassadorRequest updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.AmbassadorRequest (
        HtsId, TypeId, JobTypeId, NeedDate, NeedTime, NeedDateInText, PackageDescription,
        AmbassadorId, OutdoorAmbassadorServiceId, Destination, LastComment, StatusId, WaitingTime,
        DoneDateTimeInText, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.TypeId, s.JobTypeId, s.NeedDate, s.NeedTime, s.NeedDateInText, s.PackageDescription,
        s.AmbassadorId, s.OutdoorAmbassadorServiceId, s.Destination, s.LastComment, s.StatusId, s.WaitingTime,
        s.DoneDateTimeInText, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, @Seed, @Now, @NowShamsi, 1
    FROM #HdrSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.AmbassadorRequest t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  AmbassadorRequest inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ========== Comments (only AmbassadorRequestId IS NOT NULL; skip orphan parents) ========== */
    IF OBJECT_ID('tempdb..#CmtSrc') IS NOT NULL DROP TABLE #CmtSrc;
    SELECT
        CAST(rc.Id AS BIGINT) AS HtsId,
        hdr.Id AS AmbassadorRequestId,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(rc.Comment)), N''), N''), 256) AS Comment,
        CAST(rc.StatusId AS SMALLINT) AS StatusId,
        NULLIF(LTRIM(RTRIM(rc.CreatedDateInText)), N'') AS CreatedDateInText,
        CAST(rc.CreatedUserId AS INT) AS OldCreatedUserId,
        CASE WHEN um.NewUserId IS NULL THEN 1 ELSE 0 END AS IsUnmappedCreator,
        ISNULL(um.NewUserId, @FallbackUserId) AS CreatedById,
        ISNULL(um.NewUserName, ISNULL(@FallbackUserName, @Seed)) AS CreatedByName,
        CAST(rc.CreatedDate AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(rc.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #CmtSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] rc
    INNER JOIN Srv.AmbassadorRequest hdr
        ON hdr.HtsId = CAST(rc.AmbassadorRequestId AS BIGINT)
       AND hdr.HtsId <> 0
    LEFT JOIN #UserMap um ON um.OldUserId = CAST(rc.CreatedUserId AS INT)
    WHERE rc.AmbassadorRequestId IS NOT NULL;

    UPDATE t SET
        t.AmbassadorRequestId = s.AmbassadorRequestId,
        t.Comment = s.Comment,
        t.StatusId = s.StatusId,
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
    FROM Srv.AmbassadorRequestComment t
    INNER JOIN #CmtSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  AmbassadorRequestComment updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.AmbassadorRequestComment (
        HtsId, AmbassadorRequestId, Comment, StatusId, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.AmbassadorRequestId, s.Comment, s.StatusId, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, @Seed, @Now, @NowShamsi, 1
    FROM #CmtSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.AmbassadorRequestComment t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  AmbassadorRequestComment inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    PRINT N'=== VERIFY COUNTS ===';
    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_AmbassadorRequest]) AS TmsHeaders,
        (SELECT COUNT(*) FROM Srv.AmbassadorRequest WHERE HtsId <> 0) AS HavayarHeadersSynced,
        (SELECT COUNT(*) FROM Srv.AmbassadorRequest) AS HavayarHeadersAll,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] WHERE AmbassadorRequestId IS NOT NULL) AS TmsComments,
        (SELECT COUNT(*) FROM Srv.AmbassadorRequestComment WHERE HtsId <> 0) AS HavayarCommentsSynced,
        (SELECT COUNT(*) FROM Srv.AmbassadorRequestComment) AS HavayarCommentsAll,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] rc
         WHERE rc.AmbassadorRequestId IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM Srv.AmbassadorRequest h
               WHERE h.HtsId = CAST(rc.AmbassadorRequestId AS BIGINT) AND h.HtsId <> 0
           )) AS SkippedOrphanComments;

    PRINT N'=== NULL / UNMAPPED FKs ===';
    SELECT
        (SELECT COUNT(*) FROM Srv.AmbassadorRequest WHERE HtsId <> 0 AND AmbassadorId IS NULL) AS NullAmbassadorId,
        (SELECT COUNT(*) FROM #HdrSrc WHERE OldAmbassadorId IS NOT NULL AND AmbassadorId IS NULL) AS UnmappedAmbassador,
        (SELECT COUNT(*) FROM #HdrSrc WHERE IsUnmappedCreator = 1) AS UnmappedCreator,
        (SELECT COUNT(*) FROM #CmtSrc WHERE IsUnmappedCreator = 1) AS UnmappedCommentCreator;

    PRINT N'=== STATUS / NEEDDATE ANOMALIES ===';
    SELECT
        (SELECT COUNT(*) FROM Srv.AmbassadorRequest WHERE HtsId <> 0 AND StatusId = 0) AS StatusIdZero,
        (SELECT COUNT(*) FROM Srv.AmbassadorRequest
         WHERE HtsId <> 0
           AND (YEAR(NeedDate) < 1900 OR NeedDateInText LIKE N'0720%')) AS NeedDateAnomalies;

    PRINT N'Sync_SrvAmbassadorRequest_FromTotalSystem completed.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'Sync failed (line %d): %s', 16, 1, @Line, @Err);
END CATCH;
