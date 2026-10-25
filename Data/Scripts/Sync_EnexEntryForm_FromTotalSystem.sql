/*
================================================================================
Sync_EnexEntryForm_FromTotalSystem.sql
از [TMS].[TotalSystem]:
  Enex_Part          → Hrm.EnexPart          (همه ردیف‌ها، شامل قطعه‌های بدون استفاده)
  Enex_ExitForm      → Hrm.EnexExitForm      (فقط ستون‌های لازم برای ورود و یادآوری)
  Enex_EntryForm     → Hrm.EnexEntryForm
  Enex_FormPart      → Hrm.EnexFormPart      (فقط EntryFormId دارد)
  Enex_FormLog       → Hrm.EnexFormLog       (فقط EntryFormId دارد)
اقلام و لاگِ فقط-خروج منتقل نمی‌شوند.
FileId در اجرای مجدد پاک نمی‌شود. HtsId = 0 دست‌نخورده می‌ماند.
@DryRun = 1 پیش‌فرض، ROLLBACK.  @DryRun = 0 اعمال می‌کند.
UTF-8 BOM. پیش‌نیاز: Linked Server [TMS] و migration AddEnexEntryForm.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Sync_EnexEntryForm_FromTotalSystem.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END
IF OBJECT_ID(N'Hrm.EnexEntryForm') IS NULL
BEGIN
    RAISERROR(N'جدول Hrm.EnexEntryForm وجود ندارد.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Msg INT;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, NewUserId, NewUserName
    INTO #UserMap
    FROM (
        SELECT CAST(ou.User_ID AS INT) AS OldUserId, nu.Id AS NewUserId,
               CAST(LEFT(nu.Username, 150) AS NVARCHAR(150)) AS NewUserName,
               ROW_NUMBER() OVER (PARTITION BY ou.User_ID ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN system.[User] nu
            ON LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
            OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
    ) x WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    IF OBJECT_ID('tempdb..#HtsUser') IS NOT NULL DROP TABLE #HtsUser;
    SELECT CAST(User_ID AS INT) AS OldUserId, CAST(LEFT(FullName, 150) AS NVARCHAR(150)) AS FullName
    INTO #HtsUser FROM [TMS].[TotalSystem].[dbo].[Gnr_User];
    CREATE UNIQUE CLUSTERED INDEX IX_HtsUser ON #HtsUser (OldUserId);

    IF OBJECT_ID('tempdb..#UnitMap') IS NOT NULL DROP TABLE #UnitMap;
    SELECT OldOrgUnitId, NewOrgUnitId INTO #UnitMap
    FROM (
        SELECT CAST(ou.OrgUnit_ID AS INT) AS OldOrgUnitId, nu.Id AS NewOrgUnitId,
               ROW_NUMBER() OVER (PARTITION BY ou.OrgUnit_ID ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] ou
        INNER JOIN Hrm.OrgUnit nu ON nu.HamkaranUnitId = ou.Hamkaran_Unit_FK
        WHERE ou.Hamkaran_Unit_FK IS NOT NULL
        UNION ALL
        SELECT CAST(ou.OrgUnit_ID AS INT), nu.Id,
               ROW_NUMBER() OVER (PARTITION BY ou.OrgUnit_ID ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id)
        FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] ou
        INNER JOIN Hrm.OrgUnit nu ON nu.Title = ou.OrgUnit_Title
        WHERE ou.Hamkaran_Unit_FK IS NULL
          AND NULLIF(LTRIM(RTRIM(ou.OrgUnit_Title)), N'') IS NOT NULL
          AND (SELECT COUNT(*) FROM Hrm.OrgUnit x WHERE x.Title = ou.OrgUnit_Title) = 1
    ) x WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UnitMap ON #UnitMap (OldOrgUnitId);

    IF OBJECT_ID('tempdb..#PartUnitMap') IS NOT NULL DROP TABLE #PartUnitMap;
    SELECT OldUnitId, NewUnitId INTO #PartUnitMap
    FROM (
        SELECT CAST(ou.PartUnit_ID AS INT) AS OldUnitId, nu.Id AS NewUnitId,
               ROW_NUMBER() OVER (PARTITION BY ou.PartUnit_ID ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Inv_PartUnit] ou
        INNER JOIN Inv.PartUnit nu ON nu.HamkaranId = ou.Hamkaran_PartUnit_FK
        WHERE ou.Hamkaran_PartUnit_FK IS NOT NULL
        UNION ALL
        SELECT CAST(ou.PartUnit_ID AS INT), nu.Id,
               ROW_NUMBER() OVER (PARTITION BY ou.PartUnit_ID ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id)
        FROM [TMS].[TotalSystem].[dbo].[Inv_PartUnit] ou
        INNER JOIN Inv.PartUnit nu ON nu.Title = ou.PartUnit_Title
        WHERE NOT EXISTS (SELECT 1 FROM Inv.PartUnit h WHERE h.HamkaranId = ou.Hamkaran_PartUnit_FK AND ou.Hamkaran_PartUnit_FK IS NOT NULL)
          AND NULLIF(LTRIM(RTRIM(ou.PartUnit_Title)), N'') IS NOT NULL
          AND (SELECT COUNT(*) FROM Inv.PartUnit x WHERE x.Title = ou.PartUnit_Title) = 1
    ) x WHERE rn = 1;

    /* 1) قطعه */
    IF OBJECT_ID('tempdb..#Part') IS NOT NULL DROP TABLE #Part;
    SELECT CAST(p.Id AS BIGINT) AS HtsId, p.Title, NULLIF(LTRIM(RTRIM(p.Code)), N'') AS Code,
           um.NewUnitId AS PartUnitId, p.Comment,
           p.CreatedDate, NULLIF(LTRIM(RTRIM(p.CreatedDateInText)), N'') AS CreatedDateInText,
           cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
           CASE WHEN um.NewUnitId IS NULL THEN 1 ELSE 0 END AS UnmappedUnit
    INTO #Part
    FROM [TMS].[TotalSystem].[dbo].[Enex_Part] p
    LEFT JOIN #PartUnitMap um ON um.OldUnitId = p.UnitId
    LEFT JOIN #UserMap cu ON cu.OldUserId = p.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = p.CreatedUserId;

    UPDATE t SET t.Title = s.Title, t.Code = s.Code, t.PartUnitId = s.PartUnitId, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.CreatedById, t.ModifiedByName = s.CreatedByName,
        t.ModifiedDateMiladiDateTime = s.CreatedDate, t.ModifiedDateShamsiDateTime = s.CreatedDateInText, t.IsActive = 1
    FROM Hrm.EnexPart t INNER JOIN #Part s ON s.HtsId = t.HtsId;

    INSERT INTO Hrm.EnexPart
        (HtsId, Title, Code, PartUnitId, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.Title, s.Code, s.PartUnitId, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText, 1
    FROM #Part s WHERE NOT EXISTS (SELECT 1 FROM Hrm.EnexPart t WHERE t.HtsId = s.HtsId);
    SET @Msg = (SELECT SUM(UnmappedUnit) FROM #Part);
    PRINT N'EnexPart unmapped unit: ' + CAST(ISNULL(@Msg, 0) AS NVARCHAR(20));

    /* 2) فرم خروج */
    IF OBJECT_ID('tempdb..#Exit') IS NOT NULL DROP TABLE #Exit;
    SELECT CAST(e.Id AS BIGINT) AS HtsId, e.FormNumber, CAST(e.IsReturnable AS BIT) AS IsReturnable, e.LastStatusId,
           CAST(e.ClaimedReturnDate AS date) AS ClaimedReturnMiladiDate, NULLIF(LTRIM(RTRIM(e.ClaimedReturnDateInText)), N'') AS ClaimedReturnShamsiDate,
           CAST(e.IsReturned AS BIT) AS IsReturned,
           CAST(e.ReturnDate AS date) AS ReturnMiladiDate, NULLIF(LTRIM(RTRIM(e.ReturnDateInText)), N'') AS ReturnShamsiDate,
           e.AttachmentFileName, NULLIF(LTRIM(RTRIM(e.AttachmentFilePath)), N'') AS HtsAttachmentPath,
           uo.NewOrgUnitId AS OrganizationUnitId,
           e.CreatedDate, NULLIF(LTRIM(RTRIM(e.CreatedDateInText)), N'') AS CreatedDateInText,
           cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
           CASE WHEN uo.NewOrgUnitId IS NULL THEN 1 ELSE 0 END AS UnmappedUnit
    INTO #Exit
    FROM [TMS].[TotalSystem].[dbo].[Enex_ExitForm] e
    LEFT JOIN #UnitMap uo ON uo.OldOrgUnitId = e.OrganizationUnitId
    LEFT JOIN #UserMap cu ON cu.OldUserId = e.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = e.CreatedUserId;

    UPDATE t SET t.FormNumber = s.FormNumber, t.IsReturnable = s.IsReturnable, t.LastStatusId = s.LastStatusId,
        t.ClaimedReturnMiladiDate = s.ClaimedReturnMiladiDate, t.ClaimedReturnShamsiDate = s.ClaimedReturnShamsiDate,
        t.IsReturned = s.IsReturned, t.ReturnMiladiDate = s.ReturnMiladiDate, t.ReturnShamsiDate = s.ReturnShamsiDate,
        t.AttachmentFileName = s.AttachmentFileName, t.HtsAttachmentPath = s.HtsAttachmentPath,
        t.OrganizationUnitId = s.OrganizationUnitId,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.CreatedById, t.ModifiedByName = s.CreatedByName,
        t.ModifiedDateMiladiDateTime = s.CreatedDate, t.ModifiedDateShamsiDateTime = s.CreatedDateInText, t.IsActive = 1
    FROM Hrm.EnexExitForm t INNER JOIN #Exit s ON s.HtsId = t.HtsId;

    INSERT INTO Hrm.EnexExitForm
        (HtsId, FormNumber, IsReturnable, LastStatusId, ClaimedReturnShamsiDate, ClaimedReturnMiladiDate,
         IsReturned, ReturnShamsiDate, ReturnMiladiDate, AttachmentFileName, HtsAttachmentPath, OrganizationUnitId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.FormNumber, s.IsReturnable, s.LastStatusId, s.ClaimedReturnShamsiDate, s.ClaimedReturnMiladiDate,
           s.IsReturned, s.ReturnShamsiDate, s.ReturnMiladiDate, s.AttachmentFileName, s.HtsAttachmentPath, s.OrganizationUnitId,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText, 1
    FROM #Exit s WHERE NOT EXISTS (SELECT 1 FROM Hrm.EnexExitForm t WHERE t.HtsId = s.HtsId);
    SET @Msg = (SELECT SUM(UnmappedUnit) FROM #Exit);
    PRINT N'EnexExitForm unmapped unit: ' + CAST(ISNULL(@Msg, 0) AS NVARCHAR(20));

    /* 3) فرم ورود */
    IF OBJECT_ID('tempdb..#Entry') IS NOT NULL DROP TABLE #Entry;
    SELECT CAST(e.Id AS BIGINT) AS HtsId, e.FormNumber, CAST(ISNULL(e.IsFromExitForm, 1) AS BIT) AS IsFromExitForm,
           xf.Id AS ExitFormId, e.ImporterTitle, e.CompanyTitle, CAST(e.LastStatusId AS INT) AS LastStatusId,
           uo.NewOrgUnitId AS OrganizationUnitId, e.Comment,
           e.CreatedDate, NULLIF(LTRIM(RTRIM(e.CreatedDateInText)), N'') AS CreatedDateInText,
           cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
           CASE WHEN e.ExitFormId IS NOT NULL AND xf.Id IS NULL THEN 1 ELSE 0 END AS UnmappedExit,
           CASE WHEN uo.NewOrgUnitId IS NULL THEN 1 ELSE 0 END AS UnmappedUnit
    INTO #Entry
    FROM [TMS].[TotalSystem].[dbo].[Enex_EntryForm] e
    LEFT JOIN Hrm.EnexExitForm xf ON xf.HtsId = CAST(e.ExitFormId AS BIGINT) AND ISNULL(xf.HtsId, 0) <> 0
    LEFT JOIN #UnitMap uo ON uo.OldOrgUnitId = e.OrganizationUnitId
    LEFT JOIN #UserMap cu ON cu.OldUserId = e.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = e.CreatedUserId;

    UPDATE t SET t.FormNumber = s.FormNumber, t.IsFromExitForm = s.IsFromExitForm, t.ExitFormId = s.ExitFormId,
        t.ImporterTitle = s.ImporterTitle, t.CompanyTitle = s.CompanyTitle, t.LastStatusId = s.LastStatusId,
        t.OrganizationUnitId = s.OrganizationUnitId, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.CreatedById, t.ModifiedByName = s.CreatedByName,
        t.ModifiedDateMiladiDateTime = s.CreatedDate, t.ModifiedDateShamsiDateTime = s.CreatedDateInText, t.IsActive = 1
    FROM Hrm.EnexEntryForm t INNER JOIN #Entry s ON s.HtsId = t.HtsId;

    INSERT INTO Hrm.EnexEntryForm
        (HtsId, FormNumber, IsFromExitForm, ExitFormId, ImporterTitle, CompanyTitle, LastStatusId, OrganizationUnitId, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.FormNumber, s.IsFromExitForm, s.ExitFormId, s.ImporterTitle, s.CompanyTitle, s.LastStatusId, s.OrganizationUnitId, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText, 1
    FROM #Entry s WHERE NOT EXISTS (SELECT 1 FROM Hrm.EnexEntryForm t WHERE t.HtsId = s.HtsId);
    SET @Msg = (SELECT SUM(UnmappedExit) FROM #Entry);
    DECLARE @Msg2 INT = (SELECT SUM(UnmappedUnit) FROM #Entry);
    PRINT N'EnexEntryForm unmapped exit/unit: ' + CAST(ISNULL(@Msg, 0) AS NVARCHAR(20)) + N'/' + CAST(ISNULL(@Msg2, 0) AS NVARCHAR(20));

    /* 4) اقلام ورود */
    IF OBJECT_ID('tempdb..#FPart') IS NOT NULL DROP TABLE #FPart;
    SELECT CAST(p.Id AS BIGINT) AS HtsId, en.Id AS EntryFormId, xf.Id AS ExitFormId,
           ep.Id AS EnexPartId, ip.Id AS PartId, p.Mount, p.EnterAmount, CAST(p.IsSecurityDisApproved AS BIT) AS IsSecurityDisApproved,
           CAST(p.ReturnDate AS date) AS ReturnMiladiDate, NULLIF(LTRIM(RTRIM(p.ReturnDateInText)), N'') AS ReturnShamsiDate,
           p.Comment, NULLIF(LTRIM(RTRIM(p.AttachmentFileName)), N'') AS HtsAttachmentFileName,
           CASE WHEN p.EnexPartId IS NOT NULL AND ep.Id IS NULL THEN 1 ELSE 0 END AS UnmappedEnexPart,
           CASE WHEN p.PartId IS NOT NULL AND ip.Id IS NULL THEN 1 ELSE 0 END AS UnmappedInvPart
    INTO #FPart
    FROM [TMS].[TotalSystem].[dbo].[Enex_FormPart] p
    INNER JOIN Hrm.EnexEntryForm en ON en.HtsId = CAST(p.EntryFormId AS BIGINT) AND ISNULL(en.HtsId, 0) <> 0
    LEFT JOIN Hrm.EnexExitForm xf ON xf.HtsId = CAST(p.ExitFormId AS BIGINT) AND ISNULL(xf.HtsId, 0) <> 0
    LEFT JOIN Hrm.EnexPart ep ON ep.HtsId = CAST(p.EnexPartId AS BIGINT) AND ISNULL(ep.HtsId, 0) <> 0
    LEFT JOIN Inv.Part ip ON ip.HtsId = CAST(p.PartId AS BIGINT) AND ISNULL(ip.HtsId, 0) <> 0
    WHERE p.EntryFormId IS NOT NULL;

    UPDATE t SET t.EntryFormId = s.EntryFormId, t.ExitFormId = s.ExitFormId, t.EnexPartId = s.EnexPartId, t.PartId = s.PartId,
        t.Mount = s.Mount, t.EnterAmount = s.EnterAmount, t.IsSecurityDisApproved = s.IsSecurityDisApproved,
        t.ReturnMiladiDate = s.ReturnMiladiDate, t.ReturnShamsiDate = s.ReturnShamsiDate, t.Comment = s.Comment,
        t.HtsAttachmentFileName = s.HtsAttachmentFileName, t.IsActive = 1
    FROM Hrm.EnexFormPart t INNER JOIN #FPart s ON s.HtsId = t.HtsId;

    INSERT INTO Hrm.EnexFormPart
        (HtsId, EntryFormId, ExitFormId, EnexPartId, PartId, Mount, EnterAmount, IsSecurityDisApproved,
         ReturnShamsiDate, ReturnMiladiDate, Comment, HtsAttachmentFileName, IsActive)
    SELECT s.HtsId, s.EntryFormId, s.ExitFormId, s.EnexPartId, s.PartId, s.Mount, s.EnterAmount, s.IsSecurityDisApproved,
           s.ReturnShamsiDate, s.ReturnMiladiDate, s.Comment, s.HtsAttachmentFileName, 1
    FROM #FPart s WHERE NOT EXISTS (SELECT 1 FROM Hrm.EnexFormPart t WHERE t.HtsId = s.HtsId);
    SET @Msg = (SELECT COUNT(*) FROM #FPart);
    PRINT N'EnexFormPart rows: ' + CAST(ISNULL(@Msg, 0) AS NVARCHAR(20));

    /* 5) لاگ ورود */
    IF OBJECT_ID('tempdb..#FLog') IS NOT NULL DROP TABLE #FLog;
    SELECT CAST(l.Id AS BIGINT) AS HtsId, en.Id AS EntryFormId, CAST(l.StatusId AS INT) AS StatusId, l.Comment,
           l.CreatedDate, NULLIF(LTRIM(RTRIM(l.CreatedDateInText)), N'') AS CreatedDateInText,
           cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName
    INTO #FLog
    FROM [TMS].[TotalSystem].[dbo].[Enex_FormLog] l
    INNER JOIN Hrm.EnexEntryForm en ON en.HtsId = CAST(l.EntryFormId AS BIGINT) AND ISNULL(en.HtsId, 0) <> 0
    LEFT JOIN #UserMap cu ON cu.OldUserId = l.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = l.CreatedUserId
    WHERE l.EntryFormId IS NOT NULL;

    UPDATE t SET t.EntryFormId = s.EntryFormId, t.StatusId = s.StatusId, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.CreatedById, t.ModifiedByName = s.CreatedByName,
        t.ModifiedDateMiladiDateTime = s.CreatedDate, t.ModifiedDateShamsiDateTime = s.CreatedDateInText, t.IsActive = 1
    FROM Hrm.EnexFormLog t INNER JOIN #FLog s ON s.HtsId = t.HtsId;

    INSERT INTO Hrm.EnexFormLog
        (HtsId, EntryFormId, StatusId, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.EntryFormId, s.StatusId, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText, 1
    FROM #FLog s WHERE NOT EXISTS (SELECT 1 FROM Hrm.EnexFormLog t WHERE t.HtsId = s.HtsId);
    SET @Msg = (SELECT COUNT(*) FROM #FLog);
    PRINT N'EnexFormLog rows: ' + CAST(ISNULL(@Msg, 0) AS NVARCHAR(20));

    SELECT N'EnexPart' AS Tbl, COUNT(*) AS Cnt FROM Hrm.EnexPart
    UNION ALL SELECT N'EnexExitForm', COUNT(*) FROM Hrm.EnexExitForm
    UNION ALL SELECT N'EnexEntryForm', COUNT(*) FROM Hrm.EnexEntryForm
    UNION ALL SELECT N'EnexFormPart', COUNT(*) FROM Hrm.EnexFormPart
    UNION ALL SELECT N'EnexFormLog', COUNT(*) FROM Hrm.EnexFormLog;

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'=== DRY RUN — ROLLBACK. برای اعمال @DryRun = 0 ===';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'=== COMMIT Sync_EnexEntryForm ===';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH
