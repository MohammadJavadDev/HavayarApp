/*
================================================================================
Sync_GuardBasicInfo_FromTotalSystem.sql  —  اطلاعات پایه حراست/دژبان (HTS System 9، صفحات 421–424)
================================================================================
همگام‌سازی از [TMS].[TotalSystem].dbo:
  HRM_Guard_Part       → Hrm.GuardPart
  HRM_Guard_Driver     → Hrm.GuardDriver
  HRM_Guard_Contractor → Hrm.GuardContractor
  HRM_Guard_Location   → Hrm.GuardLocation

قواعد:
  - کلید: HtsId = Id در HTS. MERGE تکرارپذیر (به‌روزرسانی ردیف‌های دارای HtsId + درج جدیدها). ردیف‌هایی که در Havayar
    ساخته شده‌اند (HtsId = 0) دست‌نخورده می‌مانند. تا کات‌اور HTS منبع حقیقی است و اجرای مجدد، ویرایش‌های سمت Havayar
    روی ردیف‌های دارای HtsId را بازنویسی می‌کند.
  - همه ردیف‌ها منتقل می‌شوند (تصمیم کاربر)، از جمله استفاده‌نشده‌ها و تکراری‌ها.
  - واحد کالا: Gnr_Lookup نوع 4 (GuardPartUnitEnum). مقدار نامعتبر (مثلاً 1 = «عدم مشاهده» از نوع دسترسی) → NULL (گزارش می‌شود).
  - کاربر: Gnr_User.Username یا ActiveDirectoryUsername ↔ system.[User].Username. بی‌تطبیق → CreatedById/ModifiedById خالی
    و نام کاربر HTS (FullName) در CreatedByName/ModifiedByName حفظ می‌شود.
  - ایجاد/ویرایش: CreatedDate/CreatedDateInText و UpdatedDate/UpdatedDateInText از HTS (مکان در HTS فیلد ویرایش ندارد → همان ایجاد).

اجرا:  @DryRun = 1 (پیش‌فرض) همه‌چیز را در تراکنش اجرا و ROLLBACK می‌کند (فقط گزارش)؛  @DryRun = 0 → COMMIT.
پیش‌نیاز: Linked Server [TMS]، migration AddGuardBasicInfo.
UTF-8 BOM.  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Sync_GuardBasicInfo_FromTotalSystem.sql"
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

IF OBJECT_ID(N'Hrm.GuardPart') IS NULL
BEGIN
    RAISERROR(N'جدول Hrm.GuardPart وجود ندارد. ابتدا migration AddGuardBasicInfo را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-guard-basicinfo';
DECLARE @n1 INT, @n2 INT;

BEGIN TRY
    BEGIN TRANSACTION;

    /* ───────── نگاشت کاربر ───────── */
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

    /* ───────── کالا ───────── */
    PRINT N'=== GuardPart ===';
    IF OBJECT_ID('tempdb..#Part') IS NOT NULL DROP TABLE #Part;
    SELECT
        CAST(p.Id AS BIGINT) AS HtsId,
        p.Title,
        CASE WHEN p.EnumerationUnitId IN (13,1414,1415,1416,1417,1418,1419,1420,1421,1422,1423,1424,1425,1430,1469,1492,1502,1519,1534,1764,1765,2109)
             THEN CAST(p.EnumerationUnitId AS INT) END AS EnumerationUnit,
        CASE WHEN p.EnumerationUnitId IN (13,1414,1415,1416,1417,1418,1419,1420,1421,1422,1423,1424,1425,1430,1469,1492,1502,1519,1534,1764,1765,2109)
             THEN 0 ELSE 1 END AS InvalidUnit,
        CAST(p.IsDisable AS BIT) AS IsDisable,
        p.Comment,
        p.CreatedDate, NULLIF(LTRIM(RTRIM(p.CreatedDateInText)), N'') AS CreatedDateInText,
        p.UpdatedDate, NULLIF(LTRIM(RTRIM(p.UpdatedDateInText)), N'') AS UpdatedDateInText,
        cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
        uu.NewUserId AS ModifiedById, COALESCE(uu.NewUserName, uh.FullName) AS ModifiedByName
    INTO #Part
    FROM [TMS].[TotalSystem].[dbo].[HRM_Guard_Part] p
    LEFT JOIN #UserMap cu ON cu.OldUserId = p.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = p.CreatedUserId
    LEFT JOIN #UserMap uu ON uu.OldUserId = p.UpdatedUserId
    LEFT JOIN #HtsUser uh ON uh.OldUserId = p.UpdatedUserId;

    SET @n1 = (SELECT COUNT(*) FROM #Part);
    SET @n2 = (SELECT COUNT(*) FROM #Part WHERE InvalidUnit = 1);
    PRINT N'  source rows: ' + CAST(@n1 AS NVARCHAR(20)) + N' | invalid unit -> NULL: ' + CAST(@n2 AS NVARCHAR(20));

    UPDATE t SET
        t.Title = s.Title, t.EnumerationUnit = s.EnumerationUnit, t.IsDisable = s.IsDisable, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.ModifiedById, t.ModifiedByName = s.ModifiedByName,
        t.ModifiedDateMiladiDateTime = s.UpdatedDate, t.ModifiedDateShamsiDateTime = s.UpdatedDateInText,
        t.IsActive = 1
    FROM Hrm.GuardPart t
    INNER JOIN #Part s ON s.HtsId = t.HtsId;
    PRINT N'  updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Hrm.GuardPart
        (HtsId, Title, EnumerationUnit, IsDisable, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.Title, s.EnumerationUnit, s.IsDisable, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.ModifiedById, s.ModifiedByName, s.UpdatedDate, s.UpdatedDateInText, 1
    FROM #Part s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.GuardPart t WHERE t.HtsId = s.HtsId);
    PRINT N'  inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ───────── راننده ───────── */
    PRINT N'=== GuardDriver ===';
    IF OBJECT_ID('tempdb..#Driver') IS NOT NULL DROP TABLE #Driver;
    SELECT
        CAST(d.Id AS BIGINT) AS HtsId,
        d.Name, d.Family, CAST(d.CarTypeId AS INT) AS CarType, d.CarName, d.LicensePlate, d.Comment,
        d.CreatedDate, NULLIF(LTRIM(RTRIM(d.CreatedDateInText)), N'') AS CreatedDateInText,
        d.UpdatedDate, NULLIF(LTRIM(RTRIM(d.UpdatedDateInText)), N'') AS UpdatedDateInText,
        cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
        uu.NewUserId AS ModifiedById, COALESCE(uu.NewUserName, uh.FullName) AS ModifiedByName
    INTO #Driver
    FROM [TMS].[TotalSystem].[dbo].[HRM_Guard_Driver] d
    LEFT JOIN #UserMap cu ON cu.OldUserId = d.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = d.CreatedUserId
    LEFT JOIN #UserMap uu ON uu.OldUserId = d.UpdatedUserId
    LEFT JOIN #HtsUser uh ON uh.OldUserId = d.UpdatedUserId;

    SET @n1 = (SELECT COUNT(*) FROM #Driver);
    SET @n2 = (SELECT COUNT(*) FROM #Driver WHERE CarType NOT IN (1405,1406,1407,1408,1409,1447));
    PRINT N'  source rows: ' + CAST(@n1 AS NVARCHAR(20)) + N' | invalid car type: ' + CAST(@n2 AS NVARCHAR(20));

    UPDATE t SET
        t.Name = s.Name, t.Family = s.Family, t.CarType = s.CarType, t.CarName = s.CarName,
        t.LicensePlate = s.LicensePlate, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.ModifiedById, t.ModifiedByName = s.ModifiedByName,
        t.ModifiedDateMiladiDateTime = s.UpdatedDate, t.ModifiedDateShamsiDateTime = s.UpdatedDateInText,
        t.IsActive = 1
    FROM Hrm.GuardDriver t
    INNER JOIN #Driver s ON s.HtsId = t.HtsId;
    PRINT N'  updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Hrm.GuardDriver
        (HtsId, Name, Family, CarType, CarName, LicensePlate, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.Name, s.Family, s.CarType, s.CarName, s.LicensePlate, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.ModifiedById, s.ModifiedByName, s.UpdatedDate, s.UpdatedDateInText, 1
    FROM #Driver s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.GuardDriver t WHERE t.HtsId = s.HtsId);
    PRINT N'  inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ───────── پیمانکار ───────── */
    PRINT N'=== GuardContractor ===';
    IF OBJECT_ID('tempdb..#Contractor') IS NOT NULL DROP TABLE #Contractor;
    SELECT
        CAST(c.Id AS BIGINT) AS HtsId,
        c.Name, c.Family, c.PhoneNumber, c.Address, c.Comment,
        c.CreatedDate, NULLIF(LTRIM(RTRIM(c.CreatedDateInText)), N'') AS CreatedDateInText,
        c.UpdatedDate, NULLIF(LTRIM(RTRIM(c.UpdatedDateInText)), N'') AS UpdatedDateInText,
        cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
        uu.NewUserId AS ModifiedById, COALESCE(uu.NewUserName, uh.FullName) AS ModifiedByName
    INTO #Contractor
    FROM [TMS].[TotalSystem].[dbo].[HRM_Guard_Contractor] c
    LEFT JOIN #UserMap cu ON cu.OldUserId = c.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = c.CreatedUserId
    LEFT JOIN #UserMap uu ON uu.OldUserId = c.UpdatedUserId
    LEFT JOIN #HtsUser uh ON uh.OldUserId = c.UpdatedUserId;

    SET @n1 = (SELECT COUNT(*) FROM #Contractor);
    PRINT N'  source rows: ' + CAST(@n1 AS NVARCHAR(20));

    UPDATE t SET
        t.Name = s.Name, t.Family = s.Family, t.PhoneNumber = s.PhoneNumber, t.Address = s.Address, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.ModifiedById, t.ModifiedByName = s.ModifiedByName,
        t.ModifiedDateMiladiDateTime = s.UpdatedDate, t.ModifiedDateShamsiDateTime = s.UpdatedDateInText,
        t.IsActive = 1
    FROM Hrm.GuardContractor t
    INNER JOIN #Contractor s ON s.HtsId = t.HtsId;
    PRINT N'  updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Hrm.GuardContractor
        (HtsId, Name, Family, PhoneNumber, Address, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.Name, s.Family, s.PhoneNumber, s.Address, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.ModifiedById, s.ModifiedByName, s.UpdatedDate, s.UpdatedDateInText, 1
    FROM #Contractor s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.GuardContractor t WHERE t.HtsId = s.HtsId);
    PRINT N'  inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ───────── مکان ───────── */
    PRINT N'=== GuardLocation ===';
    IF OBJECT_ID('tempdb..#Location') IS NOT NULL DROP TABLE #Location;
    SELECT
        CAST(l.Id AS BIGINT) AS HtsId,
        l.Title, l.Comment,
        l.CreatedDate, NULLIF(LTRIM(RTRIM(l.CreatedDateInText)), N'') AS CreatedDateInText,
        cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName
    INTO #Location
    FROM [TMS].[TotalSystem].[dbo].[HRM_Guard_Location] l
    LEFT JOIN #UserMap cu ON cu.OldUserId = l.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = l.CreatedUserId;

    SET @n1 = (SELECT COUNT(*) FROM #Location);
    PRINT N'  source rows: ' + CAST(@n1 AS NVARCHAR(20));

    UPDATE t SET
        t.Title = s.Title, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.CreatedById, t.ModifiedByName = s.CreatedByName,
        t.ModifiedDateMiladiDateTime = s.CreatedDate, t.ModifiedDateShamsiDateTime = s.CreatedDateInText,
        t.IsActive = 1
    FROM Hrm.GuardLocation t
    INNER JOIN #Location s ON s.HtsId = t.HtsId;
    PRINT N'  updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Hrm.GuardLocation
        (HtsId, Title, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.Title, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText, 1
    FROM #Location s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.GuardLocation t WHERE t.HtsId = s.HtsId);
    PRINT N'  inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ───────── خلاصه ───────── */
    SELECT N'GuardPart' AS T, (SELECT COUNT(*) FROM #Part) AS HtsRows, (SELECT COUNT(*) FROM Hrm.GuardPart WHERE HtsId <> 0) AS HavayarRows,
           (SELECT COUNT(*) FROM #Part WHERE CreatedById IS NULL) AS CreatedByUnmapped
    UNION ALL SELECT N'GuardDriver', (SELECT COUNT(*) FROM #Driver), (SELECT COUNT(*) FROM Hrm.GuardDriver WHERE HtsId <> 0), (SELECT COUNT(*) FROM #Driver WHERE CreatedById IS NULL)
    UNION ALL SELECT N'GuardContractor', (SELECT COUNT(*) FROM #Contractor), (SELECT COUNT(*) FROM Hrm.GuardContractor WHERE HtsId <> 0), (SELECT COUNT(*) FROM #Contractor WHERE CreatedById IS NULL)
    UNION ALL SELECT N'GuardLocation', (SELECT COUNT(*) FROM #Location), (SELECT COUNT(*) FROM Hrm.GuardLocation WHERE HtsId <> 0), (SELECT COUNT(*) FROM #Location WHERE CreatedById IS NULL);

    SELECT DISTINCT x.CreatedByName AS UnmappedCreatorName
    FROM (
        SELECT CreatedById, CreatedByName FROM #Part
        UNION SELECT CreatedById, CreatedByName FROM #Driver
        UNION SELECT CreatedById, CreatedByName FROM #Contractor
        UNION SELECT CreatedById, CreatedByName FROM #Location
    ) x
    WHERE x.CreatedById IS NULL;

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'DRY RUN — هیچ تغییری اعمال نشد. برای اجرای واقعی @DryRun = 0 بگذارید.';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'=== DONE Sync_GuardBasicInfo_FromTotalSystem ===';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
