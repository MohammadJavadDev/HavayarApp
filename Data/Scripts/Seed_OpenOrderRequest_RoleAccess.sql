/*
================================================================================
Seed_OpenOrderRequest_RoleAccess.sql   (WP4 — سخت‌سازی دسترسی «درخواست های باز»)
================================================================================
دسترسی اکشن‌های OpenOrderRequestController (system.RoleAccess) مطابق PageAction های HTS
صفحه 74 «درخواست های باز همکاران سیستم»، صفحه 71 «پیوست درخواست های باز» و صفحه 76 «تنظیمات».
همان اکشن‌ها در کنترلر با [ActionDisplayName] وارد کاتالوگ دسترسی شده‌اند و نقش را دوباره چک می‌کنند.

  HTS PageAction                         → مسیر جدید (lowercase)                                         → نقش‌ها
  ------------------------------------------------------------------------------------------------------------------------
  74·Read(3)                             → /panel/sup/openorderrequest/list, /fetchdata, /edit            → همه نقش‌های بیننده (V)
  74·ExportToExcel(10)                   → /panel/sup/openorderrequest/exporttoexcel                      → SupplyAndPurchase, EngineeringAccept, Industrial, Supply, ShowAll, View, HistoryView
  71·Read/ViewAttachment                 → /attachmentlistpartial, /downloadattachment                    → V
  74·Edit(5) (DoOperation=ثبت کامنت)     → /savecomment                                                   → SupplyAndPurchase, Supply, EngineeringAccept, Industrial(*)
  74·Accept_Engineering(12)              → /engineeringaccept/{id}                                        → EngineeringAccept   (WithoutCheckFullAccess)
  74·SalesOrProjectPermission(187)       → /salesorprojectaccept                                          → SalesOrProjectAccept (+ تطبیق هویت در کنترلر)
  74·Stop(22) + مراحل DoStopOperation     → /stopoperation                                                 → V  (کنترلر: ثبت اولیه=Stop، بررسی=عامل توقف، تصمیم=ثبت‌کننده)
  74·Start(23)                           → /triggering/{id}                                               → Start, SupplyAndPurchase
  74·Sending(24)                         → /setinwaystatus/{id}                                           → Sending, SupplyAndPurchase (HTS: فقط FullAccess)
  74·Query(25)                           → /statusinquiry/{id}                                            → Query, SupplyAndPurchase   (HTS: فقط FullAccess)
  74·Terminate(101)                      → /terminate/{id}  و  /delete (خاتمه نرم — D35)                 → Terminate, SupplyAndPurchase
  74·HasEngineeringPermission(82)        → /dolinkvpis                                                    → HasEngineering, EngineeringAccept, SupplyAndPurchase (D42)
  71·New/Edit/Delete (FullAccess 71)     → /saveattachment, /deleteattachment                             → SupplyAndPurchase, Supply, EngineeringAccept
  76·FullAccess (DoOperation پرسنل)       → /addrequestedpersoneltoopenrequest                             → ConfigManage, Industrial, SupplyAndPurchase
  74·Read (گروه‌های فقط‌مشاهده 442/172/…) → نقش جدید Sup.OpenOrderRequest.View (100013) + dataProfile «مشاهده عمومی»
  77·Read (گروه‌های 395/44/70/28)        → نقش جدید Sup.OpenOrderRequest.HistoryView (100014) + dataProfile «پیشینه درخواست ها» (D39)
  (هیچ) 74·New/Delete                    → /new, /add, /save, /update, /delete برای همه نقش‌ها جز ShowAllMenus حذف می‌شود (D35)

  (*) Industrial در RoleAccess فعلی Edit/Update/Save دارد → معادل Edit(5) تلقی شده (گروه 28 HTS خودش Edit نداشت).
  V = SupplyAndPurchase, EngineeringAccept, SalesOrProjectAccept, Stop, Start, Sending, Query, Terminate, HasEngineering,
      ShowAll, Industrial, Supply, ConfigManage, View, HistoryView, Industries(نقش 3 قدیمی), ShowAllMenus

Idempotent. RoleId از system.Role با Name حل می‌شود (بدون Id ثابت جز دو نقش جدید که مثل Role.cs در بازه 1000xx درج می‌شوند).
پس از اجرا: WebApp را ری‌استارت کنید (کاتالوگ دسترسی و کش پروفایل‌ها در استارت‌آپ ساخته می‌شود).
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-oor-roleaccess-wp4';
DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sup.OpenOrderRequest';
DECLARE @Base NVARCHAR(100) = N'/panel/sup/openorderrequest';

BEGIN TRY
    BEGIN TRANSACTION;

    -----------------------------------------------------------------------------
    PRINT N'=== [1] نقش‌های مشاهده (D39): 100013 View، 100014 HistoryView ===';
    -----------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sup.OpenOrderRequest.View')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (100013, N'Sup.OpenOrderRequest.View', N'تامین و خرید -درخواست های باز - مشاهده ',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=100013 Sup.OpenOrderRequest.View';
    END
    ELSE
        PRINT N'  EXISTS Role Sup.OpenOrderRequest.View';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sup.OpenOrderRequest.HistoryView')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (100014, N'Sup.OpenOrderRequest.HistoryView', N'تامین و خرید -درخواست های باز - مشاهده پیشینه ',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=100014 Sup.OpenOrderRequest.HistoryView';
    END
    ELSE
        PRINT N'  EXISTS Role Sup.OpenOrderRequest.HistoryView';

    -----------------------------------------------------------------------------
    PRINT N'=== [2] D35: حذف دسترسی ایجاد/ویرایش فیلد/حذف فیزیکی (HTS روی 74 New/Delete نداشت) ===';
    -----------------------------------------------------------------------------
    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.ActionAccessType <> 3
      AND ra.Path IN (@Base + N'/new', @Base + N'/add', @Base + N'/save', @Base + N'/update', @Base + N'/delete')
      AND r.Name <> N'ShowAllMenus';
    PRINT N'  Removed RoleAccess rows (new/add/save/update/delete): ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [3] ردیف‌های RoleAccess اکشن‌ها ===';
    -----------------------------------------------------------------------------
    DECLARE @Rows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, RoleName NVARCHAR(200));

    -- نقش‌های بیننده (V)
    DECLARE @Viewers TABLE (RoleName NVARCHAR(200));
    INSERT INTO @Viewers (RoleName) VALUES
        (N'SupplyAndPurchase'),
        (N'Sup.OpenOrderRequest.EngineeringAccept'),
        (N'Sup.OpenOrderRequest.SalesOrProjectAccept'),
        (N'Sup.OpenOrderRequest.Stop'),
        (N'Sup.OpenOrderRequest.Start'),
        (N'Sup.OpenOrderRequest.Sending'),
        (N'Sup.OpenOrderRequest.Query'),
        (N'Sup.OpenOrderRequest.Terminate'),
        (N'Sup.OpenOrderRequest.HasEngineering'),
        (N'Sup.OpenOrderRequest.ShowAll'),
        (N'Sup.OpenOrderRequest.Industrial'),
        (N'Sup.OpenOrderRequest.Supply'),
        (N'Sup.OpenOrderRequest.ConfigManage'),
        (N'Sup.OpenOrderRequest.View'),
        (N'Sup.OpenOrderRequest.HistoryView'),
        (N'Industries'),
        (N'ShowAllMenus');

    -- 3.1  74·Read → صفحه/لیست/ویرایش(نمایش) + 71·Read/ViewAttachment → لیست و دانلود پیوست + /stopoperation (قاعده مرحله در کنترلر)
    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName)
    SELECT a.Path, a.AccessType, a.ItemType, v.RoleName
    FROM (VALUES
        (@Base + N'/list',                  1, 1),
        (@Base + N'/fetchdata',             2, 2),
        (@Base + N'/edit',                  1, 5),
        (@Base + N'/attachmentlistpartial', 1, 1000),
        (@Base + N'/downloadattachment',    2, 1000),
        (@Base + N'/stopoperation',         2, 1000)
    ) a(Path, AccessType, ItemType)
    CROSS JOIN @Viewers v;

    -- 3.2  74·ExportToExcel(10)
    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName)
    SELECT @Base + N'/exporttoexcel', 2, 0, r.RoleName
    FROM (VALUES
        (N'SupplyAndPurchase'),
        (N'Sup.OpenOrderRequest.EngineeringAccept'),
        (N'Sup.OpenOrderRequest.Industrial'),
        (N'Sup.OpenOrderRequest.Supply'),
        (N'Sup.OpenOrderRequest.ShowAll'),
        (N'Sup.OpenOrderRequest.View'),
        (N'Sup.OpenOrderRequest.HistoryView'),
        (N'Industries')
    ) r(RoleName);

    -- 3.3  74·Edit(5) → ثبت کامنت
    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName)
    SELECT @Base + N'/savecomment', 2, 1000, r.RoleName
    FROM (VALUES
        (N'SupplyAndPurchase'),
        (N'Sup.OpenOrderRequest.Supply'),
        (N'Sup.OpenOrderRequest.EngineeringAccept'),
        (N'Sup.OpenOrderRequest.Industrial'),
        (N'Industries')
    ) r(RoleName);

    -- 3.4  74·Accept_Engineering(12) / 74·SalesOrProjectPermission(187)
    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName) VALUES
        (@Base + N'/engineeringaccept/{id}',  2, 1000, N'Sup.OpenOrderRequest.EngineeringAccept'),
        (@Base + N'/salesorprojectaccept',    2, 1000, N'Sup.OpenOrderRequest.SalesOrProjectAccept');

    -- 3.5  74·Start(23) / Sending(24) / Query(25) / Terminate(101) (+ /delete = خاتمه نرم)
    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName) VALUES
        (@Base + N'/triggering/{id}',      2, 1000, N'Sup.OpenOrderRequest.Start'),
        (@Base + N'/triggering/{id}',      2, 1000, N'SupplyAndPurchase'),
        (@Base + N'/setinwaystatus/{id}',  2, 1000, N'Sup.OpenOrderRequest.Sending'),
        (@Base + N'/setinwaystatus/{id}',  2, 1000, N'SupplyAndPurchase'),
        (@Base + N'/statusinquiry/{id}',   2, 1000, N'Sup.OpenOrderRequest.Query'),
        (@Base + N'/statusinquiry/{id}',   2, 1000, N'SupplyAndPurchase'),
        (@Base + N'/terminate/{id}',       2, 1000, N'Sup.OpenOrderRequest.Terminate'),
        (@Base + N'/terminate/{id}',       2, 1000, N'SupplyAndPurchase'),
        (@Base + N'/delete',               2, 6,    N'Sup.OpenOrderRequest.Terminate'),
        (@Base + N'/delete',               2, 6,    N'SupplyAndPurchase');

    -- 3.6  71·New/Edit/Delete (FullAccess صفحه 71: گروه‌های 17، 27، 70)
    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName)
    SELECT a.Path, 2, 1000, r.RoleName
    FROM (VALUES (@Base + N'/saveattachment'), (@Base + N'/deleteattachment')) a(Path)
    CROSS JOIN (VALUES
        (N'SupplyAndPurchase'),
        (N'Sup.OpenOrderRequest.Supply'),
        (N'Sup.OpenOrderRequest.EngineeringAccept')
    ) r(RoleName);

    -- 3.7  74·HasEngineeringPermission(82) → اتصال مدارک پروژه (D42)
    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName) VALUES
        (@Base + N'/dolinkvpis', 2, 1000, N'Sup.OpenOrderRequest.HasEngineering'),
        (@Base + N'/dolinkvpis', 2, 1000, N'Sup.OpenOrderRequest.EngineeringAccept'),
        (@Base + N'/dolinkvpis', 2, 1000, N'SupplyAndPurchase');

    -- 3.8  76·FullAccess → درخواست‌کنندگان و ذینفعان (گروه‌های 430 و 28)
    INSERT INTO @Rows (Path, AccessType, ItemType, RoleName) VALUES
        (@Base + N'/addrequestedpersoneltoopenrequest', 2, 1000, N'Sup.OpenOrderRequest.ConfigManage'),
        (@Base + N'/addrequestedpersoneltoopenrequest', 2, 1000, N'Sup.OpenOrderRequest.Industrial'),
        (@Base + N'/addrequestedpersoneltoopenrequest', 2, 1000, N'Industries'),
        (@Base + N'/addrequestedpersoneltoopenrequest', 2, 1000, N'SupplyAndPurchase');

    -- نقش‌هایی که در این DB وجود ندارند گزارش می‌شوند (ردیفشان درج نمی‌شود)
    SELECT DISTINCT x.RoleName AS MissingRole
    FROM @Rows x
    WHERE NOT EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = x.RoleName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT x.Path, x.AccessType, x.ItemType, @Entity, N'', NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Rows x
    INNER JOIN system.Role r ON r.Name = x.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType);
    PRINT N'  Inserted RoleAccess rows (actions): ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4] دسترسی پروفایل‌های داده (D39) ===';
    -----------------------------------------------------------------------------
    -- View → «مشاهده عمومی» (103)؛ HistoryView → «پیشینه درخواست ها» (73)؛ گروه‌های HTS 70/44 (Read 77) → EngineeringAccept/ShowAll هم 73
    DECLARE @ProfileRole TABLE (ProfileName NVARCHAR(100), RoleName NVARCHAR(200));
    INSERT INTO @ProfileRole (ProfileName, RoleName) VALUES
        (N'vw_OpenOrderRequestOtherView',          N'Sup.OpenOrderRequest.View'),
        (N'vw_openOrderRequestPurchaseCompleted',  N'Sup.OpenOrderRequest.HistoryView'),
        (N'vw_openOrderRequestPurchaseCompleted',  N'Sup.OpenOrderRequest.EngineeringAccept'),
        (N'vw_openOrderRequestPurchaseCompleted',  N'Sup.OpenOrderRequest.ShowAll');

    SELECT DISTINCT p.ProfileName AS MissingProfile
    FROM @ProfileRole p
    WHERE NOT EXISTS (SELECT 1 FROM system.SavedQuery q WHERE q.Name = p.ProfileName AND q.Type = 1);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)), 3, 7, @Entity, q.Title, NULL, q.Id, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @ProfileRole p
    INNER JOIN system.SavedQuery q ON q.Name = p.ProfileName AND q.Type = 1
    INNER JOIN system.Role r ON r.Name = p.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = r.Id);
    PRINT N'  Inserted DataProfile RoleAccess rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_OpenOrderRequest_RoleAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-- ---------------------------------------------------------------- verify
SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.ActionAccessItemType
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path LIKE N'/panel/sup/openorderrequest/%'
ORDER BY ra.Path, r.Name;

SELECT r.Name AS RoleName, ra.Path, q.Name AS ProfileName, q.Title
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
WHERE ra.ActionAccessType = 3
  AND q.EntityFullName = N'entities.app.sup.openorderrequest'
ORDER BY q.Name, r.Name;
