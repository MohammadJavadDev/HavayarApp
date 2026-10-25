/*
Sync_EpmsVpisType_FromTotalSystem.sql
One-time / idempotent sync: Epms_VpisType (via [TMS]) -> Epms.VpisType

HtsId = Epms_VpisType.VpisType_ID
Title  = Vpis_Type_Title (copied as is, up to 500 chars; longer titles abort the script)

Idempotent UPDATE-then-INSERT on HtsId. Never deletes target rows.
Rows with HtsId = 0 (created in Portal) are never overwritten.
UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i ...
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] not found.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'Epms.VpisType', N'HtsId') IS NULL
BEGIN
    RAISERROR(N'Epms.VpisType.HtsId does not exist - apply migration AddActivityHtsIdAndDocumentPreparedUser first.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @Seed NVARCHAR(80) = N'sync-epms-vpistype';

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        CAST(VpisType_ID AS BIGINT) AS HtsId,
        LTRIM(RTRIM(Vpis_Type_Title)) AS Title
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Epms_VpisType];

    IF EXISTS (SELECT 1 FROM #Src WHERE Title IS NULL OR Title = N'' OR LEN(Title) > 500)
        THROW 51701, N'VpisType title is empty or longer than 500 chars.', 1;

    UPDATE t SET
        t.Title = s.Title,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Epms.VpisType t
    INNER JOIN #Src s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0
      AND (t.Title <> s.Title OR ISNULL(t.IsActive, 0) <> 1);
    PRINT N'  VpisType updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Epms.VpisType
        (HtsId, Title, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
         CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.Title, @Seed, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM #Src s
    WHERE NOT EXISTS (SELECT 1 FROM Epms.VpisType t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  VpisType inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Epms_VpisType]) AS TmsRows,
        (SELECT COUNT(*) FROM Epms.VpisType WHERE HtsId <> 0) AS PortalSynced,
        (SELECT COUNT(*) FROM Epms.VpisType) AS PortalAll;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'Sync failed (line %d): %s', 16, 1, @Line, @Err);
END CATCH;
