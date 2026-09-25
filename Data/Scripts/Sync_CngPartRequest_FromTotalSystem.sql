/*
Sync_CngPartRequest_FromTotalSystem.sql
همگام‌سازی Cng.PartRequest + Cng.PartRequestItem از [TMS].[TotalSystem].
HtsId = CAST(old.Id AS BIGINT). Part via Inv.Part.HtsId؛ Station via Cng.StationInfo.HtsId.
کاربر CreatedBy/ModifiedBy از #UserMap (Username / AD / Email).
LastStatusId و UsagePlaceId مقدار ۰ را حفظ می‌کنند (CAST به INT) — بازنویسی وضعیت انجام نمی‌شود.
پیوست: فقط متادیتا؛ FileId در INSERT خالی؛ در UPDATE اگر قبلاً مقدار داشته باشد دست نخورده می‌ماند.
کپی باینری: Sync_CngPartRequest_CopyFiles.ps1
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

IF OBJECT_ID(N'Cng.PartRequest', N'U') IS NULL OR OBJECT_ID(N'Cng.PartRequestItem', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول Cng.PartRequest / PartRequestItem وجود ندارند.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-cng-partrequest';

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
    PRINT N'=== Cng.PartRequest ===';
    IF OBJECT_ID('tempdb..#HdrSrc') IS NOT NULL DROP TABLE #HdrSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        NULLIF(LTRIM(RTRIM(s.RequestDescription)), N'') AS RequestDescription,
        NULLIF(LTRIM(RTRIM(s.AttachmentFileName)), N'') AS AttachmentFileName,
        s.AttachmentFileSize,
        NULLIF(LTRIM(RTRIM(s.AttachmentFilePath)), N'') AS AttachmentFilePath,
        CAST(s.LastStatusId AS INT) AS LastStatusId,
        NULLIF(LTRIM(RTRIM(s.LastStatusComment)), N'') AS LastStatusComment,
        NULLIF(LTRIM(RTRIM(s.InvoiceNumber)), N'') AS InvoiceNumber,
        CAST(s.ImmediateSending AS BIT) AS ImmediateSending,
        NULLIF(LTRIM(RTRIM(s.SendingLocation)), N'') AS SendingLocation,
        NULLIF(LTRIM(RTRIM(s.Receiver)), N'') AS Receiver,
        NULLIF(LTRIM(RTRIM(s.Comment)), N'') AS Comment,
        CAST(s.CreatedDate AS datetime2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        umCreate.NewUserId AS CreatedById,
        umCreate.NewUserName AS CreatedByName,
        CAST(s.UpdatedDate AS datetime2) AS ModifiedDateMiladiDateTime,
        NULLIF(LTRIM(RTRIM(s.UpdatedDateInText)), N'') AS ModifiedDateShamsiDateTime,
        umMod.NewUserId AS ModifiedById,
        umMod.NewUserName AS ModifiedByName,
        CASE WHEN s.CreatedUserId IS NOT NULL AND umCreate.NewUserId IS NULL THEN 1 ELSE 0 END AS UnmappedCreator,
        CASE WHEN s.UpdatedUserId IS NOT NULL AND umMod.NewUserId IS NULL THEN 1 ELSE 0 END AS UnmappedModifier
    INTO #HdrSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_PartRequest] s
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = CAST(s.CreatedUserId AS INT)
    LEFT JOIN #UserMap umMod ON umMod.OldUserId = CAST(s.UpdatedUserId AS INT);

    DECLARE @UnmappedCreator INT = (SELECT COUNT(*) FROM #HdrSrc WHERE UnmappedCreator = 1);
    DECLARE @UnmappedModifier INT = (SELECT COUNT(*) FROM #HdrSrc WHERE UnmappedModifier = 1);
    DECLARE @HdrCnt INT = (SELECT COUNT(*) FROM #HdrSrc);
    PRINT N'  Headers source: ' + CAST(@HdrCnt AS nvarchar(20));
    PRINT N'  Unmapped CreatedBy (fallback seed): ' + CAST(@UnmappedCreator AS nvarchar(20));
    PRINT N'  Unmapped ModifiedBy (fallback seed): ' + CAST(@UnmappedModifier AS nvarchar(20));

    UPDATE t SET
        t.RequestDescription = s.RequestDescription,
        t.AttachmentFileName = s.AttachmentFileName,
        t.AttachmentFileSize = s.AttachmentFileSize,
        t.AttachmentFilePath = s.AttachmentFilePath,
        /* keep FileId if already set — binary copy is a separate step */
        t.FileId = t.FileId,
        t.LastStatusId = s.LastStatusId,
        t.LastStatusComment = s.LastStatusComment,
        t.InvoiceNumber = s.InvoiceNumber,
        t.ImmediateSending = s.ImmediateSending,
        t.SendingLocation = s.SendingLocation,
        t.Receiver = s.Receiver,
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
    FROM Cng.PartRequest t
    INNER JOIN #HdrSrc s ON s.HtsId = t.HtsId;
    PRINT N'  PartRequest updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.PartRequest (
        HtsId, RequestDescription, AttachmentFileName, AttachmentFileSize, AttachmentFilePath,
        FileId, LastStatusId, LastStatusComment, InvoiceNumber, ImmediateSending,
        SendingLocation, Receiver, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.RequestDescription, s.AttachmentFileName, s.AttachmentFileSize, s.AttachmentFilePath,
        NULL, s.LastStatusId, s.LastStatusComment, s.InvoiceNumber, s.ImmediateSending,
        s.SendingLocation, s.Receiver, s.Comment,
        ISNULL(s.CreatedById, 1), ISNULL(s.CreatedByName, @Seed),
        ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
        ISNULL(s.ModifiedById, 1), ISNULL(s.ModifiedByName, @Seed),
        ISNULL(s.ModifiedDateMiladiDateTime, @Now), ISNULL(s.ModifiedDateShamsiDateTime, @NowShamsi),
        1
    FROM #HdrSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.PartRequest t WHERE t.HtsId = s.HtsId);
    PRINT N'  PartRequest inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    /* ========== Items ========== */
    PRINT N'=== Cng.PartRequestItem ===';
    IF OBJECT_ID('tempdb..#ItemSrc') IS NOT NULL DROP TABLE #ItemSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        pr.Id AS PartRequestId,
        CAST(s.UsagePlaceId AS INT) AS UsagePlaceId,
        st.Id AS StationId,
        p.Id AS PartId,
        CAST(ISNULL(s.Mount, 0) AS DECIMAL(18, 4)) AS Mount,
        NULLIF(LTRIM(RTRIM(s.Comment)), N'') AS Comment,
        CASE WHEN pr.Id IS NULL THEN 1 ELSE 0 END AS SkipParent,
        CASE WHEN p.Id IS NULL THEN 1 ELSE 0 END AS SkipPart,
        CASE WHEN s.StationId IS NOT NULL AND ISNULL(s.StationId, 0) <> 0 AND st.Id IS NULL THEN 1 ELSE 0 END AS UnmappedStation
    INTO #ItemSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_PartRequestItem] s
    LEFT JOIN Cng.PartRequest pr ON pr.HtsId = CAST(s.PartRequestId AS BIGINT)
    LEFT JOIN Inv.Part p ON p.HtsId = CAST(s.PartId AS BIGINT) AND ISNULL(p.HtsId, 0) <> 0
    LEFT JOIN Cng.StationInfo st ON st.HtsId = CAST(s.StationId AS BIGINT) AND ISNULL(st.HtsId, 0) <> 0;

    DECLARE @SkipParent INT = (SELECT COUNT(*) FROM #ItemSrc WHERE SkipParent = 1);
    DECLARE @SkipPart INT = (SELECT COUNT(*) FROM #ItemSrc WHERE SkipPart = 1);
    DECLARE @UnmappedStation INT = (SELECT COUNT(*) FROM #ItemSrc WHERE UnmappedStation = 1);
    DECLARE @ItemCnt INT = (SELECT COUNT(*) FROM #ItemSrc);
    PRINT N'  Items source: ' + CAST(@ItemCnt AS nvarchar(20));
    PRINT N'  Items skipped (parent missing): ' + CAST(@SkipParent AS nvarchar(20));
    PRINT N'  Items skipped (part unmapped): ' + CAST(@SkipPart AS nvarchar(20));
    PRINT N'  Unmapped Station (StationId=NULL): ' + CAST(@UnmappedStation AS nvarchar(20));

    IF @SkipPart > 0 OR @SkipParent > 0
    BEGIN
        PRINT N'  Sample skipped item HTS Ids:';
        SELECT TOP 20 HtsId, SkipParent, SkipPart, UnmappedStation
        FROM #ItemSrc
        WHERE SkipParent = 1 OR SkipPart = 1
        ORDER BY HtsId;
    END

    IF OBJECT_ID('tempdb..#ItemMapped') IS NOT NULL DROP TABLE #ItemMapped;
    SELECT * INTO #ItemMapped FROM #ItemSrc WHERE SkipParent = 0 AND SkipPart = 0;

    UPDATE t SET
        t.PartRequestId = s.PartRequestId,
        t.UsagePlaceId = s.UsagePlaceId,
        t.StationId = s.StationId,
        t.PartId = s.PartId,
        t.Mount = s.Mount,
        t.Comment = s.Comment,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.PartRequestItem t
    INNER JOIN #ItemMapped s ON s.HtsId = t.HtsId;
    PRINT N'  PartRequestItem updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.PartRequestItem (
        HtsId, PartRequestId, UsagePlaceId, StationId, PartId, Mount, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.PartRequestId, s.UsagePlaceId, s.StationId, s.PartId, s.Mount, s.Comment,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi, 1
    FROM #ItemMapped s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.PartRequestItem t WHERE t.HtsId = s.HtsId);
    PRINT N'  PartRequestItem inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== Attachments: metadata synced; FileId left for CopyFiles.ps1 ===';
    DECLARE @NeedFile INT = (
        SELECT COUNT(*) FROM Cng.PartRequest
        WHERE FileId IS NULL
          AND AttachmentFilePath IS NOT NULL
          AND LTRIM(RTRIM(AttachmentFilePath)) <> N''
    );
    PRINT N'  Headers needing FileEntity copy: ' + CAST(@NeedFile AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Sync_CngPartRequest_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT N'PartRequest' AS T, COUNT(*) AS Cnt FROM Cng.PartRequest
UNION ALL SELECT N'PartRequestItem', COUNT(*) FROM Cng.PartRequestItem
UNION ALL SELECT N'PartRequest_NeedFile', COUNT(*) FROM Cng.PartRequest
    WHERE FileId IS NULL AND AttachmentFilePath IS NOT NULL AND LTRIM(RTRIM(AttachmentFilePath)) <> N''
UNION ALL SELECT N'HTS_PartRequest', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_PartRequest]
UNION ALL SELECT N'HTS_PartRequestItem', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_PartRequestItem];
