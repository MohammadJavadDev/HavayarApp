/*
================================================================================
Sync_GuardPartIo_FromTotalSystem.sql  —  ورود و خروج کالا (حراست/دژبان) (HTS System 9، صفحات 426 و 431)
================================================================================
همگام‌سازی از [TMS].[TotalSystem].dbo:
  1) Gnr_PartEntryNotify    → Gnr.PartEntryNotify     (وابستگی: 47,340 رکورد PartIO به اعلان ورود وصل‌اند)
  2) HRM_Guard_PartIO       → Hrm.GuardPartIo
  3) HRM_Guard_PartIODetail → Hrm.GuardPartIoDetail   (مسیر فایل قدیمی در HtsAttachmentPath؛ انتقال فایل با جاب جدا)

قواعد:
  - کلید: HtsId = Id در HTS. تکرارپذیر (به‌روزرسانی ردیف‌های دارای HtsId + درج جدیدها). ردیف‌های ساخته‌شده در Havayar
    (HtsId = 0) دست‌نخورده می‌مانند. تا کات‌اور HTS منبع حقیقی است و اجرای مجدد، ویرایش‌های سمت Havayar
    روی ردیف‌های دارای HtsId را بازنویسی می‌کند.
  - همه ردیف‌ها منتقل می‌شوند.
  - کاربر: Gnr_User.Username یا ActiveDirectoryUsername ↔ system.[User].Username. بی‌تطبیق → شناسه خالی
    و نام کاربر HTS (FullName) در CreatedByName/ModifiedByName.
  - پرسنل: HRM_Personel.PrsCode ↔ Hcm.Personel.Code (شناسه همکاران HTS با شناسه EmployeeID رهکاران یکی نیست).
    بی‌تطبیق → PersonId خالی (گزارش می‌شود).
  - واحد مرتبط: HRM_OrgUnit.Hamkaran_Unit_FK ↔ Hrm.OrgUnit.HamkaranUnitId؛ در نبود شناسه همکاران، تطبیق با عنوان
    (فقط وقتی دقیقاً یک واحد با آن عنوان باشد). بی‌تطبیق → RelatedUnitId خالی (گزارش می‌شود).
  - درب نامعتبر (IODoorId = 0، ۲ ردیف) → درب بالا/اصلی (1437) که مقدار پیش‌فرض فرم HTS است. ۹ ردیف قلم بدون هدر در HTS منتقل نمی‌شود.
  - اعلام ورود: PartId با Inv.Part.HtsId، PartReceiverId با پرسنل (همان نگاشت). PartOwnerUnitId شناسه واحد HTS می‌ماند.
  - پیوست: فقط مسیر (HtsAttachmentPath). FileId را جاب انتقال فایل پر می‌کند و این اسکریپت آن را بازنویسی نمی‌کند.

اجرا:  @DryRun = 1 (پیش‌فرض) همه‌چیز را در تراکنش اجرا و ROLLBACK می‌کند (فقط گزارش)؛  @DryRun = 0 → COMMIT.
پیش‌نیاز: Linked Server [TMS]، migration AddGuardPartIo، Sync_GuardBasicInfo_FromTotalSystem.sql (کالا/راننده/پیمانکار/مکان).
UTF-8 BOM.  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Sync_GuardPartIo_FromTotalSystem.sql"
بعد از COMMIT، Sync_CngRepairs_FromTotalSystem.sql را دوباره اجرا کنید تا PartEntryNotifyId تعمیرات CNG وصل شود.
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

IF OBJECT_ID(N'Hrm.GuardPartIo') IS NULL OR OBJECT_ID(N'Gnr.PartEntryNotify') IS NULL
BEGIN
    RAISERROR(N'جدول‌های مقصد وجود ندارد. ابتدا migration AddGuardPartIo را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-guard-partio';
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

    /* ───────── نگاشت پرسنل ───────── */
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

    /* ───────── نگاشت واحد سازمانی ───────── */
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

    /* ───────── 1) اعلام ورود قطعه ───────── */
    PRINT N'=== PartEntryNotify ===';
    IF OBJECT_ID('tempdb..#Notify') IS NOT NULL DROP TABLE #Notify;
    SELECT
        CAST(p.Id AS BIGINT) AS HtsId,
        pr.Id AS PartId,
        p.Mount, p.PartTitle, CAST(p.PartCategoryId AS INT) AS PartCategoryId, CAST(p.PartGroupId AS INT) AS PartGroupId,
        p.SupplierTitle, CAST(p.PartOwnerUnitId AS INT) AS PartOwnerUnitId, p.PartOwner,
        CAST(p.ApproximateEntryDate AS datetime2) AS ApproximateEntryMiladiDate,
        NULLIF(LTRIM(RTRIM(p.ApproximateEntryDateInText)), N'') AS ApproximateEntryShamsiDate,
        CAST(p.EntryDate AS datetime2) AS EntryMiladiDate, NULLIF(LTRIM(RTRIM(p.EntryDateInText)), N'') AS EntryShamsiDate,
        p.TransportationVehicle, rc.NewPersonelId AS PartReceiverId, p.TemporaryReceiptNumber,
        CAST(p.IsReceived AS BIT) AS IsReceived, CAST(p.SaleTqId AS BIGINT) AS SaleTqId, CAST(p.LastStatusId AS INT) AS LastStatusId,
        p.Comment,
        p.CreatedDate, NULLIF(LTRIM(RTRIM(p.CreatedDateInText)), N'') AS CreatedDateInText,
        p.UpdatedDate, NULLIF(LTRIM(RTRIM(p.UpdatedDateInText)), N'') AS UpdatedDateInText,
        cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
        uu.NewUserId AS ModifiedById, COALESCE(uu.NewUserName, uh.FullName) AS ModifiedByName,
        CASE WHEN p.PartId IS NOT NULL AND pr.Id IS NULL THEN 1 ELSE 0 END AS UnmappedPart,
        CASE WHEN p.PartReceiverId IS NOT NULL AND rc.NewPersonelId IS NULL THEN 1 ELSE 0 END AS UnmappedReceiver
    INTO #Notify
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PartEntryNotify] p
    LEFT JOIN Inv.Part pr ON pr.HtsId = CAST(p.PartId AS BIGINT) AND ISNULL(pr.HtsId, 0) <> 0
    LEFT JOIN #PersonMap rc ON rc.OldPersonelId = p.PartReceiverId
    LEFT JOIN #UserMap cu ON cu.OldUserId = p.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = p.CreatedUserId
    LEFT JOIN #UserMap uu ON uu.OldUserId = p.UpdatedUserId
    LEFT JOIN #HtsUser uh ON uh.OldUserId = p.UpdatedUserId;

    SET @n1 = (SELECT COUNT(*) FROM #Notify);
    SET @n2 = (SELECT SUM(UnmappedPart + UnmappedReceiver) FROM #Notify);
    PRINT N'  source rows: ' + CAST(@n1 AS NVARCHAR(20)) + N' | unmapped part/receiver: ' + CAST(ISNULL(@n2, 0) AS NVARCHAR(20));

    UPDATE t SET
        t.PartId = s.PartId, t.Mount = s.Mount, t.PartTitle = s.PartTitle,
        t.PartCategoryId = s.PartCategoryId, t.PartGroupId = s.PartGroupId, t.SupplierTitle = s.SupplierTitle,
        t.PartOwnerUnitId = s.PartOwnerUnitId, t.PartOwner = s.PartOwner,
        t.ApproximateEntryMiladiDate = s.ApproximateEntryMiladiDate, t.ApproximateEntryShamsiDate = ISNULL(s.ApproximateEntryShamsiDate, N''),
        t.EntryMiladiDate = s.EntryMiladiDate, t.EntryShamsiDate = s.EntryShamsiDate,
        t.TransportationVehicle = s.TransportationVehicle, t.PartReceiverId = s.PartReceiverId,
        t.TemporaryReceiptNumber = s.TemporaryReceiptNumber, t.IsReceived = s.IsReceived,
        t.SaleTqId = s.SaleTqId, t.LastStatusId = s.LastStatusId, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.ModifiedById, t.ModifiedByName = s.ModifiedByName,
        t.ModifiedDateMiladiDateTime = s.UpdatedDate, t.ModifiedDateShamsiDateTime = s.UpdatedDateInText,
        t.IsActive = 1
    FROM Gnr.PartEntryNotify t
    INNER JOIN #Notify s ON s.HtsId = t.HtsId;
    PRINT N'  updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Gnr.PartEntryNotify
        (HtsId, PartId, Mount, PartTitle, PartCategoryId, PartGroupId, SupplierTitle, PartOwnerUnitId, PartOwner,
         ApproximateEntryMiladiDate, ApproximateEntryShamsiDate, EntryMiladiDate, EntryShamsiDate,
         TransportationVehicle, PartReceiverId, TemporaryReceiptNumber, IsReceived, SaleTqId, LastStatusId, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.PartId, s.Mount, s.PartTitle, s.PartCategoryId, s.PartGroupId, s.SupplierTitle, s.PartOwnerUnitId, s.PartOwner,
           s.ApproximateEntryMiladiDate, ISNULL(s.ApproximateEntryShamsiDate, N''), s.EntryMiladiDate, s.EntryShamsiDate,
           s.TransportationVehicle, s.PartReceiverId, s.TemporaryReceiptNumber, s.IsReceived, s.SaleTqId, s.LastStatusId, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.ModifiedById, s.ModifiedByName, s.UpdatedDate, s.UpdatedDateInText, 1
    FROM #Notify s
    WHERE NOT EXISTS (SELECT 1 FROM Gnr.PartEntryNotify t WHERE t.HtsId = s.HtsId);
    PRINT N'  inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ───────── 2) ورود و خروج کالا ───────── */
    PRINT N'=== GuardPartIo ===';
    IF OBJECT_ID('tempdb..#Io') IS NOT NULL DROP TABLE #Io;
    SELECT
        CAST(io.Id AS BIGINT) AS HtsId,
        CAST(io.IOTypeId AS INT) AS IoType,
        CASE WHEN io.IODoorId IN (1437, 1438) THEN CAST(io.IODoorId AS INT) ELSE 1437 END AS IoDoor,
        CAST(io.OperatorTypeId AS INT) AS OperatorType,
        pm.NewPersonelId AS PersonId,
        co.Id AS ContractorId,
        io.OperatorTitle,
        dr.Id AS DriverId,
        ls.Id AS IoSourceId,
        ld.Id AS IoDestinationId,
        nt.Id AS PartEntryNotifyId,
        CAST(io.IsConfirmed AS BIT) AS IsConfirmed,
        io.Comment,
        io.CreatedDate, NULLIF(LTRIM(RTRIM(io.CreatedDateInText)), N'') AS CreatedDateInText,
        io.UpdatedDate, NULLIF(LTRIM(RTRIM(io.UpdatedDateInText)), N'') AS UpdatedDateInText,
        cu.NewUserId AS CreatedById, COALESCE(cu.NewUserName, ch.FullName) AS CreatedByName,
        uu.NewUserId AS ModifiedById, COALESCE(uu.NewUserName, uh.FullName) AS ModifiedByName,
        CASE WHEN io.PersonId IS NOT NULL AND pm.NewPersonelId IS NULL THEN 1 ELSE 0 END AS UnmappedPerson,
        CASE WHEN io.PartEntryNotifyId IS NOT NULL AND nt.Id IS NULL THEN 1 ELSE 0 END AS UnmappedNotify,
        CASE WHEN io.IODoorId NOT IN (1437, 1438) THEN 1 ELSE 0 END AS InvalidDoor
    INTO #Io
    FROM [TMS].[TotalSystem].[dbo].[HRM_Guard_PartIO] io
    LEFT JOIN #PersonMap pm ON pm.OldPersonelId = io.PersonId
    LEFT JOIN Hrm.GuardContractor co ON co.HtsId = CAST(io.ContractorId AS BIGINT) AND ISNULL(co.HtsId, 0) <> 0
    LEFT JOIN Hrm.GuardDriver dr ON dr.HtsId = CAST(io.DriverId AS BIGINT) AND ISNULL(dr.HtsId, 0) <> 0
    LEFT JOIN Hrm.GuardLocation ls ON ls.HtsId = CAST(io.IOSourceId AS BIGINT) AND ISNULL(ls.HtsId, 0) <> 0
    LEFT JOIN Hrm.GuardLocation ld ON ld.HtsId = CAST(io.IODestinationId AS BIGINT) AND ISNULL(ld.HtsId, 0) <> 0
    LEFT JOIN Gnr.PartEntryNotify nt ON nt.HtsId = CAST(io.PartEntryNotifyId AS BIGINT) AND ISNULL(nt.HtsId, 0) <> 0
    LEFT JOIN #UserMap cu ON cu.OldUserId = io.CreatedUserId
    LEFT JOIN #HtsUser ch ON ch.OldUserId = io.CreatedUserId
    LEFT JOIN #UserMap uu ON uu.OldUserId = io.UpdatedUserId
    LEFT JOIN #HtsUser uh ON uh.OldUserId = io.UpdatedUserId;

    SET @n1 = (SELECT COUNT(*) FROM #Io);
    DECLARE @UnmappedPerson INT = (SELECT SUM(UnmappedPerson) FROM #Io);
    DECLARE @UnmappedNotify INT = (SELECT SUM(UnmappedNotify) FROM #Io);
    DECLARE @InvalidDoor INT = (SELECT SUM(InvalidDoor) FROM #Io);
    PRINT N'  source rows: ' + CAST(@n1 AS NVARCHAR(20))
        + N' | unmapped person: ' + CAST(@UnmappedPerson AS NVARCHAR(20))
        + N' | unmapped notify: ' + CAST(@UnmappedNotify AS NVARCHAR(20))
        + N' | invalid door -> 1437: ' + CAST(@InvalidDoor AS NVARCHAR(20));

    UPDATE t SET
        t.IoType = s.IoType, t.IoDoor = s.IoDoor, t.OperatorType = s.OperatorType,
        t.PersonId = s.PersonId, t.ContractorId = s.ContractorId, t.OperatorTitle = s.OperatorTitle,
        t.DriverId = s.DriverId, t.IoSourceId = s.IoSourceId, t.IoDestinationId = s.IoDestinationId,
        t.PartEntryNotifyId = s.PartEntryNotifyId, t.IsConfirmed = s.IsConfirmed, t.Comment = s.Comment,
        t.CreatedById = s.CreatedById, t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedDate, t.CreatedOnShamsiDateTime = s.CreatedDateInText,
        t.ModifiedById = s.ModifiedById, t.ModifiedByName = s.ModifiedByName,
        t.ModifiedDateMiladiDateTime = s.UpdatedDate, t.ModifiedDateShamsiDateTime = s.UpdatedDateInText,
        t.IsActive = 1
    FROM Hrm.GuardPartIo t
    INNER JOIN #Io s ON s.HtsId = t.HtsId;
    PRINT N'  updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Hrm.GuardPartIo
        (HtsId, IoType, IoDoor, OperatorType, PersonId, ContractorId, OperatorTitle, DriverId,
         IoSourceId, IoDestinationId, PartEntryNotifyId, IsConfirmed, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.IoType, s.IoDoor, s.OperatorType, s.PersonId, s.ContractorId, s.OperatorTitle, s.DriverId,
           s.IoSourceId, s.IoDestinationId, s.PartEntryNotifyId, s.IsConfirmed, s.Comment,
           s.CreatedById, s.CreatedByName, s.CreatedDate, s.CreatedDateInText,
           s.ModifiedById, s.ModifiedByName, s.UpdatedDate, s.UpdatedDateInText, 1
    FROM #Io s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.GuardPartIo t WHERE t.HtsId = s.HtsId);
    PRINT N'  inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ───────── 3) اقلام ورود و خروج کالا ───────── */
    PRINT N'=== GuardPartIoDetail ===';
    IF OBJECT_ID('tempdb..#Detail') IS NOT NULL DROP TABLE #Detail;
    SELECT
        CAST(d.Id AS BIGINT) AS HtsId,
        io.Id AS PartIoId,
        pt.Id AS PartId,
        d.Mount,
        CAST(d.IOReasonId AS INT) AS IoReason,
        d.PaperNumber,
        um.NewOrgUnitId AS RelatedUnitId,
        NULLIF(LTRIM(RTRIM(d.AttachmentFilePath)), N'') AS HtsAttachmentPath,
        d.Comment,
        CASE WHEN d.RelatedUnitId IS NOT NULL AND um.NewOrgUnitId IS NULL THEN 1 ELSE 0 END AS UnmappedUnit
    INTO #Detail
    FROM [TMS].[TotalSystem].[dbo].[HRM_Guard_PartIODetail] d
    INNER JOIN Hrm.GuardPartIo io ON io.HtsId = CAST(d.PartIOId AS BIGINT) AND ISNULL(io.HtsId, 0) <> 0
    INNER JOIN Hrm.GuardPart pt ON pt.HtsId = CAST(d.PartId AS BIGINT) AND ISNULL(pt.HtsId, 0) <> 0
    LEFT JOIN #UnitMap um ON um.OldOrgUnitId = d.RelatedUnitId;

    SET @n1 = (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[HRM_Guard_PartIODetail]);
    SET @n2 = (SELECT COUNT(*) FROM #Detail);
    DECLARE @UnmappedUnit INT = (SELECT SUM(UnmappedUnit) FROM #Detail);
    PRINT N'  source rows: ' + CAST(@n1 AS NVARCHAR(20)) + N' | mapped: ' + CAST(@n2 AS NVARCHAR(20))
        + N' | skipped (no header/part): ' + CAST(@n1 - @n2 AS NVARCHAR(20))
        + N' | unmapped unit -> NULL: ' + CAST(@UnmappedUnit AS NVARCHAR(20));

    UPDATE t SET
        t.PartIoId = s.PartIoId, t.PartId = s.PartId, t.Mount = s.Mount, t.IoReason = s.IoReason,
        t.PaperNumber = s.PaperNumber, t.RelatedUnitId = s.RelatedUnitId,
        t.HtsAttachmentPath = s.HtsAttachmentPath, t.Comment = s.Comment, t.IsActive = 1
    FROM Hrm.GuardPartIoDetail t
    INNER JOIN #Detail s ON s.HtsId = t.HtsId;
    PRINT N'  updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Hrm.GuardPartIoDetail
        (HtsId, PartIoId, PartId, Mount, IoReason, PaperNumber, RelatedUnitId, HtsAttachmentPath, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT s.HtsId, s.PartIoId, s.PartId, s.Mount, s.IoReason, s.PaperNumber, s.RelatedUnitId, s.HtsAttachmentPath, s.Comment,
           1, @Seed, @Now, @NowShamsi, 1, @Seed, @Now, @NowShamsi, 1
    FROM #Detail s
    WHERE NOT EXISTS (SELECT 1 FROM Hrm.GuardPartIoDetail t WHERE t.HtsId = s.HtsId);
    PRINT N'  inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'=== DRY RUN — ROLLBACK شد. برای اعمال واقعی @DryRun = 0 کنید. ===';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'=== DONE Sync_GuardPartIo_FromTotalSystem — COMMIT شد ===';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

-- ---------------------------------------------------------------- verify (بعد از COMMIT معنی‌دار است)
SELECT 'PartEntryNotify' t, COUNT(*) n, SUM(CASE WHEN HtsId <> 0 THEN 1 ELSE 0 END) fromHts FROM Gnr.PartEntryNotify
UNION ALL SELECT 'GuardPartIo', COUNT(*), SUM(CASE WHEN HtsId <> 0 THEN 1 ELSE 0 END) FROM Hrm.GuardPartIo
UNION ALL SELECT 'GuardPartIoDetail', COUNT(*), SUM(CASE WHEN HtsId <> 0 THEN 1 ELSE 0 END) FROM Hrm.GuardPartIoDetail;
