-- ============================================
-- مهاجرت Trn.CourseBank
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع:  dbo.Training_CourseBank
-- مقصد:  Trn.CourseBank
--
-- نگاشت:
--   Id                    <- Id  (حفظ شناسه برای FK دوره)
--   CourseField           <- CourseFieldId  (502/504/505)
--   CourseFieldTitle      <- CourseFieldTitle
--   CourseFieldTitleInLatin <- CourseFieldTitleInLatin
--   Type                  <- Type  (698/699/700 یا NULL)
--   Duration              <- Duration  (ساعت)
--   CreatedById/Name      <- Gnr_User.Username -> system.[User].Username
--   CreatedOnMiladiDateTime <- CreatedDate
--   CreatedOnShamsiDateTime <- CreatedDateInText
--   IsActive              = 1 (فعال)
--
-- قبل از اجرا:
--   1) روی دیتابیس HavayarApp اجرا شود
--   2) Linked Server [TMS] به TotalSystem در دسترس باشد
--   3) جدول Trn.CourseBank ساخته شده باشد
--   4) Trn.Course نباید به ردیف‌های CourseBank وابسته باشد
--      (در غیر این صورت DELETE به خاطر FK شکست می‌خورد)
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

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Training_CourseBank] cb
        WHERE cb.CourseFieldId NOT IN (502, 504, 505)
    )
    BEGIN
        RAISERROR(N'مقدار CourseFieldId ناشناخته در Training_CourseBank وجود دارد.', 16, 1);
    END;

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Training_CourseBank] cb
        WHERE cb.Type IS NOT NULL
          AND cb.Type NOT IN (698, 699, 700)
    )
    BEGIN
        RAISERROR(N'مقدار Type ناشناخته در Training_CourseBank وجود دارد.', 16, 1);
    END;

    DELETE FROM Trn.CourseBank;

    SET IDENTITY_INSERT Trn.CourseBank ON;

    INSERT INTO Trn.CourseBank
    (
        Id,
        CourseField,
        CourseFieldTitle,
        CourseFieldTitleInLatin,
        Type,
        Duration,
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
        CAST(cb.Id AS BIGINT)                                      AS Id,
        CAST(cb.CourseFieldId AS INT)                              AS CourseField,
        cb.CourseFieldTitle,
        cb.CourseFieldTitleInLatin,
        CAST(cb.Type AS INT)                                       AS Type,
        cb.Duration,
        um.NewUserId                                               AS CreatedById,
        NULL                                                       AS ModifiedById,
        um.NewUserName                                             AS CreatedByName,
        NULL                                                       AS ModifiedByName,
        cb.CreatedDate                                             AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(cb.CreatedDateInText)), N'')            AS CreatedOnShamsiDateTime,
        NULL                                                       AS ModifiedDateMiladiDateTime,
        NULL                                                       AS ModifiedDateShamsiDateTime,
        1                                                          AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_CourseBank] cb
    LEFT JOIN #UserMap um
        ON um.OldUserId = cb.CreatedUserId;

    SET IDENTITY_INSERT Trn.CourseBank OFF;
    DBCC CHECKIDENT ('Trn.CourseBank', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت CourseBank با موفقیت انجام شد.';

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_CourseBank]) AS OldCount,
        (SELECT COUNT(*) FROM Trn.CourseBank) AS NewCount;

    SELECT
        cb.Id              AS OldId,
        cb.CreatedUserId   AS OldCreatedUserId,
        ou.Username        AS OldUsername
    FROM [TMS].[TotalSystem].[dbo].[Training_CourseBank] cb
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        ON ou.User_ID = cb.CreatedUserId
    LEFT JOIN #UserMap um
        ON um.OldUserId = cb.CreatedUserId
    WHERE um.NewUserId IS NULL;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY
        SET IDENTITY_INSERT Trn.CourseBank OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
