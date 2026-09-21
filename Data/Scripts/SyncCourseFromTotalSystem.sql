-- ============================================
-- مهاجرت Trn.Course
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع:  dbo.Training_Course
-- مقصد:  Trn.Course
--
-- نگاشت:
--   Id / CourseBankId / TeacherId / SecondTeacherId / CompanyId / AgentId
--       <- همان شناسه‌ها اگر در جداول مقصد موجود باشند
--   TrainingCode       <- TrainingCode؛ در صورت تکرار، برای ردیف‌های بعدی «کد-Id»
--   ExecutingMethod    <- «فراخوان عمومی»=635 / «اختصاصی»=636
--   CngCourseType      <- CngCourseType (Lookup 123)؛ ناشناخته → 0 (عمومی)
--   Status             <- StatusId (108)
--   FinancialStatus    <- FinancialStatusId (109)
--   MethodOfHolding / CertificateStatus / SendInvitation / PaymentType / AttendanceStatus
--                      <- Lookup Idهای متناظر (در داده فعلی همه NULL)
--   Start/End          <- StartDate/StartDateInText ، EndDate/EndDateInText
--   RegistrationDeadline* <- NULL (در TMS همه 0001-01-01 / 1401/11/04 ساختگی است)
--   LocationId         <- Gnr_City.City_Title نرمال‌شده → Gnr.Region.Name
--   RelatedUnitId      <- HRM_OrgUnit.Hamkaran_Unit_FK → Hrm.OrgUnit.HamkaranUnitId
--   Cost               <- CourseCost
--   Description        <- Description  (Content / ExecuteCondition / CourseTitle منتقل نمی‌شوند)
--   SendInvitationShamsiDate <- SendInvitationDate (میلادی در قدیم نبود)
--   AttendingTrainingCourseShamsiDate <- AttendingTrainingCourseDate
--   CreatedBy          <- Username + alias allahverdi.s -> allahvirdi.s
--   IsActive           = 1
--
-- پیش‌نیاز: CourseBank، TeacherBank، Company، Agent
-- ============================================

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    IF OBJECT_ID('tempdb..#CityMap') IS NOT NULL DROP TABLE #CityMap;
    IF OBJECT_ID('tempdb..#NormRegion') IS NOT NULL DROP TABLE #NormRegion;

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
        FROM [TMS].[TotalSystem].[dbo].[Training_Course] c
        WHERE LTRIM(RTRIM(c.ExecutingMethod)) NOT IN (N'فراخوان عمومی', N'اختصاصی')
           OR c.ExecutingMethod IS NULL
    )
    BEGIN
        RAISERROR(N'مقدار ExecutingMethod ناشناخته در Training_Course وجود دارد.', 16, 1);
    END;

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Training_Course] c
        WHERE c.StatusId NOT IN (617, 618, 619, 620, 621)
    )
    BEGIN
        RAISERROR(N'مقدار StatusId ناشناخته در Training_Course وجود دارد.', 16, 1);
    END;

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Training_Course] c
        WHERE c.FinancialStatusId NOT IN (622, 623)
    )
    BEGIN
        RAISERROR(N'مقدار FinancialStatusId ناشناخته در Training_Course وجود دارد.', 16, 1);
    END;

    SELECT
        r.Id,
        r.Type,
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            r.Name,
            NCHAR(8204), N''), NCHAR(160), N''), N'ي', N'ی'), N'ك', N'ک'),
            N' ', N''), NCHAR(1616), N''), N'ء', N''), N'‌', N''), N'استان', N'') AS Norm
    INTO #NormRegion
    FROM Gnr.Region r;

    UPDATE #NormRegion SET Norm = REPLACE(Norm, N'بندر', N'');

    SELECT City_ID, RegionId
    INTO #CityMap
    FROM
    (
        SELECT
            city.City_ID,
            nr.Id AS RegionId,
            ROW_NUMBER() OVER (
                PARTITION BY city.City_ID
                ORDER BY CASE nr.Type WHEN 3 THEN 0 WHEN 2 THEN 1 ELSE 2 END, nr.Id
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_City] city
        INNER JOIN #NormRegion nr
            ON nr.Norm = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    city.City_Title,
                    NCHAR(8204), N''), NCHAR(160), N''), N'ي', N'ی'), N'ك', N'ک'),
                    N' ', N''), NCHAR(1616), N''), N'ء', N''), N'‌', N''), N'استان', N''), N'بندر', N'')
        WHERE NULLIF(nr.Norm, N'') IS NOT NULL
    ) x
    WHERE x.rn = 1;

    DELETE FROM Trn.Course;

    SET IDENTITY_INSERT Trn.Course ON;

    INSERT INTO Trn.Course
    (
        Id,
        CourseBankId,
        TrainingCode,
        CngCourseType,
        ExecutingMethod,
        MethodOfHolding,
        StartMiladiDate,
        StartShamsiDate,
        EndMiladiDate,
        EndShamsiDate,
        DurationInMinute,
        Days,
        LocationId,
        ExecutingPlace,
        CngPlace,
        RegistrationDeadlineMiladiDate,
        RegistrationDeadlineShamsiDate,
        TeacherId,
        SecondTeacherId,
        HasSynergyHistory,
        AgentId,
        CompanyId,
        RelatedUnitId,
        Capacity,
        RemainingCapacity,
        Status,
        FinancialStatus,
        CertificateStatus,
        SendInvitation,
        PaymentType,
        AttendanceStatus,
        IsEvaluated,
        Cost,
        HeadToTheSeriesPoint,
        HasCertificates,
        HasExam,
        HasVat,
        Description,
        AgentName,
        FirstPhoneNumber,
        SecondPhoneNumber,
        ShowStartupDate,
        NumberOfDevices,
        NumberOfPeople,
        InvitationSent,
        SendInvitationMiladiDate,
        SendInvitationShamsiDate,
        RecipientOfInvitation,
        AttendingTrainingCourseMiladiDate,
        AttendingTrainingCourseShamsiDate,
        PlaceOfTrainingCourse,
        TrainingCourseTitles,
        TrainingCourseTitles1,
        TrainingCourseTitles2,
        TrainingCourseTitles3,
        TrainingCourseTitles4,
        TrainingCourseTitles5,
        TrainingCourseTitles6,
        TrainingCourseTitles7,
        TrainingCourseTitles8,
        TrainingCourseTitles9,
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
        CAST(c.Id AS BIGINT) AS Id,
        CAST(c.CourseBankId AS BIGINT) AS CourseBankId,
        CASE
            WHEN ROW_NUMBER() OVER (PARTITION BY c.TrainingCode ORDER BY c.Id) = 1
                THEN NULLIF(LTRIM(RTRIM(c.TrainingCode)), N'')
            ELSE NULLIF(LTRIM(RTRIM(c.TrainingCode)), N'') + N'-' + CAST(c.Id AS NVARCHAR(20))
        END AS TrainingCode,
        CASE
            WHEN c.CngCourseType IN (701, 702, 703, 1184, 2411, 2412, 2787, 2788) THEN CAST(c.CngCourseType AS INT)
            WHEN c.CngCourseType IS NULL THEN NULL
            ELSE 0
        END AS CngCourseType,
        CASE LTRIM(RTRIM(c.ExecutingMethod))
            WHEN N'فراخوان عمومی' THEN 635
            WHEN N'اختصاصی' THEN 636
        END AS ExecutingMethod,
        CASE WHEN c.MethodOfHoldingId IN (2379, 2380, 2381) THEN CAST(c.MethodOfHoldingId AS INT) END AS MethodOfHolding,
        CAST(c.StartDate AS DATETIME2) AS StartMiladiDate,
        NULLIF(LTRIM(RTRIM(c.StartDateInText)), N'') AS StartShamsiDate,
        CAST(c.EndDate AS DATETIME2) AS EndMiladiDate,
        NULLIF(LTRIM(RTRIM(c.EndDateInText)), N'') AS EndShamsiDate,
        c.DurationInMinute,
        c.Days,
        cm.RegionId AS LocationId,
        NULLIF(LTRIM(RTRIM(c.ExecutingPlace)), N'') AS ExecutingPlace,
        NULLIF(LTRIM(RTRIM(c.CngPlace)), N'') AS CngPlace,
        NULL AS RegistrationDeadlineMiladiDate,
        NULL AS RegistrationDeadlineShamsiDate,
        CAST(c.TeacherId AS BIGINT) AS TeacherId,
        CASE WHEN tb2.Id IS NOT NULL THEN CAST(c.SecondTeacherId AS BIGINT) END AS SecondTeacherId,
        c.HasSynergyHistory,
        ag.Id AS AgentId,
        co.Id AS CompanyId,
        ou.Id AS RelatedUnitId,
        CAST(c.Capacity AS SMALLINT) AS Capacity,
        CAST(c.RemainingCapacity AS SMALLINT) AS RemainingCapacity,
        CAST(c.StatusId AS INT) AS Status,
        CAST(c.FinancialStatusId AS INT) AS FinancialStatus,
        CASE WHEN c.CertificateStatusId IN (2393, 2394, 2395) THEN CAST(c.CertificateStatusId AS INT) END AS CertificateStatus,
        CASE WHEN c.SendInvitationId IN (2390, 2391, 2392) THEN CAST(c.SendInvitationId AS INT) END AS SendInvitation,
        CASE WHEN c.PaymentTypeId IN (2396, 2397) THEN CAST(c.PaymentTypeId AS INT) END AS PaymentType,
        CASE WHEN c.AttendanceStatusId IN (2398, 2399, 2400, 2401) THEN CAST(c.AttendanceStatusId AS INT) END AS AttendanceStatus,
        c.IsEvaluated,
        CAST(c.CourseCost AS DECIMAL(18, 2)) AS Cost,
        c.HeadToTheSeriesPoint,
        c.HasCertificates,
        c.HasExam,
        c.HasVat,
        NULLIF(LTRIM(RTRIM(c.Description)), N'') AS Description,
        NULLIF(LTRIM(RTRIM(c.AgentName)), N'') AS AgentName,
        NULLIF(LTRIM(RTRIM(c.FirstPhoneNumber)), N'') AS FirstPhoneNumber,
        NULLIF(LTRIM(RTRIM(c.SecondPhoneNumber)), N'') AS SecondPhoneNumber,
        NULLIF(LTRIM(RTRIM(c.ShowStartupDate)), N'') AS ShowStartupDate,
        c.NumberOfDevices,
        c.NumberOfPeople,
        c.InvitationSent,
        NULL AS SendInvitationMiladiDate,
        NULLIF(LTRIM(RTRIM(c.SendInvitationDate)), N'') AS SendInvitationShamsiDate,
        NULLIF(LTRIM(RTRIM(c.RecipientOfInvitation)), N'') AS RecipientOfInvitation,
        NULL AS AttendingTrainingCourseMiladiDate,
        NULLIF(LTRIM(RTRIM(c.AttendingTrainingCourseDate)), N'') AS AttendingTrainingCourseShamsiDate,
        NULLIF(LTRIM(RTRIM(c.PlaceOfTrainingCourse)), N'') AS PlaceOfTrainingCourse,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles)), N'') AS TrainingCourseTitles,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles1)), N'') AS TrainingCourseTitles1,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles2)), N'') AS TrainingCourseTitles2,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles3)), N'') AS TrainingCourseTitles3,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles4)), N'') AS TrainingCourseTitles4,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles5)), N'') AS TrainingCourseTitles5,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles6)), N'') AS TrainingCourseTitles6,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles7)), N'') AS TrainingCourseTitles7,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles8)), N'') AS TrainingCourseTitles8,
        NULLIF(LTRIM(RTRIM(c.TrainingCourseTitles9)), N'') AS TrainingCourseTitles9,
        um.NewUserId AS CreatedById,
        NULL AS ModifiedById,
        um.NewUserName AS CreatedByName,
        NULL AS ModifiedByName,
        c.CreatedDate AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(c.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        NULL AS ModifiedDateMiladiDateTime,
        NULL AS ModifiedDateShamsiDateTime,
        1 AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Training_Course] c
    INNER JOIN Trn.CourseBank cb
        ON cb.Id = c.CourseBankId
    INNER JOIN Trn.TeacherBank tb
        ON tb.Id = c.TeacherId
    LEFT JOIN Trn.TeacherBank tb2
        ON tb2.Id = c.SecondTeacherId
    LEFT JOIN Trn.Company co
        ON co.Id = c.CompanyId
    LEFT JOIN Trn.Agent ag
        ON ag.Id = c.AgentId
    LEFT JOIN #CityMap cm
        ON cm.City_ID = c.LocationId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hrm
        ON hrm.OrgUnit_ID = c.RelatedUnitId
    LEFT JOIN Hrm.OrgUnit ou
        ON ou.HamkaranUnitId = hrm.Hamkaran_Unit_FK
    LEFT JOIN #UserMap um
        ON um.OldUserId = c.CreatedUserId;

    SET IDENTITY_INSERT Trn.Course OFF;
    DBCC CHECKIDENT ('Trn.Course', RESEED) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت Course با موفقیت انجام شد.';

    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Training_Course]) AS OldCount,
        (SELECT COUNT(*) FROM Trn.Course) AS NewCount,
        (SELECT COUNT(*) FROM Trn.Course WHERE CreatedById IS NULL) AS UnmappedUserCount,
        (SELECT COUNT(*) FROM Trn.Course WHERE LocationId IS NULL) AS MissingLocationCount,
        (SELECT COUNT(*) FROM Trn.Course WHERE AgentId IS NOT NULL) AS WithAgentCount,
        (SELECT COUNT(*) FROM Trn.Course WHERE CompanyId IS NOT NULL) AS WithCompanyCount,
        (SELECT COUNT(*) FROM Trn.Course WHERE CHARINDEX(N'-', TrainingCode) > 0) AS SuffixedCodeCount;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY
        SET IDENTITY_INSERT Trn.Course OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
