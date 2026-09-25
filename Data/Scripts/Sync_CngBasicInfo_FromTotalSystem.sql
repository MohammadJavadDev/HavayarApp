/*
Sync_CngBasicInfo_FromTotalSystem.sql
همگام‌سازی Cng.FailureInfo + Cng.StationInfo از [TMS].[TotalSystem].
Customer: SLS.Customer.HtsId = Cng_StationInfo.CustomerId
Unmapped CustomerId → NULL (گزارش شمارش)
CreatedBy: Gnr_User.Username/AD → system.[User].Username
Idempotent MERGE روی HtsId.
UTF-8 BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-cng-basicinfo';

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

    PRINT N'=== FailureInfo ===';
    IF OBJECT_ID('tempdb..#Fail') IS NOT NULL DROP TABLE #Fail;
    SELECT
        CAST(f.Id AS BIGINT) AS HtsId,
        CAST(f.GroupId AS INT) AS [Group],
        f.Title,
        f.InitialValue,
        f.Comment,
        f.CreatedDate,
        f.CreatedDateInText,
        um.NewUserId,
        um.NewUserName
    INTO #Fail
    FROM [TMS].[TotalSystem].[dbo].[Cng_FailureInfo] f
    LEFT JOIN #UserMap um ON um.OldUserId = f.CreatedUserId;

    UPDATE t SET
        t.[Group] = s.[Group],
        t.Title = s.Title,
        t.InitialValue = s.InitialValue,
        t.Comment = s.Comment,
        t.CreatedById = COALESCE(s.NewUserId, t.CreatedById),
        t.CreatedByName = COALESCE(s.NewUserName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedDate, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N''), t.CreatedOnShamsiDateTime),
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.FailureInfo t
    INNER JOIN #Fail s ON s.HtsId = t.HtsId;
    PRINT N'  FailureInfo updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.FailureInfo
        (HtsId, [Group], Title, InitialValue, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.[Group], s.Title, s.InitialValue, s.Comment,
        s.NewUserId, s.NewUserName, s.CreatedDate, NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N''),
        1, @Seed, @Now, @NowShamsi, 1
    FROM #Fail s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.FailureInfo t WHERE t.HtsId = s.HtsId);
    PRINT N'  FailureInfo inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== StationInfo ===';
    IF OBJECT_ID('tempdb..#Stat') IS NOT NULL DROP TABLE #Stat;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        s.CustomerId AS HtsCustomerId,
        c.Id AS CustomerId,
        s.StationTitle,
        CAST(s.EquipmentTypeId AS INT) AS EquipmentType,
        s.CompressorSerial, s.DryerSerial, s.StorageModule,
        s.Dispenser1Serial, s.Dispenser2Serial, s.Dispenser3Serial, s.Dispenser4Serial,
        CAST(s.LaunchDate AS datetime2) AS LaunchMiladiDate,
        NULLIF(LTRIM(RTRIM(s.LaunchDateInText)), N'') AS LaunchShamsiDate,
        CAST(s.PermanentDeliveryDate AS datetime2) AS PermanentDeliveryMiladiDate,
        NULLIF(LTRIM(RTRIM(s.PermanentDeliveryDateInText)), N'') AS PermanentDeliveryShamsiDate,
        s.RepresentativeName, s.RepresentativeTellNumber, s.RepresentativeMobileNumber, s.RepresentativeEmailAddress,
        s.Comment, s.CreatedDate,
        NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedDateInText,
        um.NewUserId, um.NewUserName,
        CASE WHEN s.CustomerId IS NOT NULL AND c.Id IS NULL THEN 1 ELSE 0 END AS UnmappedCustomer
    INTO #Stat
    FROM [TMS].[TotalSystem].[dbo].[Cng_StationInfo] s
    LEFT JOIN SLS.Customer c ON c.HtsId = s.CustomerId AND ISNULL(c.HtsId, 0) <> 0
    LEFT JOIN #UserMap um ON um.OldUserId = s.CreatedUserId;

    DECLARE @Unmapped INT = (SELECT COUNT(*) FROM #Stat WHERE UnmappedCustomer = 1);
    PRINT N'  Station rows with unmapped CustomerId (will insert CustomerId=NULL): ' + CAST(@Unmapped AS nvarchar(20));

    UPDATE t SET
        t.CustomerId = s.CustomerId,
        t.StationTitle = ISNULL(NULLIF(LTRIM(RTRIM(s.StationTitle)), N''), N''),
        t.EquipmentType = s.EquipmentType,
        t.CompressorSerial = s.CompressorSerial,
        t.DryerSerial = s.DryerSerial,
        t.StorageModule = s.StorageModule,
        t.Dispenser1Serial = s.Dispenser1Serial,
        t.Dispenser2Serial = s.Dispenser2Serial,
        t.Dispenser3Serial = s.Dispenser3Serial,
        t.Dispenser4Serial = s.Dispenser4Serial,
        t.LaunchMiladiDate = s.LaunchMiladiDate,
        t.LaunchShamsiDate = s.LaunchShamsiDate,
        t.PermanentDeliveryMiladiDate = s.PermanentDeliveryMiladiDate,
        t.PermanentDeliveryShamsiDate = s.PermanentDeliveryShamsiDate,
        t.RepresentativeName = s.RepresentativeName,
        t.RepresentativeTellNumber = s.RepresentativeTellNumber,
        t.RepresentativeMobileNumber = s.RepresentativeMobileNumber,
        t.RepresentativeEmailAddress = s.RepresentativeEmailAddress,
        t.Comment = s.Comment,
        t.CreatedById = COALESCE(s.NewUserId, t.CreatedById),
        t.CreatedByName = COALESCE(s.NewUserName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedDate, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(s.CreatedDateInText, t.CreatedOnShamsiDateTime),
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.StationInfo t
    INNER JOIN #Stat s ON s.HtsId = t.HtsId;
    PRINT N'  StationInfo updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.StationInfo
        (HtsId, CustomerId, StationTitle, EquipmentType,
         CompressorSerial, DryerSerial, StorageModule,
         Dispenser1Serial, Dispenser2Serial, Dispenser3Serial, Dispenser4Serial,
         LaunchMiladiDate, LaunchShamsiDate, PermanentDeliveryMiladiDate, PermanentDeliveryShamsiDate,
         RepresentativeName, RepresentativeTellNumber, RepresentativeMobileNumber, RepresentativeEmailAddress, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.CustomerId, ISNULL(NULLIF(LTRIM(RTRIM(s.StationTitle)), N''), N''), s.EquipmentType,
        s.CompressorSerial, s.DryerSerial, s.StorageModule,
        s.Dispenser1Serial, s.Dispenser2Serial, s.Dispenser3Serial, s.Dispenser4Serial,
        s.LaunchMiladiDate, s.LaunchShamsiDate, s.PermanentDeliveryMiladiDate, s.PermanentDeliveryShamsiDate,
        s.RepresentativeName, s.RepresentativeTellNumber, s.RepresentativeMobileNumber, s.RepresentativeEmailAddress, s.Comment,
        s.NewUserId, s.NewUserName, s.CreatedDate, s.CreatedDateInText,
        1, @Seed, @Now, @NowShamsi, 1
    FROM #Stat s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.StationInfo t WHERE t.HtsId = s.HtsId);
    PRINT N'  StationInfo inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Sync_CngBasicInfo_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT N'FailureInfo' AS T, COUNT(*) AS Cnt FROM Cng.FailureInfo
UNION ALL SELECT N'StationInfo', COUNT(*) FROM Cng.StationInfo
UNION ALL SELECT N'StationInfo_NullCustomer', COUNT(*) FROM Cng.StationInfo WHERE CustomerId IS NULL;
