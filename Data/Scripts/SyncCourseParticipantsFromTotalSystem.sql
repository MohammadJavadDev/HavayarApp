-- ============================================
-- مهاجرت شرکت‌کنندگان دوره
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع:  dbo.Training_CourseParticipant
--         dbo.Training_CourseCompanyParticipant
-- مقصد:  Trn.CourseParticipant          (فراخوان عمومی)
--         Trn.CourseCompanyParticipant  (اختصاصی)
--
-- نگاشت عمومی:
--   Id / CourseId / ParticipantId / CompanyId / CngScore / Place
--   ParticipantRegisterStatus <- ParticipantRegisterStatusId (624/625؛ 0 → NULL)
--   Description               <- NULL (در قدیم ستون ندارد)
--   CreatedBy                 <- NULL (در قدیم ستون ندارد)
--   IsActive                  = 1
--   ردیف بدون ParticipantId حذف می‌شود (Id=4892)
--
-- نگاشت اختصاصی:
--   Id / CourseId / ParticipantId / Score
--   ParticipantRegisterStatus <- NULL (در قدیم ستون ندارد)
--   Description               <- NULL
--   CreatedBy                 <- Username + alias allahverdi.s -> allahvirdi.s
--   IsActive                  = 1
--
-- پیش‌نیاز: Trn.Course ، Trn.Participant ، Trn.Company
-- RemainingCapacity دوره دست نخورده می‌ماند (مقدار مهاجرت‌شده Course حفظ می‌شود).
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

    DELETE FROM Trn.CourseParticipant;
    DELETE FROM Trn.CourseCompanyParticipant;

    SET IDENTITY_INSERT Trn.CourseParticipant ON;

    INSERT INTO Trn.CourseParticipant
    (
        Id,
        CourseId,
        ParticipantId,
        CompanyId,
        CngScore,
        Place,
        ParticipantRegisterStatus,
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
        CAST(p.Id AS BIGINT) AS Id,
        CAST(p.CourseId AS BIGINT) AS CourseId,
        CAST(p.ParticipantId AS BIGINT) AS ParticipantId,
        co.Id AS CompanyId,
        p.CngScore,
        NULLIF(LTRIM(RTRIM(p.Place)), N'') AS Place,
        CASE WHEN p.ParticipantRegisterStatusId IN (624, 625) THEN CAST(p.ParticipantRegisterStatusId AS INT) END AS ParticipantRegisterStatus,
        NULL AS Description,
        NULL AS CreatedById,
        NULL AS ModifiedById,
        NULL AS CreatedByName,
        NULL AS ModifiedByName,
        NULL AS CreatedOnMiladiDateTime,
        NULL AS CreatedOnShamsiDateTime,
        NULL AS ModifiedDateMiladiDateTime,
        NULL AS ModifiedDateShamsiDateTime,
        1 AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_CourseParticipant] p
    INNER JOIN Trn.Course cr
        ON cr.Id = p.CourseId
    INNER JOIN Trn.Participant pt
        ON pt.Id = p.ParticipantId
    LEFT JOIN Trn.Company co
        ON co.Id = p.CompanyId;

    SET IDENTITY_INSERT Trn.CourseParticipant OFF;
    DBCC CHECKIDENT ('Trn.CourseParticipant', RESEED) WITH NO_INFOMSGS;

    SET IDENTITY_INSERT Trn.CourseCompanyParticipant ON;

    INSERT INTO Trn.CourseCompanyParticipant
    (
        Id,
        CourseId,
        ParticipantId,
        Score,
        ParticipantRegisterStatus,
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
        CAST(p.Id AS BIGINT) AS Id,
        CAST(p.CourseId AS BIGINT) AS CourseId,
        CAST(p.ParticipantId AS BIGINT) AS ParticipantId,
        p.Score,
        NULL AS ParticipantRegisterStatus,
        NULL AS Description,
        um.NewUserId AS CreatedById,
        NULL AS ModifiedById,
        um.NewUserName AS CreatedByName,
        NULL AS ModifiedByName,
        p.CreatedDate AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(p.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        NULL AS ModifiedDateMiladiDateTime,
        NULL AS ModifiedDateShamsiDateTime,
        1 AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_CourseCompanyParticipant] p
    INNER JOIN Trn.Course cr
        ON cr.Id = p.CourseId
    INNER JOIN Trn.Participant pt
        ON pt.Id = p.ParticipantId
    LEFT JOIN #UserMap um
        ON um.OldUserId = p.CreatedUserId;

    SET IDENTITY_INSERT Trn.CourseCompanyParticipant OFF;
    DBCC CHECKIDENT ('Trn.CourseCompanyParticipant', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت شرکت‌کنندگان دوره با موفقیت انجام شد.';

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_CourseParticipant]) AS OldPublicCount,
        (SELECT COUNT(*) FROM Trn.CourseParticipant) AS NewPublicCount,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_CourseCompanyParticipant]) AS OldPrivateCount,
        (SELECT COUNT(*) FROM Trn.CourseCompanyParticipant) AS NewPrivateCount,
        (SELECT COUNT(*) FROM Trn.CourseCompanyParticipant WHERE CreatedById IS NULL) AS PrivateUnmappedUserCount;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY SET IDENTITY_INSERT Trn.CourseParticipant OFF; END TRY BEGIN CATCH END CATCH;
    BEGIN TRY SET IDENTITY_INSERT Trn.CourseCompanyParticipant OFF; END TRY BEGIN CATCH END CATCH;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
