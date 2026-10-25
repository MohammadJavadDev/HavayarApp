/*
Seed_HrmResume_FromTotalSystem.sql
انتقال یک‌باره و تکرارپذیر رزومه (New) از TotalSystem.
جدول‌های مصاحبه قدیمی، رفتاری، و جدول‌های خالی نمی‌آیند.
اطلاعات تکمیلی فرم کارجو می‌آید.
نگاشت پرسنل، واحد و شهر فقط در صورت تطبیق یکتا پر می‌شود.
UTF-8 with BOM.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_HrmResume_FromTotalSystem.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Hrm.Resume', N'U') IS NULL OR OBJECT_ID(N'Hrm.University', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول رزومه وجود ندارند. ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @Seed NVARCHAR(80) = N'seed-hrm-resume';

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE t SET t.Title = LEFT(s.University_Title, 100), t.IsActive = 1, t.ModifiedById = 1, t.ModifiedByName = @Seed, t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hrm.University t
    INNER JOIN [TMS].[TotalSystem].[dbo].[Training_University] s ON s.University_ID = t.HtsId;

    INSERT INTO Hrm.University (HtsId, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.University_ID, LEFT(s.University_Title, 100), 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Training_University] s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.University t WHERE t.HtsId = s.University_ID);

    UPDATE t SET t.Title = LEFT(NULLIF(LTRIM(RTRIM(s.Language_Title)), N''), 128), t.IsActive = 1, t.ModifiedById = 1, t.ModifiedByName = @Seed, t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hrm.TrainingLanguage t
    INNER JOIN [TMS].[TotalSystem].[dbo].[Training_Language] s ON s.Language_ID = t.HtsId;

    INSERT INTO Hrm.TrainingLanguage (HtsId, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Language_ID, LEFT(NULLIF(LTRIM(RTRIM(s.Language_Title)), N''), 128), 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Training_Language] s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.TrainingLanguage t WHERE t.HtsId = s.Language_ID);

    UPDATE t SET t.Title = LEFT(s.Software_Title, 256), t.IsActive = 1, t.ModifiedById = 1, t.ModifiedByName = @Seed, t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hrm.TrainingSoftware t
    INNER JOIN [TMS].[TotalSystem].[dbo].[Training_Software] s ON s.Software_ID = t.HtsId;

    INSERT INTO Hrm.TrainingSoftware (HtsId, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Software_ID, LEFT(s.Software_Title, 256), 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Training_Software] s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.TrainingSoftware t WHERE t.HtsId = s.Software_ID);

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT CAST(hu.User_ID AS BIGINT) AS HtsUserId, MIN(su.Id) AS UserId
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] hu
    INNER JOIN [system].[User] su
        ON LOWER(su.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(hu.ActiveDirectoryUsername)), N''), hu.Username))
        OR LOWER(su.Username) = LOWER(hu.Username)
    GROUP BY CAST(hu.User_ID AS BIGINT);

    IF OBJECT_ID('tempdb..#PersonMap') IS NOT NULL DROP TABLE #PersonMap;
    SELECT CAST(hrm.Personel_ID AS BIGINT) AS HtsPersonelId, MIN(p.Id) AS PersonelId
    INTO #PersonMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hrm
    INNER JOIN Hcm.Personel p ON ISNULL(hrm.Hamkaran_Personel_FK, 0) <> 0 AND TRY_CONVERT(INT, p.Code) = hrm.Hamkaran_Personel_FK
    GROUP BY CAST(hrm.Personel_ID AS BIGINT);

    IF OBJECT_ID('tempdb..#OrgMap') IS NOT NULL DROP TABLE #OrgMap;
    SELECT CAST(hou.OrgUnit_ID AS BIGINT) AS HtsOrgId, MIN(o.Id) AS OrgId
    INTO #OrgMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hou
    INNER JOIN Hrm.OrgUnit o ON LTRIM(RTRIM(o.Title)) = LTRIM(RTRIM(hou.OrgUnit_Title))
    GROUP BY CAST(hou.OrgUnit_ID AS BIGINT)
    HAVING COUNT(DISTINCT o.Id) = 1;

    IF OBJECT_ID('tempdb..#CityMap') IS NOT NULL DROP TABLE #CityMap;
    SELECT CAST(c.City_ID AS BIGINT) AS HtsCityId, MIN(r.Id) AS RegionId
    INTO #CityMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_City] c
    INNER JOIN Gnr.Region r ON r.Type = 3 AND LTRIM(RTRIM(r.Name)) = LTRIM(RTRIM(c.City_Title))
    GROUP BY CAST(c.City_ID AS BIGINT)
    HAVING COUNT(DISTINCT r.Id) = 1;

    IF OBJECT_ID('tempdb..#ProvMap') IS NOT NULL DROP TABLE #ProvMap;
    SELECT CAST(p.Province_ID AS BIGINT) AS HtsProvinceId, MIN(r.Id) AS RegionId
    INTO #ProvMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_Province] p
    INNER JOIN Gnr.Region r ON r.Type = 2 AND LTRIM(RTRIM(r.Name)) = LTRIM(RTRIM(p.Province_Title))
    GROUP BY CAST(p.Province_ID AS BIGINT)
    HAVING COUNT(DISTINCT r.Id) = 1;

    INSERT INTO Hrm.StaffRequest
        (HtsId, RequestOrgUnitId, HtsRequestOrgUnitId, RequestPersonelId, HtsRequestPersonelId, RequestUserId, HtsRequestUserId, JobId, RequestShamsiDate,
         CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.StaffRequest_ID, om.OrgId, s.RequestOrgUnit_FK, pm.PersonelId, s.RequestPersonel_FK, um.UserId, s.RequestUser_FK, j.Id, NULLIF(LTRIM(RTRIM(s.RequestDate)), N''),
           1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_StaffRequest] s
    LEFT JOIN #OrgMap om ON om.HtsOrgId = s.RequestOrgUnit_FK
    LEFT JOIN #PersonMap pm ON pm.HtsPersonelId = s.RequestPersonel_FK
    LEFT JOIN #UserMap um ON um.HtsUserId = s.RequestUser_FK
    LEFT JOIN Hcm.Job j ON j.HtsId = s.Job_FK
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.StaffRequest t WHERE t.HtsId = s.StaffRequest_ID);

    INSERT INTO Hrm.Resume
        (HtsId, StaffRequestId, NationalCode, Name, Family, Sex, Education, StudyCourseId, TrendOfStudyCourseId, StudyCourseInText, TrendOfStudyCourseInText,
         WorkExperience, InterestedPostId, StudentTerm, BirthDate, CityId, HtsCityId, IncomingSourceId, RepresentingPersonelId, HtsRepresentingPersonelId, UniversityId, Organization,
         FatherName, BirthPlace, IssuePlace, Religion, Trend, MaritalStatusId, NumberOfChildren, MilitaryServiceStatusId, MilitaryExemptionTypeId, MilitaryExemptionReason,
         PhoneNumber, MobileNumber, EmailAddress, HomeProvinceId, HtsHomeProvinceId, HomeCityId, HtsHomeCityId, HomeAddress, ResumeStatus, HtsCreatorUserId, ResumeShamsiDate, ResumeTime,
         YearsOfExperience, MonthsOfExperience, DaysOfExperience, YearsOfInsurance, MonthsOfInsurance, DaysOfInsurance, InterestedJobId, InterestedJobInText, ResumeFileAddress,
         HasTelephoneInterview, HasBehaviourInterview, HasSpecializedInterview, LastCheckStatusByManager, HtsUpdatedUserId, ResumeReviewDeadline, InviteForSpecializedInterview,
         RecommendedDateInText, RecommendedTimeInText, PriorityToInvite, Status, UnitIntroducedId, HtsUnitIntroducedId, ReasonForIntroduction, SuitableForJobId, ReasonForSuitableJob,
         ManagerDescription, CancelByJobApplicant, JobApplicantCancelReason, IsInvitedToInterview, SessionDate, SessionTime, SessionPlace, EmailRecipientIds, EmailRecipientEmails,
         ExpertDescription, SpecializedInterviewerId, HtsSpecializedInterviewerId, SpecializedInterviewStatus, SpecializedInterviewStatusPositivePoints, SpecializedInterviewStatusNegativePoints,
         SpecializedInterviewPriority, SpecializedInterviewUnitIntroducedId, HtsSpecializedInterviewUnitIntroducedId, SpecializedInterviewReasonForIntroduction,
         SpecializedInterviewUnitSuitableId, HtsSpecializedInterviewUnitSuitableId, SpecializedInterviewReasonForSuitableJob, SpecializedInterviewDescription,
         HrInterviewerId, HtsHrInterviewerId, HrInterviewerStatus, HrInterviewerStatusPositivePoints, HrInterviewerStatusNegativePoints, ContractAmount, HrInterviewerDescription,
         BoardOfDirectorIds, BoardOfDirectorNames, BeneficiaryIds, BeneficiaryNames, InExpertCartable, InManagerCartable, InHrmManagerCartable, InBoardOfDirectorsCartable, InOfficeExpertCartable,
         LoginId, PreviousCompanyName, PreviousManagerPhoneNumber, PreviousCompanyPhoneNumber, PreviousCompanyMobileNumber, PreviousCompanyDescription, ReferenceCheckIsApproved,
         CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        r.Resume_ID, sr.Id, NULLIF(LTRIM(RTRIM(r.NationalCode)), N''), NULLIF(LTRIM(RTRIM(r.Name)), N''), NULLIF(LTRIM(RTRIM(r.Family)), N''),
        CASE WHEN r.Sex_FK = 2 THEN 2 ELSE 3 END,
        CASE WHEN r.Education_FK BETWEEN 1 AND 14 THEN r.Education_FK ELSE NULL END,
        sc.Id, tr.Id, NULLIF(LTRIM(RTRIM(r.StudyCourseInText)), N''), NULLIF(LTRIM(RTRIM(r.TrendOfStudyCourseInText)), N''),
        r.WorkExperience, op.Id, NULLIF(r.StudentTerm, 0), NULLIF(LTRIM(RTRIM(r.BirthDate)), N''), cm.RegionId, r.City_FK, inc.Id, rpm.PersonelId, r.RepresentingPersonel_FK, un.Id,
        CASE WHEN r.OrganizationId IN (1185, 1186, 1187, 1188) THEN r.OrganizationId ELSE NULL END,
        NULLIF(LTRIM(RTRIM(r.FatherName)), N''), NULLIF(LTRIM(RTRIM(r.BirthPlace)), N''), NULLIF(LTRIM(RTRIM(r.IssuePlace)), N''), NULLIF(LTRIM(RTRIM(r.Religion)), N''), NULLIF(LTRIM(RTRIM(r.Trend)), N''),
        NULLIF(r.MaritalStatusId, 0), r.NumberOfChildren, NULLIF(r.MilitaryServiceStatusId, 0), NULLIF(r.MilitaryExemptionTypeId, 0), NULLIF(LTRIM(RTRIM(r.MilitaryExemptionReason)), N''),
        NULLIF(LTRIM(RTRIM(r.PhoneNumber)), N''), NULLIF(LTRIM(RTRIM(r.MobileNumber)), N''), NULLIF(LTRIM(RTRIM(r.Resume_EmailAddress)), N''),
        pmv.RegionId, r.HomeProvinceId, hcm.RegionId, r.HomeCityId, NULLIF(LTRIM(RTRIM(r.HomeAddress)), N''),
        CASE WHEN r.Resume_Status_FK IN (52, 53, 54, 73, 1715, 1716, 1717, 1718) THEN r.Resume_Status_FK ELSE NULL END,
        r.Resume_Creator, NULLIF(LTRIM(RTRIM(r.Resume_Date)), N''), NULLIF(LTRIM(RTRIM(r.Resume_Time)), N''),
        r.YearsOfExperience, r.MonthsOfExperience, r.DaysOfExperience, r.YearsOfInsurance, r.MonthsOfInsurance, r.DaysOfInsurance,
        ij.Id, NULLIF(LTRIM(RTRIM(r.InterestedJobInText)), N''), NULLIF(LTRIM(RTRIM(r.ResumeFileAddress)), N''),
        r.HasTelephoneInterview, r.HasBehaviourInterview, r.HasSpecializedInterview,
        CASE WHEN r.LastCheckStatusByManager IN (1694, 1695) THEN r.LastCheckStatusByManager ELSE NULL END,
        r.UpdatedUserId, NULLIF(LTRIM(RTRIM(r.ResumeReviewDeadline)), N''), r.InviteForSpecializedInterview,
        NULLIF(LTRIM(RTRIM(r.RecommendedDateInText)), N''), NULLIF(LTRIM(RTRIM(r.RecommendedTimeInText)), N''), NULLIF(LTRIM(RTRIM(r.PriorityToInvite)), N''),
        CASE WHEN r.StatusId IN (2240, 2241, 2242, 2243) THEN r.StatusId ELSE NULL END,
        uom.OrgId, r.UnitIntroducedId, NULLIF(LTRIM(RTRIM(r.ReasonForIntroduction)), N''), sj.Id, NULLIF(LTRIM(RTRIM(r.ReasonForSuitableJob)), N''),
        NULLIF(LTRIM(RTRIM(r.ManagerDescription)), N''), r.CancelByJobApplicant, NULLIF(LTRIM(RTRIM(r.JobApplicantCancelReason)), N''), r.IsInvitedToInterview,
        NULLIF(LTRIM(RTRIM(r.SessionDate)), N''), NULLIF(LTRIM(RTRIM(r.SessionTime)), N''), NULLIF(LTRIM(RTRIM(r.SessionPlace)), N''),
        NULLIF(LTRIM(RTRIM(r.EmailRecipientIds)), N''), NULLIF(LTRIM(RTRIM(r.EmailRecipientEmails)), N''), NULLIF(LTRIM(RTRIM(r.ExpertDescription)), N''),
        sim.PersonelId, r.SpecializedInterviewerId,
        CASE WHEN r.SpecializedInterviewStatusId IN (2244, 2245, 2246, 2247, 2248) THEN r.SpecializedInterviewStatusId ELSE NULL END,
        NULLIF(LTRIM(RTRIM(r.SpecializedInterviewStatusPositivePoints)), N''), NULLIF(LTRIM(RTRIM(r.SpecializedInterviewStatusNegativePoints)), N''),
        NULLIF(LTRIM(RTRIM(r.SpecializedInterviewPriority)), N''), siu.OrgId, r.SpecializedInterviewUnitIntroducedId, NULLIF(LTRIM(RTRIM(r.SpecializedInterviewReasonForIntroduction)), N''),
        sus.OrgId, r.SpecializedInterviewUnitSuitableId, NULLIF(LTRIM(RTRIM(r.SpecializedInterviewReasonForSuitableJob)), N''), NULLIF(LTRIM(RTRIM(r.SpecializedInterviewDescription)), N''),
        him.PersonelId, r.HRInterviewerId,
        CASE WHEN r.HRInterviewerStatusId IN (2249, 2250, 2251, 2252) THEN r.HRInterviewerStatusId ELSE NULL END,
        NULLIF(LTRIM(RTRIM(r.HRInterviewerStatusPositivePoints)), N''), NULLIF(LTRIM(RTRIM(r.HRInterviewerStatusNegetivePoints)), N''),
        r.ContractAmount, NULLIF(LTRIM(RTRIM(r.HRInterviewerDescription)), N''),
        NULLIF(LTRIM(RTRIM(r.BoardOfDirectorIds)), N''), NULLIF(LTRIM(RTRIM(r.BoardOfDirectorNames)), N''), NULLIF(LTRIM(RTRIM(r.BeneficiaryIds)), N''), NULLIF(LTRIM(RTRIM(r.BeneficiaryNames)), N''),
        r.InExpertCartable, r.InManagerCartable, r.InHRMManagerCartable, r.InBoardOfDirectorsCartable, r.InOfficeExpertCartable,
        r.LoginId, NULLIF(LTRIM(RTRIM(r.PreviousCompanyName)), N''), NULLIF(LTRIM(RTRIM(r.PreviousManagerPhoneNumber)), N''), NULLIF(LTRIM(RTRIM(r.PreviousCompanyPhoneNumber)), N''),
        NULLIF(LTRIM(RTRIM(r.PreviousCompanyMobileNumber)), N''), NULLIF(LTRIM(RTRIM(r.PreviousCompanyDescription)), N''), r.ReferenceCheckIsApproved,
        COALESCE(cum.UserId, 1), COALESCE(cu.FullName, @Seed), COALESCE(uum.UserId, 1), COALESCE(uu.FullName, @Seed),
        COALESCE(r.UpdatedDateTime, @Now), r.UpdatedDateTime, COALESCE(NULLIF(LTRIM(RTRIM(r.Resume_Date)), N''), @NowShamsi), NULLIF(LTRIM(RTRIM(r.UpdatedDateTimeInText)), N''), 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_Resume] r
    LEFT JOIN Hrm.StaffRequest sr ON sr.HtsId = r.StaffRequestId
    LEFT JOIN Hrm.StudyCourse sc ON sc.HtsId = r.StudyCourse_FK
    LEFT JOIN Hrm.TrendOfStudyCourse tr ON tr.HtsId = r.TrendOfStudyCourse_FK
    LEFT JOIN Hrm.OrgPost op ON op.HtsId = r.InterestedPost_FK
    LEFT JOIN #CityMap cm ON cm.HtsCityId = r.City_FK
    LEFT JOIN Hrm.IncomingSource inc ON inc.HtsId = r.Incoming_Source_FK
    LEFT JOIN #PersonMap rpm ON rpm.HtsPersonelId = r.RepresentingPersonel_FK
    LEFT JOIN Hrm.University un ON un.HtsId = r.University_FK
    LEFT JOIN #ProvMap pmv ON pmv.HtsProvinceId = r.HomeProvinceId
    LEFT JOIN #CityMap hcm ON hcm.HtsCityId = r.HomeCityId
    LEFT JOIN Hcm.Job ij ON ij.HtsId = r.InterestedJobId
    LEFT JOIN #OrgMap uom ON uom.HtsOrgId = r.UnitIntroducedId
    LEFT JOIN Hcm.Job sj ON sj.HtsId = r.SuitableForId
    LEFT JOIN #PersonMap sim ON sim.HtsPersonelId = r.SpecializedInterviewerId
    LEFT JOIN #OrgMap siu ON siu.HtsOrgId = r.SpecializedInterviewUnitIntroducedId
    LEFT JOIN #OrgMap sus ON sus.HtsOrgId = r.SpecializedInterviewUnitSuitableId
    LEFT JOIN #PersonMap him ON him.HtsPersonelId = r.HRInterviewerId
    LEFT JOIN #UserMap cum ON cum.HtsUserId = r.Resume_Creator
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] cu ON cu.User_ID = r.Resume_Creator
    LEFT JOIN #UserMap uum ON uum.HtsUserId = r.UpdatedUserId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] uu ON uu.User_ID = r.UpdatedUserId
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.Resume t WHERE t.HtsId = r.Resume_ID);

    INSERT INTO Hrm.ResumeLanguage (HtsId, ResumeId, LanguageId, KnowledgeLevel, NormalCorrespondenceLevel, SpecializedCorrespondenceLevel, UnderstandTextLevel, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Resume_Language_ID, rv.Id, lg.Id,
        CASE WHEN s.RatingOfKnowledge_FK IN (46, 47, 48, 49) THEN s.RatingOfKnowledge_FK END,
        CASE WHEN s.RatingOfNormalCorrespondence_FK IN (46, 47, 48, 49) THEN s.RatingOfNormalCorrespondence_FK END,
        CASE WHEN s.RatingOfSpecializedCorrespondence_FK IN (46, 47, 48, 49) THEN s.RatingOfSpecializedCorrespondence_FK END,
        CASE WHEN s.RatingOfUnderstandText_FK IN (46, 47, 48, 49) THEN s.RatingOfUnderstandText_FK END,
        1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_Resume_Language] s
    INNER JOIN Hrm.Resume rv ON rv.HtsId = s.Resume_FK
    LEFT JOIN Hrm.TrainingLanguage lg ON lg.HtsId = s.Language_FK
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.ResumeLanguage t WHERE t.HtsId = s.Resume_Language_ID);

    INSERT INTO Hrm.ResumeSoftware (HtsId, ResumeId, SoftwareId, OtherSoftware, KnowledgeLevel, LearnType, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Resume_Software_ID, rv.Id, sw.Id, NULLIF(LTRIM(RTRIM(s.OtherSoftware)), N''),
        CASE WHEN s.RatingOfKnowledge_FK IN (46, 47, 48, 49) THEN s.RatingOfKnowledge_FK END,
        CASE WHEN s.LearnType_FK IN (1397, 1398) THEN s.LearnType_FK END,
        1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_Resume_Software] s
    INNER JOIN Hrm.Resume rv ON rv.HtsId = s.Resume_FK
    LEFT JOIN Hrm.TrainingSoftware sw ON sw.HtsId = s.Software_FK
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.ResumeSoftware t WHERE t.HtsId = s.Resume_Software_ID);

    INSERT INTO Hrm.ResumeSkill (HtsId, ResumeId, SkillId, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Resume_Skill_ID, rv.Id, sk.Id, 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_Resume_Skill] s
    INNER JOIN Hrm.Resume rv ON rv.HtsId = s.Resume_FK
    LEFT JOIN Hrm.Skill sk ON sk.HtsId = s.Skill_FK
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.ResumeSkill t WHERE t.HtsId = s.Resume_Skill_ID);

    INSERT INTO Hrm.ResumeWorkExperience
        (HtsId, ResumeId, CompanyName, OrgPost, StartDate, EndDate, OldManagerFullName, OldManagerPhoneNumber, EmploymentType, CollaboratorsNumber, SupervisedPersonnelNumber, SalaryAmount, LeavingReason, CompanyPhone, CompanyMobile, IsStillWorking, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Resume_WorkExperience_ID, rv.Id, NULLIF(LTRIM(RTRIM(s.CompanyName)), N''), NULLIF(LTRIM(RTRIM(s.OrgPost)), N''), NULLIF(LTRIM(RTRIM(s.StartDate)), N''), NULLIF(LTRIM(RTRIM(s.EndDate)), N''),
        NULLIF(LTRIM(RTRIM(s.OldManagerFullName)), N''), NULLIF(LTRIM(RTRIM(s.OldManagerPhoneNumber)), N''),
        CASE WHEN s.EmploymentType IN (40, 41, 42) THEN s.EmploymentType END,
        s.CollaboratorsNumber, s.SupervisedPersonnelNumber, s.SaleryAmount, NULLIF(LTRIM(RTRIM(s.LeavingReason)), N''),
        NULLIF(LTRIM(RTRIM(s.CompanPhone)), N''), NULLIF(LTRIM(RTRIM(s.CompanyMobile)), N''), s.IsStillWorking,
        1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_Resume_WorkExperience] s
    INNER JOIN Hrm.Resume rv ON rv.HtsId = s.Resume_FK
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.ResumeWorkExperience t WHERE t.HtsId = s.Resume_WorkExperience_ID);

    INSERT INTO Hrm.ResumeEducationalStatus
        (HtsId, ResumeId, Degree, HtsDegreeId, StudyCourseId, TrendOfStudyCourseId, StudyFieldInText, StudyMajorInText, EducationalInstitutionTitle, EducationPlace, GraduationDate, GradeAverage, IsStudent, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Id, rv.Id, CASE WHEN s.DegreeId BETWEEN 1 AND 14 THEN s.DegreeId END, NULLIF(s.DegreeId, 0), sc.Id, tr.Id,
        NULLIF(LTRIM(RTRIM(s.StudyFieldInText)), N''), NULLIF(LTRIM(RTRIM(s.StudyMajorInText)), N''), NULLIF(LTRIM(RTRIM(s.EducationalInstitutionTitle)), N''),
        NULLIF(LTRIM(RTRIM(s.EducationPlace)), N''), NULLIF(LTRIM(RTRIM(s.GraduationDate)), N''), NULLIF(LTRIM(RTRIM(s.GradeAverage)), N''), s.IsStudent,
        1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_ResumeEducationalStatus] s
    INNER JOIN Hrm.Resume rv ON rv.HtsId = s.ResumeId
    LEFT JOIN Hrm.StudyCourse sc ON sc.HtsId = s.StudyFieldId
    LEFT JOIN Hrm.TrendOfStudyCourse tr ON tr.HtsId = s.StudyMajorId
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.ResumeEducationalStatus t WHERE t.HtsId = s.Id);

    INSERT INTO Hrm.ResumeComment
        (HtsId, ResumeId, Title, Comment, HtsCreatedUserId, CreatedDateTimeInText, HtsStatusId, StatusTitle, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Id, rv.Id, NULLIF(LTRIM(RTRIM(s.Title)), N''), LEFT(s.Comment, 256), s.CreatedUserId, NULLIF(LTRIM(RTRIM(s.CreatedDateTimeInText)), N''), s.StatusId, NULLIF(LTRIM(RTRIM(lk.Lookup_Title_Fa)), N''),
        COALESCE(um.UserId, 1), COALESCE(u.FullName, @Seed), 1, @Seed, s.CreatedDateTime, @Now, COALESCE(NULLIF(LTRIM(RTRIM(s.CreatedDateTimeInText)), N''), @NowShamsi), @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_ResumeComment] s
    INNER JOIN Hrm.Resume rv ON rv.HtsId = s.ResumeId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Lookup] lk ON lk.Lookup_ID = s.StatusId
    LEFT JOIN #UserMap um ON um.HtsUserId = s.CreatedUserId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = s.CreatedUserId
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.ResumeComment t WHERE t.HtsId = s.Id);

    INSERT INTO Hrm.ResumeFurtherInformation
        (HtsId, ResumeId, QuestionTitle, Answer, Explain, IdentifierName, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Id, rv.Id, LEFT(s.QuestionTitle, 256), NULLIF(LTRIM(RTRIM(s.Answer)), N''), NULLIF(LTRIM(RTRIM(s.Explain)), N''), NULLIF(LTRIM(RTRIM(s.IdentifierName)), N''),
        1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_ResumeFurtherInformation] s
    INNER JOIN Hrm.Resume rv ON rv.HtsId = s.ResumeId
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.ResumeFurtherInformation t WHERE t.HtsId = s.Id);

    INSERT INTO Hrm.ResumeFinalInformation
        (HtsId, ResumeId, FullName, MobileNumber, DepartmentId, HtsDepartmentId, JobId, ManagerId, HtsManagerId, SupervisorId, HtsSupervisorId, StartDateOfAgreement, Workplace, ContractAmount, BoardOfDirectorIds, BoardOfDirectorString, PromissoryNoteAmount, ComputerType, RequiredSoftwares, OrganizationEmailAddress, HtsUsername, SystemPassword, InternalPhoneNumber, HtsCreatedUserId, CreatedDateInText, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Id, rv.Id, NULLIF(LTRIM(RTRIM(s.Fullname)), N''), NULLIF(LTRIM(RTRIM(s.MobileNumber)), N''), om.OrgId, s.DepartmentId, j.Id, mm.PersonelId, s.ManagerId, sm.PersonelId, s.SupervisorId,
        NULLIF(LTRIM(RTRIM(s.StartDateOfAgreement)), N''), NULLIF(LTRIM(RTRIM(s.Workplace)), N''), s.ContractAmount, NULLIF(LTRIM(RTRIM(s.BoardOfDirectorIds)), N''), NULLIF(LTRIM(RTRIM(s.BoardOfDirectorString)), N''),
        s.PromissoryNoteAmount, NULLIF(LTRIM(RTRIM(s.ComputerType)), N''), NULLIF(LTRIM(RTRIM(s.RequiredSoftwares)), N''), NULLIF(LTRIM(RTRIM(s.OrganizationEmailAddress)), N''),
        NULLIF(LTRIM(RTRIM(s.HtsUsername)), N''), NULLIF(LTRIM(RTRIM(s.SystemPassword)), N''), NULLIF(LTRIM(RTRIM(s.InternalPhoneNumber)), N''), s.CreatedUserId, NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N''),
        COALESCE(um.UserId, 1), COALESCE(u.FullName, @Seed), 1, @Seed, s.CreatedDateTime, @Now, COALESCE(NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N''), @NowShamsi), @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_ResumeFinalInformation] s
    INNER JOIN Hrm.Resume rv ON rv.HtsId = s.ResumeId
    LEFT JOIN #OrgMap om ON om.HtsOrgId = s.DepartmentId
    LEFT JOIN Hcm.Job j ON j.HtsId = s.JobPositionId
    LEFT JOIN #PersonMap mm ON mm.HtsPersonelId = s.ManagerId
    LEFT JOIN #PersonMap sm ON sm.HtsPersonelId = s.SupervisorId
    LEFT JOIN #UserMap um ON um.HtsUserId = s.CreatedUserId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = s.CreatedUserId
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.ResumeFinalInformation t WHERE t.HtsId = s.Id);

    INSERT INTO Hrm.ResumeAttachment
        (HtsId, ResumeId, FileName, AttachmentFilePath, FileSize, Comment, CreatedShamsiDate, CreatedTime, HtsCreatedUserId, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.Resume_Attachment_ID, rv.Id, NULLIF(LTRIM(RTRIM(s.Attachment_FileName)), N''), NULLIF(LTRIM(RTRIM(s.AttachmentFilePath)), N''), s.Attachment_FileSize,
        NULLIF(LTRIM(RTRIM(s.Attachment_Comment)), N''), NULLIF(LTRIM(RTRIM(s.CreatedDate)), N''), NULLIF(LTRIM(RTRIM(s.CreatedTime)), N''), s.CreatedUser_FK,
        COALESCE(um.UserId, 1), COALESCE(u.FullName, @Seed), 1, @Seed, @Now, @Now, COALESCE(NULLIF(LTRIM(RTRIM(s.CreatedDate)), N''), @NowShamsi), @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_Resume_Attachment] s
    INNER JOIN Hrm.Resume rv ON rv.HtsId = s.Resume_FK
    LEFT JOIN #UserMap um ON um.HtsUserId = s.CreatedUser_FK
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = s.CreatedUser_FK
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.ResumeAttachment t WHERE t.HtsId = s.Resume_Attachment_ID);

    IF OBJECT_ID('tempdb..#FileMap') IS NOT NULL DROP TABLE #FileMap;
    CREATE TABLE #FileMap (HtsResumeId BIGINT NOT NULL, FileId BIGINT NOT NULL);

    MERGE dbo.FileEntity AS t
    USING (
        SELECT rv.HtsId, rv.ResumeFileAddress,
            CASE WHEN CHARINDEX(N'\', REVERSE(REPLACE(rv.ResumeFileAddress, N'/', N'\'))) > 0
                THEN RIGHT(REPLACE(rv.ResumeFileAddress, N'/', N'\'), CHARINDEX(N'\', REVERSE(REPLACE(rv.ResumeFileAddress, N'/', N'\'))) - 1)
                ELSE REPLACE(rv.ResumeFileAddress, N'/', N'\') END AS OriginalName
        FROM Hrm.Resume rv
        WHERE rv.ResumeFileId IS NULL AND NULLIF(LTRIM(RTRIM(rv.ResumeFileAddress)), N'') IS NOT NULL
    ) AS s
    ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (PhysicalPath, OriginalName, ContentType, Size, EntityType, EntityPropName, EntityId, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (s.ResumeFileAddress, LEFT(NULLIF(s.OriginalName, N''), 400), N'application/pdf', 0, N'Entities.App.Hrm.Resume', N'ResumeFile', NULL, 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1)
    OUTPUT s.HtsId, inserted.Id INTO #FileMap (HtsResumeId, FileId);

    UPDATE rv SET rv.ResumeFileId = m.FileId
    FROM Hrm.Resume rv
    INNER JOIN #FileMap m ON m.HtsResumeId = rv.HtsId
    WHERE rv.ResumeFileId IS NULL;

    UPDATE f SET f.EntityId = rv.Id
    FROM dbo.FileEntity f
    INNER JOIN Hrm.Resume rv ON rv.ResumeFileId = f.Id
    WHERE f.EntityId IS NULL;

    IF OBJECT_ID('tempdb..#AttFileMap') IS NOT NULL DROP TABLE #AttFileMap;
    CREATE TABLE #AttFileMap (HtsAttachmentId BIGINT NOT NULL, FileId BIGINT NOT NULL);

    MERGE dbo.FileEntity AS t
    USING (
        SELECT a.HtsId, a.AttachmentFilePath, COALESCE(NULLIF(a.FileName, N''), N'attachment') AS OriginalName, ISNULL(a.FileSize, 0) AS FileSize, a.Id AS AttachmentRowId
        FROM Hrm.ResumeAttachment a
        WHERE a.FileId IS NULL AND NULLIF(LTRIM(RTRIM(a.AttachmentFilePath)), N'') IS NOT NULL
    ) AS s
    ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (PhysicalPath, OriginalName, ContentType, Size, EntityType, EntityPropName, EntityId, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (s.AttachmentFilePath, LEFT(s.OriginalName, 400), N'application/octet-stream', s.FileSize, N'Entities.App.Hrm.ResumeAttachment', N'File', s.AttachmentRowId, 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1)
    OUTPUT s.HtsId, inserted.Id INTO #AttFileMap (HtsAttachmentId, FileId);

    UPDATE a SET a.FileId = m.FileId
    FROM Hrm.ResumeAttachment a
    INNER JOIN #AttFileMap m ON m.HtsAttachmentId = a.HtsId
    WHERE a.FileId IS NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
    RETURN;
END CATCH;

SELECT N'University' AS Item, COUNT(*) AS Cnt FROM Hrm.University
UNION ALL SELECT N'TrainingLanguage', COUNT(*) FROM Hrm.TrainingLanguage
UNION ALL SELECT N'TrainingSoftware', COUNT(*) FROM Hrm.TrainingSoftware
UNION ALL SELECT N'StaffRequest', COUNT(*) FROM Hrm.StaffRequest
UNION ALL SELECT N'Resume', COUNT(*) FROM Hrm.Resume
UNION ALL SELECT N'ResumeLanguage', COUNT(*) FROM Hrm.ResumeLanguage
UNION ALL SELECT N'ResumeSoftware', COUNT(*) FROM Hrm.ResumeSoftware
UNION ALL SELECT N'ResumeSkill', COUNT(*) FROM Hrm.ResumeSkill
UNION ALL SELECT N'ResumeWorkExperience', COUNT(*) FROM Hrm.ResumeWorkExperience
UNION ALL SELECT N'ResumeEducationalStatus', COUNT(*) FROM Hrm.ResumeEducationalStatus
UNION ALL SELECT N'ResumeComment', COUNT(*) FROM Hrm.ResumeComment
UNION ALL SELECT N'ResumeFurtherInformation', COUNT(*) FROM Hrm.ResumeFurtherInformation
UNION ALL SELECT N'ResumeFinalInformation', COUNT(*) FROM Hrm.ResumeFinalInformation
UNION ALL SELECT N'ResumeAttachment', COUNT(*) FROM Hrm.ResumeAttachment;

SELECT N'CityUnmatched' AS Gap, COUNT(*) AS Cnt FROM Hrm.Resume WHERE HtsCityId IS NOT NULL AND CityId IS NULL
UNION ALL SELECT N'ProvinceUnmatched', COUNT(*) FROM Hrm.Resume WHERE HtsHomeProvinceId IS NOT NULL AND HomeProvinceId IS NULL
UNION ALL SELECT N'HomeCityUnmatched', COUNT(*) FROM Hrm.Resume WHERE HtsHomeCityId IS NOT NULL AND HomeCityId IS NULL
UNION ALL SELECT N'PersonelUnmatched', COUNT(*) FROM Hrm.Resume WHERE HtsRepresentingPersonelId IS NOT NULL AND RepresentingPersonelId IS NULL
UNION ALL SELECT N'OrgUnitUnmatched', COUNT(*) FROM Hrm.Resume WHERE HtsUnitIntroducedId IS NOT NULL AND UnitIntroducedId IS NULL
UNION ALL SELECT N'StaffOrgUnmatched', COUNT(*) FROM Hrm.StaffRequest WHERE HtsRequestOrgUnitId IS NOT NULL AND RequestOrgUnitId IS NULL
UNION ALL SELECT N'StaffUserUnmatched', COUNT(*) FROM Hrm.StaffRequest WHERE HtsRequestUserId IS NOT NULL AND RequestUserId IS NULL
UNION ALL SELECT N'ResumeFileMissingPath', COUNT(*) FROM Hrm.Resume WHERE ResumeFileId IS NULL AND NULLIF(LTRIM(RTRIM(ResumeFileAddress)), N'') IS NULL
UNION ALL SELECT N'AttachmentWithoutPath', COUNT(*) FROM Hrm.ResumeAttachment WHERE FileId IS NULL;
