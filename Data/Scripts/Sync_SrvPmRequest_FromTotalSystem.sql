/*
Sync_SrvPmRequest_FromTotalSystem.sql
One-time sync: Srv_PmRequest + Srv_RequestComment (PmRequestId IS NOT NULL)
  + Srv_Attachment (PmRequestId IS NOT NULL) via [TMS]
  -> Srv.PmRequest / Srv.PmRequestComment / Srv.PmRequestAttachment

HtsId:
  Header     = Srv_PmRequest.Id
  Comment    = Srv_RequestComment.Id
  Attachment = Srv_Attachment.Id
Parent FK: PmRequestId = Srv.PmRequest.Id (via header HtsId).

Do NOT sync NeedDate / NeedTime / BranchId / IsApply (always NULL in HTS; not on target).

Mappings:
  CreatedById: COALESCE(ActiveDirectoryUsername, Username) -> system.User.Username
    + allahverdi.s -> allahvirdi.s; unmapped -> Id 1
  OrganizationUnitId: HRM_OrgUnit.Hamkaran_Unit_FK -> Hrm.OrgUnit.HamkaranUnitId only
    (no title fallback — titles collide). Else NULL + CreatedOrganizationUnitTitle = HTS title.
  TypeId / StatusId: copy as-is
  ApproximateExecutionDate / LastComment / RequestText / DoneDateTimeInText / CreatedDateInText: copy
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

IF OBJECT_ID(N'Srv.PmRequest', N'U') IS NULL
   OR OBJECT_ID(N'Srv.PmRequestComment', N'U') IS NULL
   OR OBJECT_ID(N'Srv.PmRequestAttachment', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول Srv.PmRequest / Comment / Attachment وجود ندارند — ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @Seed NVARCHAR(80) = N'sync-srv-pmrequest';
DECLARE @FallbackUserId BIGINT = 1;
DECLARE @FallbackUserName NVARCHAR(150) =
(
    SELECT TOP (1) COALESCE(NULLIF(LTRIM(RTRIM(NameFa)), N''), Name, Username)
    FROM [system].[User]
    WHERE Id = @FallbackUserId
);

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== User map ========== */
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

    /* ========== OrgUnit map: Hamkaran_Unit_FK only (no title fallback) ========== */
    IF OBJECT_ID('tempdb..#OrgUnitMap') IS NOT NULL DROP TABLE #OrgUnitMap;
    SELECT
        hrm.OrgUnit_ID AS OldOrgUnitId,
        byHam.Id AS NewOrgUnitId,
        NULLIF(LTRIM(RTRIM(hrm.OrgUnit_Title)), N'') AS HtsTitle
    INTO #OrgUnitMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hrm
    OUTER APPLY (
        SELECT TOP (1) o.Id
        FROM Hrm.OrgUnit o
        WHERE hrm.Hamkaran_Unit_FK IS NOT NULL
          AND ISNULL(hrm.Hamkaran_Unit_FK, 0) <> 0
          AND o.HamkaranUnitId = CAST(hrm.Hamkaran_Unit_FK AS BIGINT)
        ORDER BY o.Id
    ) byHam;
    CREATE UNIQUE CLUSTERED INDEX IX_OrgUnitMap ON #OrgUnitMap (OldOrgUnitId);

    /* ========== Header staging ========== */
    IF OBJECT_ID('tempdb..#HdrSrc') IS NOT NULL DROP TABLE #HdrSrc;
    SELECT
        CAST(c.Id AS BIGINT) AS HtsId,
        CAST(c.TypeId AS INT) AS TypeId,
        NULLIF(LTRIM(RTRIM(c.RequestText)), N'') AS RequestText,
        NULLIF(LTRIM(RTRIM(c.LastComment)), N'') AS LastComment,
        NULLIF(LTRIM(RTRIM(c.ApproximateExecutionDate)), N'') AS ApproximateExecutionDate,
        CAST(c.StatusId AS SMALLINT) AS StatusId,
        NULLIF(LTRIM(RTRIM(c.DoneDateTimeIntext)), N'') AS DoneDateTimeInText,
        CASE
            WHEN c.CreatedOrganizationUnitId IS NULL THEN NULL
            ELSE oum.NewOrgUnitId
        END AS OrganizationUnitId,
        CASE
            WHEN c.CreatedOrganizationUnitId IS NULL THEN NULL
            WHEN oum.NewOrgUnitId IS NULL THEN oum.HtsTitle
            ELSE NULL
        END AS CreatedOrganizationUnitTitle,
        CAST(c.CreatedOrganizationUnitId AS INT) AS OldOrgUnitId,
        NULLIF(LTRIM(RTRIM(c.CreatedDateInText)), N'') AS CreatedDateInText,
        CAST(c.CreatedUserId AS INT) AS OldCreatedUserId,
        CASE WHEN umc.NewUserId IS NULL THEN 1 ELSE 0 END AS IsUnmappedCreator,
        ISNULL(umc.NewUserId, @FallbackUserId) AS CreatedById,
        ISNULL(umc.NewUserName, ISNULL(@FallbackUserName, @Seed)) AS CreatedByName,
        CAST(c.CreatedDate AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(c.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #HdrSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_PmRequest] c
    LEFT JOIN #OrgUnitMap oum ON oum.OldOrgUnitId = c.CreatedOrganizationUnitId
    LEFT JOIN #UserMap umc ON umc.OldUserId = CAST(c.CreatedUserId AS INT);

    PRINT N'Syncing PmRequest headers...';

    UPDATE t SET
        t.TypeId = s.TypeId,
        t.RequestText = s.RequestText,
        t.LastComment = s.LastComment,
        t.ApproximateExecutionDate = s.ApproximateExecutionDate,
        t.StatusId = s.StatusId,
        t.DoneDateTimeInText = s.DoneDateTimeInText,
        t.OrganizationUnitId = s.OrganizationUnitId,
        t.CreatedOrganizationUnitTitle = s.CreatedOrganizationUnitTitle,
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
    FROM Srv.PmRequest t
    INNER JOIN #HdrSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  PmRequest updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.PmRequest (
        HtsId, TypeId, RequestText, LastComment, ApproximateExecutionDate, StatusId,
        DoneDateTimeInText, OrganizationUnitId, CreatedOrganizationUnitTitle, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.TypeId, s.RequestText, s.LastComment, s.ApproximateExecutionDate, s.StatusId,
        s.DoneDateTimeInText, s.OrganizationUnitId, s.CreatedOrganizationUnitTitle, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, @Seed, @Now, @NowShamsi, 1
    FROM #HdrSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.PmRequest t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  PmRequest inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ========== Comments ========== */
    IF OBJECT_ID('tempdb..#CmtSrc') IS NOT NULL DROP TABLE #CmtSrc;
    SELECT
        CAST(rc.Id AS BIGINT) AS HtsId,
        hdr.Id AS PmRequestId,
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
    INNER JOIN Srv.PmRequest hdr
        ON hdr.HtsId = CAST(rc.PmRequestId AS BIGINT)
       AND hdr.HtsId <> 0
    LEFT JOIN #UserMap um ON um.OldUserId = CAST(rc.CreatedUserId AS INT)
    WHERE rc.PmRequestId IS NOT NULL;

    UPDATE t SET
        t.PmRequestId = s.PmRequestId,
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
    FROM Srv.PmRequestComment t
    INNER JOIN #CmtSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  PmRequestComment updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.PmRequestComment (
        HtsId, PmRequestId, Comment, StatusId, ApproximateExecutionDate, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.PmRequestId, s.Comment, s.StatusId, s.ApproximateExecutionDate, s.CreatedDateInText,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, @Seed, @Now, @NowShamsi, 1
    FROM #CmtSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.PmRequestComment t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  PmRequestComment inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ========== Attachments ========== */
    IF OBJECT_ID('tempdb..#AttSrc') IS NOT NULL DROP TABLE #AttSrc;
    SELECT
        CAST(a.Id AS BIGINT) AS HtsId,
        hdr.Id AS PmRequestId,
        NULLIF(LTRIM(RTRIM(a.Attachment_FileName)), N'') AS FileName,
        a.Attachment_FileContent AS FileContent,
        CAST(a.Attachment_FileSize AS BIGINT) AS FileSize,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(a.Comment)), N''), N''), 4000) AS Comment,
        LEFT(NULLIF(LTRIM(RTRIM(a.CreatedDate)), N''), 10) AS CreatedDate,
        LEFT(NULLIF(LTRIM(RTRIM(a.CreatedTime)), N''), 5) AS CreatedTime,
        CAST(a.CreatedUserId AS INT) AS OldCreatedUserId,
        CASE WHEN um.NewUserId IS NULL THEN 1 ELSE 0 END AS IsUnmappedCreator,
        ISNULL(um.NewUserId, @FallbackUserId) AS CreatedById,
        ISNULL(um.NewUserName, ISNULL(@FallbackUserName, @Seed)) AS CreatedByName,
        @Now AS CreatedOnMiladiDateTime,
        LEFT(NULLIF(LTRIM(RTRIM(a.CreatedDate)), N''), 10) AS CreatedOnShamsiDateTime
    INTO #AttSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_Attachment] a
    INNER JOIN Srv.PmRequest hdr
        ON hdr.HtsId = CAST(a.PmRequestId AS BIGINT)
       AND hdr.HtsId <> 0
    LEFT JOIN #UserMap um ON um.OldUserId = CAST(a.CreatedUserId AS INT)
    WHERE a.PmRequestId IS NOT NULL;

    UPDATE t SET
        t.PmRequestId = s.PmRequestId,
        t.FileName = s.FileName,
        t.FileContent = s.FileContent,
        t.FileSize = s.FileSize,
        t.Comment = s.Comment,
        t.CreatedDate = s.CreatedDate,
        t.CreatedTime = s.CreatedTime,
        t.CreatedById = s.CreatedById,
        t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.ModifiedById = s.CreatedById,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Srv.PmRequestAttachment t
    INNER JOIN #AttSrc s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  PmRequestAttachment updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.PmRequestAttachment (
        HtsId, PmRequestId, FileName, FileContent, FileSize, Comment, CreatedDate, CreatedTime,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.PmRequestId, s.FileName, s.FileContent, s.FileSize, s.Comment, s.CreatedDate, s.CreatedTime,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, @Seed, @Now, @NowShamsi, 1
    FROM #AttSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.PmRequestAttachment t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  PmRequestAttachment inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    PRINT N'=== VERIFY COUNTS ===';
    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_PmRequest]) AS TmsHeaders,
        (SELECT COUNT(*) FROM Srv.PmRequest WHERE HtsId <> 0) AS HavayarHeadersSynced,
        (SELECT COUNT(*) FROM Srv.PmRequest) AS HavayarHeadersAll,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_RequestComment] WHERE PmRequestId IS NOT NULL) AS TmsComments,
        (SELECT COUNT(*) FROM Srv.PmRequestComment WHERE HtsId <> 0) AS HavayarCommentsSynced,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_Attachment] WHERE PmRequestId IS NOT NULL) AS TmsAttachments,
        (SELECT COUNT(*) FROM Srv.PmRequestAttachment WHERE HtsId <> 0) AS HavayarAttachmentsSynced;

    PRINT N'=== UNMAPPED ===';
    SELECT
        (SELECT COUNT(*) FROM #HdrSrc WHERE IsUnmappedCreator = 1) AS UnmappedCreator,
        (SELECT COUNT(*) FROM #CmtSrc WHERE IsUnmappedCreator = 1) AS UnmappedCommentCreator,
        (SELECT COUNT(*) FROM #AttSrc WHERE IsUnmappedCreator = 1) AS UnmappedAttachmentCreator,
        (SELECT COUNT(*) FROM #HdrSrc WHERE OldOrgUnitId IS NOT NULL AND OrganizationUnitId IS NULL) AS UnmappedOrgUnit,
        (SELECT COUNT(*) FROM Srv.PmRequest WHERE HtsId <> 0 AND OrganizationUnitId IS NULL AND CreatedOrganizationUnitTitle IS NOT NULL) AS SnapshotOrgTitleRows;

    PRINT N'Sync_SrvPmRequest_FromTotalSystem completed.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'Sync failed (line %d): %s', 16, 1, @Line, @Err);
END CATCH;
