/*
Sync_EdmsTransmital_FromTotalSystem.sql

One-shot / idempotent sync:
  TotalSystem.dbo.Edms_Transmital + Edms_Transmital_Document (via linked server [TMS])
  -> HavayarApp.Edms.Transmital

Grain: one target row per HTS Edms_Transmital_Document (new model requires DocumentId).
HtsId = Transmital_Document_ID (unique per flattened row). Parent Transmital_ID is not stored separately.

Mappings:
  ProjectId           = Edms.Project.Id WHERE Project.HtsId = Project_FK
  DocumentId          = Edms.Document.Id WHERE Document.HtsId = Document_FK
  Number              = Transmital_No
  DefinedByUserNumber = UserDefinedNumber
  Comments            = Transmital_Comment
  IsForReplySheet     = IsForReplySheet
  EmployerRelated     = IsSendToEmployer
  DeliveryDateToEmployer = SendToEmployer_ShamiDate (HTS typo Shami)
  CreatedById/Name    = Gnr_User.Username -> system.User.Username
  CreatedOnShamsi     = CreatedDate + ' ' + CreatedTime
  CreatedOnMiladi     = dbo.fn_ShamsiToMiladiDateTime / Date
  IsActive            = 1
  AttachmentId        = unchanged here; files are linked by Sync_EdmsTransmital_Attachments_FromTotalSystem.sql

Not migrated by this script:
  - 193 HTS parents with no Edms_Transmital_Document (DocumentId required)
  - Existing app-created rows with HtsId=0 are left untouched
  - Attachment bytes stay on the HTS UNC path; the attachment script points FileEntity at that path

Idempotent UPDATE/INSERT on HtsId. Encoding: UTF-8 with BOM.
  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Sync_EdmsTransmital_FromTotalSystem.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد. ابتدا Data/Scripts/CreateLinkedServer_TMS.sql را اجرا کنید.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Edms.Transmital', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Edms.Transmital وجود ندارد.', 16, 1);
    RETURN;
END;

/* Ensure HtsId column (separate batch — new column cannot be referenced in same compile batch) */
IF COL_LENGTH(N'Edms.Transmital', N'HtsId') IS NULL
BEGIN
    ALTER TABLE [Edms].[Transmital] ADD [HtsId] bigint NOT NULL CONSTRAINT [DF_Edms_Transmital_HtsId] DEFAULT CAST(0 AS bigint);
    PRINT N'Added Edms.Transmital.HtsId';
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Edms_Transmital_HtsId'
      AND object_id = OBJECT_ID(N'Edms.Transmital'))
BEGIN
    CREATE UNIQUE INDEX [IX_Edms_Transmital_HtsId] ON [Edms].[Transmital]([HtsId])
    WHERE [HtsId] <> CAST(0 AS bigint);
    PRINT N'Created IX_Edms_Transmital_HtsId';
END;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Seed NVARCHAR(150) = N'sync-edms-transmital-hts';
DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) =
    CASE WHEN OBJECT_ID(N'dbo.ToShamsiDate', N'FN') IS NOT NULL
         THEN dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108)
         ELSE CONVERT(NVARCHAR(30), @Now, 120)
    END;

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== User map ========== */
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT
        CAST(ou.User_ID AS INT) AS OldUserId,
        nu.Id AS NewUserId,
        COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username) AS NewUserName
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu ON nu.Username = ou.Username;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    /* ========== Flattened source (one row per Transmital_Document) ========== */
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        CAST(td.Transmital_Document_ID AS BIGINT) AS HtsId,
        CAST(t.Transmital_ID AS BIGINT) AS HtsTransmitalId,
        p.Id AS ProjectId,
        d.Id AS DocumentId,
        NULLIF(LTRIM(RTRIM(t.Transmital_No)), N'') AS Number,
        NULLIF(LTRIM(RTRIM(t.UserDefinedNumber)), N'') AS DefinedByUserNumber,
        NULLIF(LTRIM(RTRIM(t.Transmital_Comment)), N'') AS Comments,
        CAST(t.IsForReplySheet AS BIT) AS IsForReplySheet,
        CAST(td.IsSendToEmployer AS BIT) AS EmployerRelated,
        NULLIF(LTRIM(RTRIM(td.SendToEmployer_ShamiDate)), N'') AS DeliveryDateToEmployer,
        um.NewUserId AS CreatedById,
        um.NewUserName AS CreatedByName,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(t.CreatedDate)), N'') IS NULL THEN NULL
            WHEN NULLIF(LTRIM(RTRIM(t.CreatedTime)), N'') IS NOT NULL
                THEN dbo.fn_ShamsiToMiladiDateTime(LTRIM(RTRIM(t.CreatedDate)), LTRIM(RTRIM(t.CreatedTime)), N'/', N':')
            ELSE CAST(dbo.fn_ShamsiToMiladiDate(LTRIM(RTRIM(t.CreatedDate)), N'/') AS DATETIME2)
        END AS CreatedOnMiladiDateTime,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(t.CreatedDate)), N'') IS NULL THEN NULL
            WHEN NULLIF(LTRIM(RTRIM(t.CreatedTime)), N'') IS NOT NULL
                THEN LTRIM(RTRIM(t.CreatedDate)) + N' ' + LTRIM(RTRIM(t.CreatedTime))
            ELSE LTRIM(RTRIM(t.CreatedDate))
        END AS CreatedOnShamsiDateTime
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Edms_Transmital] t
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Transmital_Document] td
        ON td.Transmital_FK = t.Transmital_ID
    INNER JOIN Edms.Project p
        ON p.HtsId = CAST(t.Project_FK AS BIGINT) AND p.HtsId <> 0
    INNER JOIN Edms.Document d
        ON d.HtsId = CAST(td.Document_FK AS BIGINT) AND d.HtsId <> 0
    LEFT JOIN #UserMap um
        ON um.OldUserId = CAST(t.CreatedUser_FK AS INT);

    CREATE UNIQUE CLUSTERED INDEX IX_Src ON #Src (HtsId);

    DECLARE @SrcCnt INT = (SELECT COUNT(*) FROM #Src);
    DECLARE @UnmappedUser INT = (SELECT COUNT(*) FROM #Src WHERE CreatedById IS NULL);
    PRINT N'Source document-links insertable: ' + CAST(@SrcCnt AS NVARCHAR(20));
    PRINT N'Unmapped CreatedById rows (will keep NULL): ' + CAST(@UnmappedUser AS NVARCHAR(20));

    DECLARE @ParentsNoDoc INT;
    SELECT @ParentsNoDoc = COUNT(*)
    FROM [TMS].[TotalSystem].[dbo].[Edms_Transmital] t
    WHERE NOT EXISTS (
        SELECT 1 FROM [TMS].[TotalSystem].[dbo].[Edms_Transmital_Document] td
        WHERE td.Transmital_FK = t.Transmital_ID);
    PRINT N'HTS parents without document (skipped): ' + CAST(@ParentsNoDoc AS NVARCHAR(20));

    /* ========== UPDATE existing ========== */
    UPDATE t SET
        t.ProjectId = s.ProjectId,
        t.DocumentId = s.DocumentId,
        t.Number = s.Number,
        t.DefinedByUserNumber = s.DefinedByUserNumber,
        t.Comments = s.Comments,
        t.IsForReplySheet = s.IsForReplySheet,
        t.EmployerRelated = s.EmployerRelated,
        t.DeliveryDateToEmployer = s.DeliveryDateToEmployer,
        t.CreatedById = COALESCE(s.CreatedById, t.CreatedById),
        t.CreatedByName = COALESCE(s.CreatedByName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedOnMiladiDateTime, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(LEFT(s.CreatedOnShamsiDateTime, 30), t.CreatedOnShamsiDateTime),
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Edms.Transmital t
    INNER JOIN #Src s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'Updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ========== INSERT missing ========== */
    INSERT INTO Edms.Transmital (
        HtsId, ProjectId, DocumentId, Number, DefinedByUserNumber, Comments,
        IsForReplySheet, EmployerRelated, DeliveryDateToEmployer, AttachmentId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.ProjectId, s.DocumentId, s.Number, s.DefinedByUserNumber, s.Comments,
        s.IsForReplySheet, s.EmployerRelated, s.DeliveryDateToEmployer, NULL,
        s.CreatedById, LEFT(s.CreatedByName, 150), s.CreatedOnMiladiDateTime, LEFT(s.CreatedOnShamsiDateTime, 30),
        @Seed, @Now, @NowShamsi, 1
    FROM #Src s
    WHERE NOT EXISTS (
        SELECT 1 FROM Edms.Transmital t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'Inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    DECLARE @TargetCnt INT = (SELECT COUNT(*) FROM Edms.Transmital);
    DECLARE @SyncedCnt INT = (SELECT COUNT(*) FROM Edms.Transmital WHERE HtsId <> 0);
    PRINT N'Target total: ' + CAST(@TargetCnt AS NVARCHAR(20))
        + N', with HtsId: ' + CAST(@SyncedCnt AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'Sync_EdmsTransmital_FromTotalSystem completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Sync_EdmsTransmital failed: %s', 16, 1, @Err);
END CATCH;
