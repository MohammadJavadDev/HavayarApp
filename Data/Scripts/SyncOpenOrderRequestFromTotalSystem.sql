-- ============================================
-- همگام‌سازی درخواست‌های باز (OpenOrderRequest)
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- منبع حقیقت برای فیلدهای عملیاتی: سیستم قدیم (HTS / TotalSystem)
--
-- کلید اشتراک = شناسه‌های راهکاران (همان منطق OpenOrderRequestJob):
--   OrderRowId            = PRC3.OrderItem.OrderItemID          (وقتی سفارش دارد)
--   PurchaseRequestItemId = PRC3.PurchaseRequestItem.PurchaseRequestItemID (وقتی هنوز سفارش ندارد)
--
-- ترتیب تطبیق (عین SyncOpenOrderRequestJobFromRahkaran):
--   1) PurchaseRequestItemId + OrderRowId  (هر دو از راهکاران)
--   2) اگر OrderRowId در قدیم هست ولی در جدید NULL است:
--      PurchaseRequestItemId + OrderRowId IS NULL + تاریخ درخواست خرید یکسان
--   3) اگر OrderRowId در قدیم NULL است:
--      فقط PurchaseRequestItemId (و ترجیحاً OrderRowId جدید هم NULL)
--
-- نگاشت‌ها:
--   کاربر:     Gnr_User.Username          -> system.[User].Username
--   کالا:      Inv_Part.Hamkaran_Part_FK  -> Inv.Part.HamkaranId
--              (fallback: Part_Code       -> Inv.Part.Code)
--   تفصیل:     Acc_DL.Hamkaran_Acc_DL_FK  -> FIN.DL.HamkaranId
--   سفارش ساخت: Pln_ProductionOrder.Number -> Sale.ProductionOrder.Number
--   تامین‌کننده: Gnr_ManCompany.Hamkaran_ManCompany_FK -> Gnr.Party.HamkaranId -> Gnr.Supplier
--   پرسنل:     UserCategoryId 2829 = درخواست‌کننده | 2830 = مهندسی
--
-- قبل از اجرا:
--   1) روی دیتابیس HavayarApp اجرا شود
--   2) Linked Server [TMS] به TotalSystem در دسترس باشد
--   3) جداول Sup.OpenOrderRequest / Comment / Attachment / Vpis موجود باشند
--   4) یک‌بار کوئری پیش‌نمایش (بخش Preview) را اجرا و اختلاف‌ها را بررسی کنید
--   5) بکاپ بگیرید — کامنت/پیوست رکوردهای تطبیق‌خورده جایگزین می‌شوند
-- ============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* ═══════════════════════════════════════════════
   Preview (اختیاری) — فقط SELECT، بدون تغییر داده
   کلید اشتراک = شناسه راهکاران عین جاب:
     OrderRowId = OrderItemID | بدون سفارش: PurchaseRequestItemId
   برای اجرا: این بلوک را جداگانه Uncomment کنید
   ═══════════════════════════════════════════════
;WITH IdMapPreview AS
(
    SELECT o.OpenOrderRequest_ID AS OldId, n.Id AS NewId,
           N'1:OrderItemID' AS MatchBy
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest] o
    INNER JOIN Sup.OpenOrderRequest n
        ON o.OrderRowId IS NOT NULL
       AND n.OrderRowId = o.OrderRowId
       AND n.PurchaseRequestItemId = o.PurchaseRequestItemId

    UNION ALL

    SELECT o.OpenOrderRequest_ID, n.Id, N'2:PRItem+NullOrder+Date'
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest] o
    INNER JOIN Sup.OpenOrderRequest n
        ON o.OrderRowId IS NOT NULL
       AND n.OrderRowId IS NULL
       AND n.PurchaseRequestItemId = o.PurchaseRequestItemId
       AND ISNULL(n.PurchaseRequestMiladiDate, '19000101') = ISNULL(o.PurchaseRequestDate, '19000101')
    WHERE NOT EXISTS (
        SELECT 1 FROM Sup.OpenOrderRequest x
        WHERE x.OrderRowId = o.OrderRowId
          AND x.PurchaseRequestItemId = o.PurchaseRequestItemId
    )

    UNION ALL

    SELECT o.OpenOrderRequest_ID, n.Id, N'3:PurchaseRequestItemID'
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest] o
    INNER JOIN Sup.OpenOrderRequest n
        ON o.OrderRowId IS NULL
       AND n.OrderRowId IS NULL
       AND n.PurchaseRequestItemId = o.PurchaseRequestItemId
)
SELECT
    CASE
        WHEN m.NewId IS NULL THEN N'فقط در قدیم'
        WHEN o.OpenOrderRequest_ID IS NULL THEN N'فقط در جدید'
        ELSE N'مشترک'
    END AS SyncStatus,
    m.MatchBy,
    o.OpenOrderRequest_ID AS OldId,
    COALESCE(m.NewId, n.Id) AS NewId,
    COALESCE(o.PurchaseRequestItemId, n.PurchaseRequestItemId) AS Rahkaran_PurchaseRequestItemId,
    COALESCE(o.OrderRowId, n.OrderRowId) AS Rahkaran_OrderItemID,
    CAST(ISNULL(o.Engineering_Accept, 0) AS BIT) AS OldEngAccept,
    CAST(ISNULL(n.EngineeringAccept, 0) AS BIT) AS NewEngAccept,
    CAST(ISNULL(o.IsStop, 0) AS BIT) AS OldIsStop,
    CAST(ISNULL(n.IsStop, 0) AS BIT) AS NewIsStop,
    o.OpenOrder_Status AS OldStatus,
    n.Status AS NewStatus
FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest] o
FULL OUTER JOIN IdMapPreview m ON m.OldId = o.OpenOrderRequest_ID
FULL OUTER JOIN Sup.OpenOrderRequest n ON n.Id = m.NewId
ORDER BY SyncStatus, Rahkaran_PurchaseRequestItemId, Rahkaran_OrderItemID;
*/

BEGIN TRY
    BEGIN TRANSACTION;

    /* ─────────────────────────────────────────────
       0) نگاشت‌های پایه
       ───────────────────────────────────────────── */
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    IF OBJECT_ID('tempdb..#PartMap') IS NOT NULL DROP TABLE #PartMap;
    IF OBJECT_ID('tempdb..#DlMap') IS NOT NULL DROP TABLE #DlMap;
    IF OBJECT_ID('tempdb..#PoMap') IS NOT NULL DROP TABLE #PoMap;
    IF OBJECT_ID('tempdb..#SupplierMap') IS NOT NULL DROP TABLE #SupplierMap;
    IF OBJECT_ID('tempdb..#IdMap') IS NOT NULL DROP TABLE #IdMap;
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    IF OBJECT_ID('tempdb..#PersonelJson') IS NOT NULL DROP TABLE #PersonelJson;
    IF OBJECT_ID('tempdb..#CommentFileMap') IS NOT NULL DROP TABLE #CommentFileMap;
    IF OBJECT_ID('tempdb..#AltCommentFileMap') IS NOT NULL DROP TABLE #AltCommentFileMap;
    IF OBJECT_ID('tempdb..#AttachmentFileMap') IS NOT NULL DROP TABLE #AttachmentFileMap;

    SELECT
        CAST(ou.User_ID AS INT) AS OldUserId,
        nu.Id                   AS NewUserId,
        nu.Name                 AS NewUserName,
        ou.Username             AS Username
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON nu.Username = ou.Username;

    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap(OldUserId);

    -- کالا: اول HamkaranId، در غیر این صورت Code
    SELECT
        op.Part_ID AS OldPartId,
        COALESCE(p1.Id, p2.Id) AS NewPartId
    INTO #PartMap
    FROM [TMS].[TotalSystem].[dbo].[Inv_Part] op
    LEFT JOIN Inv.Part p1
        ON op.Hamkaran_Part_FK IS NOT NULL
       AND p1.HamkaranId = op.Hamkaran_Part_FK
    LEFT JOIN Inv.Part p2
        ON p1.Id IS NULL
       AND p2.Code = op.Part_Code
    WHERE COALESCE(p1.Id, p2.Id) IS NOT NULL;

    CREATE UNIQUE CLUSTERED INDEX IX_PartMap ON #PartMap(OldPartId);

    SELECT
        CAST(ad.Acc_DL_ID AS INT) AS OldDlId,
        dl.Id                     AS NewDlId
    INTO #DlMap
    FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
    INNER JOIN FIN.DL dl
        ON dl.HamkaranId = ad.Hamkaran_Acc_DL_FK
    WHERE ad.Hamkaran_Acc_DL_FK > 0;

    CREATE UNIQUE CLUSTERED INDEX IX_DlMap ON #DlMap(OldDlId);

    -- سفارش ساخت: آخرین نسخه بر اساس Number
    ;WITH PoRanked AS
    (
        SELECT
            po.Id,
            TRY_CAST(po.Number AS INT) AS PoNumber,
            ROW_NUMBER() OVER (
                PARTITION BY TRY_CAST(po.Number AS INT)
                ORDER BY po.Id DESC
            ) AS Rn
        FROM Sale.ProductionOrder po
        WHERE TRY_CAST(po.Number AS INT) IS NOT NULL
    )
    SELECT
        oldPo.Id AS OldPoId,
        oldPo.Number AS PoNumber,
        np.Id AS NewPoId
    INTO #PoMap
    FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] oldPo
    INNER JOIN PoRanked np
        ON np.PoNumber = oldPo.Number
       AND np.Rn = 1;

    CREATE UNIQUE CLUSTERED INDEX IX_PoMap ON #PoMap(OldPoId);

    SELECT
        mc.ManCompany_ID AS OldManCompanyId,
        s.Id             AS NewSupplierId
    INTO #SupplierMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
    INNER JOIN Gnr.Party py
        ON py.HamkaranId = mc.Hamkaran_ManCompany_FK
    INNER JOIN Gnr.Supplier s
        ON s.PartyId = py.Id
    WHERE mc.Hamkaran_ManCompany_FK > 0;

    CREATE UNIQUE CLUSTERED INDEX IX_SupplierMap ON #SupplierMap(OldManCompanyId);

    DECLARE @CntUser INT = (SELECT COUNT(*) FROM #UserMap);
    DECLARE @CntPart INT = (SELECT COUNT(*) FROM #PartMap);
    DECLARE @CntDl INT = (SELECT COUNT(*) FROM #DlMap);
    DECLARE @CntPo INT = (SELECT COUNT(*) FROM #PoMap);
    DECLARE @CntSupplier INT = (SELECT COUNT(*) FROM #SupplierMap);
    PRINT CONCAT(N'UserMap=', @CntUser,
                 N' | PartMap=', @CntPart,
                 N' | DlMap=', @CntDl,
                 N' | PoMap=', @CntPo,
                 N' | SupplierMap=', @CntSupplier);

    /* ─────────────────────────────────────────────
       1) منبع قدیم + FKهای حل‌شده
       ───────────────────────────────────────────── */
    SELECT
        o.OpenOrderRequest_ID AS OldId,
        o.OrderRowId,
        CAST(o.Year AS BIGINT) AS Year,
        CAST(o.OrderNo AS BIGINT) AS OrderNo,
        o.OrderDate AS OrderShamsiDate,
        o.OrderDateInEurope AS OrderMiladiDate,
        dm.NewDlId AS DlId,
        pm.NewPartId AS PartId,
        o.NeedDate AS NeedDateShamsiDate,
        CAST(NULL AS DATETIME) AS NeedDateMiladiDate,
        o.OrderConfirmDate AS OrderConfirmShamsiDate,
        CAST(NULL AS DATETIME) AS OrderConfirmMiladiDate,
        CAST(o.SumSendQty AS BIGINT) AS SumSendQty,
        CAST(o.FactoredCount AS INT) AS FactoredCount,
        CAST(o.OrderQty AS INT) AS OrderQty,
        CAST(o.RequiredQty AS INT) AS RequiredQty,
        o.OrderItmComment AS OrderItemComment,
        CAST(ISNULL(o.IsStop, 0) AS BIT) AS IsStop,
        o.Stop_Date AS StopShamsiDate,
        CAST(o.IsDeleted AS BIT) AS IsDeleted,
        o.IsDeletedDate AS IsDeletedShamsiDate,
        CAST(o.IsForceDeletedByUser AS BIT) AS IsForceDeletedByUser,
        o.Requested_Personel_RegDate AS RequestedPersonelRegShamsiDate,
        o.Requested_Personel AS RequestedPersonel,
        o.Requested_PersonelEmail AS RequestedPersonelEmail,
        o.OpenOrder_Status AS Status,
        CAST(ISNULL(o.Changed, 0) AS BIT) AS Changed,
        CAST(ISNULL(o.Notify_Email_Requested_Personel, 0) AS BIT) AS NotifyEmailRequestedPersonel,
        o.Notify_Email_Send_Paper_Ids AS NotifyEmailSendPaperIds,
        CAST(ISNULL(o.Engineering_Accept, 0) AS BIT) AS EngineeringAccept,
        eu.NewUserId AS EngineeringAcceptUserId,
        CASE
            WHEN o.Engineering_Accept_Date IS NULL OR LTRIM(RTRIM(o.Engineering_Accept_Date)) = N'' THEN NULL
            WHEN o.Engineering_Accept_Time IS NULL OR LTRIM(RTRIM(o.Engineering_Accept_Time)) = N'' THEN o.Engineering_Accept_Date
            ELSE CONCAT(o.Engineering_Accept_Date, N' ', o.Engineering_Accept_Time)
        END AS EngineeringAcceptShamsiDateTime,
        o.EngineeringAcceptDateInEurope AS EngineeringConfirmationMiladiDateTime,
        CAST(o.IsAcceptedAutomaticallyByEngineering AS BIT) AS IsAcceptedAutomaticallyByEngineering,
        o.Completion_Date AS CompletionShamsiDate,
        o.DelaysBuyDay,
        o.ProductionOrderNumber,
        CAST(o.IsRejectedByInspection AS BIT) AS IsRejectedByInspection,
        o.FactoredDate AS FactoredMiladiDate,
        o.FactoredDateInText AS FactoredShamsiDate,
        o.Comment,
        o.PurchaseRequestItemId,
        CAST(o.PurchaseRequestNumber AS BIGINT) AS PurchaseRequestNumber,
        o.PurchaseRequestDate AS PurchaseRequestMiladiDate,
        o.PurchaseRequestDateInText AS PurchaseRequestShamsiDate,
        CAST(o.HasSalesUnitConfirmation AS BIT) AS HasSalesUnitConfirmation,
        su.NewUserId AS SalesUnitConfirmationUserId,
        o.SalesUnitConfirmationDate AS SalesUnitConfirmationMiladiDateTime,
        o.SalesUnitConfirmationDateInText AS SalesUnitConfirmationShamsiDateTime,
        o.SalesUnitConfirmationComment,
        pom.NewPoId AS ProductionOrderId,
        se.NewUserId AS SalesUnitSalesExpertId,
        sm.NewUserId AS SalesUnitSalesManagerId,
        spm.NewUserId AS SalesUnitProjectManagerId,
        rpm.NewPartId AS RelatedPartId,
        o.DeliveryVoucherDate AS DeliveryVoucherMiladiDate,
        o.DeliveryVoucherDateInText AS DeliveryVoucherShamsiDate,
        o.TemporaryInventoryVoucherDate AS TemporaryInventoryVoucherMiladiDate,
        o.TemporaryInventoryVoucherDateInText AS TemporaryInventoryVoucherShamsiDate,
        o.QcVoucherDate AS QcVoucherMiladiDate,
        o.QcVoucherDateInText AS QcVoucherShamsiDate,
        o.FinalInventoryVoucherDate AS FinalInventoryVoucherMiladiDate,
        o.FinalInventoryVoucherDateInText AS FinalInventoryVoucherShamsiDate,
        o.SupplyDate AS SupplyMiladiDate,
        o.SupplyDateInText AS SupplyShamsiDate,
        CAST(o.HasSalesUnitPrimitiveApprove AS BIT) AS HasSalesUnitPrimitiveApprove,
        CAST(ISNULL(o.IsRoutineRequest, 0) AS BIT) AS IsRoutineRequest,
        CAST(o.StopStatusId AS INT) AS StopStatus,
        CAST(o.StopCheckingStatusId AS INT) AS StopCheckingStatus,
        o.Requested_EngineeringPersonelEmail AS RequestedEngineeringPersonelEmail,
        o.Requested_EngineeringPersonel AS RequestedEngineeringPersonel,
        smap.NewSupplierId AS ManCompanyId
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest] o
    LEFT JOIN #PartMap pm ON pm.OldPartId = o.Inv_Part_FK
    LEFT JOIN #PartMap rpm ON rpm.OldPartId = o.RelatedPartId
    LEFT JOIN #DlMap dm ON dm.OldDlId = o.Acc_DL_FK
    LEFT JOIN #PoMap pom ON pom.OldPoId = o.ProductionOrderId
    LEFT JOIN #SupplierMap smap ON smap.OldManCompanyId = o.ManCompanyId
    LEFT JOIN #UserMap eu ON eu.OldUserId = o.Engineering_Accept_User_FK
    LEFT JOIN #UserMap su ON su.OldUserId = o.SalesUnitConfirmationUserId
    LEFT JOIN #UserMap se ON se.OldUserId = o.SalesUnitSalesExpertId
    LEFT JOIN #UserMap sm ON sm.OldUserId = o.SalesUnitSalesManagerId
    LEFT JOIN #UserMap spm ON spm.OldUserId = o.SalesUnitProjectManagerId;

    CREATE UNIQUE CLUSTERED INDEX IX_Src ON #Src(OldId);
    CREATE NONCLUSTERED INDEX IX_Src_Key ON #Src(PurchaseRequestItemId, OrderRowId);

    DECLARE @SkippedNoPart INT =
    (
        SELECT COUNT(*) FROM #Src WHERE PartId IS NULL
    );
    IF @SkippedNoPart > 0
        PRINT CONCAT(N'هشدار: ', @SkippedNoPart, N' رکورد به‌خاطر نبود کالا در سیستم جدید رد می‌شوند.');

    /* ─────────────────────────────────────────────
       2) نگاشت Id قدیم -> جدید
          عین OpenOrderRequestJob / SyncOpenOrderRequestJobFromRahkaran:
          شناسه راهکاران = OrderItemID (OrderRowId) و PurchaseRequestItemID
       ───────────────────────────────────────────── */
    CREATE TABLE #IdMap
    (
        OldId BIGINT NOT NULL PRIMARY KEY,
        NewId BIGINT NOT NULL,
        MatchBy NVARCHAR(40) NOT NULL
    );

    -- اولویت 1: OrderRowId = Rahkaran OrderItemID + PurchaseRequestItemId
    INSERT INTO #IdMap (OldId, NewId, MatchBy)
    SELECT s.OldId, n.Id, N'1:OrderItemID'
    FROM #Src s
    INNER JOIN Sup.OpenOrderRequest n
        ON s.OrderRowId IS NOT NULL
       AND n.OrderRowId = s.OrderRowId
       AND n.PurchaseRequestItemId = s.PurchaseRequestItemId
    WHERE s.PartId IS NOT NULL;

    PRINT CONCAT(N'تطبیق اولویت1 (OrderItemID راهکاران): ', @@ROWCOUNT);

    -- اولویت 2: قدیم OrderRowId دارد، جدید هنوز NULL — با تاریخ درخواست خرید
    INSERT INTO #IdMap (OldId, NewId, MatchBy)
    SELECT s.OldId, n.Id, N'2:PRItem+NullOrder+Date'
    FROM #Src s
    INNER JOIN Sup.OpenOrderRequest n
        ON s.OrderRowId IS NOT NULL
       AND n.OrderRowId IS NULL
       AND n.PurchaseRequestItemId = s.PurchaseRequestItemId
       AND ISNULL(n.PurchaseRequestMiladiDate, '19000101') = ISNULL(s.PurchaseRequestMiladiDate, '19000101')
    WHERE s.PartId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.OldId = s.OldId)
      AND NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.NewId = n.Id);

    PRINT CONCAT(N'تطبیق اولویت2 (PRItem + OrderRow NULL + Date): ', @@ROWCOUNT);

    -- اولویت 3: بدون سفارش — فقط PurchaseRequestItemId راهکاران
    INSERT INTO #IdMap (OldId, NewId, MatchBy)
    SELECT s.OldId, n.Id, N'3:PurchaseRequestItemID'
    FROM #Src s
    INNER JOIN Sup.OpenOrderRequest n
        ON s.OrderRowId IS NULL
       AND n.OrderRowId IS NULL
       AND n.PurchaseRequestItemId = s.PurchaseRequestItemId
    WHERE s.PartId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.OldId = s.OldId)
      AND NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.NewId = n.Id);

    PRINT CONCAT(N'تطبیق اولویت3 (PurchaseRequestItemID راهکاران): ', @@ROWCOUNT);

    DECLARE @CntMapped INT = (SELECT COUNT(*) FROM #IdMap);
    PRINT CONCAT(N'مجموع تطبیق‌خورده برای UPDATE: ', @CntMapped);

    /* ─────────────────────────────────────────────
       3) UPDATE فیلدهای سیستم قدیم روی رکوردهای موجود
       ───────────────────────────────────────────── */
    UPDATE n SET
        n.OrderRowId = s.OrderRowId,
        n.Year = s.Year,
        n.OrderNo = s.OrderNo,
        n.OrderShamsiDate = s.OrderShamsiDate,
        n.OrderMiladiDate = s.OrderMiladiDate,
        n.DlId = s.DlId,
        n.PartId = s.PartId,
        n.NeedDateShamsiDate = s.NeedDateShamsiDate,
        n.OrderConfirmShamsiDate = s.OrderConfirmShamsiDate,
        n.SumSendQty = s.SumSendQty,
        n.FactoredCount = s.FactoredCount,
        n.OrderQty = s.OrderQty,
        n.RequiredQty = s.RequiredQty,
        n.OrderItemComment = s.OrderItemComment,
        n.IsStop = s.IsStop,
        n.StopShamsiDate = s.StopShamsiDate,
        n.IsDeleted = s.IsDeleted,
        n.IsDeletedShamsiDate = s.IsDeletedShamsiDate,
        n.IsForceDeletedByUser = s.IsForceDeletedByUser,
        n.RequestedPersonelRegShamsiDate = s.RequestedPersonelRegShamsiDate,
        n.RequestedPersonel = s.RequestedPersonel,
        n.RequestedPersonelEmail = s.RequestedPersonelEmail,
        n.Status = s.Status,
        n.Changed = s.Changed,
        n.NotifyEmailRequestedPersonel = s.NotifyEmailRequestedPersonel,
        n.NotifyEmailSendPaperIds = s.NotifyEmailSendPaperIds,
        n.EngineeringAccept = s.EngineeringAccept,
        n.EngineeringAcceptUserId = s.EngineeringAcceptUserId,
        n.EngineeringAcceptShamsiDateTime = s.EngineeringAcceptShamsiDateTime,
        n.EngineeringConfirmationMiladiDateTime = s.EngineeringConfirmationMiladiDateTime,
        n.IsAcceptedAutomaticallyByEngineering = s.IsAcceptedAutomaticallyByEngineering,
        n.CompletionShamsiDate = s.CompletionShamsiDate,
        n.DelaysBuyDay = s.DelaysBuyDay,
        n.ProductionOrderNumber = s.ProductionOrderNumber,
        n.IsRejectedByInspection = s.IsRejectedByInspection,
        n.FactoredMiladiDate = s.FactoredMiladiDate,
        n.FactoredShamsiDate = s.FactoredShamsiDate,
        n.Comment = s.Comment,
        n.PurchaseRequestNumber = s.PurchaseRequestNumber,
        n.PurchaseRequestMiladiDate = s.PurchaseRequestMiladiDate,
        n.PurchaseRequestShamsiDate = s.PurchaseRequestShamsiDate,
        n.HasSalesUnitConfirmation = s.HasSalesUnitConfirmation,
        n.SalesUnitConfirmationUserId = s.SalesUnitConfirmationUserId,
        n.SalesUnitConfirmationMiladiDateTime = s.SalesUnitConfirmationMiladiDateTime,
        n.SalesUnitConfirmationShamsiDateTime = s.SalesUnitConfirmationShamsiDateTime,
        n.SalesUnitConfirmationComment = s.SalesUnitConfirmationComment,
        n.ProductionOrderId = s.ProductionOrderId,
        n.SalesUnitSalesExpertId = s.SalesUnitSalesExpertId,
        n.SalesUnitSalesManagerId = s.SalesUnitSalesManagerId,
        n.SalesUnitProjectManagerId = s.SalesUnitProjectManagerId,
        n.RelatedPartId = s.RelatedPartId,
        n.DeliveryVoucherMiladiDate = s.DeliveryVoucherMiladiDate,
        n.DeliveryVoucherShamsiDate = s.DeliveryVoucherShamsiDate,
        n.TemporaryInventoryVoucherMiladiDate = s.TemporaryInventoryVoucherMiladiDate,
        n.TemporaryInventoryVoucherShamsiDate = s.TemporaryInventoryVoucherShamsiDate,
        n.QcVoucherMiladiDate = s.QcVoucherMiladiDate,
        n.QcVoucherShamsiDate = s.QcVoucherShamsiDate,
        n.FinalInventoryVoucherMiladiDate = s.FinalInventoryVoucherMiladiDate,
        n.FinalInventoryVoucherShamsiDate = s.FinalInventoryVoucherShamsiDate,
        n.SupplyMiladiDate = s.SupplyMiladiDate,
        n.SupplyShamsiDate = s.SupplyShamsiDate,
        n.HasSalesUnitPrimitiveApprove = s.HasSalesUnitPrimitiveApprove,
        n.IsRoutineRequest = s.IsRoutineRequest,
        n.StopStatus = s.StopStatus,
        n.StopCheckingStatus = s.StopCheckingStatus,
        n.RequestedEngineeringPersonelEmail = s.RequestedEngineeringPersonelEmail,
        n.RequestedEngineeringPersonel = s.RequestedEngineeringPersonel,
        n.ManCompanyId = s.ManCompanyId,
        n.ModifiedDateMiladiDateTime = GETDATE(),
        n.ModifiedByName = N'SyncFromTotalSystem'
    FROM Sup.OpenOrderRequest n
    INNER JOIN #IdMap m ON m.NewId = n.Id
    INNER JOIN #Src s ON s.OldId = m.OldId;

    PRINT CONCAT(N'UPDATE شد: ', @@ROWCOUNT);

    /* ─────────────────────────────────────────────
       4) INSERT رکوردهایی که فقط در قدیم هستند
       ───────────────────────────────────────────── */
    INSERT INTO Sup.OpenOrderRequest
    (
        OrderRowId, Year, OrderNo, OrderShamsiDate, OrderMiladiDate, DlId, PartId,
        NeedDateShamsiDate, OrderConfirmShamsiDate, SumSendQty, FactoredCount, OrderQty, RequiredQty,
        OrderItemComment, IsStop, StopShamsiDate, IsDeleted, IsDeletedShamsiDate, IsForceDeletedByUser,
        RequestedPersonelRegShamsiDate, RequestedPersonel, RequestedPersonelEmail, Status, Changed,
        NotifyEmailRequestedPersonel, NotifyEmailSendPaperIds, EngineeringAccept, EngineeringAcceptUserId,
        EngineeringAcceptShamsiDateTime, EngineeringConfirmationMiladiDateTime, IsAcceptedAutomaticallyByEngineering,
        CompletionShamsiDate, DelaysBuyDay, ProductionOrderNumber, IsRejectedByInspection,
        FactoredMiladiDate, FactoredShamsiDate, Comment, PurchaseRequestItemId, PurchaseRequestNumber,
        PurchaseRequestMiladiDate, PurchaseRequestShamsiDate, HasSalesUnitConfirmation,
        SalesUnitConfirmationUserId, SalesUnitConfirmationMiladiDateTime, SalesUnitConfirmationShamsiDateTime,
        SalesUnitConfirmationComment, ProductionOrderId, SalesUnitSalesExpertId, SalesUnitSalesManagerId,
        SalesUnitProjectManagerId, RelatedPartId, DeliveryVoucherMiladiDate, DeliveryVoucherShamsiDate,
        TemporaryInventoryVoucherMiladiDate, TemporaryInventoryVoucherShamsiDate, QcVoucherMiladiDate,
        QcVoucherShamsiDate, FinalInventoryVoucherMiladiDate, FinalInventoryVoucherShamsiDate,
        SupplyMiladiDate, SupplyShamsiDate, HasSalesUnitPrimitiveApprove, IsRoutineRequest,
        StopStatus, StopCheckingStatus, RequestedEngineeringPersonelEmail, RequestedEngineeringPersonel,
        ManCompanyId, CreatedOnMiladiDateTime, CreatedByName, IsActive
    )
    SELECT
        s.OrderRowId, s.Year, s.OrderNo, s.OrderShamsiDate, s.OrderMiladiDate, s.DlId, s.PartId,
        s.NeedDateShamsiDate, s.OrderConfirmShamsiDate, s.SumSendQty, s.FactoredCount, s.OrderQty, s.RequiredQty,
        s.OrderItemComment, s.IsStop, s.StopShamsiDate, s.IsDeleted, s.IsDeletedShamsiDate, s.IsForceDeletedByUser,
        s.RequestedPersonelRegShamsiDate, s.RequestedPersonel, s.RequestedPersonelEmail, s.Status, s.Changed,
        s.NotifyEmailRequestedPersonel, s.NotifyEmailSendPaperIds, s.EngineeringAccept, s.EngineeringAcceptUserId,
        s.EngineeringAcceptShamsiDateTime, s.EngineeringConfirmationMiladiDateTime, s.IsAcceptedAutomaticallyByEngineering,
        s.CompletionShamsiDate, s.DelaysBuyDay, s.ProductionOrderNumber, s.IsRejectedByInspection,
        s.FactoredMiladiDate, s.FactoredShamsiDate, s.Comment, s.PurchaseRequestItemId, s.PurchaseRequestNumber,
        s.PurchaseRequestMiladiDate, s.PurchaseRequestShamsiDate, s.HasSalesUnitConfirmation,
        s.SalesUnitConfirmationUserId, s.SalesUnitConfirmationMiladiDateTime, s.SalesUnitConfirmationShamsiDateTime,
        s.SalesUnitConfirmationComment, s.ProductionOrderId, s.SalesUnitSalesExpertId, s.SalesUnitSalesManagerId,
        s.SalesUnitProjectManagerId, s.RelatedPartId, s.DeliveryVoucherMiladiDate, s.DeliveryVoucherShamsiDate,
        s.TemporaryInventoryVoucherMiladiDate, s.TemporaryInventoryVoucherShamsiDate, s.QcVoucherMiladiDate,
        s.QcVoucherShamsiDate, s.FinalInventoryVoucherMiladiDate, s.FinalInventoryVoucherShamsiDate,
        s.SupplyMiladiDate, s.SupplyShamsiDate, s.HasSalesUnitPrimitiveApprove, s.IsRoutineRequest,
        s.StopStatus, s.StopCheckingStatus, s.RequestedEngineeringPersonelEmail, s.RequestedEngineeringPersonel,
        s.ManCompanyId, GETDATE(), N'SyncFromTotalSystem', 1
    FROM #Src s
    WHERE s.PartId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.OldId = s.OldId);

    -- تکمیل #IdMap برای رکوردهای تازه‌درج‌شده (با همان کلید راهکاران)
    INSERT INTO #IdMap (OldId, NewId, MatchBy)
    SELECT s.OldId, n.Id, N'4:AfterInsert'
    FROM #Src s
    INNER JOIN Sup.OpenOrderRequest n
        ON (
                (s.OrderRowId IS NOT NULL AND n.OrderRowId = s.OrderRowId AND n.PurchaseRequestItemId = s.PurchaseRequestItemId)
             OR (s.OrderRowId IS NULL AND n.OrderRowId IS NULL AND n.PurchaseRequestItemId = s.PurchaseRequestItemId)
           )
    WHERE s.PartId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM #IdMap m WHERE m.OldId = s.OldId);

    DECLARE @CntMappedAfterInsert INT = (SELECT COUNT(*) FROM #IdMap);
    PRINT CONCAT(N'مجموع نگاشت Id پس از INSERT: ', @CntMappedAfterInsert);

    /* ─────────────────────────────────────────────
       5) پرسنل درخواست‌کننده / مهندسی -> JSON List<long>
       ───────────────────────────────────────────── */
    ;WITH PersonelAgg AS
    (
        SELECT
            m.NewId,
            CONCAT(N'[', STRING_AGG(CAST(um.NewUserId AS NVARCHAR(30)), N',') WITHIN GROUP (ORDER BY um.NewUserId), N']') AS IdsJson,
            rp.UserCategoryId
        FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_Requested_Personel] rp
        INNER JOIN #IdMap m ON m.OldId = rp.OpenOrderRequest_FK
        INNER JOIN #UserMap um ON um.OldUserId = rp.RequestedPersonel_FK
        WHERE rp.UserCategoryId IN (2829, 2830)
        GROUP BY m.NewId, rp.UserCategoryId
    )
    SELECT
        NewId,
        MAX(CASE WHEN UserCategoryId = 2829 THEN IdsJson END) AS RequestedPersonelIds,
        MAX(CASE WHEN UserCategoryId = 2830 THEN IdsJson END) AS RequestedEngineeringPersonelIds
    INTO #PersonelJson
    FROM PersonelAgg
    GROUP BY NewId;

    UPDATE n SET
        n.RequestedPersonelIds = NULL,
        n.RequestedEngineeringPersonelIds = NULL
    FROM Sup.OpenOrderRequest n
    INNER JOIN #IdMap m ON m.NewId = n.Id;

    UPDATE n SET
        n.RequestedPersonelIds = pj.RequestedPersonelIds,
        n.RequestedEngineeringPersonelIds = pj.RequestedEngineeringPersonelIds
    FROM Sup.OpenOrderRequest n
    INNER JOIN #PersonelJson pj ON pj.NewId = n.Id;

    PRINT CONCAT(N'به‌روزرسانی پرسنل JSON: ', @@ROWCOUNT);

    /* ─────────────────────────────────────────────
       6) پاکسازی فرزندهای قبلی رکوردهای همگام‌شده
       ───────────────────────────────────────────── */
    DELETE v
    FROM Sup.OpenOrderRequestVpis v
    INNER JOIN #IdMap m ON m.NewId = v.OpenOrderRequestId;

    DELETE c
    FROM Sup.OpenOrderRequestComment c
    INNER JOIN #IdMap m ON m.NewId = c.OpenOrderRequestId;

    DELETE a
    FROM Sup.OpenOrderRequestAttachment a
    INNER JOIN #IdMap m ON m.NewId = a.OpenOrderRequestId;

    /* ─────────────────────────────────────────────
       7) FileEntity برای پیوست کامنت‌ها
       ───────────────────────────────────────────── */
    CREATE TABLE #CommentFileMap
    (
        OldCommentId INT NOT NULL PRIMARY KEY,
        NewFileId BIGINT NOT NULL
    );

    CREATE TABLE #AltCommentFileMap
    (
        OldCommentId INT NOT NULL PRIMARY KEY,
        NewFileId BIGINT NOT NULL
    );

    INSERT INTO [system].[FileEntity]
    (
        PhysicalPath, OriginalName, ContentType, Size,
        EntityType, EntityPropName, EntityId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, IsActive
    )
    OUTPUT
        CAST(inserted.EntityId AS INT),
        inserted.Id
    INTO #CommentFileMap (OldCommentId, NewFileId)
    SELECT
        COALESCE(NULLIF(LTRIM(RTRIM(c.AttachmentFilePath)), N''), N'migration/open-order-request/no-file') AS PhysicalPath,
        COALESCE(NULLIF(LTRIM(RTRIM(c.AttachmentFileName)), N''), N'migration-placeholder.bin') AS OriginalName,
        CASE
            WHEN c.AttachmentFileName LIKE N'%.pdf' THEN N'application/pdf'
            WHEN c.AttachmentFileName LIKE N'%.zip' THEN N'application/zip'
            WHEN c.AttachmentFileName LIKE N'%.rar' THEN N'application/x-rar-compressed'
            ELSE N'application/octet-stream'
        END AS ContentType,
        ISNULL(c.Attachment_FileSize, 0) AS Size,
        N'Entities.App.Sup.OpenOrderRequestComment' AS EntityType,
        N'Attachment' AS EntityPropName,
        CAST(c.OpenOrderRequestComment_ID AS BIGINT) AS EntityId,
        um.NewUserId,
        um.NewUserName,
        COALESCE(c.CreatedDateInEurope, c.Comment_DateInLatin, GETDATE()),
        1
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_Comment] c
    INNER JOIN #IdMap m ON m.OldId = c.OpenOrderRequest_FK
    LEFT JOIN #UserMap um ON um.OldUserId = c.CreatedUser_FK
    WHERE NULLIF(LTRIM(RTRIM(c.AttachmentFilePath)), N'') IS NOT NULL
       OR NULLIF(LTRIM(RTRIM(c.AttachmentFileName)), N'') IS NOT NULL;

    INSERT INTO [system].[FileEntity]
    (
        PhysicalPath, OriginalName, ContentType, Size,
        EntityType, EntityPropName, EntityId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, IsActive
    )
    OUTPUT
        CAST(inserted.EntityId AS INT),
        inserted.Id
    INTO #AltCommentFileMap (OldCommentId, NewFileId)
    SELECT
        COALESCE(NULLIF(LTRIM(RTRIM(c.AlternativeAttachmentFilePath)), N''), N'migration/open-order-request/no-file') AS PhysicalPath,
        COALESCE(NULLIF(LTRIM(RTRIM(c.AlternativeAttachmentFileName)), N''), N'migration-placeholder.bin') AS OriginalName,
        CASE
            WHEN c.AlternativeAttachmentFileName LIKE N'%.pdf' THEN N'application/pdf'
            WHEN c.AlternativeAttachmentFileName LIKE N'%.zip' THEN N'application/zip'
            WHEN c.AlternativeAttachmentFileName LIKE N'%.rar' THEN N'application/x-rar-compressed'
            ELSE N'application/octet-stream'
        END AS ContentType,
        ISNULL(c.AlternativeAttachment_FileSize, 0) AS Size,
        N'Entities.App.Sup.OpenOrderRequestComment' AS EntityType,
        N'AlternativeAttachment' AS EntityPropName,
        CAST(c.OpenOrderRequestComment_ID AS BIGINT) AS EntityId,
        um.NewUserId,
        um.NewUserName,
        COALESCE(c.StopCheckingDate, c.CreatedDateInEurope, GETDATE()),
        1
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_Comment] c
    INNER JOIN #IdMap m ON m.OldId = c.OpenOrderRequest_FK
    LEFT JOIN #UserMap um ON um.OldUserId = c.CreatedUser_FK
    WHERE NULLIF(LTRIM(RTRIM(c.AlternativeAttachmentFilePath)), N'') IS NOT NULL
       OR NULLIF(LTRIM(RTRIM(c.AlternativeAttachmentFileName)), N'') IS NOT NULL;

    /* ─────────────────────────────────────────────
       8) کامنت‌ها (+ ریمپ BeneficiariesIds)
       ───────────────────────────────────────────── */
    INSERT INTO Sup.OpenOrderRequestComment
    (
        OpenOrderRequestId, MiladiDate, ShamsiDate, IsStop, IsLaunched,
        HasSalesUnitConfirmation, SalesUnitConfirmationComment, CommentValue,
        StopType, StopOperatorId, BeneficiariesIds, BeneficiariesNames,
        ResponseDeadlineMiladiDate, ResponseDeadlineShamsiDate, StopOperatorComment,
        AttachmentId, StopCheckingResult, StopCheckingMiladiDateTime, StopCheckingShamsiDateTime,
        StopCheckingComment, AlternativeAttachmentId, StopCheckingDelayReasonText,
        IsNeedToUpdateBom, IsNeedToDeleteBom,
        ProjectManagerApproximateCommentMiladiDate, ProjectManagerApproximateCommentShamsiDate,
        StopCheckingStatus, StopCheckingStatusComment, AdditionalDescription,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, IsActive
    )
    SELECT
        m.NewId,
        c.Comment_DateInLatin,
        c.Comment_Date,
        CAST(ISNULL(c.IsStop, 0) AS BIT),
        CAST(ISNULL(c.IsLaunch, 0) AS BIT),
        CAST(c.HasSalesUnitConfirmation AS BIT),
        c.SalesUnitConfirmationComment,
        c.Comment_Value,
        CAST(c.StopTypeId AS INT),
        so.NewUserId,
        ben.NewBeneficiariesIds,
        ben.NewBeneficiariesNames,
        c.ResponseDeadlineDate,
        c.ResponseDeadlineDateInText,
        c.StopOperatorComment,
        cf.NewFileId,
        CAST(c.StopCheckingResultId AS INT),
        c.StopCheckingDate,
        c.StopCheckingDateInText,
        c.StopCheckingComment,
        af.NewFileId,
        c.StopCheckingDelayReasonText,
        CAST(c.IsNeedToUpdateBom AS BIT),
        CAST(c.IsNeedToDeleteBom AS BIT),
        c.ProjectManagerApproximateCommentDate,
        c.ProjectManagerApproximateCommentDateInText,
        CAST(c.StopCheckingStatusId AS INT),
        c.StopCheckingStatusComment,
        c.AdditionalDescription,
        cu.NewUserId,
        cu.NewUserName,
        COALESCE(c.CreatedDateInEurope, c.Comment_DateInLatin, GETDATE()),
        1
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_Comment] c
    INNER JOIN #IdMap m ON m.OldId = c.OpenOrderRequest_FK
    LEFT JOIN #UserMap cu ON cu.OldUserId = c.CreatedUser_FK
    LEFT JOIN #UserMap so ON so.OldUserId = c.StopOperatorId
    LEFT JOIN #CommentFileMap cf ON cf.OldCommentId = c.OpenOrderRequestComment_ID
    LEFT JOIN #AltCommentFileMap af ON af.OldCommentId = c.OpenOrderRequestComment_ID
    OUTER APPLY
    (
        SELECT
            STRING_AGG(CAST(um2.NewUserId AS NVARCHAR(30)), N',') WITHIN GROUP (ORDER BY um2.NewUserId) AS NewBeneficiariesIds,
            STRING_AGG(um2.NewUserName, N',') WITHIN GROUP (ORDER BY um2.NewUserId) AS NewBeneficiariesNames
        FROM STRING_SPLIT(ISNULL(c.BeneficiariesIds, N''), N',') ss
        INNER JOIN #UserMap um2
            ON um2.OldUserId = TRY_CAST(LTRIM(RTRIM(ss.value)) AS INT)
        WHERE LTRIM(RTRIM(ss.value)) <> N''
    ) ben;

    PRINT CONCAT(N'کامنت درج شد: ', @@ROWCOUNT);

    /* ─────────────────────────────────────────────
       9) پیوست‌های درخواست باز
       ───────────────────────────────────────────── */
    CREATE TABLE #AttachmentFileMap
    (
        OldAttachmentId INT NOT NULL PRIMARY KEY,
        NewFileId BIGINT NOT NULL
    );

    INSERT INTO [system].[FileEntity]
    (
        PhysicalPath, OriginalName, ContentType, Size,
        EntityType, EntityPropName, EntityId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, IsActive
    )
    OUTPUT
        CAST(inserted.EntityId AS INT),
        inserted.Id
    INTO #AttachmentFileMap (OldAttachmentId, NewFileId)
    SELECT
        COALESCE(NULLIF(LTRIM(RTRIM(a.AttachmentFilePath)), N''), N'migration/open-order-request/no-file') AS PhysicalPath,
        COALESCE(NULLIF(LTRIM(RTRIM(a.Attachment_FileName)), N''), N'migration-placeholder.bin') AS OriginalName,
        CASE
            WHEN a.Attachment_FileName LIKE N'%.pdf' THEN N'application/pdf'
            WHEN a.Attachment_FileName LIKE N'%.zip' THEN N'application/zip'
            WHEN a.Attachment_FileName LIKE N'%.rar' THEN N'application/x-rar-compressed'
            ELSE N'application/octet-stream'
        END AS ContentType,
        ISNULL(a.Attachment_FileSize, 0) AS Size,
        N'Entities.App.Sup.OpenOrderRequestAttachment' AS EntityType,
        N'Attachment' AS EntityPropName,
        CAST(a.OpenOrderRequest_Attachment_ID AS BIGINT) AS EntityId,
        um.NewUserId,
        um.NewUserName,
        GETDATE(),
        1
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_Attachment] a
    INNER JOIN #IdMap m ON m.OldId = a.OpenOrderRequest_FK
    LEFT JOIN #UserMap um ON um.OldUserId = a.CreatedUser_FK;

    INSERT INTO Sup.OpenOrderRequestAttachment
    (
        OpenOrderRequestId, AttachmentId, Comment, FileType,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, IsActive
    )
    SELECT
        m.NewId,
        fm.NewFileId,
        a.Attachment_Comment,
        CAST(a.DocumentTypeId AS INT),
        um.NewUserId,
        um.NewUserName,
        GETDATE(),
        1
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_Attachment] a
    INNER JOIN #IdMap m ON m.OldId = a.OpenOrderRequest_FK
    LEFT JOIN #AttachmentFileMap fm ON fm.OldAttachmentId = a.OpenOrderRequest_Attachment_ID
    LEFT JOIN #UserMap um ON um.OldUserId = a.CreatedUser_FK;

    PRINT CONCAT(N'پیوست درج شد: ', @@ROWCOUNT);

    /* ─────────────────────────────────────────────
       10) VPIS (فقط اگر Project / ProjectVpis / Document در سیستم جدید موجود باشند)
       ───────────────────────────────────────────── */
    INSERT INTO Sup.OpenOrderRequestVpis
    (
        OpenOrderRequestId, ProjectId, ProjectVpisId, DocumentId,
        RevisionNumber, IsLatest, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, IsActive
    )
    SELECT
        m.NewId,
        CAST(v.ProjectId AS BIGINT),
        CAST(v.ProjectVpisId AS BIGINT),
        CAST(v.DocumentId AS BIGINT),
        CAST(v.RevisionNumber AS INT),
        CAST(v.IsLatest AS BIT),
        v.Comment,
        cu.NewUserId,
        cu.NewUserName,
        v.CreatedDate,
        uu.NewUserId,
        uu.NewUserName,
        v.UpdatedDate,
        1
    FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequestVpis] v
    INNER JOIN #IdMap m ON m.OldId = v.OpenOrderRequestId
    INNER JOIN Edms.Project p ON p.Id = v.ProjectId
    INNER JOIN Edms.ProjectVpis pv ON pv.Id = v.ProjectVpisId
    LEFT JOIN Edms.Document d ON d.Id = v.DocumentId
    LEFT JOIN #UserMap cu ON cu.OldUserId = v.CreatedUserId
    LEFT JOIN #UserMap uu ON uu.OldUserId = v.UpdatedUserId
    WHERE v.DocumentId IS NULL OR d.Id IS NOT NULL;

    PRINT CONCAT(N'VPIS درج شد: ', @@ROWCOUNT);

    /* ─────────────────────────────────────────────
       11) گزارش خلاصه
       ───────────────────────────────────────────── */
    SELECT
        (SELECT COUNT(*) FROM #Src) AS OldTotal,
        (SELECT COUNT(*) FROM #Src WHERE PartId IS NULL) AS SkippedNoPart,
        (SELECT COUNT(*) FROM #IdMap) AS MappedTotal,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequest) AS NewTotalAfterSync,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequestComment c INNER JOIN #IdMap m ON m.NewId = c.OpenOrderRequestId) AS SyncedComments,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequestAttachment a INNER JOIN #IdMap m ON m.NewId = a.OpenOrderRequestId) AS SyncedAttachments,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequestVpis v INNER JOIN #IdMap m ON m.NewId = v.OpenOrderRequestId) AS SyncedVpis;

    COMMIT TRANSACTION;
    PRINT N'همگام‌سازی OpenOrderRequest با موفقیت انجام شد.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'خطا در همگام‌سازی OpenOrderRequest (خط %d): %s', 16, 1, @Line, @Err);
END CATCH;
