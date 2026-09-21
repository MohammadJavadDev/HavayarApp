/*
================================================================================
SyncBpmDocumentSystemFromTotalSystem.sql
================================================================================
همگام‌سازی آرشیو سازمانی (Gnr) و اسناد فرآیندی (Bpm) از TotalSystem با HtsId.

پیش‌نیاز:
  1) روی دیتابیس HavayarApp اجرا شود
  2) Linked Server [TMS] → TotalSystem (Data/Scripts/CreateLinkedServer_TMS.sql)
  3) جداول Gnr.DocumentFolder / OrganizationalDocument و Bpm.ProcessDocument* ساخته شده باشند
     (.\update-database.ps1 تا آخرین migration شامل نقش‌های 400000–400005 و ایندکس HtsId)
  4) system.[User] و Hrm.OrgUnit پر باشند
  5) ابتدا Seed_BpmDocumentSystem_DataProfiles.sql (نقش، نمایه، منو) سپس این اسکریپت
     ترتیب: migration → seed → sync. اگر seed قبل از migration اجرا شود، InsertData نقش‌ها duplicate می‌شود.

Idempotent: MERGE/Upsert روی HtsId (پوشه: FolderLevel+HtsId). باینری فایل کپی نمی‌شود؛
FileEntity.PhysicalPath به UNC موجود اشاره می‌کند.

متغیر @Strict = 1 باعث شکست کل دسته در صورت ردیف بدون نگاشت کاربر/واحد الزامی می‌شود.

Edms و Qa_CorrectiveAction عمداً همگام نمی‌شوند.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Strict BIT = 0;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-bpm-document-system';

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد. ابتدا CreateLinkedServer_TMS.sql را اجرا کنید.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    IF OBJECT_ID('tempdb..#OrgMap') IS NOT NULL DROP TABLE #OrgMap;
    IF OBJECT_ID('tempdb..#Unmapped') IS NOT NULL DROP TABLE #Unmapped;
    CREATE TABLE #Unmapped (Kind NVARCHAR(80) NOT NULL, OldId BIGINT NULL, Detail NVARCHAR(400) NULL);

    -----------------------------------------------------------------------------
    PRINT N'=== Maps: User / OrgUnit ===';
    -----------------------------------------------------------------------------

    SELECT
        CAST(ou.User_ID AS BIGINT) AS OldUserId,
        MIN(nu.Id) AS NewUserId,
        MAX(COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username)) AS NewUserName
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON nu.Username = ou.Username
        OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
            AND nu.Username = LTRIM(RTRIM(ou.ActiveDirectoryUsername)))
    GROUP BY CAST(ou.User_ID AS BIGINT);

    SELECT
        CAST(o.OrgUnit_ID AS BIGINT) AS OldOrgUnitId,
        MIN(n.Id) AS NewOrgUnitId
    INTO #OrgMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] o
    INNER JOIN [Hrm].[OrgUnit] n
        ON n.HamkaranUnitId = o.Hamkaran_Unit_FK
    WHERE o.Hamkaran_Unit_FK IS NOT NULL AND o.Hamkaran_Unit_FK <> 0
    GROUP BY CAST(o.OrgUnit_ID AS BIGINT);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Gnr_DocumentFolder_LevelHtsId' AND object_id = OBJECT_ID(N'Gnr.DocumentFolder'))
        CREATE UNIQUE NONCLUSTERED INDEX IX_Gnr_DocumentFolder_LevelHtsId
            ON Gnr.DocumentFolder (FolderLevel, HtsId) WHERE HtsId <> 0;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Gnr_OrganizationalDocument_HtsId' AND object_id = OBJECT_ID(N'Gnr.OrganizationalDocument'))
        CREATE UNIQUE NONCLUSTERED INDEX IX_Gnr_OrganizationalDocument_HtsId
            ON Gnr.OrganizationalDocument (HtsId) WHERE HtsId <> 0;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bpm_ProcessDocument_HtsId' AND object_id = OBJECT_ID(N'Bpm.ProcessDocument'))
        CREATE UNIQUE NONCLUSTERED INDEX IX_Bpm_ProcessDocument_HtsId
            ON Bpm.ProcessDocument (HtsId) WHERE HtsId <> 0;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bpm_ProcessDocumentStatusLog_HtsId' AND object_id = OBJECT_ID(N'Bpm.ProcessDocumentStatusLog'))
        CREATE UNIQUE NONCLUSTERED INDEX IX_Bpm_ProcessDocumentStatusLog_HtsId
            ON Bpm.ProcessDocumentStatusLog (HtsId) WHERE HtsId <> 0;

    -----------------------------------------------------------------------------
    PRINT N'=== Folders L1 / L2 / L3 ===';
    -----------------------------------------------------------------------------

    MERGE Gnr.DocumentFolder AS t
    USING (
        SELECT
            CAST(s.Id AS BIGINT) AS HtsId,
            1 AS FolderLevel,
            CAST(NULL AS BIGINT) AS ParentId,
            om.NewOrgUnitId AS OrganizationUnitId,
            s.Title,
            s.FolderTitle,
            CAST(s.IsPrivate AS BIT) AS IsPrivate,
            s.Comment,
            um.NewUserId AS CreatedById,
            um.NewUserName AS CreatedByName,
            s.CreatedDate AS CreatedOnMiladiDateTime,
            NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Gnr_Document1stLevel] s
        LEFT JOIN #OrgMap om ON om.OldOrgUnitId = s.OrganizationUnitId
        LEFT JOIN #UserMap um ON um.OldUserId = s.CreatedUserId
    ) AS s
    ON t.HtsId = s.HtsId AND t.FolderLevel = 1
    WHEN MATCHED THEN UPDATE SET
        t.Title = s.Title, t.FolderTitle = s.FolderTitle, t.IsPrivate = s.IsPrivate, t.Comment = s.Comment,
        t.OrganizationUnitId = s.OrganizationUnitId,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED AND s.OrganizationUnitId IS NOT NULL THEN INSERT
        (HtsId, FolderLevel, ParentId, OrganizationUnitId, Title, FolderTitle, IsPrivate, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, 1, NULL, s.OrganizationUnitId, s.Title, s.FolderTitle, s.IsPrivate, s.Comment,
         s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
         1, @SeedUser, @Now, @NowShamsi, 1);

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'FolderL1-Org', CAST(s.Id AS BIGINT), s.Title
    FROM [TMS].[TotalSystem].[dbo].[Gnr_Document1stLevel] s
    LEFT JOIN #OrgMap om ON om.OldOrgUnitId = s.OrganizationUnitId
    WHERE om.NewOrgUnitId IS NULL;

    DELETE fu
    FROM Gnr.DocumentFolderUser fu
    INNER JOIN Gnr.DocumentFolder f ON f.Id = fu.DocumentFolderId
    WHERE f.FolderLevel = 1 AND f.HtsId <> 0;

    INSERT INTO Gnr.DocumentFolderUser
        (DocumentFolderId, UserId, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT f.Id, um.NewUserId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Gnr_Document1stLevel] s
    INNER JOIN Gnr.DocumentFolder f ON f.HtsId = s.Id AND f.FolderLevel = 1
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.UserIds, N''), N' ', N''), N',') tok
    INNER JOIN #UserMap um ON um.OldUserId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0;

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'FolderUser', TRY_CAST(tok.value AS BIGINT), N'FolderHtsId=' + CAST(s.Id AS NVARCHAR(20))
    FROM [TMS].[TotalSystem].[dbo].[Gnr_Document1stLevel] s
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.UserIds, N''), N' ', N''), N',') tok
    LEFT JOIN #UserMap um ON um.OldUserId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0 AND um.NewUserId IS NULL;

    MERGE Gnr.DocumentFolder AS t
    USING (
        SELECT
            CAST(s.Id AS BIGINT) AS HtsId,
            2 AS FolderLevel,
            p.Id AS ParentId,
            p.OrganizationUnitId,
            p.IsPrivate,
            s.Title,
            s.FolderTitle,
            s.Comment,
            um.NewUserId AS CreatedById,
            um.NewUserName AS CreatedByName,
            s.CreatedDate AS CreatedOnMiladiDateTime,
            NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Gnr_Document2ndLevel] s
        INNER JOIN Gnr.DocumentFolder p ON p.HtsId = s.Document1stLevelId AND p.FolderLevel = 1
        LEFT JOIN #UserMap um ON um.OldUserId = s.CreatedUserId
    ) AS s
    ON t.HtsId = s.HtsId AND t.FolderLevel = 2
    WHEN MATCHED THEN UPDATE SET
        t.Title = s.Title, t.FolderTitle = s.FolderTitle, t.Comment = s.Comment,
        t.ParentId = s.ParentId, t.OrganizationUnitId = s.OrganizationUnitId, t.IsPrivate = s.IsPrivate,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, FolderLevel, ParentId, OrganizationUnitId, Title, FolderTitle, IsPrivate, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, 2, s.ParentId, s.OrganizationUnitId, s.Title, s.FolderTitle, s.IsPrivate, s.Comment,
         s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
         1, @SeedUser, @Now, @NowShamsi, 1);

    MERGE Gnr.DocumentFolder AS t
    USING (
        SELECT
            CAST(s.Id AS BIGINT) AS HtsId,
            3 AS FolderLevel,
            p.Id AS ParentId,
            p.OrganizationUnitId,
            p.IsPrivate,
            s.Title,
            s.FolderTitle,
            s.Comment,
            um.NewUserId AS CreatedById,
            um.NewUserName AS CreatedByName,
            s.CreatedDate AS CreatedOnMiladiDateTime,
            NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Gnr_Document3rdLevel] s
        INNER JOIN Gnr.DocumentFolder p ON p.HtsId = s.Document2ndLevelId AND p.FolderLevel = 2
        LEFT JOIN #UserMap um ON um.OldUserId = s.CreatedUserId
    ) AS s
    ON t.HtsId = s.HtsId AND t.FolderLevel = 3
    WHEN MATCHED THEN UPDATE SET
        t.Title = s.Title, t.FolderTitle = s.FolderTitle, t.Comment = s.Comment,
        t.ParentId = s.ParentId, t.OrganizationUnitId = s.OrganizationUnitId, t.IsPrivate = s.IsPrivate,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, FolderLevel, ParentId, OrganizationUnitId, Title, FolderTitle, IsPrivate, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, 3, s.ParentId, s.OrganizationUnitId, s.Title, s.FolderTitle, s.IsPrivate, s.Comment,
         s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
         1, @SeedUser, @Now, @NowShamsi, 1);

    -----------------------------------------------------------------------------
    PRINT N'=== Organizational documents + files ===';
    -----------------------------------------------------------------------------

    MERGE Gnr.OrganizationalDocument AS t
    USING (
        SELECT
            CAST(s.Id AS BIGINT) AS HtsId,
            f1.Id AS FolderId,
            f2.Id AS SecondLevelFolderId,
            f3.Id AS ThirdLevelFolderId,
            s.Title,
            s.TitleInEurope AS TitleInLatin,
            s.Brand,
            s.Revision,
            s.Year,
            s.Comment,
            um.NewUserId AS CreatedById,
            um.NewUserName AS CreatedByName,
            um2.NewUserId AS ModifiedById,
            um2.NewUserName AS ModifiedByName,
            s.CreatedDate AS CreatedOnMiladiDateTime,
            NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
            s.UpdatedDate AS ModifiedDateMiladiDateTime,
            NULLIF(LTRIM(RTRIM(s.UpdatedDateInText)), N'') AS ModifiedDateShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Gnr_Document] s
        INNER JOIN Gnr.DocumentFolder f1 ON f1.HtsId = s.Document1stLevelId AND f1.FolderLevel = 1
        LEFT JOIN Gnr.DocumentFolder f2 ON f2.HtsId = NULLIF(s.Document2ndLevelId, 0) AND f2.FolderLevel = 2
        LEFT JOIN Gnr.DocumentFolder f3 ON f3.HtsId = NULLIF(s.Document3rdLevelId, 0) AND f3.FolderLevel = 3
        LEFT JOIN #UserMap um ON um.OldUserId = s.CreatedUserId
        LEFT JOIN #UserMap um2 ON um2.OldUserId = s.UpdatedUserId
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.FolderId = s.FolderId, t.SecondLevelFolderId = s.SecondLevelFolderId, t.ThirdLevelFolderId = s.ThirdLevelFolderId,
        t.Title = s.Title, t.TitleInLatin = s.TitleInLatin, t.Brand = s.Brand, t.Revision = s.Revision, t.Year = s.Year, t.Comment = s.Comment,
        t.ModifiedById = s.ModifiedById, t.ModifiedByName = s.ModifiedByName,
        t.ModifiedDateMiladiDateTime = ISNULL(s.ModifiedDateMiladiDateTime, @Now),
        t.ModifiedDateShamsiDateTime = ISNULL(s.ModifiedDateShamsiDateTime, @NowShamsi)
    WHEN NOT MATCHED THEN INSERT
        (HtsId, FolderId, SecondLevelFolderId, ThirdLevelFolderId, Title, TitleInLatin, Brand, Revision, Year, Comment, FileId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.FolderId, s.SecondLevelFolderId, s.ThirdLevelFolderId, s.Title, s.TitleInLatin, s.Brand, s.Revision, s.Year, s.Comment, NULL,
         s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
         s.ModifiedById, s.ModifiedByName, s.ModifiedDateMiladiDateTime, s.ModifiedDateShamsiDateTime, 1);

    IF OBJECT_ID('tempdb..#FileWork') IS NOT NULL DROP TABLE #FileWork;
    CREATE TABLE #FileWork (
        TargetId BIGINT NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        PropName NVARCHAR(100) NOT NULL,
        PhysicalPath NVARCHAR(MAX) NOT NULL,
        OriginalName NVARCHAR(MAX) NOT NULL,
        Size BIGINT NOT NULL,
        ContentType NVARCHAR(200) NOT NULL
    );

    INSERT INTO #FileWork (TargetId, EntityType, PropName, PhysicalPath, OriginalName, Size, ContentType)
    SELECT d.Id, N'OrganizationalDocument', N'File',
        LTRIM(RTRIM(s.FinalFilePath)),
        COALESCE(NULLIF(LTRIM(RTRIM(s.DocumentFileName)), N''), N'file'),
        ISNULL(s.DocumentFileSize, 0),
        CASE LOWER(RIGHT(COALESCE(s.DocumentFileName, s.FinalFilePath, N''), 5))
            WHEN N'.docx' THEN N'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
            WHEN N'.xlsx' THEN N'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
            WHEN N'.jpeg' THEN N'image/jpeg'
            ELSE CASE LOWER(RIGHT(COALESCE(s.DocumentFileName, s.FinalFilePath, N''), 4))
                WHEN N'.pdf' THEN N'application/pdf'
                WHEN N'.doc' THEN N'application/msword'
                WHEN N'.xls' THEN N'application/vnd.ms-excel'
                WHEN N'.zip' THEN N'application/zip'
                WHEN N'.rar' THEN N'application/x-rar-compressed'
                WHEN N'.png' THEN N'image/png'
                WHEN N'.jpg' THEN N'image/jpeg'
                ELSE N'application/octet-stream'
            END
        END
    FROM [TMS].[TotalSystem].[dbo].[Gnr_Document] s
    INNER JOIN Gnr.OrganizationalDocument d ON d.HtsId = s.Id
    WHERE NULLIF(LTRIM(RTRIM(s.FinalFilePath)), N'') IS NOT NULL;

    -----------------------------------------------------------------------------
    PRINT N'=== Process documents ===';
    -----------------------------------------------------------------------------

    MERGE Bpm.ProcessDocument AS t
    USING (
        SELECT
            CAST(s.Id AS BIGINT) AS HtsId,
            CAST(s.Revision AS SMALLINT) AS Revision,
            s.RequestTypeId AS RequestType,
            s.DocumentTypeId AS DocumentType,
            LEFT(s.DocumentTitle, 128) AS DocumentTitle,
            LEFT(s.DocumentNumber, 20) AS DocumentNumber,
            s.PermissionTypeId AS AccessLevel,
            s.SourceId AS Source,
            s.PriorityId AS Priority,
            s.ProcessId AS ProcessSet,
            LEFT(s.SubProcessTitle, 128) AS SubProcessTitle,
            po.NewUserId AS ProcessOwnerId,
            ap.NewUserId AS ApproverId,
            fa.NewUserId AS FinalApproverId,
            s.CompletionForecastDate AS CompletionForecastMiladiDate,
            LEFT(s.CompletionForecastDateInText, 30) AS CompletionForecastShamsiDate,
            s.NotificationDate AS NotificationMiladiDate,
            LEFT(s.NotificationDateInText, 30) AS NotificationShamsiDate,
            s.LastStatusId AS LastStatus,
            ls.NewUserId AS LastStatusUserId,
            LEFT(s.RelatedDocumentsText, 1024) AS RelatedDocumentsText,
            CAST(s.IsDeprecate AS BIT) AS IsDeprecate,
            rq.NewUserId AS RequesterUserId,
            om.NewOrgUnitId AS CreatedOrganizationUnitId,
            LEFT(s.Comment, 2048) AS Comment,
            cu.NewUserId AS CreatedById,
            LEFT(cu.NewUserName, 150) AS CreatedByName,
            uu.NewUserId AS ModifiedById,
            LEFT(uu.NewUserName, 150) AS ModifiedByName,
            s.CreatedDate AS CreatedOnMiladiDateTime,
            LEFT(NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N''), 30) AS CreatedOnShamsiDateTime,
            s.UpdatedDate AS ModifiedDateMiladiDateTime,
            LEFT(NULLIF(LTRIM(RTRIM(s.UpdatedDateInText)), N''), 30) AS ModifiedDateShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
        LEFT JOIN #UserMap rq ON rq.OldUserId = s.RequesterUserId
        LEFT JOIN #UserMap po ON po.OldUserId = s.ProcessOwnerId
        LEFT JOIN #UserMap ap ON ap.OldUserId = s.ApproverId
        LEFT JOIN #UserMap fa ON fa.OldUserId = s.FinalApproverId
        LEFT JOIN #UserMap ls ON ls.OldUserId = s.LastStatusUserId
        LEFT JOIN #UserMap cu ON cu.OldUserId = s.CreatedUserId
        LEFT JOIN #UserMap uu ON uu.OldUserId = s.UpdatedUserId
        LEFT JOIN #OrgMap om ON om.OldOrgUnitId = s.CreatedOrganizationUnitId
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED AND s.RequesterUserId IS NOT NULL AND s.CreatedOrganizationUnitId IS NOT NULL THEN UPDATE SET
        t.Revision = ISNULL(s.Revision, 0), t.RequestType = s.RequestType, t.DocumentType = s.DocumentType,
        t.DocumentTitle = s.DocumentTitle, t.DocumentNumber = s.DocumentNumber, t.AccessLevel = s.AccessLevel,
        t.Source = s.Source, t.Priority = s.Priority, t.ProcessSet = s.ProcessSet, t.SubProcessTitle = s.SubProcessTitle,
        t.ProcessOwnerId = s.ProcessOwnerId, t.ApproverId = s.ApproverId, t.FinalApproverId = s.FinalApproverId,
        t.CompletionForecastMiladiDate = s.CompletionForecastMiladiDate, t.CompletionForecastShamsiDate = s.CompletionForecastShamsiDate,
        t.NotificationMiladiDate = s.NotificationMiladiDate, t.NotificationShamsiDate = s.NotificationShamsiDate,
        t.LastStatus = s.LastStatus, t.LastStatusUserId = s.LastStatusUserId,
        t.RelatedDocumentsText = s.RelatedDocumentsText, t.IsDeprecate = s.IsDeprecate,
        t.RequesterUserId = s.RequesterUserId, t.CreatedOrganizationUnitId = s.CreatedOrganizationUnitId, t.Comment = s.Comment,
        t.ModifiedById = s.ModifiedById, t.ModifiedByName = s.ModifiedByName,
        t.ModifiedDateMiladiDateTime = ISNULL(s.ModifiedDateMiladiDateTime, @Now),
        t.ModifiedDateShamsiDateTime = ISNULL(s.ModifiedDateShamsiDateTime, @NowShamsi)
    WHEN NOT MATCHED AND s.RequesterUserId IS NOT NULL AND s.CreatedOrganizationUnitId IS NOT NULL THEN INSERT
        (HtsId, ParentId, Revision, RequestType, DocumentType, DocumentTitle, DocumentNumber, AccessLevel, Source, Priority,
         ProcessSet, SubProcessTitle, ProcessOwnerId, ApproverId, FinalApproverId,
         CompletionForecastMiladiDate, CompletionForecastShamsiDate, NotificationMiladiDate, NotificationShamsiDate,
         LastStatus, LastStatusUserId, RelatedDocumentsText, IsDeprecate, RequesterUserId, CreatedOrganizationUnitId, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, NULL, ISNULL(s.Revision, 0), s.RequestType, s.DocumentType, s.DocumentTitle, s.DocumentNumber, s.AccessLevel, s.Source, s.Priority,
         s.ProcessSet, s.SubProcessTitle, s.ProcessOwnerId, s.ApproverId, s.FinalApproverId,
         s.CompletionForecastMiladiDate, s.CompletionForecastShamsiDate, s.NotificationMiladiDate, s.NotificationShamsiDate,
         s.LastStatus, s.LastStatusUserId, s.RelatedDocumentsText, s.IsDeprecate, s.RequesterUserId, s.CreatedOrganizationUnitId, s.Comment,
         s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
         s.ModifiedById, s.ModifiedByName, s.ModifiedDateMiladiDateTime, s.ModifiedDateShamsiDateTime, 1);

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'ProcessDocument-Skip', CAST(s.Id AS BIGINT),
        CASE WHEN rq.NewUserId IS NULL THEN N'requester ' ELSE N'' END
        + CASE WHEN om.NewOrgUnitId IS NULL THEN N'org ' ELSE N'' END
        + ISNULL(s.DocumentTitle, N'')
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    LEFT JOIN #UserMap rq ON rq.OldUserId = s.RequesterUserId
    LEFT JOIN #OrgMap om ON om.OldOrgUnitId = s.CreatedOrganizationUnitId
    WHERE rq.NewUserId IS NULL OR om.NewOrgUnitId IS NULL;

    UPDATE pd SET ParentId = p.Id
    FROM Bpm.ProcessDocument pd
    INNER JOIN [TMS].[TotalSystem].[dbo].[Bpm_Document] s ON s.Id = pd.HtsId
    INNER JOIN Bpm.ProcessDocument p ON p.HtsId = NULLIF(s.ParentId, 0)
    WHERE s.ParentId IS NOT NULL AND s.ParentId <> 0;

    -----------------------------------------------------------------------------
    PRINT N'=== Junctions from CSV ===';
    -----------------------------------------------------------------------------

    DELETE j FROM Bpm.ProcessDocumentEditingOrgUnit j INNER JOIN Bpm.ProcessDocument d ON d.Id = j.ProcessDocumentId WHERE d.HtsId <> 0;
    DELETE j FROM Bpm.ProcessDocumentExecuterOrgUnit j INNER JOIN Bpm.ProcessDocument d ON d.Id = j.ProcessDocumentId WHERE d.HtsId <> 0;
    DELETE j FROM Bpm.ProcessDocumentProcessExecuter j INNER JOIN Bpm.ProcessDocument d ON d.Id = j.ProcessDocumentId WHERE d.HtsId <> 0;
    DELETE j FROM Bpm.ProcessDocumentBeneficiaryOrgUnit j INNER JOIN Bpm.ProcessDocument d ON d.Id = j.ProcessDocumentId WHERE d.HtsId <> 0;
    DELETE j FROM Bpm.ProcessDocumentNotificationRecipient j INNER JOIN Bpm.ProcessDocument d ON d.Id = j.ProcessDocumentId WHERE d.HtsId <> 0;
    DELETE j FROM Bpm.ProcessDocumentRelatedOrgUnit j INNER JOIN Bpm.ProcessDocument d ON d.Id = j.ProcessDocumentId WHERE d.HtsId <> 0;

    INSERT INTO Bpm.ProcessDocumentEditingOrgUnit (ProcessDocumentId, OrgUnitId, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT pd.Id, om.NewOrgUnitId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.EditingOrganizationUnitIds, N''), N' ', N''), N',') tok
    INNER JOIN #OrgMap om ON om.OldOrgUnitId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0;

    INSERT INTO Bpm.ProcessDocumentExecuterOrgUnit (ProcessDocumentId, OrgUnitId, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT pd.Id, om.NewOrgUnitId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.ExecuterOrganizationUnitIds, N''), N' ', N''), N',') tok
    INNER JOIN #OrgMap om ON om.OldOrgUnitId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0;

    INSERT INTO Bpm.ProcessDocumentBeneficiaryOrgUnit (ProcessDocumentId, OrgUnitId, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT pd.Id, om.NewOrgUnitId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.BeneficiaryIds, N''), N' ', N''), N',') tok
    INNER JOIN #OrgMap om ON om.OldOrgUnitId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0;

    INSERT INTO Bpm.ProcessDocumentRelatedOrgUnit (ProcessDocumentId, OrgUnitId, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT pd.Id, om.NewOrgUnitId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.RelatedUnitsRef, N''), N' ', N''), N',') tok
    INNER JOIN #OrgMap om ON om.OldOrgUnitId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0;

    INSERT INTO Bpm.ProcessDocumentProcessExecuter (ProcessDocumentId, UserId, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT pd.Id, um.NewUserId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.ProcessExecuterIds, N''), N' ', N''), N',') tok
    INNER JOIN #UserMap um ON um.OldUserId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0;

    INSERT INTO Bpm.ProcessDocumentNotificationRecipient (ProcessDocumentId, UserId, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT pd.Id, um.NewUserId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.NotificationRecipientIds, N''), N' ', N''), N',') tok
    INNER JOIN #UserMap um ON um.OldUserId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0;

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'CsvUser', TRY_CAST(tok.value AS BIGINT), N'NotificationRecipient doc=' + CAST(s.Id AS NVARCHAR(20))
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.NotificationRecipientIds, N''), N' ', N''), N',') tok
    LEFT JOIN #UserMap um ON um.OldUserId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0 AND um.NewUserId IS NULL;

    -----------------------------------------------------------------------------
    PRINT N'=== Status log ===';
    -----------------------------------------------------------------------------

    MERGE Bpm.ProcessDocumentStatusLog AS t
    USING (
        SELECT
            CAST(s.Id AS BIGINT) AS HtsId,
            pd.Id AS ProcessDocumentId,
            s.StatusId AS Status,
            s.SendDate AS SendMiladiDate,
            s.SendDateInText AS SendShamsiDate,
            s.ReceiveDate AS ReceiveMiladiDate,
            s.ReceiveDateInText AS ReceiveShamsiDate,
            s.Comment,
            um.NewUserId AS CreatedById,
            um.NewUserName AS CreatedByName,
            s.CreatedDate AS CreatedOnMiladiDateTime,
            NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Bpm_DocumentDetail] s
        INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.DocumentId
        LEFT JOIN #UserMap um ON um.OldUserId = s.CreatedUserId
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.ProcessDocumentId = s.ProcessDocumentId, t.Status = s.Status,
        t.SendMiladiDate = s.SendMiladiDate, t.SendShamsiDate = s.SendShamsiDate,
        t.ReceiveMiladiDate = s.ReceiveMiladiDate, t.ReceiveShamsiDate = s.ReceiveShamsiDate,
        t.Comment = s.Comment, t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName
    WHEN NOT MATCHED THEN INSERT
        (HtsId, ProcessDocumentId, Status, SendMiladiDate, SendShamsiDate, ReceiveMiladiDate, ReceiveShamsiDate, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.ProcessDocumentId, s.Status, s.SendMiladiDate, s.SendShamsiDate, s.ReceiveMiladiDate, s.ReceiveShamsiDate, s.Comment,
         s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
         1, @SeedUser, @Now, @NowShamsi, 1);

    UPDATE pd SET LastStatus = x.Status, LastStatusUserId = x.CreatedById
    FROM Bpm.ProcessDocument pd
    INNER JOIN (
        SELECT ProcessDocumentId, Status, CreatedById,
            ROW_NUMBER() OVER (PARTITION BY ProcessDocumentId ORDER BY HtsId DESC, Id DESC) AS rn
        FROM Bpm.ProcessDocumentStatusLog
        WHERE HtsId <> 0
    ) x ON x.ProcessDocumentId = pd.Id AND x.rn = 1
    WHERE pd.HtsId <> 0;

    -----------------------------------------------------------------------------
    PRINT N'=== Process files (UNC, no copy) ===';
    -----------------------------------------------------------------------------

    INSERT INTO #FileWork (TargetId, EntityType, PropName, PhysicalPath, OriginalName, Size, ContentType)
    SELECT pd.Id, N'ProcessDocument', slot.PropName, LTRIM(RTRIM(slot.PathVal)),
        COALESCE(NULLIF(LTRIM(RTRIM(slot.NameVal)), N''), N'file'), ISNULL(slot.SizeVal, 0),
        N'application/octet-stream'
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY (VALUES
        (N'MainFile', s.MainFilePath, s.MainFileName, s.MainFileSize),
        (N'TempFile', s.TempFilePath, s.TempFileName, s.TempFileSize),
        (N'FinalFile', s.FinalFilePath, s.FinalFileName, s.FinalFileSize),
        (N'FinalPdfFile', s.FinalPdfFilePath, s.FinalPdfFileName, s.FinalPdfFileSize),
        (N'CommentFile', s.CommentFilePath, s.CommentFileName, s.CommentFileSize)
    ) slot(PropName, PathVal, NameVal, SizeVal)
    WHERE NULLIF(LTRIM(RTRIM(slot.PathVal)), N'') IS NOT NULL;

    UPDATE #FileWork SET ContentType =
        CASE LOWER(RIGHT(OriginalName, 5))
            WHEN N'.docx' THEN N'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
            WHEN N'.xlsx' THEN N'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
            WHEN N'.jpeg' THEN N'image/jpeg'
            ELSE CASE LOWER(RIGHT(OriginalName, 4))
                WHEN N'.pdf' THEN N'application/pdf'
                WHEN N'.doc' THEN N'application/msword'
                WHEN N'.xls' THEN N'application/vnd.ms-excel'
                WHEN N'.zip' THEN N'application/zip'
                WHEN N'.rar' THEN N'application/x-rar-compressed'
                WHEN N'.png' THEN N'image/png'
                WHEN N'.jpg' THEN N'image/jpeg'
                ELSE N'application/octet-stream'
            END
        END;

    UPDATE f SET
        f.PhysicalPath = w.PhysicalPath, f.OriginalName = w.OriginalName, f.Size = w.Size, f.ContentType = w.ContentType,
        f.ModifiedById = 1, f.ModifiedByName = @SeedUser, f.ModifiedDateMiladiDateTime = @Now, f.ModifiedDateShamsiDateTime = @NowShamsi
    FROM dbo.FileEntity f
    INNER JOIN #FileWork w ON w.TargetId = f.EntityId AND w.EntityType = f.EntityType AND w.PropName = f.EntityPropName;

    INSERT INTO dbo.FileEntity
        (PhysicalPath, OriginalName, ContentType, Size, EntityType, EntityPropName, EntityId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT w.PhysicalPath, w.OriginalName, w.ContentType, w.Size, w.EntityType, w.PropName, w.TargetId,
        1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM #FileWork w
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.FileEntity f
        WHERE f.EntityId = w.TargetId AND f.EntityType = w.EntityType AND f.EntityPropName = w.PropName
    );

    UPDATE d SET FileId = f.Id
    FROM Gnr.OrganizationalDocument d
    INNER JOIN dbo.FileEntity f ON f.EntityId = d.Id AND f.EntityType = N'OrganizationalDocument' AND f.EntityPropName = N'File';

    UPDATE d SET MainFileId = f.Id
    FROM Bpm.ProcessDocument d
    INNER JOIN dbo.FileEntity f ON f.EntityId = d.Id AND f.EntityType = N'ProcessDocument' AND f.EntityPropName = N'MainFile';
    UPDATE d SET TempFileId = f.Id
    FROM Bpm.ProcessDocument d
    INNER JOIN dbo.FileEntity f ON f.EntityId = d.Id AND f.EntityType = N'ProcessDocument' AND f.EntityPropName = N'TempFile';
    UPDATE d SET FinalFileId = f.Id
    FROM Bpm.ProcessDocument d
    INNER JOIN dbo.FileEntity f ON f.EntityId = d.Id AND f.EntityType = N'ProcessDocument' AND f.EntityPropName = N'FinalFile';
    UPDATE d SET FinalPdfFileId = f.Id
    FROM Bpm.ProcessDocument d
    INNER JOIN dbo.FileEntity f ON f.EntityId = d.Id AND f.EntityType = N'ProcessDocument' AND f.EntityPropName = N'FinalPdfFile';
    UPDATE d SET CommentFileId = f.Id
    FROM Bpm.ProcessDocument d
    INNER JOIN dbo.FileEntity f ON f.EntityId = d.Id AND f.EntityType = N'ProcessDocument' AND f.EntityPropName = N'CommentFile';

    -----------------------------------------------------------------------------
    PRINT N'=== Announcement number + HTS group roles ===';
    -----------------------------------------------------------------------------

    DECLARE @Ann INT;
    SELECT @Ann = TRY_CAST(Configuration_Value AS INT)
    FROM [TMS].[TotalSystem].[dbo].[Gnr_MainConfiguration]
    WHERE Configuration_Key = N'Bpm_LastAnnouncementNumber';

    IF @Ann IS NOT NULL
        UPDATE Bpm.ProcessDocumentModuleSetting
        SET LastAnnouncementNumber = @Ann, ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = 1;

    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #UserRoleMap (Username, RoleName)
    SELECT DISTINCT u.Username, m.RoleName
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] gm
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = gm.User_FK
    INNER JOIN (VALUES
        (353, N'Bpm.ProcessDocument.Supervisor'),
        (354, N'Bpm.ProcessDocument.Expert'),
        (355, N'Bpm.ProcessDocument.PublishedList'),
        (418, N'Bpm.ProcessDocument.ViewManage')
    ) m(GroupId, RoleName) ON m.GroupId = gm.UserGroup_FK;

    DECLARE @MapUsername NVARCHAR(200), @MapRoleName NVARCHAR(200), @MapRoleId BIGINT, @MapUserId BIGINT;
    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName;
        SELECT TOP 1 @MapUserId = Id FROM system.[User] WHERE LOWER(Username) = LOWER(@MapUsername);
        IF @MapUserId IS NOT NULL AND @MapRoleId IS NOT NULL
           AND NOT EXISTS (
                SELECT 1 FROM system.[User] u CROSS APPLY OPENJSON(u.RoleIds) j
                WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS BIGINT) = @MapRoleId)
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]') THEN N'[' + CAST(@MapRoleId AS NVARCHAR(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS NVARCHAR(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]') THEN N'["' + @MapRoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @MapRoleName + N'"]')
                END,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
        END
        SET @MapUserId = NULL; SET @MapRoleId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    DECLARE @SkipCount INT = (SELECT COUNT(*) FROM #Unmapped);
    IF @Strict = 1 AND @SkipCount > 0
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT TOP 50 * FROM #Unmapped;
        RAISERROR(N'Strict: ردیف بدون نگاشت وجود دارد.', 16, 1);
        RETURN;
    END;

    COMMIT TRANSACTION;

    PRINT N'=== DONE SyncBpmDocumentSystemFromTotalSystem ===';
    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Gnr_Document1stLevel]) AS HtsFolder1,
        (SELECT COUNT(*) FROM Gnr.DocumentFolder WHERE FolderLevel = 1 AND HtsId <> 0) AS AppFolder1,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Gnr_Document]) AS HtsOrgDoc,
        (SELECT COUNT(*) FROM Gnr.OrganizationalDocument WHERE HtsId <> 0) AS AppOrgDoc,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Bpm_Document]) AS HtsProcess,
        (SELECT COUNT(*) FROM Bpm.ProcessDocument WHERE HtsId <> 0) AS AppProcess,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Bpm_DocumentDetail]) AS HtsLog,
        (SELECT COUNT(*) FROM Bpm.ProcessDocumentStatusLog WHERE HtsId <> 0) AS AppLog,
        (SELECT LastAnnouncementNumber FROM Bpm.ProcessDocumentModuleSetting WHERE Id = 1) AS AnnouncementNumber,
        (SELECT COUNT(*) FROM Edms.Document) AS EdmsDocumentUnchangedCheck;

    SELECT Kind, COUNT(*) AS Cnt FROM #Unmapped GROUP BY Kind;
    SELECT TOP 30 * FROM #Unmapped ORDER BY Kind, OldId;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
