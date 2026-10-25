/*
================================================================================
Sync_GuardPersonIo_FromTotalSystem.sql  —  ورود/خروج پرسنل/پیمانکار (HTS صفحه 427)
================================================================================
HRM_Guard_PersonIO → Hrm.GuardPersonIo
همه ردیف‌ها. کلید HtsId. ردیف‌های ساخته‌شده در Havayar (HtsId = 0) دست‌نخورده می‌مانند.
اجرای مجدد، ویرایش Havayar روی ردیف‌های دارای HtsId را بازنویسی می‌کند. EntryFileId / ExitFileId بازنویسی نمی‌شود.

نگاشت:
  کاربر: Gnr_User.Username یا ActiveDirectoryUsername ↔ system.[User].Username. بی‌تطبیق → شناسه خالی و نام HTS.
  پرسنل: HRM_Personel.PrsCode ↔ Hcm.Personel.Code (فقط وقتی دقیقاً یک پرسنل با آن کد باشد).
  واحد: HRM_OrgUnit.Hamkaran_Unit_FK ↔ Hrm.OrgUnit.HamkaranUnitId؛ در نبود شناسه همکاران، تطبیق با عنوان یکتا.
  پیمانکار: Hrm.GuardContractor.HtsId.
بی‌تطبیق → خالی و در خروجی گزارش می‌شود. ۵۴ ردیف منبع که UnitId خالی است همان‌طور خالی می‌مانند.

پیوست ورود: فقط مسیر در HtsEntryFilePath. انتقال فایل با جاب GuardPersonIoFileImportJob.

اجرا: @DryRun = 1 (پیش‌فرض) ROLLBACK؛ @DryRun = 0 → COMMIT.
پیش‌نیاز: Linked Server [TMS]، migration AddGuardPersonIo، Sync_GuardBasicInfo (پیمانکار).
UTF-8 BOM.  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Sync_GuardPersonIo_FromTotalSystem.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;   -- 0 = اعمال واقعی

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Hrm.GuardPersonIo') IS NULL
BEGIN
    RAISERROR(N'جدول Hrm.GuardPersonIo وجود ندارد. ابتدا migration AddGuardPersonIo را اعمال کنید.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, NewUserId, NewUserName
    INTO #UserMap
    FROM (
        SELECT
            CAST(ou.User_ID AS INT) AS OldUserId,
            nu.Id AS NewUserId,
            COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username) AS NewUserName,
            ROW_NUMBER() OVER (
                PARTITION BY ou.User_ID
                ORDER BY
                    CASE WHEN LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username))) THEN 0 ELSE 1 END,
                    CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END,
                    nu.Id
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN system.[User] nu
            ON LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
            OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    IF OBJECT_ID('tempdb..#HtsUser') IS NOT NULL DROP TABLE #HtsUser;
    SELECT CAST(User_ID AS INT) AS OldUserId, CAST(FullName AS NVARCHAR(150)) AS FullName
    INTO #HtsUser
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User];
    CREATE UNIQUE CLUSTERED INDEX IX_HtsUser ON #HtsUser (OldUserId);

    IF OBJECT_ID('tempdb..#PersonMap') IS NOT NULL DROP TABLE #PersonMap;
    SELECT OldPersonelId, NewPersonelId
    INTO #PersonMap
    FROM (
        SELECT
            CAST(op.Personel_ID AS INT) AS OldPersonelId,
            np.Id AS NewPersonelId,
            ROW_NUMBER() OVER (
                PARTITION BY op.Personel_ID
                ORDER BY CASE WHEN np.IsActive = 1 THEN 0 ELSE 1 END, np.Id
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] op
        INNER JOIN Hcm.Personel np ON np.Code = op.PrsCode
        WHERE NULLIF(LTRIM(RTRIM(op.PrsCode)), N'') IS NOT NULL
          AND (SELECT COUNT(*) FROM Hcm.Personel x WHERE x.Code = op.PrsCode) = 1
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_PersonMap ON #PersonMap (OldPersonelId);

    IF OBJECT_ID('tempdb..#UnitMap') IS NOT NULL DROP TABLE #UnitMap;
    SELECT OldOrgUnitId, NewOrgUnitId
    INTO #UnitMap
    FROM (
        SELECT
            CAST(ou.OrgUnit_ID AS INT) AS OldOrgUnitId,
            nu.Id AS NewOrgUnitId,
            ROW_NUMBER() OVER (
                PARTITION BY ou.OrgUnit_ID
                ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] ou
        INNER JOIN Hrm.OrgUnit nu ON nu.HamkaranUnitId = ou.Hamkaran_Unit_FK
        WHERE ou.Hamkaran_Unit_FK IS NOT NULL
        UNION ALL
        SELECT
            CAST(ou.OrgUnit_ID AS INT), nu.Id,
            ROW_NUMBER() OVER (PARTITION BY ou.OrgUnit_ID ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id)
        FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] ou
        INNER JOIN Hrm.OrgUnit nu ON nu.Title = ou.OrgUnit_Title
        WHERE ou.Hamkaran_Unit_FK IS NULL
          AND NULLIF(LTRIM(RTRIM(ou.OrgUnit_Title)), N'') IS NOT NULL
          AND (SELECT COUNT(*) FROM Hrm.OrgUnit x WHERE x.Title = ou.OrgUnit_Title) = 1
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UnitMap ON #UnitMap (OldOrgUnitId);

    PRINT N'=== GuardPersonIo ===';
    IF OBJECT_ID('tempdb..#Io') IS NOT NULL DROP TABLE #Io;
    SELECT
        CAST(io.Id AS BIGINT) AS HtsId,
        CAST(io.OperatorTypeId AS INT) AS OperatorType,
        pm.NewPersonelId AS PersonId,
        um.NewOrgUnitId AS UnitId,
        co.Id AS ContractorId,
        CAST(io.EntryDate AS date) AS EntryMiladiDate,
        NULLIF(LTRIM(RTRIM(io.EntryDateInText)), N'') AS EntryShamsiDate,
        NULLIF(LTRIM(RTRIM(io.EntryTime)), N'') AS EntryTime,
        CAST(io.ExitDate AS date) AS ExitMiladiDate,
        NULLIF(LTRIM(RTRIM(io.ExitDateInText)), N'') AS ExitShamsiDate,
        NULLIF(LTRIM(RTRIM(io.ExitTime)), N'') AS ExitTime,
        NULLIF(LTRIM(RTRIM(io.EntryFilePath)), N'') AS HtsEntryFilePath,
        io.Comment,
        io.CreatedDate, NULLIF(LTRIM(RTRIM(io.CreatedDateInText)), N'') AS CreatedDateInText,
        io.UpdatedDate, NULLIF(LTRIM(RTRIM(io.UpdatedDateInText)), N'') AS UpdatedDateInText,
        cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
        uu.NewUserId AS ModifiedById, COALESCE(uu.NewUserName, uh.FullName) AS ModifiedByName,
        CASE WHEN io.PersonId IS NOT NULL AND pm.NewPersonelId IS NULL THEN 1 ELSE 0 END AS UnmappedPerson,
        CASE WHEN io.UnitId IS NOT NULL AND um.NewOrgUnitId IS NULL THEN 1 ELSE 0 END AS UnmappedUnit,
        CASE WHEN io.ContractorId IS NOT NULL AND co.Id IS NULL THEN 1 ELSE 0 END AS UnmappedContractor
    INTO #Io
    FROM [TMS].[TotalSystem].[dbo].[HRM_Guard_PersonIO] io
    LEFT JOIN #PersonMap pm ON pm.OldPersonelId = io.PersonId
    LEFT JOIN #UnitMap um ON um.OldOrgUnitId = io.UnitId
    LEFT JOIN Hrm.GuardContractor co ON co.HtsId = CAST(io.ContractorId AS BIGINT) AND ISNULL(co.HtsId, 0) <> 0
    LEFT JOIN #UserMap cu ON cu.OldUserId = io.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = io.CreatedUserId
    LEFT JOIN #UserMap uu ON uu.OldUserId = io.UpdatedUserId
    LEFT JOIN #HtsUser uh ON uh.OldUserId = io.UpdatedUserId;

    DECLARE @Src INT = (SELECT COUNT(*) FROM #Io);
    DECLARE @UnmappedPerson INT = (SELECT SUM(UnmappedPerson) FROM #Io);
    DECLARE @UnmappedUnit INT = (SELECT SUM(UnmappedUnit) FROM #Io);
    DECLARE @UnmappedContractor INT = (SELECT SUM(UnmappedContractor) FROM #Io);
    DECLARE @WithFile INT = (SELECT SUM(CASE WHEN HtsEntryFilePath IS NOT NULL THEN 1 ELSE 0 END) FROM #Io);
    PRINT N'  source rows: ' + CAST(@Src AS NVARCHAR(20))
        + N' | unmapped person: ' + CAST(ISNULL(@UnmappedPerson, 0) AS NVARCHAR(20))
        + N' | unmapped unit: ' + CAST(ISNULL(@UnmappedUnit, 0) AS NVARCHAR(20))
        + N' | unmapped contractor: ' + CAST(ISNULL(@UnmappedContractor, 0) AS NVARCHAR(20))
        + N' | entry files: ' + CAST(ISNULL(@WithFile, 0) AS NVARCHAR(20));

    UPDATE t SET
        t.OperatorType = s.OperatorType,
        t.PersonId = s.PersonId, t.UnitId = s.UnitId, t.ContractorId = s.ContractorId,
        t.EntryMiladiDate = s.EntryMiladiDate, t.EntryShamsiDate = s.EntryShamsiDate, t.EntryTime = s.EntryTime,
        t.ExitMiladiDate = s.ExitMiladiDate, t.ExitShamsiDate = s.ExitShamsiDate, t.ExitTime = s.ExitTime,
        t.HtsEntryFilePath = s.HtsEntryFilePath, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.ModifiedById, t.ModifiedByName = s.ModifiedByName,
        t.ModifiedDateMiladiDateTime = s.UpdatedDate, t.ModifiedDateShamsiDateTime = s.UpdatedDateInText,
        t.IsActive = 1
    FROM Hrm.GuardPersonIo t
    INNER JOIN #Io s ON s.HtsId = t.HtsId;
    PRINT N'  updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Hrm.GuardPersonIo
        (HtsId, OperatorType, PersonId, UnitId, ContractorId,
         EntryMiladiDate, EntryShamsiDate, EntryTime, ExitMiladiDate, ExitShamsiDate, ExitTime,
         HtsEntryFilePath, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.OperatorType, s.PersonId, s.UnitId, s.ContractorId,
           s.EntryMiladiDate, s.EntryShamsiDate, s.EntryTime, s.ExitMiladiDate, s.ExitShamsiDate, s.ExitTime,
           s.HtsEntryFilePath, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.ModifiedById, s.ModifiedByName, s.UpdatedDate, s.UpdatedDateInText, 1
    FROM #Io s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.GuardPersonIo t WHERE t.HtsId = s.HtsId);
    PRINT N'  inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'=== DRY RUN — ROLLBACK شد. برای اعمال واقعی @DryRun = 0 کنید. ===';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'=== DONE Sync_GuardPersonIo_FromTotalSystem — COMMIT شد ===';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

SELECT 'GuardPersonIo' AS t, COUNT(*) AS n, SUM(CASE WHEN HtsId <> 0 THEN 1 ELSE 0 END) AS fromHts
FROM Hrm.GuardPersonIo;
