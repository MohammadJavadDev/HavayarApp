/*
  Seed_HrManagers_ImplementedRoleAccess.sql
  مدیران سازمانی (سمت منابع انسانی: مدیر / جانشین مدیر / قائم مقام / رییس هیئت مدیره / مدیرعامل)
  دسترسی HTS آن‌ها روی صفحات پیاده‌سازی‌شده → نقش‌های موجود Havayar.

  صفحات پیاده‌سازی‌نشده نادیده گرفته می‌شوند. نقش جدید ساخته نمی‌شود.
  Idempotent. نیازمند Linked Server [TMS] → TotalSystem.
  Encoding: UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    THROW 51001, N'Linked Server [TMS] یافت نشد.', 1;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-hr-managers-implemented-access';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] مدیران از سمت منابع انسانی ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Managers') IS NOT NULL DROP TABLE #Managers;
    CREATE TABLE #Managers
    (
        User_FK INT NOT NULL PRIMARY KEY,
        HtsUsername NVARCHAR(200) NULL,
        AdName NVARCHAR(200) NULL,
        OrgPostTitle NVARCHAR(200) NULL
    );

    INSERT INTO #Managers (User_FK, HtsUsername, AdName, OrgPostTitle)
    SELECT DISTINCT
        u.User_ID,
        NULLIF(LTRIM(RTRIM(u.Username)), N''),
        NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
        op.OrgPost_Title
    FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] p
    INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_OrgPost] op ON op.OrgPost_ID = p.OrgPost_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u
        ON u.Personel_FK = p.Personel_ID AND u.IsActive = 1
    WHERE p.IsActive = 1
      AND p.IsExternal_Person = 0
      AND (
            op.OrgPost_Title LIKE N'مدیر%'
         OR op.OrgPost_Title LIKE N'جانشین مدیر%'
         OR op.OrgPost_Title LIKE N'قائم مقام%'
         OR op.OrgPost_Title LIKE N'رییس هیئت مدیره%'
         OR op.OrgPost_Title LIKE N'رئیس هیئت مدیره%'
         OR op.OrgPost_Title LIKE N'%مدیرعامل%'
      )
      AND op.OrgPost_Title NOT LIKE N'%دفتر%'
      AND op.OrgPost_Title NOT LIKE N'%دستیار%';

    PRINT N'  Managers: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [2] گرنت‌های HTS مدیران روی صفحات پیاده‌سازی‌شده ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#HtsGrant') IS NOT NULL DROP TABLE #HtsGrant;
    CREATE TABLE #HtsGrant
    (
        Page_ID INT NOT NULL,
        Permission_ID INT NOT NULL,
        UserGroup_ID INT NULL,
        User_FK INT NOT NULL,
        AdName NVARCHAR(200) NULL,
        HtsUsername NVARCHAR(200) NULL,
        Source NVARCHAR(80) NOT NULL
    );

    INSERT INTO #HtsGrant (Page_ID, Permission_ID, UserGroup_ID, User_FK, AdName, HtsUsername, Source)
    SELECT
        vp.Page_ID, vp.Permission_ID, vp.UserGroup_ID, vp.User_FK,
        m.AdName, m.HtsUsername,
        CASE WHEN vp.UserGroup_ID IS NULL THEN N'direct' ELSE N'group:' + CAST(vp.UserGroup_ID AS nvarchar(20)) END
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] vp
    INNER JOIN #Managers m ON m.User_FK = vp.User_FK
    WHERE vp.IsActive = 1
      AND vp.Permission_ID <> 1
      AND vp.Page_ID IN (
            70, 71, 72, 73, 74, 76, 77, 104,
            132, 133, 136, 137, 139, 140, 141, 143, 147, 151, 158,
            206, 214, 216, 226, 234, 270,
            326, 327, 328, 334, 341, 343, 348, 349, 354, 355, 356,
            362, 370, 371, 372, 373, 414, 418, 419,
            446, 450, 463, 469, 493, 503, 508, 536, 537, 538, 546
      );

    INSERT INTO #HtsGrant (Page_ID, Permission_ID, UserGroup_ID, User_FK, AdName, HtsUsername, Source)
    SELECT DISTINCT pa.Page_FK, pa.Permission_FK, pug.UserGroup_FK, mgr.User_FK, mgr.AdName, mgr.HtsUsername,
           N'live-group:' + CAST(pug.UserGroup_FK AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] gm ON gm.UserGroup_FK = pug.UserGroup_FK
    INNER JOIN #Managers mgr ON mgr.User_FK = gm.User_FK
    WHERE pa.Permission_FK <> 1
      AND pa.Page_FK IN (
            70, 71, 72, 73, 74, 76, 77, 104,
            132, 133, 136, 137, 139, 140, 141, 143, 147, 151, 158,
            206, 214, 216, 226, 234, 270,
            326, 327, 328, 334, 341, 343, 348, 349, 354, 355, 356,
            362, 370, 371, 372, 373, 414, 418, 419,
            446, 450, 463, 469, 493, 503, 508, 536, 537, 538, 546
      )
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant x
          WHERE x.Page_ID = pa.Page_FK AND x.Permission_ID = pa.Permission_FK AND x.User_FK = mgr.User_FK
      );

    INSERT INTO #HtsGrant (Page_ID, Permission_ID, UserGroup_ID, User_FK, AdName, HtsUsername, Source)
    SELECT pa.Page_FK, pa.Permission_FK, NULL, pau.User_FK, mgr.AdName, mgr.HtsUsername, N'live-direct'
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pau.PageAction_FK
    INNER JOIN #Managers mgr ON mgr.User_FK = pau.User_FK
    WHERE pa.Permission_FK <> 1
      AND pa.Page_FK IN (
            70, 71, 72, 73, 74, 76, 77, 104,
            132, 133, 136, 137, 139, 140, 141, 143, 147, 151, 158,
            206, 214, 216, 226, 234, 270,
            326, 327, 328, 334, 341, 343, 348, 349, 354, 355, 356,
            362, 370, 371, 372, 373, 414, 418, 419,
            446, 450, 463, 469, 493, 503, 508, 536, 537, 538, 546
      )
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant x
          WHERE x.Page_ID = pa.Page_FK AND x.Permission_ID = pa.Permission_FK AND x.User_FK = mgr.User_FK
      );

    DECLARE @GrantCount int = (SELECT COUNT(*) FROM #HtsGrant);
    PRINT N'  HTS grant rows: ' + CAST(@GrantCount AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [3] نگاشت صفحه/مجوز → نقش موجود ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#PermMap') IS NOT NULL DROP TABLE #PermMap;
    CREATE TABLE #PermMap (Page_ID INT NOT NULL, Permission_ID INT NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #PermMap (Page_ID, Permission_ID, RoleName) VALUES
        -- تامین: درخواست باز / تنظیمات / پیشینه / دسته خرید / LeadTime
        (74, 2, N'SupplyAndPurchase'),
        (74, 7, N'Sup.OpenOrderRequest.ShowAll'),
        (74, 12, N'Sup.OpenOrderRequest.EngineeringAccept'),
        (74, 22, N'Sup.OpenOrderRequest.Stop'),
        (74, 23, N'Sup.OpenOrderRequest.Start'),
        (74, 24, N'Sup.OpenOrderRequest.Sending'),
        (74, 25, N'Sup.OpenOrderRequest.Query'),
        (74, 82, N'Sup.OpenOrderRequest.HasEngineering'),
        (74, 101, N'Sup.OpenOrderRequest.Terminate'),
        (74, 187, N'Sup.OpenOrderRequest.SalesOrProjectAccept'),
        (76, 2, N'Sup.OpenOrderRequest.ConfigManage'),
        (77, 2, N'Sup.OpenOrderRequest.HistoryView'),
        (77, 3, N'Sup.OpenOrderRequest.HistoryView'),
        (70, 2, N'SupplyAndPurchase'),
        (414, 2, N'SupplyAndPurchase'),
        (414, 4, N'SupplyAndPurchase'),
        (414, 5, N'SupplyAndPurchase'),

        -- حواله / سریال
        (132, 2, N'Sale.OrderDetail.Manage'),
        (132, 2, N'Sale.OrderDetail.ViewWithPrice'),
        (132, 2, N'Sale.OrderDetail.ExportToExcel'),
        (132, 8, N'Sale.OrderDetail.ViewWithPrice'),
        (132, 10, N'Sale.OrderDetail.ExportToExcel'),
        (132, 3, N'Sale.OrderDetail.View'),
        (133, 2, N'Sale.OrderDetail.Manage'),
        (133, 4, N'Sale.OrderDetail.Manage'),
        (133, 5, N'Sale.OrderDetail.Manage'),

        -- درخواست پشتیبانی / ماموریت / گزارش کار
        (136, 2, N'Sale.ServiceRequest.Manage'),
        (136, 4, N'Sale.ServiceRequest.Manage'),
        (136, 5, N'Sale.ServiceRequest.Manage'),
        (137, 2, N'Sale.ServiceRequest.Manage'),
        (137, 4, N'Sale.ServiceRequest.Manage'),
        (137, 5, N'Sale.ServiceRequest.Manage'),
        (137, 7, N'Sale.ServiceRequest.ShowAll'),
        (139, 2, N'Sale.ServiceRequest.Manage'),
        (140, 2, N'Sale.ServiceRequest.Manage'),
        (140, 5, N'Sale.ServiceRequest.Manage'),
        (143, 2, N'Sale.ServiceRequest.Manage'),
        (143, 4, N'Sale.ServiceRequest.Manage'),
        (143, 5, N'Sale.ServiceRequest.Manage'),
        (147, 2, N'Sale.ServiceRequest.Manage'),
        (158, 2, N'Sale.ServiceRequest.Manage'),
        (158, 2, N'Sale.ServiceRequest.ShowAll'),
        (158, 7, N'Sale.ServiceRequest.ShowAll'),

        -- حق ماموریت
        (141, 2, N'Sale.MissionSalary.Manage'),
        (141, 3, N'Sale.MissionSalary.View'),

        -- مانیتورینگ
        (151, 2, N'Sale.AfterSales'),
        (151, 3, N'Sale.AfterSales.Monitoring.View'),
        (151, 5, N'Sale.AfterSales.Monitoring.Edit'),
        (151, 53, N'Sale.AfterSales.Monitoring.IndustrialConfirm'),
        (151, 188, N'Sale.AfterSales.Monitoring.Exit'),
        (151, 189, N'Sale.AfterSales.Monitoring.Inventory'),

        -- گارانتی / سایت / مسئول منطقه / درخواست مشتری
        (206, 2, N'Sale.AfterSales.CustomerAllowedGuarantee.Manage'),
        (206, 3, N'Sale.AfterSales.CustomerAllowedGuarantee.View'),
        (234, 2, N'Sale.AfterSales.CustomerAddress.Manage'),
        (234, 4, N'Sale.AfterSales.CustomerAddress.Manage'),
        (234, 5, N'Sale.AfterSales.CustomerAddress.Manage'),
        (234, 3, N'Sale.AfterSales.CustomerAddress.View'),
        (356, 2, N'Sale.AfterSales.ResponsibleZone.Manage'),
        (356, 4, N'Sale.AfterSales.ResponsibleZone.Manage'),
        (356, 5, N'Sale.AfterSales.ResponsibleZone.Manage'),
        (356, 3, N'Sale.AfterSales.ResponsibleZone.View'),
        (419, 2, N'Sale.AfterSales.CustomerRequest.Manage'),
        (419, 4, N'Sale.AfterSales.CustomerRequest.Manage'),
        (419, 5, N'Sale.AfterSales.CustomerRequest.Manage'),
        (419, 3, N'Sale.AfterSales.CustomerRequest.View'),

        -- تعمیر
        (270, 2, N'Rpr.RepairRequest.ShowAll'),
        (270, 3, N'Rpr.RepairRequest.View'),
        (270, 7, N'Rpr.RepairRequest.ShowAll'),
        (270, 70, N'Rpr.RepairRequest.AfterSale'),
        (270, 71, N'Rpr.RepairRequest.Repairs'),
        (270, 130, N'Rpr.RepairRequest.Imaging'),

        -- مناقصه
        (493, 3, N'Sale.Tenders.View'),
        (493, 4, N'Sale.Tenders.View'),
        (493, 2, N'Sale.Tenders.ShowAll'),
        (493, 7, N'Sale.Tenders.ShowAll'),
        (493, 151, N'Sale.Tenders.ShowAll'),
        (493, 152, N'Sale.Tenders.CompressedAir'),
        (493, 153, N'Sale.Tenders.OilGas'),
        (493, 154, N'Sale.Tenders.Approver'),
        (503, 2, N'Sale.Tenders.Approver'),
        (503, 3, N'Sale.Tenders.View'),
        (503, 7, N'Sale.Tenders.Manager'),
        (503, 122, N'Sale.Tenders.Manager'),
        (503, 123, N'Sale.Tenders.Approver'),

        -- سفارش ساخت / اقلام
        (214, 2, N'Sale.ProductionOrder.ShowAll'),
        (214, 3, N'Sale.ProductionOrderItem.ViewRelated'),
        (214, 51, N'Sale.ProductionOrder.FinancialConfirm'),
        (214, 53, N'Sale.ProductionOrderItem.AcceptIndustrial'),
        (214, 119, N'Sale.ProductionOrderItem.ReadyForShipping'),
        (214, 136, N'Planning.ProductionOrderItem.Swap'),
        (214, 137, N'Sale.ProductionOrderItem.SupplyCommitteeBoss'),
        (214, 138, N'Sale.ProductionOrderItem.Inquirer'),
        (214, 139, N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer'),
        (463, 7, N'Sale.ProductionOrder.ShowAll'),
        (463, 3, N'Sale.ProductionOrder.ViewRelated'),
        (463, 5, N'Sale.ProductionOrder.ViewRelated'),

        -- آموزش
        (326, 2, N'Trn.FullAccess'), (326, 3, N'Trn.ReadOnly'),
        (327, 2, N'Trn.FullAccess'), (327, 3, N'Trn.ReadOnly'),
        (328, 2, N'Trn.FullAccess'), (328, 3, N'Trn.ReadOnly'),
        (334, 2, N'Trn.FullAccess'), (334, 3, N'Trn.ReadOnly'),
        (341, 2, N'Trn.FullAccess'), (341, 3, N'Trn.ReadOnly'),
        (343, 2, N'Trn.FullAccess'), (343, 3, N'Trn.ReadOnly'),
        (348, 2, N'Trn.FullAccess'), (348, 3, N'Trn.ReadOnly'),
        (349, 2, N'Trn.FullAccess'), (349, 3, N'Trn.ReadOnly'),
        (354, 2, N'Trn.FullAccess'), (354, 3, N'Trn.ReadOnly'),
        (355, 2, N'Trn.FullAccess'), (355, 3, N'Trn.ReadOnly'),
        (370, 2, N'Trn.FullAccess'), (370, 3, N'Trn.ReadOnly'),
        (371, 2, N'Trn.FullAccess'), (371, 3, N'Trn.ReadOnly'),
        (372, 2, N'Trn.FullAccess'), (372, 3, N'Trn.ReadOnly'),
        (373, 2, N'Trn.FullAccess'), (373, 3, N'Trn.ReadOnly'),

        -- اسناد فرآیندی
        (446, 7, N'Bpm.ProcessDocument.ShowAll'),
        (446, 128, N'Bpm.ProcessDocument.Supervisor'),
        (446, 129, N'Bpm.ProcessDocument.Expert'),
        (446, 143, N'Bpm.ProcessDocument.ViewAllAttachments'),
        (450, 3, N'Bpm.ProcessDocument.PublishedList'),
        (450, 7, N'Bpm.ProcessDocument.ShowAll'),

        -- توقف / تاخیر / کسری
        (536, 3, N'Prd.StopRequst.Requester'),
        (536, 177, N'Prd.StopRequst.Requester'),
        (537, 7, N'Prd.StopsManagement.ShowAll'),
        (537, 178, N'Prd.StopsManagement.ProductionExpert'),
        (537, 179, N'Prd.StopsManagement.ResponsibleUsers'),
        (537, 180, N'Prd.StopsManagement.CftTeam'),
        (537, 184, N'Prd.StopsManagement.QcManager'),
        (362, 2, N'Pln.ProductionOrderDelay.FullAccess'),
        (362, 4, N'Pln.ProductionOrderDelay.FullAccess'),
        (362, 5, N'Pln.ProductionOrderDelay.FullAccess'),
        (362, 6, N'Pln.ProductionOrderDelay.FullAccess'),
        (546, 69, N'Pln.ProductionOrerItemDeficit.ImportExcell');

    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleName NVARCHAR(200) NOT NULL,
        Source NVARCHAR(120) NOT NULL
    );

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT COALESCE(g.AdName, g.HtsUsername), m.RoleName,
           g.Source + N' p' + CAST(g.Page_ID AS nvarchar(10)) + N'.' + CAST(g.Permission_ID AS nvarchar(10))
    FROM #HtsGrant g
    INNER JOIN #PermMap m ON m.Page_ID = g.Page_ID AND m.Permission_ID = g.Permission_ID
    WHERE COALESCE(g.AdName, g.HtsUsername) IS NOT NULL;

    -- هر مجوز صفحه 74 جز بدون‌دسترسی → مشاهده درخواست باز
    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT COALESCE(g.AdName, g.HtsUsername), N'Sup.OpenOrderRequest.View', g.Source + N' p74-any'
    FROM #HtsGrant g
    WHERE g.Page_ID = 74 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL;

    -- مشاهده پشتیبانی فقط اگر Manage ندارند
    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT COALESCE(g.AdName, g.HtsUsername), N'Sale.ServiceRequest.View', g.Source + N' srv-view'
    FROM #HtsGrant g
    WHERE g.Page_ID IN (136, 137, 139, 140, 143, 147, 158)
      AND g.Permission_ID IN (2, 3, 7, 10, 20)
      AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant f
          WHERE f.User_FK = g.User_FK
            AND f.Page_ID IN (136, 137, 139, 140, 143, 147)
            AND f.Permission_ID IN (2, 4, 5)
      );

    -- مشاهده حواله فقط اگر FullAccess/Manage ندارند
    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT COALESCE(g.AdName, g.HtsUsername), N'Sale.OrderDetail.View', g.Source + N' od-view'
    FROM #HtsGrant g
    WHERE g.Page_ID = 132 AND g.Permission_ID = 3
      AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant f
          WHERE f.User_FK = g.User_FK AND f.Page_ID = 132 AND f.Permission_ID = 2
      );

    -- آموزش: ReadOnly را از کسانی که FullAccess دارند بردار
    DELETE v
    FROM #UserRoleMap v
    WHERE v.RoleName = N'Trn.ReadOnly'
      AND EXISTS (
          SELECT 1 FROM #UserRoleMap f
          WHERE LOWER(f.Username) = LOWER(v.Username) AND f.RoleName = N'Trn.FullAccess'
      );

    -- گروه ۶۹ مدیران خدمات پس از فروش + اعلان خروج
    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT COALESCE(mgr.AdName, mgr.HtsUsername),
           CASE gm.UserGroup_FK
                WHEN 69 THEN N'Sale.AfterSales'
                WHEN 542 THEN N'Sale.AfterSales.FactoryExitNotify'
                WHEN 570 THEN N'Sale.AfterSales.FactoryExitCustomerCall'
                WHEN 27 THEN N'Sup.OpenOrderRequest.Supply'
                WHEN 28 THEN N'Sup.OpenOrderRequest.Industrial'
           END,
           N'hts-group:' + CAST(gm.UserGroup_FK AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] gm
    INNER JOIN #Managers mgr ON mgr.User_FK = gm.User_FK
    WHERE gm.UserGroup_FK IN (27, 28, 69, 542, 570)
      AND COALESCE(mgr.AdName, mgr.HtsUsername) IS NOT NULL;

    -- نام کاربری جایگزین HTS / AD
    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT hu.Username, m.RoleName, m.Source + N'+htsUser'
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON LOWER(hu.ActiveDirectoryUsername) = LOWER(m.Username)
    WHERE hu.Username IS NOT NULL AND LOWER(hu.Username) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(hu.Username) AND x.RoleName = m.RoleName
      );

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT N'dr.h-ghoroori', m.RoleName, m.Source + N'+alias-ghoroori'
    FROM #UserRoleMap m
    WHERE LOWER(m.Username) = N'drghoroori'
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = N'dr.h-ghoroori' AND x.RoleName = m.RoleName
      );

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT hu.ActiveDirectoryUsername, m.RoleName, m.Source + N'+htsAd'
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON LOWER(hu.Username) = LOWER(m.Username)
    WHERE hu.ActiveDirectoryUsername IS NOT NULL AND LOWER(hu.ActiveDirectoryUsername) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(hu.ActiveDirectoryUsername) AND x.RoleName = m.RoleName
      );

    ;WITH d AS (
        SELECT Username, RoleName, ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleName ORDER BY Username) AS rn
        FROM #UserRoleMap
    )
    DELETE FROM d WHERE rn > 1;

    DELETE FROM #UserRoleMap
    WHERE NOT EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = #UserRoleMap.RoleName);

    DECLARE @MapCount int = (SELECT COUNT(*) FROM #UserRoleMap);
    PRINT N'  Mapped user-role rows: ' + CAST(@MapCount AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4] اعطای نقش‌های جاافتاده ===';
    -----------------------------------------------------------------------------
    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0, @MissingRole int = 0;

    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Username, RoleName FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT TOP 1 @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName ORDER BY Id;
        SELECT TOP 1 @MapUserId = Id
        FROM system.[User] u
        WHERE LOWER(u.Username) = LOWER(@MapUsername)
           OR LOWER(REPLACE(ISNULL(u.Email, N''), N'@havayar.com', N'')) = LOWER(@MapUsername)
        ORDER BY CASE WHEN LOWER(u.Username) = LOWER(@MapUsername) THEN 0 ELSE 1 END;

        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' / ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF @MapRoleId IS NULL
        BEGIN
            SET @MissingRole = @MissingRole + 1;
        END
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(CASE WHEN ISJSON(ISNULL(u.RoleIds, N'[]')) = 1 THEN u.RoleIds ELSE N'[]' END) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
        )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]' OR ISJSON(RoleIds) = 0
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN ISJSON(ISNULL(Roles, N'[]')) = 1
                     AND EXISTS (SELECT 1 FROM OPENJSON(Roles) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]' OR ISJSON(Roles) = 0
                        THEN N'["' + @MapRoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @MapRoleName + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END

        SET @MapUserId = NULL;
        SET @MapRoleId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    PRINT N'  Assigned: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already had: ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing users: ' + CAST(@Missing AS nvarchar(20));
    PRINT N'  Missing roles skipped: ' + CAST(@MissingRole AS nvarchar(20));

    -- مشاهده مانیتورینگ را از ویرایش/دسترسی کامل بردار
    DECLARE @ViewRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales.Monitoring.View');
    DECLARE @EditRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales.Monitoring.Edit');
    DECLARE @AfterSalesRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales');
    DECLARE @ViewStripped int = 0;

    IF @ViewRoleId IS NOT NULL AND @EditRoleId IS NOT NULL AND @AfterSalesRoleId IS NOT NULL
    BEGIN
        DECLARE @StripUserId bigint, @StripRoleIds nvarchar(max), @StripRoles nvarchar(max);
        DECLARE strip_cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT u.Id, u.RoleIds, u.Roles
            FROM system.[User] u
            WHERE EXISTS (
                SELECT 1 FROM OPENJSON(CASE WHEN ISJSON(ISNULL(u.RoleIds, N'[]')) = 1 THEN u.RoleIds ELSE N'[]' END) j
                WHERE TRY_CAST(j.value AS bigint) = @ViewRoleId
            )
            AND EXISTS (
                SELECT 1 FROM OPENJSON(CASE WHEN ISJSON(ISNULL(u.RoleIds, N'[]')) = 1 THEN u.RoleIds ELSE N'[]' END) j
                WHERE TRY_CAST(j.value AS bigint) IN (@EditRoleId, @AfterSalesRoleId)
            )
            AND EXISTS (
                SELECT 1 FROM #Managers mgr
                WHERE LOWER(COALESCE(mgr.AdName, mgr.HtsUsername)) = LOWER(u.Username)
                   OR LOWER(mgr.HtsUsername) = LOWER(u.Username)
            );
        OPEN strip_cur;
        FETCH NEXT FROM strip_cur INTO @StripUserId, @StripRoleIds, @StripRoles;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @StripRoleIds = N'[' + STUFF((
                SELECT N',' + j.value
                FROM OPENJSON(CASE WHEN ISJSON(ISNULL(@StripRoleIds, N'[]')) = 1 THEN @StripRoleIds ELSE N'[]' END) j
                WHERE TRY_CAST(j.value AS bigint) <> @ViewRoleId
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
            IF @StripRoleIds IS NULL OR @StripRoleIds = N'[]'
                SET @StripRoleIds = N'[]';

            SELECT @StripRoles = N'[' + STUFF((
                SELECT N',"' + REPLACE(j.value, N'"', N'\"') + N'"'
                FROM OPENJSON(CASE WHEN ISJSON(ISNULL(@StripRoles, N'[]')) = 1 THEN @StripRoles ELSE N'[]' END) j
                WHERE j.value <> N'Sale.AfterSales.Monitoring.View'
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
            IF @StripRoles IS NULL
                SET @StripRoles = N'[]';

            UPDATE system.[User]
            SET RoleIds = @StripRoleIds,
                Roles = @StripRoles,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @StripUserId;

            SET @ViewStripped = @ViewStripped + 1;
            FETCH NEXT FROM strip_cur INTO @StripUserId, @StripRoleIds, @StripRoles;
        END
        CLOSE strip_cur;
        DEALLOCATE strip_cur;
    END
    PRINT N'  Stripped Monitoring.View from Edit/AfterSales managers: ' + CAST(@ViewStripped AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_HrManagers_ImplementedRoleAccess committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR: Seed_HrManagers_ImplementedRoleAccess rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify managers vs implemented roles ---';
SELECT u.Username, r.Name AS RoleName
FROM system.[User] u
CROSS APPLY OPENJSON(CASE WHEN ISJSON(ISNULL(u.RoleIds, N'[]')) = 1 THEN u.RoleIds ELSE N'[]' END) j
INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS bigint)
WHERE r.Name NOT IN (N'AllMenus', N'ShowAllMenus')
  AND LOWER(u.Username) IN (
        N'ahrarnejad.h', N'azadbakhsh.s', N'bagheri.h', N'bagheri.m', N'dordab.y',
        N'dr.h-ghoroori', N'erfani.k', N'fazeli.e', N'golestaneh.a', N'habibi.m',
        N'kadkhodaei.v', N'kargar.m', N'khalili.aa', N'khodakarami.f', N'khorshidi.m',
        N'm.nikpour', N'mahani.b', N'maranaki.m', N'mehrabi.h', N'mehrafzoon',
        N'moeini.m', N'mohammadian.b', N'mohammadian.m', N'naghdi.f', N'naghizadeh.m',
        N'rajabi.m', N'sajdeh.n', N'salim.sh', N'sh.abed', N'sharifi.m',
        N'shirnejad.a', N'taheran.f', N'taheran.fa'
  )
ORDER BY u.Username, r.Name;
