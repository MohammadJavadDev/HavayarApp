/*
================================================================================
Sync_SaleOrder_FromHts.sql
================================================================================
همگام‌سازی Sale.Order / OrderDetail / OrderDetailSerial از HTS (TotalSystem).

جاب راهکاران (SaleOrderJob) سفارش و قلم را با HamkaranId می‌سازد ولی HtsId را
نمی‌گذارد. MERGE خام روی HtsId ردیف تکراری می‌سازد. این اسکریپت:

  1) HtsId را روی ردیف موجود با HamkaranId = VchHdrId / Hamkaran_Vchitm_FK می‌زند
  2) ردیف HTS بدون معادل راهکاران را INSERT می‌کند
  3) سریال را MERGE می‌کند (منبع حقیقت HTS)

Idempotent. Linked Server [TMS] لازم است.
ایجاد/به‌روزرسانی رویه Sale.SyncOrderFromHts و اجرای آن.

۴۷ قلم HTS با Order_FK خالی و بدون هدر در Sale_Order عمداً درج نمی‌شوند (یتییم).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

BEGIN TRY
    EXEC sp_testlinkedserver [TMS];
END TRY
BEGIN CATCH
    DECLARE @Ls NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'sp_testlinkedserver [TMS] ناموفق: %s', 16, 1, @Ls);
    RETURN;
END CATCH;
GO

CREATE OR ALTER PROCEDURE Sale.SyncOrderFromHts
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET QUOTED_IDENTIFIER ON;
    SET ANSI_NULLS ON;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'sync-saleorder-hts';
    DECLARE @OrderStamped INT = 0;
    DECLARE @OrderInserted INT = 0;
    DECLARE @DetailStamped INT = 0;
    DECLARE @DetailInserted INT = 0;
    DECLARE @ProjectMerged INT = 0;
    DECLARE @SerialMerged INT = 0;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    IF OBJECT_ID('tempdb..#HtsOrder') IS NOT NULL DROP TABLE #HtsOrder;
    IF OBJECT_ID('tempdb..#HtsDetail') IS NOT NULL DROP TABLE #HtsDetail;
    IF OBJECT_ID('tempdb..#HtsSerial') IS NOT NULL DROP TABLE #HtsSerial;
    IF OBJECT_ID('tempdb..#HtsProject') IS NOT NULL DROP TABLE #HtsProject;

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
        CAST(s.Order_ID AS BIGINT) AS HtsId,
        s.VchHdrId AS HamkaranId,
        CAST(s.Year AS NVARCHAR(20)) AS [Year],
        s.VchDate AS VchDateMiladiDate,
        NULLIF(LTRIM(RTRIM(s.VchDate_Shamsi)), N'') AS VchDateShamsiDate,
        CAST(s.Customer_FK AS BIGINT) AS CustomerHtsId,
        CAST(s.VchType_FK AS INT) AS VoucherTypeStatus,
        CAST(s.Status_FK AS INT) AS StatusEnum,
        CAST(s.Branch_FK AS BIGINT) AS BranchHtsId,
        NULLIF(LTRIM(RTRIM(s.Contract_Number)), N'') AS ContractNumber,
        CAST(s.VchNo AS BIGINT) AS OrderNumber,
        CAST(s.VchNo AS BIGINT) AS HtsVchNo,
        CAST(s.Stock_FK AS BIGINT) AS HtsStockId,
        CAST(s.BaseVchType_FK AS BIGINT) AS HtsBaseVoucherTypeId,
        s.ContractVchHeaderId AS HtsContractVchHeaderId,
        CAST(s.Confirmer_FK AS BIGINT) AS ConfirmerHtsId,
        CAST(s.Operator_FK AS BIGINT) AS OperatorHtsId
    INTO #HtsOrder
    FROM OPENQUERY(TMS, N'
        SELECT Order_ID, VchHdrId, Year, VchDate, VchDate_Shamsi, Customer_FK,
               VchType_FK, Status_FK, Branch_FK, Contract_Number, VchNo,
               Stock_FK, BaseVchType_FK, ContractVchHeaderId, Confirmer_FK, Operator_FK
        FROM TotalSystem.dbo.Sale_Order') s;

    CREATE UNIQUE CLUSTERED INDEX IX_HtsOrder_HtsId ON #HtsOrder (HtsId);
    CREATE NONCLUSTERED INDEX IX_HtsOrder_HamkaranId ON #HtsOrder (HamkaranId);

    SELECT
        CAST(d.OrderDetail_ID AS BIGINT) AS HtsId,
        CAST(d.Order_FK AS BIGINT) AS OrderHtsId,
        d.Hamkaran_Vchitm_FK AS HamkaranId,
        d.Hamkaran_VchHdr_FK AS HamkaranOrderHdrId,
        d.Hamkaran_InvVchitm_FK AS HamkaranInvVchItmId,
        CAST(d.Prefactor_VchNo AS BIGINT) AS Prefactor_VchNo,
        CAST(d.Seq AS INT) AS Seq,
        CAST(d.Part_FK AS BIGINT) AS PartHtsId,
        d.Qty,
        d.BaseVchItmRef,
        d.Description,
        d.UnitPrice, d.Price, d.IncPrice, d.DecPrice,
        CAST(d.PayMethod_FK AS INT) AS PayMethod,
        d.PriceTemp,
        ISNULL(d.QtyTemp, 0) AS QtyTemp,
        d.IsPackage, d.NotComplete
    INTO #HtsDetail
    FROM OPENQUERY(TMS, N'
        SELECT OrderDetail_ID, Order_FK, Hamkaran_Vchitm_FK, Hamkaran_VchHdr_FK,
               Hamkaran_InvVchitm_FK, Prefactor_VchNo, Seq, Part_FK, Qty,
               BaseVchItmRef, Description, UnitPrice, Price, IncPrice, DecPrice,
               PayMethod_FK, PriceTemp, QtyTemp, IsPackage, NotComplete
        FROM TotalSystem.dbo.Sale_OrderDetail') d;

    CREATE UNIQUE CLUSTERED INDEX IX_HtsDetail_HtsId ON #HtsDetail (HtsId);
    CREATE NONCLUSTERED INDEX IX_HtsDetail_HamkaranId ON #HtsDetail (HamkaranId);

    SELECT
        CAST(s.OrderDetail_Serial_ID AS BIGINT) AS HtsId,
        CAST(s.OrderDetail_FK AS BIGINT) AS OrderDetailHtsId,
        CAST(s.NewCustomer_FK AS BIGINT) AS CustomerHtsId,
        CAST(s.Customer_Address_FK AS BIGINT) AS AddressHtsId,
        CAST(s.Pln_Project_FK AS BIGINT) AS ProjectHtsId,
        NULLIF(LTRIM(RTRIM(s.Serial)), N'') AS Serial,
        NULLIF(LTRIM(RTRIM(s.Model)), N'') AS Model,
        CAST(s.Hours_Daily AS NVARCHAR(20)) AS HoursDaily,
        s.Runtime, s.LifeTime, s.Guarantee_Time AS GuaranteeTime,
        CAST(s.Guarantee_SendDay AS INT) AS GuaranteeSendDay,
        CAST(s.Guarantee_LaunchDay AS INT) AS GuaranteeLaunchDay,
        s.HasInsurance,
        s.SendDate AS SendMiladiDate,
        NULLIF(LTRIM(RTRIM(s.SendDate_Shamsi)), N'') AS SendShamsiDate,
        NULLIF(LTRIM(RTRIM(s.SendTime)), N'') AS SendTime,
        s.LaunchDate AS LaunchMiladiDate,
        NULLIF(LTRIM(RTRIM(s.LaunchDate_Shamsi)), N'') AS LaunchShamsiDate,
        s.ExitFactory_Date AS ExitFactoryMiladiDate,
        NULLIF(LTRIM(RTRIM(s.ExitFactory_Date_Shamsi)), N'') AS ExitFactoryShamsiDate,
        NULLIF(LTRIM(RTRIM(s.ExitFactory_Time)), N'') AS ExitFactoryTime,
        CAST(s.ProductionOrder_Number AS NVARCHAR(50)) AS ProductionOrderNumber,
        s.IndustrialComment, s.InvComment, s.IndustrialConfirm,
        s.FinalExitDate AS FinalExitMiladiDate,
        NULLIF(LTRIM(RTRIM(s.FinalExitDate_Shamsi)), N'') AS FinalExitShamsiDate,
        NULLIF(LTRIM(RTRIM(s.ExitTimeFinal)), N'') AS ExitTimeFinal
    INTO #HtsSerial
    FROM OPENQUERY(TMS, N'
        SELECT OrderDetail_Serial_ID, OrderDetail_FK, NewCustomer_FK, Customer_Address_FK,
               Pln_Project_FK, Serial, Model, Hours_Daily, Runtime, LifeTime,
               Guarantee_Time, Guarantee_SendDay, Guarantee_LaunchDay, HasInsurance,
               SendDate, SendDate_Shamsi, SendTime, LaunchDate, LaunchDate_Shamsi,
               ExitFactory_Date, ExitFactory_Date_Shamsi, ExitFactory_Time,
               ProductionOrder_Number, IndustrialComment, InvComment, IndustrialConfirm,
               FinalExitDate, FinalExitDate_Shamsi, ExitTimeFinal
        FROM TotalSystem.dbo.Sale_OrderDetail_Serial') s;

    CREATE UNIQUE CLUSTERED INDEX IX_HtsSerial_HtsId ON #HtsSerial (HtsId);

    BEGIN TRANSACTION;

    ;WITH cand AS (
        SELECT
            o.Id,
            h.HtsId,
            h.HtsVchNo,
            h.HtsStockId,
            h.HtsBaseVoucherTypeId,
            h.HtsContractVchHeaderId,
            umc.NewUserId AS ConfirmerUserId,
            umo.NewUserId AS OperatorUserId,
            b.Id AS BranchId,
            ROW_NUMBER() OVER (PARTITION BY h.HtsId ORDER BY o.Id) AS RnHts,
            ROW_NUMBER() OVER (PARTITION BY o.Id ORDER BY h.HtsId) AS RnOrd
        FROM Sale.[Order] o
        INNER JOIN #HtsOrder h ON o.HamkaranId = h.HamkaranId
        LEFT JOIN Sale.Branch b ON b.HtsId = h.BranchHtsId AND b.HtsId <> 0
        LEFT JOIN #UserMap umc ON umc.OldUserId = h.ConfirmerHtsId
        LEFT JOIN #UserMap umo ON umo.OldUserId = h.OperatorHtsId
        WHERE ISNULL(o.HtsId, 0) = 0
          AND NOT EXISTS (
              SELECT 1 FROM Sale.[Order] x
              WHERE x.HtsId = h.HtsId AND x.HtsId <> 0)
    )
    UPDATE o SET
        o.HtsId = c.HtsId,
        o.HtsVchNo = c.HtsVchNo,
        o.HtsStockId = c.HtsStockId,
        o.HtsBaseVoucherTypeId = c.HtsBaseVoucherTypeId,
        o.HtsContractVchHeaderId = c.HtsContractVchHeaderId,
        o.ConfirmerUserId = COALESCE(c.ConfirmerUserId, o.ConfirmerUserId),
        o.OperatorUserId = COALESCE(c.OperatorUserId, o.OperatorUserId),
        o.BranchId = COALESCE(c.BranchId, o.BranchId),
        o.ModifiedById = 1,
        o.ModifiedByName = @SeedUser,
        o.ModifiedDateMiladiDateTime = @Now,
        o.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.[Order] o
    INNER JOIN cand c ON c.Id = o.Id AND c.RnHts = 1 AND c.RnOrd = 1;

    SET @OrderStamped = @@ROWCOUNT;

    INSERT INTO Sale.[Order]
        (HtsId, [Year], VchDateMiladiDate, VchDateShamsiDate, CustomerId, VoucherTypeStatus, StatusEnum,
         BranchId, ContractNumber, HamkaranId, OrderNumber, HtsVchNo, HtsStockId, HtsBaseVoucherTypeId,
         HtsContractVchHeaderId, ConfirmerUserId, OperatorUserId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        h.HtsId, h.[Year], h.VchDateMiladiDate, h.VchDateShamsiDate, c.Id, h.VoucherTypeStatus, h.StatusEnum,
        b.Id, h.ContractNumber, h.HamkaranId, h.OrderNumber, h.HtsVchNo, h.HtsStockId, h.HtsBaseVoucherTypeId,
        h.HtsContractVchHeaderId, umc.NewUserId, umo.NewUserId,
        1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM #HtsOrder h
    LEFT JOIN SLS.Customer c ON c.HtsId = h.CustomerHtsId AND c.HtsId <> 0
    LEFT JOIN Sale.Branch b ON b.HtsId = h.BranchHtsId AND b.HtsId <> 0
    LEFT JOIN #UserMap umc ON umc.OldUserId = h.ConfirmerHtsId
    LEFT JOIN #UserMap umo ON umo.OldUserId = h.OperatorHtsId
    WHERE NOT EXISTS (
        SELECT 1 FROM Sale.[Order] o
        WHERE o.HtsId = h.HtsId AND o.HtsId <> 0);

    SET @OrderInserted = @@ROWCOUNT;

    ;WITH cand AS (
        SELECT
            d.Id,
            h.HtsId,
            h.HamkaranInvVchItmId,
            h.IsPackage,
            h.NotComplete,
            o.Id AS SaleOrderId,
            o.CustomerId,
            ROW_NUMBER() OVER (PARTITION BY h.HtsId ORDER BY d.Id) AS RnHts,
            ROW_NUMBER() OVER (PARTITION BY d.Id ORDER BY h.HtsId) AS RnDet
        FROM Sale.OrderDetail d
        INNER JOIN #HtsDetail h ON d.HamkaranId = h.HamkaranId
        LEFT JOIN Sale.[Order] o ON o.HtsId = h.OrderHtsId AND o.HtsId <> 0
        WHERE ISNULL(d.HtsId, 0) = 0
          AND NOT EXISTS (
              SELECT 1 FROM Sale.OrderDetail x
              WHERE x.HtsId = h.HtsId AND x.HtsId <> 0)
    )
    UPDATE d SET
        d.HtsId = c.HtsId,
        d.HamkaranInvVchItmId = c.HamkaranInvVchItmId,
        d.IsPackage = c.IsPackage,
        d.NotComplete = c.NotComplete,
        d.Sale_OrderId = COALESCE(c.SaleOrderId, d.Sale_OrderId),
        d.CustomerId = COALESCE(d.CustomerId, c.CustomerId),
        d.ModifiedById = 1,
        d.ModifiedByName = @SeedUser,
        d.ModifiedDateMiladiDateTime = @Now,
        d.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.OrderDetail d
    INNER JOIN cand c ON c.Id = d.Id AND c.RnHts = 1 AND c.RnDet = 1;

    SET @DetailStamped = @@ROWCOUNT;

    INSERT INTO Sale.OrderDetail
        (HtsId, Sale_OrderId, HamkaranId, HamkaranOrderHdrId, HamkaranInvVchItmId, Prefactor_VchNo, Seq, PartId,
         Qty, BaseVchItmRef, Description, UnitPrice, Price, IncPrice, DecPrice, PayMethod, PriceTemp, QtyTemp,
         IsPackage, NotComplete, CustomerId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        h.HtsId, o.Id, h.HamkaranId, h.HamkaranOrderHdrId, h.HamkaranInvVchItmId, h.Prefactor_VchNo, ISNULL(h.Seq, 0), p.Id,
        ISNULL(h.Qty, 0), h.BaseVchItmRef, h.Description, h.UnitPrice, h.Price, h.IncPrice, h.DecPrice,
        ISNULL(h.PayMethod, 0), h.PriceTemp, h.QtyTemp, h.IsPackage, h.NotComplete, o.CustomerId,
        1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM #HtsDetail h
    INNER JOIN Sale.[Order] o ON o.HtsId = h.OrderHtsId AND o.HtsId <> 0
    LEFT JOIN Inv.Part p ON p.HtsId = h.PartHtsId AND p.HtsId <> 0
    WHERE NOT EXISTS (
        SELECT 1 FROM Sale.OrderDetail d
        WHERE d.HtsId = h.HtsId AND d.HtsId <> 0);

    SET @DetailInserted = @@ROWCOUNT;

    SELECT
        CAST(p.Project_ID AS BIGINT) AS HtsId,
        NULLIF(LTRIM(RTRIM(p.Project_Code)), N'') AS Code,
        NULLIF(LTRIM(RTRIM(p.Project_Name)), N'') AS Name,
        NULLIF(LTRIM(RTRIM(p.Project_Startdate)), N'') AS ProjectStartShamsiDate,
        NULLIF(LTRIM(RTRIM(p.Project_Enddate)), N'') AS ProjectEndShamsiDate,
        NULLIF(LTRIM(RTRIM(p.Project_Description)), N'') AS Description,
        CASE WHEN p.Project_Status_FK BETWEEN 0 AND 7 THEN p.Project_Status_FK ELSE 0 END AS ProjectStatus,
        CASE WHEN p.IsActive = 1 THEN 1 ELSE 0 END AS IsActive
    INTO #HtsProject
    FROM [TMS].[TotalSystem].[dbo].[Pln_Project] p
    WHERE p.Project_ID IN (
        SELECT DISTINCT s.ProjectHtsId
        FROM #HtsSerial s
        WHERE s.ProjectHtsId IS NOT NULL AND s.ProjectHtsId <> 0
    );

    MERGE Pln.Project AS t
    USING #HtsProject AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.Code = s.Code, t.Name = s.Name,
        t.ProjectStartShamsiDate = s.ProjectStartShamsiDate,
        t.ProjectEndShamsiDate = s.ProjectEndShamsiDate,
        t.Description = s.Description, t.ProjectStatus = s.ProjectStatus,
        t.IsActive = s.IsActive,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, Code, Name, ProjectStartShamsiDate, ProjectEndShamsiDate, Description, ProjectStatus, IsActive,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES
        (s.HtsId, s.Code, s.Name, s.ProjectStartShamsiDate, s.ProjectEndShamsiDate, s.Description, s.ProjectStatus, s.IsActive,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi);

    SET @ProjectMerged = @@ROWCOUNT;

    MERGE Sale.OrderDetailSerial AS t
    USING (
        SELECT
            s.HtsId,
            d.Id AS OrderDetailId,
            c.Id AS CustomerId,
            addr.Id AS CustomerAddressId,
            pr.Id AS PlaningProjectId,
            s.Serial, s.Model, s.HoursDaily, s.Runtime, s.LifeTime,
            s.GuaranteeTime, s.GuaranteeSendDay, s.GuaranteeLaunchDay, s.HasInsurance,
            s.SendMiladiDate, s.SendShamsiDate, s.SendTime,
            s.LaunchMiladiDate, s.LaunchShamsiDate,
            s.ExitFactoryMiladiDate, s.ExitFactoryShamsiDate, s.ExitFactoryTime,
            s.ProductionOrderNumber, s.IndustrialComment, s.InvComment, s.IndustrialConfirm,
            s.FinalExitMiladiDate, s.FinalExitShamsiDate, s.ExitTimeFinal
        FROM #HtsSerial s
        INNER JOIN Sale.OrderDetail d ON d.HtsId = s.OrderDetailHtsId AND d.HtsId <> 0
        LEFT JOIN SLS.Customer c ON c.HtsId = s.CustomerHtsId AND c.HtsId <> 0
        LEFT JOIN SLS.CustomerAddress addr ON addr.HtsId = s.AddressHtsId AND addr.HtsId <> 0
        LEFT JOIN Pln.Project pr ON pr.HtsId = s.ProjectHtsId AND pr.HtsId <> 0
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.OrderDetailId = s.OrderDetailId, t.CustomerId = s.CustomerId, t.CustomerAddressId = s.CustomerAddressId,
        t.PlaningProjectId = s.PlaningProjectId,
        t.Serial = s.Serial, t.Model = s.Model, t.HoursDaily = s.HoursDaily, t.Runtime = s.Runtime, t.LifeTime = s.LifeTime,
        t.GuaranteeTime = s.GuaranteeTime, t.GuaranteeSendDay = s.GuaranteeSendDay, t.GuaranteeLaunchDay = s.GuaranteeLaunchDay,
        t.HasInsurance = s.HasInsurance, t.SendMiladiDate = s.SendMiladiDate, t.SendShamsiDate = s.SendShamsiDate, t.SendTime = s.SendTime,
        t.LaunchMiladiDate = s.LaunchMiladiDate, t.LaunchShamsiDate = s.LaunchShamsiDate,
        t.ExitFactoryMiladiDate = s.ExitFactoryMiladiDate, t.ExitFactoryShamsiDate = s.ExitFactoryShamsiDate, t.ExitFactoryTime = s.ExitFactoryTime,
        t.ProductionOrderNumber = s.ProductionOrderNumber, t.IndustrialComment = s.IndustrialComment, t.InvComment = s.InvComment,
        t.IndustrialConfirm = s.IndustrialConfirm, t.FinalExitMiladiDate = s.FinalExitMiladiDate,
        t.FinalExitShamsiDate = s.FinalExitShamsiDate, t.ExitTimeFinal = s.ExitTimeFinal,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, OrderDetailId, CustomerId, CustomerAddressId, PlaningProjectId, Serial, Model, HoursDaily, Runtime, LifeTime,
         GuaranteeTime, GuaranteeSendDay, GuaranteeLaunchDay, HasInsurance, SendMiladiDate, SendShamsiDate, SendTime,
         LaunchMiladiDate, LaunchShamsiDate, ExitFactoryMiladiDate, ExitFactoryShamsiDate, ExitFactoryTime,
         ProductionOrderNumber, IndustrialComment, InvComment, IndustrialConfirm,
         FinalExitMiladiDate, FinalExitShamsiDate, ExitTimeFinal,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.OrderDetailId, s.CustomerId, s.CustomerAddressId, s.PlaningProjectId, s.Serial, s.Model, s.HoursDaily, s.Runtime, s.LifeTime,
         s.GuaranteeTime, s.GuaranteeSendDay, s.GuaranteeLaunchDay, s.HasInsurance, s.SendMiladiDate, s.SendShamsiDate, s.SendTime,
         s.LaunchMiladiDate, s.LaunchShamsiDate, s.ExitFactoryMiladiDate, s.ExitFactoryShamsiDate, s.ExitFactoryTime,
         s.ProductionOrderNumber, s.IndustrialComment, s.InvComment, s.IndustrialConfirm,
         s.FinalExitMiladiDate, s.FinalExitShamsiDate, s.ExitTimeFinal,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

    SET @SerialMerged = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT
        @OrderStamped AS OrderStamped,
        @OrderInserted AS OrderInserted,
        @DetailStamped AS DetailStamped,
        @DetailInserted AS DetailInserted,
        @ProjectMerged AS ProjectMerged,
        @SerialMerged AS SerialMerged,
        (SELECT COUNT(*) FROM Sale.[Order] WHERE HtsId <> 0) AS OrderWithHts,
        (SELECT COUNT(*) FROM Sale.OrderDetail WHERE HtsId <> 0) AS DetailWithHts,
        (SELECT COUNT(*) FROM Sale.OrderDetailSerial WHERE HtsId <> 0) AS SerialWithHts,
        (SELECT COUNT(*) FROM #HtsOrder h WHERE NOT EXISTS (SELECT 1 FROM Sale.[Order] o WHERE o.HtsId = h.HtsId AND o.HtsId <> 0)) AS OrderStillMissing,
        (SELECT COUNT(*) FROM #HtsDetail h WHERE NOT EXISTS (SELECT 1 FROM Sale.OrderDetail d WHERE d.HtsId = h.HtsId AND d.HtsId <> 0)) AS DetailStillMissing,
        (SELECT COUNT(*) FROM #HtsSerial h WHERE NOT EXISTS (SELECT 1 FROM Sale.OrderDetailSerial t WHERE t.HtsId = h.HtsId AND t.HtsId <> 0)) AS SerialStillMissing;
END
GO

EXEC Sale.SyncOrderFromHts;
GO
