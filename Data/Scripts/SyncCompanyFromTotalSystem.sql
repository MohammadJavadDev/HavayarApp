-- ============================================
-- مهاجرت Trn.Company
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع:  dbo.Training_Company
-- مقصد:  Trn.Company
--
-- نگاشت:
--   Id                    <- Id  (حفظ شناسه برای FK شرکت‌کننده / دوره)
--   Title                 <- Title
--   Phone                 <- PhoneNumber
--   Fax                   <- FaxNumber
--   Email                 <- Email
--   Address               <- Address
--   ActivityKind          <- عنوان Gnr_Lookup (LookupType_FK = 103) از ActivityFieldId
--   PartyId               <- Gnr_ManCompany.Hamkaran_ManCompany_FK -> Gnr.Party.HamkaranId
--                           (در صورت نبود، Party.Id = ManCompanyId)
--   CeoName / Website     <- NULL (در سیستم قدیم ستون ندارند)
--   Description           <- TitleInLatin (برای از دست نرفتن عنوان لاتین)
--   CreatedBy / ModifiedBy <- Gnr_User.Username -> system.[User].Username
--                            + alias allahverdi.s -> allahvirdi.s
--   IsActive              = 1
--
-- قبل از اجرا:
--   1) روی دیتابیس HavayarApp اجرا شود
--   2) Linked Server [TMS] در دسترس باشد
--   3) جداول وابسته (Participant/Course/...) خالی باشند یا FK مانع DELETE نشود
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

    DELETE FROM Trn.Company;

    SET IDENTITY_INSERT Trn.Company ON;

    INSERT INTO Trn.Company
    (
        Id,
        Title,
        CeoName,
        Website,
        Email,
        Phone,
        Fax,
        Address,
        ActivityKind,
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
        CAST(c.Id AS BIGINT)                                       AS Id,
        NULLIF(LTRIM(RTRIM(c.Title)), N'')                         AS Title,
        NULL                                                       AS CeoName,
        NULL                                                       AS Website,
        NULLIF(LTRIM(RTRIM(c.Email)), N'')                         AS Email,
        NULLIF(LTRIM(RTRIM(c.PhoneNumber)), N'')                   AS Phone,
        NULLIF(LTRIM(RTRIM(c.FaxNumber)), N'')                     AS Fax,
        NULLIF(LTRIM(RTRIM(c.Address)), N'')                       AS Address,
        lk.Lookup_Title_Fa                                         AS ActivityKind,
        NULLIF(LTRIM(RTRIM(c.TitleInLatin)), N'')                  AS Description,
        COALESCE(pyHam.Id, pyId.Id)                                AS PartyId,
        umc.NewUserId                                              AS CreatedById,
        umu.NewUserId                                              AS ModifiedById,
        umc.NewUserName                                            AS CreatedByName,
        umu.NewUserName                                            AS ModifiedByName,
        c.CreatedDate                                              AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(c.CreatedDateInText)), N'')             AS CreatedOnShamsiDateTime,
        c.UpdatedDate                                              AS ModifiedDateMiladiDateTime,
        NULLIF(LTRIM(RTRIM(c.UpdatedDateInText)), N'')             AS ModifiedDateShamsiDateTime,
        1                                                          AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_Company] c
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Lookup] lk
        ON lk.Lookup_ID = c.ActivityFieldId
       AND lk.LookupType_FK = 103
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
        ON mc.ManCompany_ID = c.ManCompanyId
    LEFT JOIN Gnr.Party pyHam
        ON pyHam.HamkaranId = mc.Hamkaran_ManCompany_FK
       AND mc.Hamkaran_ManCompany_FK > 0
    LEFT JOIN Gnr.Party pyId
        ON pyId.Id = c.ManCompanyId
    LEFT JOIN #UserMap umc
        ON umc.OldUserId = c.CreatedUserId
    LEFT JOIN #UserMap umu
        ON umu.OldUserId = c.UpdatedUserId;

    SET IDENTITY_INSERT Trn.Company OFF;
    DBCC CHECKIDENT ('Trn.Company', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت Company با موفقیت انجام شد.';

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_Company]) AS OldCount,
        (SELECT COUNT(*) FROM Trn.Company) AS NewCount,
        (SELECT COUNT(*) FROM Trn.Company WHERE CreatedById IS NULL) AS UnmappedUserCount,
        (SELECT COUNT(*) FROM Trn.Company WHERE PartyId IS NOT NULL) AS WithPartyCount;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY
        SET IDENTITY_INSERT Trn.Company OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
