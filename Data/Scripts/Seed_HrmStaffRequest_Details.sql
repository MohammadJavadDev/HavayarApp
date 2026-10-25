/*
به‌روزرسانی سربرگ درخواست نیرو و فرزندانش از TotalSystem.
UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;
IF COL_LENGTH(N'Hrm.StaffRequest', N'Status') IS NULL
BEGIN
    RAISERROR(N'ستون‌های جدید درخواست نیرو نیست. ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @Seed NVARCHAR(80) = N'seed-hrm-staff-request';

IF OBJECT_ID('tempdb..#PersonMap') IS NOT NULL DROP TABLE #PersonMap;
SELECT CAST(hrm.Personel_ID AS BIGINT) AS HtsPersonelId, MIN(p.Id) AS PersonelId
INTO #PersonMap
FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hrm
INNER JOIN Hcm.Personel p ON ISNULL(hrm.Hamkaran_Personel_FK, 0) <> 0 AND TRY_CONVERT(INT, p.Code) = hrm.Hamkaran_Personel_FK
GROUP BY CAST(hrm.Personel_ID AS BIGINT);

IF OBJECT_ID('tempdb..#CityMap') IS NOT NULL DROP TABLE #CityMap;
SELECT CAST(c.City_ID AS BIGINT) AS HtsCityId, MIN(r.Id) AS RegionId
INTO #CityMap
FROM [TMS].[TotalSystem].[dbo].[Gnr_City] c
INNER JOIN Gnr.Region r ON r.Type = 3 AND LTRIM(RTRIM(r.Name)) = LTRIM(RTRIM(c.City_Title))
GROUP BY CAST(c.City_ID AS BIGINT)
HAVING COUNT(DISTINCT r.Id) = 1;

UPDATE t SET
    t.RequestTime = NULLIF(LTRIM(RTRIM(s.RequestTime)), N''),
    t.ApproverPersonelId = ap.PersonelId,
    t.HtsApproverPersonelId = s.ApproverId,
    t.RequiredNumber = s.RequiredNumber,
    t.CooperationType = CASE WHEN s.CooperationType_FK IN (40, 41, 42) THEN s.CooperationType_FK END,
    t.Sex = CASE WHEN s.Sex_FK IN (1, 2, 3) THEN s.Sex_FK END,
    t.MaritalStatus = CASE WHEN s.MaritalStatus_FK IN (1, 2, 3) THEN s.MaritalStatus_FK END,
    t.FromAge = s.FromAge,
    t.ToAge = s.ToAge,
    t.EmploymentKind = CASE WHEN s.Employment_Status_FK IN (50, 51) THEN s.Employment_Status_FK END,
    t.ReplacePersonelId = rp.PersonelId,
    t.HtsReplacePersonelId = s.ReplacePersonel_FK,
    t.ReplacePersonelTitle = NULLIF(LTRIM(RTRIM(s.ReplacePersonel)), N''),
    t.ExperienceRequired = s.ExperienceRequired,
    t.EducationDiploma = s.Education_Diploma,
    t.EducationAssociate = s.Education_Associate,
    t.EducationBachelor = s.Education_Bachelor,
    t.EducationMaster = s.Education_Master,
    t.StudyCourseOneId = c1.Id,
    t.StudyCourseTwoId = c2.Id,
    t.StudyCourseThreeId = c3.Id,
    t.CityId = cm.RegionId,
    t.HtsCityId = s.CityService_FK,
    t.Workplace = CASE WHEN s.WorkPlace_FK IN (43, 44) THEN s.WorkPlace_FK END,
    t.TasksExpected = NULLIF(LTRIM(RTRIM(s.TasksExpected)), N''),
    t.PersonalityFeature = NULLIF(LTRIM(RTRIM(s.PersonalityFeature)), N''),
    t.Comment = NULLIF(LTRIM(RTRIM(s.Comment)), N''),
    t.Status = CASE WHEN s.StaffRequest_Status_FK IN (76, 77, 78, 80, 1773, 2539) THEN s.StaffRequest_Status_FK END,
    t.HasManagerAccept = s.HasManagerAccept,
    t.HiddenFromSupervisor = s.HiddenFromSuppervisor,
    t.HtsUpdatedUserId = s.UpdatedUserId,
    t.ModifiedByName = @Seed,
    t.ModifiedDateMiladiDateTime = @Now,
    t.ModifiedDateShamsiDateTime = @NowShamsi
FROM Hrm.StaffRequest t
INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_StaffRequest] s ON s.StaffRequest_ID = t.HtsId
LEFT JOIN #PersonMap ap ON ap.HtsPersonelId = s.ApproverId
LEFT JOIN #PersonMap rp ON rp.HtsPersonelId = s.ReplacePersonel_FK
LEFT JOIN Hrm.StudyCourse c1 ON c1.HtsId = s.Related_StudyCourse_One_FK
LEFT JOIN Hrm.StudyCourse c2 ON c2.HtsId = s.Related_StudyCourse_Two_FK
LEFT JOIN Hrm.StudyCourse c3 ON c3.HtsId = s.Related_StudyCourse_Three_FK
LEFT JOIN #CityMap cm ON cm.HtsCityId = s.CityService_FK;

INSERT INTO Hrm.StaffRequestLanguage (HtsId, StaffRequestId, LanguageId, KnowledgeLevel, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT s.StaffRequest_Language_ID, sr.Id, lg.Id, CASE WHEN s.RatingOfKnowledge_FK IN (46,47,48,49) THEN s.RatingOfKnowledge_FK END,
       1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
FROM [TMS].[TotalSystem].[dbo].[HRM_StaffRequest_Language] s
INNER JOIN Hrm.StaffRequest sr ON sr.HtsId = s.StaffingRequest_FK
LEFT JOIN Hrm.TrainingLanguage lg ON lg.HtsId = s.Language_FK
WHERE NOT EXISTS (SELECT 1 FROM Hrm.StaffRequestLanguage t WHERE t.HtsId = s.StaffRequest_Language_ID);

INSERT INTO Hrm.StaffRequestSkill (HtsId, StaffRequestId, SkillId, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT s.StaffRequest_Skill_ID, sr.Id, sk.Id, 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
FROM [TMS].[TotalSystem].[dbo].[HRM_StaffRequest_Skill] s
INNER JOIN Hrm.StaffRequest sr ON sr.HtsId = s.StaffingRequest_FK
LEFT JOIN Hrm.Skill sk ON sk.HtsId = s.Skill_FK
WHERE NOT EXISTS (SELECT 1 FROM Hrm.StaffRequestSkill t WHERE t.HtsId = s.StaffRequest_Skill_ID);

INSERT INTO Hrm.StaffRequestSoftware (HtsId, StaffRequestId, SoftwareId, KnowledgeLevel, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT s.StaffRequest_Software_ID, sr.Id, sw.Id, CASE WHEN s.RatingOfKnowledge_FK IN (46,47,48,49) THEN s.RatingOfKnowledge_FK END,
       1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
FROM [TMS].[TotalSystem].[dbo].[HRM_StaffRequest_Software] s
INNER JOIN Hrm.StaffRequest sr ON sr.HtsId = s.StaffingRequest_FK
LEFT JOIN Hrm.TrainingSoftware sw ON sw.HtsId = s.Software_FK
WHERE NOT EXISTS (SELECT 1 FROM Hrm.StaffRequestSoftware t WHERE t.HtsId = s.StaffRequest_Software_ID);

INSERT INTO Hrm.StaffRequestStaffer (HtsId, StaffRequestId, StafferName, StafferDescription, HtsHiringStateId, HiringDateInText, FillingDateInText, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT s.Id, sr.Id, NULLIF(LTRIM(RTRIM(s.StafferName)), N''), NULLIF(LTRIM(RTRIM(s.StafferDescription)), N''), s.HiringStateId,
       NULLIF(LTRIM(RTRIM(s.HiringDateInText)), N''), NULLIF(LTRIM(RTRIM(s.FillingDateInText)), N''),
       1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
FROM [TMS].[TotalSystem].[dbo].[HRM_StaffRequest_Staffer] s
INNER JOIN Hrm.StaffRequest sr ON sr.HtsId = s.StaffRequest_Id
WHERE NOT EXISTS (SELECT 1 FROM Hrm.StaffRequestStaffer t WHERE t.HtsId = s.Id);

INSERT INTO Hrm.StaffRequestConfirm (HtsId, StaffRequestId, ConfirmedPersonelId, HtsConfirmedPersonelId, ConfirmStatus, Comment, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT s.StaffRequest_Confirm_ID, sr.Id, pm.PersonelId, s.ConfirmedPersonel_FK, s.ConfirmStatus, NULLIF(LTRIM(RTRIM(s.Comment)), N''),
       1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
FROM [TMS].[TotalSystem].[dbo].[HRM_StaffRequest_Confirm] s
INNER JOIN Hrm.StaffRequest sr ON sr.HtsId = s.StaffRequest_FK
LEFT JOIN #PersonMap pm ON pm.HtsPersonelId = s.ConfirmedPersonel_FK
WHERE NOT EXISTS (SELECT 1 FROM Hrm.StaffRequestConfirm t WHERE t.HtsId = s.StaffRequest_Confirm_ID);

SELECT N'StaffRequest' Item, COUNT(*) Cnt FROM Hrm.StaffRequest
UNION ALL SELECT N'WithStatus', COUNT(*) FROM Hrm.StaffRequest WHERE Status IS NOT NULL
UNION ALL SELECT N'Language', COUNT(*) FROM Hrm.StaffRequestLanguage
UNION ALL SELECT N'Skill', COUNT(*) FROM Hrm.StaffRequestSkill
UNION ALL SELECT N'Software', COUNT(*) FROM Hrm.StaffRequestSoftware
UNION ALL SELECT N'Staffer', COUNT(*) FROM Hrm.StaffRequestStaffer
UNION ALL SELECT N'Confirm', COUNT(*) FROM Hrm.StaffRequestConfirm;
