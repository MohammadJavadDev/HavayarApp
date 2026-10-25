/*
================================================================================
Sync_GuardGuestIo_FromTotalSystem.sql  —  ورود/خروج میهمان (HTS صفحه 428)
================================================================================
HRM_Guard_GuestIO → Hrm.GuardGuestIo
همه ردیف‌ها. کلید HtsId. ردیف‌های ساخته‌شده در Havayar (HtsId = 0) دست‌نخورده می‌مانند.
اجرای مجدد، ویرایش Havayar روی ردیف‌های دارای HtsId را بازنویسی می‌کند. EntryFileId / ExitFileId بازنویسی نمی‌شود.
در منبع هیچ پیوستی نیست؛ جاب انتقال فایل ساخته نشده است.

نگاشت:
  کاربر: Gnr_User.Username یا ActiveDirectoryUsername ↔ system.[User].Username. بی‌تطبیق → شناسه خالی و نام HTS.
  پرسنل: HRM_Personel.PrsCode ↔ Hcm.Personel.Code (فقط وقتی دقیقاً یک پرسنل با آن کد باشد).
  واحد: HRM_OrgUnit.Hamkaran_Unit_FK ↔ Hrm.OrgUnit.HamkaranUnitId؛ در نبود شناسه همکاران، تطبیق با عنوان یکتا.
بی‌تطبیق → خالی و در خروجی گزارش می‌شود. واحد سازمانی جدید ساخته نمی‌شود.

اجرا: @DryRun = 1 (پیش‌فرض) ROLLBACK؛ @DryRun = 0 → COMMIT.
پیش‌نیاز: Linked Server [TMS]، migration AddGuardGuestIo.
UTF-8 BOM.  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Sync_GuardGuestIo_FromTotalSystem.sql"
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

IF OBJECT_ID(N'Hrm.GuardGuestIo') IS NULL
BEGIN
    RAISERROR(N'جدول Hrm.GuardGuestIo وجود ندارد. ابتدا migration AddGuardGuestIo را اعمال کنید.', 16, 1);
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
            LEFT(COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username), 150) AS NewUserName,
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
    SELECT CAST(User_ID AS INT) AS OldUserId, LEFT(CAST(FullName AS NVARCHAR(150)), 150) AS FullName
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

    PRINT N'=== GuardGuestIo ===';
    IF OBJECT_ID('tempdb..#Io') IS NOT NULL DROP TABLE #Io;
    SELECT
        CAST(io.Id AS BIGINT) AS HtsId,
        CAST(io.GuestTypeId AS INT) AS GuestType,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(io.Name)), N''), N''), 50) AS Name,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(io.Family)), N''), N''), 50) AS Family,
        pm.NewPersonelId AS PersonId,
        um.NewOrgUnitId AS UnitId,
        LEFT(NULLIF(LTRIM(RTRIM(io.VisitReason)), N''), 128) AS VisitReason,
        LEFT(NULLIF(LTRIM(RTRIM(io.CompanyTitle)), N''), 50) AS CompanyTitle,
        LEFT(NULLIF(LTRIM(RTRIM(io.Attendant)), N''), 255) AS Attendant,
        CAST(io.EntryDate AS date) AS EntryMiladiDate,
        LEFT(NULLIF(LTRIM(RTRIM(io.EntryDateInText)), N''), 16) AS EntryShamsiDate,
        LEFT(NULLIF(LTRIM(RTRIM(io.EntryTime)), N''), 5) AS EntryTime,
        CAST(io.ExitDate AS date) AS ExitMiladiDate,
        LEFT(NULLIF(LTRIM(RTRIM(io.ExitDateInText)), N''), 16) AS ExitShamsiDate,
        LEFT(NULLIF(LTRIM(RTRIM(io.ExitTime)), N''), 5) AS ExitTime,
        io.Comment,
        io.CreatedDate, LEFT(NULLIF(LTRIM(RTRIM(io.CreatedDateInText)), N''), 30) AS CreatedDateInText,
        io.UpdatedDate, LEFT(NULLIF(LTRIM(RTRIM(io.UpdatedDateInText)), N''), 30) AS UpdatedDateInText,
        cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
        uu.NewUserId AS ModifiedById, COALESCE(uu.NewUserName, uh.FullName) AS ModifiedByName,
        CASE WHEN pm.NewPersonelId IS NULL THEN 1 ELSE 0 END AS UnmappedPerson,
        CASE WHEN um.NewOrgUnitId IS NULL THEN 1 ELSE 0 END AS UnmappedUnit
    INTO #Io
    FROM [TMS].[TotalSystem].[dbo].[HRM_Guard_GuestIO] io
    LEFT JOIN #PersonMap pm ON pm.OldPersonelId = io.VisitorPersonId
    LEFT JOIN #UnitMap um ON um.OldOrgUnitId = io.VisitorUnitId
    LEFT JOIN #UserMap cu ON cu.OldUserId = io.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = io.CreatedUserId
    LEFT JOIN #UserMap uu ON uu.OldUserId = io.UpdatedUserId
    LEFT JOIN #HtsUser uh ON uh.OldUserId = io.UpdatedUserId;

    DECLARE @Src INT = (SELECT COUNT(*) FROM #Io);
    DECLARE @UnmappedPerson INT = (SELECT SUM(UnmappedPerson) FROM #Io);
    DECLARE @UnmappedUnit INT = (SELECT SUM(UnmappedUnit) FROM #Io);
    PRINT N'  source rows: ' + CAST(@Src AS NVARCHAR(20))
        + N' | unmapped person: ' + CAST(ISNULL(@UnmappedPerson, 0) AS NVARCHAR(20))
        + N' | unmapped unit: ' + CAST(ISNULL(@UnmappedUnit, 0) AS NVARCHAR(20));

    UPDATE t SET
        t.GuestType = s.GuestType,
        t.Name = s.Name, t.Family = s.Family,
        t.PersonId = s.PersonId, t.UnitId = s.UnitId,
        t.VisitReason = s.VisitReason, t.CompanyTitle = s.CompanyTitle, t.Attendant = s.Attendant,
        t.EntryMiladiDate = s.EntryMiladiDate, t.EntryShamsiDate = s.EntryShamsiDate, t.EntryTime = s.EntryTime,
        t.ExitMiladiDate = s.ExitMiladiDate, t.ExitShamsiDate = s.ExitShamsiDate, t.ExitTime = s.ExitTime,
        t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.ModifiedById, t.ModifiedByName = s.ModifiedByName,
        t.ModifiedDateMiladiDateTime = s.UpdatedDate, t.ModifiedDateShamsiDateTime = s.UpdatedDateInText,
        t.IsActive = 1
    FROM Hrm.GuardGuestIo t
    INNER JOIN #Io s ON s.HtsId = t.HtsId;
    PRINT N'  updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Hrm.GuardGuestIo
        (HtsId, GuestType, Name, Family, PersonId, UnitId,
         VisitReason, CompanyTitle, Attendant,
         EntryMiladiDate, EntryShamsiDate, EntryTime, ExitMiladiDate, ExitShamsiDate, ExitTime,
         Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.GuestType, s.Name, s.Family, s.PersonId, s.UnitId,
           s.VisitReason, s.CompanyTitle, s.Attendant,
           s.EntryMiladiDate, s.EntryShamsiDate, s.EntryTime, s.ExitMiladiDate, s.ExitShamsiDate, s.ExitTime,
           s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.ModifiedById, s.ModifiedByName, s.UpdatedDate, s.UpdatedDateInText, 1
    FROM #Io s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.GuardGuestIo t WHERE t.HtsId = s.HtsId);
    PRINT N'  inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'=== DRY RUN — ROLLBACK شد. برای اعمال واقعی @DryRun = 0 کنید. ===';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'=== DONE Sync_GuardGuestIo_FromTotalSystem — COMMIT شد ===';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

SELECT 'GuardGuestIo' AS t, COUNT(*) AS n, SUM(CASE WHEN HtsId <> 0 THEN 1 ELSE 0 END) AS fromHts
FROM Hrm.GuardGuestIo;
