/*
================================================================================
Backfill_ProcessDocument_EditingUsers_ExecuterOrgUnits.sql
تصحیح معنای دو ستون HTS در اسناد فرآیندی:

  Bpm_Document.EditingOrganizationUnitIds  («تیم بازنگری/تدوین»)  = شناسهٔ کاربران  (Gnr_User)
  Bpm_Document.ProcessExecuterIds          («مجری فرآیند»)         = شناسهٔ واحدهای سازمانی (HRM_OrgUnit)

هوایار قبلاً برعکس مدل کرده بود. این اسکریپت جدول‌های جدید را از HTS پر می‌کند:
  Bpm.ProcessDocumentEditingUser
  Bpm.ProcessDocumentProcessExecuterOrgUnit
جدول‌های قدیمی (ProcessDocumentEditingOrgUnit / ProcessDocumentProcessExecuter) دست نمی‌خورند.

پیش‌نیاز: Linked Server [TMS]، migration AddProcessDocumentEditingUserAndExecuterOrgUnit.
Idempotent: فقط برای اسناد با HtsId <> 0، حذف و درج مجدد.

اجرا: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Backfill_ProcessDocument_EditingUsers_ExecuterOrgUnits.sql"
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'backfill-bpm-editing-executer';

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    IF OBJECT_ID('tempdb..#OrgMap') IS NOT NULL DROP TABLE #OrgMap;

    SELECT
        CAST(ou.User_ID AS BIGINT) AS OldUserId,
        MIN(nu.Id) AS NewUserId
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON nu.Username = ou.Username
        OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
            AND nu.Username = LTRIM(RTRIM(ou.ActiveDirectoryUsername)))
    GROUP BY CAST(ou.User_ID AS BIGINT);

    SELECT
        CAST(o.OrgUnit_ID AS BIGINT) AS OldOrgUnitId,
        MIN(n.Id) AS NewOrgUnitId
    INTO #OrgMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] o
    INNER JOIN [Hrm].[OrgUnit] n
        ON n.HamkaranUnitId = o.Hamkaran_Unit_FK
    WHERE o.Hamkaran_Unit_FK IS NOT NULL AND o.Hamkaran_Unit_FK <> 0
    GROUP BY CAST(o.OrgUnit_ID AS BIGINT);

    DELETE j FROM Bpm.ProcessDocumentEditingUser j INNER JOIN Bpm.ProcessDocument d ON d.Id = j.ProcessDocumentId WHERE d.HtsId <> 0;
    DELETE j FROM Bpm.ProcessDocumentProcessExecuterOrgUnit j INNER JOIN Bpm.ProcessDocument d ON d.Id = j.ProcessDocumentId WHERE d.HtsId <> 0;

    INSERT INTO Bpm.ProcessDocumentEditingUser
        (ProcessDocumentId, UserId, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT pd.Id, um.NewUserId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.EditingOrganizationUnitIds, N''), N' ', N''), N',') tok
    INNER JOIN #UserMap um ON um.OldUserId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0;
    DECLARE @EditingRows INT = @@ROWCOUNT;

    INSERT INTO Bpm.ProcessDocumentProcessExecuterOrgUnit
        (ProcessDocumentId, OrgUnitId, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT pd.Id, om.NewOrgUnitId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.ProcessExecuterIds, N''), N' ', N''), N',') tok
    INNER JOIN #OrgMap om ON om.OldOrgUnitId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0;
    DECLARE @ExecuterRows INT = @@ROWCOUNT;

    COMMIT TRANSACTION;
    PRINT N'EditingUser rows: ' + CAST(@EditingRows AS NVARCHAR(20));
    PRINT N'ProcessExecuterOrgUnit rows: ' + CAST(@ExecuterRows AS NVARCHAR(20));

    -- ردیف‌های HTS که نگاشت نشدند (برای بررسی)
    SELECT N'EditingUser' AS Kind, COUNT(*) AS UnmappedTokens
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.EditingOrganizationUnitIds, N''), N' ', N''), N',') tok
    LEFT JOIN #UserMap um ON um.OldUserId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0 AND um.NewUserId IS NULL
    UNION ALL
    SELECT N'ProcessExecuterOrgUnit', COUNT(*)
    FROM [TMS].[TotalSystem].[dbo].[Bpm_Document] s
    INNER JOIN Bpm.ProcessDocument pd ON pd.HtsId = s.Id
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(s.ProcessExecuterIds, N''), N' ', N''), N',') tok
    LEFT JOIN #OrgMap om ON om.OldOrgUnitId = TRY_CAST(tok.value AS BIGINT)
    WHERE TRY_CAST(tok.value AS BIGINT) > 0 AND om.NewOrgUnitId IS NULL;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Backfill failed: %s', 16, 1, @Err);
END CATCH;
