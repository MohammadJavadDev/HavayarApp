-- ============================================
-- مهاجرت Trn.Participant
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع:  dbo.Training_Participant
-- مقصد:  Trn.Participant
--
-- فایل / عکس / پیوست منتقل نمی‌شود (PhotoFileId = NULL).
--
-- نگاشت:
--   Id                    <- Id
--   CompanyId             <- CompanyId اگر در Trn.Company موجود باشد
--   FirstName / LastName  <- FirstName / LastName
--   FirstNameInLatin / LastNameInLatin <- FirstNameInLatin / LastNameInLatin
--   FatherName            <- FatherName
--   NationalCode          <- NationalCode
--   BirthCertificateNo    <- CAST(BirthCertificateNo AS nvarchar)
--   BirthdayYear          <- BirthdayYear
--   IssuedPlace           <- IssuedPlace
--   Gender                <- Gender (695 آقا / 696 خانم)
--   Education             <- EducationId
--   Mobile                <- LEFT(MobileNumber, 20)
--   Phone                 <- LEFT(PhoneNumber یا DirectPhoneNumber, 20)
--   Email / PostalCode / Address
--   Workplace             <- WorkplaceName
--   JobPosition / FieldOfStudy / PartyId / PhotoFileId <- NULL
--   CreatedBy / ModifiedBy <- Username + alias allahverdi.s -> allahvirdi.s
--   IsActive              = 1
--
-- پیش‌نیاز: Trn.Company از قبل مهاجرت شده باشد.
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
        FROM [TMS].[TotalSystem].[dbo].[Training_Participant] p
        WHERE p.Gender IS NOT NULL
          AND p.Gender NOT IN (695, 696)
    )
    BEGIN
        RAISERROR(N'مقدار Gender ناشناخته در Training_Participant وجود دارد.', 16, 1);
    END;

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Training_Participant] p
        WHERE p.EducationId IS NOT NULL
          AND p.EducationId NOT IN (506, 507, 508, 509, 510, 2457, 2458, 2459, 2460, 2461)
    )
    BEGIN
        RAISERROR(N'مقدار EducationId ناشناخته در Training_Participant وجود دارد.', 16, 1);
    END;

    DELETE FROM Trn.Participant;

    SET IDENTITY_INSERT Trn.Participant ON;

    INSERT INTO Trn.Participant
    (
        Id,
        FirstName,
        LastName,
        FirstNameInLatin,
        LastNameInLatin,
        FatherName,
        NationalCode,
        BirthCertificateNo,
        BirthdayYear,
        IssuedPlace,
        Gender,
        Education,
        FieldOfStudy,
        Mobile,
        Phone,
        Email,
        PostalCode,
        Address,
        Workplace,
        JobPosition,
        CompanyId,
        PhotoFileId,
        Description,
        PartyId,
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
        CAST(p.Id AS BIGINT)                                       AS Id,
        NULLIF(LTRIM(RTRIM(p.FirstName)), N'')                     AS FirstName,
        NULLIF(LTRIM(RTRIM(p.LastName)), N'')                      AS LastName,
        NULLIF(LTRIM(RTRIM(p.FirstNameInLatin)), N'')              AS FirstNameInLatin,
        NULLIF(LTRIM(RTRIM(p.LastNameInLatin)), N'')               AS LastNameInLatin,
        NULLIF(LTRIM(RTRIM(p.FatherName)), N'')                    AS FatherName,
        NULLIF(LTRIM(RTRIM(p.NationalCode)), N'')                  AS NationalCode,
        CASE
            WHEN p.BirthCertificateNo IS NULL THEN NULL
            ELSE CAST(p.BirthCertificateNo AS NVARCHAR(20))
        END                                                        AS BirthCertificateNo,
        CAST(p.BirthdayYear AS INT)                                AS BirthdayYear,
        NULLIF(LTRIM(RTRIM(p.IssuedPlace)), N'')                   AS IssuedPlace,
        p.Gender                                                   AS Gender,
        CAST(p.EducationId AS INT)                                 AS Education,
        NULL                                                       AS FieldOfStudy,
        LEFT(NULLIF(LTRIM(RTRIM(p.MobileNumber)), N''), 20)        AS Mobile,
        LEFT(COALESCE(
            NULLIF(LTRIM(RTRIM(p.PhoneNumber)), N''),
            NULLIF(LTRIM(RTRIM(p.DirectPhoneNumber)), N'')
        ), 20)                                                     AS Phone,
        NULLIF(LTRIM(RTRIM(p.Email)), N'')                         AS Email,
        NULLIF(LTRIM(RTRIM(p.PostalCode)), N'')                    AS PostalCode,
        NULLIF(LTRIM(RTRIM(p.Address)), N'')                       AS Address,
        NULLIF(LTRIM(RTRIM(p.WorkplaceName)), N'')                 AS Workplace,
        NULL                                                       AS JobPosition,
        nc.Id                                                      AS CompanyId,
        NULL                                                       AS PhotoFileId,
        NULL                                                       AS Description,
        NULL                                                       AS PartyId,
        umc.NewUserId                                              AS CreatedById,
        umu.NewUserId                                              AS ModifiedById,
        umc.NewUserName                                            AS CreatedByName,
        umu.NewUserName                                            AS ModifiedByName,
        p.CreatedDate                                              AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(p.CreatedDateInText)), N'')             AS CreatedOnShamsiDateTime,
        p.UpdatedDate                                              AS ModifiedDateMiladiDateTime,
        NULLIF(LTRIM(RTRIM(p.UpdatedDateInText)), N'')             AS ModifiedDateShamsiDateTime,
        1                                                          AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_Participant] p
    LEFT JOIN Trn.Company nc
        ON nc.Id = p.CompanyId
    LEFT JOIN #UserMap umc
        ON umc.OldUserId = p.CreatedUserId
    LEFT JOIN #UserMap umu
        ON umu.OldUserId = p.UpdatedUserId;

    SET IDENTITY_INSERT Trn.Participant OFF;
    DBCC CHECKIDENT ('Trn.Participant', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت Participant با موفقیت انجام شد.';

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_Participant]) AS OldCount,
        (SELECT COUNT(*) FROM Trn.Participant) AS NewCount,
        (SELECT COUNT(*) FROM Trn.Participant WHERE CreatedById IS NULL) AS UnmappedUserCount,
        (SELECT COUNT(*) FROM Trn.Participant WHERE CompanyId IS NOT NULL) AS WithCompanyCount,
        (SELECT COUNT(*) FROM Trn.Participant WHERE PhotoFileId IS NOT NULL) AS WithPhotoCount,
        (SELECT COUNT(*) FROM Trn.Participant WHERE FirstNameInLatin IS NOT NULL) AS WithFirstNameInLatinCount,
        (SELECT COUNT(*) FROM Trn.Participant WHERE LastNameInLatin IS NOT NULL) AS WithLastNameInLatinCount;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY
        SET IDENTITY_INSERT Trn.Participant OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
