/*
Backfill_EdmsDocument_PreparedUser_FromHts.sql
One-time / idempotent backfill of Edms.Document.PreparedUserId.

Source of truth (HtsId > 0):
  [TMS].[TotalSystem].[dbo].[Edms_Document].PreparedUser_FK -> Gnr_User -> system.User
  (same user matching as the other Sync_*_FromTotalSystem scripts:
   COALESCE(ActiveDirectoryUsername, Username) = system.User.Username)

Portal-only rows (HtsId = 0, or HtsId not found in HTS), only when PreparedUserId IS NULL:
  Edms.ProjectVpis.ProducerId, else Edms.Document.CreatedById.

HTS rows whose preparer cannot be mapped to a Portal user stay NULL and are listed.
They are never filled with another user.

PreparedUserId is a snapshot on the document: a later change of the VPIS producer
does not change old revisions / the performance reports.

UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i ...
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] not found.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'Edms.Document', N'PreparedUserId') IS NULL
BEGIN
    RAISERROR(N'Edms.Document.PreparedUserId does not exist - apply migration AddActivityHtsIdAndDocumentPreparedUser first.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== User map (HTS User_ID -> system.User.Id) ========== */
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, NewUserId
    INTO #UserMap
    FROM (
        SELECT
            CAST(ou.User_ID AS INT) AS OldUserId,
            nu.Id AS NewUserId,
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

    /* ========== HTS documents (prepared user) ========== */
    IF OBJECT_ID('tempdb..#HtsDoc') IS NOT NULL DROP TABLE #HtsDoc;
    SELECT CAST(Document_ID AS BIGINT) AS HtsId, CAST(PreparedUser_FK AS INT) AS PreparedUser_FK
    INTO #HtsDoc
    FROM [TMS].[TotalSystem].[dbo].[Edms_Document];
    CREATE UNIQUE CLUSTERED INDEX IX_HtsDoc ON #HtsDoc (HtsId);

    /* ========== 1) HtsId > 0: copy HTS PreparedUser_FK ========== */
    UPDATE d
    SET d.PreparedUserId = m.NewUserId
    FROM Edms.Document d
    INNER JOIN #HtsDoc h ON h.HtsId = d.HtsId
    INNER JOIN #UserMap m ON m.OldUserId = h.PreparedUser_FK
    WHERE d.HtsId > 0
      AND (d.PreparedUserId IS NULL OR d.PreparedUserId <> m.NewUserId);
    DECLARE @FromHts INT = @@ROWCOUNT;

    /* ========== 2) Portal-only rows: VPIS producer, else CreatedById ========== */
    UPDATE d
    SET d.PreparedUserId = COALESCE(v.ProducerId, d.CreatedById)
    FROM Edms.Document d
    LEFT JOIN Edms.ProjectVpis v ON v.Id = d.DocumentVpisId
    WHERE d.PreparedUserId IS NULL
      AND (d.HtsId = 0 OR NOT EXISTS (SELECT 1 FROM #HtsDoc h WHERE h.HtsId = d.HtsId))
      AND COALESCE(v.ProducerId, d.CreatedById) IS NOT NULL;
    DECLARE @FromPortal INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    PRINT CONCAT(N'PreparedUserId set from HTS: ', @FromHts);
    PRINT CONCAT(N'PreparedUserId set for Portal-only rows: ', @FromPortal);

    /* ========== Report: rows still NULL ========== */
    SELECT
        CASE WHEN d.HtsId > 0 AND h.HtsId IS NOT NULL THEN N'HTS row, user not mapped'
             ELSE N'Portal-only row, no producer/creator' END AS Reason,
        COUNT(*) AS Docs,
        SUM(CAST(ISNULL(d.HourPrePerson, 0) AS BIGINT)) AS Minutes
    FROM Edms.Document d
    LEFT JOIN #HtsDoc h ON h.HtsId = d.HtsId
    WHERE d.PreparedUserId IS NULL
    GROUP BY CASE WHEN d.HtsId > 0 AND h.HtsId IS NOT NULL THEN N'HTS row, user not mapped'
                  ELSE N'Portal-only row, no producer/creator' END;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
