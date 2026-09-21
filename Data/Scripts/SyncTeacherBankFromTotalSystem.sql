-- ============================================
-- مهاجرت Trn.TeacherBank (+ پیوست‌ها)
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع:  dbo.Training_TeacherBank
--         dbo.Training_TeacherBank_Attachment
-- مقصد:  Trn.TeacherBank
--         Trn.TeacherBankAttachment
--         dbo.FileEntity
--
-- نگاشت مدرس:
--   Id                 <- Id  (حفظ شناسه برای FK دوره)
--   FirstName          <- FirstName
--   LastName           <- LastName
--   Education          <- EducationId  (506..510 / 2457..2461)
--   FieldOfStudy       <- DegreOfEducationId
--   Mobile             <- MobileNumber
--   Email              <- Email
--   NationalCode       <- NationalCode
--   IsHavayarPerson    <- IsHavayarPerson
--   DailyWages         <- DailyWages
--   Address            <- Address
--   TeachingField      <- TeachingFields
--   Description        <- NULL (در سیستم قدیم وجود ندارد)
--   CreatedById/Name   <- Gnr_User.Username -> system.[User].Username
--   CreatedOnMiladiDateTime <- CreatedDate
--   CreatedOnShamsiDateTime <- CreatedDateInText
--   IsActive           = 1 (فعال)
--
-- نگاشت پیوست:
--   Title              <- Comment
--   File.OriginalName  <- Attachment_FileName
--   محتوا باینری روی دیسک کپی نمی‌شود (در TMS به صورت byte[] است)
--   PhysicalPath       <- migration/teacherbank/{OldAttachmentId}/{FileName}
--
-- قبل از اجرا:
--   1) روی دیتابیس HavayarApp اجرا شود
--   2) Linked Server [TMS] به TotalSystem در دسترس باشد
--   3) جداول Trn.TeacherBank و Trn.TeacherBankAttachment ساخته شده باشند
--   4) Trn.Course نباید به ردیف‌های TeacherBank وابسته باشد
-- ============================================

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;

    SELECT
        CAST(ou.User_ID AS INT) AS OldUserId,
        nu.Id                   AS NewUserId,
        COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name) AS NewUserName,
        ou.Username             AS Username
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON nu.Username = ou.Username;

    -- تفاوت املای یوزرنیم سیستم قدیم / جدید
    IF NOT EXISTS (SELECT 1 FROM #UserMap WHERE OldUserId = 40)
    BEGIN
        INSERT INTO #UserMap (OldUserId, NewUserId, NewUserName, Username)
        SELECT
            40,
            nu.Id,
            COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name),
            nu.Username
        FROM [system].[User] nu
        WHERE nu.Username = N'allahvirdi.s';
    END;

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Training_TeacherBank] tb
        WHERE tb.EducationId NOT IN (506, 507, 508, 509, 510, 2457, 2458, 2459, 2460, 2461)
    )
    BEGIN
        RAISERROR(N'مقدار EducationId ناشناخته در Training_TeacherBank وجود دارد.', 16, 1);
    END;

    DELETE FROM Trn.TeacherBankAttachment;

    DELETE FROM dbo.FileEntity
    WHERE EntityType = N'Entities.App.Trn.TeacherBankAttachment';

    DELETE FROM Trn.TeacherBank;

    SET IDENTITY_INSERT Trn.TeacherBank ON;

    INSERT INTO Trn.TeacherBank
    (
        Id,
        FirstName,
        LastName,
        Education,
        FieldOfStudy,
        Mobile,
        Email,
        NationalCode,
        IsHavayarPerson,
        DailyWages,
        Address,
        TeachingField,
        Description,
        CreatedById,
        ModifiedById,
        CreatedByName,
        ModifiedByName,
        CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime,
        ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        CAST(tb.Id AS BIGINT)                                      AS Id,
        NULLIF(LTRIM(RTRIM(tb.FirstName)), N'')                    AS FirstName,
        NULLIF(LTRIM(RTRIM(tb.LastName)), N'')                     AS LastName,
        CAST(tb.EducationId AS INT)                                AS Education,
        NULLIF(LTRIM(RTRIM(tb.DegreOfEducationId)), N'')           AS FieldOfStudy,
        LEFT(LTRIM(RTRIM(tb.MobileNumber)), 20)                    AS Mobile,
        NULLIF(LTRIM(RTRIM(tb.Email)), N'')                        AS Email,
        NULLIF(LTRIM(RTRIM(tb.NationalCode)), N'')                 AS NationalCode,
        tb.IsHavayarPerson,
        CAST(tb.DailyWages AS DECIMAL(18, 2))                      AS DailyWages,
        NULLIF(LTRIM(RTRIM(tb.Address)), N'')                      AS Address,
        NULLIF(LTRIM(RTRIM(tb.TeachingFields)), N'')               AS TeachingField,
        NULL                                                         AS Description,
        um.NewUserId                                               AS CreatedById,
        NULL                                                       AS ModifiedById,
        um.NewUserName                                             AS CreatedByName,
        NULL                                                       AS ModifiedByName,
        tb.CreatedDate                                             AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(tb.CreatedDateInText)), N'')            AS CreatedOnShamsiDateTime,
        NULL                                                       AS ModifiedDateMiladiDateTime,
        NULL                                                       AS ModifiedDateShamsiDateTime,
        1                                                          AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_TeacherBank] tb
    LEFT JOIN #UserMap um
        ON um.OldUserId = tb.CreatedUserId;

    SET IDENTITY_INSERT Trn.TeacherBank OFF;
    DBCC CHECKIDENT ('Trn.TeacherBank', RESEED) WITH NO_INFOMSGS;

    IF OBJECT_ID('tempdb..#AttachmentFileMap') IS NOT NULL DROP TABLE #AttachmentFileMap;

    CREATE TABLE #AttachmentFileMap
    (
        OldAttachmentId INT NOT NULL PRIMARY KEY,
        NewFileId       BIGINT NOT NULL
    );

    INSERT INTO dbo.FileEntity
    (
        PhysicalPath,
        OriginalName,
        ContentType,
        Size,
        EntityType,
        EntityPropName,
        EntityId,
        CreatedById,
        CreatedByName,
        CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime,
        IsActive
    )
    OUTPUT
        CAST(inserted.EntityId AS INT),
        inserted.Id
    INTO #AttachmentFileMap (OldAttachmentId, NewFileId)
    SELECT
        N'migration/teacherbank/' + CAST(a.Id AS NVARCHAR(20)) + N'/'
            + COALESCE(NULLIF(LTRIM(RTRIM(a.Attachment_FileName)), N''), N'file.bin')
                                                               AS PhysicalPath,
        COALESCE(NULLIF(LTRIM(RTRIM(a.Attachment_FileName)), N''), N'file.bin')
                                                               AS OriginalName,
        CASE
            WHEN a.Attachment_FileName LIKE N'%.pdf' THEN N'application/pdf'
            WHEN a.Attachment_FileName LIKE N'%.png' THEN N'image/png'
            WHEN a.Attachment_FileName LIKE N'%.jpg'
              OR a.Attachment_FileName LIKE N'%.jpeg' THEN N'image/jpeg'
            WHEN a.Attachment_FileName LIKE N'%.zip' THEN N'application/zip'
            WHEN a.Attachment_FileName LIKE N'%.doc' THEN N'application/msword'
            WHEN a.Attachment_FileName LIKE N'%.docx' THEN N'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
            ELSE N'application/octet-stream'
        END                                                    AS ContentType,
        ISNULL(a.Attachment_FileSize, 0)                       AS Size,
        N'Entities.App.Trn.TeacherBankAttachment'              AS EntityType,
        N'File'                                                AS EntityPropName,
        CAST(a.Id AS BIGINT)                                   AS EntityId,
        um.NewUserId                                           AS CreatedById,
        um.NewUserName                                         AS CreatedByName,
        GETDATE()                                              AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(a.CreatedDate + N' ' + a.CreatedTime)), N'')
                                                               AS CreatedOnShamsiDateTime,
        1                                                      AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_TeacherBank_Attachment] a
    INNER JOIN Trn.TeacherBank tb
        ON tb.Id = a.TeacherBankId
    LEFT JOIN #UserMap um
        ON um.OldUserId = a.CreatedUserId;

    SET IDENTITY_INSERT Trn.TeacherBankAttachment ON;

    INSERT INTO Trn.TeacherBankAttachment
    (
        Id,
        TeacherBankId,
        Title,
        FileId,
        CreatedById,
        ModifiedById,
        CreatedByName,
        ModifiedByName,
        CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime,
        ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        CAST(a.Id AS BIGINT)                                   AS Id,
        CAST(a.TeacherBankId AS BIGINT)                        AS TeacherBankId,
        LEFT(COALESCE(NULLIF(LTRIM(RTRIM(a.Comment)), N''), a.Attachment_FileName, N'پیوست'), 256)
                                                               AS Title,
        fm.NewFileId                                           AS FileId,
        um.NewUserId                                           AS CreatedById,
        NULL                                                   AS ModifiedById,
        um.NewUserName                                         AS CreatedByName,
        NULL                                                   AS ModifiedByName,
        GETDATE()                                              AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(a.CreatedDate + N' ' + a.CreatedTime)), N'')
                                                               AS CreatedOnShamsiDateTime,
        NULL                                                   AS ModifiedDateMiladiDateTime,
        NULL                                                   AS ModifiedDateShamsiDateTime,
        1                                                      AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_TeacherBank_Attachment] a
    INNER JOIN Trn.TeacherBank tb
        ON tb.Id = a.TeacherBankId
    INNER JOIN #AttachmentFileMap fm
        ON fm.OldAttachmentId = a.Id
    LEFT JOIN #UserMap um
        ON um.OldUserId = a.CreatedUserId;

    SET IDENTITY_INSERT Trn.TeacherBankAttachment OFF;
    DBCC CHECKIDENT ('Trn.TeacherBankAttachment', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت TeacherBank با موفقیت انجام شد.';

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_TeacherBank]) AS OldTeacherCount,
        (SELECT COUNT(*) FROM Trn.TeacherBank) AS NewTeacherCount,
        (SELECT COUNT(*) FROM Trn.TeacherBank WHERE CreatedById IS NULL) AS UnmappedUserCount,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_TeacherBank_Attachment]) AS OldAttachmentCount,
        (SELECT COUNT(*) FROM Trn.TeacherBankAttachment) AS NewAttachmentCount;

    SELECT
        tb.Id              AS OldId,
        tb.CreatedUserId   AS OldCreatedUserId,
        ou.Username        AS OldUsername
    FROM [TMS].[TotalSystem].[dbo].[Training_TeacherBank] tb
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        ON ou.User_ID = tb.CreatedUserId
    LEFT JOIN #UserMap um
        ON um.OldUserId = tb.CreatedUserId
    WHERE um.NewUserId IS NULL;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY
        SET IDENTITY_INSERT Trn.TeacherBank OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    BEGIN TRY
        SET IDENTITY_INSERT Trn.TeacherBankAttachment OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
