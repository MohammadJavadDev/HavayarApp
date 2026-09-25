/*
Sync_CngMaintenanceContract_FromTotalSystem.sql
همگام‌سازی Cng.MaintenanceContract از [TMS].[TotalSystem].[dbo].[Cng_MaintenanceContract].
HtsId = CAST(old.Id AS BIGINT).
Agency / StationBeneficiary via SLS.Customer.HtsId؛ Station via Cng.StationInfo.HtsId.
کاربران Created/Modified از #UserMap.
ردیف به‌خاطر FK اختیاری پیدا نشده حذف نمی‌شود (Station/Beneficiary → NULL).
Agency الزامی است؛ بدون نگاشت نمایندگی ردیف درج نمی‌شود و گزارش می‌شود.
Idempotent MERGE/UPDATE روی HtsId. UTF-8 BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Cng.MaintenanceContract', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Cng.MaintenanceContract وجود ندارد. ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-cng-maintenancecontract';

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
                    nu.Id
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN system.[User] nu
            ON LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
            OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
            OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    PRINT N'=== Cng.MaintenanceContract ===';
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        agency.Id AS AgencyId,
        station.Id AS StationId,
        beneficiary.Id AS StationBeneficiaryId,
        CAST(s.StartDate AS date) AS StartDate,
        LEFT(NULLIF(LTRIM(RTRIM(s.StartDateInText)), N''), 10) AS StartDateInText,
        CAST(s.EndDate AS date) AS EndDate,
        LEFT(NULLIF(LTRIM(RTRIM(s.EndDateInText)), N''), 10) AS EndDateInText,
        CAST(s.VisitCountInMonth AS tinyint) AS VisitCountInMonth,
        CAST(s.ContractDurationInMonth AS tinyint) AS ContractDurationInMonth,
        CAST(s.MonthlyPrice AS bigint) AS MonthlyPrice,
        CAST(s.TotalPrice AS bigint) AS TotalPrice,
        CAST(s.StatusId AS int) AS Status,
        CAST(s.IsCanceled AS bit) AS IsCanceled,
        LEFT(NULLIF(LTRIM(RTRIM(s.CancelDescription)), N''), 255) AS CancelDescription,
        LEFT(NULLIF(LTRIM(RTRIM(s.Comment)), N''), 2048) AS Comment,
        umCreate.NewUserId AS CreatedById,
        umCreate.NewUserName AS CreatedByName,
        CAST(s.CreatedDate AS datetime2) AS CreatedOnMiladiDateTime,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N''), @NowShamsi), 30) AS CreatedOnShamsiDateTime,
        umUpdate.NewUserId AS ModifiedById,
        umUpdate.NewUserName AS ModifiedByName,
        CAST(s.UpdatedDate AS datetime2) AS ModifiedDateMiladiDateTime,
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(s.UpdatedDateInText)), N''), @NowShamsi), 30) AS ModifiedDateShamsiDateTime,
        CASE WHEN agency.Id IS NULL THEN 1 ELSE 0 END AS UnmappedAgency,
        CASE WHEN s.StationId IS NOT NULL AND station.Id IS NULL THEN 1 ELSE 0 END AS UnmappedStation,
        CASE WHEN s.StationBeneficiaryId IS NOT NULL AND beneficiary.Id IS NULL THEN 1 ELSE 0 END AS UnmappedBeneficiary,
        CASE WHEN umCreate.NewUserId IS NULL THEN 1 ELSE 0 END AS UnmappedCreatedUser,
        CASE WHEN umUpdate.NewUserId IS NULL THEN 1 ELSE 0 END AS UnmappedUpdatedUser
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Cng_MaintenanceContract] s
    LEFT JOIN SLS.Customer agency ON agency.HtsId = CAST(s.AgencyId AS BIGINT) AND ISNULL(agency.HtsId, 0) <> 0
    LEFT JOIN Cng.StationInfo station ON station.HtsId = CAST(s.StationId AS BIGINT) AND ISNULL(station.HtsId, 0) <> 0
    LEFT JOIN SLS.Customer beneficiary ON beneficiary.HtsId = CAST(s.StationBeneficiaryId AS BIGINT) AND ISNULL(beneficiary.HtsId, 0) <> 0
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = CAST(s.CreatedUserId AS INT)
    LEFT JOIN #UserMap umUpdate ON umUpdate.OldUserId = CAST(s.UpdatedUserId AS INT);

    DECLARE @SrcCnt INT = (SELECT COUNT(*) FROM #Src);
    DECLARE @UnAgency INT = (SELECT COUNT(*) FROM #Src WHERE UnmappedAgency = 1);
    DECLARE @UnStation INT = (SELECT COUNT(*) FROM #Src WHERE UnmappedStation = 1);
    DECLARE @UnBenef INT = (SELECT COUNT(*) FROM #Src WHERE UnmappedBeneficiary = 1);
    DECLARE @UnCreate INT = (SELECT COUNT(*) FROM #Src WHERE UnmappedCreatedUser = 1);
    DECLARE @UnUpdate INT = (SELECT COUNT(*) FROM #Src WHERE UnmappedUpdatedUser = 1);
    PRINT N'  Source rows: ' + CAST(@SrcCnt AS nvarchar(20));
    PRINT N'  Unmapped Agency (skipped): ' + CAST(@UnAgency AS nvarchar(20));
    PRINT N'  Unmapped Station (null kept): ' + CAST(@UnStation AS nvarchar(20));
    PRINT N'  Unmapped Beneficiary (null kept): ' + CAST(@UnBenef AS nvarchar(20));
    PRINT N'  Unmapped CreatedUser: ' + CAST(@UnCreate AS nvarchar(20));
    PRINT N'  Unmapped UpdatedUser: ' + CAST(@UnUpdate AS nvarchar(20));

    IF OBJECT_ID('tempdb..#Mapped') IS NOT NULL DROP TABLE #Mapped;
    SELECT * INTO #Mapped FROM #Src WHERE UnmappedAgency = 0;

    UPDATE t SET
        t.AgencyId = s.AgencyId,
        t.StationId = s.StationId,
        t.StationBeneficiaryId = s.StationBeneficiaryId,
        t.StartDate = s.StartDate,
        t.StartDateInText = s.StartDateInText,
        t.EndDate = s.EndDate,
        t.EndDateInText = s.EndDateInText,
        t.VisitCountInMonth = s.VisitCountInMonth,
        t.ContractDurationInMonth = s.ContractDurationInMonth,
        t.MonthlyPrice = s.MonthlyPrice,
        t.TotalPrice = s.TotalPrice,
        t.Status = s.Status,
        t.IsCanceled = s.IsCanceled,
        t.CancelDescription = s.CancelDescription,
        t.Comment = s.Comment,
        t.CreatedById = COALESCE(s.CreatedById, t.CreatedById),
        t.CreatedByName = COALESCE(s.CreatedByName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedOnMiladiDateTime, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(s.CreatedOnShamsiDateTime, t.CreatedOnShamsiDateTime),
        t.ModifiedById = COALESCE(s.ModifiedById, 1),
        t.ModifiedByName = COALESCE(s.ModifiedByName, @Seed),
        t.ModifiedDateMiladiDateTime = COALESCE(s.ModifiedDateMiladiDateTime, @Now),
        t.ModifiedDateShamsiDateTime = COALESCE(s.ModifiedDateShamsiDateTime, @NowShamsi),
        t.IsActive = 1
    FROM Cng.MaintenanceContract t
    INNER JOIN #Mapped s ON s.HtsId = t.HtsId;
    PRINT N'  MaintenanceContract updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.MaintenanceContract (
        HtsId, AgencyId, StationId, StationBeneficiaryId,
        StartDate, StartDateInText, EndDate, EndDateInText,
        VisitCountInMonth, ContractDurationInMonth, MonthlyPrice, TotalPrice,
        Status, IsCanceled, CancelDescription, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive
    )
    SELECT
        s.HtsId, s.AgencyId, s.StationId, s.StationBeneficiaryId,
        s.StartDate, s.StartDateInText, s.EndDate, s.EndDateInText,
        s.VisitCountInMonth, s.ContractDurationInMonth, s.MonthlyPrice, s.TotalPrice,
        s.Status, s.IsCanceled, s.CancelDescription, s.Comment,
        COALESCE(s.CreatedById, 1), COALESCE(s.CreatedByName, @Seed),
        COALESCE(s.CreatedOnMiladiDateTime, @Now), COALESCE(s.CreatedOnShamsiDateTime, @NowShamsi),
        COALESCE(s.ModifiedById, 1), COALESCE(s.ModifiedByName, @Seed),
        COALESCE(s.ModifiedDateMiladiDateTime, @Now), COALESCE(s.ModifiedDateShamsiDateTime, @NowShamsi),
        1
    FROM #Mapped s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.MaintenanceContract t WHERE t.HtsId = s.HtsId);
    PRINT N'  MaintenanceContract inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Sync_CngMaintenanceContract_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT COUNT(*) AS AppCount FROM Cng.MaintenanceContract;
SELECT COUNT(*) AS HtsSourceCount FROM [TMS].[TotalSystem].[dbo].[Cng_MaintenanceContract];
