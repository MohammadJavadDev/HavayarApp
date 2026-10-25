/*
Seed_HrmRecruitmentBase_FromTotalSystem.sql
اطلاعات پایه جذب و استخدام از TotalSystem، یک‌بار و تکرارپذیر:
  HRM_Skill                  → Hrm.Skill
  HRM_StudyCourse            → Hrm.StudyCourse
  HRM_TrendOfStudyCourse     → Hrm.TrendOfStudyCourse
  Gnr_Lookup LookupType_FK=20 → Hrm.IncomingSource  (شناسه ۰ = جابینجا)
پست سازمانی سید نمی‌شود. مدل شایستگی و ویژگی شخصیتی منتقل نمی‌شوند.
UTF-8 with BOM. Linked server [TMS].
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_HrmRecruitmentBase_FromTotalSystem.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Hrm.Skill', N'U') IS NULL
    OR OBJECT_ID(N'Hrm.StudyCourse', N'U') IS NULL
    OR OBJECT_ID(N'Hrm.TrendOfStudyCourse', N'U') IS NULL
    OR OBJECT_ID(N'Hrm.IncomingSource', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول اطلاعات پایه وجود ندارند. ابتدا migration AddHrmRecruitmentBaseInfo را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @Seed NVARCHAR(80) = N'seed-hrm-recruitment-base';

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE t SET
        t.Title = LEFT(NULLIF(LTRIM(RTRIM(s.Skill_Title)), N''), 400),
        t.IsActive = 1,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hrm.Skill t
    INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_Skill] s ON t.HtsId = s.Skill_ID;

    INSERT INTO Hrm.Skill
        (HtsId, Title, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.Skill_ID, LEFT(NULLIF(LTRIM(RTRIM(s.Skill_Title)), N''), 400),
        1, @Seed, @Now, @NowShamsi, 1, @Seed, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_Skill] s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.Skill t WHERE t.HtsId = s.Skill_ID);
    PRINT N'  Skill inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE t SET
        t.Title = LEFT(NULLIF(LTRIM(RTRIM(s.StudyCourse_Title)), N''), 400),
        t.HamkaranStudyCourseId = s.Hamkaran_StudyCourse_FK,
        t.IsActive = 1,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hrm.StudyCourse t
    INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_StudyCourse] s ON t.HtsId = s.StudyCourse_ID;

    INSERT INTO Hrm.StudyCourse
        (HtsId, Title, HamkaranStudyCourseId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.StudyCourse_ID, LEFT(NULLIF(LTRIM(RTRIM(s.StudyCourse_Title)), N''), 400), s.Hamkaran_StudyCourse_FK,
        1, @Seed, @Now, @NowShamsi, 1, @Seed, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_StudyCourse] s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.StudyCourse t WHERE t.HtsId = s.StudyCourse_ID);
    PRINT N'  StudyCourse inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE t SET
        t.StudyCourseId = sc.Id,
        t.Title = LEFT(NULLIF(LTRIM(RTRIM(s.TrendOfStudyCourse_Title)), N''), 100),
        t.IsActive = 1,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hrm.TrendOfStudyCourse t
    INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_TrendOfStudyCourse] s ON t.HtsId = s.TrendOfStudyCourse_ID
    INNER JOIN Hrm.StudyCourse sc ON sc.HtsId = s.StudyCourse_FK;

    INSERT INTO Hrm.TrendOfStudyCourse
        (HtsId, StudyCourseId, Title,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.TrendOfStudyCourse_ID, sc.Id, LEFT(NULLIF(LTRIM(RTRIM(s.TrendOfStudyCourse_Title)), N''), 100),
        1, @Seed, @Now, @NowShamsi, 1, @Seed, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[HRM_TrendOfStudyCourse] s
    INNER JOIN Hrm.StudyCourse sc ON sc.HtsId = s.StudyCourse_FK
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.TrendOfStudyCourse t WHERE t.HtsId = s.TrendOfStudyCourse_ID);
    PRINT N'  Trend inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE t SET
        t.Title = LEFT(NULLIF(LTRIM(RTRIM(s.Lookup_Title_Fa)), N''), 100),
        t.IsActive = 1,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hrm.IncomingSource t
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_Lookup] s
        ON t.HtsId = s.Lookup_ID AND s.LookupType_FK = 20;

    INSERT INTO Hrm.IncomingSource
        (HtsId, Title,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.Lookup_ID, LEFT(NULLIF(LTRIM(RTRIM(s.Lookup_Title_Fa)), N''), 100),
        1, @Seed, @Now, @NowShamsi, 1, @Seed, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Gnr_Lookup] s
    WHERE s.LookupType_FK = 20
      AND NOT EXISTS (SELECT 1 FROM Hrm.IncomingSource t WHERE t.HtsId = s.Lookup_ID);
    PRINT N'  IncomingSource inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_HrmRecruitmentBase_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT N'Skill' AS T, COUNT(*) AS Cnt FROM Hrm.Skill
UNION ALL SELECT N'StudyCourse', COUNT(*) FROM Hrm.StudyCourse
UNION ALL SELECT N'TrendOfStudyCourse', COUNT(*) FROM Hrm.TrendOfStudyCourse
UNION ALL SELECT N'IncomingSource', COUNT(*) FROM Hrm.IncomingSource;

SELECT HtsId, Title FROM Hrm.IncomingSource ORDER BY HtsId;
