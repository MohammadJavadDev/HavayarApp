/*
================================================================================
SyncLawFromTotalSystem.sql  —  سیستم حقوقی: HTS (TotalSystem) → HavayarApp (schema Law)
================================================================================
جدول‌ها: Law_JudicialFile، _Demand، _Parties، _Result، _Costs، _RelatedWork، _ReviewAuthority، _ImportantDates، _Attachment
کلید تکرارپذیری: HtsId (MERGE). هیچ ردیفی حذف نمی‌شود. اکشن‌های موجودیت (کد پرونده، پیامک) اجرا نمی‌شوند.
نگاشت کاربر : Gnr_User.ActiveDirectoryUsername / Username ↔ system.User.Username
نگاشت واحد  : HRM_OrgUnit.Hamkaran_Unit_FK ↔ Hrm.OrgUnit.HamkaranUnitId؛ اگر شناسه همکاران نداشت، تطبیق با عنوان
نگاشت پرسنل : HRM_Personel.Hamkaran_Personel_FK ↔ Hcm.Personel.Code (LegalExpertId؛ در HTS همیشه خالی است)
تاریخ میلادی: از متن شمسی با dbo.fn_ShamsiToMiladiDate (ReferralDate در HTS در همه ردیف‌ها NULL است)
فایل پیوست : فقط HtsFilePath (مسیر UNC) ثبت می‌شود؛ آپلود با جاب JudicialFileAttachmentImportJob.
پیش‌نیاز   : Linked Server [TMS]؛ migration AddLawJudicialFile.
اجرا       : sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\SyncLawFromTotalSystem.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('tempdb..#Gap') IS NOT NULL DROP TABLE #Gap;
CREATE TABLE #Gap (Kind NVARCHAR(80), HtsId BIGINT, Detail NVARCHAR(400));

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'sync-law-judicialfile';

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, NewUserId, NewName
    INTO #UserMap
    FROM (
        SELECT ou.User_ID AS OldUserId, nu.Id AS NewUserId,
               COALESCE(NULLIF(nu.NameFa, N''), nu.Name) AS NewName,
               ROW_NUMBER() OVER (PARTITION BY ou.User_ID ORDER BY nu.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN [system].[User] nu
            ON LOWER(nu.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N''), LTRIM(RTRIM(ou.Username))))
            OR LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
    ) x WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    IF OBJECT_ID('tempdb..#OrgMap') IS NOT NULL DROP TABLE #OrgMap;
    SELECT hrm.OrgUnit_ID AS OldOrgUnitId,
           COALESCE(
               (SELECT MIN(o.Id) FROM Hrm.OrgUnit o WHERE ISNULL(hrm.Hamkaran_Unit_FK, 0) <> 0 AND o.HamkaranUnitId = CAST(hrm.Hamkaran_Unit_FK AS bigint)),
               (SELECT MIN(o.Id) FROM Hrm.OrgUnit o WHERE LTRIM(RTRIM(o.Title)) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(hrm.OrgUnit_Title)) COLLATE DATABASE_DEFAULT)
           ) AS OrgUnitId
    INTO #OrgMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hrm;
    CREATE UNIQUE CLUSTERED INDEX IX_OrgMap ON #OrgMap (OldOrgUnitId);

    IF OBJECT_ID('tempdb..#PersonMap') IS NOT NULL DROP TABLE #PersonMap;
    SELECT hrm.Personel_ID AS OldPersonelId, MIN(p.Id) AS PersonelId
    INTO #PersonMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hrm
    INNER JOIN Hcm.Personel p ON TRY_CONVERT(int, p.Code) = hrm.Hamkaran_Personel_FK
    WHERE ISNULL(hrm.Hamkaran_Personel_FK, 0) <> 0
    GROUP BY hrm.Personel_ID;
    CREATE UNIQUE CLUSTERED INDEX IX_PersonMap ON #PersonMap (OldPersonelId);

    /* ------------------------------------------------------------- Law.JudicialFile */
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        h.Id AS HtsId, h.JudicialFileCode,
        h.JudicialFileNatureId AS Nature, h.Subject, h.TypeId AS [Type],
        h.JudicialReferenceFileNumber, h.FileArchiveClass,
        org.OrgUnitId AS ReferralUnitId, h.ReferralUnitId AS OldOrgUnitId,
        NULLIF(LTRIM(RTRIM(h.ReferralDateInText)), N'') AS ReferralDateShamsiDate,
        COALESCE(CAST(h.ReferralDate AS date),
                 TRY_CAST(dbo.fn_ShamsiToMiladiDate(NULLIF(LTRIM(RTRIM(h.ReferralDateInText)), N''), N'/') AS date)) AS ReferralDateMiladiDate,
        pm.PersonelId AS LegalExpertId, h.LegalExpertId AS OldLegalExpertId,
        h.TrackingNumber, h.Lawyer, h.IsClosed,
        NULLIF(LTRIM(RTRIM(h.RegisterationDateInText)), N'') AS RegistrationDateShamsiDate,
        TRY_CAST(dbo.fn_ShamsiToMiladiDate(NULLIF(LTRIM(RTRIM(h.RegisterationDateInText)), N''), N'/') AS date) AS RegistrationDateMiladiDate,
        h.CurrentStatus,
        cu.NewUserId AS CreatedById, COALESCE(cu.NewName, ou.FullName, @SeedUser) AS CreatedByName, h.CreatedUserId AS OldUserId,
        h.CreatedDateTime AS CreatedOnMiladiDateTime, h.CreatedDateTimeInText AS CreatedOnShamsiDateTime
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFile] h
    LEFT JOIN #OrgMap org ON org.OldOrgUnitId = h.ReferralUnitId
    LEFT JOIN #PersonMap pm ON pm.OldPersonelId = h.LegalExpertId
    LEFT JOIN #UserMap cu ON cu.OldUserId = h.CreatedUserId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = h.CreatedUserId;

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'OrgUnit', HtsId, N'HTS ReferralUnitId ' + CAST(OldOrgUnitId AS nvarchar(20)) FROM #Src WHERE OldOrgUnitId IS NOT NULL AND ReferralUnitId IS NULL
    UNION ALL SELECT N'User', HtsId, N'HTS CreatedUserId ' + CAST(OldUserId AS nvarchar(20)) FROM #Src WHERE CreatedById IS NULL
    UNION ALL SELECT N'LegalExpert', HtsId, N'HTS LegalExpertId ' + CAST(OldLegalExpertId AS nvarchar(20)) FROM #Src WHERE OldLegalExpertId IS NOT NULL AND LegalExpertId IS NULL
    UNION ALL SELECT N'ReferralDate', HtsId, ReferralDateShamsiDate FROM #Src WHERE ReferralDateShamsiDate IS NOT NULL AND ReferralDateMiladiDate IS NULL
    UNION ALL SELECT N'RegistrationDate', HtsId, RegistrationDateShamsiDate FROM #Src WHERE RegistrationDateShamsiDate IS NOT NULL AND RegistrationDateMiladiDate IS NULL;

    MERGE Law.JudicialFile AS t
    USING #Src AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        Nature = s.Nature, Subject = s.Subject, [Type] = s.[Type],
        JudicialReferenceFileNumber = s.JudicialReferenceFileNumber, FileArchiveClass = s.FileArchiveClass,
        ReferralUnitId = s.ReferralUnitId,
        ReferralDateShamsiDate = s.ReferralDateShamsiDate, ReferralDateMiladiDate = s.ReferralDateMiladiDate,
        LegalExpertId = s.LegalExpertId, TrackingNumber = s.TrackingNumber, Lawyer = s.Lawyer, IsClosed = s.IsClosed,
        RegistrationDateShamsiDate = s.RegistrationDateShamsiDate, RegistrationDateMiladiDate = s.RegistrationDateMiladiDate,
        CurrentStatus = s.CurrentStatus,
        IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, JudicialFileCode, Nature, Subject, [Type], JudicialReferenceFileNumber, FileArchiveClass, ReferralUnitId,
        ReferralDateShamsiDate, ReferralDateMiladiDate, LegalExpertId, TrackingNumber, Lawyer, IsClosed,
        RegistrationDateShamsiDate, RegistrationDateMiladiDate, CurrentStatus,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.JudicialFileCode, s.Nature, s.Subject, s.[Type], s.JudicialReferenceFileNumber, s.FileArchiveClass, s.ReferralUnitId,
        s.ReferralDateShamsiDate, s.ReferralDateMiladiDate, s.LegalExpertId, s.TrackingNumber, s.Lawyer, s.IsClosed,
        s.RegistrationDateShamsiDate, s.RegistrationDateMiladiDate, s.CurrentStatus,
        s.CreatedById, s.CreatedByName, 1, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, @Now, @NowShamsi, 1);

    IF OBJECT_ID('tempdb..#FileMap') IS NOT NULL DROP TABLE #FileMap;
    SELECT HtsId, Id INTO #FileMap FROM Law.JudicialFile WHERE HtsId <> 0;
    CREATE UNIQUE CLUSTERED INDEX IX_FileMap ON #FileMap (HtsId);

    /* ------------------------------------------------------------- Law.JudicialFileDemand ← Law_JudicialFileDemand */
    MERGE Law.JudicialFileDemand AS t
    USING (
        SELECT src.Id AS HtsId, fm.Id AS JudicialFileId,
               src.DemandTypeId AS DemandType,
               src.DemandValue AS DemandValue,
               src.Comment AS Comment,
               cu.NewUserId AS CreatedById, COALESCE(cu.NewName, ou.FullName, @SeedUser) AS CreatedByName,
               src.CreatedDateTime AS CreatedOnMiladiDateTime, src.CreatedDateTimeInText AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileDemand] src
        INNER JOIN #FileMap fm ON fm.HtsId = src.JudicialFileId
        LEFT JOIN #UserMap cu ON cu.OldUserId = src.CreatedUserId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = src.CreatedUserId
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        JudicialFileId = s.JudicialFileId,
        DemandType = s.DemandType,
        DemandValue = s.DemandValue,
        Comment = s.Comment,
        IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, JudicialFileId, DemandType, DemandValue, Comment,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.JudicialFileId, s.DemandType, s.DemandValue, s.Comment,
        s.CreatedById, s.CreatedByName, 1, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, @Now, @NowShamsi, 1);

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'JudicialFileDemand: parent file missing', src.Id, N'HTS JudicialFileId ' + CAST(src.JudicialFileId AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileDemand] src
    WHERE NOT EXISTS (SELECT 1 FROM #FileMap fm WHERE fm.HtsId = src.JudicialFileId);

    /* ------------------------------------------------------------- Law.JudicialFileParty ← Law_JudicialFileParties */
    MERGE Law.JudicialFileParty AS t
    USING (
        SELECT src.Id AS HtsId, fm.Id AS JudicialFileId,
               src.TypeId AS Type,
               src.Fullname AS Fullname,
               cu.NewUserId AS CreatedById, COALESCE(cu.NewName, ou.FullName, @SeedUser) AS CreatedByName,
               src.CreatedDateTime AS CreatedOnMiladiDateTime, src.CreatedDateTimeInText AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileParties] src
        INNER JOIN #FileMap fm ON fm.HtsId = src.JudicialFileId
        LEFT JOIN #UserMap cu ON cu.OldUserId = src.CreatedUserId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = src.CreatedUserId
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        JudicialFileId = s.JudicialFileId,
        Type = s.Type,
        Fullname = s.Fullname,
        IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, JudicialFileId, Type, Fullname,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.JudicialFileId, s.Type, s.Fullname,
        s.CreatedById, s.CreatedByName, 1, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, @Now, @NowShamsi, 1);

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'JudicialFileParty: parent file missing', src.Id, N'HTS JudicialFileId ' + CAST(src.JudicialFileId AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileParties] src
    WHERE NOT EXISTS (SELECT 1 FROM #FileMap fm WHERE fm.HtsId = src.JudicialFileId);

    /* ------------------------------------------------------------- Law.JudicialFileResult ← Law_JudicialFileResult */
    MERGE Law.JudicialFileResult AS t
    USING (
        SELECT src.Id AS HtsId, fm.Id AS JudicialFileId,
               src.ResultId AS Result,
               src.Comment AS Comment,
               cu.NewUserId AS CreatedById, COALESCE(cu.NewName, ou.FullName, @SeedUser) AS CreatedByName,
               src.CreatedDateTime AS CreatedOnMiladiDateTime, src.CreatedDateTimeInText AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileResult] src
        INNER JOIN #FileMap fm ON fm.HtsId = src.JudicialFileId
        LEFT JOIN #UserMap cu ON cu.OldUserId = src.CreatedUserId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = src.CreatedUserId
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        JudicialFileId = s.JudicialFileId,
        Result = s.Result,
        Comment = s.Comment,
        IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, JudicialFileId, Result, Comment,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.JudicialFileId, s.Result, s.Comment,
        s.CreatedById, s.CreatedByName, 1, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, @Now, @NowShamsi, 1);

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'JudicialFileResult: parent file missing', src.Id, N'HTS JudicialFileId ' + CAST(src.JudicialFileId AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileResult] src
    WHERE NOT EXISTS (SELECT 1 FROM #FileMap fm WHERE fm.HtsId = src.JudicialFileId);

    /* ------------------------------------------------------------- Law.JudicialFileCost ← Law_JudicialFileCosts */
    MERGE Law.JudicialFileCost AS t
    USING (
        SELECT src.Id AS HtsId, fm.Id AS JudicialFileId,
               src.TypeId AS Type,
               src.Value AS Value,
               src.Comment AS Comment,
               cu.NewUserId AS CreatedById, COALESCE(cu.NewName, ou.FullName, @SeedUser) AS CreatedByName,
               src.CreatedDateTime AS CreatedOnMiladiDateTime, src.CreatedDateTimeInText AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileCosts] src
        INNER JOIN #FileMap fm ON fm.HtsId = src.JudicialFileId
        LEFT JOIN #UserMap cu ON cu.OldUserId = src.CreatedUserId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = src.CreatedUserId
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        JudicialFileId = s.JudicialFileId,
        Type = s.Type,
        Value = s.Value,
        Comment = s.Comment,
        IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, JudicialFileId, Type, Value, Comment,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.JudicialFileId, s.Type, s.Value, s.Comment,
        s.CreatedById, s.CreatedByName, 1, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, @Now, @NowShamsi, 1);

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'JudicialFileCost: parent file missing', src.Id, N'HTS JudicialFileId ' + CAST(src.JudicialFileId AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileCosts] src
    WHERE NOT EXISTS (SELECT 1 FROM #FileMap fm WHERE fm.HtsId = src.JudicialFileId);

    /* ------------------------------------------------------------- Law.JudicialFileRelatedWork ← Law_JudicialFileRelatedWork */
    MERGE Law.JudicialFileRelatedWork AS t
    USING (
        SELECT src.Id AS HtsId, fm.Id AS JudicialFileId,
               src.Text AS Text,
               NULLIF(LTRIM(RTRIM(src.DateInText)), N'') AS DateShamsiDate,
               COALESCE(CAST(src.Date AS date), TRY_CAST(dbo.fn_ShamsiToMiladiDate(NULLIF(LTRIM(RTRIM(src.DateInText)), N''), N'/') AS date)) AS DateMiladiDate,
               cu.NewUserId AS CreatedById, COALESCE(cu.NewName, ou.FullName, @SeedUser) AS CreatedByName,
               src.CreatedDateTime AS CreatedOnMiladiDateTime, src.CreatedDateTimeInText AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileRelatedWork] src
        INNER JOIN #FileMap fm ON fm.HtsId = src.JudicialFileId
        LEFT JOIN #UserMap cu ON cu.OldUserId = src.CreatedUserId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = src.CreatedUserId
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        JudicialFileId = s.JudicialFileId,
        Text = s.Text,
        DateShamsiDate = s.DateShamsiDate,
        DateMiladiDate = s.DateMiladiDate,
        IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, JudicialFileId, Text, DateShamsiDate, DateMiladiDate,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.JudicialFileId, s.Text, s.DateShamsiDate, s.DateMiladiDate,
        s.CreatedById, s.CreatedByName, 1, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, @Now, @NowShamsi, 1);

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'JudicialFileRelatedWork: parent file missing', src.Id, N'HTS JudicialFileId ' + CAST(src.JudicialFileId AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileRelatedWork] src
    WHERE NOT EXISTS (SELECT 1 FROM #FileMap fm WHERE fm.HtsId = src.JudicialFileId);

    /* ------------------------------------------------------------- Law.JudicialReviewAuthority ← Law_JudicialReviewAuthority */
    MERGE Law.JudicialReviewAuthority AS t
    USING (
        SELECT src.Id AS HtsId, fm.Id AS JudicialFileId,
               src.Title AS Title,
               src.FileAttachment AS FileAttachment,
               src.Comment AS Comment,
               cu.NewUserId AS CreatedById, COALESCE(cu.NewName, ou.FullName, @SeedUser) AS CreatedByName,
               src.CreatedDateTime AS CreatedOnMiladiDateTime, src.CreatedDateTimeInText AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Law_JudicialReviewAuthority] src
        INNER JOIN #FileMap fm ON fm.HtsId = src.JudicialFileId
        LEFT JOIN #UserMap cu ON cu.OldUserId = src.CreatedUserId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = src.CreatedUserId
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        JudicialFileId = s.JudicialFileId,
        Title = s.Title,
        FileAttachment = s.FileAttachment,
        Comment = s.Comment,
        IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, JudicialFileId, Title, FileAttachment, Comment,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.JudicialFileId, s.Title, s.FileAttachment, s.Comment,
        s.CreatedById, s.CreatedByName, 1, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, @Now, @NowShamsi, 1);

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'JudicialReviewAuthority: parent file missing', src.Id, N'HTS JudicialFileId ' + CAST(src.JudicialFileId AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Law_JudicialReviewAuthority] src
    WHERE NOT EXISTS (SELECT 1 FROM #FileMap fm WHERE fm.HtsId = src.JudicialFileId);

    /* ------------------------------------------------------------- Law.JudicialImportantDate ← Law_JudicialImportantDates */
    MERGE Law.JudicialImportantDate AS t
    USING (
        SELECT src.Id AS HtsId, fm.Id AS JudicialFileId,
               src.Title AS Title,
               NULLIF(LTRIM(RTRIM(src.DateInText)), N'') AS DateShamsiDate,
               COALESCE(CAST(src.Date AS date), TRY_CAST(dbo.fn_ShamsiToMiladiDate(NULLIF(LTRIM(RTRIM(src.DateInText)), N''), N'/') AS date)) AS DateMiladiDate,
               src.IsSend AS IsSend,
               src.Recipients AS Recipients,
               cu.NewUserId AS CreatedById, COALESCE(cu.NewName, ou.FullName, @SeedUser) AS CreatedByName,
               src.CreatedDate AS CreatedOnMiladiDateTime, src.CreatedDateInText AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Law_JudicialImportantDates] src
        INNER JOIN #FileMap fm ON fm.HtsId = src.JudicialFileId
        LEFT JOIN #UserMap cu ON cu.OldUserId = src.CreatedUserId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = src.CreatedUserId
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        JudicialFileId = s.JudicialFileId,
        Title = s.Title,
        DateShamsiDate = s.DateShamsiDate,
        DateMiladiDate = s.DateMiladiDate,
        IsSend = s.IsSend,
        Recipients = s.Recipients,
        IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, JudicialFileId, Title, DateShamsiDate, DateMiladiDate, IsSend, Recipients,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.JudicialFileId, s.Title, s.DateShamsiDate, s.DateMiladiDate, s.IsSend, s.Recipients,
        s.CreatedById, s.CreatedByName, 1, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, @Now, @NowShamsi, 1);

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'JudicialImportantDate: parent file missing', src.Id, N'HTS JudicialFileId ' + CAST(src.JudicialFileId AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Law_JudicialImportantDates] src
    WHERE NOT EXISTS (SELECT 1 FROM #FileMap fm WHERE fm.HtsId = src.JudicialFileId);

    /* ------------------------------------------------------------- Law.JudicialFileAttachment ← Law_JudicialFileAttachment */
    MERGE Law.JudicialFileAttachment AS t
    USING (
        SELECT src.Id AS HtsId, fm.Id AS JudicialFileId,
               src.AttachmentFilePath AS HtsFilePath,
               src.Comment AS Comment,
               cu.NewUserId AS CreatedById, COALESCE(cu.NewName, ou.FullName, @SeedUser) AS CreatedByName,
               src.CreatedDateTime AS CreatedOnMiladiDateTime, src.CreatedDateTimeInText AS CreatedOnShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileAttachment] src
        INNER JOIN #FileMap fm ON fm.HtsId = src.JudicialFileId
        LEFT JOIN #UserMap cu ON cu.OldUserId = src.CreatedUserId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = src.CreatedUserId
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        JudicialFileId = s.JudicialFileId,
        HtsFilePath = s.HtsFilePath,
        Comment = s.Comment,
        IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, JudicialFileId, HtsFilePath, Comment,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.JudicialFileId, s.HtsFilePath, s.Comment,
        s.CreatedById, s.CreatedByName, 1, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, @Now, @NowShamsi, 1);

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'JudicialFileAttachment: parent file missing', src.Id, N'HTS JudicialFileId ' + CAST(src.JudicialFileId AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileAttachment] src
    WHERE NOT EXISTS (SELECT 1 FROM #FileMap fm WHERE fm.HtsId = src.JudicialFileId);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

/* ------------------------------------------------------------- گزارش مغایرت‌ها و شمارش */
SELECT Kind, COUNT(*) AS Cnt FROM #Gap GROUP BY Kind ORDER BY Kind;
SELECT TOP 50 Kind, HtsId, Detail FROM #Gap ORDER BY Kind, HtsId;

SELECT N'JudicialFile' AS TableName,
       (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFile]) AS Hts, (SELECT COUNT(*) FROM Law.JudicialFile WHERE HtsId <> 0) AS Havayar
UNION ALL SELECT N'Demand', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileDemand]), (SELECT COUNT(*) FROM Law.JudicialFileDemand WHERE HtsId <> 0)
UNION ALL SELECT N'Parties', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileParties]), (SELECT COUNT(*) FROM Law.JudicialFileParty WHERE HtsId <> 0)
UNION ALL SELECT N'Result', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileResult]), (SELECT COUNT(*) FROM Law.JudicialFileResult WHERE HtsId <> 0)
UNION ALL SELECT N'Costs', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileCosts]), (SELECT COUNT(*) FROM Law.JudicialFileCost WHERE HtsId <> 0)
UNION ALL SELECT N'RelatedWork', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileRelatedWork]), (SELECT COUNT(*) FROM Law.JudicialFileRelatedWork WHERE HtsId <> 0)
UNION ALL SELECT N'ReviewAuthority', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Law_JudicialReviewAuthority]), (SELECT COUNT(*) FROM Law.JudicialReviewAuthority WHERE HtsId <> 0)
UNION ALL SELECT N'ImportantDates', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Law_JudicialImportantDates]), (SELECT COUNT(*) FROM Law.JudicialImportantDate WHERE HtsId <> 0)
UNION ALL SELECT N'Attachment', (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Law_JudicialFileAttachment]), (SELECT COUNT(*) FROM Law.JudicialFileAttachment WHERE HtsId <> 0);
