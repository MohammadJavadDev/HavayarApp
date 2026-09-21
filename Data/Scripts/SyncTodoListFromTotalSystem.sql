-- ============================================
-- مهاجرت Trn.TodoList
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع:  dbo.Training_TodoList
-- مقصد:  Trn.TodoList
--
-- نگاشت:
--   Id                 <- Id
--   CompanyId          <- CompanyId اگر در Trn.Company موجود باشد
--   Title              <- Title
--   Description        <- Description
--   TodoMiladiDate     <- TodoDate
--   TodoShamsiDate     <- TodoDateInText
--   CreatedBy          <- Gnr_User.Username -> system.[User].Username
--                         + alias allahverdi.s -> allahvirdi.s
--   IsActive           = 1
--
-- IsDone در سیستم جدید ستون ندارد و منتقل نمی‌شود.
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

    DELETE FROM Trn.TodoList;

    SET IDENTITY_INSERT Trn.TodoList ON;

    INSERT INTO Trn.TodoList
    (
        Id,
        CompanyId,
        Title,
        Description,
        TodoShamsiDate,
        TodoMiladiDate,
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
        CAST(t.Id AS BIGINT)                                       AS Id,
        nc.Id                                                      AS CompanyId,
        NULLIF(LTRIM(RTRIM(t.Title)), N'')                         AS Title,
        NULLIF(LTRIM(RTRIM(t.Description)), N'')                   AS Description,
        NULLIF(LTRIM(RTRIM(t.TodoDateInText)), N'')                AS TodoShamsiDate,
        CAST(t.TodoDate AS DATETIME2)                              AS TodoMiladiDate,
        um.NewUserId                                               AS CreatedById,
        NULL                                                       AS ModifiedById,
        um.NewUserName                                             AS CreatedByName,
        NULL                                                       AS ModifiedByName,
        t.CreatedDate                                              AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(t.CreatedDateInText)), N'')             AS CreatedOnShamsiDateTime,
        NULL                                                       AS ModifiedDateMiladiDateTime,
        NULL                                                       AS ModifiedDateShamsiDateTime,
        1                                                          AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_TodoList] t
    LEFT JOIN Trn.Company nc
        ON nc.Id = t.CompanyId
    LEFT JOIN #UserMap um
        ON um.OldUserId = t.CreatedUserId;

    SET IDENTITY_INSERT Trn.TodoList OFF;
    DBCC CHECKIDENT ('Trn.TodoList', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت TodoList با موفقیت انجام شد.';

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_TodoList]) AS OldCount,
        (SELECT COUNT(*) FROM Trn.TodoList) AS NewCount,
        (SELECT COUNT(*) FROM Trn.TodoList WHERE CreatedById IS NULL) AS UnmappedUserCount,
        (SELECT COUNT(*) FROM Trn.TodoList WHERE CompanyId IS NOT NULL) AS WithCompanyCount;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY
        SET IDENTITY_INSERT Trn.TodoList OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
