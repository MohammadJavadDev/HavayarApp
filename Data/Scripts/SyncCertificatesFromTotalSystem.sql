-- ============================================
-- مهاجرت Trn.Certificate و Trn.CertificateEmailLog
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع:  dbo.Training_Certificate (۱۳۱۴۷ ردیف)
--         dbo.Training_CertificateEmailLog (۳ ردیف)
-- مقصد:  Trn.Certificate
--         Trn.CertificateEmailLog
--
-- نگاشت گواهی:
--   Id                <- همان شناسه
--   CourseParticipantId / CourseCompanyParticipantId <- همان FK اگر ثبت‌نام مهاجرت شده باشد
--   CertificateNo     <- CertificateNo
--   IssueMiladiDate   <- CreatedDate
--   IssueShamsiDate   <- CreatedDateInText
--   Description       <- Comment
--   TemplateKey       <- معادل ResolveTemplateKey (چاپ تکی HTS)
--   CreatedBy         <- Username + alias allahverdi.s -> allahvirdi.s
--   IsActive          = 1
--
-- حذف:
--   ۳ ردیف بدون FK
--   گواهی‌های متصل به ثبت‌نام مهاجرت‌نشده (مثل CourseParticipant Id=4892)
--
-- لاگ ایمیل:
--   Id / CourseId حفظ می‌شود
--   CertificateId = NULL (در قدیم به گواهی وصل نبود)
--   RecipientEmail <- Receiver
--   IsSuccess = 1
--   ResultMessage <- Comment یا «مهاجرت از TMS»
--
-- پیش‌نیاز: Course، CourseParticipant، CourseCompanyParticipant، CourseBank
-- ============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

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

    DELETE FROM Trn.CertificateEmailLog;
    DELETE FROM Trn.Certificate;

    SET IDENTITY_INSERT Trn.Certificate ON;

    INSERT INTO Trn.Certificate
    (
        Id,
        CourseParticipantId,
        CourseCompanyParticipantId,
        CertificateNo,
        IssueShamsiDate,
        IssueMiladiDate,
        TemplateKey,
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
        src.Id,
        CASE WHEN cp.Id IS NOT NULL THEN src.CourseParticipantId ELSE NULL END,
        CASE WHEN ccp.Id IS NOT NULL THEN src.CourseCompanyParticipantId ELSE NULL END,
        src.CertificateNo,
        src.CreatedDateInText,
        src.CreatedDate,
        CASE
            WHEN src.CourseCompanyParticipantId IS NOT NULL THEN
                CASE
                    WHEN co.CngCourseType = 2787 THEN N'Certificate9'
                    WHEN co.CngCourseType = 2788 THEN N'Certificate10'
                    WHEN co.HasExam = 1 THEN N'Certificate3'
                    ELSE N'Certificate1'
                END
            WHEN cb.CourseField = 502 THEN N'Cng'
            WHEN co.CngCourseType = 703 THEN N'Certificate5'
            WHEN co.CngCourseType = 1184 THEN N'Certificate6'
            WHEN co.CngCourseType = 2411 THEN N'Certificate7'
            WHEN co.CngCourseType = 2412 THEN N'Certificate8'
            WHEN co.CngCourseType = 2787 THEN N'Certificate10'
            WHEN co.CngCourseType = 2788 THEN N'Certificate9'
            ELSE N'Certificate2'
        END,
        src.Comment,
        um.NewUserId,
        um.NewUserId,
        um.NewUserName,
        um.NewUserName,
        src.CreatedDate,
        src.CreatedDateInText,
        src.CreatedDate,
        src.CreatedDateInText,
        1
    FROM [TMS].[TotalSystem].[dbo].[Training_Certificate] src
    LEFT JOIN Trn.CourseParticipant cp
        ON cp.Id = src.CourseParticipantId
    LEFT JOIN Trn.CourseCompanyParticipant ccp
        ON ccp.Id = src.CourseCompanyParticipantId
    INNER JOIN Trn.Course co
        ON co.Id = COALESCE(cp.CourseId, ccp.CourseId)
    LEFT JOIN Trn.CourseBank cb
        ON cb.Id = co.CourseBankId
    LEFT JOIN #UserMap um
        ON um.OldUserId = src.CreatedUserId
    WHERE (src.CourseParticipantId IS NOT NULL AND cp.Id IS NOT NULL)
       OR (src.CourseCompanyParticipantId IS NOT NULL AND ccp.Id IS NOT NULL);

    SET IDENTITY_INSERT Trn.Certificate OFF;
    DBCC CHECKIDENT ('Trn.Certificate', RESEED) WITH NO_INFOMSGS;

    SET IDENTITY_INSERT Trn.CertificateEmailLog ON;

    INSERT INTO Trn.CertificateEmailLog
    (
        Id,
        CourseId,
        CertificateId,
        RecipientEmail,
        IsSuccess,
        ResultMessage,
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
        src.Id,
        src.CourseId,
        NULL,
        src.Receiver,
        1,
        COALESCE(NULLIF(LTRIM(RTRIM(src.Comment)), N''), N'مهاجرت از TMS'),
        um.NewUserId,
        um.NewUserId,
        um.NewUserName,
        um.NewUserName,
        GETDATE(),
        src.CreatedDate + N' ' + src.CreatedTime,
        GETDATE(),
        src.CreatedDate + N' ' + src.CreatedTime,
        1
    FROM [TMS].[TotalSystem].[dbo].[Training_CertificateEmailLog] src
    INNER JOIN Trn.Course co
        ON co.Id = src.CourseId
    LEFT JOIN #UserMap um
        ON um.OldUserId = src.CreatedUserId;

    SET IDENTITY_INSERT Trn.CertificateEmailLog OFF;
    DBCC CHECKIDENT ('Trn.CertificateEmailLog', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت گواهی‌ها با موفقیت انجام شد.';

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_Certificate]) AS OldCertificateCount,
        (SELECT COUNT(*) FROM Trn.Certificate) AS NewCertificateCount,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_CertificateEmailLog]) AS OldEmailLogCount,
        (SELECT COUNT(*) FROM Trn.CertificateEmailLog) AS NewEmailLogCount,
        (SELECT COUNT(*) FROM Trn.Certificate WHERE CreatedById IS NULL) AS CertificateUnmappedUserCount,
        (SELECT COUNT(*) FROM Trn.CertificateEmailLog WHERE CreatedById IS NULL) AS EmailLogUnmappedUserCount;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY SET IDENTITY_INSERT Trn.Certificate OFF; END TRY BEGIN CATCH END CATCH;
    BEGIN TRY SET IDENTITY_INSERT Trn.CertificateEmailLog OFF; END TRY BEGIN CATCH END CATCH;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
