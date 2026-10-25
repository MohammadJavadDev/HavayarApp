/*
Sync_EdmsTransmital_Attachments_FromTotalSystem.sql

آخرین پیوست هر ترانسمیتال HTS را به Edms.Transmital.AttachmentId وصل می‌کند.
فایل کپی نمی‌شود؛ dbo.FileEntity.PhysicalPath همان مسیر UNC می‌ماند.

Grain: یک FileEntity برای آخرین Edms_Transmital_Attachment هر Transmital_FK
(بیشترین Transmital_Attachment_ID). همه ردیف‌های مدرک همان ترانسمیتال همان فایل را می‌گیرند.
ردیف‌هایی که AttachmentId دارند دست نمی‌خورند.

  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Sync_EdmsTransmital_Attachments_FromTotalSystem.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @Seed NVARCHAR(150) = N'sync-edms-transmital-attachment';

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#Latest') IS NOT NULL DROP TABLE #Latest;
    SELECT
        x.Transmital_Attachment_ID,
        x.Transmital_FK,
        LTRIM(RTRIM(x.AttachmentFilePath)) AS PhysicalPath,
        COALESCE(NULLIF(LTRIM(RTRIM(x.Attachment_FileName)), N''), N'transmittal-file') AS OriginalName,
        ISNULL(x.Attachment_FileSize, 0) AS Size
    INTO #Latest
    FROM (
        SELECT
            a.Transmital_Attachment_ID,
            a.Transmital_FK,
            a.AttachmentFilePath,
            a.Attachment_FileName,
            a.Attachment_FileSize,
            ROW_NUMBER() OVER (
                PARTITION BY a.Transmital_FK
                ORDER BY a.Transmital_Attachment_ID DESC
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Edms_Transmital_Attachment] a
        WHERE NULLIF(LTRIM(RTRIM(a.AttachmentFilePath)), N'') IS NOT NULL
    ) x
    WHERE x.rn = 1;

    CREATE UNIQUE CLUSTERED INDEX IX_Latest_Parent ON #Latest (Transmital_FK);

    DECLARE @LatestCnt INT = (SELECT COUNT(*) FROM #Latest);
    PRINT N'Latest HTS attachments: ' + CAST(@LatestCnt AS NVARCHAR(20));

    INSERT INTO dbo.FileEntity
    (
        PhysicalPath, OriginalName, ContentType, Size,
        EntityType, EntityPropName, EntityId,
        CreatedByName, CreatedOnMiladiDateTime, IsActive
    )
    SELECT
        l.PhysicalPath,
        l.OriginalName,
        CASE LOWER(RIGHT(l.OriginalName, CHARINDEX(N'.', REVERSE(l.OriginalName) + N'.')))
            WHEN N'.pdf' THEN N'application/pdf'
            WHEN N'.zip' THEN N'application/zip'
            WHEN N'.rar' THEN N'application/x-rar-compressed'
            WHEN N'.jpg' THEN N'image/jpeg'
            WHEN N'.jpeg' THEN N'image/jpeg'
            WHEN N'.png' THEN N'image/png'
            WHEN N'.gif' THEN N'image/gif'
            WHEN N'.xls' THEN N'application/vnd.ms-excel'
            WHEN N'.xlsx' THEN N'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
            WHEN N'.doc' THEN N'application/msword'
            WHEN N'.docx' THEN N'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
            WHEN N'.dwg' THEN N'application/acad'
            ELSE N'application/octet-stream'
        END,
        l.Size,
        N'Entities.App.Edms.Transmital',
        N'Attachment',
        NULL,
        @Seed,
        @Now,
        1
    FROM #Latest l
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.FileEntity f
        WHERE f.PhysicalPath = l.PhysicalPath
          AND f.EntityType = N'Entities.App.Edms.Transmital'
          AND f.EntityPropName = N'Attachment'
          AND ISNULL(f.IsActive, 1) = 1
    );
    PRINT N'FileEntity inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    IF OBJECT_ID('tempdb..#FileMap') IS NOT NULL DROP TABLE #FileMap;
    SELECT
        l.Transmital_FK,
        MIN(f.Id) AS FileId
    INTO #FileMap
    FROM #Latest l
    INNER JOIN dbo.FileEntity f
        ON f.PhysicalPath = l.PhysicalPath
       AND f.EntityType = N'Entities.App.Edms.Transmital'
       AND f.EntityPropName = N'Attachment'
       AND ISNULL(f.IsActive, 1) = 1
    GROUP BY l.Transmital_FK;

    UPDATE t
    SET
        t.AttachmentId = m.FileId,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now
    FROM Edms.Transmital t
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Transmital_Document] td
        ON td.Transmital_Document_ID = t.HtsId
    INNER JOIN #FileMap m
        ON m.Transmital_FK = td.Transmital_FK
    WHERE t.HtsId <> 0
      AND t.AttachmentId IS NULL;
    PRINT N'Transmital rows linked: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'Sync_EdmsTransmital_Attachments_FromTotalSystem completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Sync_EdmsTransmital_Attachments failed: %s', 16, 1, @Err);
END CATCH;
