/*
Sync_SrvCarRequest_FromTotalSystem.sql
One-time sync: Srv_CarRequest + Srv_RequestComment (CarRequestId IS NOT NULL) via [TMS]
  -> Srv.CarRequest / Srv.CarRequestComment (HavayarApp)

HtsId:
  Header  = Srv_CarRequest.Id
  Comment = Srv_RequestComment.Id
Parent FK on comments: CarRequestId = Srv.CarRequest.Id (via header HtsId).
Do NOT sync Srv_Attachment (no car-request files).
Do NOT insert IsApply / IsOutdoorCarService (HTS-only; not on target).

Mappings:
  CreatedById / ModifiedById: COALESCE(ActiveDirectoryUsername, Username) -> system.User.Username
    + only alias: allahverdi.s missing -> allahvirdi.s (see Sync_SrvTicketHotelRequest / SyncCourse)
    If still unmapped -> use user Id 1; count as UnmappedCreator (do not drop row).
  CostCenterId: Acc_CostCenter.Hamkaran_CostCenter_FK -> Gnr.CostCenter.HamkaranId, else Title; NULL if unmatched
  PassengerId / DriverId: HRM_Personel.Hamkaran_Personel_FK -> Hcm.Personel.HamkaranId, else PrsCode -> Code; NULL if unmatched
  BranchId, TypeId, OutdoorCarServiceId, StatusId: copy HTS lookup ids as-is (StatusId 0 stays 0)
  NeedDate / NeedDateInText / CreatedDateInText / DoneDateTimeInText: copy as stored (no year-778 fix)
  CreatedOnMiladiDateTime <- HTS CreatedDate; CreatedOnShamsiDateTime <- CreatedDateInText when present
  IsActive = 1

Idempotent UPDATE-then-INSERT on HtsId. Do not delete target rows absent from source.
Havayar-only rows (HtsId = 0) are never overwritten.
UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i ...
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Srv.CarRequest', N'U') IS NULL
   OR OBJECT_ID(N'Srv.CarRequestComment', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول Srv.CarRequest / Srv.CarRequestComment وجود ندارند — ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @Seed NVARCHAR(80) = N'sync-srv-carrequest';
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

    /* ========== CostCenter map ========== */
    IF OBJECT_ID('tempdb..#CostCenterMap') IS NOT NULL DROP TABLE #CostCenterMap;
    SELECT
        cc.CostCenter_ID AS OldCostCenterId,
        COALESCE(byHam.Id, byTitle.Id) AS NewCostCenterId
    INTO #CostCenterMap
    FROM [TMS].[TotalSystem].[dbo].[Acc_CostCenter] cc
    OUTER APPLY (
        SELECT TOP (1) c.Id
        FROM Gnr.CostCenter c
        WHERE ISNULL(cc.Hamkaran_CostCenter_FK, 0) <> 0
          AND c.HamkaranId = CAST(cc.Hamkaran_CostCenter_FK AS BIGINT)
        ORDER BY c.Id
    ) byHam
    OUTER APPLY (
        SELECT TOP (1) c.Id
        FROM Gnr.CostCenter c
        WHERE byHam.Id IS NULL
          AND NULLIF(LTRIM(RTRIM(cc.CostCenter_Title)), N'') IS NOT NULL
          AND LTRIM(RTRIM(c.Title)) = LTRIM(RTRIM(cc.CostCenter_Title))
        ORDER BY c.Id
    ) byTitle;
    CREATE UNIQUE CLUSTERED INDEX IX_CostCenterMap ON #CostCenterMap (OldCostCenterId);

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
        CAST(c.Id AS BIGINT) AS HtsId,
        CAST(c.TypeId AS INT) AS TypeId,
        CAST(c.NeedDate AS DATE) AS NeedDate,
        LEFT(NULLIF(LTRIM(RTRIM(c.NeedTime)), N''), 5) AS NeedTime,
        NULLIF(LTRIM(RTRIM(c.NeedDateInText)), N'') AS NeedDateInText,
        CASE WHEN c.PassengerId IS NULL THEN NULL ELSE pmPass.NewPersonelId END AS PassengerId,
        CAST(c.PassengerId AS INT) AS OldPassengerId,
        CAST(c.CountPeople AS SMALLINT) AS CountPeople,
        CASE WHEN c.CostCenterId IS NULL THEN NULL ELSE ccm.NewCostCenterId END AS CostCenterId,
        CAST(c.CostCenterId AS BIGINT) AS OldCostCenterId,
        CASE WHEN c.DriverId IS NULL THEN NULL ELSE pmDrv.NewPersonelId END AS DriverId,
        CAST(c.DriverId AS INT) AS OldDriverId,
        CAST(c.OutdoorCarServiceId AS INT) AS OutdoorCarServiceId,
        NULLIF(LTRIM(RTRIM(c.Destination)), N'') AS Destination,
        NULLIF(LTRIM(RTRIM(c.LastComment)), N'') AS LastComment,
        NULLIF(LTRIM(RTRIM(c.Description)), N'') AS Description,
        CAST(c.StatusId AS SMALLINT) AS StatusId,
        CAST(c.WaitingTime AS DECIMAL(18, 1)) AS WaitingTime,
        NULLIF(LTRIM(RTRIM(c.DoneDateTimeIntext)), N'') AS DoneDateTimeInText,
        CAST(c.BranchId AS INT) AS BranchId,
        NULLIF(LTRIM(RTRIM(c.CreatedDateInText)), N'') AS CreatedDateInText,
        CAST(c.CreatedUserId AS INT) AS OldCreatedUserId,
        CASE WHEN umc.NewUserId IS NULL THEN 1 ELSE 0 END AS IsUnmappedCreator,
        ISNULL(umc.NewUserId, @FallbackUserId) AS CreatedById,
        ISNULL(umc.NewUserName, ISNULL(@FallbackUserName, @Seed)) AS CreatedByName,
        CAST(c.CreatedDate AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(c.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #HdrSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_CarRequest] c
    LEFT JOIN #CostCenterMap ccm ON ccm.OldCostCenterId = c.CostCenterId
    LEFT JOIN #PersonelMap pmPass ON c.PassengerId IS NOT NULL AND pmPass.OldPersonelId = CAST(c.PassengerId AS INT)
    LEFT JOIN #PersonelMap pmDrv ON c.DriverId IS NOT NULL AND pmDrv.OldPersonelId = CAST(c.DriverId AS INT)
    LEFT JOIN #UserMap umc ON umc.OldUserId = CAST(c.CreatedUserId AS INT);

    PRINT N'Syncing CarRequest headers (unmapped passenger/driver/costcenter -> NULL; unmapped creator -> Id 1)...';

    /* ========== Headers: UPDATE then INSERT (skip HtsId = 0 targets) ========== */
    UPDATE t SET
        t.TypeId = s.TypeId,
        t.NeedDate = s.NeedDate,
        t.NeedTime = s.NeedTime,
        t.NeedDateInText = s.NeedDateInText,
        t.PassengerId = s.PassengerId,
        t.CountPeople = s.CountPeople,
        t.CostCenterId = s.CostCenterId,
        t.DriverId = s.DriverId,
        t.OutdoorCarServiceId = s.OutdoorCarServiceId,
        t.Destination = s.Destination,
        t.LastComment = s.LastComment,
        t.Description = s.Description,
        t.StatusId = s.StatusId,
        t.WaitingTime = s.WaitingTime,
        t.DoneDateTimeInText = s.DoneDateTimeInText,
        t.BranchId = s.BranchId,
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
    FROM Srv.CarRequest t
    INNER JOIN #HdrSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  CarRequest updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.CarRequest (
        HtsId, TypeId, NeedDate, NeedTime, NeedDateInText, PassengerId, CountPeople, CostCenterId,
        DriverId, OutdoorCarServiceId, Destination, LastComment, Description, StatusId, WaitingTime,
        DoneDateTimeInText, BranchId, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.TypeId, s.NeedDate, s.NeedTime, s.NeedDateInText, s.PassengerId, s.CountPeople, s.CostCenterId,
        s.DriverId, s.OutdoorCarServiceId, s.Destination, s.LastComment, s.Description, s.StatusId, s.WaitingTime,
        s.DoneDateTimeInText, s.BranchId, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, @Seed, @Now, @NowShamsi, 1
    FROM #HdrSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.CarRequest t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  CarRequest inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ========== Comments (only CarRequestId IS NOT NULL; skip orphan parents) ========== */
    IF OBJECT_ID('tempdb..#CmtSrc') IS NOT NULL DROP TABLE #CmtSrc;
    SELECT
        CAST(rc.Id AS BIGINT) AS HtsId,
        hdr.Id AS CarRequestId,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(rc.Comment)), N''), N''), 256) AS Comment,
        CAST(rc.StatusId AS SMALLINT) AS StatusId,
        NULLIF(LTRIM(RTRIM(rc.ApproximateExecutionDate)), N'') AS ApproximateExecutionDate,
        NULLIF(LTRIM(RTRIM(rc.CreatedDateInText)), N'') AS CreatedDateInText,
        CAST(rc.CreatedUserId AS INT) AS OldCreatedUserId,
        CASE WHEN um.NewUserId IS NULL THEN 1 ELSE 0 END AS IsUnmappedCreator,
        ISNULL(um.NewUserId, @FallbackUserId) AS CreatedById,
        ISNULL(um.NewUserName, ISNULL(@FallbackUserName, @Seed)) AS CreatedByName,
        CAST(rc.CreatedDate AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(rc.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #CmtSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] rc
    INNER JOIN Srv.CarRequest hdr
        ON hdr.HtsId = CAST(rc.CarRequestId AS BIGINT)
       AND hdr.HtsId <> 0
    LEFT JOIN #UserMap um ON um.OldUserId = CAST(rc.CreatedUserId AS INT)
    WHERE rc.CarRequestId IS NOT NULL;

    UPDATE t SET
        t.CarRequestId = s.CarRequestId,
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
    FROM Srv.CarRequestComment t
    INNER JOIN #CmtSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  CarRequestComment updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.CarRequestComment (
        HtsId, CarRequestId, Comment, StatusId, ApproximateExecutionDate, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.CarRequestId, s.Comment, s.StatusId, s.ApproximateExecutionDate, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, @Seed, @Now, @NowShamsi, 1
    FROM #CmtSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.CarRequestComment t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  CarRequestComment inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    PRINT N'=== VERIFY COUNTS ===';
    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_CarRequest]) AS TmsHeaders,
        (SELECT COUNT(*) FROM Srv.CarRequest WHERE HtsId <> 0) AS HavayarHeadersSynced,
        (SELECT COUNT(*) FROM Srv.CarRequest) AS HavayarHeadersAll,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] WHERE CarRequestId IS NOT NULL) AS TmsComments,
        (SELECT COUNT(*) FROM Srv.CarRequestComment WHERE HtsId <> 0) AS HavayarCommentsSynced,
        (SELECT COUNT(*) FROM Srv.CarRequestComment) AS HavayarCommentsAll,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] rc
         WHERE rc.CarRequestId IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM Srv.CarRequest h
               WHERE h.HtsId = CAST(rc.CarRequestId AS BIGINT) AND h.HtsId <> 0
           )) AS SkippedOrphanComments;

    PRINT N'=== NULL / UNMAPPED FKs ===';
    SELECT
        (SELECT COUNT(*) FROM Srv.CarRequest WHERE HtsId <> 0 AND PassengerId IS NULL) AS NullPassengerId,
        (SELECT COUNT(*) FROM #HdrSrc WHERE OldPassengerId IS NOT NULL AND PassengerId IS NULL) AS UnmappedPassenger,
        (SELECT COUNT(*) FROM Srv.CarRequest WHERE HtsId <> 0 AND DriverId IS NULL) AS NullDriverId,
        (SELECT COUNT(*) FROM #HdrSrc WHERE OldDriverId IS NOT NULL AND DriverId IS NULL) AS UnmappedDriver,
        (SELECT COUNT(*) FROM Srv.CarRequest WHERE HtsId <> 0 AND CostCenterId IS NULL) AS NullCostCenterId,
        (SELECT COUNT(*) FROM #HdrSrc WHERE OldCostCenterId IS NOT NULL AND CostCenterId IS NULL) AS UnmappedCostCenter,
        (SELECT COUNT(*) FROM #HdrSrc WHERE IsUnmappedCreator = 1) AS UnmappedCreator,
        (SELECT COUNT(*) FROM #CmtSrc WHERE IsUnmappedCreator = 1) AS UnmappedCommentCreator;

    PRINT N'=== STATUS / NEEDDATE ANOMALIES ===';
    SELECT
        (SELECT COUNT(*) FROM Srv.CarRequest WHERE HtsId <> 0 AND StatusId = 0) AS StatusIdZero,
        (SELECT COUNT(*) FROM Srv.CarRequest
         WHERE HtsId <> 0
           AND (YEAR(NeedDate) < 1900 OR NeedDateInText LIKE N'778%')) AS NeedDateAnomalies;

    PRINT N'Sync_SrvCarRequest_FromTotalSystem completed.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'Sync failed (line %d): %s', 16, 1, @Line, @Err);
END CATCH;
