/*
================================================================================
Seed_IndustrialItSupport_NotificationGroups.sql
================================================================================
فعال‌سازی مجدد اعلان‌های IT Support واحد صنایع مطابق گیرندگان سخت‌کد HTS.

  گروه                                              منبع HTS
  ------------------------------------------------  ---------------------------------------------------------
  Sale.ProductionOrderItemBom.Industrial            ProductionOrderItemBomService.GetIndustrialStaticReceivers
  Sale.ProductionOrderItemBom.Engineering           ProductionOrderItemBomService.GetEngineeringStaticReceivers
  Bom.FormulChange.Applied                          BomChangeRequestController.SendChangePartNotificationEmail (To)
  Bom.FormulChange.AppliedExtra                     همان متد، وقتی قطعه در BOM قلم سفارش ساخت باز وجود دارد
  Bom.FormulChange.AppliedCc                        CC ثابت (abdolmaleki.h) + CC پیشوند کالا در کد
  Bom.FormulChange.Cartable                         SendEmailNotificationForEngineeringRequests (To = Neysari.n)
  Bom.FormulChange.CartableCc                       همان متد (CC صنایع)
  Sup.OpenOrderRequest.ArrivalWarehouse             WriteData رسید کامل: zamani.z و Zandi.n که در گروه نبودند

Idempotent. عضو تکراری اضافه نمی‌شود. کاربران پیدانشده در خروجی آخر گزارش می‌شوند.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-industrial-itsupport';

DECLARE @Groups TABLE (Code NVARCHAR(100), DisplayName NVARCHAR(200), Description NVARCHAR(1500));
INSERT INTO @Groups (Code, DisplayName, Description) VALUES
 (N'Bom.FormulChange.Applied',      N'BOM محصول - ایجاد/تغییر در اقلام',           N'گیرندگان ایمیل ایجاد/ویرایش/حذف قلم BOM محصول (معادل SendChangePartNotificationEmail در HTS)'),
 (N'Bom.FormulChange.AppliedExtra', N'BOM محصول - اقلام باز سفارش ساخت',          N'گیرندگان اضافه وقتی قطعه تغییرکرده در BOM قلم سفارش ساخت باز هم وجود دارد'),
 (N'Bom.FormulChange.AppliedCc',    N'BOM محصول - رونوشت ثابت',                    N'رونوشت ثابت اعلان ایجاد/تغییر در اقلام'),
 (N'Bom.FormulChange.Cartable',     N'BOM محصول - کارتابل مهندسی',                 N'گیرنده اصلی درخواست مشاهده کارتابل تغییر BOM'),
 (N'Bom.FormulChange.CartableCc',   N'BOM محصول - رونوشت کارتابل صنایع',           N'رونوشت واحد صنایع برای درخواست مشاهده کارتابل تغییر BOM');

INSERT INTO system.NotificationGroup
    (Code, DisplayName, Description,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT g.Code, g.DisplayName, g.Description,
       1, 1, @SeedUser, @SeedUser,
       @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM @Groups g
WHERE NOT EXISTS (SELECT 1 FROM system.NotificationGroup n WHERE n.Code = g.Code);

DECLARE @Map TABLE (GroupCode NVARCHAR(100), Username NVARCHAR(200));
INSERT INTO @Map (GroupCode, Username) VALUES
 -- BOM قلم سفارش ساخت / صنایع
 -- HTS قبل از ارسال، Mohammadi.ma را از To و CC حذف می‌کند؛ اینجا عضو نمی‌شود.
 (N'Sale.ProductionOrderItemBom.Industrial', N'Khalili.aa'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'Mehrabi.s'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'zamani.z'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'Zandi.n'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'Eezi.a'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'Azadbakhsh.s'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'Moradmand.a'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'Azadbakhsh.sh'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'Kian.r'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'ahmadi.sh'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'kaboli.f'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'Saemian.r'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'mosayebi.m'),
 (N'Sale.ProductionOrderItemBom.Industrial', N'salim.sh'),
 -- BOM قلم سفارش ساخت / مهندسی
 (N'Sale.ProductionOrderItemBom.Engineering', N'kadkhodaei.v'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'javadian.p'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Samii.m'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Bamimohammadi.gh'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Saeedi.m'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Mirlohi.a'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Neysari.n'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Erfani.m'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Ranjbar.v'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Kondori.v'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Sardashti.s'),
 (N'Sale.ProductionOrderItemBom.Engineering', N'Saemian.r'),
 -- ایجاد/تغییر در اقلام
 (N'Bom.FormulChange.Applied', N'Khalili.aa'),
 (N'Bom.FormulChange.Applied', N'Salim.sh'),
 (N'Bom.FormulChange.Applied', N'Golestaneh.a'),
 (N'Bom.FormulChange.Applied', N'Kian.r'),
 (N'Bom.FormulChange.Applied', N'Azadbakhsh.s'),
 (N'Bom.FormulChange.Applied', N'Moradmand.a'),
 (N'Bom.FormulChange.Applied', N'Azadbakhsh.sh'),
 (N'Bom.FormulChange.Applied', N'Saemian.r'),
 (N'Bom.FormulChange.Applied', N'Mehrabi.s'),
 (N'Bom.FormulChange.Applied', N'Eezi.a'),
 (N'Bom.FormulChange.Applied', N'givehchi.h'),
 (N'Bom.FormulChange.Applied', N'Mehrafzoon'),
 (N'Bom.FormulChange.Applied', N'mohammadi.s'),
 (N'Bom.FormulChange.AppliedExtra', N'kadkhodaei.v'),
 (N'Bom.FormulChange.AppliedExtra', N'Mirlohi.a'),
 (N'Bom.FormulChange.AppliedExtra', N'kalhor.m'),
 (N'Bom.FormulChange.AppliedExtra', N'masoudi.l'),
 (N'Bom.FormulChange.AppliedExtra', N'piri.m'),
 (N'Bom.FormulChange.AppliedExtra', N'masoumi.s'),
 (N'Bom.FormulChange.AppliedExtra', N'marashi.m'),
 (N'Bom.FormulChange.AppliedExtra', N'Asgari.v'),
 (N'Bom.FormulChange.AppliedExtra', N'norouzi.m'),
 (N'Bom.FormulChange.AppliedExtra', N'Sazvar.m'),
 (N'Bom.FormulChange.AppliedExtra', N'mahani.b'),
 (N'Bom.FormulChange.AppliedExtra', N'golestaneh.m'),
 (N'Bom.FormulChange.AppliedExtra', N'Ahmadpour.s'),
 (N'Bom.FormulChange.AppliedExtra', N'ebrahimi.f'),
 (N'Bom.FormulChange.AppliedExtra', N'javadian.p'),
 (N'Bom.FormulChange.AppliedExtra', N'Samii.m'),
 (N'Bom.FormulChange.AppliedExtra', N'Saeedi.m'),
 (N'Bom.FormulChange.AppliedExtra', N'Neysari.n'),
 (N'Bom.FormulChange.AppliedExtra', N'Kondori.v'),
 (N'Bom.FormulChange.AppliedExtra', N'Erfani.m'),
 (N'Bom.FormulChange.AppliedExtra', N'bagheri.m'),
 (N'Bom.FormulChange.AppliedExtra', N'noroozi.s'),
 (N'Bom.FormulChange.AppliedExtra', N'sharifi.mo'),
 (N'Bom.FormulChange.AppliedCc', N'abdolmaleki.h'),
 (N'Bom.FormulChange.Cartable', N'Neysari.n'),
 (N'Bom.FormulChange.CartableCc', N'Khalili.aa'),
 (N'Bom.FormulChange.CartableCc', N'Golestaneh.a'),
 (N'Bom.FormulChange.CartableCc', N'Kian.r'),
 (N'Bom.FormulChange.CartableCc', N'Azadbakhsh.s'),
 (N'Bom.FormulChange.CartableCc', N'Moradmand.a'),
 (N'Bom.FormulChange.CartableCc', N'Azadbakhsh.sh'),
 (N'Bom.FormulChange.CartableCc', N'Mehrabi.s'),
 (N'Bom.FormulChange.CartableCc', N'zamani.z'),
 (N'Bom.FormulChange.CartableCc', N'Eezi.a'),
 (N'Bom.FormulChange.CartableCc', N'abdolmaleki.h'),
 (N'Bom.FormulChange.CartableCc', N'Golestaneh.m'),
 (N'Bom.FormulChange.CartableCc', N'Saemian.r'),
 -- رسید کامل انبار: این دو نفر در HTS بودند و در گروه پنل نبودند
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'zamani.z'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse', N'Zandi.n');

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Resolved TABLE (GroupId BIGINT, GroupCode NVARCHAR(100), UserId BIGINT, Email NVARCHAR(200), FullName NVARCHAR(200));
    INSERT INTO @Resolved (GroupId, GroupCode, UserId, Email, FullName)
    SELECT DISTINCT g.Id, g.Code, u.Id,
           COALESCE(NULLIF(u.Email, N''), u.Username + N'@havayar.com'),
           COALESCE(NULLIF(u.NameFa, N''), NULLIF(u.Name, N''), u.Username)
    FROM @Map m
    INNER JOIN system.NotificationGroup g ON g.Code = m.GroupCode
    INNER JOIN system.[User] u ON LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1;

    INSERT INTO system.NotificationGroupMember
        (NotificationGroupId, UserId, Email, FullName,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT r.GroupId, r.UserId, r.Email, r.FullName,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Resolved r
    WHERE NOT EXISTS (
        SELECT 1 FROM system.NotificationGroupMember x
        WHERE x.NotificationGroupId = r.GroupId AND x.UserId = r.UserId);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

SELECT DISTINCT m.GroupCode, m.Username
FROM @Map m
WHERE NOT EXISTS (SELECT 1 FROM system.[User] u WHERE LOWER(u.Username) = LOWER(m.Username) AND u.IsActive = 1)
ORDER BY m.GroupCode, m.Username;

SELECT g.Code, COUNT(x.Id) AS Members
FROM system.NotificationGroup g
LEFT JOIN system.NotificationGroupMember x ON x.NotificationGroupId = g.Id AND x.IsActive = 1
WHERE g.Code IN (
    N'Sale.ProductionOrderItemBom.Industrial',
    N'Sale.ProductionOrderItemBom.Engineering',
    N'Bom.FormulChange.Applied',
    N'Bom.FormulChange.AppliedExtra',
    N'Bom.FormulChange.AppliedCc',
    N'Bom.FormulChange.Cartable',
    N'Bom.FormulChange.CartableCc',
    N'Sup.OpenOrderRequest.ArrivalWarehouse')
GROUP BY g.Code
ORDER BY g.Code;
