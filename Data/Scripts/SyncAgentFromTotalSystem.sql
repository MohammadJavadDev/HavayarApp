-- ============================================
-- مهاجرت Trn.Agent
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع:  dbo.Training_Agent
-- مقصد:  Trn.Agent
--
-- نگاشت:
--   Id              <- Id  (حفظ شناسه برای FK دوره)
--   FirstName       <- Firstname
--   LastName        <- Lastname
--   Zone            <- Crm_Zone.Zone_Title (ZoneId)
--   Phone           <- LEFT(TRIM Phone, 20)
--   Mobile          <- NULL (در سیستم قدیم ستون ندارد)
--   Address         <- Address
--   CreatedBy       <- Gnr_User.Username -> system.[User].Username
--   IsActive        = 1
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

    DELETE FROM Trn.Agent;

    SET IDENTITY_INSERT Trn.Agent ON;

    INSERT INTO Trn.Agent
    (
        Id,
        FirstName,
        LastName,
        Zone,
        Mobile,
        Phone,
        Address,
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
        CAST(a.Id AS BIGINT) AS Id,
        NULLIF(LTRIM(RTRIM(a.Firstname)), N'') AS FirstName,
        NULLIF(LTRIM(RTRIM(a.Lastname)), N'') AS LastName,
        NULLIF(LTRIM(RTRIM(z.Zone_Title)), N'') AS Zone,
        NULL AS Mobile,
        LEFT(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(a.Phone, N''))), CHAR(13), N''), CHAR(10), N''), 20) AS Phone,
        NULLIF(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(a.Address, N''))), CHAR(13), N' '), CHAR(10), N' '), N'') AS Address,
        um.NewUserId AS CreatedById,
        NULL AS ModifiedById,
        um.NewUserName AS CreatedByName,
        NULL AS ModifiedByName,
        a.CreatedDate AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(a.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        NULL AS ModifiedDateMiladiDateTime,
        NULL AS ModifiedDateShamsiDateTime,
        1 AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_Agent] a
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Zone] z
        ON z.Zone_ID = a.ZoneId
    LEFT JOIN #UserMap um
        ON um.OldUserId = a.CreatedUserId;

    SET IDENTITY_INSERT Trn.Agent OFF;
    DBCC CHECKIDENT ('Trn.Agent', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت Agent با موفقیت انجام شد.';

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_Agent]) AS OldCount,
        (SELECT COUNT(*) FROM Trn.Agent) AS NewCount;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY
        SET IDENTITY_INSERT Trn.Agent OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
