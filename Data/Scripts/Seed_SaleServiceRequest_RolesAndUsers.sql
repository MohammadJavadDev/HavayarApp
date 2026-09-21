/*
  Seed_SaleServiceRequest_RolesAndUsers.sql
  نقش و انتساب درخواست پشتیبانی از Vw_Permission + Gnr_UserGroupMember:
    137 Manage / View روی هدر
    136 ذخیره (FullAccess/New/Edit → Manage)
    158 View کارتابل؛ ShowAll = همه مناطق
    صفحه ۱۵۸: گروه + کاربر مستقیم از Gnr_PageAction_* (شامل غیرفعال)
    FullAccess گروه ۴۸/۶۹ → Manage؛ Read/Attach → View؛ perm 7 فقط مستقیم → ShowAll
    139 / 147 پیوست و قطعات
    140 / 143 ماموریت و گزارش کار
  نقش‌ها: 200063 Manage / 200064 View / 200065 ShowAll
  Idempotent. Requires [TMS] → TotalSystem.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sale-servicerequest-roles';

    PRINT N'=== [1] Roles 200063 Manage / 200064 View / 200065 ShowAll ===';
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ServiceRequest.Manage')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200063, N'Sale.ServiceRequest.Manage', N'فروش - درخواست پشتیبانی - مدیریت',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Sale.ServiceRequest.Manage';
    END
    ELSE PRINT N'  EXISTS Sale.ServiceRequest.Manage';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ServiceRequest.View')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200064, N'Sale.ServiceRequest.View', N'فروش - درخواست پشتیبانی - مشاهده',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Sale.ServiceRequest.View';
    END
    ELSE PRINT N'  EXISTS Sale.ServiceRequest.View';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ServiceRequest.ShowAll')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200065, N'Sale.ServiceRequest.ShowAll', N'فروش - درخواست پشتیبانی - نمایش همه مناطق کارتابل',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Sale.ServiceRequest.ShowAll';
    END
    ELSE PRINT N'  EXISTS Sale.ServiceRequest.ShowAll';

    PRINT N'=== [2] Controller RoleAccess ===';
    IF OBJECT_ID('tempdb..#SrAction') IS NOT NULL DROP TABLE #SrAction;
    CREATE TABLE #SrAction (
        RoleName NVARCHAR(200) NOT NULL,
        Path NVARCHAR(300) NOT NULL,
        ActionAccessType INT NOT NULL,
        ActionAccessItemType INT NOT NULL,
        EntityName NVARCHAR(200) NULL
    );

    -- مشاهده هدر / جزئیات / فرزندان
    INSERT INTO #SrAction (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, a.AType, a.IType, a.EntityName
    FROM (VALUES
        (N'/panel/sale/servicerequest/list',                    1, 1,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/dispatch',                1, 1,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/workreport',              1, 1000, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/mission',                 1, 1000, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/requestpart',             1, 1000, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/sendattachment',          1, 1000, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/fetchdata',               2, 2,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/edit',                    1, 5,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/exporttoexcel',           2, 0,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/getcustomerinfo',         2, 1000, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/getcustomeraddressinfo',  2, 1000, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequestdetail/list',              1, 1,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/cartable',          1, 1,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/fetchdata',         2, 2,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/edit',              1, 5,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/listbyparentid',    1, 1,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/getlistbyparentid', 2, 2,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/hasworkreportattachments', 2, 1000, N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/downloadworkreportattachments', 1, 1000, N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestattachment/list',         1, 1,    N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestattachment/fetchdata',    2, 2,    N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestattachment/edit',         1, 5,    N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestattachment/listbyparentid', 1, 1,  N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestattachment/getlistbyparentid', 2, 2, N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestpart/list',               1, 1,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/fetchdata',          2, 2,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/edit',               1, 5,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/listbyparentid',     1, 1,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/getlistbyparentid',  2, 2,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/fetchpieces',        2, 1000, N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/getpiecesdata',      2, 1000, N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/getrelateddetails',  2, 1000, N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/getformlookups',     2, 1000, N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestexpertmission/list',      1, 1,    N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/servicerequestexpertmission/fetchdata', 2, 2,    N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/servicerequestexpertmission/edit',      1, 5,    N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/servicerequestexpertmission/listbyparentid', 1, 1, N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/servicerequestexpertmission/getlistbyparentid', 2, 2, N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/mission/list',                          1, 1,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/mission/fetchdata',                     2, 2,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/mission/edit',                          1, 5,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/mission/listbyparentid',                1, 1,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/mission/getlistbyparentid',             2, 2,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/mission/getpersonelmissionsalaryinfo',  2, 2,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/workreport/list',                       1, 1,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/fetchdata',                  2, 2,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/exporttoexcel',              2, 0,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/edit',                       1, 5,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/listbyparentid',             1, 1,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/getlistbyparentid',          2, 2,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/getsamereports',            2, 2,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/getsamereportinfo',          2, 2,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreportattachment/list',             1, 1,    N'Entities.App.Sale.WorkReportAttachment'),
        (N'/panel/sale/workreportattachment/fetchdata',        2, 2,    N'Entities.App.Sale.WorkReportAttachment'),
        (N'/panel/sale/workreportattachment/edit',             1, 5,    N'Entities.App.Sale.WorkReportAttachment'),
        (N'/panel/sale/workreportattachment/listbyparentid',   1, 1,    N'Entities.App.Sale.WorkReportAttachment'),
        (N'/panel/sale/workreportattachment/getlistbyparentid', 2, 2,  N'Entities.App.Sale.WorkReportAttachment'),
        (N'/system/reportbuilder/viewreportbyname',            1, 1000, N'Entities.App.Sale.ServiceRequest')
    ) a(Path, AType, IType, EntityName)
    CROSS JOIN (VALUES
        (N'Sale.ServiceRequest.Manage'),
        (N'Sale.ServiceRequest.View'),
        (N'Sale.ServiceRequest.ShowAll')
    ) r(RoleName);

    -- مدیریت (نوشتن)
    INSERT INTO #SrAction (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT N'Sale.ServiceRequest.Manage', a.Path, a.AType, a.IType, a.EntityName
    FROM (VALUES
        (N'/panel/sale/servicerequest/new',            1, 4,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/save',           2, 3,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/add',            2, 4,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/update',         2, 5,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/delete',         2, 6,    N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/senddispatch',   2, 1000, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/sendworkreportattachment', 2, 1000, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequestdetail/new',      1, 4,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/save',     2, 3,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/add',      2, 4,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/update',   2, 5,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/delete',   2, 6,    N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestattachment/new',  1, 4,    N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestattachment/save', 2, 3,    N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestattachment/add',  2, 4,    N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestattachment/update', 2, 5,  N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestattachment/delete', 2, 6,  N'Entities.App.Sale.ServiceRequestAttachment'),
        (N'/panel/sale/servicerequestpart/new',        1, 4,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/save',       2, 3,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/add',        2, 4,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/update',     2, 5,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/delete',     2, 6,    N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestpart/showprice',  1, 1000, N'Entities.App.Sale.ServiceRequestPart'),
        (N'/panel/sale/servicerequestexpertmission/new', 1, 4,  N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/servicerequestexpertmission/save', 2, 3, N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/servicerequestexpertmission/add', 2, 4,  N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/servicerequestexpertmission/update', 2, 5, N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/servicerequestexpertmission/delete', 2, 6, N'Entities.App.Sale.ServiceRequestExpertMission'),
        (N'/panel/sale/mission/new',                    1, 4,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/mission/save',                   2, 3,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/mission/add',                    2, 4,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/mission/update',                 2, 5,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/mission/delete',                 2, 6,    N'Entities.App.Sale.Mission'),
        (N'/panel/sale/workreport/new',                 1, 4,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/save',                2, 3,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/add',                 2, 4,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/update',              2, 5,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/delete',              2, 6,    N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreportattachment/new',       1, 4,    N'Entities.App.Sale.WorkReportAttachment'),
        (N'/panel/sale/workreportattachment/save',      2, 3,    N'Entities.App.Sale.WorkReportAttachment'),
        (N'/panel/sale/workreportattachment/add',       2, 4,    N'Entities.App.Sale.WorkReportAttachment'),
        (N'/panel/sale/workreportattachment/update',    2, 5,    N'Entities.App.Sale.WorkReportAttachment'),
        (N'/panel/sale/workreportattachment/delete',    2, 6,    N'Entities.App.Sale.WorkReportAttachment')
    ) a(Path, AType, IType, EntityName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #SrAction a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'/panel/sale/servicerequestdetail/cartable', 1, 1, N'Entities.App.Sale.ServiceRequestDetail',
        NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'Sale.AfterSales', N'ShowAllMenus')
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = r.Id AND x.Path = N'/panel/sale/servicerequestdetail/cartable' AND x.ActionAccessType = 1
      );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.AType, a.IType, a.EntityName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
        (N'/panel/sale/servicerequest/dispatch', 1, 1, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/servicerequest/sendworkreportattachment', 2, 1000, N'Entities.App.Sale.ServiceRequest'),
        (N'/panel/sale/workreport/list', 1, 1, N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/exporttoexcel', 2, 0, N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/getsamereports', 2, 2, N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreport/getsamereportinfo', 2, 2, N'Entities.App.Sale.WorkReport'),
        (N'/panel/sale/workreportattachment/listbyparentid', 1, 1, N'Entities.App.Sale.WorkReportAttachment'),
        (N'/panel/sale/workreportattachment/getlistbyparentid', 2, 2, N'Entities.App.Sale.WorkReportAttachment')
    ) a(Path, AType, IType, EntityName)
    CROSS JOIN system.Role r
    WHERE r.Name IN (N'Sale.AfterSales', N'ShowAllMenus')
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.AType
      );
    PRINT N'  Inserted cartable RoleAccess for AfterSales/ShowAllMenus: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.AType, a.IType, a.EntityName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
        (N'/panel/sale/servicerequestdetail/hasworkreportattachments', 2, 1000, N'Entities.App.Sale.ServiceRequestDetail'),
        (N'/panel/sale/servicerequestdetail/downloadworkreportattachments', 1, 1000, N'Entities.App.Sale.ServiceRequestDetail')
    ) a(Path, AType, IType, EntityName)
    CROSS JOIN system.Role r
    WHERE r.Name IN (N'Sale.AfterSales', N'ShowAllMenus')
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.AType
      );
    PRINT N'  Inserted ZIP RoleAccess for AfterSales/ShowAllMenus: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [3] DataProfile RoleAccess ===';
    IF OBJECT_ID('tempdb..#SrProfileRole') IS NOT NULL DROP TABLE #SrProfileRole;
    CREATE TABLE #SrProfileRole (ProfileName NVARCHAR(100) NOT NULL, RoleName NVARCHAR(200) NOT NULL);
    INSERT INTO #SrProfileRole (ProfileName, RoleName) VALUES
        (N'AfterSales_ServiceRequest_List', N'Sale.ServiceRequest.Manage'),
        (N'AfterSales_ServiceRequest_List', N'Sale.ServiceRequest.View'),
        (N'AfterSales_ServiceRequest_List', N'Sale.AfterSales'),
        (N'AfterSales_ServiceRequest_List', N'ShowAllMenus'),
        (N'AfterSales_ServiceRequest_Cartable', N'Sale.ServiceRequest.Manage'),
        (N'AfterSales_ServiceRequest_Cartable', N'Sale.ServiceRequest.View'),
        (N'AfterSales_ServiceRequest_Cartable', N'Sale.ServiceRequest.ShowAll'),
        (N'AfterSales_ServiceRequest_Cartable', N'Sale.AfterSales'),
        (N'AfterSales_ServiceRequest_Cartable', N'ShowAllMenus'),
        (N'AfterSales_ServiceRequestDetail_List', N'Sale.ServiceRequest.Manage'),
        (N'AfterSales_ServiceRequestDetail_List', N'Sale.AfterSales'),
        (N'AfterSales_ServiceRequestDetail_List', N'ShowAllMenus');

    -- EntityName باید Pascal باشد و با SavedQuery.EntityFullName entیتی فعلی هم‌خوان
    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
    WHERE ra.ActionAccessType = 3
      AND q.Name IN (N'AfterSales_ServiceRequest_List', N'AfterSales_ServiceRequest_Cartable');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7,
        CASE q.Name
            WHEN N'AfterSales_ServiceRequest_Cartable' THEN N'Entities.App.Sale.ServiceRequestDetail'
            WHEN N'AfterSales_ServiceRequestDetail_List' THEN N'Entities.App.Sale.ServiceRequestDetail'
            ELSE N'Entities.App.Sale.ServiceRequest'
        END,
        q.Title, NULL, q.Id, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #SrProfileRole map
    INNER JOIN system.SavedQuery q ON q.Name = map.ProfileName
    INNER JOIN system.Role r ON r.Name = map.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = r.Id
    );
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -- Detail_List فقط اگر ردیف نباشد
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7, N'Entities.App.Sale.ServiceRequestDetail', q.Title, NULL, q.Id, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #SrProfileRole map
    INNER JOIN system.SavedQuery q ON q.Name = map.ProfileName AND q.Name = N'AfterSales_ServiceRequestDetail_List'
    INNER JOIN system.Role r ON r.Name = map.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = r.Id
    );

    PRINT N'=== [4] Menu AccessRoleIds merge + cartable path ===';
    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'AfterSalesServiceSystem');
    IF @MenuId IS NOT NULL
    BEGIN
        UPDATE system.SystemMenu
        SET Content = REPLACE(REPLACE(Content,
                N'/panel/sale/servicerequest/cartable', N'/panel/sale/servicerequestdetail/cartable'),
                N'"/panel/sale/servicerequestdetail/list"', N'"/panel/sale/servicerequestdetail/cartable"'),
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId AND Content LIKE N'%/panel/sale/servicerequest/cartable%';
        PRINT N'  Cartable path replaced: ' + CAST(@@ROWCOUNT AS nvarchar(20));

        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT j.value
        FROM system.SystemMenu m
        CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND NULLIF(LTRIM(RTRIM(j.value)), N'') IS NOT NULL;

        INSERT INTO #MenuRoles (RoleName)
        SELECT v.RoleName FROM (VALUES
            (N'Sale.ServiceRequest.Manage'), (N'Sale.ServiceRequest.View'), (N'Sale.ServiceRequest.ShowAll'),
            (N'Sale.AfterSales'), (N'ShowAllMenus')
        ) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles x WHERE x.RoleName = v.RoleName);

        DECLARE @AccessRoles NVARCHAR(MAX);
        DECLARE @AccessRoleIds NVARCHAR(MAX);
        SELECT @AccessRoles = N'[' + STUFF((
            SELECT N',"' + RoleName + N'"' FROM #MenuRoles ORDER BY RoleName
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        SELECT @AccessRoleIds = N'[' + STUFF((
            SELECT N',' + CAST(r.Id AS nvarchar(20))
            FROM #MenuRoles m INNER JOIN system.Role r ON r.Name = m.RoleName
            ORDER BY r.Id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

        UPDATE system.SystemMenu
        SET AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
        PRINT N'  Menu AccessRoleIds merged';
    END
    ELSE PRINT N'  WARN menu AfterSalesServiceSystem not found';

    PRINT N'=== [5] Assign HTS users (direct + group) pages 136/137/139/140/143/147/158 ===';
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, Source NVARCHAR(80) NOT NULL);
    IF OBJECT_ID('tempdb..#HtsGrant') IS NOT NULL DROP TABLE #HtsGrant;
    CREATE TABLE #HtsGrant (
        Page_ID INT NOT NULL,
        Permission_ID INT NOT NULL,
        User_FK INT NULL,
        AdName NVARCHAR(200) NULL,
        HtsUsername NVARCHAR(200) NULL,
        Source NVARCHAR(80) NOT NULL
    );

    INSERT INTO #HtsGrant (Page_ID, Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT p.Page_ID, p.Permission_ID, p.User_FK,
           NULLIF(LTRIM(RTRIM(p.ActiveDirectoryUsername)), N''),
           NULLIF(LTRIM(RTRIM(hu.Username)), N''),
           CASE WHEN p.UserGroup_ID IS NULL THEN N'direct' ELSE N'group:' + CAST(p.UserGroup_ID AS nvarchar(20)) END
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
    WHERE p.System_ID = 14
      AND p.Page_ID IN (136, 137, 139, 140, 143, 147, 158)
      AND p.IsActive = 1
      AND p.User_FK IS NOT NULL;

    INSERT INTO #HtsGrant (Page_ID, Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT DISTINCT gp.Page_ID, gp.Permission_ID, u.User_ID,
           NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
           NULLIF(LTRIM(RTRIM(u.Username)), N''),
           N'group-member:' + CAST(gp.UserGroup_ID AS nvarchar(20))
    FROM (
        SELECT DISTINCT Page_ID, Permission_ID, UserGroup_ID
        FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
        WHERE System_ID = 14
          AND Page_ID IN (136, 137, 139, 140, 143, 147, 158)
          AND IsActive = 1
          AND UserGroup_ID IS NOT NULL
    ) gp
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = gp.UserGroup_ID
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    WHERE NOT EXISTS (
          SELECT 1 FROM #HtsGrant x
          WHERE x.Page_ID = gp.Page_ID AND x.Permission_ID = gp.Permission_ID AND x.User_FK = u.User_ID
      );

    -- صفحه ۱۵۸ از PageAction (گروه + کاربر مستقیم)، شامل کاربران غیرفعال HTS
    INSERT INTO #HtsGrant (Page_ID, Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT DISTINCT 158, pa.Permission_FK, u.User_ID,
           NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
           NULLIF(LTRIM(RTRIM(u.Username)), N''),
           N'158-group:' + CAST(pug.UserGroup_FK AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    WHERE pa.Page_FK = 158
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant x
          WHERE x.Page_ID = 158 AND x.Permission_ID = pa.Permission_FK AND x.User_FK = u.User_ID
      );

    INSERT INTO #HtsGrant (Page_ID, Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT 158, pa.Permission_FK, u.User_ID,
           NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
           NULLIF(LTRIM(RTRIM(u.Username)), N''),
           N'158-direct'
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pau.PageAction_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pau.User_FK
    WHERE pa.Page_FK = 158
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant x
          WHERE x.Page_ID = 158 AND x.Permission_ID = pa.Permission_FK AND x.User_FK = u.User_ID
      );

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT x.Username, x.RoleName, x.Source
    FROM (
        SELECT COALESCE(g.AdName, g.HtsUsername) AS Username, N'Sale.ServiceRequest.Manage' AS RoleName, g.Source
        FROM #HtsGrant g
        WHERE (
                (g.Page_ID IN (136, 137, 139, 140, 143, 147) AND g.Permission_ID IN (2, 4, 5))
             OR (g.Page_ID = 158 AND g.Permission_ID = 2)
          )
          AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.ServiceRequest.ShowAll', g.Source
        FROM #HtsGrant g
        WHERE g.Page_ID IN (137, 158) AND g.Permission_ID = 7
          AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.ServiceRequest.View', g.Source
        FROM #HtsGrant g
        WHERE (
                (g.Page_ID IN (136, 137, 139, 140, 143, 147, 158) AND g.Permission_ID IN (2, 3, 7, 10, 20))
             OR (g.Page_ID = 158 AND g.Permission_ID = 2)
          )
          AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL
              AND NOT EXISTS (
              SELECT 1 FROM #HtsGrant f
              WHERE f.Page_ID IN (136, 137, 139, 140, 143, 147) AND f.Permission_ID IN (2, 4, 5)
                AND LOWER(COALESCE(f.AdName, f.HtsUsername)) = LOWER(COALESCE(g.AdName, g.HtsUsername))
          )
    ) x
    WHERE x.Username IS NOT NULL;

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT hu.Username, m.RoleName, m.Source + N'+htsUser'
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON LOWER(hu.ActiveDirectoryUsername) = LOWER(m.Username)
    WHERE hu.Username IS NOT NULL AND LOWER(hu.Username) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x WHERE LOWER(x.Username) = LOWER(hu.Username) AND x.RoleName = m.RoleName
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

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0;
    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName;
        SELECT TOP 1 @MapUserId = Id
        FROM system.[User] u
        WHERE LOWER(u.Username) = LOWER(@MapUsername)
           OR LOWER(REPLACE(ISNULL(u.Email, N''), N'@havayar.com', N'')) = LOWER(@MapUsername)
        ORDER BY CASE WHEN LOWER(u.Username) = LOWER(@MapUsername) THEN 0 ELSE 1 END;
        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF @MapRoleId IS NULL
            PRINT N'  MISSING ROLE: ' + @MapRoleName;
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u CROSS APPLY OPENJSON(u.RoleIds) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
        )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + @MapRoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @MapRoleName + N'"]')
                END,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END
        SET @MapUserId = NULL; SET @MapRoleId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    END
    CLOSE map_cur; DEALLOCATE map_cur;
    PRINT N'  Assigned: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already: ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing: ' + CAST(@Missing AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SaleServiceRequest_RolesAndUsers ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify roles ---';
SELECT r.Name, r.Id,
       (SELECT COUNT(*) FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND ra.Path LIKE N'/panel/sale/servicerequest%') AS Acc,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS bigint) = r.Id) AS Users
FROM system.Role r
WHERE r.Name LIKE N'Sale.ServiceRequest%'
ORDER BY r.Name;

PRINT N'--- verify cartable entity ---';
SELECT Id, Name, EntityFullName FROM system.SavedQuery
WHERE Name IN (N'AfterSales_ServiceRequest_List', N'AfterSales_ServiceRequest_Cartable');

PRINT N'--- verify menu ---';
SELECT Name,
       CASE WHEN Content LIKE N'%/panel/sale/servicerequestdetail/cartable%' THEN 1 ELSE 0 END AS HasCartablePath,
       CASE WHEN Content LIKE N'"/panel/sale/servicerequestdetail/list"' THEN 1 ELSE 0 END AS HasDetailListMenuPath,
       CASE WHEN Content LIKE N'%/panel/sale/servicerequest/cartable%' THEN 1 ELSE 0 END AS HasOldCartablePath,
       AccessRoleIds
FROM system.SystemMenu WHERE Name = N'AfterSalesServiceSystem';

PRINT N'--- missing users (HTS grant, no Havayar user) ---';
IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL
    SELECT DISTINCT m.Username, m.RoleName, m.Source
    FROM #UserRoleMap m
    WHERE NOT EXISTS (SELECT 1 FROM system.[User] u WHERE LOWER(u.Username) = LOWER(m.Username))
    ORDER BY m.RoleName, m.Username;

