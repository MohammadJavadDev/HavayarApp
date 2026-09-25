/*
Sync_CngRepairs_FromTotalSystem.sql
همگام‌سازی Cng.Repairs + Cng.RepairsPart + Cng.RepairsManHours از [TMS].[TotalSystem].
Attachments: skip (خالی می‌ماند).
DlId: Acc_DL.Acc_DL_ID = HTS DlId سپس FIN.DL.Title = Acc_DLTitle (نه HamkaranId = DL_FK).
Product/Agency: Inv.Part.HtsId / SLS.Customer.HtsId — بدون نگاشت FK درج نمی‌شود.
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

IF OBJECT_ID(N'Cng.Repairs', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Cng.Repairs وجود ندارد.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-cng-repairs';

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== User map (Confirmer / CreatedBy) ========== */
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

    /* ========== Acc_DL → FIN.DL (Title match, prefer Hamkaran code) ========== */
    IF OBJECT_ID('tempdb..#DlMap') IS NOT NULL DROP TABLE #DlMap;
    SELECT HtsDlId, DlId
    INTO #DlMap
    FROM (
        SELECT ad.Acc_DL_ID AS HtsDlId, d.Id AS DlId,
               ROW_NUMBER() OVER (
                   PARTITION BY ad.Acc_DL_ID
                   ORDER BY CASE WHEN d.Code = CAST(ad.Hamkaran_Acc_DL_FK AS nvarchar(64)) THEN 0 ELSE 1 END, d.Id
               ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
        INNER JOIN FIN.DL d ON LTRIM(RTRIM(d.Title)) = LTRIM(RTRIM(ad.Acc_DLTitle))
    ) z
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_DlMap ON #DlMap (HtsDlId);

    PRINT N'=== Cng.Repairs ===';
    IF OBJECT_ID('tempdb..#RepSrc') IS NOT NULL DROP TABLE #RepSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        NULLIF(LTRIM(RTRIM(s.IDNumber)), N'') AS IdNumber,
        p.Id AS ProductId,
        ag.Id AS AgencyId,
        cust.Id AS CustomerId,
        dl.DlId,
        NULLIF(LTRIM(RTRIM(s.Serial)), N'') AS Serial,
        s.[Count],
        CAST(s.IsGuarantee AS BIT) AS IsGuarantee,
        CAST(s.NeedForSendToEmployer AS BIT) AS NeedForSendToEmployer,
        CAST(s.EntryDate AS datetime2) AS EntryMiladiDate,
        NULLIF(LTRIM(RTRIM(s.EntryDate_Shamsi)), N'') AS EntryShamsiDate,
        CAST(s.DeliveryDate_Warehouse AS datetime2) AS WarehouseDeliveryMiladiDate,
        NULLIF(LTRIM(RTRIM(s.DeliveryDate_Warehouse_Shamsi)), N'') AS WarehouseDeliveryShamsiDate,
        CAST(s.WarehouseExitDate AS datetime2) AS WarehouseExitMiladiDate,
        NULLIF(LTRIM(RTRIM(s.WarehouseExitDateInText)), N'') AS WarehouseExitShamsiDate,
        CAST(s.DestinationAfterRepair_FK AS INT) AS DestinationAfterRepair,
        s.DestinationAddress,
        s.Customer_Phone AS CustomerPhone,
        s.CustomerPhoneFromHistory,
        s.PreFactor_Number AS PreFactorNumber,
        s.DeliveryForm_Number AS DeliveryFormNumber,
        s.LoanForm_Number AS LoanFormNumber,
        umConf.NewUserId AS ConfirmerId,
        CAST(s.Confirm_DateLatin AS datetime2) AS ConfirmMiladiDate,
        NULLIF(LTRIM(RTRIM(s.Confirm_Date)), N'') AS ConfirmShamsiDate,
        NULLIF(LTRIM(RTRIM(s.Confirm_Time)), N'') AS ConfirmTime,
        CAST(s.StatusId AS INT) AS [Status],
        pe.Id AS ResponsiblePersonnelId,
        CAST(s.EstimatedRepairDate AS datetime2) AS EstimatedRepairMiladiDate,
        NULLIF(LTRIM(RTRIM(s.EstimatedRepairDate_Shamsi)), N'') AS EstimatedRepairShamsiDate,
        s.FailureReason,
        s.PartDescriptionBelonging,
        CAST(s.HasVendor AS BIT) AS HasVendor,
        s.VendorName,
        s.VendoringCost,
        CAST(s.SendMethodId AS INT) AS SendMethod,
        pen.Id AS PartEntryNotifyId,
        s.Comment,
        CAST(s.CreatedDate AS datetime2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime,
        umCreate.NewUserId AS CreatedById,
        umCreate.NewUserName AS CreatedByName,
        CASE WHEN p.Id IS NULL THEN 1 ELSE 0 END AS SkipProduct,
        CASE WHEN ag.Id IS NULL THEN 1 ELSE 0 END AS SkipAgency,
        CASE WHEN s.DlId IS NOT NULL AND dl.DlId IS NULL THEN 1 ELSE 0 END AS UnmappedDl,
        CASE WHEN s.Customer_FK IS NOT NULL AND cust.Id IS NULL THEN 1 ELSE 0 END AS UnmappedCustomer,
        CASE WHEN s.ConfirmerUserId IS NOT NULL AND umConf.NewUserId IS NULL THEN 1 ELSE 0 END AS UnmappedConfirmer,
        CASE WHEN s.ResponsiblePersonnel_FK IS NOT NULL AND pe.Id IS NULL THEN 1 ELSE 0 END AS UnmappedResp,
        CASE WHEN s.PartEntryNotifyId IS NOT NULL AND pen.Id IS NULL THEN 1 ELSE 0 END AS UnmappedPen
    INTO #RepSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_Repairs] s
    LEFT JOIN Inv.Part p ON p.HtsId = CAST(s.Product_FK AS BIGINT) AND ISNULL(p.HtsId, 0) <> 0
    LEFT JOIN SLS.Customer ag ON ag.HtsId = CAST(s.AgencyId AS BIGINT) AND ISNULL(ag.HtsId, 0) <> 0
    LEFT JOIN SLS.Customer cust ON cust.HtsId = CAST(s.Customer_FK AS BIGINT) AND ISNULL(cust.HtsId, 0) <> 0
    LEFT JOIN #DlMap dl ON dl.HtsDlId = s.DlId
    LEFT JOIN #UserMap umConf ON umConf.OldUserId = s.ConfirmerUserId
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = s.CreatedUserId
    LEFT JOIN Hcm.Personel pe ON pe.HamkaranId = s.ResponsiblePersonnel_FK
    LEFT JOIN Gnr.PartEntryNotify pen ON pen.HtsId = CAST(s.PartEntryNotifyId AS BIGINT) AND ISNULL(pen.HtsId, 0) <> 0;

    DECLARE @SkipRep INT = (SELECT COUNT(*) FROM #RepSrc WHERE SkipProduct = 1 OR SkipAgency = 1);
    DECLARE @UnmappedDl INT = (SELECT COUNT(*) FROM #RepSrc WHERE UnmappedDl = 1);
    DECLARE @UnmappedCustomer INT = (SELECT COUNT(*) FROM #RepSrc WHERE UnmappedCustomer = 1);
    DECLARE @UnmappedConfirmer INT = (SELECT COUNT(*) FROM #RepSrc WHERE UnmappedConfirmer = 1);
    DECLARE @UnmappedResp INT = (SELECT COUNT(*) FROM #RepSrc WHERE UnmappedResp = 1);
    DECLARE @UnmappedPen INT = (SELECT COUNT(*) FROM #RepSrc WHERE UnmappedPen = 1);
    PRINT N'  Repairs skipped (Product/Agency FK): ' + CAST(@SkipRep AS nvarchar(20));
    PRINT N'  Unmapped Dl (will insert DlId=NULL): ' + CAST(@UnmappedDl AS nvarchar(20));
    PRINT N'  Unmapped Customer (NULL): ' + CAST(@UnmappedCustomer AS nvarchar(20));
    PRINT N'  Unmapped Confirmer (NULL): ' + CAST(@UnmappedConfirmer AS nvarchar(20));
    PRINT N'  Unmapped ResponsiblePersonnel (NULL): ' + CAST(@UnmappedResp AS nvarchar(20));
    PRINT N'  Unmapped PartEntryNotify (NULL): ' + CAST(@UnmappedPen AS nvarchar(20));

    IF @SkipRep > 0
    BEGIN
        PRINT N'  Sample skipped HTS Ids:';
        SELECT TOP 15 HtsId, SkipProduct, SkipAgency FROM #RepSrc WHERE SkipProduct = 1 OR SkipAgency = 1 ORDER BY HtsId;
    END

    IF OBJECT_ID('tempdb..#RepMapped') IS NOT NULL DROP TABLE #RepMapped;
    SELECT * INTO #RepMapped FROM #RepSrc WHERE SkipProduct = 0 AND SkipAgency = 0;

    UPDATE t SET
        t.IdNumber = s.IdNumber,
        t.ProductId = s.ProductId,
        t.AgencyId = s.AgencyId,
        t.CustomerId = s.CustomerId,
        t.DlId = s.DlId,
        t.Serial = s.Serial,
        t.[Count] = s.[Count],
        t.IsGuarantee = s.IsGuarantee,
        t.NeedForSendToEmployer = s.NeedForSendToEmployer,
        t.EntryMiladiDate = s.EntryMiladiDate,
        t.EntryShamsiDate = s.EntryShamsiDate,
        t.WarehouseDeliveryMiladiDate = s.WarehouseDeliveryMiladiDate,
        t.WarehouseDeliveryShamsiDate = s.WarehouseDeliveryShamsiDate,
        t.WarehouseExitMiladiDate = s.WarehouseExitMiladiDate,
        t.WarehouseExitShamsiDate = s.WarehouseExitShamsiDate,
        t.DestinationAfterRepair = s.DestinationAfterRepair,
        t.DestinationAddress = s.DestinationAddress,
        t.CustomerPhone = s.CustomerPhone,
        t.CustomerPhoneFromHistory = s.CustomerPhoneFromHistory,
        t.PreFactorNumber = s.PreFactorNumber,
        t.DeliveryFormNumber = s.DeliveryFormNumber,
        t.LoanFormNumber = s.LoanFormNumber,
        t.ConfirmerId = s.ConfirmerId,
        t.ConfirmMiladiDate = s.ConfirmMiladiDate,
        t.ConfirmShamsiDate = s.ConfirmShamsiDate,
        t.ConfirmTime = s.ConfirmTime,
        t.[Status] = s.[Status],
        t.ResponsiblePersonnelId = s.ResponsiblePersonnelId,
        t.EstimatedRepairMiladiDate = s.EstimatedRepairMiladiDate,
        t.EstimatedRepairShamsiDate = s.EstimatedRepairShamsiDate,
        t.FailureReason = s.FailureReason,
        t.PartDescriptionBelonging = s.PartDescriptionBelonging,
        t.HasVendor = s.HasVendor,
        t.VendorName = s.VendorName,
        t.VendoringCost = s.VendoringCost,
        t.SendMethod = s.SendMethod,
        t.PartEntryNotifyId = s.PartEntryNotifyId,
        t.Comment = s.Comment,
        t.CreatedById = COALESCE(s.CreatedById, t.CreatedById),
        t.CreatedByName = COALESCE(s.CreatedByName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedOnMiladiDateTime, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(s.CreatedOnShamsiDateTime, t.CreatedOnShamsiDateTime),
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.Repairs t
    INNER JOIN #RepMapped s ON s.HtsId = t.HtsId;
    PRINT N'  Repairs updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.Repairs (
        HtsId, IdNumber, ProductId, AgencyId, CustomerId, DlId, Serial, [Count],
        IsGuarantee, NeedForSendToEmployer,
        EntryMiladiDate, EntryShamsiDate,
        WarehouseDeliveryMiladiDate, WarehouseDeliveryShamsiDate,
        WarehouseExitMiladiDate, WarehouseExitShamsiDate,
        DestinationAfterRepair, DestinationAddress, CustomerPhone, CustomerPhoneFromHistory,
        PreFactorNumber, DeliveryFormNumber, LoanFormNumber,
        ConfirmerId, ConfirmMiladiDate, ConfirmShamsiDate, ConfirmTime,
        [Status], ResponsiblePersonnelId,
        EstimatedRepairMiladiDate, EstimatedRepairShamsiDate,
        FailureReason, PartDescriptionBelonging, HasVendor, VendorName, VendoringCost,
        SendMethod, PartEntryNotifyId, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.IdNumber, s.ProductId, s.AgencyId, s.CustomerId, s.DlId, s.Serial, s.[Count],
        s.IsGuarantee, s.NeedForSendToEmployer,
        s.EntryMiladiDate, s.EntryShamsiDate,
        s.WarehouseDeliveryMiladiDate, s.WarehouseDeliveryShamsiDate,
        s.WarehouseExitMiladiDate, s.WarehouseExitShamsiDate,
        s.DestinationAfterRepair, s.DestinationAddress, s.CustomerPhone, s.CustomerPhoneFromHistory,
        s.PreFactorNumber, s.DeliveryFormNumber, s.LoanFormNumber,
        s.ConfirmerId, s.ConfirmMiladiDate, s.ConfirmShamsiDate, s.ConfirmTime,
        s.[Status], s.ResponsiblePersonnelId,
        s.EstimatedRepairMiladiDate, s.EstimatedRepairShamsiDate,
        s.FailureReason, s.PartDescriptionBelonging, s.HasVendor, s.VendorName, s.VendoringCost,
        s.SendMethod, s.PartEntryNotifyId, s.Comment,
        ISNULL(s.CreatedById, 1), ISNULL(s.CreatedByName, @Seed),
        ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
        1, @Seed, @Now, @NowShamsi, 1
    FROM #RepMapped s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.Repairs t WHERE t.HtsId = s.HtsId);
    PRINT N'  Repairs inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== Cng.RepairsPart ===';
    IF OBJECT_ID('tempdb..#PartSrc') IS NOT NULL DROP TABLE #PartSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        rr.Id AS RepairsId,
        COALESCE(dl.DlId, dlHam.Id) AS DlId,
        s.Hamkaran_Dl_Fk AS HamkaranDlFk,
        p.Id AS PartId,
        COALESCE(pu.Id, p.UnitId) AS PartUnitId,
        s.Hamkaran_InvVchItm_FK AS HamkaranInvVchItmFk,
        CAST(ISNULL(s.DamagedPart, 0) AS BIT) AS DamagedPart,
        s.DamagedPartDeliveryFormNumber,
        s.StockRef,
        s.StockName,
        s.UnitName,
        CAST(ISNULL(s.UsedMount, 0) AS DECIMAL(18, 4)) AS UsedMount,
        CAST(ISNULL(s.ReturnMount, 0) AS DECIMAL(18, 4)) AS ReturnMount,
        s.BuyPrice,
        LEFT(NULLIF(LTRIM(RTRIM(s.LastBuyPriceDate_Shamsi)), N''), 80) AS LastBuyPriceDateShamsi,
        s.SalePrice,
        CASE WHEN rr.Id IS NULL THEN 1 ELSE 0 END AS SkipParent,
        CASE WHEN p.Id IS NULL THEN 1 ELSE 0 END AS SkipPart,
        CASE WHEN COALESCE(dl.DlId, dlHam.Id) IS NULL THEN 1 ELSE 0 END AS SkipDl
    INTO #PartSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_RepairsPart] s
    LEFT JOIN Cng.Repairs rr ON rr.HtsId = CAST(s.RepairsId AS BIGINT)
    LEFT JOIN Inv.Part p ON p.HtsId = CAST(s.Part_FK AS BIGINT) AND ISNULL(p.HtsId, 0) <> 0
    LEFT JOIN Inv.PartUnit pu ON pu.HamkaranId = s.PartUnit_FK
    LEFT JOIN #DlMap dl ON dl.HtsDlId = s.AccDL_FK
    OUTER APPLY (
        SELECT TOP 1 d.Id
        FROM FIN.DL d
        WHERE d.HamkaranId = s.AccDL_FK
           OR TRY_CONVERT(INT, LTRIM(RTRIM(d.Code))) = s.Hamkaran_Dl_Fk
        ORDER BY CASE WHEN TRY_CONVERT(INT, LTRIM(RTRIM(d.Code))) = s.Hamkaran_Dl_Fk THEN 0 ELSE 1 END, d.Id
    ) dlHam;

    IF OBJECT_ID('tempdb..#PartMappedAll') IS NOT NULL DROP TABLE #PartMappedAll;
    ;WITH Ranked AS (
        SELECT *,
               ROW_NUMBER() OVER (PARTITION BY HtsId ORDER BY CASE WHEN DlId IS NOT NULL THEN 0 ELSE 1 END, PartId) AS rn
        FROM #PartSrc
    )
    SELECT HtsId, RepairsId, DlId, HamkaranDlFk, PartId, PartUnitId, HamkaranInvVchItmFk,
           DamagedPart, DamagedPartDeliveryFormNumber, StockRef, StockName, UnitName,
           UsedMount, ReturnMount, BuyPrice, LastBuyPriceDateShamsi, SalePrice,
           SkipParent, SkipPart, SkipDl
    INTO #PartMappedAll
    FROM Ranked
    WHERE rn = 1;

    DECLARE @SkipPartCnt INT = (SELECT COUNT(*) FROM #PartMappedAll WHERE SkipParent = 1 OR SkipPart = 1 OR SkipDl = 1);
    PRINT N'  RepairsPart skipped (parent/part/dl FK): ' + CAST(@SkipPartCnt AS nvarchar(20));
    IF @SkipPartCnt > 0
    BEGIN
        PRINT N'  Sample skipped part HTS Ids:';
        SELECT TOP 20 HtsId, SkipParent, SkipPart, SkipDl FROM #PartMappedAll
        WHERE SkipParent = 1 OR SkipPart = 1 OR SkipDl = 1 ORDER BY HtsId;
    END

    IF OBJECT_ID('tempdb..#PartMapped') IS NOT NULL DROP TABLE #PartMapped;
    SELECT * INTO #PartMapped FROM #PartMappedAll WHERE SkipParent = 0 AND SkipPart = 0 AND SkipDl = 0;

    UPDATE t SET
        t.RepairsId = s.RepairsId,
        t.DlId = s.DlId,
        t.HamkaranDlFk = s.HamkaranDlFk,
        t.PartId = s.PartId,
        t.PartUnitId = s.PartUnitId,
        t.HamkaranInvVchItmFk = s.HamkaranInvVchItmFk,
        t.DamagedPart = s.DamagedPart,
        t.DamagedPartDeliveryFormNumber = s.DamagedPartDeliveryFormNumber,
        t.StockRef = s.StockRef,
        t.StockName = s.StockName,
        t.UnitName = s.UnitName,
        t.UsedMount = s.UsedMount,
        t.ReturnMount = s.ReturnMount,
        t.BuyPrice = s.BuyPrice,
        t.LastBuyPriceDateShamsi = s.LastBuyPriceDateShamsi,
        t.SalePrice = s.SalePrice,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.RepairsPart t
    INNER JOIN #PartMapped s ON s.HtsId = t.HtsId;
    PRINT N'  RepairsPart updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.RepairsPart (
        HtsId, RepairsId, DlId, HamkaranDlFk, PartId, PartUnitId, HamkaranInvVchItmFk,
        DamagedPart, DamagedPartDeliveryFormNumber, StockRef, StockName, UnitName,
        UsedMount, ReturnMount, BuyPrice, LastBuyPriceDateShamsi, SalePrice,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.RepairsId, s.DlId, s.HamkaranDlFk, s.PartId, s.PartUnitId, s.HamkaranInvVchItmFk,
        s.DamagedPart, s.DamagedPartDeliveryFormNumber, s.StockRef, s.StockName, s.UnitName,
        s.UsedMount, s.ReturnMount, s.BuyPrice, s.LastBuyPriceDateShamsi, s.SalePrice,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi, 1
    FROM #PartMapped s
    WHERE NOT EXISTS (SELECT 1 FROM Cng.RepairsPart t WHERE t.HtsId = s.HtsId);
    PRINT N'  RepairsPart inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== Cng.RepairsManHours ===';
    IF OBJECT_ID('tempdb..#MhSrc') IS NOT NULL DROP TABLE #MhSrc;
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        rr.Id AS RepairsId,
        pe.Id AS PersonelId,
        NULLIF(LTRIM(RTRIM(s.WorkDate)), N'') AS WorkDate,
        NULLIF(LTRIM(RTRIM(s.StartTime)), N'') AS StartTime,
        NULLIF(LTRIM(RTRIM(s.EndTime)), N'') AS EndTime,
        s.ManHourPrice,
        s.Comment,
        CASE WHEN rr.Id IS NULL THEN 1 ELSE 0 END AS SkipParent,
        CASE WHEN pe.Id IS NULL THEN 1 ELSE 0 END AS SkipPersonel
    INTO #MhSrc
    FROM [TMS].[TotalSystem].[dbo].[Cng_RepairsManHours] s
    LEFT JOIN Cng.Repairs rr ON rr.HtsId = CAST(s.RepairsId AS BIGINT)
    LEFT JOIN Hcm.Personel pe ON pe.HamkaranId = s.Personel_FK;

    DECLARE @SkipMh INT = (SELECT COUNT(*) FROM #MhSrc WHERE SkipParent = 1 OR SkipPersonel = 1);
    PRINT N'  ManHours skipped: ' + CAST(@SkipMh AS nvarchar(20));
    IF @SkipMh > 0
        SELECT HtsId, SkipParent, SkipPersonel FROM #MhSrc WHERE SkipParent = 1 OR SkipPersonel = 1;

    UPDATE t SET
        t.RepairsId = s.RepairsId,
        t.PersonelId = s.PersonelId,
        t.WorkDate = s.WorkDate,
        t.StartTime = s.StartTime,
        t.EndTime = s.EndTime,
        t.ManHourPrice = s.ManHourPrice,
        t.Comment = s.Comment,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.RepairsManHours t
    INNER JOIN #MhSrc s ON s.HtsId = t.HtsId
    WHERE s.SkipParent = 0 AND s.SkipPersonel = 0;
    PRINT N'  ManHours updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.RepairsManHours (
        HtsId, RepairsId, PersonelId, WorkDate, StartTime, EndTime, ManHourPrice, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.RepairsId, s.PersonelId, s.WorkDate, s.StartTime, s.EndTime, s.ManHourPrice, s.Comment,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi, 1
    FROM #MhSrc s
    WHERE s.SkipParent = 0 AND s.SkipPersonel = 0
      AND NOT EXISTS (SELECT 1 FROM Cng.RepairsManHours t WHERE t.HtsId = s.HtsId);
    PRINT N'  ManHours inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== Attachments: skipped (empty by design) ===';

    COMMIT TRANSACTION;
    PRINT N'=== DONE Sync_CngRepairs_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT N'Repairs' AS T, COUNT(*) AS Cnt FROM Cng.Repairs
UNION ALL SELECT N'RepairsPart', COUNT(*) FROM Cng.RepairsPart
UNION ALL SELECT N'RepairsManHours', COUNT(*) FROM Cng.RepairsManHours
UNION ALL SELECT N'RepairsAttachment', COUNT(*) FROM Cng.RepairsAttachment
UNION ALL SELECT N'HTS_Repairs', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_Repairs]
UNION ALL SELECT N'HTS_RepairsPart', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_RepairsPart]
UNION ALL SELECT N'HTS_ManHours', COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Cng_RepairsManHours];
