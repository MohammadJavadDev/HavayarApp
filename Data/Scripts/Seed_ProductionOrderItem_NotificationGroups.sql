/*
================================================================================
Seed_ProductionOrderItem_NotificationGroups.sql
================================================================================
گروه‌های اعلان (system.NotificationGroup) سفارش ساخت / اقلام سفارش ساخت — جایگزین لیست‌های ایمیل سخت‌کد و
گروه‌های کاربری HTS. اعضا را از پنل «گروه‌های اعلان» اضافه کنید (این اسکریپت عضو اضافه نمی‌کند).

  Code                                              معادل HTS
  ------------------------------------------------  ---------------------------------------------------------
  Sale.ProductionOrderItem.Industrial               GetIndustrialStaticReceivers / گیرندگان isSendToPlanningUnitMode
  Sale.ProductionOrderItem.ProductionStatusChange   GetChangeStatusStaticReceivers (CC اعلان تغییر وضعیت تولید)
  Sale.ProductionOrderItem.TestStart                گروه کاربری 540 (شروع تست ۲۲۰۹ برای سفارش دارای مدیر پروژه)
  Sale.ProductionOrderItem.SerialChangeQc           گروه کاربری 538 (تغییر سریال قلم دارای بازرسی)
  Sale.ProductionOrderItem.NearDeliveryDate         گروه کاربری 567 (هشدار نزدیک به موعد تحویل)
  Sale.ProductionOrderItem.SupplyCommitteeExperts   گروه کاربری 380 (کارشناسان رئیس کمیته تامین)
  Sale.ProductionOrderItemBom.Industrial            لیست صنایع اعلان BOM (قبلاً runtime ساخته می‌شد)
  Sale.ProductionOrderItemBom.Engineering           لیست مهندسی اعلان BOM (قبلاً runtime ساخته می‌شد)

Idempotent روی Code. کدها باید با Services.ProductionOrderServices.ProductionOrderItemNotificationHelper یکی بمانند.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-notificationgroups';

DECLARE @Groups TABLE (Code NVARCHAR(100), DisplayName NVARCHAR(200), Description NVARCHAR(1500));
INSERT INTO @Groups (Code, DisplayName, Description) VALUES
 (N'Sale.ProductionOrderItem.Industrial',             N'سفارش ساخت - اقلام - واحد صنایع',                    N'گیرندگان اعلان ارجاع قلم به کارتابل صنایع (معادل لیست ثابت صنایع در HTS)'),
 (N'Sale.ProductionOrderItem.ProductionStatusChange', N'سفارش ساخت - اقلام - تغییر وضعیت تولید',             N'CC ثابت اعلان تغییر وضعیت تولید قلم (معادل GetChangeStatusStaticReceivers در HTS)'),
 (N'Sale.ProductionOrderItem.TestStart',              N'سفارش ساخت - اقلام - شروع تست (IT Support)',          N'اعلان شروع تست تولید (۲۲۰۹) برای سفارش‌های دارای مدیر پروژه (معادل گروه 540 در HTS)'),
 (N'Sale.ProductionOrderItem.SerialChangeQc',         N'سفارش ساخت - اقلام - تغییر سریال (کنترل کیفیت)',      N'اعلان تغییر سریال قلمی که بازرسی حین ساخت دارد (معادل گروه 538 در HTS)'),
 (N'Sale.ProductionOrderItem.NearDeliveryDate',       N'سفارش ساخت - اقلام - نزدیک به موعد تحویل',            N'دریافت‌کنندگان هشدار کالاهای نزدیک به موعد تحویل (معادل گروه 567 در HTS)'),
 (N'Sale.ProductionOrderItem.SupplyCommitteeExperts', N'سفارش ساخت - اقلام - کارشناسان کمیته تامین',          N'گیرندگان اعلان‌های استعلام/کمیته تامین (معادل گروه 380 در HTS)'),
 (N'Sale.ProductionOrderItemBom.Industrial',          N'سفارش ساخت - BOM - واحد صنایع',                       N'گیرندگان اصلی اعلان افزودن/ویرایش BOM (معادل لیست صنایع در HTS)'),
 (N'Sale.ProductionOrderItemBom.Engineering',         N'سفارش ساخت - BOM - واحد مهندسی',                      N'گیرندگان CC اعلان BOM (معادل لیست مهندسی در HTS)');

INSERT INTO system.NotificationGroup
    (Code, DisplayName, Description,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT g.Code, g.DisplayName, g.Description,
       1, 1, @SeedUser, @SeedUser,
       @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM @Groups g
WHERE NOT EXISTS (SELECT 1 FROM system.NotificationGroup n WHERE n.Code = g.Code);
PRINT N'NotificationGroup rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

SELECT n.Id, n.Code, n.DisplayName,
       (SELECT COUNT(*) FROM system.NotificationGroupMember m WHERE m.NotificationGroupId = n.Id AND m.IsActive = 1) AS Members
FROM system.NotificationGroup n
WHERE n.Code IN (SELECT Code FROM @Groups)
ORDER BY n.Code;
